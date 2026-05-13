using UnityEditor;

namespace RevCore.Editor
{
    public class EditorPrefString : EditorPref
    {
        private string m_value;

        public string Value
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

        public EditorPrefString(string key, string defaultValue = "") : base(key)
        {
            m_value = EditorPrefs.GetString(key, defaultValue);
        }

        public override string ToString() => m_value;

        public override void SaveChange()
        {
            if (!changed)
                return;
            EditorPrefs.SetString(Key, m_value);
            changed = false;
        }
    }
}
