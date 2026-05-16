using UnityEngine;

namespace RevCore
{
    /// <summary>Cached <see cref="int"/> PlayerPrefs entry.</summary>
    public class PlayerPrefInt : PlayerPref
    {
        private int m_value;

        /// <summary>The cached value. Setting marks the entry dirty.</summary>
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

        /// <summary>Loads <paramref name="key"/> from PlayerPrefs, falling back to <paramref name="defaultValue"/>.</summary>
        public PlayerPrefInt(string key, int defaultValue = 0) : base(key)
        {
            m_value = PlayerPrefs.GetInt(key, defaultValue);
        }

        /// <inheritdoc />
        public override string ToString() => m_value.ToString();

        /// <inheritdoc />
        public override void SaveChange()
        {
            if (!changed)
                return;
            PlayerPrefs.SetInt(Key, m_value);
            changed = false;
        }
    }
}
