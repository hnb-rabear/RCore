/**
 * Author HNB-RaBear - 2021
 **/

using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
    public class REditorPrefs
    {
        protected string mainKey;
        protected int subKey;

        protected string Key => $"{mainKey}_{subKey}";

        public REditorPrefs(int pMainKey, int pSubKey = 0)
        {
            mainKey = $"{pMainKey}_{pSubKey}";
        }

        public REditorPrefs(string pMainKey, int pSubKey = 0)
        {
            mainKey = $"{pMainKey}_{pSubKey}";
        }
    }

    public class EditorPrefsBool : REditorPrefs
    {
        private bool m_value;

        public EditorPrefsBool(int pMainKey, bool pDefault = false, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            m_value = EditorPrefs.GetBool(Key, pDefault);
        }

        public EditorPrefsBool(string pMainKey, bool pDefault = false, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            m_value = EditorPrefs.GetBool(Key, pDefault);
        }

        public bool Value
        {
            get => m_value;
            set
            {
                if (m_value != value)
                {
                    m_value = value;
                    EditorPrefs.SetBool(Key, value);
                }
            }
        }

        public override string ToString()
        {
            return m_value.ToString();
        }
    }

    public class EditorPrefsString : REditorPrefs
    {
        private string m_value;

        public EditorPrefsString(int pMainKey, string pDefault = "", int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            m_value = EditorPrefs.GetString(Key, pDefault);
        }

        public EditorPrefsString(string pMainKey, string pDefault = "", int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            m_value = EditorPrefs.GetString(Key, pDefault);
        }

        public string Value
        {
            get => m_value;
            set
            {
                if (m_value != value)
                {
                    m_value = value;
                    EditorPrefs.SetString(Key, value);
                }
            }
        }

        public override string ToString()
        {
            return m_value;
        }
    }

    public class EditorPrefsVector : REditorPrefs
    {
        private Vector3 m_value;

        public EditorPrefsVector(int pMainKey, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            float x = EditorPrefs.GetFloat($"{Key}x");
            float y = EditorPrefs.GetFloat($"{Key}y");
            float z = EditorPrefs.GetFloat($"{Key}z");
            m_value = new Vector3(x, y, z);
        }

        public EditorPrefsVector(string pMainKey, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            float x = EditorPrefs.GetFloat($"{Key}x");
            float y = EditorPrefs.GetFloat($"{Key}y");
            float z = EditorPrefs.GetFloat($"{Key}z");
            m_value = new Vector3(x, y, z);
        }

        public Vector3 Value
        {
            get => m_value;
            set
            {
                if (m_value != value)
                {
                    m_value = value;
                    EditorPrefs.SetFloat($"{Key}x", value.x);
                    EditorPrefs.SetFloat($"{Key}y", value.y);
                    EditorPrefs.SetFloat($"{Key}z", value.z);
                }
            }
        }

        public override string ToString()
        {
            return m_value.ToString();
        }
    }

    public class EditorPrefsEnum<T> : REditorPrefs
    {
        private T m_value;

        public EditorPrefsEnum(int pMainKey, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            string strValue = EditorPrefs.GetString(Key);
            foreach (T item in Enum.GetValues(typeof(T)))
                if (item.ToString() == strValue)
                    m_value = item;
        }

        public EditorPrefsEnum(string pMainKey, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            string strValue = EditorPrefs.GetString(Key);
            foreach (T item in Enum.GetValues(typeof(T)))
                if (item.ToString() == strValue)
                    m_value = item;
        }

        public T Value
        {
            get => m_value;
            set
            {
                if (!typeof(T).IsEnum)
                    throw new ArgumentException("T must be an enumerated type");
                var inputValue = value.ToString();
                var strValue = m_value.ToString();
                if (strValue != inputValue)
                {
                    m_value = value;
                    EditorPrefs.SetString(Key, inputValue);
                }
            }
        }

        public override string ToString()
        {
            return m_value.ToString();
        }
    }

    public class EditorPrefsFloat : REditorPrefs
    {
        private float m_value;

        public EditorPrefsFloat(int pMainKey, float pDefault = 0, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            m_value = EditorPrefs.GetFloat(Key, pDefault);
        }

        public EditorPrefsFloat(string pMainKey, float pDefault = 0, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            m_value = EditorPrefs.GetFloat(Key, pDefault);
        }

        public float Value
        {
            get => m_value;
            set
            {
                if (m_value != value)
                {
                    m_value = value;
                    EditorPrefs.SetFloat(Key, value);
                }
            }
        }

        public override string ToString()
        {
            return m_value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class EditorPrefsInt : REditorPrefs
    {
        private int m_value;

        public EditorPrefsInt(int pMainKey, int pDefault = 0, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            m_value = EditorPrefs.GetInt(Key, pDefault);
        }

        public EditorPrefsInt(string pMainKey, int pDefault = 0, int pSubKey = 0) : base(pMainKey, pSubKey)
        {
            m_value = EditorPrefs.GetInt(Key, pDefault);
        }

        public int Value
        {
            get => m_value;
            set
            {
                if (m_value != value)
                {
                    m_value = value;
                    EditorPrefs.SetInt(Key, value);
                }
            }
        }

        public override string ToString()
        {
            return m_value.ToString();
        }
    }
}