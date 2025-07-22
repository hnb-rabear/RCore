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
    /// A generic, serializable object pooling system for Unity Components.
    /// It's designed to improve performance by recycling objects (like particle effects, bullets, or UI elements)
    /// instead of constantly instantiating and destroying them.
    /// </summary>
    /// <typeparam name="T">The type of Component this pool will manage.</typeparam>
    [Serializable]
    public class CustomPool<T> where T : Component
    {
        /// <summary>
        /// An action invoked whenever an object is spawned from the pool.
        /// Useful for re-initializing the object's state (e.g., resetting health, animation, etc.).
        /// </summary>
        public Action<T> onSpawn;

        [SerializeField] protected T m_Prefab;
        [SerializeField] protected Transform m_Parent;
        [SerializeField] protected string m_Name;
        [SerializeField] protected bool m_PushToLastSibling;
        [SerializeField] protected bool m_AutoRelocate;
        [SerializeField] protected int m_LimitNumber;
        [SerializeField] private List<T> m_ActiveList = new List<T>();
        [SerializeField] private List<T> m_InactiveList = new List<T>();

        /// <summary>
        /// The template Component used to create new objects when the pool is empty.
        /// </summary>
        public T Prefab => m_Prefab;
        /// <summary>
        /// The Transform that will parent all pooled objects, keeping the hierarchy organized.
        /// </summary>
        public Transform Parent => m_Parent;
        /// <summary>
        /// The name assigned to newly instantiated objects for easier identification in the hierarchy.
        /// </summary>
        public string Name => m_Name;
        /// <summary>
        /// If true, moves the spawned object to the bottom of the hierarchy under its parent. Useful for UI elements.
        /// </summary>
        public bool pushToLastSibling { get => m_PushToLastSibling; set => m_PushToLastSibling = value; }
        /// <summary>
        /// An optional limit on the number of active objects. When the limit is reached,
        /// the oldest active object will be recycled to make room for a new one.
        /// </summary>
        public int limitNumber { get => m_LimitNumber; set => m_LimitNumber = value; }
        protected bool m_Initialized;
        protected int m_InitialCount;

        /// <summary>
        /// Gets the list of all currently active objects managed by the pool.
        /// </summary>
        public List<T> ActiveList() => m_ActiveList;
        /// <summary>
        /// Gets the list of all currently inactive (pooled) objects available for reuse.
        /// </summary>
        public List<T> InactiveList() => m_InactiveList;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CustomPool() { }

        /// <summary>
        /// Creates and initializes a new pool.
        /// </summary>
        /// <param name="pPrefab">The component prefab to be pooled.</param>
        /// <param name="pInitialCount">The number of objects to pre-warm the pool with.</param>
        /// <param name="pParent">The parent transform for all pooled objects.</param>
        /// <param name="pName">The name for the pool container and its objects.</param>
        /// <param name="pAutoRelocate">If true, automatically moves objects that are inactive in the hierarchy to the inactive list.</param>
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
        /// Creates and initializes a new pool from a GameObject prefab.
        /// </summary>
        public CustomPool(GameObject pPrefab, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            if (pPrefab != null)
                pPrefab.TryGetComponent(out m_Prefab);
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            Init();
        }

        /// <summary>
        /// Initializes the pool, creating the parent container and pre-warming if specified.
        /// </summary>
        protected void Init()
        {
            if (m_Initialized)
                return;

            if (m_Prefab == null)
            {
                Debug.LogError("CustomPool cannot be initialized with a null prefab!");
                return;
            }

            if (string.IsNullOrEmpty(m_Name))
                m_Name = m_Prefab.name;

            if (m_Parent == null)
            {
                GameObject temp = new GameObject($"Pool_{m_Name}");
                m_Parent = temp.transform;
            }

            m_ActiveList = new List<T>();
            m_InactiveList = new List<T>();
            m_InactiveList.Prepare(m_Prefab, m_Parent, m_InitialCount, m_Prefab.name);
            
            // If the prefab is actually an instance in the scene, add it to the pool.
            if (!m_Prefab.gameObject.IsPrefab())
            {
                m_InactiveList.Add(m_Prefab);
                m_Prefab.transform.SetParent(m_Parent);
                m_Prefab.transform.SetAsLastSibling();
                m_Prefab.gameObject.SetActive(false);
            }
            m_Initialized = true;
        }

        /// <summary>
        /// Pre-instantiates a specified number of objects to prevent hitches during gameplay.
        /// </summary>
        /// <param name="pInitialCount">The total number of inactive objects the pool should have ready.</param>
        public void Prepare(int pInitialCount)
        {
            int numberNeeded = pInitialCount - m_InactiveList.Count;
            if (numberNeeded > 0)
            {
                m_InactiveList.Prepare(m_Prefab, m_Parent, numberNeeded, m_Prefab.name);
            }
        }

        /// <summary>
        /// Spawns an object from the pool at the parent's local origin.
        /// </summary>
        /// <returns>A component of type T from the pool.</returns>
        public T Spawn()
        {
            return Spawn(Vector3.zero, false);
        }

        /// <summary>
        /// Spawns an object from the pool at the specified transform's world position.
        /// </summary>
        /// <param name="pPoint">The transform to spawn the object at.</param>
        /// <returns>A component of type T from the pool.</returns>
        public T Spawn(Transform pPoint)
        {
            return Spawn(pPoint.position, true);
        }

        /// <summary>
        /// Spawns an object from the pool at a given position.
        /// </summary>
        /// <param name="position">The position to spawn the object at.</param>
        /// <param name="pIsWorldPosition">Is the provided position in world space or local space?</param>
        /// <returns>A component of type T from the pool.</returns>
        public T Spawn(Vector3 position, bool pIsWorldPosition)
        {
            return Spawn(position, pIsWorldPosition, out _);
        }

        /// <summary>
        /// Spawns an object from the pool at a given position and indicates if it was reused.
        /// </summary>
        /// <param name="position">The position to spawn the object at.</param>
        /// <param name="pIsWorldPosition">Is the provided position in world space or local space?</param>
        /// <param name="pReused">Outputs true if an existing object was reused, false if a new one was instantiated.</param>
        /// <returns>A component of type T from the pool.</returns>
        public T Spawn(Vector3 position, bool pIsWorldPosition, out bool pReused)
        {
            // If the pool has a limit, recycle the oldest active object.
            if (m_LimitNumber > 0 && m_ActiveList.Count >= m_LimitNumber)
            {
                var activeItem = m_ActiveList[0];
                Release(activeItem);
            }

            // If auto-relocating, check for any active objects that were externally deactivated.
            int count = m_InactiveList.Count;
            if (m_AutoRelocate && count == 0)
            {
                RelocateInactive(out bool relocated);
                if (relocated)
                    count = m_InactiveList.Count;
            }

            // Reuse an object from the inactive list if available.
            if (count > 0)
            {
                var item = m_InactiveList[count - 1];
                if (item == null)
                {
                    // This can happen if an object was destroyed externally.
                    m_InactiveList.RemoveAt(count - 1);
                    pReused = false;
                    return Spawn(position, pIsWorldPosition, out pReused);
                }
                
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
            T newItem;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                newItem = (T)UnityEditor.PrefabUtility.InstantiatePrefab(m_Prefab, m_Parent);
            }
            else
#endif
            {
                newItem = Object.Instantiate(m_Prefab, m_Parent);
            }

            newItem.name = m_Name;
            m_InactiveList.Add(newItem);

            pReused = false;
            // Recursively call Spawn to use the newly created item.
            return Spawn(position, pIsWorldPosition, out pReused);
        }

        /// <summary>
        /// Adds a list of objects that were already in the scene to be managed by this pool.
        /// </summary>
        /// <param name="pInSceneObjs">A list of objects to add.</param>
        public void AddOutsiders(List<T> pInSceneObjs)
        {
            for (int i = pInSceneObjs.Count - 1; i >= 0; i--)
                AddOutsider(pInSceneObjs[i]);
        }

        /// <summary>
        /// Adds a single object that was already in the scene to be managed by this pool.
        /// </summary>
        /// <param name="pInSceneObj">The object to add.</param>
        public void AddOutsider(T pInSceneObj)
        {
            if (m_InactiveList.Contains(pInSceneObj) || m_ActiveList.Contains(pInSceneObj))
                return;

            if (pInSceneObj.gameObject.activeSelf)
                m_ActiveList.Add(pInSceneObj);
            else
                m_InactiveList.Add(pInSceneObj);
            pInSceneObj.transform.SetParent(m_Parent);
        }

        /// <summary>
        /// Returns an active object to the pool, making it inactive and available for reuse.
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
        /// Returns an active object to the pool after a specified delay.
        /// </summary>
        /// <param name="pObj">The component instance to release.</param>
        /// <param name="pDelay">The delay in seconds before releasing.</param>
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
        /// Returns an active object to the pool when a specified condition is met.
        /// </summary>
        /// <param name="pObj">The component instance to release.</param>
        /// <param name="pCondition">The delegate that must return true to trigger the release.</param>
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
        /// Convenience method to release an object via its GameObject.
        /// </summary>
        /// <param name="pObj">The GameObject to release.</param>
        public void Release(GameObject pObj)
        {
            for (int i = m_ActiveList.Count - 1; i >= 0; i--)
            {
                if (m_ActiveList[i].gameObject.GetInstanceID() == pObj.GetInstanceID())
                {
                    Active(m_ActiveList[i], false, i);
                    return;
                }
            }
        }
        
        /// <summary>
        /// Convenience method to release a GameObject after a delay.
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
        /// Convenience method to release a GameObject when a condition is met.
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
		        triggerCondition = pCondition
	        });
        }

        /// <summary>
        /// Deactivates and returns all active objects to the pool.
        /// </summary>
        public void ReleaseAll()
        {
            for (int i = m_ActiveList.Count - 1; i >= 0; i--)
            {
                var item = m_ActiveList[i];
                Active(item, false, i);
            }
        }

        /// <summary>
        /// Permanently destroys every object managed by this pool (both active and inactive) and clears the lists.
        /// </summary>
        public void DestroyAll()
        {
            foreach (var item in m_ActiveList.Concat(m_InactiveList))
            {
                if (item != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(item.gameObject);
                    else
                        Object.DestroyImmediate(item.gameObject);
                }
            }
            m_ActiveList.Clear();
            m_InactiveList.Clear();
        }

        /// <summary>
        /// Removes a specific item from the pool's tracking and destroys its GameObject.
        /// </summary>
        /// <param name="pItem">The item to destroy.</param>
        public void Destroy(T pItem)
        {
            m_ActiveList.Remove(pItem);
            m_InactiveList.Remove(pItem);
            if (pItem != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(pItem.gameObject);
                else
                    Object.DestroyImmediate(pItem.gameObject);
            }
        }

        /// <summary>
        /// Finds a specific component instance within the active list.
        /// </summary>
        public T FindFromActive(T t)
        {
            return m_ActiveList.FirstOrDefault(item => item == t);
        }

        /// <summary>
        /// Finds the pooled component associated with a given GameObject.
        /// </summary>
        public T FindComponent(GameObject pObj)
        {
            return m_ActiveList.Concat(m_InactiveList).FirstOrDefault(item => item.gameObject == pObj);
        }
        
        /// <summary>
        /// Retrieves an active object by its index in the active list.
        /// </summary>
        public T GetFromActive(int pIndex)
        {
            if (pIndex < 0 || pIndex >= m_ActiveList.Count)
                return null;
            return m_ActiveList[pIndex];
        }

        /// <summary>
        /// Finds a specific component instance within the inactive list.
        /// </summary>
        public T FindFromInactive(T t)
        {
            return m_InactiveList.FirstOrDefault(item => item == t);
        }

        /// <summary>
        /// Scans the active list for any objects that have been deactivated externally and moves them to the inactive list.
        /// </summary>
        private void RelocateInactive(out bool relocated)
        {
            relocated = false;
            for (int i = m_ActiveList.Count - 1; i >= 0; i--)
            {
                if (!m_ActiveList[i].gameObject.activeSelf)
                {
                    Active(m_ActiveList[i], false, i);
                    relocated = true;
                }
            }
        }

        /// <summary>
        /// Sets the parent transform for the pool at runtime.
        /// </summary>
        public void SetParent(Transform pParent)
        {
            m_Parent = pParent;
        }

        /// <summary>
        /// Sets the name for newly instantiated objects at runtime.
        /// </summary>
        public void SetName(string pName)
        {
            m_Name = pName;
        }
        
        /// <summary>
        /// Internal method to handle moving an item between the active and inactive lists and setting its GameObject's active state.
        /// </summary>
        private void Active(T pItem, bool pValue, int index = -1)
        {
            if (pValue)
            {
                if (index != -1)
                    m_InactiveList.RemoveAt(index);
                else
                    m_InactiveList.Remove(pItem);
                m_ActiveList.Add(pItem);
            }
            else
            {
                if (index != -1)
                    m_ActiveList.RemoveAt(index);
                else
                    m_ActiveList.Remove(pItem);
                m_InactiveList.Add(pItem);
            }
            pItem.gameObject.SetActive(pValue);
        }

#if UNITY_EDITOR
        /// <summary>
        /// An editor-only method intended to be called from a CustomEditor's `OnInspectorGUI`
        /// to display the active and inactive lists for easy debugging while in play mode.
        /// </summary>
        public void DrawOnEditor()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                EditorHelper.BoxVertical(() =>
                {
                    if (m_ActiveList != null)
                        EditorHelper.ListReadonlyObjects(m_Name + " ActiveList", m_ActiveList, null, false);
                    if (m_InactiveList != null)
                        EditorHelper.ListReadonlyObjects(m_Name + " InactiveList", m_InactiveList, null, false);
                }, Color.white, true);
            }
        }
#endif
    }
}