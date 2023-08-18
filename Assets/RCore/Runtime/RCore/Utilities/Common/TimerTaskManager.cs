using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using RCore.Common;
using Cysharp.Threading.Tasks;

namespace RCore.Framework.Data
{
    public class TimerTaskManager : IUpdate
    {
        #region Constants

        public const int MONTHS_PER_YEAR = 12;
        public const int DAYS_PER_WEEK = 7;
        public const int DAYS_PER_MONTH = 30;
        public const int HOURS_PER_DAY = 24;
        public const int MINUTES_PER_HOUR = 60;

        public const int MILLISECONDS_PER_SECOND = 1000;
        public const int MICROSECONDS_PER_SECOND = 1000 * 1000;
        public const long NANOSECONDS_PER_SECOND = 1000 * 1000 * 1000;

        public const long MICROSECONDS_PER_MILLISECOND = MICROSECONDS_PER_SECOND / MILLISECONDS_PER_SECOND;

        public const long NANOSECONDS_PER_MICROSECOND = NANOSECONDS_PER_SECOND / MICROSECONDS_PER_SECOND;
        public const long NANOSECONDS_PER_MILLISECOND = NANOSECONDS_PER_SECOND / MILLISECONDS_PER_SECOND;

        public const float SECONDS_PER_NANOSECOND = 1f / NANOSECONDS_PER_SECOND;
        public const float MICROSECONDS_PER_NANOSECOND = 1f / NANOSECONDS_PER_MICROSECOND;
        public const float MILLISECONDS_PER_NANOSECOND = 1f / NANOSECONDS_PER_MILLISECOND;

        public const float SECONDS_PER_MICROSECOND = 1f / MICROSECONDS_PER_SECOND;
        public const float MILLISECONDS_PER_MICROSECOND = 1f / MICROSECONDS_PER_MILLISECOND;

        public const float SECONDS_PER_MILLISECOND = 1f / MILLISECONDS_PER_SECOND;

        public const int SECONDS_PER_MINUTE = 60;
        public const int SECONDS_PER_HOUR = SECONDS_PER_MINUTE * MINUTES_PER_HOUR;
        public const int SECONDS_PER_DAY = SECONDS_PER_HOUR * HOURS_PER_DAY;
        public const int SECONDS_PER_WEEK = SECONDS_PER_DAY * DAYS_PER_WEEK;
        public const int SECONDS_PER_MONTH = SECONDS_PER_DAY * DAYS_PER_MONTH;
        public const int SECONDS_PER_YEAR = SECONDS_PER_MONTH * MONTHS_PER_YEAR;

        #endregion

        //=============================================================

        #region Members

        private static TimerTaskManager m_Instance;
        public static TimerTaskManager Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new TimerTaskManager();
                return m_Instance;
            }
        }

        public Action<bool> OnFetched;

        private DateTime m_DayZero;
        private bool m_LocalTimeSynced;
        private long m_LocalMilisSecondsSinceBoot;

        private bool m_TimeServerFetched;
        private bool m_FetchingTimeServer;
        private float m_SecondsElapsed;

        private static DateTime? m_StartServerTime;
        private float m_AppTimeWhenGetServerTime;

        private List<TimerTask> m_TimerTasks;

        public bool FetchingTimeServer => m_FetchingTimeServer;
        public bool TimeServerFetched => m_TimeServerFetched;

        #endregion

        //=============================================================

        #region Public

        public TimerTaskManager()
        {
            m_TimerTasks = new List<TimerTask>();
            m_LocalTimeSynced = false;
            m_TimeServerFetched = false;
            m_DayZero = new DateTime(2017, 1, 1);

            SyncTimers();

            WaitUtil.AddUpdate(this);
        }

        public long GetSecondsSinceBoot()
        {
            SyncTimeLocal();

            var millisSinceBoot = (long)GetLocalMillisSeconds() + m_LocalMilisSecondsSinceBoot;
            return millisSinceBoot / MILLISECONDS_PER_SECOND;
        }

        public long GetCurrentServerSeconds()
        {
            SyncTimeServer();

            var now = GetNowInServer();
            if (now != null)
                return (long)(now.Value - m_DayZero).TotalSeconds;

            return 0;
        }

        public void OnApplicationFocus(bool pFocus)
        {
            if (pFocus)
            {
                SyncTimers();
            }
            else
            {
                m_LocalTimeSynced = false;
                m_TimeServerFetched = false;
            }
        }

        public DateTime GetNow()
        {
            var now = GetNowInServer();
            if (now == null)
                return DateTime.UtcNow;
            return now.Value;
        }

        public double GetSecondsTillMidNight()
        {
            var now = GetNow();
            var secondsTillMidNight = TimeHelper.GetSecondsTillMidNight(now);
            return secondsTillMidNight;
        }

        public DateTime? GetNowInServer()
        {
            if (m_StartServerTime != null && m_AppTimeWhenGetServerTime != 0)
                return m_StartServerTime.Value.AddSeconds(Time.unscaledTime - m_AppTimeWhenGetServerTime);
            return null;
        }

        public void SyncTimers()
        {
            SyncTimeLocal();
            SyncTimeServer();
        }

        public void AddTimerTask(TimerTask pTimer)
        {
            if (!m_TimerTasks.Contains(pTimer))
                m_TimerTasks.Add(pTimer);
        }

        public void Update(float pUnscaledDetalTime)
        {
            int count = m_TimerTasks.Count;
            if (count > 0)
            {
                m_SecondsElapsed += pUnscaledDetalTime;
                if (m_SecondsElapsed >= 1.0f)
                {
                    m_SecondsElapsed -= 1.0f;
                    long currentServerSeconds = GetCurrentServerSeconds();
                    long currentLocalSeconds = GetSecondsSinceBoot();
                    for (int i = count - 1; i >= 0; i--)
                    {
                        var task = m_TimerTasks[i];
                        if (task != null)
                        {
                            if (task.IsRunning)
                                task.Update(currentServerSeconds, currentLocalSeconds, 1);
                            else
                                m_TimerTasks.RemoveAt(i);
                        }
                    }
                }
            }
        }

        #endregion

        //================================================================

        #region Private

        private double GetLocalMillisSeconds()
        {
            return (DateTime.UtcNow - m_DayZero).TotalMilliseconds;
        }

        private void SyncTimeLocal()
        {
            if (m_LocalTimeSynced == false)
            {
                m_LocalTimeSynced = true;
                m_LocalMilisSecondsSinceBoot = RNative.getMillisSinceBoot() - (long)GetLocalMillisSeconds();
            }
        }

        private async void SyncTimeServer()
        {
            if (!m_TimeServerFetched && !m_FetchingTimeServer)
            {
                string url = "http://divmob.com/api/zombieage/time.php";

                var form = new WWWForm();
                var request = UnityWebRequest.Post(url, form);

                m_FetchingTimeServer = true;

                await request.SendWebRequest();
                
                m_FetchingTimeServer = false;
                m_TimeServerFetched = false;
                
                if (!request.isNetworkError && !request.isHttpError)
                {
                    if (request.responseCode == 200)
                    {
                        var text = request.downloadHandler.text;
                        if (TimeHelper.TryParse(text, out var utcTime))
                        {
                            m_TimeServerFetched = true;
                            m_StartServerTime = utcTime;
                            m_AppTimeWhenGetServerTime = Time.unscaledTime;
                        }
                    }
                }
                OnFetched?.Invoke(m_TimeServerFetched);
            }
        }

        #endregion
    }
}