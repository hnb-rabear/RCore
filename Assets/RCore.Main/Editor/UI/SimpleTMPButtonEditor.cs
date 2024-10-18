using RCore.Common;
using RCore.UI;
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SimpleTMPButton), true)]
    public class SimpleTMPButtonEditor : JustButton.JustButtonEditor
    {
        private SimpleTMPButton m_target;
        private string[] m_matsName;
        private Material[] m_labelMats;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_target = (SimpleTMPButton)target;
            m_matsName = Array.Empty<string>();
            m_labelMats = Array.Empty<Material>();
            if (m_target.Label != null)
            {
                m_labelMats = TMPro.EditorUtilities.TMP_EditorUtility.FindMaterialReferences(m_target.Label.font);
                m_matsName = new string[m_labelMats.Length];
                for (int i = 0; i < m_labelMats.Length; i++)
                    m_matsName[i] = m_labelMats[i].name;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginVertical("box");
            {
                var fontColorSwap = EditorHelper.SerializeField(serializedObject, "m_fontColorOnOffSwap");
                if (fontColorSwap.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "m_fontColorOn");
                    EditorHelper.SerializeField(serializedObject, "m_fontColorOff");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }

                var labelMatSwap = EditorHelper.SerializeField(serializedObject, "m_labelMatOnOffSwap");
                if (labelMatSwap.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    var labelMatActiveName = EditorHelper.DropdownList(m_target.m_labelMatOn ? m_target.m_labelMatOn.name : "", "Active Mat", m_matsName.ToArray());
                    m_target.m_labelMatOn = m_matsName.Contains(labelMatActiveName) ? m_labelMats[m_matsName.IndexOf(labelMatActiveName)] : null;
                    var labelMatInactiveName = EditorHelper.DropdownList(m_target.m_labelMatOff ? m_target.m_labelMatOff.name : "", "Inactive Mat", m_matsName.ToArray());
                    m_target.m_labelMatOff = m_matsName.Contains(labelMatInactiveName) ? m_labelMats[m_matsName.IndexOf(labelMatInactiveName)] : null;
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }

                var label = EditorHelper.SerializeField(serializedObject, "m_label");
                var text = label.objectReferenceValue as TextMeshProUGUI;
                if (text != null)
                {
                    var textObj = new SerializedObject(text);
                    EditorHelper.SerializeField(textObj, "m_text");

                    if (GUI.changed)
                        textObj.ApplyModifiedProperties();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("GameObject/RCore/UI/Replace Button By SimpleTMPButton")]
        private static void ReplaceButton()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var buttons = gameObjects[i].GetComponentsInChildren<UnityEngine.UI.Button>();
                for (int j = 0; j < buttons.Length; j++)
                {
                    var btn = buttons[j];
                    if (btn is not SimpleTMPButton)
                    {
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
}