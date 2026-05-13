using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RevCore.UI
{
    public class CustomToggleGroup : ToggleGroup
    {
        public RectTransform dynamicBackground;
        public bool resizeY = true;
        public bool resizeX = true;

        public void SetTarget(RectTransform target, float tweenDuration = 0)
        {
            if (dynamicBackground == null)
                return;

            float lerp = 0;
            var oldPos = dynamicBackground.anchoredPosition;
            var oldSize = dynamicBackground.sizeDelta;

            if (!Application.isPlaying || tweenDuration == 0)
            {
                StartCoroutine(IESetTarget(target));
                return;
            }

#if DOTWEEN
            DOTween.Kill(GetInstanceID());
            DOTween.To(() => lerp, value => lerp = value, 1f, tweenDuration)
                .OnUpdate(() =>
                {
                    dynamicBackground.anchoredPosition = Vector2.Lerp(oldPos, target.anchoredPosition, lerp);

                    var size = Vector2.Lerp(oldSize, target.sizeDelta, lerp);
                    if (resizeX && resizeY)
                        dynamicBackground.sizeDelta = size;
                    else if (resizeX)
                        dynamicBackground.sizeDelta = new Vector2(size.x, dynamicBackground.sizeDelta.y);
                    else if (resizeY)
                        dynamicBackground.sizeDelta = new Vector2(dynamicBackground.sizeDelta.x, size.y);
                })
                .OnComplete(() =>
                {
                    dynamicBackground.anchoredPosition = target.anchoredPosition;
                    if (resizeX && resizeY)
                        dynamicBackground.sizeDelta = target.sizeDelta;
                    else if (resizeX)
                        dynamicBackground.sizeDelta = new Vector2(target.sizeDelta.x, dynamicBackground.sizeDelta.y);
                    else if (resizeY)
                        dynamicBackground.sizeDelta = new Vector2(dynamicBackground.sizeDelta.x, target.sizeDelta.y);
                })
                .SetEase(Ease.OutCubic)
                .SetId(GetInstanceID());
#else
            StartCoroutine(IESetTarget(target));
#endif
        }

        private IEnumerator IESetTarget(RectTransform target)
        {
            float time = 0.2f;
            while (time > 0)
            {
                dynamicBackground.anchoredPosition = target.anchoredPosition;
                if (resizeX && resizeY)
                    dynamicBackground.sizeDelta = target.sizeDelta;
                else if (resizeX)
                    dynamicBackground.sizeDelta = new Vector2(target.sizeDelta.x, dynamicBackground.sizeDelta.y);
                else if (resizeY)
                    dynamicBackground.sizeDelta = new Vector2(dynamicBackground.sizeDelta.x, target.sizeDelta.y);
                time -= Time.deltaTime;
                yield return null;
            }
        }

        public void SetToggleInteractable(bool value)
        {
            foreach (var toggle in m_Toggles)
                if (toggle != null)
                    toggle.interactable = value;
        }
    }
}
