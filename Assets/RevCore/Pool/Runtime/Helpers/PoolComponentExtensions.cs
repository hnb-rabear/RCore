using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore
{
    /// <summary>
    /// List-based light pool helpers — used by UI components (e.g. optimized scroll views) that
    /// need to prewarm and reuse instances but don't want the full <see cref="RevPool{T}"/> machinery.
    /// </summary>
    public static class PoolComponentExtensions
    {
        /// <summary>
        /// Instantiates <paramref name="count"/> copies of <paramref name="prefab"/> under <paramref name="parent"/>,
        /// deactivates them, and adds them to <paramref name="list"/>. Optionally renames each instance.
        /// </summary>
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

        /// <summary>
        /// Returns the first inactive item, activating it and reparenting under <paramref name="parent"/>.
        /// When all items are active, clones the first as a template, activates, and appends. Returns
        /// <c>null</c> only when the list is empty (no template to clone).
        /// </summary>
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

        /// <summary>Deactivates every item without reparenting.</summary>
        public static void Free<T>(this List<T> list) where T : Component
        {
            for (int i = 0; i < list.Count; i++)
                list[i].gameObject.SetActive(false);
        }

        /// <summary>Reparents every item under <paramref name="parent"/> and deactivates it.</summary>
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
