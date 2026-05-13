using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SimpleTMPButton), true)]
    public class SimpleTMPButtonEditor : JustButtonEditor
    {
        private SimpleTMPButton m_target;
        private string[] m_matsName = Array.Empty<string>();
        private Material[] m_labelMats = Array.Empty<Material>();

        protected override void OnEnable()
        {
            base.OnEnable();

            m_target = (SimpleTMPButton)target;
            RefreshMaterials();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginVertical("box");
            serializedObject.Update();
            var fontColorSwap = serializedObject.FindProperty(nameof(SimpleTMPButton.fontColorOnOffSwap));
            EditorGUILayout.PropertyField(fontColorSwap);
            if (fontColorSwap.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SimpleTMPButton.fontColorOn)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SimpleTMPButton.fontColorOff)));
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            var labelMatSwap = serializedObject.FindProperty(nameof(SimpleTMPButton.labelMatOnOffSwap));
            EditorGUILayout.PropertyField(labelMatSwap);
            if (labelMatSwap.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");
                DrawMaterialPopup(nameof(SimpleTMPButton.labelMatOn), "Active Mat");
                DrawMaterialPopup(nameof(SimpleTMPButton.labelMatOff), "Inactive Mat");
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            var labelProp = serializedObject.FindProperty(nameof(SimpleTMPButton.label));
            EditorGUILayout.PropertyField(labelProp);
            if (labelProp.objectReferenceValue is TextMeshProUGUI text)
            {
                var textObj = new SerializedObject(text);
                textObj.Update();
                EditorGUILayout.PropertyField(textObj.FindProperty("m_text"));
                textObj.ApplyModifiedProperties();
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawMaterialPopup(string propertyName, string label)
        {
            var prop = serializedObject.FindProperty(propertyName);
            int index = prop.objectReferenceValue == null
                ? -1
                : Array.IndexOf(m_labelMats, prop.objectReferenceValue as Material);
            int selected = EditorGUILayout.Popup(label, index, m_matsName);
            prop.objectReferenceValue = selected >= 0 && selected < m_labelMats.Length ? m_labelMats[selected] : null;
        }

        private void RefreshMaterials()
        {
            m_matsName = Array.Empty<string>();
            m_labelMats = Array.Empty<Material>();
            if (m_target.Label == null || m_target.Label.font == null)
                return;

            string fontPath = AssetDatabase.GetAssetPath(m_target.Label.font);
            if (string.IsNullOrEmpty(fontPath))
                return;

            m_labelMats = AssetDatabase.LoadAllAssetsAtPath(fontPath).OfType<Material>().ToArray();
            m_matsName = m_labelMats.Select(mat => mat.name).ToArray();
        }

        [MenuItem("GameObject/RevCore/UI/Replace Button By SimpleTMPButton")]
        private static void ReplaceButton()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var buttons = gameObjects[i].GetComponentsInChildren<Button>(true);
                for (int j = 0; j < buttons.Length; j++)
                {
                    var btn = buttons[j];
                    if (btn is SimpleTMPButton)
                        continue;

                    var go = btn.gameObject;
                    var onClick = btn.onClick;
                    var enabled = btn.enabled;
                    var interactable = btn.interactable;
                    var transition = btn.transition;
                    var targetGraphic = btn.targetGraphic;
                    var colors = btn.colors;
                    DestroyImmediate(btn);
                    var newBtn = go.AddComponent<SimpleTMPButton>();
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
