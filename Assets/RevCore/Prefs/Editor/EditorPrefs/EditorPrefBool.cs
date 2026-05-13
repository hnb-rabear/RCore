using UnityEditor;

namespace RevCore.Editor
{
    public class EditorPrefBool : EditorPref
    {
        private bool m_value;

        public bool Value
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

        public EditorPrefBool(string key, bool defaultValue = false) : base(key)
        {
            m_value = EditorPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        public override string ToString() => m_value.ToString();

        public override void SaveChange()
        {
            if (!changed)
                return;
            EditorPrefs.SetInt(Key, m_value ? 1 : 0);
            changed = false;
        }
    }
}
