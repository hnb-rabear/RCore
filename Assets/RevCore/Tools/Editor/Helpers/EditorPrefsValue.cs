using UnityEditor;

namespace RevCore.Tools.Editor
{
    public sealed class EditorPrefsValue<T>
    {
        private readonly string m_key;
        private readonly T m_defaultValue;

        public EditorPrefsValue(string key, T defaultValue = default)
        {
            m_key = key;
            m_defaultValue = defaultValue;
        }

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        private T GetValue()
        {
            object value;
            if (typeof(T) == typeof(string))
                value = EditorPrefs.GetString(m_key, (string)(object)m_defaultValue);
            else if (typeof(T) == typeof(bool))
                value = EditorPrefs.GetBool(m_key, (bool)(object)m_defaultValue);
            else if (typeof(T) == typeof(int))
                value = EditorPrefs.GetInt(m_key, (int)(object)m_defaultValue);
            else if (typeof(T) == typeof(float))
                value = EditorPrefs.GetFloat(m_key, (float)(object)m_defaultValue);
            else
                throw new System.NotSupportedException(typeof(T).Name);
            return (T)value;
        }

        private void SetValue(T value)
        {
            if (typeof(T) == typeof(string))
                EditorPrefs.SetString(m_key, (string)(object)value);
            else if (typeof(T) == typeof(bool))
                EditorPrefs.SetBool(m_key, (bool)(object)value);
            else if (typeof(T) == typeof(int))
                EditorPrefs.SetInt(m_key, (int)(object)value);
            else if (typeof(T) == typeof(float))
                EditorPrefs.SetFloat(m_key, (float)(object)value);
            else
                throw new System.NotSupportedException(typeof(T).Name);
        }
    }
}
