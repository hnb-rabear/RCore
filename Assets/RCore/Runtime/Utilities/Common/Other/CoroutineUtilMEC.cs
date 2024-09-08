/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

//#define USE_MEC

#if USE_MEC
using MEC;
#endif

namespace RCore.Common
{
    public class CoroutineUtilMEC
    {
#if USE_MEC
        private static List<KeyValuePair<int, CoroutineHandle>> mListCoroutine = new List<KeyValuePair<int, CoroutineHandle>>();

        public static IEnumerator<float> IEWaitForRealSeconds(float time)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup < start + time)
            {
                yield return Timing.WaitForOneFrame;
            }
        }

        //===

        public static CoroutineHandle StartCoroutine(IEnumerator<float> pCoroutine)
        {
            return Timing.RunCoroutine(pCoroutine);
        }

        public static void StopCoroutine(CoroutineHandle pCoroutine)
        {
            Timing.KillCoroutines(pCoroutine);
        }

        //===

        public static CoroutineHandle WaitUntil(int pId, Action pAction, float pTime, bool pIgnoreTimeScale = false)
        {
            CheckListCoroutine(pId);

            var cc = Timing.RunCoroutine(IEWaitUntil(pAction, pTime, pIgnoreTimeScale));
            mListCoroutine.Add(new KeyValuePair<int, CoroutineHandle>(pId, cc));
            return cc;
        }

        public static CoroutineHandle WaitUntil(Action pOnFinished, float pTime, bool pIgnoreTimeScale = false)
        {
            return Timing.RunCoroutine(IEWaitUntil(pOnFinished, pTime, pIgnoreTimeScale));
        }

        private static IEnumerator<float> IEWaitUntil(Action pOnFinished, float pTime, bool pIgnoreTimeScale = false)
        {
            if (!pIgnoreTimeScale)
                yield return Timing.WaitForSeconds(pTime);
            else
                yield return Timing.WaitUntilDone(Timing.RunCoroutine(IEWaitForRealSeconds(pTime)));

            pOnFinished();
        }

        //===

        public static CoroutineHandle WaitUntil(Action pOnFinish, Action pOnUpdated, float pTime)
        {
            return Timing.RunCoroutine(IEWaitUntil(pOnFinish, pOnUpdated, pTime));
        }

        private static IEnumerator<float> IEWaitUntil(Action pOnFinish, Action pOnUpdate, float pTime)
        {
            float time = 0;
            while (true)
            {
                if (pOnUpdate != null)
                    pOnUpdate();

                time += Time.deltaTime;
                if (time > pTime)
                    break;

                yield return Timing.WaitForOneFrame;
            }

            pOnFinish();
        }

        //===

        public static CoroutineHandle WaitUntil(Action pAction, ConditionalDelegate pBreakCondition)
        {
            return Timing.RunCoroutine(IEWaitUntil(pAction, pBreakCondition));
        }

        private static IEnumerator<float> IEWaitUntil(Action pAction, ConditionalDelegate pBreakCondition)
        {
            while (true)
            {
                if (pBreakCondition())
                    break;

                yield return Timing.WaitForOneFrame;
            }

            pAction();
        }

        //===

        public static CoroutineHandle Update(GameObject pCancelWith, Action pUpdateAction, float pTime = 0)
        {
            if (pCancelWith != null)
                return Timing.RunCoroutine(IEUpdate(pUpdateAction, pTime).CancelWith(pCancelWith));
            else
                return Timing.RunCoroutine(IEUpdate(pUpdateAction, pTime));
        }

        private static IEnumerator<float> IEUpdate(Action pUpdateAction, float pTime = 0)
        {
            while (true)
            {
                pUpdateAction();

                if (pTime > 0)
                    yield return Timing.WaitForSeconds(pTime);
                else
                    yield return Timing.WaitForOneFrame;
            }
        }

        //===

        public static CoroutineHandle Update(Action pFirstUpdate, ConditionalDelegate pBreakCondition, Action pOnBreak, Action pLateUpdate, float pTime = 0)
        {
            return Timing.RunCoroutine(IEUpdate(pFirstUpdate, pBreakCondition, pOnBreak, pLateUpdate, pTime));
        }

        private static IEnumerator<float> IEUpdate(Action pFirstUpdate, ConditionalDelegate pBreakCondition, Action pOnBreak, Action pLateUpdate, float pTime = 0)
        {
            while (true)
            {
                if (pBreakCondition())
                {
                    if (pOnBreak != null)
                        pOnBreak();
                    break;
                }

                if (pFirstUpdate != null)
                    pFirstUpdate();

                if (pTime > 0)
                    yield return Timing.WaitForSeconds(pTime);
                else
                    yield return Timing.WaitForOneFrame;

                if (pLateUpdate != null)
                    pLateUpdate();
            }
        }

        //===

        private static void CheckListCoroutine(int pId)
        {
            foreach (var c in mListCoroutine)
            {
                if (c.Key == pId)
                {
                    Debug.Log("Stop coroutine " + c.Key);
                    Timing.KillCoroutines(c.Value);
                    mListCoroutine.Remove(c);
                    break;
                }
            }
        }
    }

    public class CustomUpdateMEC
    {
        public Action onFirstUpdate;
        public Action onLateUpdate;
        public ConditionalDelegate breakCondition;
        public Action onBreak;
        public float interval;
        public float timeOffset;
        public bool needDetail = true;

        private CoroutineHandle mCoroutine;
        public float progress { get; private set; }
        public float remainSeconds { get { return interval * (1 - progress); } }
        public float elapsedSeconds { get { return interval * progress; } }
        public bool isWorking { get; private set; }

        public void Run()
        {
            if (needDetail)
                mCoroutine = Timing.RunCoroutine(IE_RunDetail());
            else
                mCoroutine = Timing.RunCoroutine(IE_RunSimple());
        }

        private IEnumerator<float> IE_RunDetail()
        {
            while (true)
            {
                isWorking = true;

                if (onFirstUpdate != null)
                    onFirstUpdate();

                progress = 0;

                float elapsedTime = timeOffset;
                while (interval > 0)
                {
                    if (breakCondition != null && breakCondition())
                    {
                        if (onBreak != null) onBreak();
                        Kill();
                    }

                    yield return Timing.WaitForOneFrame;

                    elapsedTime += Time.deltaTime;
                    progress = elapsedTime / interval;

                    if (elapsedTime > interval)
                        break;
                }
                if (interval == 0)
                    yield return Timing.WaitForOneFrame;

                progress = 1f;

                if (onLateUpdate != null)
                    onLateUpdate();
            }
        }

        private IEnumerator<float> IE_RunSimple()
        {
            while (true)
            {
                isWorking = true;

                if (onFirstUpdate != null)
                    onFirstUpdate();

                progress = 0;

                if (breakCondition != null && breakCondition())
                {
                    if (onBreak != null) onBreak();
                    Kill();
                }

                if (interval == 0)
                    yield return Timing.WaitForOneFrame;
                else
                    yield return Timing.WaitForSeconds(interval);

                progress = 1f;

                if (breakCondition != null && breakCondition())
                {
                    if (onBreak != null) onBreak();
                    Kill();
                }

                if (onLateUpdate != null)
                    onLateUpdate();
            }
        }

        public void Kill()
        {
            Timing.KillCoroutines(mCoroutine);
            isWorking = false;
        }
    }

    public class CustomWaitMEC
    {
        public Action onStart;
        public Action onDone;
        public ConditionalDelegate breakCondition;
        public Action onBreak;
        public float time;
        public float timeOffset;
        public bool needDetail = true;
        private CoroutineHandle mCoroutine;

        public float progress { get; private set; }
        public bool isWorking { get; private set; }

        public void Run()
        {
            if (needDetail)
                mCoroutine = Timing.RunCoroutine(IE_RunDetail());
            else
                mCoroutine = Timing.RunCoroutine(IE_RunSimple());
        }

        private IEnumerator<float> IE_RunDetail()
        {
            isWorking = true;

            if (onStart != null)
                onStart();

            progress = 0;

            float elapsedTime = timeOffset;
            while (true)
            {
                elapsedTime += Time.deltaTime;
                progress = elapsedTime / time;

                if (elapsedTime >= time)
                    break;

                if (breakCondition != null && breakCondition())
                    Kill();

                yield return Timing.WaitForOneFrame;
            }

            progress = 1f;

            isWorking = false;

            if (onDone != null)
                onDone();
        }

        private IEnumerator<float> IE_RunSimple()
        {
            isWorking = true;

            if (onStart != null)
                onStart();

            progress = 0;

            if (breakCondition != null && breakCondition())
            {
                if (onBreak != null) onBreak();
                Kill();
            }

            if (time == 0)
                yield return Timing.WaitForOneFrame;
            else
                yield return Timing.WaitForSeconds(time);

            if (breakCondition != null && breakCondition())
            {
                if (onBreak != null) onBreak();
                Kill();
            }

            progress = 1f;

            isWorking = false;

            if (onDone != null)
                onDone();
        }

        public void Kill()
        {
            Timing.KillCoroutines(mCoroutine);
            isWorking = false;
        }
#endif
    }
}
