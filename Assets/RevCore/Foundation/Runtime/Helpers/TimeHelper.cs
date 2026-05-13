using System;
using System.Globalization;
using System.Text;

namespace RevCore
{
	public static class TimeHelper
	{
		public static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly StringBuilder s_builder = new();

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

		public static double GetSecondsTillMidNight(DateTime from) => (from.Date.AddDays(1) - from).TotalSeconds;
		public static double GetSecondsTillTargetHour(DateTime from, int targetHour)
		{
			if (targetHour < 0 || targetHour > 23) throw new ArgumentException("Hour must be between 0 and 23.");
			var targetTime = new DateTime(from.Year, from.Month, from.Day, targetHour, 0, 0);
			if (from >= targetTime) targetTime = targetTime.AddDays(1);
			return (targetTime - from).TotalSeconds;
		}

		public static DateTime GetStartTimeOfNextWeekDay(DateTime date, DayOfWeek day)
		{
			int daysUntil = ((int)day - (int)date.DayOfWeek + 7) % 7;
			if (daysUntil == 0) daysUntil = 7;
			var target = date.AddDays(daysUntil);
			return new DateTime(target.Year, target.Month, target.Day, 0, 0, 0, 0);
		}

		public static DateTime GetEndTimeOfNextWeekDay(DateTime date, DayOfWeek day) => GetStartTimeOfNextWeekDay(date, day).AddDays(1).Date;
		public static DateTime GetStartTimeOfMonth(DateTime date) => new(date.Year, date.Month, 1);
		public static DateTime GetEndTimeOfMonth(DateTime date) => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 23, 59, 59, 999);

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

		public static bool TryParse(string timeString, out DateTime output, DateTime fallback)
		{
			if (string.IsNullOrEmpty(timeString)) { output = fallback; return false; }
			if (DateTime.TryParse(timeString, out output)) return true;
			if (DateTime.TryParse(timeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out output)) return true;
			output = fallback;
			return false;
		}

		public static DateTime UnixTimestampToDateTime(double unixTimestamp) => Epoch.AddSeconds(unixTimestamp);
		public static DateTime UnixTimestampToDateTime(int unixTimestamp) => Epoch.AddSeconds(unixTimestamp);
		public static long DateTimeToUnixTimestamp(DateTime value) => (long)(value - Epoch).TotalSeconds;
		public static int DateTimeToUnixTimestampInt(DateTime value) => (int)(value - Epoch).TotalSeconds;

		public static int GetCurrentWeekNumber(DateTime date)
		{
			var calendar = CultureInfo.InvariantCulture.Calendar;
			return calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		}
	}

	public static class TimeExtension
	{
		public static long ToUnixTimestamp(this DateTime value) => (long)(value - TimeHelper.Epoch).TotalSeconds;
		public static int ToUnixTimestampInt(this DateTime value) => (int)(value - TimeHelper.Epoch).TotalSeconds;
	}
}
