using UnityEngine;

namespace RevCore
{
    /// <summary>Cached <see cref="string"/> PlayerPrefs entry.</summary>
    public class PlayerPrefString : PlayerPref
    {
        private string m_value;

        /// <summary>The cached value. Setting marks the entry dirty.</summary>
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

        /// <summary>Loads <paramref name="key"/> from PlayerPrefs, falling back to <paramref name="defaultValue"/>.</summary>
        public PlayerPrefString(string key, string defaultValue = "") : base(key)
        {
            m_value = PlayerPrefs.GetString(key, defaultValue);
        }

        /// <inheritdoc />
        public override string ToString() => m_value;

        /// <inheritdoc />
        public override void SaveChange()
        {
            if (!changed)
                return;
            PlayerPrefs.SetString(Key, m_value);
            changed = false;
        }
    }
}
