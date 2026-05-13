using UnityEditor;

namespace RevCore.Editor
{
    public abstract class EditorPref : IPref
    {
        protected bool changed;

        public string Key { get; }
        public bool IsChanged => changed;

        protected EditorPref(string key)
        {
            Key = key;
            EditorPrefContainer.Register(this);
        }

        public void Delete()
        {
            EditorPrefs.DeleteKey(Key);
            changed = false;
        }

        public abstract void SaveChange();
    }
}
