using System;
using UnityEngine;

namespace RCore
{
    /// <summary>
    /// A simple timer class that executes an action after a specified duration.
    /// It can be updated manually and supports starting, stopping, and finishing prematurely.
    /// </summary>
    public class TimedAction
    {
        /// <summary>
        /// The action to be invoked when the timer finishes.
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
        /// Updates the timer's elapsed time, scaled by Time.timeScale.
        /// Call this from an Update loop to advance the timer.
        /// </summary>
        /// <param name="pElapsedTime">The time elapsed since the last update (e.g., Time.deltaTime).</param>
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
        /// Updates the timer's elapsed time, ignoring Time.timeScale.
        /// Call this from an Update loop to advance the timer independently of game speed.
        /// </summary>
        /// <param name="pElapsedTime">The time elapsed since the last update (e.g., Time.unscaledDeltaTime).</param>
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
        /// Starts or restarts the timer.
        /// </summary>
        /// <param name="pTargetTime">The duration in seconds for this timer run.</param>
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
        /// Immediately finishes the timer, sets its state to finished, and invokes the onFinished action.
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
        /// </summary>
        /// <param name="pValue">The new elapsed time value.</param>
        public void SetElapsedTime(float pValue)
        {
            m_elapsedTime = pValue;
        }

        /// <summary>
        /// Gets the current elapsed time of the timer.
        /// </summary>
        /// <returns>The elapsed time in seconds.</returns>
        public float GetElapsedTime() => m_elapsedTime;

        /// <summary>
        /// Stops the timer and resets its elapsed time without invoking the onFinished action.
        /// </summary>
        public void Stop()
        {
            m_elapsedTime = 0;
            m_finished = false;
            m_active = false;
        }
    }
}