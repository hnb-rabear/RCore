using System;
using UnityEngine;

namespace RCore
{
    /// <summary>
    /// A simple, lightweight, and reusable countdown timer class.
    /// This is a non-MonoBehaviour class that must be manually updated from an external source
    /// (like a MonoBehaviour's Update loop). It's useful for managing timed actions, cooldowns,
    /// or delays without the overhead of coroutines or a more complex timer system.
    /// </summary>
    public class TimedAction
    {
        /// <summary>
        /// The event that is invoked when the timer completes its countdown.
        /// </summary>
        public Action onFinished;
        
        /// <summary>
        /// The total duration in seconds that the timer will run for.
        /// </summary>
        public float timeTarget;

        private bool m_active;
        private bool m_finished = true;
        private float m_elapsedTime;

        /// <summary>
        /// Gets a value indicating whether the timer is currently active and running.
        /// </summary>
        public bool IsRunning => m_active && !m_finished;
        
        /// <summary>
        /// Gets the remaining time in seconds before the timer finishes.
        /// </summary>
        public float RemainTime => timeTarget - m_elapsedTime;

        /// <summary>
        /// Advances the timer's elapsed time, scaled by `Time.timeScale`.
        /// This method must be called manually from an external update loop (e.g., MonoBehaviour.Update).
        /// </summary>
        /// <param name="pElapsedTime">The time delta, typically `Time.deltaTime`.</param>
        public void UpdateWithTimeScale(float pElapsedTime)
        {
            if (m_active)
            {
                m_elapsedTime += pElapsedTime * Time.timeScale;
                if (m_elapsedTime >= timeTarget)
                    Finish();
            }
        }

        /// <summary>
        /// Advances the timer's elapsed time without scaling.
        /// This method must be called manually from an external update loop (e.g., MonoBehaviour.Update).
        /// </summary>
        /// <param name="pElapsedTime">The unscaled time delta, typically `Time.unscaledDeltaTime` or a custom value.</param>
        public void Update(float pElapsedTime)
        {
            if (m_active)
            {
                m_elapsedTime += pElapsedTime;
                if (m_elapsedTime >= timeTarget)
                    Finish();
            }
        }

        /// <summary>
        /// Starts or restarts the timer with a new target duration.
        /// </summary>
        /// <param name="pTargetTime">The total time in seconds for the countdown. If less than or equal to zero, the timer will not start.</param>
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

        /// <summary>
        /// Immediately stops the timer, marks it as finished, and invokes the `onFinished` callback.
        /// </summary>
        public void Finish()
        {
            m_elapsedTime = timeTarget;
            m_active = false;
            m_finished = true;

            onFinished?.Invoke();
        }

        /// <summary>
        /// Manually sets the current elapsed time of the timer.
        /// This can be used to restore a timer's state or to fast-forward/rewind it.
        /// </summary>
        /// <param name="pValue">The new elapsed time in seconds.</param>
        public void SetElapsedTime(float pValue)
        {
            m_elapsedTime = pValue;
        }
        
        /// <summary>
        /// Gets the current elapsed time in seconds.
        /// </summary>
        /// <returns>The amount of time that has passed since the timer started.</returns>
        public float GetElapsedTime() => m_elapsedTime;

        /// <summary>
        /// Stops the timer and resets its state. Unlike `Finish`, this does NOT invoke the `onFinished` callback.
        /// </summary>
        public void Stop()
        {
            m_elapsedTime = 0;
            m_finished = true; // Mark as finished to prevent running, but without calling the event.
            m_active = false;
        }
    }
}