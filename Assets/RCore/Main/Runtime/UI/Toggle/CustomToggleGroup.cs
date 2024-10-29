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
                    dynamicBackground.sizeDelta = size;
                })
                .OnComplete(() =>
                {
                    dynamicBackground.anchoredPosition = pTarget.anchoredPosition;
                    dynamicBackground.sizeDelta = pTarget.sizeDelta;
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
                dynamicBackground.sizeDelta = pTarget.sizeDelta;
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