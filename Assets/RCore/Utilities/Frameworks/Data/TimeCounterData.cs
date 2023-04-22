using System;
using RCore.Common;

namespace RCore.Framework.Data
{
    /// <summary>
    /// Note: this can not not prevent time cheating
    /// </summary>
    public class TimeCounterData : DataGroup
    {
        private Action<double> m_OnFinished;
        private DateTimeData m_LocalStart;
        private DateTimeData m_ServerStart;
        private LongData m_Duration;
        private LongData m_SecondsSinceBoot;
        private WaitUtil.CountdownEvent m_Counter;

        public TimeCounterData(int pId) : base(pId)
        {
            m_LocalStart = AddData(new DateTimeData(0, null));
            m_ServerStart = AddData(new DateTimeData(1, null));
            m_Duration = AddData(new LongData(2));

            TimeHelper.CheckServerTime(null);
        }

        public override void PostLoad()
        {
            base.PostLoad();

            if (IsRunning())
                Register();
        }

        public void SetListener(Action<double> pOnFinished)
        {
            m_OnFinished = pOnFinished;
        }

        public double GetRemainSeconds()
        {
            var now = TimeHelper.GetServerTime();
            if (now != null && m_ServerStart.Value == null)
            {
                double offset = 0;
                if (m_LocalStart.Value != null)
                    offset = (DateTime.UtcNow - m_LocalStart.Value.Value).TotalSeconds;
                m_ServerStart.Value = now.Value.AddSeconds(-offset);
                var endTime = m_ServerStart.Value.Value.AddSeconds(m_Duration.Value);

                return (endTime - now.Value).TotalSeconds;
            }
            else if (now != null && m_ServerStart.Value != null)
            {
                var startTime = m_ServerStart.Value;
                var endTime = startTime.Value.AddSeconds(m_Duration.Value);
                return (endTime - now.Value).TotalSeconds;
            }
            else
            {
                var startTime = m_LocalStart.Value;
                var endTime = startTime.Value.AddSeconds(m_Duration.Value);
                return (endTime - DateTime.UtcNow).TotalSeconds;
            }
        }

        public void Start(int pSeconds)
        {
            m_Duration.Value = pSeconds;
            m_LocalStart.Value = DateTime.UtcNow;

            var now = TimeHelper.GetServerTime();
            if (now != null)
            {
                m_ServerStart.Value = now;
                m_LocalStart.Value = now;
            }

            Register();
        }

        public void Stop()
        {
            if (m_Counter != null)
                WaitUtil.RemoveCountdownEvent(m_Counter);
        }

        public void Finish()
        {
            if (m_Counter != null)
                WaitUtil.RemoveCountdownEvent(m_Counter);

            m_OnFinished?.Invoke(GetRemainSeconds());
        }

        private void Register()
        {
            if (m_Counter != null)
                WaitUtil.RemoveCountdownEvent(m_Counter);

            m_Counter = WaitUtil.Start(new WaitUtil.CountdownEvent()
            {
                waitTime = (float)GetRemainSeconds(),
                unscaledTime = true,
                doSomething = (pass) =>
                {
                    m_OnFinished?.Invoke(GetRemainSeconds());
                }
            });
        }

        public bool IsRunning()
        {
            if (m_LocalStart.Value == null)
                return false;
            else
                return GetRemainSeconds() > 0;
        }

        public override void OnApplicationPaused(bool pPaused)
        {
            base.OnApplicationPaused(pPaused);

            TimeHelper.CheckServerTime(null);
        }
    }
}