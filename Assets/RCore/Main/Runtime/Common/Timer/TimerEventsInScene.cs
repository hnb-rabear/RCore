using UnityEngine;

namespace RCore
{
    public class TimerEventsInScene : TimerEvents
    {
        private static TimerEventsInScene m_Instance;
        public static TimerEventsInScene Instance
        {
            get
            {
	            if (m_Instance == null)
	            {
		            var obj = new GameObject("TimerEventsInScene");
		            m_Instance = obj.AddComponent<TimerEventsInScene>();
		            obj.hideFlags = HideFlags.HideInHierarchy;
	            }

                return m_Instance;
            }
        }
    }
}