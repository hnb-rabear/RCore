using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore
{
    public static class PoolComponentExtensions
    {
        public static void Prepare<T>(this List<T> list, T prefab, Transform parent, int count, string name = null) where T : Component
        {
            for (int i = 0; i < count; i++)
            {
                var item = Object.Instantiate(prefab, parent);
                if (!string.IsNullOrEmpty(name))
                    item.name = name;
                item.gameObject.SetActive(false);
                list.Add(item);
            }
        }

        public static T Obtain<T>(this List<T> list, Transform parent) where T : Component
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (!item.gameObject.activeSelf)
                {
                    item.transform.SetParent(parent, false);
                    item.gameObject.SetActive(true);
                    return item;
                }
            }

            if (list.Count == 0)
                return null;

            var clone = Object.Instantiate(list[0], parent);
            clone.gameObject.SetActive(true);
            list.Add(clone);
            return clone;
        }

        public static void Free<T>(this List<T> list) where T : Component
        {
            for (int i = 0; i < list.Count; i++)
                list[i].gameObject.SetActive(false);
        }

        public static void Free<T>(this List<T> list, Transform parent) where T : Component
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                item.transform.SetParent(parent, false);
                item.gameObject.SetActive(false);
            }
        }
    }
}
