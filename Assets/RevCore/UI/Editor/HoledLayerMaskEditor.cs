using UnityEditor;
using UnityEngine;

namespace RevCore.UI.Editor
{
    [CustomEditor(typeof(HoledLayerMask))]
    public class HoledLayerMaskEditor : UnityEditor.Editor
    {
        private HoledLayerMask m_script;
        private Sprite m_sprite;

        private void OnEnable()
        {
            m_script = (HoledLayerMask)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            m_sprite = (Sprite)EditorGUILayout.ObjectField("Sprite to Clone", m_sprite, typeof(Sprite), true);

            if (GUILayout.Button("Clone Sprite"))
                m_script.CreateHoleFromSprite(m_sprite);
            if (GUILayout.Button("Focus To Test Target"))
                m_script.FocusToTargetImmediately(m_script.testTarget);
            if (GUILayout.Button("Create Components"))
                m_script.CreateComponents();
        }
    }
}
