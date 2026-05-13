using UnityEditor;
using UnityEditor.UI;

namespace RevCore.UI.Editor
{
    [CustomEditor(typeof(CustomToggleSlider), true)]
    public class CustomToggleSliderEditor : ToggleEditor
    {
        private CustomToggleSlider m_toggle;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_toggle = (CustomToggleSlider)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.txtLabel)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.toggleTransform)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.onPosition)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.offPosition)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.sfxClip)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.sfxClipOff)));

            var enableContent = serializedObject.FindProperty(nameof(CustomToggleSlider.enableOnOffContent));
            EditorGUILayout.PropertyField(enableContent);
            if (enableContent.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.onObjects)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.offObjects)));
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            var enableColor = serializedObject.FindProperty(nameof(CustomToggleSlider.enableOnOffColor));
            EditorGUILayout.PropertyField(enableColor);
            if (enableColor.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.onColor)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CustomToggleSlider.offColor)));
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            if (m_toggle.txtLabel != null)
                m_toggle.txtLabel.text = EditorGUILayout.TextField("Label", m_toggle.txtLabel.text);

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();

            base.OnInspectorGUI();
        }
    }
}
