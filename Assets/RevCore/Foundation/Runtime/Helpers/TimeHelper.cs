using System;
using System.Globalization;
using System.Text;

namespace RevCore
{
	/// <summary>Static helpers for dates, durations, and Unix timestamps. All formatting methods share an internal <see cref="StringBuilder"/> — not thread-safe.</summary>
	public static class TimeHelper
	{
		/// <summary>Unix epoch (1970-01-01T00:00:00Z).</summary>
		public static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly StringBuilder s_builder = new();

		/// <summary>
		/// Formats a duration as <c>"3d 4h 12m 30s"</c>, emitting at most <paramref name="maxSplits"/>
		/// granularity levels (so <c>maxSplits=2</c> gives <c>"3d 4h"</c>). Zero or negative input returns empty.
		/// </summary>
		public static string FormatDHMs(double seconds, int maxSplits = 2)
		{
			if (seconds <= 0) return "";
			int split = 0;
			s_builder.Length = 0;
			var t = TimeSpan.FromSeconds(seconds);
			if (t.Days > 0) { split++; s_builder.Append(t.Days).Append("d"); }
			if ((t.Hours > 0 || t.Minutes > 0 || t.Seconds > 0) && split < maxSplits)
			{
				if (split > 0) s_builder.Append(" ");
				if (split > 0 || t.Hours > 0) { split++; s_builder.Append(t.Hours).Append("h"); }
				if ((t.Minutes > 0 || t.Seconds > 0) && split < maxSplits)
				{
					if (split > 0) s_builder.Append(" ");
					if (split > 0 || t.Minutes > 0) { split++; s_builder.Append(t.Minutes).Append("m"); }
					if (t.Seconds > 0 && split < maxSplits) { if (split > 0) s_builder.Append(" "); s_builder.Append(t.Seconds).Append("s"); }
				}
			}
			return s_builder.ToString();
		}

		/// <summary>
		/// Formats a duration as <c>HH:MM:SS</c>, dropping leading zero components when <paramref name="maxSplits"/>
		/// is 2 or 3. <c>maxSplits=4</c> includes a leading <c>Nd</c> day count.
		/// </summary>
		public static string FormatHhMmSs(double seconds, int maxSplits = 3)
		{
			int totalSeconds = (int)seconds;
			int totalHours = totalSeconds / 3600;
			int minutes = totalSeconds % 3600 / 60;
			int secs = totalSeconds % 60;
			s_builder.Length = 0;
			if (maxSplits >= 4)
			{
				int days = totalHours / 24;
				int hours = totalHours % 24;
				s_builder.Append(days).Append('d').Append(hours).Append(':').Append(minutes.ToString("D2")).Append(':').Append(secs.ToString("D2"));
			}
			else if (maxSplits == 3)
			{
				if (totalHours > 0) s_builder.Append(totalHours).Append(':').Append(minutes.ToString("D2")).Append(':').Append(secs.ToString("D2"));
				else s_builder.Append(minutes).Append(':').Append(secs.ToString("D2"));
			}
			else if (maxSplits == 2)
			{
				if (totalHours > 0) s_builder.Append(totalHours).Append(':').Append(minutes.ToString("D2"));
				else s_builder.Append(minutes).Append(':').Append(secs.ToString("D2"));
			}
			else s_builder.Append(seconds);
			return s_builder.ToString();
		}

		/// <summary>Seconds from <paramref name="from"/> to the next midnight in <paramref name="from"/>'s timezone.</summary>
		public static double GetSecondsTillMidNight(DateTime from) => (from.Date.AddDays(1) - from).TotalSeconds;

		/// <summary>
		/// Seconds from <paramref name="from"/> to the next occurrence of <paramref name="targetHour"/>:00.
		/// If <paramref name="from"/> is already past today's <paramref name="targetHour"/>, the next day's is used.
		/// </summary>
		/// <exception cref="ArgumentException"><paramref name="targetHour"/> is outside 0–23.</exception>
		public static double GetSecondsTillTargetHour(DateTime from, int targetHour)
		{
			if (targetHour < 0 || targetHour > 23) throw new ArgumentException("Hour must be between 0 and 23.");
			var targetTime = new DateTime(from.Year, from.Month, from.Day, targetHour, 0, 0);
			if (from >= targetTime) targetTime = targetTime.AddDays(1);
			return (targetTime - from).TotalSeconds;
		}

		/// <summary>Returns midnight of the next <paramref name="day"/> after <paramref name="date"/>. If <paramref name="date"/> is already on that <paramref name="day"/>, returns next week's.</summary>
		public static DateTime GetStartTimeOfNextWeekDay(DateTime date, DayOfWeek day)
		{
			int daysUntil = ((int)day - (int)date.DayOfWeek + 7) % 7;
			if (daysUntil == 0) daysUntil = 7;
			var target = date.AddDays(daysUntil);
			return new DateTime(target.Year, target.Month, target.Day, 0, 0, 0, 0);
		}

