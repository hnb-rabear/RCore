using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore
{
    [Serializable]
    public class RevPool<T> : IPool<T> where T : Component
    {
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

        public T Prefab => m_prefab;
        public Transform Parent => m_parent;
        public string Name => m_name;
        public int ActiveCount => m_activeList.Count;
        public int InactiveCount => m_inactiveList.Count;
        public IReadOnlyList<T> ActiveItems => m_activeList;
        public IReadOnlyList<T> InactiveItems => m_inactiveList;
        public bool PushToLastSibling { get => m_pushToLastSibling; set => m_pushToLastSibling = value; }
        public int LimitNumber { get => m_limitNumber; set => m_limitNumber = value; }

        public RevPool() { }

        public RevPool(T prefab, int initialCount, Transform parent, string name = "", bool autoRelocate = true)
        {
            m_prefab = prefab;
            m_parent = parent;
            m_name = name;
            m_autoRelocate = autoRelocate;
            m_initialCount = initialCount;
            Init();
        }

        public RevPool(GameObject prefab, int initialCount, Transform parent, string name = "", bool autoRelocate = true)
        {
            m_prefab = prefab.GetComponent<T>();
            m_parent = parent;
            m_name = name;
            m_autoRelocate = autoRelocate;
            m_initialCount = initialCount;
            Init();
        }

        public List<T> ActiveList() => m_activeList;
        public List<T> InactiveList() => m_inactiveList;

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

        public void Prepare(int count)
        {
            int needed = count - m_inactiveList.Count;
            if (needed > 0)
                m_inactiveList.Prepare(m_prefab, m_parent, needed, m_name);
        }

        public T Spawn()
        {
            return Spawn(Vector3.zero, false);
        }

        public T Spawn(Transform point)
        {
            return Spawn(point.position, true);
        }

        public T Spawn(Vector3 position, bool worldPosition = true)
        {
            return Spawn(position, worldPosition, out _);
        }

        public T Spawn(Vector3 position, bool worldPosition, out bool reused)
        {
            if (m_limitNumber > 0 && m_activeList.Count == m_limitNumber)
            {
                var oldest = m_activeList[0];
                MoveToInactive(oldest, 0);
            }

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

        public void AddOutsiders(List<T> sceneObjects)
        {
            for (int i = sceneObjects.Count - 1; i >= 0; i--)
                AddOutsider(sceneObjects[i]);
        }

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

        public void Release(T item, ConditionalDelegate condition)
        {
            if (item == null)
                return;

            Timers.WaitForCondition(condition, () => Release(item), item.GetInstanceID());
        }

        public void Release(GameObject item)
        {
            var component = FindComponent(item);
            Release(component);
        }

        public void ReleaseAll()
        {
            for (int i = m_activeList.Count - 1; i >= 0; i--)
                MoveToInactive(m_activeList[i], i);
        }

        public void DestroyAll()
        {
            DestroyList(m_activeList);
            DestroyList(m_inactiveList);
            m_activeList.Clear();
            m_inactiveList.Clear();
        }

        public void Destroy(T item)
        {
            if (item == null)
                return;

            m_activeList.Remove(item);
            m_inactiveList.Remove(item);
            DestroyObject(item.gameObject);
        }

        public T FindFromActive(T item)
        {
            return m_activeList.Contains(item) ? item : null;
        }

        public T FindFromInactive(T item)
        {
            return m_inactiveList.Contains(item) ? item : null;
        }

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

        public T GetFromActive(int index)
        {
            return index >= 0 && index < m_activeList.Count ? m_activeList[index] : null;
        }

        public void SetParent(Transform parent)
        {
            m_parent = parent;
        }

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
                Object.Destroy(gameObject);
            else
                Object.DestroyImmediate(gameObject);
        }
    }
}
