using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Common
{
    public class CoroutineMediatorForScene : MonoBehaviour
    {
        private static CoroutineMediator m_Instance;
        public static CoroutineMediator Instance
        {
            get
            {
                if (m_Instance == null)
                    CreatInstance();

                return m_Instance;
            }
        }

        private CountdownEventsManager m_CountdownEventsManager = new CountdownEventsManager();
        private ConditionEventsManager m_ConditionEventsManager = new ConditionEventsManager();
        private List<IUpdate> m_UpdateList = new List<IUpdate>();
        private Queue<Action> m_ListQueueActions = new Queue<Action>();

        public static void CreatInstance()
        {
            var obj = new GameObject("CoroutineMediatorForScene");
            m_Instance = obj.AddComponent<CoroutineMediator>();
            obj.hideFlags = HideFlags.HideInHierarchy;
        }

        private void LateUpdate()
        {
            m_CountdownEventsManager.LateUpdate();
            m_ConditionEventsManager.LateUpdate();

            for (int i = m_UpdateList.Count - 1; i >= 0; i--)
            {
                var d = m_UpdateList[i];
                d.Update(Time.unscaledDeltaTime);
            }
            if (m_ListQueueActions.Count > 0)
            {
                m_ListQueueActions.Peek().Raise();
                m_ListQueueActions.Dequeue();
            }
            enabled = !m_CountdownEventsManager.IsEmpty || !m_ConditionEventsManager.IsEmpty || m_UpdateList.Count > 0 || m_ListQueueActions.Count > 0;
        }

        public WaitUtil.CountdownEvent WaitForSecond(WaitUtil.CountdownEvent pEvent)
        {
            m_CountdownEventsManager.Register(pEvent);
            enabled = true;
            return pEvent;
        }

        public void Enqueue(Action pDoSomething)
        {
            m_ListQueueActions.Enqueue(pDoSomething);
            enabled = true;
        }

        public WaitUtil.ConditionEvent WaitForCondition(WaitUtil.ConditionEvent pEvent)
        {
            m_ConditionEventsManager.Register(pEvent);
            enabled = true;
            return pEvent;
        }

        public IUpdate AddUpdate(IUpdate pUpdater)
        {
            if (!m_UpdateList.Contains(pUpdater))
                m_UpdateList.Add(pUpdater);
            enabled = true;
            return pUpdater;
        }

        public void Clear()
        {
            m_CountdownEventsManager = new CountdownEventsManager();
            m_ConditionEventsManager = new ConditionEventsManager();
            m_UpdateList = new List<IUpdate>();
            m_ListQueueActions = new Queue<Action>();
            enabled = false;
        }

        public void RemoveTimeAction(int id)
        {
            m_CountdownEventsManager.UnRegister(id);
        }

        public void RemoveTriggerAction(int id)
        {
            m_ConditionEventsManager.UnRegister(id);
        }

        public void RemoveTimeAction(WaitUtil.CountdownEvent pCounter)
        {
            m_CountdownEventsManager.UnRegister(pCounter);
        }

        public void RemoveTriggerAction(WaitUtil.ConditionEvent pCounter)
        {
            m_ConditionEventsManager.UnRegister(pCounter);
        }

        public void RemoveUpdate(IUpdate pUpdate)
        {
            m_UpdateList.Remove(pUpdate);
        }

        public void OnApplicationPause(bool pause)
        {
            m_CountdownEventsManager.OnApplicationPause(pause);
        }
    }
}