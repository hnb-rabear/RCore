/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace RCore.Common
{
    public class WaitUtil
    {
        [Serializable]
        public class CountdownEvent
        {
            public int id;
            public Action<float> doSomething;
            public ConditionalDelegate breakCondition;
            public float waitTime;
            public bool unscaledTime;
            public bool autoRestart;

            private float mElapsed;
            private float mElapsedOffset;

            public float Elapsed => mElapsed;
            public float ElapsedOffset => mElapsedOffset;
            public bool IsTimeOut => mElapsed >= waitTime;
            public float Remain => waitTime - mElapsed > 0 ? waitTime - mElapsed : 0;

            public CountdownEvent(int pid = 0)
            {
                id = pid;
            }

            public void Set(CountdownEvent other)
            {
                id = other.id;
                doSomething = other.doSomething;
                breakCondition = other.breakCondition;
                waitTime = other.waitTime;
                unscaledTime = other.unscaledTime;
                mElapsed = other.mElapsed;
                mElapsedOffset = other.mElapsedOffset;
            }

            public void AddElapsedTime(float pValue)
            {
                mElapsed += pValue;
            }

            public void Restart()
            {
                mElapsed = 0;
                mElapsedOffset = 0;
            }

            public void SetElapsedOffset(float pValue)
            {
                mElapsedOffset = pValue;
            }

            public void Run()
            {
                Restart();
                Start(this);
            }

            public void SetId(int pId)
            {
                id = pId;
            }
        }

        //=======================================================

        [Serializable]
        public class ConditionEvent
        {
            public int id;
            public ConditionalDelegate triggerCondition;
            public Action onTrigger;
            public Action onUpdate;

            public ConditionEvent(int pId = 0)
            {
                id = pId;
            }

            public void Set(ConditionEvent other)
            {
                id = other.id;
                triggerCondition = other.triggerCondition;
                onTrigger = other.onTrigger;
                onUpdate = other.onUpdate;
            }

            public void Start()
            {
                WaitUtil.Start(this);
            }
        }

        //=======================================================

        private static CoroutineMediator mMediator => CoroutineMediator.Instance;

        /// <summary>
        /// This Wait uses Update to calcualate time
        /// </summary>
        public static CountdownEvent Start(CountdownEvent pScheduleEvent)
        {
            return mMediator.WaitForSecond(pScheduleEvent);
        }
        public static CountdownEvent Start(float pTime, Action<float> pDoSomething)
        {
            return mMediator.WaitForSecond(new CountdownEvent() { waitTime = pTime, doSomething = pDoSomething });
        }
        public static CountdownEvent Start(float pTime, bool pUnscaledTime, Action<float> pDoSomething)
        {
            return mMediator.WaitForSecond(new CountdownEvent() { waitTime = pTime, doSomething = pDoSomething, unscaledTime = pUnscaledTime });
        }
        public static void RemoveCountdownEvent(int pId)
        {
            mMediator.RemoveTimeAction(pId);
        }
        public static void RemoveCountdownEvent(CountdownEvent pEvent)
        {
            mMediator.RemoveTimeAction(pEvent);
        }

        /// <summary>
        /// This Wait uses Update to check condition
        /// </summary>
        public static ConditionEvent Start(ConditionEvent pScheduleEvent)
        {
            return mMediator.WaitForCondition(pScheduleEvent);
        }
        public static ConditionEvent Start(ConditionalDelegate pTriggerCondition, Action pDoSomething)
        {
            return mMediator.WaitForCondition(new ConditionEvent() { onTrigger = pDoSomething, triggerCondition = pTriggerCondition });
        }
        public static void RemoveConditionEvent(int pId)
        {
            mMediator.RemoveTriggerAction(pId);
        }
        public static void RemoveConditionEvent(ConditionEvent pEvent)
        {
            mMediator.RemoveTriggerAction(pEvent);
        }

        /// <summary>
        /// External update
        /// </summary>
        public static IUpdate AddUpdate(IUpdate pUpdate)
        {
            return mMediator.AddUpdate(pUpdate);
        }
        public static void RemoveUpdate(IUpdate pUpdate)
        {
            mMediator.RemoveUpdate(pUpdate);
        }

        /// <summary>
        /// External Queue
        /// </summary>
        public static void Enqueue(Action pDoSomething)
        {
            mMediator.Enqueue(pDoSomething);
        }

        public static Task WaitTask(Task pTask, Action pOnTaskFinished)
        {
            Start(new ConditionEvent()
            {
                triggerCondition = () => pTask.IsCompleted,
                onTrigger = pOnTaskFinished
            });
            return pTask;
        }
    }

    //====================== COUNT DOWN EVENTS SYSTEM =========================

    public class CountdownEventsManager
    {
        private List<WaitUtil.CountdownEvent> mCountdownEvents = new List<WaitUtil.CountdownEvent>();
        private float mTimeBeforePause;

        public bool IsEmpty => mCountdownEvents.Count == 0;

        public void LateUpdate()
        {
            float pausedTime = 0;
            if (mTimeBeforePause > 0)
            {
                pausedTime = Time.unscaledTime - mTimeBeforePause;
                mTimeBeforePause = 0;
            }

            lock (mCountdownEvents)
            {
                for (int i = mCountdownEvents.Count - 1; i >= 0; i--)
                {
                    var d = mCountdownEvents[i];

                    if (d.unscaledTime)
                        d.AddElapsedTime(Time.unscaledDeltaTime + pausedTime);
                    else
                        d.AddElapsedTime(Time.deltaTime);
                    if (d.breakCondition != null && d.breakCondition())
                    {
                        if (!d.autoRestart)
                            mCountdownEvents.Remove(d);
                        else
                            d.Restart();
                    }
                    else if (d.IsTimeOut)
                    {
                        d.doSomething(d.Elapsed - d.waitTime);
                        if (!d.autoRestart)
                            mCountdownEvents.Remove(d);
                        else
                            d.Restart();
                    }
                }
            }
        }

        public void OnApplicationPause(bool pause)
        {
            if (!pause)
                mTimeBeforePause = Time.unscaledTime;
        }

        public void Register(WaitUtil.CountdownEvent pEvent)
        {
            if (pEvent.id == 0)
            {
                mCountdownEvents.Add(pEvent);
            }
            else
            {
                bool exist = false;
                for (int i = 0; i < mCountdownEvents.Count; i++)
                {
                    if (pEvent.id == mCountdownEvents[i].id)
                    {
                        exist = true;
                        mCountdownEvents[i] = pEvent;
                        break;
                    }
                }

                if (!exist)
                    mCountdownEvents.Add(pEvent);
            }
        }

        public void UnRegister(int pId)
        {
            for (int i = 0; i < mCountdownEvents.Count; i++)
            {
                var d = mCountdownEvents[i];
                if (d.id == pId)
                {
                    mCountdownEvents.Remove(d);
                    return;
                }
            }
        }

        public void UnRegister(WaitUtil.CountdownEvent pEvent)
        {
            mCountdownEvents.Remove(pEvent);
        }
    }

    //====================== CONDITION EVENTS SYSTEM ===============================

    public class ConditionEventsManager
    {
        private List<WaitUtil.ConditionEvent> mConditionEvents = new List<WaitUtil.ConditionEvent>();

        public bool IsEmpty => mConditionEvents.Count == 0;

        public void LateUpdate()
        {
            lock (mConditionEvents)
            {
                for (int i = mConditionEvents.Count - 1; i >= 0; i--)
                {
                    var d = mConditionEvents[i];
                    if (d.triggerCondition())
                    {
                        d.onTrigger();
                        mConditionEvents.Remove(d);
                    }
                    else
                    {
                        d.onUpdate?.Invoke();
                    }
                }
            }
        }

        public void Register(WaitUtil.ConditionEvent pEvent)
        {
            if (pEvent.id == 0)
            {
                mConditionEvents.Add(pEvent);
            }
            else
            {
                bool exist = false;
                for (int i = 0; i < mConditionEvents.Count; i++)
                {
                    if (pEvent.id == mConditionEvents[i].id)
                    {
                        exist = true;
                        mConditionEvents[i] = pEvent;
                        break;
                    }
                }

                if (!exist)
                    mConditionEvents.Add(pEvent);
            }
        }

        public void UnRegister(int pId)
        {
            for (int i = 0; i < mConditionEvents.Count; i++)
            {
                var d = mConditionEvents[i];
                if (d.id == pId)
                {
                    mConditionEvents.Remove(d);
                    return;
                }
            }
        }

        public void UnRegister(WaitUtil.ConditionEvent pEvent)
        {
            mConditionEvents.Remove(pEvent);
        }
    }
}
