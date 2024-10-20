﻿/**
 * Author RadBear - nbhung71711 @gmail.com - 2018
 **/

using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RCore.Common
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
        private static bool m_WaitingRequest;
        private static int m_RequestTimeAttempt;
        private static float m_GetServerTimeAt;
        private static DateTime m_ServerTime;
        public static bool FetchedTime => m_GetServerTimeAt > 0;

        /// <summary>
        /// 00:00:00
        /// </summary>
        public static string FormatHHMMss(double seconds, bool showFull)
        {
            if (seconds > 0)
            {
                var t = TimeSpan.FromSeconds(seconds);
                var hours = t.Hours + t.Days * 24;
                if (showFull || hours > 0)
                {
                    //00:00:00
                    return m_TimeBuilder.Clear()
                        .Append(hours.ToString("D2")).Append(":")
                        .Append(t.Minutes.ToString("D2")).Append(":")
                        .Append(t.Seconds.ToString("D2"))
                        .ToString();
                }
                else if (hours == 0)
                {
                    if (t.Minutes > 0)
                    {
                        //00:00
                        return m_TimeBuilder.Clear()
                            .Append(t.Minutes.ToString("D2")).Append(":")
                            .Append(t.Seconds.ToString("D2"))
                            .ToString();
                    }
                    else
                    {
                        //00
                        return m_TimeBuilder.Clear()
                            .Append(t.Seconds.ToString("D2"))
                            .ToString();
                    }
                }
            }
            else if (showFull)
            {
                return "00:00:00";
            }

            return "";
        }

        /// <summary>
        /// 00:00:00
        /// </summary>
        public static string FormatMMss(double seconds, bool showFull)
        {
            if (seconds > 0)
            {
                var t = TimeSpan.FromSeconds(seconds);

                if (showFull || t.Hours > 0)
                {
                    //00:00
                    return m_TimeBuilder.Clear()
                        .Append((t.Hours * 60 + t.Minutes).ToString("D2")).Append(":")
                        .Append(t.Seconds.ToString("D2"))
                        .ToString();
                }
                else if (t.Hours == 0)
                {
                    if (t.Minutes > 0)
                    {
                        //00:00
                        return m_TimeBuilder.Clear()
                            .Append(t.Minutes.ToString("D2")).Append(":")
                            .Append(t.Seconds.ToString("D2"))
                            .ToString();
                    }
                    else
                    {
                        //00
                        return m_TimeBuilder.Clear()
                            .Append(t.Seconds.ToString("D2"))
                            .ToString();
                    }
                }
            }
            else if (showFull)
            {
                return "00:00";
            }

            return "";
        }

        /// <summary>
        /// Format to 00:00:00:000
        /// </summary>
        /// <returns></returns>
        public static string FormatHHMMssMs(double seconds, bool showFull)
        {
            if (seconds > 0)
            {
                var t = TimeSpan.FromSeconds(seconds);

                //I keep below code as a result to provide that StringBuilder is much faster than string.format
                //StringBuilder create gabrage lesser than string.Format about 65%

                //if (showFull || t.Hours > 0)
                //{
                //    return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}",
                //        t.Hours,
                //        t.Minutes,
                //        t.Seconds,
                //        t.Milliseconds);
                //}
                //else if (t.Hours == 0)
                //{
                //    if (t.Minutes > 0)
                //    {
                //        return string.Format("{0:D2}:{1:D2}:{2:D3}",
                //        t.Minutes,
                //        t.Seconds,
                //        t.Milliseconds);
                //    }
                //    else
                //    {
                //        return string.Format("{0:D2}:{1:D3}",
                //            t.Seconds,
                //            t.Milliseconds);
                //    }
                //}

                if (showFull || t.Hours > 0)
                {
                    //00:00:00:000
                    return m_TimeBuilder.Clear()
                        .Append(t.Hours.ToString("D2")).Append(":")
                        .Append(t.Minutes.ToString("D2")).Append(":")
                        .Append(t.Seconds.ToString("D2")).Append(":")
                        .Append(t.Milliseconds.ToString("D3"))
                        .ToString();
                }
                else if (t.Hours == 0)
                {
                    if (t.Minutes > 0)
                    {
                        //00:00:000
                        return m_TimeBuilder.Clear()
                            .Append(t.Minutes.ToString("D2")).Append(":")
                            .Append(t.Seconds.ToString("D2")).Append(":")
                            .Append(t.Milliseconds.ToString("D3"))
                            .ToString();
                    }
                    else
                    {
                        //00:000
                        return m_TimeBuilder.Clear()
                            .Append(t.Seconds.ToString("D2")).Append(":")
                            .Append(t.Milliseconds.ToString("D3"))
                            .ToString();
                    }
                }
            }
            else if (showFull)
            {
                return "00:00:00:000";
            }

            return "";
        }

        /// <summary>
        /// d h m s
        /// </summary>
        public static string FormatDayHMs(double seconds, int pMaxSplits = 2)
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
                        m_TimeBuilder.Append(t.Hours.ToString("D2")).Append("h");
                    }

                    if ((t.Minutes > 0 || t.Seconds > 0) && split < pMaxSplits)
                    {
                        if (split > 0)
                            m_TimeBuilder.Append(" ");

                        if (split > 0 || t.Minutes > 0)
                        {
                            split++;
                            m_TimeBuilder.Append(t.Minutes.ToString("D2")).Append("m");
                        }

                        if (t.Seconds > 0 && split < pMaxSplits)
                        {
                            if (split > 0)
                                m_TimeBuilder.Append(" ");

                            m_TimeBuilder.Append(t.Seconds.ToString("D2")).Append("s");
                        }
                    }
                }
                return m_TimeBuilder.ToString();
            }

            return "";
        }

        /// <summary>
        /// d 00:00:00
        /// </summary>
        public static string FormatDHMs(double seconds, int pMaxSplits)
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

                if (split < pMaxSplits)
                {
                    if (split > 0)
                        m_TimeBuilder.Append(" ");

                    split++;
                    m_TimeBuilder.Append(t.Hours.ToString("D2"));

                    if (split < pMaxSplits)
                    {
                        split++;
                        m_TimeBuilder.Append(":").Append(t.Minutes.ToString("D2"));

                        if (split < pMaxSplits)
                            m_TimeBuilder.Append(":").Append(t.Seconds.ToString("D2"));
                    }
                }
                return m_TimeBuilder.ToString();
            }

            return "";
        }

        /// <summary>
        /// day:00:00:00
        /// </summary>
        public static string FormatDayHHMMss(double seconds, bool showFull)
        {
            if (seconds > 0)
            {
                var t = TimeSpan.FromSeconds(seconds);
                if (showFull || t.Days > 0)
                {
                    //00:00:00:000
                    string day = t.Days > 0 ? (t.Days > 1 ? " days" : " day") : "";
                    return m_TimeBuilder.Clear()
                        .Append(t.Days > 0 ? (t.Days + day) : "").Append(t.Days > 0 ? " " : "")
                        .Append(t.Hours.ToString("D2")).Append(":")
                        .Append(t.Minutes.ToString("D2")).Append(":")
                        .Append(t.Seconds.ToString("D2"))
                        .ToString();
                }
                else if (t.Days == 0)
                {
                    if (t.Hours > 0)
                    {
                        //00:00:000
                        return m_TimeBuilder.Clear()
                            .Append(t.Hours.ToString("D2")).Append(":")
                            .Append(t.Minutes.ToString("D2")).Append(":")
                            .Append(t.Seconds.ToString("D2"))
                            .ToString();
                    }
                    else
                    {
                        //00:000
                        return m_TimeBuilder.Clear()
                            .Append(t.Minutes.ToString("D2")).Append(":")
                            .Append(t.Seconds.ToString("D2"))
                            .ToString();
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// 6h 15m 7s
        /// </summary>
        public static string FormatHMs(double seconds, bool showFull)
        {
            if (seconds > 0)
            {
                var t = TimeSpan.FromSeconds(seconds);

                if (showFull || t.Hours > 0)
                {
                    //00h00m00s
                    return m_TimeBuilder.Clear()
                        .Append(t.Hours).Append("h ")
                        .Append(t.Minutes).Append("m ")
                        .Append(t.Seconds).Append("s")
                        .ToString();
                }
                else if (t.Hours == 0)
                {
                    if (t.Minutes > 0)
                    {
                        //00m00s
                        return m_TimeBuilder.Clear()
                            .Append(t.Minutes).Append("m ")
                            .Append(t.Seconds).Append("s")
                            .ToString();
                    }
                    else
                    {
                        //00s
                        return m_TimeBuilder.Clear()
                            .Append(t.Seconds).Append("s")
                            .ToString();
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// 12 hours 1 minute 23 seconds
        /// </summary>
        public static string FormatHMsFull(double seconds, bool showFull)
        {
            if (seconds > 0)
            {
                var t = TimeSpan.FromSeconds(seconds);

                if (showFull || t.Hours > 0)
                {
                    if (t.Seconds > 0)
                    {
                        //Hour Minute Second
                        return m_TimeBuilder.Clear()
                            .Append(t.Hours).Append(t.Hours <= 1 ? " Hour " : " Hours ")
                            .Append(t.Minutes > 0 ? t.Minutes.ToString() : "").Append(t.Minutes > 0 ? (t.Minutes == 1 ? " Minute " : " Minutes ") : "")
                            .Append(t.Seconds > 0 ? t.Seconds.ToString() : "").Append(t.Seconds > 0 ? (t.Seconds == 1 ? " Second" : " Seconds") : "")
                            .ToString();
                    }
                }
                else if (t.Hours == 0)
                {
                    if (t.Minutes > 0)
                    {
                        //Minute Second
                        return m_TimeBuilder.Clear()
                            .Append(t.Minutes > 0 ? t.Minutes.ToString() : "").Append(t.Minutes == 1 ? " Minute " : " Minutes ")
                            .Append(t.Seconds > 0 ? t.Seconds.ToString() : "").Append(t.Seconds > 0 ? (t.Seconds == 1 ? " Second" : " Seconds") : "")
                            .ToString();
                    }
                    else
                    {
                        //Second
                        if (t.Seconds > 0)
                        {
                            return m_TimeBuilder.Clear()
                                .Append(t.Seconds).Append(t.Seconds <= 1 ? " Second" : " Seconds")
                                .ToString();
                        }
                        return "";
                    }
                }
            }

            return "";
        }

        public static double GetSecondsTillMidNight()
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

        /// <summary>
        /// Get start of week from date
        /// </summary>
        public static DateTime GetStartTimeOfWeekDay(DateTime pDate, DayOfWeek pDay)
        {
            int dayOfWeek = (int)pDate.DayOfWeek;
            var startTimeOfMonday = pDate.AddDays(-dayOfWeek + 1).Date;
            if (pDay > DayOfWeek.Sunday)
                return startTimeOfMonday.AddDays((int)pDay - 1);
            else
                return startTimeOfMonday.AddDays(6);
        }

        /// <summary>
        /// Get end of week from date
        /// </summary>
        public static DateTime GetEndTimeOfWeekDay(DateTime pDate, DayOfWeek pDay)
        {
            var startTime = GetStartTimeOfWeekDay(pDate, pDay);
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
            var firstDayOfMonth = new DateTime(pDate.Year, pDate.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var lastTimeOfMonth = lastDayOfMonth.AddDays(1).Date;
            return lastTimeOfMonth;
        }

        public static DateTime? GetServerTimeUtc()
        {
            if (m_GetServerTimeAt > 0)
                return m_ServerTime.AddSeconds(Time.unscaledTime - m_GetServerTimeAt);
            return null;
        }

        public static void RequestServerTime(bool renew = false, Action<bool> pCallback = null)
        {
            if (m_GetServerTimeAt > 0 && !renew)
            {
                pCallback?.Invoke(true);
                return;
            }
            
            if (m_WaitingRequest)
                return;

            string url = "https://farmcityer.com/gettime.php";

            var w = UnityWebRequest.Get(url);
            w.SendWebRequest();

            m_WaitingRequest = true;
            TimerEventsGlobal.Instance.WaitForCondition(() => w.isDone, () =>
            {
                m_WaitingRequest = false;
                bool success = false;
                if (w.result == UnityWebRequest.Result.Success)
                {
                    if (w.responseCode == 200)
                    {
                        var text = w.downloadHandler.text;
                        if (int.TryParse(text, out int timestamp))
                        {
                            m_ServerTime = UnixTimestampToDateTime(timestamp);
                            m_GetServerTimeAt = Time.unscaledTime;
                            success = true;
                        }
                    }
                }
                if (!success && m_RequestTimeAttempt < 5)
                    TimerEventsGlobal.Instance.WaitForSeconds(30, _ => RequestServerTime()); // Retry after 30 seconds
                m_RequestTimeAttempt++;
                pCallback?.Invoke(success);
            });
        }

        /// <summary>
        /// Get mod in seconds from amount of time
        /// </summary>
        /// <param name="pIntervalInSeconds"></param>
        /// <param name="pPassedSeconds"></param>
        /// <returns>Step Count and Remain Seconds</returns>
        public static int CalcTimeStepsPassed(long pPassedSeconds, long pIntervalInSeconds, out long pModSeconds)
        {
            int stepPassed = 0;
            pModSeconds = pIntervalInSeconds;
            if (pPassedSeconds > 0)
            {
                stepPassed += Mathf.FloorToInt(pPassedSeconds * 1f / pIntervalInSeconds);
                pModSeconds = pPassedSeconds % pIntervalInSeconds;
            }
            return stepPassed;
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
        
        public static int GetUtcNowTimestamp()
        {
            var utcNow = GetServerTimeUtc() ?? DateTime.UtcNow;
            int timestamp = DateTimeToUnixTimestampInt(utcNow);
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