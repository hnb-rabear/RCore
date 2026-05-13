using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace RevCore.UI.Editor
{
    [CustomEditor(typeof(JustToggle), true)]
    public class JustToggleEditor : ToggleEditor
    {
        private JustToggle m_toggle;
        private SerializedProperty imgBackgroundProp;
        private SerializedProperty txtLabelProp;
        private SerializedProperty contentsActiveProp;
        private SerializedProperty contentsInactiveProp;
        private SerializedProperty sfxClipProp;
        private SerializedProperty sfxClipOffProp;
        private SerializedProperty scaleBounceEffectProp;
        private SerializedProperty enableSizeSwitchProp;
        private SerializedProperty sizeActiveProp;
        private SerializedProperty sizeInactiveProp;
        private SerializedProperty tweenTimeProp;
        private SerializedProperty sizeTransitionsProp;
        private SerializedProperty positionTransitionsProp;
        private SerializedProperty colorTransitionsProp;
        private SerializedProperty spriteTransitionsProp;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_toggle = (JustToggle)target;

            imgBackgroundProp = serializedObject.FindProperty(nameof(JustToggle.imgBackground));
            txtLabelProp = serializedObject.FindProperty(nameof(JustToggle.txtLabel));
            contentsActiveProp = serializedObject.FindProperty(nameof(JustToggle.contentsActive));
            contentsInactiveProp = serializedObject.FindProperty(nameof(JustToggle.contentsInactive));
            sfxClipProp = serializedObject.FindProperty(nameof(JustToggle.sfxClip));
            sfxClipOffProp = serializedObject.FindProperty(nameof(JustToggle.sfxClipOff));
            scaleBounceEffectProp = serializedObject.FindProperty(nameof(JustToggle.scaleBounceEffect));
            enableSizeSwitchProp = serializedObject.FindProperty(nameof(JustToggle.enableSizeSwitch));
            sizeActiveProp = serializedObject.FindProperty(nameof(JustToggle.sizeActive));
            sizeInactiveProp = serializedObject.FindProperty(nameof(JustToggle.sizeInactive));
            tweenTimeProp = serializedObject.FindProperty(nameof(JustToggle.tweenTime));
            sizeTransitionsProp = serializedObject.FindProperty(nameof(JustToggle.sizeTransitions));
            positionTransitionsProp = serializedObject.FindProperty(nameof(JustToggle.positionTransitions));
            colorTransitionsProp = serializedObject.FindProperty(nameof(JustToggle.colorTransitions));
            spriteTransitionsProp = serializedObject.FindProperty(nameof(JustToggle.spriteTransitions));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("JustToggle Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(imgBackgroundProp);
            EditorGUILayout.PropertyField(txtLabelProp);
            EditorGUILayout.PropertyField(contentsActiveProp);
            EditorGUILayout.PropertyField(contentsInactiveProp);
            EditorGUILayout.PropertyField(sfxClipProp);
            EditorGUILayout.PropertyField(sfxClipOffProp);
            EditorGUILayout.PropertyField(scaleBounceEffectProp);

            EditorGUILayout.PropertyField(enableSizeSwitchProp);
            if (enableSizeSwitchProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(sizeActiveProp);
                EditorGUILayout.PropertyField(sizeInactiveProp);
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tweenTimeProp);
            EditorGUILayout.PropertyField(sizeTransitionsProp);
            EditorGUILayout.PropertyField(positionTransitionsProp);
            EditorGUILayout.PropertyField(colorTransitionsProp);
            EditorGUILayout.PropertyField(spriteTransitionsProp);

            if (m_toggle.txtLabel != null)
            {
                EditorGUI.BeginChangeCheck();
                string newLabelText = EditorGUILayout.TextField("Label", m_toggle.txtLabel.text);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_toggle.txtLabel, "Change Label Text");
                    m_toggle.txtLabel.text = newLabelText;
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Toggle Properties", EditorStyles.boldLabel);
            base.OnInspectorGUI();
        }
    }
}
