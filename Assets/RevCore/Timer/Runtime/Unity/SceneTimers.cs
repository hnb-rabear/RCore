using UnityEngine;

namespace RevCore
{
	public sealed class SceneTimers : TimerDriver
	{
		private static SceneTimers s_instance;

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
