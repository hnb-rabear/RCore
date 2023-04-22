using UnityEngine;
using RCore.Common;
using Debug = UnityEngine.Debug;

namespace RCore.Demo
{
    public class ExampleTimeCounterJobSystem : MonoBehaviour
    {
        private TimeCounterJobSystem m_TimeCounterJobSystem;

        private void Start()
        {
            m_TimeCounterJobSystem = new TimeCounterJobSystem();
            m_TimeCounterJobSystem.Register(3, true, (id, time, loop) =>
            {
                Debug.Log($"Time counter finished id:{id}, time:{time}, loop:{loop}");
            });
        }

        private void Update()
        {
            m_TimeCounterJobSystem.Update(Time.deltaTime);
        }

        private void LateUpdate()
        {
            m_TimeCounterJobSystem.LateUpdate();
        }

        private void OnDestroy()
        {
            m_TimeCounterJobSystem.Dispose();
        }
    }
}