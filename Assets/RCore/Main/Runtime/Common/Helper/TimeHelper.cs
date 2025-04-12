/**
 * Author HNB-RaBear - 2018
 **/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RCore
{
	public static class StringBuilderExtension
	{
		public static StringBuilder Clear(this StringBuilder pBuilder)
		{
			pBuilder.Length = 0;
			pBuilder.Capacity = 0;
			return pBuilder;
		}
	}

	public class TimeHelper
	{
		public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static StringBuilder m_TimeBuilder = new StringBuilder();
		private static bool m_HasInternet;

		/// <summary>
		/// d h m s
		/// </summary>
		public static string FormatDHMs(double seconds, int pMaxSplits = 2)
		{
			int split = 0;
			if (seconds > 0)
			{
				m_TimeBuilder.Clear();
				var t = TimeSpan.FromSeconds(seconds);

				if (t.Days > 0)
				{
					split++;
					m_TimeBuilder.Append(t.Days).Append("d");
				}

				if ((t.Hours > 0 || t.Minutes > 0 || t.Seconds > 0) && split < pMaxSplits)
				{
					if (split > 0)
						m_TimeBuilder.Append(" ");

					if (split > 0 || t.Hours > 0)
					{
						split++;
						m_TimeBuilder.Append(t.Hours).Append("h");
					}

					if ((t.Minutes > 0 || t.Seconds > 0) && split < pMaxSplits)
					{
						if (split > 0)
							m_TimeBuilder.Append(" ");

						if (split > 0 || t.Minutes > 0)
						{
							split++;
							m_TimeBuilder.Append(t.Minutes).Append("m");
						}

						if (t.Seconds > 0 && split < pMaxSplits)
						{
							if (split > 0)
								m_TimeBuilder.Append(" ");

							m_TimeBuilder.Append(t.Seconds).Append("s");
						}
					}
				}
				return m_TimeBuilder.ToString();
			}

			return "";
		}

		/// <summary>
		/// 00:00:00
		/// </summary>
		public static string FormatHhMmSs(double seconds, int pMaxSplits = 3)
		{
			int totalSeconds = (int)seconds;
			int totalHours = totalSeconds / 3600;
			int minutes = totalSeconds % 3600 / 60;
			int secs = totalSeconds % 60;

			m_TimeBuilder.Clear();

			// Format the time based on pMaxSplits
			if (pMaxSplits >= 4)
			{
				// Include days if pMaxSplits >= 4
				int days = totalHours / 24;
				int hours = totalHours % 24;
				m_TimeBuilder.Append(days.ToString()).Append('d').Append(hours.ToString()).Append(':').Append(minutes.ToString("D2")).Append(':').Append(secs.ToString("D2"));
			}
			else if (pMaxSplits == 3)
			{
				// Format as total hours:MM:SS
				m_TimeBuilder.Append(totalHours.ToString()).Append(':').Append(minutes.ToString("D2")).Append(':').Append(secs.ToString("D2"));
			}
			else if (pMaxSplits == 2)
			{
				// Format as HH:MM or MM:SS based on whether totalHours is non-zero
				if (totalHours > 0)
				{
					// Include total hours if totalHours > 0
					m_TimeBuilder.Append(totalHours.ToString()).Append(':').Append(minutes.ToString("D2"));
				}
				else
				{
					// Only minutes and seconds if total hours are 0
					m_TimeBuilder.Append(minutes.ToString()).Append(':').Append(secs.ToString("D2"));
				}
			}
			else
				m_TimeBuilder.Append(seconds);

			return m_TimeBuilder.ToString();
		}
		public static double GetSecondsTillMidNightUtc()
		{
			var utcNow = GetServerTimeUtc() ?? DateTime.UtcNow;
			var secondsTillMidNight = GetSecondsTillMidNight(utcNow);
			return secondsTillMidNight;
		}

		public static double GetSecondsTillMidNight(DateTime pFrom)
		{
			var midNight = pFrom.Date.AddDays(1);
			var remainingTime = (midNight - pFrom).TotalSeconds;
			return remainingTime;
		}

		public static double GetSecondsTillTargetHour(DateTime pFrom, int targetHour) // 0 - 23
		{
			if (targetHour < 0 || targetHour > 23)
				throw new ArgumentException("Hour must be between 0 and 23.");

			var targetTime = new DateTime(pFrom.Year, pFrom.Month, pFrom.Day, targetHour, 0, 0);

			if (pFrom >= targetTime)
				targetTime = targetTime.AddDays(1);

			return (targetTime - pFrom).TotalSeconds;
		}

		/// <summary>
		/// Get the start time of the next occurrence of a specified weekday based on a given date
		/// </summary>
		public static DateTime GetStartTimeOfNextWeekDay(DateTime pDate, DayOfWeek pDay)
		{
			// Calculate the number of days until the next occurrence of the specified day
			int daysUntilNextWeekDay = ((int)pDay - (int)pDate.DayOfWeek + 7) % 7;
			if (daysUntilNextWeekDay == 0)
				daysUntilNextWeekDay = 7;
			var targetDate = pDate.AddDays(daysUntilNextWeekDay);
			return new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, 0, 0, 0, 0);
		}


		/// <summary>
		/// Get the end time of the next occurrence of a specified weekday based on a given date
		/// </summary>
		public static DateTime GetEndTimeOfNextWeekDay(DateTime pDate, DayOfWeek pDay)
		{
			var startTime = GetStartTimeOfNextWeekDay(pDate, pDay);
			return startTime.AddDays(1).Date;
		}

		/// <summary>
		/// Get start time of month next from date
		/// </summary>
		public static DateTime GetStartTimeOfMonth(DateTime pDate)
		{
			return new DateTime(pDate.Year, pDate.Month, 1).Date;
		}

		/// <summary>
		/// Get end of month next from date
		/// </summary>
		public static DateTime GetEndTimeOfMonth(DateTime pDate)
		{
			var endOfMonth = new DateTime(pDate.Year, pDate.Month, DateTime.DaysInMonth(pDate.Year, pDate.Month), 23, 59, 59, 999);
			return endOfMonth;
		}

		/// <summary>
		/// Get mod in seconds from amount of time
		/// </summary>
		/// <param name="pIntervalSeconds"></param>
		/// <param name="pTotalSeconds"></param>
		/// <returns>Step Count and Remain Seconds</returns>
		public static int CalcStepsPassed(int pTotalSeconds, int pIntervalSeconds, out int pModSeconds)
		{
			int stepsPassed = 0;
			pModSeconds = pIntervalSeconds;
			if (pTotalSeconds > 0)
			{
				stepsPassed += Mathf.FloorToInt(pTotalSeconds * 1f / pIntervalSeconds);
				pModSeconds = pTotalSeconds % pIntervalSeconds;
			}
			return stepsPassed;
		}

		/// <summary>
		/// Server time have format type MM/dd/yyyy HH:mm:ss
		/// </summary>
		public static bool TryParse(string pTimeString, out DateTime pOutput, DateTime pFallback)
		{
			if (string.IsNullOrEmpty(pTimeString))
			{
				pOutput = pFallback;
				return false;
			}

			if (DateTime.TryParse(pTimeString, out pOutput))
				return true;

			if (DateTime.TryParse(pTimeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out pOutput))
				return true;

			UnityEngine.Debug.LogError($"String was not recognized as a valid DateTime {pTimeString}");
			pOutput = pFallback;

			return false;
		}

		public static double GetSecondsTillDayOfWeek(DayOfWeek pDayOfWeek, DateTime pNow)
		{
			int dayCount = pDayOfWeek.GetHashCode() - pNow.DayOfWeek.GetHashCode();
			if (dayCount <= 0)
				dayCount += 7;
			double seconds = (pNow.AddDays(dayCount).Date - pNow).TotalSeconds;
			return seconds;
		}

		public static double GetSecondsTillEndDayOfWeek(DayOfWeek pDayOfWeek, DateTime pNow)
		{
			int dayCount = pDayOfWeek.GetHashCode() - pNow.DayOfWeek.GetHashCode() + 1;
			if (dayCount <= 0)
				dayCount += 7;
			double seconds = (pNow.AddDays(dayCount).Date - pNow).TotalSeconds;
			return seconds;
		}

		public static DateTime UnixTimestampToDateTime(float unixTimestamp)
		{
			var dtDateTime = Epoch.AddSeconds(unixTimestamp);
			return dtDateTime;
		}

		public static DateTime UnixTimestampToDateTime(double unixTimestamp)
		{
			var dtDateTime = Epoch.AddSeconds(unixTimestamp);
			return dtDateTime;
		}

		public static DateTime UnixTimestampToDateTime(int unixTimestamp)
		{
			var dtDateTime = Epoch.AddSeconds(unixTimestamp);
			return dtDateTime;
		}

		public static long DateTimeToUnixTimestamp(DateTime value)
		{
			var elapsedTime = value - Epoch;
			return (long)elapsedTime.TotalSeconds;
		}

		public static int DateTimeToUnixTimestampInt(DateTime value)
		{
			var elapsedTime = value - Epoch;
			return (int)elapsedTime.TotalSeconds;
		}

		public static void LogCulture()
		{
			var culture = CultureInfo.CurrentCulture;
			UnityEngine.Debug.Log($"[{culture.Name}]"
				+ $"\n Now: \t {DateTime.Now}"
				+ $"\n ShortDatePattern: \t {culture.DateTimeFormat.ShortDatePattern} \t {DateTime.Now.ToString(culture.DateTimeFormat.ShortDatePattern)}"
				+ $"\n LongTimePattern: \t {culture.DateTimeFormat.LongTimePattern} \t {DateTime.Now.ToString(culture.DateTimeFormat.LongTimePattern)}"
				+ $"\n ShortTimePattern: \t {culture.DateTimeFormat.ShortTimePattern} \t {DateTime.Now.ToString(culture.DateTimeFormat.ShortTimePattern)}"
				+ $"\n SortableDateTimePattern: \t {culture.DateTimeFormat.SortableDateTimePattern} \t {DateTime.Now.ToString(culture.DateTimeFormat.SortableDateTimePattern)}"
				+ $"\n UniversalSortableDateTimePattern: \t {culture.DateTimeFormat.UniversalSortableDateTimePattern} \t {DateTime.Now.ToString(culture.DateTimeFormat.UniversalSortableDateTimePattern)}");
		}

		public static DateTime? GetServerTimeUtc() => WebRequestHelper.GetServerTimeUtc();

		public static DateTime GetNow(bool utcTime)
		{
			var utcNow = GetServerTimeUtc() ?? DateTime.UtcNow;
			return utcTime ? utcNow : utcNow.ToLocalTime();
		}

		public static int GetNowTimestamp(bool utcTime)
		{
			int timestamp = DateTimeToUnixTimestampInt(GetNow(utcTime));
			return timestamp;
		}

		public static int GetCurrentWeekNumber(DateTime date)
		{
			// Get the ISO 8601 week number for the specified date
			var cultureInfo = CultureInfo.InvariantCulture;
			var calendar = cultureInfo.Calendar;

			// Specify that the first day of the week is Monday and
			// use the rule that the first week has at least 4 days
			var weekRule = CalendarWeekRule.FirstFourDayWeek;
			var firstDayOfWeek = DayOfWeek.Monday;

			// Get the week number
			int weekNumber = calendar.GetWeekOfYear(date, weekRule, firstDayOfWeek);

			return weekNumber;
		}

		public static int CalcSecondsPassed(DateTime fromDate, DateTime toDate, DayOfWeek[] includeDays, int[] includeHours)
		{
			bool ContainHour(int hour)
			{
				for (var i = 0; i < includeHours.Length; i++)
					if (includeHours[i] == hour)
						return true;
				return false;
			}
			bool ContainDay(DayOfWeek day)
			{
				for (var i = 0; i < includeDays.Length; i++)
					if (includeDays[i] == day)
						return true;
				return false;
			}
			
			double validSeconds = 0;
			while (fromDate < toDate)
			{
				// Check if the current day is an active day and the current hour is an active hour
				var date = fromDate;
				if (ContainDay(date.DayOfWeek) && ContainHour(date.Hour))
				{
					// Calculate the end of the current hour
					var endOfHour = fromDate.AddHours(1).AddMinutes(-fromDate.Minute).AddSeconds(-fromDate.Second);
					var nextTime = endOfHour < toDate ? endOfHour : toDate;

					// Add valid seconds
					validSeconds += (nextTime - fromDate).TotalSeconds;

					// Move to the next period
					fromDate = nextTime;
				}
				else
				{
					// Move to the next hour directly
					fromDate = fromDate.AddHours(1).AddMinutes(-fromDate.Minute).AddSeconds(-fromDate.Second);
				}
			}
			return (int)validSeconds;
		}

		public static DayOfWeek[] GetRandomDayOfWeeks()
		{
			int range = Random.Range(2, 8);
			var daysOfWeeks = new[]
			{
				DayOfWeek.Monday,
				DayOfWeek.Tuesday,
				DayOfWeek.Wednesday,
				DayOfWeek.Thursday,
				DayOfWeek.Friday,
				DayOfWeek.Saturday,
				DayOfWeek.Sunday,
			};
			// Fisher-Yates shuffle using UnityEngine.Random
			int n = daysOfWeeks.Length;
			for (int i = n - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				var temp = daysOfWeeks[i];
				daysOfWeeks[i] = daysOfWeeks[j];
				daysOfWeeks[j] = temp;
			}
			// Take the first 'range' elements
			var result = new DayOfWeek[range];
			Array.Copy(daysOfWeeks, result, range);
			return result;
		}

		public static int[] GetRandomHours(int pCount)
		{
			int[] allHours = new int[24];
			for (int i = 0; i < 24; i++)
				allHours[i] = i;

			// Shuffle the array of hours using Fisher-Yates shuffle algorithm
			for (int i = 23; i > 0; i--)
			{
				int j = Random.Range(0, i + 1); // Get a random index
				// Swap elements at indices i and j
				int temp = allHours[i];
				allHours[i] = allHours[j];
				allHours[j] = temp;
			}

			// Create a list to store the selected random hours
			var hours = new int[pCount];
			for (int i = 0; i < pCount; i++)
				hours[i] = allHours[i];
			return hours;
		}
	}

	public static class TimeExtension
	{
		public static long ToUnixTimestamp(this DateTime value)
		{
			var elapsedTime = value - TimeHelper.Epoch;
			return (long)elapsedTime.TotalSeconds;
		}
		public static int ToUnixTimestampInt(this DateTime value)
		{
			var elapsedTime = value - TimeHelper.Epoch;
			return (int)elapsedTime.TotalSeconds;
		}
	}
}