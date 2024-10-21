/**
 * Author RadBear - nbhung71711@gmail.com - 2018
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
    [Serializable]
    public class CustomPool<T> where T : Component
    {
        public Action<T> onSpawn;

        [SerializeField] protected T m_Prefab;
        [SerializeField] protected Transform m_Parent;
        [SerializeField] protected string m_Name;
        [SerializeField] protected bool m_PushToLastSibling;
        [SerializeField] protected bool m_AutoRelocate;
        [SerializeField] protected int m_LimitNumber;
        [SerializeField] private List<T> m_ActiveList = new List<T>();
        [SerializeField] private List<T> m_InactiveList = new List<T>();

        public T Prefab => m_Prefab;
        public Transform Parent => m_Parent;
        public string Name => m_Name;
        public bool pushToLastSibling { get => m_PushToLastSibling; set => m_PushToLastSibling = value; }
        public int limitNumber { get => m_LimitNumber; set => m_LimitNumber = value; }
        protected bool m_Initialized;
        protected int m_InitialCount;

        public List<T> ActiveList() => m_ActiveList;
        public List<T> InactiveList() => m_InactiveList;

        public CustomPool() { }

        public CustomPool(T pPrefab, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            m_Prefab = pPrefab;
            m_Parent = pParent;
            m_Name = pName;
            m_AutoRelocate = pAutoRelocate;
            m_InitialCount = pInitialCount;
            Init();
        }

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
            if (!m_Prefab.gameObject.IsPrefab())
            {
                m_InactiveList.Add(m_Prefab);
                m_Prefab.SetParent(m_Parent);
                m_Prefab.transform.SetAsLastSibling();
                m_Prefab.SetActive(false);
            }
            m_Initialized = true;
        }

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

        public T Spawn()
        {
            return Spawn(Vector3.zero, false);
        }

        public T Spawn(Transform pPoint)
        {
            return Spawn(pPoint.position, true);
        }

        public T Spawn(Vector3 position, bool pIsWorldPosition)
        {
            return Spawn(position, pIsWorldPosition, out _);
        }

        public T Spawn(Vector3 position, bool pIsWorldPosition, out bool pReused)
        {
            if (m_LimitNumber > 0 && m_ActiveList.Count == m_LimitNumber)
            {
                var activeItem = m_ActiveList[0];
                m_InactiveList.Add(activeItem);
                m_ActiveList.Remove(activeItem);
            }

            int count = m_InactiveList.Count;
            if (m_AutoRelocate && count == 0)
            {
                RelocateInactive(out bool relocated);
                if (relocated)
                    count = m_InactiveList.Count;
            }

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

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                T newItem = (T)UnityEditor.PrefabUtility.InstantiatePrefab(m_Prefab, m_Parent);
                newItem.name = m_Name;
                m_InactiveList.Add(newItem);

            }
            else
#endif
            {
                T newItem = Object.Instantiate(m_Prefab, m_Parent);
                newItem.name = m_Name;
                m_InactiveList.Add(newItem);
            }
            pReused = false;

            return Spawn(position, pIsWorldPosition, out pReused);
        }

        public void AddOutsiders(List<T> pInSceneObjs)
        {
            for (int i = pInSceneObjs.Count - 1; i >= 0; i--)
                AddOutsider(pInSceneObjs[i]);
        }

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

        public void ReleaseAll()
        {
            int count = m_ActiveList.Count;
            for (int i = 0; i < count; i++)
            {
                var item = m_ActiveList[i];
                m_InactiveList.Add(item);
                item.SetActive(false);
            }
            m_ActiveList.Clear();
        }

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

        public void Destroy(T pItem)
        {
            m_ActiveList.Remove(pItem);
            m_InactiveList.Remove(pItem);
            if (Application.isPlaying)
                Object.Destroy(pItem.gameObject);
            else
                Object.DestroyImmediate(pItem.gameObject);
        }

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

        public T GetFromActive(int pIndex)
        {
            if (pIndex < 0 || pIndex >= m_ActiveList.Count)
                return null;
            return m_ActiveList[pIndex];
        }

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

        public void SetParent(Transform pParent)
        {
            m_Parent = pParent;
        }

        public void SetName(string pName)
        {
            m_Name = pName;
        }

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
            pItem.SetActive(pValue);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Used in OnInspectorGUI() of CustomEditor
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