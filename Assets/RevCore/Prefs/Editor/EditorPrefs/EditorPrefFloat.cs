using UnityEditor;

namespace RevCore.Editor
{
    public class EditorPrefFloat : EditorPref
    {
        private float m_value;

        public float Value
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

        public EditorPrefFloat(string key, float defaultValue = 0f) : base(key)
        {
            m_value = EditorPrefs.GetFloat(key, defaultValue);
        }

        public override string ToString() => m_value.ToString();

        public override void SaveChange()
        {
            if (!changed)
                return;
            EditorPrefs.SetFloat(Key, m_value);
            changed = false;
        }
    }
}
