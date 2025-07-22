using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	/// <summary>
	/// A global, singleton implementation of the TimerEvents system.
	/// It provides a persistent, scene-independent way to manage timed and conditional events.
	/// A key feature of this class is the addition of a thread-safe execution queue, allowing actions
	/// from background threads to be safely executed on Unity's main thread during the Update loop.
	/// </summary>
	public class TimerEventsGlobal : TimerEvents
	{
		private static TimerEventsGlobal m_Instance;
		// A thread-safe queue for actions that need to be run on the main thread.
		private static readonly Queue<Action> m_ExecutionQueue = new();

		/// <summary>
		/// Gets the singleton instance of the TimerEventsGlobal.
		/// If an instance does not exist, a new GameObject is created to host it.
		/// This GameObject is hidden and not saved with the scene, ensuring it persists across scene loads
		/// without cluttering the hierarchy.
		/// </summary>
		public static TimerEventsGlobal Instance
		{
			get
			{
				if (m_Instance == null)
				{
					var obj = new GameObject(nameof(TimerEventsGlobal));
					m_Instance = obj.AddComponent<TimerEventsGlobal>();
					// Hide from hierarchy, don't save to scene, and don't unload on scene change.
					DontDestroyOnLoad(obj);
					obj.hideFlags = HideFlags.HideAndDontSave;
				}
				return m_Instance;
			}
		}

		/// <summary>
		/// The Unity Update loop, used here to process the main-thread execution queue.
		/// It dequeues and invokes any pending actions in a thread-safe manner.
		/// </summary>
		protected void Update()
		{
			lock (m_ExecutionQueue)
			{
				while (m_ExecutionQueue.Count > 0)
				{
					// Dequeue and invoke the next action.
					m_ExecutionQueue.Dequeue().Invoke();
				}
			}
		}

		/// <summary>
		/// Adds an action to the queue to be executed on the main thread.
		/// This is the primary method for interacting with the Unity API from background threads.
		/// </summary>
		/// <param name="action">The action to execute on the main thread.</param>
		public void Enqueue(Action action)
		{
			lock (m_ExecutionQueue)
			{
				m_ExecutionQueue.Enqueue(action);
				// Ensure the component is enabled to process the queue.
				enabled = true;
			}
		}

		/// <summary>
		/// Overrides the base class method to also check if the execution queue has pending actions.
		/// This ensures the component remains active (and its Update/LateUpdate methods run) as long as there is
		/// any work to do, either from timed/conditional events or from the execution queue.
		/// </summary>
		/// <returns>True if there are any events or actions to process, otherwise false.</returns>
		protected override bool CheckEnabled()
		{
			lock (m_ExecutionQueue)
			{
				return base.CheckEnabled() || m_ExecutionQueue.Count > 0;
			}
		}
	}
}