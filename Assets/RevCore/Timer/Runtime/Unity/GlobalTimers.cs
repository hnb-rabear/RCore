using UnityEngine;

namespace RevCore
{
	public sealed class GlobalTimers : TimerDriver
	{
		private static GlobalTimers s_instance;

		public static GlobalTimers Instance
		{
			get
			{
				if (s_instance == null)
				{
					var obj = new GameObject(nameof(GlobalTimers));
					s_instance = obj.AddComponent<GlobalTimers>();
					DontDestroyOnLoad(obj);
					obj.hideFlags = HideFlags.HideAndDontSave;
				}

				return s_instance;
			}
		}
	}
}
