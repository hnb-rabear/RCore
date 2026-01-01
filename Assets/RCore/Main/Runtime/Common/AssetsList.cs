/***
 * Author HNB-RaBear - 2019
 **/

#if UNITY_EDITOR
using RCore.Editor;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
    /// <summary>
    /// A generic wrapper for a list of assets (e.g., Sprites, AudioClips) that provides helper methods
    /// to retrieve items by index or name, with a fallback default asset.
    /// </summary>
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

        /// <summary>
        /// Initializes the list with a source collection and an optional default asset.
        /// </summary>
        public AssetsList<T> Init(List<T> pSource, T pDefault = null)
        {
            source = pSource;
            defaultAsset = pDefault;
            return this;
        }

        /// <summary>
        /// Retrieves an asset by its name.
        /// </summary>
        /// <param name="pSpriteName">The name of the asset to find.</param>
        /// <param name="pContain">If true, checks if the asset name contains the search string; otherwise, checks for equality.</param>
        /// <returns>The matching asset, or the default asset if not found.</returns>
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

        /// <summary>
        /// Retrieves an asset by its index.
        /// </summary>
        /// <param name="pIndex">The index of the asset.</param>
        /// <returns>The asset at the specified index, or the default asset if the index is invalid.</returns>
        public T GetAsset(int pIndex)
        {
            if (pIndex < 0 || pIndex >= source.Count)
            {
                Debug.LogError($"Index {pIndex} {typeof(T).Name} is invalid!");
                return defaultAsset;
            }
            return source[pIndex];
        }

        /// <summary>
        /// Finds the index of an asset by its name.
        /// </summary>
        /// <param name="pSpriteName">The name of the asset.</param>
        /// <returns>The index of the asset, or -1 if not found.</returns>
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