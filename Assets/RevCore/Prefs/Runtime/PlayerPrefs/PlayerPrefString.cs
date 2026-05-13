using UnityEngine;

namespace RevCore
{
    public class PlayerPrefString : PlayerPref
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

        public PlayerPrefString(string key, string defaultValue = "") : base(key)
        {
            m_value = PlayerPrefs.GetString(key, defaultValue);
        }

        public override string ToString() => m_value;

        public override void SaveChange()
        {
            if (!changed)
                return;
            PlayerPrefs.SetString(Key, m_value);
            changed = false;
        }
    }
}
