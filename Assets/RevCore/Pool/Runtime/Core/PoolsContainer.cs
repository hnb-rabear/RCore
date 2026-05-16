using System.Collections.Generic;
using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// Default <see cref="IPoolContainer{T}"/> implementation. Maintains per-prefab <see cref="RevPool{T}"/>
    /// instances under a shared container transform. Looks up a clone's owning pool via an internal
    /// dictionary so <see cref="Release(T)"/> works without specifying the prefab.
    /// </summary>
    public class PoolsContainer<T> : IPoolContainer<T> where T : Component
    {
        private readonly Dictionary<int, RevPool<T>> m_poolByPrefabId = new();
        private readonly Dictionary<T, RevPool<T>> m_cloneToPool = new();
        private readonly Dictionary<T, RevPool<T>> m_prefabToPool = new();
        private readonly int m_initialCount;

        /// <summary>Shared parent transform for every contained pool's instances.</summary>
        public Transform Container { get; }

        /// <summary>Per-pool active-item limit applied to pools created after this property is set. See <see cref="RevPool{T}.LimitNumber"/>.</summary>
        public int LimitNumber { get; set; }

        /// <inheritdoc />
        public int PoolCount => m_poolByPrefabId.Count;

        /// <summary>Creates an empty container hosting pools under <paramref name="container"/>.</summary>
        public PoolsContainer(Transform container)
        {
            Container = container;
        }

        /// <summary>
        /// Creates an empty container with a new transform named <paramref name="containerName"/>.
        /// Newly-created pools prewarm to <paramref name="initialCount"/> inactive items each.
        /// </summary>
        public PoolsContainer(string containerName, int initialCount = 1, Transform parent = null)
        {
            var root = new GameObject(containerName);
            root.transform.SetParent(parent);
            root.transform.localPosition = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            Container = root.transform;
            m_initialCount = initialCount;
        }

        /// <inheritdoc />
        public RevPool<T> Get(T prefab)
        {
            if (prefab == null)
                return null;

            if (m_prefabToPool.TryGetValue(prefab, out var cached))
                return cached;

            int id = prefab.gameObject.GetInstanceID();
            if (m_poolByPrefabId.TryGetValue(id, out var existing))
            {
                m_prefabToPool[prefab] = existing;
                return existing;
            }

            var pool = new RevPool<T>(prefab, m_initialCount, Container);
            pool.LimitNumber = LimitNumber;
            m_poolByPrefabId[id] = pool;
            m_prefabToPool[prefab] = pool;
            return pool;
        }

        /// <summary>Creates the pool for <paramref name="prefab"/> if needed and adopts <paramref name="sceneObjects"/> into it (see <see cref="RevPool{T}.AddOutsiders"/>).</summary>
        public void CreatePool(T prefab, List<T> sceneObjects)
        {
            Get(prefab).AddOutsiders(sceneObjects);
        }

        /// <inheritdoc />
        public T Spawn(T prefab)
        {
            return Spawn(prefab, Vector3.zero, false);
        }

        /// <inheritdoc />
        public T Spawn(T prefab, Vector3 position, bool worldPosition = true)
        {
            var pool = Get(prefab);
            var clone = pool.Spawn(position, worldPosition);
            if (!m_cloneToPool.ContainsKey(clone))
                m_cloneToPool.Add(clone, pool);
            return clone;
        }

        /// <summary>Spawns <paramref name="prefab"/> at the world position of <paramref name="point"/>.</summary>
        public T Spawn(T prefab, Transform point)
        {
            return Spawn(prefab, point.position, true);
        }

        /// <summary>Alias of <see cref="Get(T)"/>. Returns (creating if necessary) the pool for <paramref name="prefab"/>.</summary>
        public RevPool<T> Add(T prefab)
        {
            return Get(prefab);
        }

        /// <summary>
        /// Registers an already-built <paramref name="pool"/>. When a pool for the same prefab already
        /// exists, the new pool's items are merged into the existing one rather than replacing it.
        /// </summary>
        public void Add(RevPool<T> pool)
        {
            if (pool == null || pool.Prefab == null)
                return;

            int id = pool.Prefab.gameObject.GetInstanceID();
            if (!m_poolByPrefabId.ContainsKey(id))
            {
                m_poolByPrefabId[id] = pool;
                m_prefabToPool[pool.Prefab] = pool;
                return;
            }

            var existing = m_poolByPrefabId[id];
            foreach (var item in pool.ActiveList())
                if (!existing.ActiveList().Contains(item))
                    existing.ActiveList().Add(item);
            foreach (var item in pool.InactiveList())
                if (!existing.InactiveList().Contains(item))
                    existing.InactiveList().Add(item);
        }

        /// <summary>
        /// Allocates a fresh list containing every active item across every contained pool. Allocates
        /// on each call — Phase 4 will add a zero-alloc <c>CopyActiveTo(List)</c> overload.
        /// </summary>
        public List<T> GetActiveList()
        {
            var output = new List<T>();
            foreach (var pool in m_poolByPrefabId.Values)
                output.AddRange(pool.ActiveList());
            return output;
        }

        /// <summary>
        /// Allocates a fresh list containing every item (active and inactive) across every contained pool.
        /// Allocates on each call.
        /// </summary>
        public List<T> GetAllItems()
        {
            var output = new List<T>();
            foreach (var pool in m_poolByPrefabId.Values)
            {
                output.AddRange(pool.ActiveList());
                output.AddRange(pool.InactiveList());
            }
            return output;
        }

        /// <inheritdoc />
        public void Release(T item)
        {
            if (item == null)
                return;

            if (m_cloneToPool.TryGetValue(item, out var pool))
            {
                pool.Release(item);
                return;
            }

            foreach (var candidate in m_poolByPrefabId.Values)
                candidate.Release(item);
        }

        /// <summary>Direct release variant — skips the clone-to-pool lookup. Use when you already know the prefab.</summary>
        public void Release(T prefab, T item)
        {
            Get(prefab)?.Release(item);
        }

        /// <summary>Releases the pool item carrying <paramref name="gameObject"/>, scanning every contained pool.</summary>
        public void Release(GameObject gameObject)
        {
            foreach (var pool in m_poolByPrefabId.Values)
                pool.Release(gameObject);
        }

        /// <inheritdoc />
        public void ReleaseAll()
        {
            foreach (var pool in m_poolByPrefabId.Values)
                pool.ReleaseAll();
        }

        /// <summary>Searches every contained pool for the component pooled on <paramref name="gameObject"/>. Returns <c>null</c> when no pool owns it.</summary>
        public T FindComponent(GameObject gameObject)
        {
            foreach (var pool in m_poolByPrefabId.Values)
            {
                var component = pool.FindComponent(gameObject);
                if (component != null)
                    return component;
            }

            return null;
        }
    }
}
