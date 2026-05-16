using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RevCore.UI
{
    /// <summary>
    /// Extended <see cref="ToggleGroup"/> that animates a shared "selector" background to the rect of
    /// the active toggle — common in segmented controls and tab bars. With DOTWEEN, the move tweens
    /// over <paramref name="tweenDuration"/>; otherwise snaps via a short coroutine.
    /// </summary>
    public class CustomToggleGroup : ToggleGroup
    {
        /// <summary>The selector graphic that follows the active toggle.</summary>
        public RectTransform dynamicBackground;
        /// <summary>Match the selector's height to the active toggle's height.</summary>
        public bool resizeY = true;
        /// <summary>Match the selector's width to the active toggle's width.</summary>
        public bool resizeX = true;

        /// <summary>
        /// Animates <see cref="dynamicBackground"/> to <paramref name="target"/>'s anchored position and
        /// size. <paramref name="tweenDuration"/> = 0 (or out-of-play-mode) snaps; non-zero tweens
        /// when DOTWEEN is available.
        /// </summary>
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

        /// <summary>Bulk-sets <see cref="Selectable.interactable"/> on every toggle in the group.</summary>
        public void SetToggleInteractable(bool value)
        {
            foreach (var toggle in m_Toggles)
                if (toggle != null)
                    toggle.interactable = value;
        }
    }
}
