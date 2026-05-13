using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RevCore.UI
{
    [Obsolete("Use JustToggle instead")]
    [AddComponentMenu("RevCore/UI/CustomToggleSlider")]
    public class CustomToggleSlider : Toggle
    {
        public TextMeshProUGUI txtLabel;

        [Tooltip("Marker which move to On/Off position")]
        public RectTransform toggleTransform;
        [Tooltip("Position that marker move to when toggle is on")]
        public Vector2 onPosition;
        [Tooltip("Position that marker move to when toggle is off")]
        public Vector2 offPosition;

        public bool enableOnOffContent;
        [Tooltip("Objects which active when toggle is on")]
        public GameObject[] onObjects;
        [Tooltip("Objects which active when toggle is off")]
        public GameObject[] offObjects;

        public bool enableOnOffColor;
        public Color onColor;
        public Color offColor;

        public string sfxClip = "button";
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
