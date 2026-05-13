using UnityEditor;

namespace RevCore.Editor
{
    public class EditorPrefInt : EditorPref
    {
        private int m_value;

        public int Value
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

        public EditorPrefInt(string key, int defaultValue = 0) : base(key)
        {
            m_value = EditorPrefs.GetInt(key, defaultValue);
        }

        public override string ToString() => m_value.ToString();

        public override void SaveChange()
        {
            if (!changed)
                return;
            EditorPrefs.SetInt(Key, m_value);
            changed = false;
        }
    }
}
