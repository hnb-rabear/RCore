/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        public string sfxClip = "button";
        public string sfxClipOff = "button";

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
            if (pIsOn && !string.IsNullOrEmpty(sfxClip))
                EventDispatcher.Raise(new Audio.SFXTriggeredEvent(sfxClip));
            else if (!pIsOn && !string.IsNullOrEmpty(sfxClipOff))
                EventDispatcher.Raise(new Audio.SFXTriggeredEvent(sfxClipOff));

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
    class CustomToggleEditor : UnityEditor.UI.ToggleEditor
    {
        private CustomToggleSlider mToggle;

        protected override void OnEnable()
        {
            base.OnEnable();

            mToggle = (CustomToggleSlider)target;
        }

        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.BeginVertical("box");
            {
                EditorHelper.SerializeField(serializedObject, "txtLabel");
                EditorHelper.SerializeField(serializedObject, "onPosition");
                EditorHelper.SerializeField(serializedObject, "offPosition");
                EditorHelper.SerializeField(serializedObject, "toggleTransform");
                EditorHelper.SerializeField(serializedObject, "sfxClip");

                var property1 = EditorHelper.SerializeField(serializedObject, "enableOnOffContent");
                if (property1.boolValue)
                {
                    UnityEditor.EditorGUI.indentLevel++;
                    UnityEditor.EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "onObjects");
                    EditorHelper.SerializeField(serializedObject, "offObjects");
                    UnityEditor.EditorGUILayout.EndVertical();
                    UnityEditor.EditorGUI.indentLevel--;
                }
                var property2 = EditorHelper.SerializeField(serializedObject, "enableOnOffColor");
                if (property2.boolValue)
                {
                    UnityEditor.EditorGUI.indentLevel++;
                    UnityEditor.EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "onColor");
                    EditorHelper.SerializeField(serializedObject, "offColor");
                    UnityEditor.EditorGUILayout.EndVertical();
                    UnityEditor.EditorGUI.indentLevel--;
                }
                EditorHelper.SerializeField(serializedObject, "customTargetGraphic");
                EditorHelper.SerializeField(serializedObject, "m_TargetGraphic");

                if (mToggle.txtLabel != null)
                    mToggle.txtLabel.text = UnityEditor.EditorGUILayout.TextField("Label", mToggle.txtLabel.text);

                serializedObject.ApplyModifiedProperties();
            }
            UnityEditor.EditorGUILayout.EndVertical();

            base.OnInspectorGUI();
        }
    }
#endif
}