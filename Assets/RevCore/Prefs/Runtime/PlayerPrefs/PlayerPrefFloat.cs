using UnityEngine;

namespace RevCore
{
    /// <summary>Cached <see cref="float"/> PlayerPrefs entry.</summary>
    public class PlayerPrefFloat : PlayerPref
    {
        private float m_value;

        /// <summary>The cached value. Setting marks the entry dirty.</summary>
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

        /// <summary>Loads <paramref name="key"/> from PlayerPrefs, falling back to <paramref name="defaultValue"/>.</summary>
        public PlayerPrefFloat(string key, float defaultValue = 0f) : base(key)
        {
            m_value = PlayerPrefs.GetFloat(key, defaultValue);
        }

        /// <inheritdoc />
        public override string ToString() => m_value.ToString();

        /// <inheritdoc />
        public override void SaveChange()
        {
            if (!changed)
                return;
            PlayerPrefs.SetFloat(Key, m_value);
            changed = false;
        }
    }
}
