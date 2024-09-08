using UnityEngine;
using UnityEngine.UI;
using RCore.Common;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
    [AddComponentMenu("RCore/UI/SimpleButton")]
    public class SimpleButton : JustButton
    {
        [SerializeField]
        protected Text mLabel;
        public Text label
        {
            get
            {
                if (mLabel == null && !mFindLabel)
                {
                    mLabel = GetComponentInChildren<Text>();
                    mFindLabel = true;
                }
                return mLabel;
            }
        }
        protected bool mFindLabel;

#if UNITY_EDITOR
        [ContextMenu("Validate")]
        protected override void OnValidate()
        {
            if (mLabel == null)
                mLabel = GetComponentInChildren<Text>();
        }
#endif
    }

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SimpleButton), true)]
    internal class SimpleButtonEditor : JustButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.BeginVertical("box");
            {
                var label = EditorHelper.SerializeField(serializedObject, "mLabel");
                var text = label.objectReferenceValue as Text;
                if (text != null)
                {
                    var textObj = new SerializedObject(text);
                    EditorHelper.SerializeField(textObj, "m_Text");

                    if (GUI.changed)
                        textObj.ApplyModifiedProperties();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("RCore/UI/Replace Button By SimpleButton")]
        private static void ReplaceButton()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var buttons = gameObjects[i].FindComponentsInChildren<Button>();
                for (int j = 0; j < buttons.Count; j++)
                {
                    var btn = buttons[j];
                    if (btn is not SimpleButton)
                    {
                        var obj = btn.gameObject;
                        DestroyImmediate(btn);
                        obj.AddComponent<SimpleButton>();
                    }
                }
            }
        }
    }
#endif
}