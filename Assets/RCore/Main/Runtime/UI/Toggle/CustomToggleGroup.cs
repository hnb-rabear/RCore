using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore.UI
{
    public class CustomToggleGroup : ToggleGroup
    {
        public RectTransform dynamicBackground;
        public bool resizeY = true;
        public bool resizeX = true;

        public void SetTarget(RectTransform pTarget, float pTweenDuration = 0)
        {
            if (dynamicBackground == null)
                return;
            
            float lerp = 0;
            var oldPos = dynamicBackground.anchoredPosition;
            var oldSize = dynamicBackground.sizeDelta;

            if (!Application.isPlaying || pTweenDuration == 0)
                StartCoroutine(IESetTarget(pTarget));

#if DOTWEEN
            DOTween.Kill(GetInstanceID());
            DOTween.To(() => lerp, x => lerp = x, 1f, pTweenDuration)
                .OnUpdate(() =>
                {
                    var pos = Vector2.Lerp(oldPos, pTarget.anchoredPosition, lerp);
                    dynamicBackground.anchoredPosition = pos;

                    var size = Vector2.Lerp(oldSize, pTarget.sizeDelta, lerp);
                    if (resizeX && resizeY)
                        dynamicBackground.sizeDelta = size;
                    else if (resizeX)
                        dynamicBackground.sizeDelta = new Vector2(size.x, dynamicBackground.sizeDelta.y);
                    else if (resizeY)
                        dynamicBackground.sizeDelta = new Vector2(dynamicBackground.sizeDelta.x, size.y);
                })
                .OnComplete(() =>
                {
                    dynamicBackground.anchoredPosition = pTarget.anchoredPosition;
                    if (resizeX && resizeY)
                        dynamicBackground.sizeDelta = pTarget.sizeDelta;
                    else if (resizeX)
                        dynamicBackground.sizeDelta = new Vector2(pTarget.sizeDelta.x, dynamicBackground.sizeDelta.y);
                    else if (resizeY)
                        dynamicBackground.sizeDelta = new Vector2(dynamicBackground.sizeDelta.x, pTarget.sizeDelta.y);
                })
                .SetEase(Ease.OutCubic)
                .SetId(GetInstanceID());
#else
            StartCoroutine(IESetTarget(pTarget));
#endif
        }

        private IEnumerator IESetTarget(RectTransform pTarget)
        {
            float time = 0.2f;
            while (time > 0)
            {
                dynamicBackground.anchoredPosition = pTarget.anchoredPosition;
                if (resizeX && resizeY)
                    dynamicBackground.sizeDelta = pTarget.sizeDelta;
                else if (resizeX)
                    dynamicBackground.sizeDelta = new Vector2(pTarget.sizeDelta.x, dynamicBackground.sizeDelta.y);
                else if (resizeY)
                    dynamicBackground.sizeDelta = new Vector2(dynamicBackground.sizeDelta.x, pTarget.sizeDelta.y);
                time -= Time.deltaTime;
                yield return null;
            }
        }

        public void SetToggleInteractable(bool value)
        {
            foreach (var toggle in m_Toggles)
                toggle.interactable = value;
        }
    }
}