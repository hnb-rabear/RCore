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
    class SimpleButtonEditor : JustButtonEditor
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
                    SerializedObject textObj = new SerializedObject(text);
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
            var gameobjects = Selection.gameObjects;
            for (int i = 0; i < gameobjects.Length; i++)
            {
                var btns = gameobjects[i].FindComponentsInChildren<Button>();
                for (int j = 0; j < btns.Count; j++)
                {
                    var btn = btns[j];
                    if (!(btn is SimpleButton))
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