/***
 * Author RaBear - HNB - 2019
 **/

using System;
using UnityEngine;

namespace RCore
{
    public class Benchmark : IUpdate
    {
        public int fps;
        public int minFps;
        public int maxFps;
        private Action<int, int, int> m_onFinishedBenchmark;
        private float m_elapsedTime;
        private int m_countFrame;
        private float m_benchmarkDuration;
        private bool m_skipFirstFrame;
        public Benchmark(float duration, Action<int, int, int> onFinishedBenchmark)
        {
            stop = false;
            minFps = 0;
            maxFps = 0;
            m_benchmarkDuration = duration;
            m_onFinishedBenchmark = onFinishedBenchmark;
            m_skipFirstFrame = true;
        }
        public bool stop { get; set; }
        public void Update(float pDeltaTime)
        {
            if (m_skipFirstFrame)
                m_skipFirstFrame = false;

            if (m_benchmarkDuration > 0)
            {
                m_benchmarkDuration -= Time.deltaTime;
                if (m_benchmarkDuration <= 0)
                {
                    m_onFinishedBenchmark(fps, minFps, maxFps);
                    stop = true;
                }
            }

            m_elapsedTime += Time.deltaTime;
            m_countFrame++;
            if (m_elapsedTime >= 1)
            {
                fps = Mathf.RoundToInt(m_countFrame * 1f / m_elapsedTime);
                if (fps > maxFps) maxFps = Mathf.Min(fps, Application.targetFrameRate);
                if (fps < minFps || minFps == 0) minFps = Mathf.Min(fps, Application.targetFrameRate);

                m_elapsedTime = 0;
                m_countFrame = 0;
            }
        }
    }
}