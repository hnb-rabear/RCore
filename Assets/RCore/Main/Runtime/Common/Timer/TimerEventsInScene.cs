using UnityEngine;

namespace RCore
{
	/// <summary>
	/// A scene-specific singleton implementation of the TimerEvents system.
	/// This provides a convenient, globally accessible point for managing timed and conditional events
	/// that are relevant only to the currently loaded scene. Unlike TimerEventsGlobal, this instance
	/// and all its pending events will be destroyed when a new scene is loaded.
	/// </summary>
	public class TimerEventsInScene : TimerEvents
	{
		private static TimerEventsInScene m_Instance;

		/// <summary>
		/// Gets the singleton instance of TimerEventsInScene for the current scene.
		/// If an instance does not exist, a new GameObject is automatically created to host it.
		/// This GameObject is hidden from the hierarchy to avoid clutter.
		/// </summary>
		public static TimerEventsInScene Instance
		{
			get
			{
				// If the instance is null (e.g., first access or after a scene load), create it.
				if (m_Instance == null)
				{
					// Check if an instance already exists in the scene to avoid duplicates.
					m_Instance = FindObjectOfType<TimerEventsInScene>();
					if (m_Instance == null)
					{
						var obj = new GameObject(nameof(TimerEventsInScene));
						m_Instance = obj.AddComponent<TimerEventsInScene>();
						// Hide the manager from the hierarchy view to keep the scene clean.
						obj.hideFlags = HideFlags.HideInHierarchy;
					}
				}
				return m_Instance;
			}
		}
	}
}