using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore
{
    internal static class PoolComponentExtensions
    {
        public static void Prepare<T>(this List<T> list, T prefab, Transform parent, int count, string name) where T : Component
        {
            for (int i = 0; i < count; i++)
            {
                var item = Object.Instantiate(prefab, parent);
                item.name = name;
                item.gameObject.SetActive(false);
                list.Add(item);
            }
        }

    }
}
