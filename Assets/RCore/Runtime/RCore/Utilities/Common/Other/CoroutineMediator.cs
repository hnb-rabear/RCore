using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Common
{
    public interface IUpdate
    {
        void Update(float pUnscaledDeltaTime);
    }

    public class CoroutineMediator : MonoBehaviour
    {
        private static CoroutineMediator mInstance;
        public static CoroutineMediator Instance
        {
            get
            {
                if (mInstance == null)
                    CreatInstance();

                return mInstance;
            }
        }

        private CountdownEventsManager mCountdownEventsManager = new CountdownEventsManager();
        private ConditionEventsManager mConditionEventsManager = new ConditionEventsManager();
        private List<IUpdate> mUpdateList = new List<IUpdate>();
        private Queue<Action> mListQueueActions = new Queue<Action>();

        public static void CreatInstance()
        {
            var obj = new GameObject("CoroutineMedator");
            mInstance = obj.AddComponent<CoroutineMediator>();
            obj.hideFlags = HideFlags.HideAndDontSave;
        }

        private void LateUpdate()
        {
            mCountdownEventsManager.LateUpdate();
            mConditionEventsManager.LateUpdate();

            for (int i = mUpdateList.Count - 1; i >= 0; i--)
            {
                var d = mUpdateList[i];
                d.Update(Time.unscaledDeltaTime);
            }
            if (mListQueueActions.Count > 0)
            {
                mListQueueActions.Peek().Raise();
                mListQueueActions.Dequeue();
            }
            enabled = !mCountdownEventsManager.IsEmpty || !mConditionEventsManager.IsEmpty || mUpdateList.Count > 0 || mListQueueActions.Count > 0;
        }

        public WaitUtil.CountdownEvent WaitForSecond(WaitUtil.CountdownEvent pEvent)
        {
            mCountdownEventsManager.Register(pEvent);
            enabled = true;
            return pEvent;
        }

        public void Enqueue(Action pDoSomething)
        {
            mListQueueActions.Enqueue(pDoSomething);
            enabled = true;
        }

        public WaitUtil.ConditionEvent WaitForCondition(WaitUtil.ConditionEvent pEvent)
        {
            mConditionEventsManager.Register(pEvent);
            enabled = true;
            return pEvent;
        }

        public IUpdate AddUpdate(IUpdate pUpdater)
        {
            if (!mUpdateList.Contains(pUpdater))
                mUpdateList.Add(pUpdater);
            enabled = true;
            return pUpdater;
        }

        public void Clear()
        {
            mCountdownEventsManager = new CountdownEventsManager();
            mConditionEventsManager = new ConditionEventsManager();
            mUpdateList = new List<IUpdate>();
            mListQueueActions = new Queue<Action>();
            enabled = false;
        }

        public void RemoveTimeAction(int id)
        {
            mCountdownEventsManager.UnRegister(id);
        }

        public void RemoveTriggerAction(int id)
        {
            mConditionEventsManager.UnRegister(id);
        }

        public void RemoveTimeAction(WaitUtil.CountdownEvent pCounter)
        {
            mCountdownEventsManager.UnRegister(pCounter);
        }

        public void RemoveTriggerAction(WaitUtil.ConditionEvent pCounter)
        {
            mConditionEventsManager.UnRegister(pCounter);
        }

        public void RemoveUpdate(IUpdate pUpdate)
        {
            mUpdateList.Remove(pUpdate);
        }

        public void OnApplicationPause(bool pause)
        {
            mCountdownEventsManager.OnApplicationPause(pause);
        }
    }
}