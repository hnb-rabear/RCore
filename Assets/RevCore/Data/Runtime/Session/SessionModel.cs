using System;
using UnityEngine;

namespace RevCore
{
    /// <summary>Fired the first time <see cref="SessionModel"/> detects a new local-time day relative to the last active timestamp.</summary>
    public readonly struct NewDayStartedEvent : IEvent { }

    /// <summary>
    /// Read-only view of session counters maintained by <see cref="SessionModel"/>. Useful when a
    /// consumer cares about session telemetry but should not write to the underlying data.
    /// </summary>
    public interface ISessionModel
    {
        /// <summary>Number of distinct calendar days the player has been active.</summary>
        int Days { get; }
        /// <summary>Cumulative seconds the application has been in the foreground across all sessions.</summary>
        float ActiveTime { get; }
        /// <summary>Unix timestamp (UTC seconds) of the last save.</summary>
        int LastActive { get; }
        /// <summary>Consecutive-day streak. Reset to zero when a day is skipped.</summary>
        int DaysStreak { get; }
        /// <summary>Lifetime session count.</summary>
        int SessionsTotal { get; }
        /// <summary>Sessions started in the current calendar day.</summary>
        int SessionsDaily { get; }
        /// <summary>Sessions started in the current ISO week.</summary>
        int SessionsWeekly { get; }
        /// <summary>Sessions started in the current calendar month.</summary>
        int SessionsMonthly { get; }
    }

    /// <summary>
    /// Session and engagement tracking model: days played, active time, streaks, sessions per
    /// period. Fires <see cref="NewDayStartedEvent"/> when a day boundary is crossed.
    /// </summary>
    public class SessionModel : JObjectModel<SessionData>, ISessionModel
    {
        /// <summary>Seconds until midnight in local time. Counts down each <see cref="OnUpdate"/>.</summary>
        public float secondsTillNextDay;
        /// <summary>Seconds until the next ISO-week boundary (Monday midnight). Counts down each <see cref="OnUpdate"/>.</summary>
        public float secondsTillNextWeek;

        int ISessionModel.Days => data.days;
        float ISessionModel.ActiveTime => data.activeTime;
        int ISessionModel.LastActive => data.lastActive;
        int ISessionModel.DaysStreak => data.daysStreak;
        int ISessionModel.SessionsTotal => data.SessionsTotal;
        int ISessionModel.SessionsDaily => data.SessionsDaily;
        int ISessionModel.SessionsWeekly => data.SessionsWeekly;
        int ISessionModel.SessionsMonthly => data.SessionsMonthly;

        /// <inheritdoc />
        public override void Init() { }

        /// <inheritdoc />
        public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
        {
            if (data.firstActive == 0)
                data.firstActive = utcNowTimestamp;

            var lastActive = TimeHelper.UnixTimestampToDateTime(data.lastActive).ToLocalTime();
            var now = TimeHelper.UnixTimestampToDateTime(utcNowTimestamp).ToLocalTime();

            if ((now.Date - lastActive.Date).TotalDays > 1)
                data.daysStreak = 0;

            if (lastActive.Date != now.Date)
            {
                data.days++;
                data.daysStreak++;
                data.SessionsDaily = 0;
            }
            if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
                data.SessionsWeekly = 0;
            if (lastActive.Year != now.Year || lastActive.Month != now.Month)
                data.SessionsMonthly = 0;

            data.SessionsTotal++;
            data.SessionsDaily++;
            data.SessionsWeekly++;
            data.SessionsMonthly++;

            secondsTillNextDay = (float)(now.Date.AddDays(1) - now).TotalSeconds;
        }

        /// <inheritdoc />
        public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
        {
            if (!pause)
                CheckNewDay();
            else
                data.lastActive = utcNowTimestamp;
        }

        /// <inheritdoc />
        public override void OnUpdate(float deltaTime)
        {
            data.activeTime += deltaTime;
            if (secondsTillNextDay > 0)
            {
                secondsTillNextDay -= deltaTime;
                if (secondsTillNextDay <= 0)
                    CheckNewDay();
            }
            if (secondsTillNextWeek > 0)
                secondsTillNextWeek -= deltaTime;
        }

        /// <inheritdoc />
        public override void OnPreSave(int utcNowTimestamp)
        {
            data.lastActive = utcNowTimestamp;
            string ver = Application.version;
            if (string.IsNullOrEmpty(data.installVersion))
                data.installVersion = ver;
            data.updateVersion = ver;
        }

        /// <inheritdoc />
        public override void OnRemoteConfigFetched() { }

        /// <summary>
        /// Returns the seconds elapsed between the last save and now (UTC). Clamped to 0. Returns 0
        /// when <see cref="SessionData.lastActive"/> has never been set (first launch).
        /// </summary>
        public virtual int GetOfflineSeconds()
        {
            if (data.lastActive <= 0) return 0;
            int offline = TimeHelper.GetNowTimestamp(true) - data.lastActive;
            return Mathf.Max(0, offline);
        }

        private void CheckNewDay()
        {
            var lastActive = TimeHelper.UnixTimestampToDateTime(data.lastActive).ToLocalTime();
            var now = TimeHelper.GetNow(false);

            if ((now.Date - lastActive.Date).TotalDays > 1)
                data.daysStreak = 0;

            if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
                data.SessionsWeekly = 1;
            if (lastActive.Year != now.Year || lastActive.Month != now.Month)
                data.SessionsMonthly = 1;
            if (lastActive.Date != now.Date)
                AddOneDay();

            secondsTillNextDay = (float)(now.Date.AddDays(1) - now).TotalSeconds;
            secondsTillNextWeek = (float)TimeHelper.GetSecondsTillDayOfWeek(DayOfWeek.Monday, now);
        }

        /// <summary>Manually advance the day counter and reset the daily session counter. Publishes <see cref="NewDayStartedEvent"/>.</summary>
        public void AddOneDay()
        {
            data.days++;
            data.daysStreak++;
            data.SessionsDaily = 1;
            DispatchEvent(new NewDayStartedEvent());
        }
    }
}
