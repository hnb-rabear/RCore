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
        [FormerlySerializedAs("toggleTransform")]
        [SerializeField] private RectTransform m_toggleTransform;
        [Tooltip("Position that marker move to when toggle is on")]
        [FormerlySerializedAs("onPosition")]
        [SerializeField] private Vector2 m_onPosition;
        [Tooltip("Position that marker move to when toggle is off")]
        [FormerlySerializedAs("offPosition")]
        [SerializeField] private Vector2 m_offPosition;

        [FormerlySerializedAs("enableOnOffContent")]
        [SerializeField] private bool m_enableOnOffContent;
        [Tooltip("Objects which active when toggle is on")]
        [FormerlySerializedAs("onObjects")]
        [SerializeField] private GameObject[] m_onObjects;
        [Tooltip("Objects which active when toggle is off")]
        [FormerlySerializedAs("offObjects")]
        [SerializeField] private GameObject[] m_offObjects;

        [FormerlySerializedAs("enableOnOffColor")]
        [SerializeField] private bool m_enableOnOffColor;
        [FormerlySerializedAs("onColor")]
        [SerializeField] private Color m_onColor;
        [FormerlySerializedAs("offColor")]
        [SerializeField] private Color m_offColor;
        
        [FormerlySerializedAs("sfxClip")]
        [SerializeField] private string m_sfxClip = "button";
        [FormerlySerializedAs("sfxClipOff")]
        [SerializeField] private string m_sfxClipOff = "button";
        [SerializeField] private bool m_hapticTouch;

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
            if (pIsOn && !string.IsNullOrEmpty(m_sfxClip))
                EventDispatcher.Raise(new Audio.SFXTriggeredEvent(m_sfxClip));
            else if (!pIsOn && !string.IsNullOrEmpty(m_sfxClipOff))
                EventDispatcher.Raise(new Audio.SFXTriggeredEvent(m_sfxClipOff));

            Refresh();
        }

        private void Refresh()
        {
            if (m_enableOnOffContent)
            {
                if (m_onObjects != null)
                    foreach (var onObject in m_onObjects)
                        onObject.SetActive(isOn);
                if (m_offObjects != null)
                    foreach (var offObject in m_offObjects)
                        offObject.SetActive(!isOn);
            }
            if (m_toggleTransform != null)
                m_toggleTransform.anchoredPosition = isOn ? m_onPosition : m_offPosition;
            if (m_enableOnOffColor)
            {
                var targetImg = m_toggleTransform.GetComponent<Image>();
                if (targetImg != null)
                    targetImg.color = isOn ? m_onColor : m_offColor;
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            
            if (m_hapticTouch)
                Vibration.VibratePop();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (txtLabel == null)
                txtLabel = gameObject.GetComponentInChildren<TextMeshProUGUI>();

            if (graphic != null)
                graphic.gameObject.SetActive(isOn);

            if (m_toggleTransform != null)
                m_toggleTransform.anchoredPosition = isOn ? m_onPosition : m_offPosition;

            if (m_enableOnOffContent)
            {
                if (m_onObjects != null)
                    foreach (var onObject in m_onObjects)
                        onObject.SetActive(isOn);
                if (m_offObjects != null)
                    foreach (var offObject in m_offObjects)
                        offObject.SetActive(!isOn);
            }

            if (targetGraphic == null)
            {
                var images = GetComponentsInChildren<Image>();
                if (images.Length > 0)
                    targetGraphic = images[0];
            }

            if (m_enableOnOffColor)
            {
                var targetImg = m_toggleTransform.GetComponent<Image>();
                if (targetImg != null)
                    targetImg.color = isOn ? m_onColor : m_offColor;
            }
        }
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(CustomToggleSlider), true)]
    class CustomToggleEditor : UnityEditor.UI.ToggleEditor
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
                serializedObject.SerializeField("m_txtLabel");
                serializedObject.SerializeField("m_toggleTransform");
                serializedObject.SerializeField("m_onPosition");
                serializedObject.SerializeField("m_offPosition");
                serializedObject.SerializeField("m_sfxClip");
                serializedObject.SerializeField("m_sfxClipOff");
                serializedObject.SerializeField("m_hapticTouch");

                var property1 = serializedObject.SerializeField("m_enableOnOffContent");
                if (property1.boolValue)
                {
                    UnityEditor.EditorGUI.indentLevel++;
                    UnityEditor.EditorGUILayout.BeginVertical("box");
                    serializedObject.SerializeField("m_onObjects");
                    serializedObject.SerializeField("m_offObjects");
                    UnityEditor.EditorGUILayout.EndVertical();
                    UnityEditor.EditorGUI.indentLevel--;
                }
                var property2 = serializedObject.SerializeField("m_enableOnOffColor");
                if (property2.boolValue)
                {
                    UnityEditor.EditorGUI.indentLevel++;
                    UnityEditor.EditorGUILayout.BeginVertical("box");
                    serializedObject.SerializeField("m_onColor");
                    serializedObject.SerializeField("m_offColor");
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