using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
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
                mElapsedTime += (pElapsedTime * Time.timeScale);
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

            if (onFinished != null)
                onFinished();
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

    [Obsolete("This is an example of how Job is inappropriate in custom class! Job should use only in MonoBehaviour")]
    public class MiscTimerJob
    {
        public Action onFinished;
        public float timeTarget;

        public float elapsedTime { get; private set; }
        public bool active { get; private set; }
        public bool finished { get; private set; }

        public bool IsRunning => active && !finished;
        public float RemainTime => timeTarget - elapsedTime;

        private NativeArray<float> mResults;
        private CountdownJob mJob;
        private JobHandle mJobHandle;

        public void Init()
        {
            mResults = new NativeArray<float>(2, Allocator.Persistent);
        }

        public void Destroy()
        {
            mResults.Dispose();
        }

        public void Update(float pDeltaTime)
        {
            if (active)
            {
                mResults[0] = elapsedTime;
                mJob = new CountdownJob()
                {
                    deltaTime = Time.deltaTime,
                    results = mResults,
                    timeTarget = timeTarget,
                };

                mJobHandle = mJob.Schedule();
                mJobHandle.Complete();

                elapsedTime = mResults[0];
                var complete = mResults[1] == 1;
                if (complete)
                    Finish();
            }
        }

        public void Start(float pTagetTime)
        {
            if (pTagetTime <= 0)
            {
                finished = true;
                active = false;
            }
            else
            {
                elapsedTime = 0;
                timeTarget = pTagetTime;
                finished = false;
                active = true;
            }
        }

        public void Finish()
        {
            elapsedTime = timeTarget;
            active = false;
            finished = true;

            if (onFinished != null)
                onFinished();
        }

        public void Stop()
        {
            elapsedTime = 0;
            finished = false;
            active = false;
        }

        [BurstCompile]
        public struct CountdownJob : IJob
        {
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float timeTarget;
            public NativeArray<float> results;

            public void Execute()
            {
                results[0] += deltaTime;
                if (results[0] > timeTarget)
                {
                    results[0] = timeTarget;
                    results[1] = 1;
                }
                else
                {
                    results[1] = 0;
                }
            }
        }
    }
}