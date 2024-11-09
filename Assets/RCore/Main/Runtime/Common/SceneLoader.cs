/***
 * Author RaBear - HNB - 2017 - 2019
 **/

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RCore
{
    public class SceneLoader
    {
        public static AsyncOperation LoadSceneAsync(string pScene, bool pIsAdditive, bool pAutoActive, Action<float> pOnProgress, Action pOnCompleted, float pFixedLoadTime = 0, bool reLoad = false)
        {
            var scene = SceneManager.GetSceneByName(pScene);
            if (scene.isLoaded && !reLoad)
            {
                pOnProgress?.Invoke(1);
                pOnCompleted?.Invoke();
                return null;
            }

            var sceneOperator = SceneManager.LoadSceneAsync(pScene, pIsAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single);
            sceneOperator.allowSceneActivation = false;
            TimerEventsGlobal.Instance.StartCoroutine(IEProcessOperation(sceneOperator, pAutoActive, pOnProgress, pOnCompleted, pFixedLoadTime));
            return sceneOperator;
        }

        public static void LoadScene(string pScene, bool pIsAdditive, bool pReload = false)
        {
            var scene = SceneManager.GetSceneByName(pScene);
            if (scene.isLoaded && !pReload)
                return;
            SceneManager.LoadScene(pScene, pIsAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single);
        }

        private static IEnumerator IEProcessOperation(AsyncOperation sceneOperator, bool pAutoActive, Action<float> pOnProgress, Action pOnCompleted, float pFixedLoadTime = 0)
        {
            pOnProgress?.Invoke(0f);

            float startTime = Time.unscaledTime;
            float fakeProgress = 0.25f;
            float offsetProgress = pFixedLoadTime > 0 ? fakeProgress : 0;

            while (true)
            {
                float progress = Mathf.Clamp01(sceneOperator.progress / 0.9f);
                pOnProgress?.Invoke(progress - offsetProgress);
                yield return null;

                if (sceneOperator.isDone || progress >= 1)
                    break;
            }

            float loadTime = Time.unscaledTime - startTime;
            float additionalTime = pFixedLoadTime - loadTime;
            if (additionalTime <= 0)
                pOnProgress?.Invoke(1);
            else
            {
                float time = 0;
                while (true)
                {
                    time += Time.deltaTime;
                    if (time > additionalTime)
                        break;

                    float progress = 1 - fakeProgress + time / additionalTime * fakeProgress;
                    pOnProgress?.Invoke(progress);
                    yield return null;
                }
                pOnProgress?.Invoke(1);
            }

            pOnCompleted?.Invoke();

            if (pAutoActive)
                sceneOperator.allowSceneActivation = true;
        }

        public static AsyncOperation UnloadSceneAsync(string pScene, Action<float> pOnProgress, Action pOnCompleted)
        {
            var scene = SceneManager.GetSceneByName(pScene);
            if (!scene.isLoaded)
            {
                pOnProgress(1f);
                return null;
            }

            var sceneOperator = SceneManager.UnloadSceneAsync(pScene);
            TimerEventsGlobal.Instance.StartCoroutine(IEProcessOperation(sceneOperator, false, pOnProgress, pOnCompleted));
            return sceneOperator;
        }

        public static AsyncOperation UnloadScene(Scene pScene, Action<float> pOnProgress, Action pOnCompleted)
        {
            if (!pScene.isLoaded)
            {
                pOnProgress(1f);
                return null;
            }

            var sceneOperator = SceneManager.UnloadSceneAsync(pScene);
            TimerEventsGlobal.Instance.StartCoroutine(IEProcessOperation(sceneOperator, false, pOnProgress, pOnCompleted));
            return sceneOperator;
        }
    }
}