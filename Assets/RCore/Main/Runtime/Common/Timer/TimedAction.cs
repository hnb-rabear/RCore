using System;
using UnityEngine;

namespace RCore
{
    public class TimedAction
    {
        public Action onFinished;
        public float timeTarget;
        private bool m_active;
        private bool m_finished = true;
        private float m_elapsedTime;

        public bool IsRunning => m_active && !m_finished;
        public float RemainTime => timeTarget - m_elapsedTime;

        public void UpdateWithTimeScale(float pElapsedTime)
        {
            if (m_active)
            {
                m_elapsedTime += pElapsedTime * Time.timeScale;
                if (m_elapsedTime > timeTarget)
                    Finish();
            }
        }

        public void Update(float pElapsedTime)
        {
            if (m_active)
            {
                m_elapsedTime += pElapsedTime;
                if (m_elapsedTime > timeTarget)
                    Finish();
            }
        }

        public void Start(float pTargetTime)
        {
            if (pTargetTime <= 0)
            {
                m_finished = true;
                m_active = false;
            }
            else
            {
                m_elapsedTime = 0;
                timeTarget = pTargetTime;
                m_finished = false;
                m_active = true;
            }
        }

        public void Finish()
        {
            m_elapsedTime = timeTarget;
            m_active = false;
            m_finished = true;

            onFinished?.Invoke();
        }

        public void SetElapsedTime(float pValue)
        {
            m_elapsedTime = pValue;
        }

        public float GetElapsedTime() => m_elapsedTime;

        public void Stop()
        {
            m_elapsedTime = 0;
            m_finished = false;
            m_active = false;
        }
    }
}