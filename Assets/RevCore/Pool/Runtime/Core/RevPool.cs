using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore
{
    /// <summary>
    /// Default <see cref="IPool{T}"/> implementation. Serializable so its state survives play-mode reload
    /// when held by a <see cref="MonoBehaviour"/>. Items are appended to an active list on spawn and
    /// removed from the tail of an inactive list (LIFO reuse).
    /// </summary>
    /// <remarks>
    /// When <see cref="LimitNumber"/> > 0 and the active list is full, the oldest active item is
    /// evicted (moved to inactive) before the new spawn. Set <see cref="LimitNumber"/> to 0 to disable
    /// the cap. Set <see cref="PushToLastSibling"/> to keep newly-spawned objects on top in the
    /// transform hierarchy.
    /// </remarks>
    [Serializable]
    public class RevPool<T> : IPool<T> where T : Component
    {
        /// <summary>Invoked whenever <see cref="Spawn()"/> activates an item (both new instantiations and reuses).</summary>
        public Action<T> OnSpawn;

        [SerializeField] private T m_prefab;
        [SerializeField] private Transform m_parent;
        [SerializeField] private string m_name;
        [SerializeField] private bool m_pushToLastSibling;
        [SerializeField] private bool m_autoRelocate = true;
        [SerializeField] private int m_limitNumber;
        [SerializeField] private List<T> m_activeList = new();
        [SerializeField] private List<T> m_inactiveList = new();

        private int m_initialCount;
        private bool m_initialized;

        /// <inheritdoc />
        public T Prefab => m_prefab;
        /// <inheritdoc />
        public Transform Parent => m_parent;
        /// <inheritdoc />
        public string Name => m_name;
        /// <inheritdoc />
        public int ActiveCount => m_activeList.Count;
        /// <inheritdoc />
        public int InactiveCount => m_inactiveList.Count;
        /// <inheritdoc />
        public IReadOnlyList<T> ActiveItems => m_activeList;
        /// <inheritdoc />
        public IReadOnlyList<T> InactiveItems => m_inactiveList;
        /// <summary>When <c>true</c>, every spawned item is moved to the last sibling index — useful for stacking UI elements above earlier spawns.</summary>
        public bool PushToLastSibling { get => m_pushToLastSibling; set => m_pushToLastSibling = value; }
        /// <summary>Maximum active count. When reached, the next <see cref="Spawn()"/> evicts the oldest active item before spawning. Zero disables the cap.</summary>
        public int LimitNumber { get => m_limitNumber; set => m_limitNumber = value; }

        /// <summary>Empty constructor for serialized usage. Call <see cref="Init"/> before use.</summary>
        public RevPool() { }

        /// <summary>
        /// Creates and initializes a pool. Prewarms <paramref name="initialCount"/> inactive instances.
        /// When <paramref name="parent"/> is <c>null</c>, a new container GameObject is created.
        /// </summary>
        /// <param name="prefab">Source prefab.</param>
        /// <param name="initialCount">Number of pre-instantiated inactive instances.</param>
        /// <param name="parent">Transform to parent pooled instances under. <c>null</c> creates a "Pool_&lt;Name&gt;" container.</param>
        /// <param name="name">Optional display name. Defaults to the prefab's name.</param>
        /// <param name="autoRelocate">When <c>true</c>, items deactivated externally are auto-migrated from active to inactive on next spawn.</param>
        public RevPool(T prefab, int initialCount, Transform parent, string name = "", bool autoRelocate = true)
        {
            m_prefab = prefab;
            m_parent = parent;
            m_name = name;
            m_autoRelocate = autoRelocate;
            m_initialCount = initialCount;
            Init();
        }

        /// <summary>Convenience overload that resolves <typeparamref name="T"/> from a <see cref="GameObject"/> prefab.</summary>
        public RevPool(GameObject prefab, int initialCount, Transform parent, string name = "", bool autoRelocate = true)
        {
            m_prefab = prefab.GetComponent<T>();
            m_parent = parent;
            m_name = name;
            m_autoRelocate = autoRelocate;
            m_initialCount = initialCount;
            Init();
        }

        /// <summary>Returns the backing list of active items directly. Caller must not mutate the list while iterating other pool operations. Prefer <see cref="ActiveItems"/>.</summary>
        public List<T> ActiveList() => m_activeList;

        /// <summary>Returns the backing list of inactive items directly. Caller must not mutate. Prefer <see cref="InactiveItems"/>.</summary>
        public List<T> InactiveList() => m_inactiveList;

        /// <summary>
        /// One-time initialization. Creates the parent container if needed, prewarms the inactive list,
        /// and marks the pool ready. Safe to call multiple times — only the first call has effect.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Prefab"/> is <c>null</c>.</exception>
        public void Init()
        {
            if (m_initialized)
                return;

            if (m_prefab == null)
                throw new InvalidOperationException("RevPool prefab is null.");

            if (string.IsNullOrEmpty(m_name))
                m_name = m_prefab.name;

            if (m_parent == null)
            {
                var root = new GameObject($"Pool_{m_name}");
                m_parent = root.transform;
            }

            m_activeList = new List<T>();
            m_inactiveList = new List<T>();
            m_inactiveList.Prepare(m_prefab, m_parent, m_initialCount, m_name);
            m_initialized = true;
        }

        /// <summary>Instantiates additional inactive items if the inactive bucket holds fewer than <paramref name="count"/>. No-op when already at capacity.</summary>
        public void Prepare(int count)
        {
            int needed = count - m_inactiveList.Count;
            if (needed > 0)
                m_inactiveList.Prepare(m_prefab, m_parent, needed, m_name);
        }

        /// <inheritdoc />
        public T Spawn()
        {
            return Spawn(Vector3.zero, false);
        }

        /// <summary>Spawns and places the instance at <paramref name="point"/>'s world position.</summary>
        public T Spawn(Transform point)
        {
            return Spawn(point.position, true);
        }

        /// <inheritdoc />
        public T Spawn(Vector3 position, bool worldPosition = true)
        {
            return Spawn(position, worldPosition, out _);
        }

        /// <summary>
        /// Full <see cref="Spawn(Vector3, bool)"/> overload that reports whether the returned item was
        /// reused from the inactive bucket (<paramref name="reused"/> = <c>true</c>) or freshly instantiated.
        /// </summary>
        public T Spawn(Vector3 position, bool worldPosition, out bool reused)
        {
            if (m_limitNumber > 0 && m_activeList.Count == m_limitNumber)
            {
                var oldest = m_activeList[0];
                MoveToInactive(oldest, 0);
            }

            // Lazy null cleanup: items in the inactive bucket only become null when their
            // GameObjects were destroyed externally (scene unload, manual Destroy, etc.) — rare
            // in steady-state pool usage. We pop from the tail (LIFO), so only do the full-list
            // null walk when the tail is actually null. The common case skips an O(N) scan per
            // spawn. RelocateInactive (the active->inactive sweep) still runs when the bucket
            // is empty so externally-deactivated items can be reused.
            if (m_inactiveList.Count > 0 && m_inactiveList[m_inactiveList.Count - 1] == null)
                RemoveNullInactiveItems();
            if (m_autoRelocate && m_inactiveList.Count == 0)
                RelocateInactive();

            if (m_inactiveList.Count == 0)
            {
                var item = Object.Instantiate(m_prefab, m_parent);
                item.name = m_name;
                item.gameObject.SetActive(false);
                m_inactiveList.Add(item);
                reused = false;
            }
            else
            {
                reused = true;
            }

            int index = m_inactiveList.Count - 1;
            var spawned = m_inactiveList[index];
            if (worldPosition)
                spawned.transform.position = position;
            else
                spawned.transform.localPosition = position;

            MoveToActive(spawned, index);
            if (m_pushToLastSibling)
                spawned.transform.SetAsLastSibling();

            OnSpawn?.Invoke(spawned);
            return spawned;
        }

        /// <summary>Bulk variant of <see cref="AddOutsider"/>. Iterates the list in reverse order.</summary>
        public void AddOutsiders(List<T> sceneObjects)
        {
            for (int i = sceneObjects.Count - 1; i >= 0; i--)
                AddOutsider(sceneObjects[i]);
        }

        /// <summary>
        /// Adopts a scene-placed instance into this pool. Reparents under <see cref="Parent"/> and slots
        /// the instance into the active or inactive bucket based on its current <c>activeSelf</c>. No-op
        /// when the instance is null or already tracked.
        /// </summary>
        public void AddOutsider(T sceneObject)
        {
            if (sceneObject == null || m_activeList.Contains(sceneObject) || m_inactiveList.Contains(sceneObject))
                return;

            sceneObject.transform.SetParent(m_parent);
            if (sceneObject.gameObject.activeSelf)
                m_activeList.Add(sceneObject);
            else
                m_inactiveList.Add(sceneObject);
        }

        /// <inheritdoc />
        public void Release(T item)
        {
            if (item == null)
                return;

            for (int i = 0; i < m_activeList.Count; i++)
            {
                if (ReferenceEquals(m_activeList[i], item))
                {
                    MoveToInactive(item, i);
                    return;
                }
            }
        }

        /// <summary>
        /// Schedules a release after <paramref name="delaySeconds"/>. A non-positive delay releases
        /// immediately. The schedule is keyed on the item's instance ID, so repeated calls supersede
        /// the previous schedule for that item.
        /// </summary>
        public void Release(T item, float delaySeconds)
        {
            if (delaySeconds <= 0f)
            {
                Timers.Cancel(item.GetInstanceID());
                Release(item);
                return;
            }

            Timers.WaitForSeconds(delaySeconds, () => Release(item), false, item.GetInstanceID());
        }

        /// <summary>Releases <paramref name="item"/> once <paramref name="condition"/> returns <c>true</c>. The poll runs on the timer driver each frame.</summary>
        public void Release(T item, ConditionalDelegate condition)
        {
            if (item == null)
                return;

            Timers.WaitForCondition(condition, () => Release(item), item.GetInstanceID());
        }

        /// <summary>Releases the pool item carrying <paramref name="item"/> as its GameObject. No-op if not owned by this pool.</summary>
        public void Release(GameObject item)
        {
            var component = FindComponent(item);
            Release(component);
        }

        /// <inheritdoc />
        public void ReleaseAll()
        {
            for (int i = m_activeList.Count - 1; i >= 0; i--)
                MoveToInactive(m_activeList[i], i);
        }

        /// <summary>Destroys every active and inactive instance, clearing both buckets. Use this on scene unload to avoid leaking pooled prefabs.</summary>
        public void DestroyAll()
        {
            DestroyList(m_activeList);
            DestroyList(m_inactiveList);
            m_activeList.Clear();
            m_inactiveList.Clear();
        }

        /// <summary>Destroys a single instance and removes it from whichever bucket holds it. No-op for null or unknown items.</summary>
        public void Destroy(T item)
        {
            if (item == null)
                return;

            m_activeList.Remove(item);
            m_inactiveList.Remove(item);
            DestroyObject(item.gameObject);
        }

        /// <summary>Returns <paramref name="item"/> if it is currently in the active bucket, otherwise <c>null</c>.</summary>
        public T FindFromActive(T item)
        {
            return m_activeList.Contains(item) ? item : null;
        }

        /// <summary>Returns <paramref name="item"/> if it is currently in the inactive bucket, otherwise <c>null</c>.</summary>
        public T FindFromInactive(T item)
        {
            return m_inactiveList.Contains(item) ? item : null;
        }

        /// <summary>Looks up the <typeparamref name="T"/> component on the given GameObject across both buckets. Returns <c>null</c> if not pooled by this pool.</summary>
        public T FindComponent(GameObject gameObject)
        {
            for (int i = 0; i < m_activeList.Count; i++)
                if (m_activeList[i] != null && m_activeList[i].gameObject == gameObject)
                    return m_activeList[i];

            for (int i = 0; i < m_inactiveList.Count; i++)
                if (m_inactiveList[i] != null && m_inactiveList[i].gameObject == gameObject)
                    return m_inactiveList[i];

            return null;
        }

        /// <summary>Index-based access to the active list. Returns <c>null</c> when out of range. Equivalent to <c>ActiveItems[index]</c> but null-safe.</summary>
        public T GetFromActive(int index)
        {
            return index >= 0 && index < m_activeList.Count ? m_activeList[index] : null;
        }

        /// <summary>Replaces the parent transform. Does not reparent existing pooled instances — call after <see cref="Init"/> only if you understand the implications.</summary>
        public void SetParent(Transform parent)
        {
            m_parent = parent;
        }

        /// <summary>Sets the display name used for newly instantiated items.</summary>
        public void SetName(string name)
        {
            m_name = name;
        }

        private void MoveToActive(T item, int inactiveIndex)
        {
            m_inactiveList.RemoveAt(inactiveIndex);
            m_activeList.Add(item);
            item.gameObject.SetActive(true);
        }

        private void MoveToInactive(T item, int activeIndex)
        {
            m_activeList.RemoveAt(activeIndex);
            m_inactiveList.Add(item);
            item.gameObject.SetActive(false);
        }

        private void RelocateInactive()
        {
            for (int i = m_activeList.Count - 1; i >= 0; i--)
            {
                var item = m_activeList[i];
                if (item == null)
                {
                    m_activeList.RemoveAt(i);
                    continue;
                }

                if (!item.gameObject.activeSelf)
                    MoveToInactive(item, i);
            }
        }

        private void RemoveNullInactiveItems()
        {
            for (int i = m_inactiveList.Count - 1; i >= 0; i--)
                if (m_inactiveList[i] == null)
                    m_inactiveList.RemoveAt(i);
        }

        private static void DestroyList(List<T> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i] != null)
                    DestroyObject(list[i].gameObject);
        }

        private static void DestroyObject(GameObject gameObject)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(gameObject);
            else
                UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
