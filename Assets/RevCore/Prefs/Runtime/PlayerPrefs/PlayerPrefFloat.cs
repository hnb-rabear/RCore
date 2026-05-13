using UnityEngine;

namespace RevCore
{
    public class PlayerPrefFloat : PlayerPref
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

        public PlayerPrefFloat(string key, float defaultValue = 0f) : base(key)
        {
            m_value = PlayerPrefs.GetFloat(key, defaultValue);
        }

        public override string ToString() => m_value.ToString();

        public override void SaveChange()
        {
            if (!changed)
                return;
            PlayerPrefs.SetFloat(Key, m_value);
            changed = false;
        }
    }
}
