using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// DontDestroyOnLoad driver that wires the process-global <see cref="Timers.Scheduler"/> to a
	/// real Unity frame loop. Auto-instantiated on first access — consumers never construct it manually.
	/// </summary>
	public sealed class GlobalTimers : TimerDriver
	{
		private static GlobalTimers s_instance;

		/// <summary>Singleton accessor. Creates the host GameObject on first call and marks it hidden so it doesn't clutter the hierarchy.</summary>
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
