using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RevCore.UI
{
    /// <summary>
    /// Slider-style toggle (a "handle" that moves between two anchored positions). Deprecated in favor
    /// of <see cref="JustToggle"/>'s richer transition system; kept for prefabs that have not migrated.
    /// </summary>
    [Obsolete("Use JustToggle instead")]
    [AddComponentMenu("RevCore/UI/CustomToggleSlider")]
    public class CustomToggleSlider : Toggle
    {
        /// <summary>Optional label.</summary>
        public TextMeshProUGUI txtLabel;

        /// <summary>The sliding marker. Moves between <see cref="onPosition"/> and <see cref="offPosition"/>.</summary>
        [Tooltip("Marker which move to On/Off position")]
        public RectTransform toggleTransform;
        /// <summary>Anchored position of the marker when on.</summary>
        [Tooltip("Position that marker move to when toggle is on")]
        public Vector2 onPosition;
        /// <summary>Anchored position of the marker when off.</summary>
        [Tooltip("Position that marker move to when toggle is off")]
        public Vector2 offPosition;

        /// <summary>Enable show/hide of <see cref="onObjects"/>/<see cref="offObjects"/> based on state.</summary>
        public bool enableOnOffContent;
        /// <summary>Objects visible while the toggle is on.</summary>
        [Tooltip("Objects which active when toggle is on")]
        public GameObject[] onObjects;
        /// <summary>Objects visible while the toggle is off.</summary>
        [Tooltip("Objects which active when toggle is off")]
        public GameObject[] offObjects;

        /// <summary>Enable color swap of the marker image based on state.</summary>
        public bool enableOnOffColor;
        /// <summary>Marker color when on.</summary>
        public Color onColor;
        /// <summary>Marker color when off.</summary>
        public Color offColor;

        /// <summary>SFX clip played when toggling on.</summary>
        public string sfxClip = "button";
        /// <summary>SFX clip played when toggling off.</summary>
        public string sfxClipOff = "button";

        private bool m_clicked;

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
            onValueChanged.AddListener(OnValueChanged);
        }

        protected override void OnDisable()
        {
            onValueChanged.RemoveListener(OnValueChanged);
            base.OnDisable();
        }

        private void OnValueChanged(bool value)
        {
            if (m_clicked)
            {
                if (isOn && !string.IsNullOrEmpty(sfxClip))
                    Events.Publish(new UISfxTriggeredEvent(sfxClip));
                else if (!isOn && !string.IsNullOrEmpty(sfxClipOff))
                    Events.Publish(new UISfxTriggeredEvent(sfxClipOff));
                m_clicked = false;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (enableOnOffContent)
            {
                if (onObjects != null)
                    foreach (var onObject in onObjects)
                        onObject.SetActive(isOn);
                if (offObjects != null)
                    foreach (var offObject in offObjects)
                        offObject.SetActive(!isOn);
            }
            if (toggleTransform != null)
                toggleTransform.anchoredPosition = isOn ? onPosition : offPosition;
            if (enableOnOffColor && toggleTransform != null)
            {
                var targetImg = toggleTransform.GetComponent<Image>();
                if (targetImg != null)
                    targetImg.color = isOn ? onColor : offColor;
            }
        }

        /// <inheritdoc />
        public override void OnPointerClick(PointerEventData eventData)
        {
            m_clicked = true;
            base.OnPointerClick(eventData);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (txtLabel == null)
                txtLabel = gameObject.GetComponentInChildren<TextMeshProUGUI>();

            if (graphic != null)
                graphic.gameObject.SetActive(isOn);

            if (toggleTransform != null)
                toggleTransform.anchoredPosition = isOn ? onPosition : offPosition;

            if (enableOnOffContent)
            {
                if (onObjects != null)
                    foreach (var onObject in onObjects)
                        onObject.SetActive(isOn);
                if (offObjects != null)
                    foreach (var offObject in offObjects)
                        offObject.SetActive(!isOn);
            }

            if (targetGraphic == null)
            {
                var images = GetComponentsInChildren<Image>();
                if (images.Length > 0)
                    targetGraphic = images[0];
            }

            if (enableOnOffColor && toggleTransform != null)
            {
                var targetImg = toggleTransform.GetComponent<Image>();
                if (targetImg != null)
                    targetImg.color = isOn ? onColor : offColor;
            }
        }
#endif
    }
}
