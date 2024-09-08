/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Common
{
    [System.Serializable]
    public class AssetsList<T> where T : Object
    {
        public virtual bool showBox { get; }
        public virtual bool @readonly { get; }
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
        public void DrawInEditor(string pDisplayName, List<string> labels = null)
        {
            var draw = new EditorObject<T>()
            {
                value = defaultAsset,
                showAsBox = defaultAsset is Sprite,
                label = "Default",
            };
            if (EditorHelper.ListObjects(pDisplayName, ref source, labels, showBox, @readonly, new IDraw[] { draw }))
                defaultAsset = (T)draw.OutputValue;
        }
#endif
    }

    [System.Serializable]
    public class AssetsArray<T> where T : Object
    {
        public T[] source;
        public T defaultAsset;

        public AssetsArray() { }

        public AssetsArray(T[] pSource, T pDefault)
        {
            source = pSource;
            defaultAsset = pDefault;
        }

        public AssetsArray<T> Init(T[] pSource, T pDefault = null)
        {
            source = pSource;
            defaultAsset = pDefault;
            return this;
        }

        public T GetAsset(string pSpriteName)
        {
            foreach (var s in source)
                if (s != null && pSpriteName != null && s.name.ToLower() == pSpriteName.ToLower())
                    return s;

            Debug.LogError($"Not found {typeof(T).Name} with name {pSpriteName}");
            return defaultAsset;
        }

        public T GetAsset(int pIndex)
        {
            if (pIndex < 0 || pIndex >= source.Length)
            {
                Debug.LogError($"Index {pIndex} {typeof(T).Name} is invalid!");
                return defaultAsset;
            }
            return source[pIndex];
        }

        public int GetAssetIndex(string pSpriteName)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i].name == pSpriteName)
                    return i;
            }
            Debug.LogError($"Not found {typeof(T).Name} with name {pSpriteName}");
            return -1;
        }
    }

    [System.Serializable]
    public class SpritesList : AssetsList<Sprite>
    {
        public override bool showBox => true;
        public override bool @readonly => false;
    }

    [System.Serializable]
    public class GameObjectList : AssetsList<GameObject>
    {
        public override bool showBox => false;
        public override bool @readonly => false;
    }
}
