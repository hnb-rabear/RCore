/**
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace RCore.Common
{
    public class CoroutineUtil
    {
        private static CoroutineMediator m_Mediator => CoroutineMediator.Instance;

        public static Coroutine StartCoroutine(IEnumerator pCoroutine)
        {
            return m_Mediator.StartCoroutine(pCoroutine);
        }

        public static void StopCoroutine(Coroutine pCoroutine)
        {
            if (pCoroutine != null)
                m_Mediator.StopCoroutine(pCoroutine);
        }

        public static IEnumerator WaitForRealSeconds(float time)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup < start + time)
            {
                yield return null;
            }
        }

        public static IEnumerator WaitForAction(float time, Action pAction)
        {
            yield return new WaitForSeconds(time);
            pAction?.Invoke();
        }
    }

    //======================================================================================================

    public class CustomUpdate : IDisposable
    {
        public Action onFirstUpdate;
        public Action onLateUpdate;
        public ConditionalDelegate breakCondition;
        public Action onBreak;
        public float interval;
        public float timeOffset;

        private Coroutine mCoroutine;
        public float progress { get; private set; }
        public bool isWorking { get; private set; }

        public void Run()
        {
            mCoroutine = CoroutineUtil.StartCoroutine(IERun());
        }

        private IEnumerator IERun()
        {
            while (true)
            {
                isWorking = true;

                onFirstUpdate?.Invoke();

                progress = 0;

                float elapsedTime = timeOffset;
                while (interval > 0)
                {
                    if (breakCondition != null && breakCondition())
                    {
                        onBreak?.Invoke();
                        Kill();
                    }

                    yield return null;

                    elapsedTime += Time.deltaTime;
                    progress = elapsedTime / interval;

                    if (elapsedTime > interval)
                        break;
                }
                if (interval == 0)
                    yield return null;

                progress = 1f;

                onLateUpdate?.Invoke();
            }
        }

        public void Kill()
        {
            if (mCoroutine != null)
                CoroutineUtil.StopCoroutine(mCoroutine);

            isWorking = false;
        }

        public void Dispose()
        {
            Kill();
        }
    }

    //======================================================================================================

    public class CustomWait : IDisposable
    {
        public Action onStart;
        public Action onComplete;
        public ConditionalDelegate breakCondition;
        public Action onBreak;
        public float waitTime;
        public float offsetTime;

        private Coroutine mCoroutine;
        public float progress { get; private set; }
        public bool isRunning { get; private set; }
        public float remainTime { get; private set; }

        public void Run()
        {
            mCoroutine = CoroutineUtil.StartCoroutine(IERun());
        }

        private IEnumerator IERun()
        {
            isRunning = true;

            onStart?.Invoke();

            remainTime = waitTime - offsetTime;
            while (true)
            {
                remainTime -= Time.deltaTime;
                if (remainTime <= 0)
                    break;

                if (breakCondition != null && breakCondition())
                    Kill();

                yield return null;
            }

            isRunning = false;

            onComplete?.Invoke();
        }

        public float GetProgress()
        {
            return 1 - (remainTime / waitTime);
        }

        public void Kill()
        {
            if (mCoroutine != null)
                CoroutineUtil.StopCoroutine(mCoroutine);

            isRunning = false;
        }

        public void Dispose()
        {
            Kill();
        }
    }
}