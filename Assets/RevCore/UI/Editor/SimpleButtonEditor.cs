using UnityEditor;
using UnityEngine.UI;

namespace RevCore.UI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SimpleButton), true)]
    public class SimpleButtonEditor : JustButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SimpleButton Properties", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            serializedObject.Update();
            var labelProp = serializedObject.FindProperty(nameof(SimpleButton.label));
            EditorGUILayout.PropertyField(labelProp);
            if (labelProp.objectReferenceValue is Text text)
            {
                var textObj = new SerializedObject(text);
                textObj.Update();
                EditorGUILayout.PropertyField(textObj.FindProperty("m_Text"));
                textObj.ApplyModifiedProperties();
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        [MenuItem("GameObject/RevCore/UI/Replace Button By SimpleButton")]
        private static void ReplaceButton()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var buttons = gameObjects[i].GetComponentsInChildren<Button>(true);
                for (int j = 0; j < buttons.Length; j++)
                {
                    var btn = buttons[j];
                    if (btn is SimpleButton)
                        continue;

                    var go = btn.gameObject;
                    var onClick = btn.onClick;
                    var enabled = btn.enabled;
                    var interactable = btn.interactable;
                    var transition = btn.transition;
                    var targetGraphic = btn.targetGraphic;
                    var colors = btn.colors;
                    DestroyImmediate(btn);
                    var newBtn = go.AddComponent<SimpleButton>();
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
