using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    public class EditorPrefVector : EditorPref
    {
        private Vector3 m_value;

        public Vector3 Value
        {
            get => m_value;
            set
            {
                if (m_value == value)
                    return;
                m_value = value;
                changed = true;
            }
        }

        public EditorPrefVector(string key, Vector3 defaultValue = default) : base(key)
        {
            m_value = new Vector3(
                EditorPrefs.GetFloat($"{key}_x", defaultValue.x),
                EditorPrefs.GetFloat($"{key}_y", defaultValue.y),
                EditorPrefs.GetFloat($"{key}_z", defaultValue.z)
            );
        }

        public override string ToString() => m_value.ToString();

        public override void SaveChange()
        {
            if (!changed)
                return;
            EditorPrefs.SetFloat($"{Key}_x", m_value.x);
            EditorPrefs.SetFloat($"{Key}_y", m_value.y);
            EditorPrefs.SetFloat($"{Key}_z", m_value.z);
            changed = false;
        }
    }
}
