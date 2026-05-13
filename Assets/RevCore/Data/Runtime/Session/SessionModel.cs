using System;
using UnityEngine;

namespace RevCore
{
    public readonly struct NewDayStartedEvent : IEvent { }

    public interface ISessionModel
    {
        int Days { get; }
        float ActiveTime { get; }
        int LastActive { get; }
        int DaysStreak { get; }
        int SessionsTotal { get; }
        int SessionsDaily { get; }
        int SessionsWeekly { get; }
        int SessionsMonthly { get; }
    }

    public class SessionModel : JObjectModel<SessionData>, ISessionModel
    {
        public float secondsTillNextDay;
        public float secondsTillNextWeek;

        int ISessionModel.Days => data.days;
        float ISessionModel.ActiveTime => data.activeTime;
        int ISessionModel.LastActive => data.lastActive;
        int ISessionModel.DaysStreak => data.daysStreak;
        int ISessionModel.SessionsTotal => data.SessionsTotal;
        int ISessionModel.SessionsDaily => data.SessionsDaily;
        int ISessionModel.SessionsWeekly => data.SessionsWeekly;
        int ISessionModel.SessionsMonthly => data.SessionsMonthly;

        public override void Init() { }

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

        public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
        {
            if (!pause)
                CheckNewDay();
            else
                data.lastActive = utcNowTimestamp;
        }

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

        public override void OnPreSave(int utcNowTimestamp)
        {
            data.lastActive = utcNowTimestamp;
            string ver = Application.version;
            if (string.IsNullOrEmpty(data.installVersion))
                data.installVersion = ver;
            data.updateVersion = ver;
        }

        public override void OnRemoteConfigFetched() { }

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

        public void AddOneDay()
        {
            data.days++;
            data.daysStreak++;
            data.SessionsDaily = 1;
            DispatchEvent(new NewDayStartedEvent());
        }
    }
}
