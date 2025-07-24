/**
 * Author HNB-RaBear - 2018
 **/

#if UNITY_EDITOR
using RCore.Editor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore
{
    /// <summary>
    /// A generic object pooling system for Unity Components.
    /// It manages the lifecycle of objects (creation, activation, deactivation) to reduce the performance
    /// overhead of frequent instantiation and destruction. This class manages a pool for a single prefab type.
    /// </summary>
    /// <typeparam name="T">The type of Component to be pooled. Must inherit from UnityEngine.Component.</typeparam>
    [Serializable]
    public class CustomPool<T> where T : Component
    {
        /// <summary>
        /// An action that is invoked whenever a new item is spawned from the pool.
        /// Useful for re-initializing the state of a reused object.
        /// </summary>
        public Action<T> onSpawn;

        /// <summary>The component prefab used to create new instances for the pool.</summary>
        [SerializeField] protected T m_Prefab;
        /// <summary>The parent transform under which all pooled objects (active and inactive) will be organized in the hierarchy.</summary>
        [SerializeField] protected Transform m_Parent;
        /// <summary>The name to be assigned to all GameObjects instantiated by this pool.</summary>
        [SerializeField] protected string m_Name;
        /// <summary>If true, spawned objects will be moved to the last position in their parent's hierarchy.</summary>
        [SerializeField] protected bool m_PushToLastSibling;
        /// <summary>If true, the pool will automatically search for and reclaim active objects that have been manually set to inactive in the hierarchy.</summary>
        [SerializeField] protected bool m_AutoRelocate;
        /// <summary>If greater than 0, this will cap the number of active objects. When the limit is reached, spawning a new object will force the oldest active one to be released.</summary>
        [SerializeField] protected int m_LimitNumber;
        /// <summary>The list of currently active (in-use) objects.</summary>
        [SerializeField] private List<T> m_ActiveList = new List<T>();
        /// <summary>The list of currently inactive (available for reuse) objects.</summary>
        [SerializeField] private List<T> m_InactiveList = new List<T>();

        /// <summary>Gets the prefab used by this pool to create new instances.</summary>
        public T Prefab => m_Prefab;
        /// <summary>Gets the parent transform where pooled objects are stored.</summary>
        public Transform Parent => m_Parent;
        /// <summary>Gets the name assigned to new instances created by the pool.</summary>
        public string Name => m_Name;
        /// <summary>Gets or sets a value indicating whether spawned objects will be moved to the last position in their parent's hierarchy.</summary>
        public bool pushToLastSibling { get => m_PushToLastSibling; set => m_PushToLastSibling = value; }
        /// <summary>Gets or sets a value that limits the number of active objects. When the limit is reached, the oldest active object will be reused.</summary>
        public int limitNumber { get => m_LimitNumber; set => m_LimitNumber = value; }
        /// <summary>A flag indicating whether the pool has been initialized.</summary>
        protected bool m_Initialized;
        /// <summary>The number of objects to create when the pool is first initialized.</summary>
        protected int m_InitialCount;

        /// <summary>Gets a direct reference to the list of active objects.</summary>
        public List<T> ActiveList() => m_ActiveList;
        /// <summary>Gets a direct reference to the list of inactive (available) objects.</summary>
        public List<T> InactiveList() => m_InactiveList;

        public CustomPool() { }

        /// <summary>
        /// Initializes a new instance of the CustomPool class.
        /// </summary>
        /// <param name="pPrefab">The component prefab to be pooled.</param>
        /// <param name="pInitialCount">The number of instances to pre-instantiate.</param>
        /// <param name="pParent">The parent transform for the pooled objects.</param>
        /// <param name="pName">A name for the new instances. Defaults to the prefab's name.</param>
        /// <param name="pAutoRelocate">If true, the pool will automatically move inactive objects from the active list to the inactive list.</param>
        public CustomPool(T pPrefab, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            m_Prefab = pPrefab;
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            m_InitialCount = pInitialCount;
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the CustomPool class from a GameObject prefab.
        /// </summary>
        /// <param name="pPrefab">The GameObject prefab that has the component of type T.</param>
        /// <param name="pInitialCount">The number of instances to pre-instantiate.</param>
        /// <param name="pParent">The parent transform for the pooled objects.</param>
        /// <param name="pName">A name for the new instances. Defaults to the prefab's name.</param>
        /// <param name="pAutoRelocate">If true, the pool will automatically move inactive objects from the active list to the inactive list.</param>
        public CustomPool(GameObject pPrefab, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
#if UNITY_2019_2_OR_NEWER
            pPrefab.TryGetComponent(out m_Prefab);
#else
            m_Prefab = pPrefab.GetComponent<T>();
#endif
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            Init();
        }

        /// <summary>
        /// Initializes the pool, creates a parent object if one isn't provided, and pre-warms the pool with initial instances.
        /// </summary>
        protected void Init()
        {
            if (m_Initialized)
                return;

            if (string.IsNullOrEmpty(m_Name))
                m_Name = m_Prefab.name;

            if (m_Parent == null)
            {
                GameObject temp = new GameObject();
                temp.name = $"Pool_{m_Name}";
                m_Parent = temp.transform;
            }

            m_ActiveList = new List<T>();
            m_InactiveList = new List<T>();
            m_InactiveList.Prepare(m_Prefab, m_Parent, m_InitialCount, m_Prefab.name);
            // If the provided "prefab" is actually an object in the scene, add it to the pool to be managed.
            if (!m_Prefab.gameObject.IsPrefab())
            {
                m_InactiveList.Add(m_Prefab);
                m_Prefab.SetParent(m_Parent);
                m_Prefab.transform.SetAsLastSibling();
                m_Prefab.gameObject.SetActive(false);
            }
            m_Initialized = true;
        }

        /// <summary>
        /// Ensures the pool has at least a certain number of inactive instances available by creating more if needed.
        /// </summary>
        /// <param name="pInitialCount">The desired number of available instances.</param>
        public void Prepare(int pInitialCount)
        {
            int numberNeeded = pInitialCount - m_InactiveList.Count;
            if (numberNeeded > 0)
            {
                var list = new List<T>();
                list.Prepare(m_Prefab, m_Parent, pInitialCount, m_Prefab.name);
                m_InactiveList.AddRange(list);
            }
        }

        /// <summary>Spawns an item from the pool at the parent's origin.</summary>
        /// <returns>An active instance of the pooled component.</returns>
        public T Spawn()
        {
            return Spawn(Vector3.zero, false);
        }

        /// <summary>Spawns an item from the pool at the position of a given Transform.</summary>
        /// <param name="pPoint">The transform whose world position will be matched.</param>
        /// <returns>An active instance of the pooled component.</returns>
        public T Spawn(Transform pPoint)
        {
            return Spawn(pPoint.position, true);
        }

        /// <summary>Spawns an item from the pool at a specific position.</summary>
        /// <param name="position">The position to spawn the object at.</param>
        /// <param name="pIsWorldPosition">If true, the position is treated as world space; otherwise, local space.</param>
        /// <returns>An active instance of the pooled component.</returns>
        public T Spawn(Vector3 position, bool pIsWorldPosition)
        {
            return Spawn(position, pIsWorldPosition, out _);
        }

        /// <summary>
        /// The core method for retrieving an object from the pool.
        /// It reuses an inactive object if available, or creates a new one if not.
        /// </summary>
        /// <param name="position">The position to spawn the object at.</param>
        /// <param name="pIsWorldPosition">If true, the position is treated as world space; otherwise, local space.</param>
        /// <param name="pReused">An out parameter indicating if the returned object was reused from the pool (true) or newly instantiated (false).</param>
        /// <returns>An active instance of the pooled component.</returns>
        public T Spawn(Vector3 position, bool pIsWorldPosition, out bool pReused)
        {
            // If a limit is set and reached, recycle the oldest active item.
            if (m_LimitNumber > 0 && m_ActiveList.Count == m_LimitNumber)
            {
                var activeItem = m_ActiveList[0];
                m_InactiveList.Add(activeItem);
                m_ActiveList.Remove(activeItem);
            }

            int count = m_InactiveList.Count;
            // If auto-relocate is on and we're out of items, check for any active objects that were manually deactivated.
            if (m_AutoRelocate && count == 0)
            {
                RelocateInactive(out bool relocated);
                if (relocated)
                    count = m_InactiveList.Count;
            }

            // If an inactive object is available, reuse it.
            if (count > 0)
            {
                var item = m_InactiveList[count - 1];
                if (pIsWorldPosition)
                    item.transform.position = position;
                else
                    item.transform.localPosition = position;
                Active(item, true, count - 1);
                pReused = true;

                onSpawn?.Invoke(item);

                if (m_PushToLastSibling)
                    item.transform.SetAsLastSibling();
                return item;
            }

            // If no inactive objects are available, instantiate a new one.
#if UNITY_EDITOR
            // In the editor, use PrefabUtility to maintain the prefab connection.
            if (!Application.isPlaying)
            {
                T newItem = (T)UnityEditor.PrefabUtility.InstantiatePrefab(m_Prefab, m_Parent);
                newItem.name = m_Name;
                m_InactiveList.Add(newItem);

            }
            else
#endif
            {
                // At runtime, use the standard Instantiate method.
                T newItem = Object.Instantiate(m_Prefab, m_Parent);
                newItem.name = m_Name;
                m_InactiveList.Add(newItem);
            }
            pReused = false;

            // Recursively call Spawn to use the newly created item.
            return Spawn(position, pIsWorldPosition, out pReused);
        }

        /// <summary>
        /// Adds a list of existing scene objects to be managed by this pool.
        /// </summary>
        /// <param name="pInSceneObjs">A list of components already in the scene.</param>
        public void AddOutsiders(List<T> pInSceneObjs)
        {
            for (int i = pInSceneObjs.Count - 1; i >= 0; i--)
                AddOutsider(pInSceneObjs[i]);
        }

        /// <summary>
        /// Adds a single existing scene object to be managed by this pool. The object will be reparented.
        /// </summary>
        /// <param name="pInSceneObj">A component instance already in the scene.</param>
        public void AddOutsider(T pInSceneObj)
        {
            if (m_InactiveList.Contains(pInSceneObj)
                || m_ActiveList.Contains(pInSceneObj))
                return;

            if (pInSceneObj.gameObject.activeSelf)
                m_ActiveList.Add(pInSceneObj);
            else
                m_InactiveList.Add(pInSceneObj);
            pInSceneObj.transform.SetParent(m_Parent);
        }

        /// <summary>
        /// Deactivates an object and returns it to the inactive pool.
        /// </summary>
        /// <param name="pObj">The component instance to release.</param>
        public void Release(T pObj)
        {
            for (int i = 0; i < m_ActiveList.Count; i++)
            {
                if (ReferenceEquals(m_ActiveList[i], pObj))
                {
                    Active(m_ActiveList[i], false, i);
                    return;
                }
            }
        }

        /// <summary>
        /// Releases an object back to the pool after a specified delay.
        /// </summary>
        /// <param name="pObj">The component instance to release.</param>
        /// <param name="pDelay">The delay in seconds.</param>
        public void Release(T pObj, float pDelay)
        {
            TimerEventsInScene.Instance.WaitForSeconds(new CountdownEvent()
            {
                id = pObj.GetInstanceID(),
                onTimeOut = s =>
                {
                    if (pObj != null) Release(pObj);
                },
                waitTime = pDelay
            });
        }

        /// <summary>
        /// Releases an object back to the pool when a specified condition is met.
        /// </summary>
        /// <param name="pObj">The component instance to release.</param>
        /// <param name="pCondition">The delegate that returns true when the object should be released.</param>
        public void Release(T pObj, ConditionalDelegate pCondition)
        {
            TimerEventsInScene.Instance.WaitForCondition(new ConditionEvent()
            {
                id = pObj.GetInstanceID(),
                onTrigger = () =>
                {
                    if (pObj != null) Release(pObj);
                },
                triggerCondition = pCondition
            });
        }

        /// <summary>
        /// Deactivates an object and returns it to the inactive pool, finding it by its GameObject.
        /// </summary>
        /// <param name="pObj">The GameObject of the instance to release.</param>
        public void Release(GameObject pObj)
        {
            for (int i = 0; i < m_ActiveList.Count; i++)
            {
                if (m_ActiveList[i].gameObject.GetInstanceID() == pObj.GetInstanceID())
                {
                    Active(m_ActiveList[i], false, i);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Releases an object back to the pool after a specified delay, finding it by its GameObject.
        /// </summary>
        public void Release(GameObject pObj, float pDelay)
        {
            TimerEventsInScene.Instance.WaitForSeconds(new CountdownEvent()
            {
                id = pObj.GetInstanceID(),
                onTimeOut = s =>
                {
                    if (pObj != null) Release(pObj);
                },
                waitTime = pDelay
            });
        }
        
        /// <summary>
        /// Releases an object back to the pool when a specified condition is met, finding it by its GameObject.
        /// </summary>
        public void Release(GameObject pObj, ConditionalDelegate pCondition)
        {
            TimerEventsInScene.Instance.WaitForCondition(new ConditionEvent()
            {
                id = pObj.GetInstanceID(),
                onTrigger = () =>
                {
                    if (pObj != null) Release(pObj);
                },
                triggerCondition = pCondition,
            });
        }

        /// <summary>
        /// Releases all currently active objects back to the pool.
        /// </summary>
        public void ReleaseAll()
        {
            int count = m_ActiveList.Count;
            for (int i = 0; i < count; i++)
            {
                var item = m_ActiveList[i];
                m_InactiveList.Add(item);
                item.gameObject.SetActive(false);
            }
            m_ActiveList.Clear();
        }

        /// <summary>
        /// Destroys all objects managed by this pool (both active and inactive) and clears the lists.
        /// </summary>
        public void DestroyAll()
        {
            foreach (var item in m_ActiveList.Concat(m_InactiveList))
            {
                if (Application.isPlaying)
                    Object.Destroy(item.gameObject);
                else
                    Object.DestroyImmediate(item.gameObject);
            }
            m_ActiveList.Clear();
            m_InactiveList.Clear();
        }

        /// <summary>
        /// Removes a specific item from the pool's tracking and destroys its GameObject.
        /// </summary>
        /// <param name="pItem">The component instance to destroy.</param>
        public void Destroy(T pItem)
        {
            m_ActiveList.Remove(pItem);
            m_InactiveList.Remove(pItem);
            if (Application.isPlaying)
                Object.Destroy(pItem.gameObject);
            else
                Object.DestroyImmediate(pItem.gameObject);
        }

        /// <summary>
        /// Finds a specific item within the active list.
        /// </summary>
        /// <param name="t">The item to find.</param>
        /// <returns>The item if found, otherwise null.</returns>
        public T FindFromActive(T t)
        {
            for (int i = 0; i < m_ActiveList.Count; i++)
            {
                var item = m_ActiveList[i];
                if (item == t)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Finds the component associated with a given GameObject by searching both active and inactive lists.
        /// </summary>
        /// <param name="pObj">The GameObject to search for.</param>
        /// <returns>The component instance if found, otherwise null.</returns>
        public T FindComponent(GameObject pObj)
        {
            for (int i = 0; i < m_ActiveList.Count; i++)
            {
                if (m_ActiveList[i].gameObject == pObj)
                {
                    var temp = m_ActiveList[i];
                    return temp;
                }
            }
            for (int i = 0; i < m_InactiveList.Count; i++)
            {
                if (m_InactiveList[i].gameObject == pObj)
                {
                    var temp = m_InactiveList[i];
                    return temp;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves an active item by its index in the active list.
        /// </summary>
        /// <param name="pIndex">The index of the item to retrieve.</param>
        /// <returns>The component instance at the index, or null if the index is out of bounds.</returns>
        public T GetFromActive(int pIndex)
        {
            if (pIndex < 0 || pIndex >= m_ActiveList.Count)
                return null;
            return m_ActiveList[pIndex];
        }

        /// <summary>
        /// Finds a specific item within the inactive list.
        /// </summary>
        /// <param name="t">The item to find.</param>
        /// <returns>The item if found, otherwise null.</returns>
        public T FindFromInactive(T t)
        {
            for (int i = 0; i < m_InactiveList.Count; i++)
            {
                var item = m_InactiveList[i];
                if (item == t)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// A helper method that finds objects in the active list that have been externally deactivated
        /// (i.e., `gameObject.SetActive(false)` was called elsewhere) and moves them to the inactive list.
        /// </summary>
        /// <param name="relocated">An out parameter that is true if at least one object was relocated.</param>
        private void RelocateInactive(out bool relocated)
        {
            relocated = false;
            int count = m_ActiveList.Count;
            for (int i = count - 1; i >= 0; i--)
                if (!m_ActiveList[i].gameObject.activeSelf)
                {
                    Active(m_ActiveList[i], false, i);
                    relocated = true;
                }
        }

        /// <summary>Sets or changes the parent transform for this pool.</summary>
        public void SetParent(Transform pParent)
        {
            m_Parent = pParent;
        }

        /// <summary>Sets or changes the base name for objects instantiated by this pool.</summary>
        public void SetName(string pName)
        {
            m_Name = pName;
        }

        /// <summary>
        /// Internal helper method to move an item between the active and inactive lists and set its GameObject's active state.
        /// </summary>
        /// <param name="pItem">The item to move.</param>
        /// <param name="pValue">The new active state (true for active, false for inactive).</param>
        /// <param name="index">The current index of the item in its source list, for performance.</param>
        private void Active(T pItem, bool pValue, int index = -1)
        {
            if (pValue)
            {
                m_ActiveList.Add(pItem);
                if (index == -1)
                    m_InactiveList.Remove(pItem);
                else
                    m_InactiveList.RemoveAt(index);
            }
            else
            {
                m_InactiveList.Add(pItem);
                if (index == -1)
                    m_ActiveList.Remove(pItem);
                else
                    m_ActiveList.RemoveAt(index);
            }
            pItem.gameObject.SetActive(pValue);
        }

#if UNITY_EDITOR
        /// <summary>
        /// A helper method to draw a debug view of the pool's state in a custom editor inspector.
        /// This should be called from the OnInspectorGUI method of a custom editor script.
        /// </summary>
        public void DrawOnEditor()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                EditorHelper.BoxVertical(() =>
                {
                    if (m_ActiveList != null)
                        EditorHelper.ListReadonlyObjects(m_Name + "ActiveList", m_ActiveList, null, false);
                    if (m_InactiveList != null)
                        EditorHelper.ListReadonlyObjects(m_Name + "InactiveList", m_InactiveList, null, false);
                }, Color.white, true);
            }
        }
#endif
    }
}