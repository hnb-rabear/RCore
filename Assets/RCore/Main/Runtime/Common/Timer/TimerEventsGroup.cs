/**
 * Author HNB-RaBear - 2018
 **/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
    /// <summary>
    /// Represents a timer-based event that executes an action after a specified duration.
    /// It can be configured to use scaled or unscaled time, to auto-restart, and to stop early based on a condition.
    /// </summary>
    public class CountdownEvent
    {
        /// <summary>
        /// A unique identifier for the event. If non-zero, registering a new event with the same ID will replace the existing one.
        /// </summary>
        public int id;
        /// <summary>
        /// The action to execute when the timer reaches zero. The float parameter provides the amount of time elapsed beyond the waitTime.
        /// </summary>
        public Action<float> onTimeOut;
        /// <summary>
        /// A delegate that, if it returns true, will stop the event prematurely without invoking onTimeOut.
        /// </summary>
        public ConditionalDelegate breakCondition;
        /// <summary>
        /// The total duration in seconds that the event should wait before firing.
        /// </summary>
        public float waitTime;
        /// <summary>
        /// If true, the countdown uses Time.unscaledDeltaTime, making it immune to changes in Time.timeScale. Useful for UI animations or timers that must run during pause.
        /// </summary>
        public bool unscaledTime;
        /// <summary>
        /// If true, the event will automatically restart its countdown after firing.
        /// </summary>
        public bool autoRestart;
        
        /// <summary>
        /// The total time that has passed since the event started.
        /// </summary>
        public float Elapsed { get; private set; }
        /// <summary>
        /// An optional offset to apply to the starting elapsed time.
        /// </summary>
        public float ElapsedOffset { get; private set; }
        /// <summary>
        /// Returns true if the elapsed time has met or exceeded the wait time.
        /// </summary>
        public bool IsTimeOut => Elapsed >= waitTime;
        /// <summary>
        /// Gets the remaining time in seconds before the event times out. Returns 0 if already timed out.
        /// </summary>
        public float RemainSeconds() => waitTime - Elapsed > 0 ? waitTime - Elapsed : 0;
        
        /// <summary>
        /// Initializes a new CountdownEvent with an optional ID.
        /// </summary>
        /// <param name="pid">The unique identifier for the event.</param>
        public CountdownEvent(int pid = 0)
        {
            id = pid;
        }

        /// <summary>
        /// Copies the properties of another CountdownEvent to this instance.
        /// </summary>
        public void Set(CountdownEvent other)
        {
            id = other.id;
            onTimeOut = other.onTimeOut;
            breakCondition = other.breakCondition;
            waitTime = other.waitTime;
            unscaledTime = other.unscaledTime;
            Elapsed = other.Elapsed;
            ElapsedOffset = other.ElapsedOffset;
        }

        /// <summary>
        /// Increases the elapsed time by a given amount.
        /// </summary>
        /// <param name="pValue">The amount of time to add.</param>
        public virtual void AddElapsedTime(float pValue)
        {
            Elapsed += pValue;
        }

        /// <summary>
        /// Resets the elapsed time to zero, effectively restarting the countdown.
        /// </summary>
        public void Restart()
        {
            Elapsed = 0;
            ElapsedOffset = 0;
        }

        /// <summary>
        /// Sets an initial offset for the elapsed time.
        /// </summary>
        public void SetElapsedOffset(float pValue)
        {
            ElapsedOffset = pValue;
        }
        
        /// <summary>
        /// Sets the unique identifier for the event.
        /// </summary>
        public void SetId(int pId)
        {
            id = pId;
        }
        
        /// <summary>
        /// Immediately stops the countdown by setting the wait time equal to the elapsed time. The onTimeOut action will fire on the next update.
        /// </summary>
        public void Stop()
        {
            waitTime = Elapsed;
        }
    }

    /// <summary>
    /// Represents an event that executes an action when a specific condition becomes true.
    /// </summary>
    public class ConditionEvent
    {
        /// <summary>
        /// A unique identifier for the event. If non-zero, registering a new event with the same ID will replace the existing one.
        /// </summary>
        public int id;
        /// <summary>
        /// The delegate that is checked every frame. The event triggers when this returns true.
        /// </summary>
        public ConditionalDelegate triggerCondition;
        /// <summary>
        /// The action to execute once the triggerCondition is met.
        /// </summary>
        public Action onTrigger;
        /// <summary>
        /// An optional action that is executed every frame while the triggerCondition is false.
        /// </summary>
        public Action onUpdate;
        
        public ConditionEvent(int pId = 0)
        {
            id = pId;
        }

        /// <summary>
        /// Copies the properties of another ConditionEvent to this instance.
        /// </summary>
        public void Set(ConditionEvent other)
        {
            id = other.id;
            triggerCondition = other.triggerCondition;
            onTrigger = other.onTrigger;
            onUpdate = other.onUpdate;
        }
    }

    /// <summary>
    /// A wrapper class to associate a `BaseEvent` with a delay for deferred dispatching.
    /// </summary>
    public class DelayableEvent
    {
	    public string key;
	    public float delay;
	    public BaseEvent @event;
	    public DelayableEvent(BaseEvent pEvent, float pDelay)
	    {
		    @event = pEvent;
		    key = pEvent.GetType().ToString();
		    delay = pDelay;
	    }
    }

    //====================== COUNT DOWN EVENTS SYSTEM =========================

    /// <summary>
    /// A manager class that processes a collection of CountdownEvent objects.
    /// It should be updated every frame (e.g., in a central MonoBehaviour's LateUpdate).
    /// </summary>
    public class CountdownEventsGroup
    {
        private readonly List<CountdownEvent> m_countdownEvents = new List<CountdownEvent>();
        private float m_timeBeforePause;
        private float m_pauseSeconds;
        
        /// <summary>
        /// Returns true if there are no events currently registered.
        /// </summary>
        public bool IsEmpty => m_countdownEvents.Count == 0;
        
        /// <summary>
        /// Updates all registered countdown events. This should be called every frame.
        /// </summary>
        public void LateUpdate()
        {
            // Lock to ensure thread safety if events are added/removed from other threads.
            lock (m_countdownEvents)
            {
                // Iterate backwards to allow for safe removal.
                for (int i = m_countdownEvents.Count - 1; i >= 0; i--)
                {
                    var d = m_countdownEvents[i];

                    // Add elapsed time, respecting the unscaledTime flag.
                    if (d.unscaledTime)
                        d.AddElapsedTime(Time.unscaledDeltaTime + m_pauseSeconds);
                    else
                        d.AddElapsedTime(Time.deltaTime);

                    // Check for early termination.
                    if (d.breakCondition != null && d.breakCondition())
                    {
                        if (!d.autoRestart)
                            m_countdownEvents.RemoveAt(i);
                        else
                            d.Restart();
                    }
                    // Check for timeout.
                    else if (d.IsTimeOut)
                    {
                        d.onTimeOut?.Invoke(d.Elapsed - d.waitTime);
                        if (!d.autoRestart)
                            m_countdownEvents.RemoveAt(i);
                        else
                            d.Restart();
                    }
                }
            }
            // Reset the pause compensation after it has been applied once.
            m_pauseSeconds = 0;
        }

        /// <summary>
        /// Handles time calculation when the application is paused, for use with unscaled timers.
        /// </summary>
        public void OnApplicationPause(bool pause)
        {
            if (pause)
                m_timeBeforePause = Time.realtimeSinceStartup;
            else if (m_timeBeforePause > 0)
                m_pauseSeconds = Time.realtimeSinceStartup - m_timeBeforePause;
        }
        
        /// <summary>
        /// Adds a new CountdownEvent to be processed. If an event with the same ID already exists, it will be replaced.
        /// </summary>
        /// <param name="pEvent">The event to register.</param>
        public void Register(CountdownEvent pEvent)
        {
            if (pEvent.id == 0)
            {
                // If ID is 0, always add as a new event.
                if (!m_countdownEvents.Contains(pEvent))
                    m_countdownEvents.Add(pEvent);
            }
            else
            {
                // If ID is non-zero, check for an existing event to replace.
                bool exist = false;
                for (int i = 0; i < m_countdownEvents.Count; i++)
                {
                    if (pEvent.id == m_countdownEvents[i].id)
                    {
                        exist = true;
                        m_countdownEvents[i] = pEvent; // Replace
                        break;
                    }
                }
                if (!exist)
                    m_countdownEvents.Add(pEvent); // Add new
            }
        }
        
        /// <summary>
        /// Removes a registered event by its unique ID.
        /// </summary>
        /// <param name="pId">The ID of the event to remove.</param>
        public void UnRegister(int pId)
        {
            m_countdownEvents.RemoveAll(d => d.id == pId);
        }
        
        /// <summary>
        /// Removes a registered event by its object reference.
        /// </summary>
        /// <param name="pEvent">The event instance to remove.</param>
        public void UnRegister(CountdownEvent pEvent)
        {
            m_countdownEvents.Remove(pEvent);
        }
    }

    //====================== CONDITION EVENTS SYSTEM ===============================

    /// <summary>
    /// A manager class that processes a collection of ConditionEvent objects.
    /// It should be updated every frame (e.g., in a central MonoBehaviour's LateUpdate).
    /// </summary>
    public class ConditionEventsGroup
    {
        private readonly List<ConditionEvent> m_ConditionEvents = new List<ConditionEvent>();
        
        /// <summary>
        /// Returns true if there are no events currently registered.
        /// </summary>
        public bool IsEmpty => m_ConditionEvents.Count == 0;
        
        /// <summary>
        /// Updates all registered conditional events. This should be called every frame.
        /// </summary>
        public void LateUpdate()
        {
            // Lock to ensure thread safety if events are added/removed from other threads.
            lock (m_ConditionEvents)
            {
                // Iterate backwards to allow for safe removal.
                for (int i = m_ConditionEvents.Count - 1; i >= 0; i--)
                {
                    var d = m_ConditionEvents[i];
                    if (d.triggerCondition())
                    {
                        d.onTrigger?.Invoke();
                        m_ConditionEvents.RemoveAt(i);
                    }
                    else
                    {
                        d.onUpdate?.Invoke();
                    }
                }
            }
        }
        
        /// <summary>
        /// Adds a new ConditionEvent to be processed. If an event with the same ID already exists, it will be replaced.
        /// </summary>
        /// <param name="pEvent">The event to register.</param>
        public void Register(ConditionEvent pEvent)
        {
            if (pEvent.id == 0)
            {
                // If ID is 0, always add as a new event.
                m_ConditionEvents.Add(pEvent);
            }
            else
            {
                // If ID is non-zero, check for an existing event to replace.
                bool exist = false;
                for (int i = 0; i < m_ConditionEvents.Count; i++)
                {
                    if (pEvent.id == m_ConditionEvents[i].id)
                    {
                        exist = true;
                        m_ConditionEvents[i] = pEvent; // Replace
                        break;
                    }
                }
                if (!exist)
                    m_ConditionEvents.Add(pEvent); // Add new
            }
        }
        
        /// <summary>
        /// Removes a registered event by its unique ID.
        /// </summary>
        /// <param name="pId">The ID of the event to remove.</param>
        public void UnRegister(int pId)
        {
            m_ConditionEvents.RemoveAll(d => d.id == pId);
        }
        
        /// <summary>
        /// Removes a registered event by its object reference.
        /// </summary>
        /// <param name="pEvent">The event instance to remove.</param>
        public void UnRegister(ConditionEvent pEvent)
        {
            m_ConditionEvents.Remove(pEvent);
        }
    }
}