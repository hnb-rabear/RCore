using System.Collections.Generic;
using UnityEngine;

namespace RevCore
{
    public class PoolsContainer<T> : IPoolContainer<T> where T : Component
    {
        private readonly Dictionary<int, RevPool<T>> m_poolByPrefabId = new();
        private readonly Dictionary<T, RevPool<T>> m_cloneToPool = new();
        private readonly Dictionary<T, RevPool<T>> m_prefabToPool = new();
        private readonly int m_initialCount;

        public Transform Container { get; }
        public int LimitNumber { get; set; }
        public int PoolCount => m_poolByPrefabId.Count;

        public PoolsContainer(Transform container)
        {
            Container = container;
        }

        public PoolsContainer(string containerName, int initialCount = 1, Transform parent = null)
        {
            var root = new GameObject(containerName);
            root.transform.SetParent(parent);
            root.transform.localPosition = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            Container = root.transform;
            m_initialCount = initialCount;
        }

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

        public void CreatePool(T prefab, List<T> sceneObjects)
        {
            Get(prefab).AddOutsiders(sceneObjects);
        }

        public T Spawn(T prefab)
        {
            return Spawn(prefab, Vector3.zero, false);
        }

        public T Spawn(T prefab, Vector3 position, bool worldPosition = true)
        {
            var pool = Get(prefab);
            var clone = pool.Spawn(position, worldPosition);
            if (!m_cloneToPool.ContainsKey(clone))
                m_cloneToPool.Add(clone, pool);
            return clone;
        }

        public T Spawn(T prefab, Transform point)
        {
            return Spawn(prefab, point.position, true);
        }

        public RevPool<T> Add(T prefab)
        {
            return Get(prefab);
        }

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

        public List<T> GetActiveList()
        {
            var output = new List<T>();
            foreach (var pool in m_poolByPrefabId.Values)
                output.AddRange(pool.ActiveList());
            return output;
        }

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

        public void Release(T prefab, T item)
        {
            Get(prefab)?.Release(item);
        }

        public void Release(GameObject gameObject)
        {
            foreach (var pool in m_poolByPrefabId.Values)
                pool.Release(gameObject);
        }

        public void ReleaseAll()
        {
            foreach (var pool in m_poolByPrefabId.Values)
                pool.ReleaseAll();
        }

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
