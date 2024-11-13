/***
 * Author RaBear - HNB - 2019
 **/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using RCore.Editor;
#endif

namespace RCore.UI
{
    [AddComponentMenu("RCore/UI/CustomToggleSlider")]
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

        public TapFeedback tapFeedback = TapFeedback.Haptic;
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
        }

        private void OnValueChanged(bool pIsOn)
        {
			if (m_clicked)
			{
				if (tapFeedback == TapFeedback.Haptic || tapFeedback == TapFeedback.SoundAndHaptic)
					Vibration.VibratePop();
				if (tapFeedback == TapFeedback.Sound || tapFeedback == TapFeedback.SoundAndHaptic)
				{
					if (isOn && !string.IsNullOrEmpty(sfxClip))
						EventDispatcher.Raise(new Audio.SFXTriggeredEvent(sfxClip));
					else if (!isOn && !string.IsNullOrEmpty(sfxClipOff))
						EventDispatcher.Raise(new Audio.SFXTriggeredEvent(sfxClipOff));
				}
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
            if (enableOnOffColor)
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

            if (enableOnOffColor)
            {
                var targetImg = toggleTransform.GetComponent<Image>();
                if (targetImg != null)
                    targetImg.color = isOn ? onColor : offColor;
            }
        }
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(CustomToggleSlider), true)]
    public class CustomToggleEditor : UnityEditor.UI.ToggleEditor
    {
        private CustomToggleSlider m_toggle;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_toggle = (CustomToggleSlider)target;
        }

        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.BeginVertical("box");
            {
                serializedObject.SerializeField("txtLabel");
                serializedObject.SerializeField("toggleTransform");
                serializedObject.SerializeField("onPosition");
                serializedObject.SerializeField("offPosition");
                serializedObject.SerializeField("tapFeedback");
                serializedObject.SerializeField("sfxClip");
                serializedObject.SerializeField("sfxClipOff");

                var property1 = serializedObject.SerializeField("enableOnOffContent");
                if (property1.boolValue)
                {
                    UnityEditor.EditorGUI.indentLevel++;
                    UnityEditor.EditorGUILayout.BeginVertical("box");
                    serializedObject.SerializeField("onObjects");
                    serializedObject.SerializeField("offObjects");
                    UnityEditor.EditorGUILayout.EndVertical();
                    UnityEditor.EditorGUI.indentLevel--;
                }
                var property2 = serializedObject.SerializeField("enableOnOffColor");
                if (property2.boolValue)
                {
                    UnityEditor.EditorGUI.indentLevel++;
                    UnityEditor.EditorGUILayout.BeginVertical("box");
                    serializedObject.SerializeField("onColor");
                    serializedObject.SerializeField("offColor");
                    UnityEditor.EditorGUILayout.EndVertical();
                    UnityEditor.EditorGUI.indentLevel--;
                }

                if (m_toggle.txtLabel != null)
                    m_toggle.txtLabel.text = UnityEditor.EditorGUILayout.TextField("Label", m_toggle.txtLabel.text);

                serializedObject.ApplyModifiedProperties();
            }
            UnityEditor.EditorGUILayout.EndVertical();

            base.OnInspectorGUI();
        }
    }
#endif
}