using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Common
{
    public interface IUpdate
    {
        void Update(float pDeltaTime);
    }

    public class CoroutineMediator : MonoBehaviour
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

        private List<DelayableEvent> m_DelayableEvents = new List<DelayableEvent>();
        private CountdownEventsManager m_CountdownEventsManager = new CountdownEventsManager();
        private ConditionEventsManager m_ConditionEventsManager = new ConditionEventsManager();
        private List<IUpdate> m_UpdateActions = new List<IUpdate>();
        private Queue<Action> m_QueueActions = new Queue<Action>();

        private static void CreatInstance()
        {
            var obj = new GameObject("CoroutineMediator");
            m_Instance = obj.AddComponent<CoroutineMediator>();
            obj.hideFlags = HideFlags.HideAndDontSave;
        }

        private void LateUpdate()
        {
            m_CountdownEventsManager.LateUpdate();
            m_ConditionEventsManager.LateUpdate();

            for (int i = m_UpdateActions.Count - 1; i >= 0; i--)
            {
                var d = m_UpdateActions[i];
                d.Update(Time.unscaledDeltaTime);
            }
            if (m_QueueActions.Count > 0)
            {
                m_QueueActions.Peek().Raise();
                m_QueueActions.Dequeue();
            }
            if (m_DelayableEvents.Count > 0)
            {
                for (int i = m_DelayableEvents.Count - 1; i >= 0; i--)
                {
                    m_DelayableEvents[i].delay -= Time.deltaTime;
                    if (m_DelayableEvents[i].delay <= 0)
                    {
                        EventDispatcher.Raise(m_DelayableEvents[i].@event);
                        m_DelayableEvents.RemoveAt(i);
                    }
                }
            }
            enabled = !m_CountdownEventsManager.IsEmpty || !m_ConditionEventsManager.IsEmpty || m_UpdateActions.Count > 0 || m_QueueActions.Count > 0 || m_DelayableEvents.Count > 0;
        }

        public WaitUtil.CountdownEvent WaitForSecond(WaitUtil.CountdownEvent pEvent)
        {
            m_CountdownEventsManager.Register(pEvent);
            enabled = true;
            return pEvent;
        }

        public void Enqueue(Action pDoSomething)
        {
            m_QueueActions.Enqueue(pDoSomething);
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
            if (!m_UpdateActions.Contains(pUpdater))
                m_UpdateActions.Add(pUpdater);
            enabled = true;
            return pUpdater;
        }

        public void Clear()
        {
            m_CountdownEventsManager = new CountdownEventsManager();
            m_ConditionEventsManager = new ConditionEventsManager();
            m_UpdateActions = new List<IUpdate>();
            m_QueueActions = new Queue<Action>();
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
            m_UpdateActions.Remove(pUpdate);
        }

        public void OnApplicationPause(bool pause)
        {
            m_CountdownEventsManager.OnApplicationPause(pause);
        }

        public void AddDelayableEvent(DelayableEvent e)
        {
            for (int i = 0; i < m_DelayableEvents.Count; i++)
            {
                if (m_DelayableEvents[i].key == e.key)
                {
                    m_DelayableEvents[i].@event = e.@event;
                    m_DelayableEvents[i].delay = e.delay;
                    enabled = true;
                    return;
                }
            }
            m_DelayableEvents.Add(e);
            enabled = true;
        }
    }
    
    public class DelayableEvent
    {
	    public readonly string key;
	    public float delay;
	    public BaseEvent @event;
	    public DelayableEvent(BaseEvent pEvent, float pDelay, int pSubKey)
	    {
		    @event = pEvent;
		    key = pEvent.GetType().ToString();
		    delay = pDelay;
		    if (pSubKey > 0)
			    key += pSubKey;
	    }
    }
}