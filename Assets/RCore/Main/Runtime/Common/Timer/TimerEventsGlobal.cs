using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	public class TimerEventsGlobal : TimerEvents
	{
		private static TimerEventsGlobal m_Instance;
		private static readonly Queue<Action> m_ExecutionQueue = new();

		public static TimerEventsGlobal Instance
		{
			get
			{
				if (m_Instance == null)
				{
					var obj = new GameObject(nameof(TimerEventsGlobal));
					m_Instance = obj.AddComponent<TimerEventsGlobal>();
					obj.hideFlags = HideFlags.HideAndDontSave;
				}

				return m_Instance;
			}
		}

		protected void Update()
		{
			lock (m_ExecutionQueue)
			{
				while (m_ExecutionQueue.Count > 0)
				{
					m_ExecutionQueue.Dequeue().Invoke();
				}
			}
		}

		public void Enqueue(Action action)
		{
			lock (m_ExecutionQueue)
			{
				m_ExecutionQueue.Enqueue(action);
				enabled = true;
			}
		}

		protected override bool CheckEnabled()
		{
			lock (m_ExecutionQueue)
			{
				return base.CheckEnabled() || m_ExecutionQueue.Count > 0;
			}
		}
	}
}