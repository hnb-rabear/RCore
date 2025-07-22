using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RCore
{
	/// <summary>
	/// A MonoBehaviour that acts as a central processor for various timed and conditional events.
	/// It efficiently manages countdowns, conditional waits, and delayed events by disabling itself
	/// when there are no active events to process, saving update cycles.
	/// This class is often implemented as a singleton (e.g., TimerEventsInScene) for global access.
	/// </summary>
	public class TimerEvents : MonoBehaviour
	{
		private CountdownEventsGroup m_countdownEventsGroup = new();
		private ConditionEventsGroup m_conditionEventsGroup = new();
		private List<DelayableEvent> m_delayableEvents = new();

		/// <summary>
		/// The main update loop, called every frame by Unity when the component is enabled.
		/// It processes all event groups and then determines if it should remain enabled.
		/// </summary>
		protected virtual void LateUpdate()
		{
			m_countdownEventsGroup.LateUpdate();
			m_conditionEventsGroup.LateUpdate();

			// Process simple delayed events.
			if (m_delayableEvents.Count > 0)
			{
				for (int i = m_delayableEvents.Count - 1; i >= 0; i--)
				{
					m_delayableEvents[i].delay -= Time.deltaTime;
					if (m_delayableEvents[i].delay <= 0)
					{
						EventDispatcher.Raise(m_delayableEvents[i].@event);
						m_delayableEvents.RemoveAt(i);
					}
				}
			}
			
			// Performance optimization: disable this component if there's nothing to process.
			enabled = CheckEnabled();
		}

		/// <summary>
		/// Checks if there are any active events that need processing.
		/// </summary>
		/// <returns>True if there are active events, otherwise false.</returns>
		protected virtual bool CheckEnabled()
		{
			bool active = !m_countdownEventsGroup.IsEmpty || !m_conditionEventsGroup.IsEmpty || m_delayableEvents.Count > 0;
			return active;
		}

#region Countdown Events

		/// <summary>
		/// Registers a pre-configured CountdownEvent to be processed.
		/// </summary>
		/// <param name="pEvent">The CountdownEvent instance to register.</param>
		/// <returns>The registered event, for chaining or later reference.</returns>
		public CountdownEvent WaitForSeconds(CountdownEvent pEvent)
		{
			m_countdownEventsGroup.Register(pEvent);
			enabled = true; // Ensure the component is active to process the new event.
			return pEvent;
		}

		/// <summary>
		/// Creates and registers a new countdown timer.
		/// </summary>
		/// <param name="pTime">The duration in seconds to wait.</param>
		/// <param name="pDoSomething">The action to execute on timeout. The float parameter is the overtime value.</param>
		/// <returns>The created CountdownEvent instance.</returns>
		public CountdownEvent WaitForSeconds(float pTime, Action<float> pDoSomething)
		{
			var @event = new CountdownEvent
			{
				waitTime = pTime,
				onTimeOut = pDoSomething
			};
			return WaitForSeconds(@event);
		}
		
		/// <summary>
		/// Creates and registers a new countdown timer.
		/// </summary>
		/// <param name="pTime">The duration in seconds to wait.</param>
		/// <param name="onTimeOut">The action to execute on timeout.</param>
		/// <returns>The created CountdownEvent instance.</returns>
		public CountdownEvent WaitForSeconds(float pTime, Action onTimeOut)
		{
			var @event = new CountdownEvent
			{
				waitTime = pTime,
				onTimeOut = s => onTimeOut?.Invoke()
			};
			return WaitForSeconds(@event);
		}

		/// <summary>
		/// Creates and registers a new countdown timer, with an option for unscaled time.
		/// </summary>
		/// <param name="pTime">The duration in seconds to wait.</param>
		/// <param name="pUnscaledTime">If true, the timer ignores Time.timeScale.</param>
		/// <param name="pDoSomething">The action to execute on timeout. The float parameter is the overtime value.</param>
		/// <returns>The created CountdownEvent instance.</returns>
		public CountdownEvent WaitForSeconds(float pTime, bool pUnscaledTime, Action<float> pDoSomething)
		{
			var @event = new CountdownEvent
			{
				waitTime = pTime,
				onTimeOut = pDoSomething,
				unscaledTime = pUnscaledTime
			};
			return WaitForSeconds(@event);
		}

		/// <summary>
		/// Removes a countdown event by its unique ID, stopping it from being processed further.
		/// </summary>
		/// <param name="id">The ID of the event to remove.</param>
		public void RemoveCountdownEvent(int id)
		{
			m_countdownEventsGroup.UnRegister(id);
		}

		/// <summary>
		/// Removes a countdown event by its object reference.
		/// </summary>
		/// <param name="pCounter">The CountdownEvent instance to remove.</param>
		public void RemoveCountdownEvent(CountdownEvent pCounter)
		{
			m_countdownEventsGroup.UnRegister(pCounter);
		}

#endregion

#region Condition Events

		/// <summary>
		/// Registers a pre-configured ConditionEvent to be processed.
		/// </summary>
		/// <param name="pEvent">The ConditionEvent instance to register.</param>
		/// <returns>The registered event, for chaining or later reference.</returns>
		public ConditionEvent WaitForCondition(ConditionEvent pEvent)
		{
			m_conditionEventsGroup.Register(pEvent);
			enabled = true; // Ensure the component is active to process the new event.
			return pEvent;
		}

		/// <summary>
		/// Creates and registers a new event that waits for a condition to be met.
		/// </summary>
		/// <param name="pTriggerCondition">The delegate that will be checked each frame. The event triggers when it returns true.</param>
		/// <param name="onTrigger">The action to execute once the condition is met.</param>
		/// <returns>The created ConditionEvent instance.</returns>
		public ConditionEvent WaitForCondition(ConditionalDelegate pTriggerCondition, Action onTrigger)
		{
			var @event = new ConditionEvent
			{
				onTrigger = onTrigger,
				triggerCondition = pTriggerCondition
			};
			return WaitForCondition(@event);
		}

		/// <summary>
		/// Removes a conditional event by its unique ID.
		/// </summary>
		/// <param name="id">The ID of the event to remove.</param>
		public void RemoveConditionEvent(int id)
		{
			m_conditionEventsGroup.UnRegister(id);
		}

		/// <summary>
		/// Removes a conditional event by its object reference.
		/// </summary>
		/// <param name="pCounter">The ConditionEvent instance to remove.</param>
		public void RemoveConditionEvent(ConditionEvent pCounter)
		{
			m_conditionEventsGroup.UnRegister(pCounter);
		}

		/// <summary>
		/// A helper method to execute an action when a `System.Threading.Tasks.Task` is completed.
		/// </summary>
		/// <param name="pTask">The Task to wait for.</param>
		/// <param name="onTrigger">The action to execute upon completion.</param>
		/// <returns>The original Task, for chaining.</returns>
		public Task WaitTask(Task pTask, Action onTrigger)
		{
			WaitForCondition(new ConditionEvent()
			{
				triggerCondition = () => pTask.IsCompleted,
				onTrigger = onTrigger
			});
			return pTask;
		}
		
		/// <summary>
		/// A helper method to execute an action when a Unity `ResourceRequest` is completed.
		/// </summary>
		/// <param name="pTask">The ResourceRequest to wait for.</param>
		/// <param name="onTrigger">The action to execute upon completion.</param>
		/// <returns>The original ResourceRequest, for chaining.</returns>
		public ResourceRequest WaitTask(ResourceRequest pTask, Action onTrigger)
		{
			WaitForCondition(new ConditionEvent()
			{
				triggerCondition = () => pTask.isDone,
				onTrigger = onTrigger
			});
			return pTask;
		}

#endregion

		/// <summary>
		/// Passes application pause state changes to the CountdownEventsGroup to handle unscaled time correctly.
		/// </summary>
		public void OnApplicationPause(bool pause)
		{
			m_countdownEventsGroup.OnApplicationPause(pause);
		}

		/// <summary>
		/// Adds a simple delayed event to be dispatched via the EventDispatcher.
		/// If an event with the same key (type name) already exists, it is replaced, effectively debouncing it.
		/// </summary>
		/// <param name="e">The DelayableEvent to add.</param>
		public void AddDelayableEvent(DelayableEvent e)
		{
			// Check if an event of the same type is already queued.
			for (int i = 0; i < m_delayableEvents.Count; i++)
			{
				if (m_delayableEvents[i].key == e.key)
				{
					// If so, update it instead of adding a new one.
					m_delayableEvents[i].@event = e.@event;
					m_delayableEvents[i].delay = e.delay;
					enabled = true;
					return;
				}
			}
			m_delayableEvents.Add(e);
			enabled = true;
		}

		/// <summary>
		/// Removes all pending events from all groups, effectively resetting the component.
		/// </summary>
		public void Clear()
		{
			m_countdownEventsGroup = new CountdownEventsGroup();
			m_conditionEventsGroup = new ConditionEventsGroup();
			m_delayableEvents = new List<DelayableEvent>();
		}
	}
}