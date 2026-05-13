using System;
using System.Collections.Generic;
using UnityEditor;

namespace RevCore.Editor
{
    public class EditorPrefEnum<T> : EditorPref where T : Enum
    {
        private T m_value;

        public T Value
        {
            get => m_value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(m_value, value))
                    return;
                m_value = value;
                changed = true;
            }
        }

        public EditorPrefEnum(string key, T defaultValue = default) : base(key)
        {
            int defaultInt = Convert.ToInt32(defaultValue);
            m_value = (T)Enum.ToObject(typeof(T), EditorPrefs.GetInt(key, defaultInt));
        }

        public override string ToString() => m_value.ToString();

        public override void SaveChange()
        {
            if (!changed)
                return;
            EditorPrefs.SetInt(Key, Convert.ToInt32(m_value));
            changed = false;
        }
    }
}
