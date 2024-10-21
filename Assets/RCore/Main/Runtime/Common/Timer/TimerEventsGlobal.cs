using UnityEngine;

namespace RCore
{
    public class TimerEventsGlobal : TimerEvents
    {
        private static TimerEventsGlobal m_Instance;
        public static TimerEventsGlobal Instance
        {
            get
            {
                if (m_Instance == null)
                    CreatInstance();

                return m_Instance;
            }
        }

        private static void CreatInstance()
        {
            var obj = new GameObject("TimerEventsGlobal");
            m_Instance = obj.AddComponent<TimerEventsGlobal>();
            obj.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}