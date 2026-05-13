using UnityEngine;

namespace RevCore
{
    public abstract class PlayerPref : IPref
    {
        protected bool changed;

        public string Key { get; }
        public bool IsChanged => changed;

        protected PlayerPref(string key)
        {
            Key = key;
            PlayerPrefContainer.Register(this);
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            changed = false;
        }

        public abstract void SaveChange();
    }
}
