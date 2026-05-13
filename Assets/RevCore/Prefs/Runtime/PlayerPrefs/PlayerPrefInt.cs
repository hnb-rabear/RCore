using UnityEngine;

namespace RevCore
{
    public class PlayerPrefInt : PlayerPref
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

        public PlayerPrefInt(string key, int defaultValue = 0) : base(key)
        {
            m_value = PlayerPrefs.GetInt(key, defaultValue);
        }

        public override string ToString() => m_value.ToString();

        public override void SaveChange()
        {
            if (!changed)
                return;
            PlayerPrefs.SetInt(Key, m_value);
            changed = false;
        }
    }
}
