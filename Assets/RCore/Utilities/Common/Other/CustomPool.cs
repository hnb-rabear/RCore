/**
 * Author NBear - nbhung71711@gmail.com - 2017
 **/

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Common
{
    public class PoolsContainer<T> where T : Component
    {
        public Dictionary<int, CustomPool<T>> poolDict;
        public Transform container;
        private int mInitialNumber;
        private Dictionary<int, int> idOfAllClones; //Keep tracking the clone instance id and its prefab instance id

        public PoolsContainer(Transform pContainer)
        {
            container = pContainer;
            poolDict = new Dictionary<int, CustomPool<T>>();
            idOfAllClones = new Dictionary<int, int>();
        }

        public PoolsContainer(string pContainerName, int pInitialNumber = 1, Transform pParent = null)
        {
            var container = new GameObject(pContainerName);
            container.transform.SetParent(pParent);
            container.transform.localPosition = Vector3.zero;
            container.transform.rotation = Quaternion.identity;
            this.container = container.transform;
            poolDict = new Dictionary<int, CustomPool<T>>();
            idOfAllClones = new Dictionary<int, int>();
            mInitialNumber = pInitialNumber;
        }

        public CustomPool<T> Get(T pPrefab)
        {
            if (poolDict.ContainsKey(pPrefab.GameObjectId()))
                return poolDict[pPrefab.GameObjectId()];
            else
            {
                var pool = new CustomPool<T>(pPrefab, mInitialNumber, container.transform);
                poolDict.Add(pPrefab.GameObjectId(), pool);
                return pool;
            }
        }

        public void CreatePool(T pPrefab, List<T> pBuiltInObjs)
        {
            var pool = Get(pPrefab);
            pool.AddOutsiders(pBuiltInObjs);
        }

        public T Spawn(T prefab)
        {
            return Spawn(prefab, Vector3.zero);
        }

        public T Spawn(T prefab, Vector3 position)
        {
            var pool = Get(prefab);
            var clone = pool.Spawn(position, true);
            //Keep the trace of clone
            if (!idOfAllClones.ContainsKey(clone.GameObjectId()))
                idOfAllClones.Add(clone.GameObjectId(), prefab.GameObjectId());
            return clone;
        }

        public T Spawn(T prefab, Transform transform)
        {
            var pool = Get(prefab);
            var clone = pool.Spawn(transform);
            //Keep the trace of clone
            if (!idOfAllClones.ContainsKey(clone.GameObjectId()))
                idOfAllClones.Add(clone.GameObjectId(), prefab.GameObjectId());
            return clone;
        }

        public CustomPool<T> Add(T pPrefab)
        {
            if (!poolDict.ContainsKey(pPrefab.GameObjectId()))
            {
                var pool = new CustomPool<T>(pPrefab, mInitialNumber, container.transform);
                poolDict.Add(pPrefab.GameObjectId(), pool);
            }
            else
                Debug.Log($"Pool Prefab {pPrefab.name} has already existed!");
            return poolDict[pPrefab.GameObjectId()];
        }

        public void Add(CustomPool<T> pPool)
        {
            if (!poolDict.ContainsKey(pPool.Prefab.GameObjectId()))
                poolDict.Add(pPool.Prefab.GameObjectId(), pPool);
            else
            {
                var pool = poolDict[pPool.Prefab.GameObjectId()];
                //Merge two pool
                foreach (var obj in pPool.ActiveList)
                    if (!pool.ActiveList.Contains(obj))
                        pool.ActiveList.Add(obj);
                foreach (var obj in pPool.InactiveList)
                    if (!pool.InactiveList.Contains(obj))
                        pool.InactiveList.Add(obj);
            }
        }

        public List<T> GetActiveList()
        {
            var list = new List<T>();
            foreach (var pool in poolDict)
            {
                list.AddRange(pool.Value.ActiveList);
            }
            return list;
        }

        public void Release(T pObj)
        {
            if (idOfAllClones.ContainsKey(pObj.GameObjectId()))
                Release(idOfAllClones[pObj.GameObjectId()], pObj);
            else
            {
                foreach (var pool in poolDict)
                    pool.Value.Release(pObj);
            }
        }

        public void Release(T pPrefab, T pObj)
        {
            Release(pPrefab.GameObjectId(), pObj);
        }

        public void Release(int pPrefabId, T pObj)
        {
            if (poolDict.ContainsKey(pPrefabId))
                poolDict[pPrefabId].Release(pObj);
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log("We should not release obj by this way");
#endif
                foreach (var pool in poolDict)
                    pool.Value.Release(pObj);
            }
        }

        public void ReleaseAll()
        {
            foreach (var pool in poolDict)
            {
                pool.Value.ReleaseAll();
            }
        }

        public void Release(GameObject pObj)
        {
            foreach (var pool in poolDict)
            {
                pool.Value.Release(pObj);
            }
        }

        public T FindComponent(GameObject pObj)
        {
            foreach (var pool in poolDict)
            {
                var component = pool.Value.FindComponent(pObj);
                if (component != null)
                    return component;
            }
            return null;
        }

#if UNITY_EDITOR
        public void DrawOnEditor()
        {
            if (UnityEditor.EditorApplication.isPlaying && poolDict.Count > 0)
            {
                EditorHelper.BoxVertical(() =>
                {
                    foreach (var item in poolDict)
                    {
                        var pool = item.Value;
                        if (EditorHelper.HeaderFoldout($"{pool.Prefab.name} Pool {pool.ActiveList.Count}/{pool.InactiveList.Count} (Key:{item.Key})", item.Key.ToString()))
                            pool.DrawOnEditor();
                    }
                }, Color.white, true);
            }
        }
#endif
    }

    public class CustomPool<T> where T : Component
    {
        #region Members

        public Action<T> onSpawn;

        [SerializeField] protected T mPrefab;
        [SerializeField] protected Transform mParent;
        [SerializeField] protected string mName;
        [SerializeField] protected bool mPushToLastSibling;
        [SerializeField] protected bool mAutoRelocate;
        [SerializeField] protected int mLimitNumber;
        [SerializeField] private List<T> mActiveList = new List<T>();
        [SerializeField] private List<T> mInactiveList = new List<T>();

        public T Prefab { get { return mPrefab; } }
        public Transform Parent { get { return mParent; } }
        public string Name { get { return mName; } }
        public List<T> ActiveList { get { return mActiveList; } }
        public List<T> InactiveList { get { return mInactiveList; } }
        public bool pushToLastSibling { get { return mPushToLastSibling; } set { mPushToLastSibling = value; } }
        public int limitNumber { get { return mLimitNumber; } set { mLimitNumber = value; } }
        public Transform parent { get { return mParent; } }

        #endregion

        //====================================

        #region Public

        public CustomPool(T pPrefab, int pInitialCount, Transform pParent, string pName = "", bool pAutoRelocate = true)
        {
            mPrefab = pPrefab;
            mParent = pParent;
            mName = pName;
            mAutoRelocate = pAutoRelocate;

            if (string.IsNullOrEmpty(mName))
                mName = mPrefab.name;

            if (mParent == null)
            {
                GameObject temp = new GameObject();
                temp.name = string.Format("Pool_{0}", mName);
                mParent = temp.transform;
            }

            mActiveList = new List<T>();
            mInactiveList = new List<T>();
            mInactiveList.Prepare(mPrefab, pParent, pInitialCount, pPrefab.name);
            if (!pPrefab.gameObject.IsPrefab())
            {
                mInactiveList.Add(mPrefab);
                pPrefab.SetParent(pParent);
                pPrefab.transform.SetAsLastSibling();
                pPrefab.SetActive(false);
            }
        }

        public CustomPool(GameObject pPrefab, int pInitialCount, Transform pParent, bool pBuildinPrefab, string pName = "", bool pAutoRelocate = true)
        {
#if UNITY_2019_2_OR_NEWER
            pPrefab.TryGetComponent(out T component);
            mPrefab = component;
#else
            mPrefab = pPrefab.GetComponent<T>();
#endif
            mParent = pParent;
            mName = pName;
            mAutoRelocate = pAutoRelocate;

            if (string.IsNullOrEmpty(mName))
                mName = mPrefab.name;

            if (mParent == null)
            {
                GameObject temp = new GameObject();
                temp.name = string.Format("Pool_{0}", mName);
                mParent = temp.transform;
            }

            mActiveList = new List<T>();
            mInactiveList = new List<T>();
            mInactiveList.Prepare(mPrefab, pParent, pInitialCount, pPrefab.name);
            if (pBuildinPrefab)
            {
                mInactiveList.Add(mPrefab);
                pPrefab.transform.SetParent(pParent);
                pPrefab.transform.SetAsLastSibling();
                pPrefab.SetActive(false);
            }
        }

        public void Prepare(int pInitialCount)
        {
            int numberNeeded = pInitialCount - mInactiveList.Count;
            if (numberNeeded > 0)
            {
                var list = new List<T>();
                list.Prepare(mPrefab, mParent, pInitialCount, mPrefab.name);
                mInactiveList.AddRange(list);
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
            if (mLimitNumber > 0 && mActiveList.Count == mLimitNumber)
            {
                var activeItem = mActiveList[0];
                mInactiveList.Add(activeItem);
                mActiveList.Remove(activeItem);
            }

            int count = mInactiveList.Count;
            if (mAutoRelocate && count == 0)
                RelocateInactive();

            if (count > 0)
            {
                var item = mInactiveList[mInactiveList.Count - 1];
                if (pIsWorldPosition)
                    item.transform.position = position;
                else
                    item.transform.localPosition = position;
                Active(item, true, mInactiveList.Count - 1);

                if (onSpawn != null)
                    onSpawn(item);

                if (mPushToLastSibling)
                    item.transform.SetAsLastSibling();
                return item;
            }

            T newItem = Object.Instantiate(mPrefab, mParent);
            newItem.name = mName;
            mInactiveList.Add(newItem);

            return Spawn(position, pIsWorldPosition);
        }

        public T Spawn(Vector3 position, bool pIsWorldPosition, ref bool pReused)
        {
            if (mLimitNumber > 0 && mActiveList.Count == mLimitNumber)
            {
                var activeItem = mActiveList[0];
                mInactiveList.Add(activeItem);
                mActiveList.Remove(activeItem);
            }

            int count = mInactiveList.Count;
            if (mAutoRelocate && count == 0)
                RelocateInactive();

            if (count > 0)
            {
                var item = mInactiveList[mInactiveList.Count - 1];
                if (pIsWorldPosition)
                    item.transform.position = position;
                else
                    item.transform.localPosition = position;
                Active(item, true, mInactiveList.Count - 1);
                pReused = true;

                if (onSpawn != null)
                    onSpawn(item);

                if (mPushToLastSibling)
                    item.transform.SetAsLastSibling();
                return item;
            }

            T newItem = Object.Instantiate(mPrefab, mParent);
            newItem.name = mName;
            mInactiveList.Add(newItem);
            pReused = false;

            return Spawn(position, pIsWorldPosition, ref pReused);
        }

        public void AddOutsiders(List<T> pInSceneObjs)
        {
            for (int i = pInSceneObjs.Count - 1; i >= 0; i--)
                AddOutsider(pInSceneObjs[i]);
        }

        public void AddOutsider(T pInSceneObj)
        {
            if (mInactiveList == null)
                mInactiveList = new List<T>();
            if (mActiveList == null)
                mActiveList = new List<T>();

            if (mInactiveList.Contains(pInSceneObj)
                || mActiveList.Contains(pInSceneObj))
                return;

            if (pInSceneObj.gameObject.activeSelf)
                mActiveList.Add(pInSceneObj);
            else
                mInactiveList.Add(pInSceneObj);
            pInSceneObj.transform.SetParent(mParent);
        }

        public void Release(T pObj)
        {
            for (int i = 0; i < mActiveList.Count; i++)
            {
                if (ReferenceEquals(mActiveList[i], pObj))
                {
                    Active(mActiveList[i], false, i);
                    return;
                }
            }
        }

        public void Release(GameObject pObj)
        {
            for (int i = 0; i < mActiveList.Count; i++)
            {
                if (mActiveList[i].GameObjectId() == pObj.GetInstanceID())
                {
                    Active(mActiveList[i], false, i);
                    return;
                }
            }
        }

        public void ReleaseAll()
        {
            for (int i = 0; i < mActiveList.Count; i++)
            {
                var item = mActiveList[i];
                mInactiveList.Add(item);
                item.SetActive(false);
            }
            mActiveList.Clear();
        }

        public void DestroyAll()
        {
            while (mActiveList.Count > 0)
            {
                int index = mActiveList.Count - 1;
                Object.Destroy(mActiveList[index]);
                mActiveList.RemoveAt(index);
            }
            while (mInactiveList.Count > 0)
            {
                int index = mInactiveList.Count - 1;
                Object.Destroy(mInactiveList[index]);
                mInactiveList.RemoveAt(index);
            }
        }

        public void Destroy(T pItem)
        {
            mActiveList.Remove(pItem);
            mInactiveList.Remove(pItem);
            Object.Destroy(pItem);
        }

        public T FindFromActive(T t)
        {
            for (int i = 0; i < mActiveList.Count; i++)
            {
                var item = mActiveList[i];
                if (item == t)
                    return item;
            }
            return null;
        }

        public T FindComponent(GameObject pObj)
        {
            for (int i = 0; i < mActiveList.Count; i++)
            {
                if (mActiveList[i].gameObject == pObj)
                {
                    var temp = mActiveList[i];
                    return temp;
                }
            }
            for (int i = 0; i < mInactiveList.Count; i++)
            {
                if (mInactiveList[i].gameObject == pObj)
                {
                    var temp = mInactiveList[i];
                    return temp;
                }
            }
            return null;
        }

        public T GetFromActive(int pIndex)
        {
            if (pIndex < 0 || pIndex >= mActiveList.Count)
                return null;
            return mActiveList[pIndex];
        }

        public T FindFromInactive(T t)
        {
            for (int i = 0; i < mInactiveList.Count; i++)
            {
                var item = mInactiveList[i];
                if (item == t)
                    return item;
            }
            return null;
        }

        public void RelocateInactive()
        {
            for (int i = mActiveList.Count - 1; i >= 0; i--)
                if (!mActiveList[i].gameObject.activeSelf)
                    Active(mActiveList[i], false, i);
        }

        public void SetParent(Transform pParent)
        {
            mParent = pParent;
        }

        public void SetName(string pName)
        {
            mName = pName;
        }

        #endregion

        //========================================

        #region Private

        private void Active(T pItem, bool pValue, int index = -1)
        {
            if (pValue)
            {
                mActiveList.Add(pItem);
                if (index == -1)
                    mInactiveList.Remove(pItem);
                else
                    mInactiveList.RemoveAt(index);
            }
            else
            {
                mInactiveList.Add(pItem);
                if (index == -1)
                    mActiveList.Remove(pItem);
                else
                    mActiveList.RemoveAt(index);
            }
            pItem.SetActive(pValue);
        }

        #endregion

        //=========================================

#if UNITY_EDITOR
        public void DrawOnEditor()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                EditorHelper.BoxVertical(() =>
                {
                    if (mActiveList != null)
                        EditorHelper.ListReadonlyObjects(mName + "ActiveList", mActiveList, null, false);
                    if (mInactiveList != null)
                        EditorHelper.ListReadonlyObjects(mName + "InactiveList", mInactiveList, null, false);
                    if (EditorHelper.Button("Relocate"))
                        RelocateInactive();
                }, Color.white, true);
            }
        }
#endif
    }
}