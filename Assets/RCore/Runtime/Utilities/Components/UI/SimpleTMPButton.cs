/**
 * Author RadBear - nbhung71711 @gmail.com - 2018
 **/

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RCore.Common;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
    [AddComponentMenu("RCore/UI/SimpleTMPButton")]
    public class SimpleTMPButton : JustButton
    {
        [SerializeField]
        protected TextMeshProUGUI mLabelTMP;
        public TextMeshProUGUI labelTMP
        {
            get
            {
                if (mLabelTMP == null && !mFindLabel)
                {
                    mLabelTMP = GetComponentInChildren<TextMeshProUGUI>();
                    mFindLabel = true;
                }
                return mLabelTMP;
            }
        }
        private bool mFindLabel;

        [SerializeField] protected bool mFontColorSwap;
        [SerializeField] protected Color mFontColorActive;
        [SerializeField] protected Color mFontColorInactive;

#if UNITY_EDITOR
        [ContextMenu("Validate")]
        protected override void OnValidate()
        {
            base.OnValidate();

            if (mLabelTMP == null)
                mLabelTMP = GetComponentInChildren<TextMeshProUGUI>();
        }
#endif

        public override void SetEnable(bool pValue)
        {
            base.SetEnable(pValue);

            if (pValue)
            {
                if (mFontColorSwap)
                    mLabelTMP.color = mFontColorActive;
            }
            else
            {
                if (mFontColorSwap)
                    mLabelTMP.color = mFontColorInactive;
            }
        }
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SimpleTMPButton), true)]
    class SimpleTMPButtonEditor : JustButtonEditor
    {
        private SimpleTMPButton mButton;

        protected override void OnEnable()
        {
            base.OnEnable();

            mButton = (SimpleTMPButton)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginVertical("box");
            {
                var fontColorSwap = EditorHelper.SerializeField(serializedObject, "mFontColorSwap");
                if (fontColorSwap.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginVertical("box");
                    EditorHelper.SerializeField(serializedObject, "mFontColorActive");
                    EditorHelper.SerializeField(serializedObject, "mFontColorInactive");
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }

                var label = EditorHelper.SerializeField(serializedObject, "mLabelTMP");
                var text = label.objectReferenceValue as TextMeshProUGUI;
                if (text != null)
                {
                    SerializedObject textObj = new SerializedObject(text);
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

        [MenuItem("RCore/UI/Replace Button By SimpleTMPButton")]
        private static void ReplaceButton()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var buttons = gameObjects[i].FindComponentsInChildren<Button>();
                for (int j = 0; j < buttons.Count; j++)
                {
                    var btn = buttons[j];
                    if (btn is not SimpleTMPButton)
                    {
                        var obj = btn.gameObject;
                        DestroyImmediate(btn);
                        obj.AddComponent<SimpleTMPButton>();
                    }
                }
            }
        }
    }
#endif
}