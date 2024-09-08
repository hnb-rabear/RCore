using System;
using UnityEngine;

namespace RCore.Common
{
    public class MiscTimer
    {
        public Action onFinished;
        public float timeTarget;
        private bool mActive;
        private bool mFinished;
        private float mElapsedTime;

        public bool IsRunning => mActive && !mFinished;
        public float RemainTime => timeTarget - mElapsedTime;

        public MiscTimer()
        {
            mFinished = true;
        }

        public void UpdateWithTimeScale(float pElapsedTime)
        {
            if (mActive)
            {
                mElapsedTime += pElapsedTime * Time.timeScale;
                if (mElapsedTime > timeTarget)
                    Finish();
            }
        }

        public void Update(float pElapsedTime)
        {
            if (mActive)
            {
                mElapsedTime += pElapsedTime;
                if (mElapsedTime > timeTarget)
                    Finish();
            }
        }

        public void Start(float pTagetTime)
        {
            if (pTagetTime <= 0)
            {
                mFinished = true;
                mActive = false;
            }
            else
            {
                mElapsedTime = 0;
                timeTarget = pTagetTime;
                mFinished = false;
                mActive = true;
            }
        }

        public void Finish()
        {
            mElapsedTime = timeTarget;
            mActive = false;
            mFinished = true;

            onFinished?.Invoke();
        }

        internal void SetElapsedTime(float pValue)
        {
            mElapsedTime = pValue;
        }

        public float GetElapsedTime() => mElapsedTime;

        public void Stop()
        {
            mElapsedTime = 0;
            mFinished = false;
            mActive = false;
        }
    }
}