using System;
using UnityEngine;

namespace RevCore
{
    public class PlayerPrefBool : PlayerPref
    {
        public Action OnUpdated;

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
                OnUpdated?.Invoke();
            }
        }

        public PlayerPrefBool(string key, bool defaultValue = false) : base(key)
        {
            m_value = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        public override string ToString() => m_value.ToString();

        public override void SaveChange()
        {
            if (!changed)
                return;
            PlayerPrefs.SetInt(Key, m_value ? 1 : 0);
            changed = false;
        }
    }
}
