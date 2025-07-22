using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore.UI
{
    /// <summary>
    /// Extends the standard Unity ToggleGroup to include a dynamic background element.
    /// This background can move and resize to visually highlight the currently selected toggle,
    /// with optional smooth animations using DOTween.
    /// </summary>
    public class CustomToggleGroup : ToggleGroup
    {
        /// <summary>
        /// The RectTransform of the UI element that will serve as the dynamic background or selector.
        /// This element will be moved and resized to match the currently active toggle.
        /// </summary>
        [Tooltip("The RectTransform of the UI element that will serve as the dynamic background or selector.")]
        public RectTransform dynamicBackground;

        /// <summary>
        /// If true, the dynamic background will resize its width to match the selected toggle.
        /// </summary>
        [Tooltip("If true, the dynamic background will resize its width to match the selected toggle.")]
        public bool resizeY = true;

        /// <summary>
        /// If true, the dynamic background will resize its height to match the selected toggle.
        /// </summary>
        [Tooltip("If true, the dynamic background will resize its height to match the selected toggle.")]
        public bool resizeX = true;

        /// <summary>
        /// Moves and resizes the dynamic background to align with a specified target toggle's RectTransform.
        /// Can perform the transition instantly or as a smooth animation over a given duration.
        /// </summary>
        /// <param name="pTarget">The RectTransform of the toggle to move the background to.</param>
        /// <param name="pTweenDuration">The duration of the animation in seconds. If 0, the change is instant. Requires DOTween for animation.</param>
        public void SetTarget(RectTransform pTarget, float pTweenDuration = 0)
        {
            if (dynamicBackground == null)
                return;
            
            // Store the starting properties for the tween
            float lerp = 0;
            var oldPos = dynamicBackground.anchoredPosition;
            var oldSize = dynamicBackground.sizeDelta;
            
            // If not in play mode or no tween duration is set, update the target immediately via coroutine.
            if (!Application.isPlaying || pTweenDuration == 0)
            {
                StartCoroutine(IESetTarget(pTarget));
                return;
            }

#if DOTWEEN
            // Use DOTween for a smooth animated transition
            DOTween.Kill(GetInstanceID()); // Kill any previous tweens on this object
            DOTween.To(() => lerp, x => lerp = x, 1f, pTweenDuration)
                .OnUpdate(() =>
                {
                    // Interpolate position and size during the tween
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
                    // Ensure the final position and size are set precisely upon completion
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
            // Fallback to the coroutine if DOTween is not available
            StartCoroutine(IESetTarget(pTarget));
#endif
        }

        /// <summary>
        /// A coroutine that sets the dynamic background's transform to match the target.
        /// This is used as a fallback when DOTween is not available or an instant transition is desired.
        /// It runs for a few frames to help ensure UI layout updates correctly.
        /// </summary>
        /// <param name="pTarget">The RectTransform of the target toggle.</param>
        private IEnumerator IESetTarget(RectTransform pTarget)
        {
            // This loop ensures the position is set correctly after layout updates, running for a short duration.
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
        
        /// <summary>
        /// Enables or disables the 'interactable' property for all toggles registered with this group.
        /// This is useful for preventing user input during transitions.
        /// </summary>
        /// <param name="value">True to make all toggles interactable, false to disable them.</param>
        public void SetToggleInteractable(bool value)
        {
            // Note: m_Toggles is a protected field from the base ToggleGroup class.
            foreach (var toggle in m_Toggles)
            {
                if (toggle != null)
                    toggle.interactable = value;
            }
        }
    }
}