		/// <summary>Returns midnight of the day after the next <paramref name="day"/> — i.e. the end of that day.</summary>
		public static DateTime GetEndTimeOfNextWeekDay(DateTime date, DayOfWeek day) => GetStartTimeOfNextWeekDay(date, day).AddDays(1).Date;

		/// <summary>Returns the first instant of the month containing <paramref name="date"/>.</summary>
		public static DateTime GetStartTimeOfMonth(DateTime date) => new(date.Year, date.Month, 1);

		/// <summary>Returns 23:59:59.999 of the last day of the month containing <paramref name="date"/>.</summary>
		public static DateTime GetEndTimeOfMonth(DateTime date) => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 23, 59, 59, 999);

		/// <summary>
		/// Computes how many complete <paramref name="intervalSeconds"/>-long steps fit into <paramref name="totalSeconds"/>,
		/// returning the leftover seconds via <paramref name="modSeconds"/>.
		/// </summary>
		/// <returns>Number of full steps. Zero (and <paramref name="modSeconds"/> == <paramref name="intervalSeconds"/>) when <paramref name="totalSeconds"/> is &lt;= 0.</returns>
		public static int CalcStepsPassed(int totalSeconds, int intervalSeconds, out int modSeconds)
		{
			int steps = 0;
			modSeconds = intervalSeconds;
			if (totalSeconds > 0)
			{
				steps += totalSeconds / intervalSeconds;
				modSeconds = totalSeconds % intervalSeconds;
			}
			return steps;
		}

		/// <summary>
		/// Tries to parse a date string under the current culture, then under invariant culture.
		/// Returns <c>false</c> and assigns <paramref name="fallback"/> on failure (including null/empty input).
		/// </summary>
		public static bool TryParse(string timeString, out DateTime output, DateTime fallback)
		{
			if (string.IsNullOrEmpty(timeString)) { output = fallback; return false; }
			if (DateTime.TryParse(timeString, out output)) return true;
			if (DateTime.TryParse(timeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out output)) return true;
			output = fallback;
			return false;
		}

		/// <summary>Converts a Unix timestamp (seconds since epoch) to a UTC <see cref="DateTime"/>.</summary>
		public static DateTime UnixTimestampToDateTime(double unixTimestamp) => Epoch.AddSeconds(unixTimestamp);

		/// <summary>Converts a Unix timestamp (seconds since epoch) to a UTC <see cref="DateTime"/>.</summary>
		public static DateTime UnixTimestampToDateTime(int unixTimestamp) => Epoch.AddSeconds(unixTimestamp);

		/// <summary>Converts a <see cref="DateTime"/> to seconds since Unix epoch as a 64-bit integer.</summary>
		public static long DateTimeToUnixTimestamp(DateTime value) => (long)(value - Epoch).TotalSeconds;

		/// <summary>Converts a <see cref="DateTime"/> to seconds since Unix epoch as a 32-bit integer (overflows after 2038-01-19).</summary>
		public static int DateTimeToUnixTimestampInt(DateTime value) => (int)(value - Epoch).TotalSeconds;

		/// <summary>Returns the ISO 8601 week-of-year number for <paramref name="date"/> using the FirstFourDayWeek rule and Monday start.</summary>
		public static int GetCurrentWeekNumber(DateTime date)
		{
			var calendar = CultureInfo.InvariantCulture.Calendar;
			return calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		}

		/// <summary>Returns <see cref="DateTime.UtcNow"/> if <paramref name="utc"/>, otherwise <see cref="DateTime.Now"/>.</summary>
		public static DateTime GetNow(bool utc) => utc ? DateTime.UtcNow : DateTime.Now;

		/// <summary>Returns the current Unix timestamp (32-bit seconds). UTC vs local toggled by <paramref name="utc"/>.</summary>
		public static int GetNowTimestamp(bool utc) => DateTimeToUnixTimestampInt(GetNow(utc));

		/// <summary>Seconds from <paramref name="from"/> to the next occurrence of <paramref name="day"/> at midnight (uses <see cref="GetStartTimeOfNextWeekDay"/>).</summary>
		public static double GetSecondsTillDayOfWeek(DayOfWeek day, DateTime from) => (GetStartTimeOfNextWeekDay(from, day) - from).TotalSeconds;
	}

	/// <summary>Extension methods on <see cref="DateTime"/>.</summary>
	public static class TimeExtension
	{
		/// <summary>Converts to Unix timestamp as a 64-bit integer.</summary>
		public static long ToUnixTimestamp(this DateTime value) => (long)(value - TimeHelper.Epoch).TotalSeconds;

		/// <summary>Converts to Unix timestamp as a 32-bit integer (overflows after 2038-01-19).</summary>
		public static int ToUnixTimestampInt(this DateTime value) => (int)(value - TimeHelper.Epoch).TotalSeconds;
	}
}
