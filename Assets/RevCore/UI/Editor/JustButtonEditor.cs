using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

namespace RevCore.UI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(JustButton), true)]
    public class JustButtonEditor : ButtonEditor
    {
        private JustButton m_target;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_target = (JustButton)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("JustButton Properties", EditorStyles.boldLabel);

            m_target.CheckPerfectRatio();
            EditorGUILayout.BeginVertical("box");
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_img"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JustButton.scaleBounceEffect)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JustButton.greyscaleEffect)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JustButton.clickSfx)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JustButton.perfectRatio)));

            var imgSwapEnabled = serializedObject.FindProperty(nameof(JustButton.imgOnOffSwap));
            EditorGUILayout.PropertyField(imgSwapEnabled);
            if (imgSwapEnabled.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JustButton.imgOn)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JustButton.imgOff)));
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        [MenuItem("GameObject/RevCore/UI/Replace Button By JustButton")]
        private static void ReplaceButton()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var buttons = gameObjects[i].GetComponentsInChildren<Button>(true);
                for (int j = 0; j < buttons.Length; j++)
                {
                    var btn = buttons[j];
                    if (btn is JustButton)
                        continue;

                    var go = btn.gameObject;
                    var onClick = btn.onClick;
                    var enabled = btn.enabled;
                    var interactable = btn.interactable;
                    var transition = btn.transition;
                    var targetGraphic = btn.targetGraphic;
                    var colors = btn.colors;
                    DestroyImmediate(btn);
                    var newBtn = go.AddComponent<JustButton>();
                    newBtn.onClick = onClick;
                    newBtn.enabled = enabled;
                    newBtn.interactable = interactable;
                    newBtn.transition = transition;
                    newBtn.targetGraphic = targetGraphic;
                    newBtn.colors = colors;
                    EditorUtility.SetDirty(go);
                }
            }
        }
    }
}
