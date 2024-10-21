/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

#if UNITY_EDITOR
using RCore.Editor;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
    [System.Serializable]
    public class AssetsList<T> where T : Object
    {
        public List<T> source = new List<T>();

        public T defaultAsset;
        public T this[int index]
        {
            get => GetAsset(index);
            set => source[index] = value;
        }

        public AssetsList() { }

        public AssetsList(List<T> pSource, T pDefault = null)
        {
            source = pSource;
            defaultAsset = pDefault;
        }

        public AssetsList<T> Init(List<T> pSource, T pDefault = null)
        {
            source = pSource;
            defaultAsset = pDefault;
            return this;
        }

        public T GetAsset(string pSpriteName, bool pContain = false)
        {
            foreach (var s in source)
            {
                if (s != null && pSpriteName != null && (s.name.ToLower() == pSpriteName.ToLower() || pContain && s.name.ToLower().Contains(pSpriteName.ToLower())))
                    return s;
            }

            Debug.LogError($"Not found {typeof(T).Name} with name {pSpriteName}");
            return defaultAsset;
        }

        public T GetAsset(int pIndex)
        {
            if (pIndex < 0 || pIndex >= source.Count)
            {
                Debug.LogError($"Index {pIndex} {typeof(T).Name} is invalid!");
                return defaultAsset;
            }
            return source[pIndex];
        }

        public int GetAssetIndex(string pSpriteName)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].name == pSpriteName)
                    return i;
            }
            Debug.LogError($"Not found {typeof(T).Name} with name {pSpriteName}");
            return -1;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Used in OnInspectorGUI() of CustomEditor
        /// </summary>
        public void Draw(string pDisplayName, bool @readonly = false, List<string> labels = null)
        {
            EditorHelper.DrawAssetsList(this, pDisplayName, @readonly, labels);
        }
#endif
    }
}