using System;

namespace RCore.Data.KeyValue
{
    public class TimedTaskData : DataGroup
    {
        private LongData m_secondSinceBoot;
        private DateTimeData m_dateTime;
        private FloatData m_duration;
        private CountdownEvent m_countdownEvent;
        private Action<float> m_onFinished;

        public bool IsRunning => m_duration.Value > 0;
        public float RemainSeconds => m_countdownEvent?.RemainSeconds() ?? 0;
        
        public TimedTaskData(int pId) : base(pId)
        {
            m_secondSinceBoot = AddData(new LongData(0));
            m_dateTime = AddData(new DateTimeData(1));
            m_duration = AddData(new FloatData(2));
        }

        public override bool Stage()
        {
            if (m_countdownEvent != null)
            {
                m_secondSinceBoot.Value = RNative.getSecondsSinceBoot();
                m_dateTime.Set(TimeHelper.GetServerTimeUtc() ?? DateTime.UtcNow);
                m_duration.Value = m_countdownEvent.RemainSeconds();
            }
            
            return base.Stage();
        }

        public override void PostLoad()
        {
            base.PostLoad();
            
            if (m_duration.Value > 0)
            {
                long elapsedSeconds = 0;
                var svNowUtc = TimeHelper.GetServerTimeUtc();
                if (svNowUtc != null) // If player is online, Use the server time
                    elapsedSeconds = (long)(svNowUtc.Value - m_dateTime.Get()).TotalSeconds;
                else
                {
                    // When the player is offline, use the time elapsed since the device was booted
                    long elapsedSecondsSinceBoot = RNative.getSecondsSinceBoot() - m_secondSinceBoot.Value;
                    if (elapsedSecondsSinceBoot > 0)
                        elapsedSeconds = elapsedSecondsSinceBoot;
                    else
                        elapsedSeconds = (long)(DateTime.UtcNow - m_dateTime.Get()).TotalSeconds;
                }

                var duration = m_duration.Value - elapsedSeconds;
                if (duration > 0)
                {
                    Start(duration);
                }
                else
                {
                    Finish(duration);
                }
            }
        }

        private void Finish(float passSeconds)
        {
            m_onFinished?.Invoke(passSeconds);
            Stop();
        }

        public void AddSeconds(float pValue)
        {
            if (m_duration.Value <= 0)
                return;
            
            m_countdownEvent.waitTime += pValue;
            m_duration.Value = m_countdownEvent.RemainSeconds();
        }

        public void Stop()
        {
            m_duration.Value = 0;
            m_secondSinceBoot.Value = 0;
            m_dateTime.Set(0);
            m_countdownEvent?.Stop();
        }

        public void Start(float duration)
        {
            if (duration <= 0)
                return;
            m_duration.Value = duration;
            m_secondSinceBoot.Value = RNative.getSecondsSinceBoot();
            m_dateTime.Set(TimeHelper.GetServerTimeUtc() ?? DateTime.UtcNow);
            m_countdownEvent = TimerEventsGlobal.Instance.WaitForSeconds(new CountdownEvent
            {
                onTimeOut = Finish,
                unscaledTime = true,
                waitTime = m_duration.Value,
            });
        }
        
        public void SetOnFinished(Action<float> pAction)
        {
            m_onFinished = pAction;
        }
    }
}