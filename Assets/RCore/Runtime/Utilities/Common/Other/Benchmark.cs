/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using System;
using UnityEngine;

namespace RCore.Common
{
    public class Benchmark : MonoBehaviour
    {
        private static Benchmark mInstance;
        public static Benchmark Instance
        {
            get
            {
                if (mInstance == null)
                {
                    mInstance = FindObjectOfType<Benchmark>();
                    if (mInstance == null)
                    {
                        var obj = new GameObject("Benchmark");
                        mInstance = obj.AddComponent<Benchmark>();
                    }
                }
                return mInstance;
            }
        }

        public bool autoDeactive = true;
        public int fps;
        public int minFps;
        public int maxFps;

        private Action<int, int, int> m_OnFinishedBenchmark;
        private float m_ElapsedTime;
        private int m_CountFrame;
        private float m_DelayStart;
        private float m_BenchmarkDuration;
        private float m_DelayWait;

        private void Start()
        {
            if (mInstance == null)
                mInstance = this;
            else if (mInstance != this)
                Destroy(gameObject);
        }

        private void Update()
        {
            if (m_BenchmarkDuration > 0)
            {
                if (m_DelayStart > 0)
                {
                    m_DelayWait += Time.deltaTime;
                    if (m_DelayWait < m_DelayStart)
                        return;
                }

                m_BenchmarkDuration -= Time.deltaTime;
                if (m_BenchmarkDuration <= 0)
                    m_OnFinishedBenchmark(fps, minFps, maxFps);
            }
            else if (autoDeactive)
                enabled = false;

            m_ElapsedTime += Time.deltaTime;
            m_CountFrame++;
            if (m_ElapsedTime >= 1)
            {
                fps = Mathf.RoundToInt(m_CountFrame * 1f / m_ElapsedTime);
                if (fps > maxFps) maxFps = Mathf.Min(fps, Application.targetFrameRate);
                if (fps < minFps || minFps == 0) minFps = Mathf.Min(fps, Application.targetFrameRate);

                m_ElapsedTime = 0;
                m_CountFrame = 0;
            }
        }

        public void StartBenchmark(float pDuration, Action<int, int, int> pOnFinishedBenchmark, float pDelayStart = 0)
        {
            minFps = 0;
            maxFps = 0;
            m_DelayStart = pDelayStart;
            m_BenchmarkDuration = pDuration;
            m_OnFinishedBenchmark = pOnFinishedBenchmark;
            enabled = true;
        }
    }
}