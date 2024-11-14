/**
 * Author HNB-RaBear - 2019
 **/

#if UNITY_EDITOR
using RCore.Editor;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	public class PoolsContainer<T> where T : Component
	{
		public Dictionary<int, CustomPool<T>> poolDict = new Dictionary<int, CustomPool<T>>();
		public Transform container;
		public int limitNumber;
		private int m_initialNumber;
		private Dictionary<int, int> m_idOfAllClones = new Dictionary<int, int>(); //Keep tracking the clone instance id and its prefab instance id

		public PoolsContainer(Transform pContainer)
		{
			container = pContainer;
		}

		public PoolsContainer(string pContainerName, int pInitialNumber = 1, Transform pParent = null)
		{
			var container = new GameObject(pContainerName);
			container.transform.SetParent(pParent);
			container.transform.localPosition = Vector3.zero;
			container.transform.rotation = Quaternion.identity;
			this.container = container.transform;
			m_initialNumber = pInitialNumber;
		}

		public CustomPool<T> Get(T pPrefab)
		{
			if (pPrefab == null)
				return null;
			int prefabInstanceId = pPrefab.gameObject.GetInstanceID();
			if (poolDict.TryGetValue(prefabInstanceId, out var prefabPool))
				return prefabPool;
			var pool = new CustomPool<T>(pPrefab, m_initialNumber, container.transform);
			pool.limitNumber = limitNumber;
			poolDict.Add(prefabInstanceId, pool);
			return pool;
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

		public T Spawn(T prefab, Vector3 position, bool pIsWorldPosition = true)
		{
			var pool = Get(prefab);
			var clone = pool.Spawn(position, pIsWorldPosition);
			//Keep the trace of clone
			if (!m_idOfAllClones.ContainsKey(clone.gameObject.GetInstanceID()))
				m_idOfAllClones.Add(clone.gameObject.GetInstanceID(), prefab.gameObject.GetInstanceID());
			return clone;
		}

		public T Spawn(T prefab, Transform transform)
		{
			var pool = Get(prefab);
			var clone = pool.Spawn(transform);
			//Keep the trace of clone
			if (!m_idOfAllClones.ContainsKey(clone.gameObject.GetInstanceID()))
				m_idOfAllClones.Add(clone.gameObject.GetInstanceID(), prefab.gameObject.GetInstanceID());
			return clone;
		}

		public CustomPool<T> Add(T pPrefab)
		{
			if (!poolDict.ContainsKey(pPrefab.gameObject.GetInstanceID()))
			{
				var pool = new CustomPool<T>(pPrefab, m_initialNumber, container.transform);
				pool.limitNumber = limitNumber;
				poolDict.Add(pPrefab.gameObject.GetInstanceID(), pool);
			}
			else
				Debug.Log($"Pool Prefab {pPrefab.name} has already existed!");
			return poolDict[pPrefab.gameObject.GetInstanceID()];
		}

		public void Add(CustomPool<T> pPool)
		{
			if (!poolDict.ContainsKey(pPool.Prefab.gameObject.GetInstanceID()))
				poolDict.Add(pPool.Prefab.gameObject.GetInstanceID(), pPool);
			else
			{
				var pool = poolDict[pPool.Prefab.gameObject.GetInstanceID()];
				//Merge two pool
				foreach (var obj in pPool.ActiveList())
					if (!pool.ActiveList().Contains(obj))
						pool.ActiveList().Add(obj);
				foreach (var obj in pPool.InactiveList())
					if (!pool.InactiveList().Contains(obj))
						pool.InactiveList().Add(obj);
			}
		}

		public List<T> GetActiveList()
		{
			var list = new List<T>();
			foreach (var pool in poolDict)
			{
				list.AddRange(pool.Value.ActiveList());
			}
			return list;
		}

		public List<T> GetAllItems()
		{
			var list = new List<T>();
			foreach (var pool in poolDict)
			{
				list.AddRange(pool.Value.ActiveList());
				list.AddRange(pool.Value.InactiveList());
			}
			return list;
		}

		public void Release(T pObj)
		{
			if (m_idOfAllClones.ContainsKey(pObj.gameObject.GetInstanceID()))
				Release(m_idOfAllClones[pObj.gameObject.GetInstanceID()], pObj);
			else
			{
				foreach (var pool in poolDict)
					pool.Value.Release(pObj);
			}
		}

		public void Release(T pPrefab, T pObj)
		{
			Release(pPrefab.gameObject.GetInstanceID(), pObj);
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
		/// <summary>
		/// Used in OnInspectorGUI() of CustomEditor
		/// </summary>
		public void DrawOnEditor()
		{
			if (UnityEditor.EditorApplication.isPlaying && poolDict.Count > 0)
			{
				EditorHelper.BoxVertical(() =>
				{
					foreach (var item in poolDict)
					{
						var pool = item.Value;
						if (EditorHelper.HeaderFoldout($"{pool.Prefab.name} Pool {pool.ActiveList().Count}/{pool.InactiveList().Count} (Key:{item.Key})", item.Key.ToString()))
							pool.DrawOnEditor();
					}
				}, Color.white, true);
			}
		}
#endif
	}
}