using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RCore
{
	public interface IUpdate
	{
		public bool stop { get; set; }
		void Update(float pDeltaTime);
	}

	public class TimerEvents : MonoBehaviour
	{
		private CountdownEventsGroup m_countdownEventsGroup = new CountdownEventsGroup();
		private ConditionEventsGroup m_conditionEventsGroup = new ConditionEventsGroup();
		private List<DelayableEvent> m_DelayableEvents = new List<DelayableEvent>();
		private List<IUpdate> m_updateActions = new List<IUpdate>();

		public Benchmark benchmark;
		protected virtual void Update()
		{
			for (int i = m_updateActions.Count - 1; i >= 0; i--)
			{
				var d = m_updateActions[i];
				d.Update(Time.deltaTime);
				if (d.stop)
					m_updateActions.RemoveAt(i);
			}
		}
		protected virtual void LateUpdate()
		{
			m_countdownEventsGroup.LateUpdate();
			m_conditionEventsGroup.LateUpdate();

			if (m_DelayableEvents.Count > 0)
			{
				for (int i = m_DelayableEvents.Count - 1; i >= 0; i--)
				{
					m_DelayableEvents[i].delay -= Time.deltaTime;
					if (m_DelayableEvents[i].delay <= 0)
					{
						EventDispatcher.Raise(m_DelayableEvents[i].@event);
						m_DelayableEvents.RemoveAt(i);
					}
				}
			}
			enabled = CheckEnabled();
		}

		protected virtual bool CheckEnabled()
		{
			bool active = !m_countdownEventsGroup.IsEmpty || !m_conditionEventsGroup.IsEmpty || m_updateActions.Count > 0 || m_DelayableEvents.Count > 0;
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

		public void RemoveUpdate(IUpdate pUpdate)
		{
			m_updateActions.Remove(pUpdate);
		}
		public void OnApplicationPause(bool pause)
		{
			m_countdownEventsGroup.OnApplicationPause(pause);
		}
		public void AddDelayableEvent(DelayableEvent e)
		{
			for (int i = 0; i < m_DelayableEvents.Count; i++)
			{
				if (m_DelayableEvents[i].key == e.key)
				{
					m_DelayableEvents[i].@event = e.@event;
					m_DelayableEvents[i].delay = e.delay;
					enabled = true;
					return;
				}
			}
			m_DelayableEvents.Add(e);
			enabled = true;
		}
		public IUpdate AddUpdate(IUpdate pUpdater)
		{
			if (!m_updateActions.Contains(pUpdater))
				m_updateActions.Add(pUpdater);
			enabled = true;
			return pUpdater;
		}
		public void Clear()
		{
			m_countdownEventsGroup = new CountdownEventsGroup();
			m_conditionEventsGroup = new ConditionEventsGroup();
			m_DelayableEvents = new List<DelayableEvent>();
			m_updateActions = new List<IUpdate>();
		}

		public void StartBenchmark(float duration, Action<int, int, int> onFinishedBenchmark)
		{
			benchmark = new Benchmark(duration, onFinishedBenchmark);
			AddUpdate(benchmark);
		}
	}
}