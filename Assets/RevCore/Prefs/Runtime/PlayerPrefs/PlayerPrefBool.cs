using System;
using UnityEngine;

namespace RevCore
{
    /// <summary>Cached boolean PlayerPrefs entry (stored as 0/1 int).</summary>
    public class PlayerPrefBool : PlayerPref
    {
        /// <summary>Invoked when <see cref="Value"/> changes (not on no-op assignments).</summary>
        public Action OnUpdated;

        private bool m_value;

        /// <summary>The cached value. Setting marks the entry dirty and raises <see cref="OnUpdated"/>.</summary>
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

        /// <summary>Loads <paramref name="key"/> from PlayerPrefs, falling back to <paramref name="defaultValue"/>.</summary>
        public PlayerPrefBool(string key, bool defaultValue = false) : base(key)
        {
            m_value = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        /// <inheritdoc />
        public override string ToString() => m_value.ToString();

        /// <inheritdoc />
        public override void SaveChange()
        {
            if (!changed)
                return;
            PlayerPrefs.SetInt(Key, m_value ? 1 : 0);
            changed = false;
        }
    }
}
