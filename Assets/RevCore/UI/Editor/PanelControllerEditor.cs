using RevCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI.Editor
{
    [CustomEditor(typeof(PanelController), true)]
    public class PanelControllerEditor : PanelStackEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Add Blocker Image"))
            {
                var img = m_script.gameObject.GetOrAddComponent<Image>();
                if (img.color != Color.clear)
                    img.color = Color.clear;
            }

            if (GUILayout.Button("Add Transition Animation"))
            {
                var animator = m_script.gameObject.GetOrAddComponent<Animator>();
                if (animator.runtimeAnimatorController != null)
                    animator.gameObject.GetOrAddComponent<CanvasGroup>();
                ((PanelController)target).enableFXTransition = true;
                EditorUtility.SetDirty(m_script);
            }
        }
    }
}
