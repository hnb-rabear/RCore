using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RCore
{
	public class TimerEvents : MonoBehaviour
	{
		private CountdownEventsGroup m_countdownEventsGroup = new();
		private ConditionEventsGroup m_conditionEventsGroup = new();
		private List<DelayableEvent> m_delayableEvents = new();

		protected virtual void LateUpdate()
		{
			m_countdownEventsGroup.LateUpdate();
			m_conditionEventsGroup.LateUpdate();

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
			enabled = CheckEnabled();
		}
		protected virtual bool CheckEnabled()
		{
			bool active = !m_countdownEventsGroup.IsEmpty || !m_conditionEventsGroup.IsEmpty || m_delayableEvents.Count > 0;
			return active;
		}

#region Countdown Events

		public CountdownEvent WaitForSeconds(CountdownEvent pEvent)
		{
			m_countdownEventsGroup.Register(pEvent);
			enabled = true;
			return pEvent;
		}
		public CountdownEvent WaitForSeconds(float pTime, Action<float> pDoSomething)
		{
			var @event = new CountdownEvent
			{
				waitTime = pTime,
				onTimeOut = pDoSomething
			};
			return WaitForSeconds(@event);
		}
		public CountdownEvent WaitForSeconds(float pTime, Action onTimeOut)
		{
			var @event = new CountdownEvent
			{
				waitTime = pTime,
				onTimeOut = s => onTimeOut()
			};
			return WaitForSeconds(@event);
		}
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
		public void RemoveCountdownEvent(int id)
		{
			m_countdownEventsGroup.UnRegister(id);
		}
		public void RemoveCountdownEvent(CountdownEvent pCounter)
		{
			m_countdownEventsGroup.UnRegister(pCounter);
		}

#endregion

#region Condition Events

		public ConditionEvent WaitForCondition(ConditionEvent pEvent)
		{
			m_conditionEventsGroup.Register(pEvent);
			enabled = true;
			return pEvent;
		}
		public ConditionEvent WaitForCondition(ConditionalDelegate pTriggerCondition, Action onTrigger)
		{
			var @event = new ConditionEvent
			{
				onTrigger = onTrigger,
				triggerCondition = pTriggerCondition
			};
			return WaitForCondition(@event);
		}
		public void RemoveConditionEvent(int id)
		{
			m_conditionEventsGroup.UnRegister(id);
		}
		public void RemoveConditionEvent(ConditionEvent pCounter)
		{
			m_conditionEventsGroup.UnRegister(pCounter);
		}
		public Task WaitTask(Task pTask, Action onTrigger)
		{
			WaitForCondition(new ConditionEvent()
			{
				triggerCondition = () => pTask.IsCompleted,
				onTrigger = onTrigger
			});
			return pTask;
		}
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

		public void OnApplicationPause(bool pause)
		{
			m_countdownEventsGroup.OnApplicationPause(pause);
		}
		public void AddDelayableEvent(DelayableEvent e)
		{
			for (int i = 0; i < m_delayableEvents.Count; i++)
			{
				if (m_delayableEvents[i].key == e.key)
				{
					m_delayableEvents[i].@event = e.@event;
					m_delayableEvents[i].delay = e.delay;
					enabled = true;
					return;
				}
			}
			m_delayableEvents.Add(e);
			enabled = true;
		}
		public void Clear()
		{
			m_countdownEventsGroup = new CountdownEventsGroup();
			m_conditionEventsGroup = new ConditionEventsGroup();
			m_delayableEvents = new List<DelayableEvent>();
		}
	}
}