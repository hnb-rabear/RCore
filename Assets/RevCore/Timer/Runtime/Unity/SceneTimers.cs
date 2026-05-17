using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// Scene-scoped <see cref="TimerDriver"/>. Unlike <see cref="GlobalTimers"/>, the host GameObject
	/// is destroyed on scene unload, so all pending timers cancel automatically when the scene exits.
	/// Use this for timers tied to a level / screen lifecycle.
	/// </summary>
	public sealed class SceneTimers : TimerDriver
	{
		private static SceneTimers s_instance;

		/// <summary>Singleton accessor. Reuses an existing instance in the scene if present; otherwise creates one and marks it hidden in the hierarchy.</summary>
		public static SceneTimers Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = FindObjectOfType<SceneTimers>();
					if (s_instance == null)
					{
						var obj = new GameObject(nameof(SceneTimers));
						s_instance = obj.AddComponent<SceneTimers>();
						obj.hideFlags = HideFlags.HideInHierarchy;
					}
				}

				return s_instance;
			}
		}
	}
}
