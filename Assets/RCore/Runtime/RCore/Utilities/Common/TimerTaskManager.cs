/**
 *  Based on TimeManager of hnim.
 *  Copyright (c) 2017 RedAntz. All rights reserved.
 */

using System;
using System.Collections.Generic;
using RCore.Common;

namespace RCore.Framework.Data
{
    public class TimerTaskManager : IUpdate
    {
        private const int MILLISECONDS_PER_SECOND = 1000;

        private static TimerTaskManager m_Instance;
        public static TimerTaskManager Instance => m_Instance ??= new TimerTaskManager();

        private DateTime m_DayZero = new DateTime(2017, 1, 1);
        private long m_LocalMilliSecondsSinceBoot;

        private float m_SecondsElapsed;

        private List<TimerTask> m_TimerTasks = new List<TimerTask>();

        public TimerTaskManager()
        {
            WaitUtil.AddUpdate(this);
        }

        public long GetSecondsSinceBoot()
        {
            var localMillisSeconds = (long)(DateTime.UtcNow - m_DayZero).TotalMilliseconds;
            if (m_LocalMilliSecondsSinceBoot == 0)
                m_LocalMilliSecondsSinceBoot = RNative.getMillisSinceBoot() - localMillisSeconds;

            var millisSinceBoot = localMillisSeconds + m_LocalMilliSecondsSinceBoot;
            return millisSinceBoot / MILLISECONDS_PER_SECOND;
        }

        public long GetCurrentServerSeconds()
        {
            var now = TimeHelper.GetServerTimeUtc();
            if (now != null)
                return (long)(now.Value - m_DayZero).TotalSeconds;
            return 0;
        }

        public void AddTimerTask(TimerTask pTimer)
        {
            if (!m_TimerTasks.Contains(pTimer))
                m_TimerTasks.Add(pTimer);
        }

        public void Update(float pUnscaledDeltaTime)
        {
            int count = m_TimerTasks.Count;
            if (count > 0)
            {
                m_SecondsElapsed += pUnscaledDeltaTime;
                if (m_SecondsElapsed >= 1.0f)
                {
                    m_SecondsElapsed -= 1.0f;
                    var currentServerSeconds = GetCurrentServerSeconds();
                    var currentLocalSeconds = GetSecondsSinceBoot();
                    for (int i = count - 1; i >= 0; i--)
                    {
                        var task = m_TimerTasks[i];
                        if (task != null)
                        {
                            if (task.IsRunning)
                                task.Update(currentServerSeconds, currentLocalSeconds);
                            else
                                m_TimerTasks.RemoveAt(i);
                        }
                    }
                }
            }
        }
    }
}