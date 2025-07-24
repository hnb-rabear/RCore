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
	/// <summary>
	/// A manager class that holds and provides access to multiple CustomPool instances.
	/// It acts as a central hub for object pooling, where each distinct prefab gets its own pool,
	/// all organized under a single container transform.
	/// </summary>
	/// <typeparam name="T">The type of Component being pooled. Must inherit from UnityEngine.Component.</typeparam>
	public class PoolsContainer<T> where T : Component
	{
		/// <summary>A dictionary of all pools managed by this container, keyed by the prefab's instance ID.</summary>
		public Dictionary<int, CustomPool<T>> poolDict = new Dictionary<int, CustomPool<T>>();
		/// <summary>The root transform under which all pool parent objects will be created.</summary>
		public Transform container;
		/// <summary>A global limit for the number of active objects per pool created by this container.</summary>
		public int limitNumber;
		/// <summary>The default number of objects to create when a new pool is initialized.</summary>
		private int m_initialNumber;
		/// <summary>A lookup dictionary that maps a clone's instance ID to its original prefab's instance ID. This provides a fast way to find the correct pool for a given object.</summary>
		private Dictionary<int, int> m_idOfAllClones = new Dictionary<int, int>(); //Keep tracking the clone instance id and its prefab instance id

		/// <summary>
		/// Initializes a new instance of the PoolsContainer class with a pre-existing container transform.
		/// </summary>
		/// <param name="pContainer">The root transform for all pools.</param>
		public PoolsContainer(Transform pContainer)
		{
			container = pContainer;
		}

		/// <summary>
		/// Initializes a new instance of the PoolsContainer class, creating a new container GameObject.
		/// </summary>
		/// <param name="pContainerName">The name for the new container GameObject.</param>
		/// <param name="pInitialNumber">The default number of instances to pre-warm for each new pool.</param>
		/// <param name="pParent">An optional parent for the container GameObject.</param>
		public PoolsContainer(string pContainerName, int pInitialNumber = 1, Transform pParent = null)
		{
			var container = new GameObject(pContainerName);
			container.transform.SetParent(pParent);
			container.transform.localPosition = Vector3.zero;
			container.transform.rotation = Quaternion.identity;
			this.container = container.transform;
			m_initialNumber = pInitialNumber;
		}

		/// <summary>
		/// Retrieves the pool for a specific prefab. If a pool for that prefab doesn't exist, it creates one automatically.
		/// </summary>
		/// <param name="pPrefab">The prefab whose pool is requested.</param>
		/// <returns>The CustomPool instance for the given prefab.</returns>
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

		/// <summary>
		/// Creates a pool for a prefab and populates it with a list of pre-existing objects from the scene.
		/// </summary>
		/// <param name="pPrefab">The prefab key for the pool.</param>
		/// <param name="pBuiltInObjs">The list of scene objects to add to the pool.</param>
		public void CreatePool(T pPrefab, List<T> pBuiltInObjs)
		{
			var pool = Get(pPrefab);
			pool.AddOutsiders(pBuiltInObjs);
		}

		/// <summary>
		/// Spawns an instance of a prefab from its corresponding pool.
		/// </summary>
		/// <param name="prefab">The prefab to spawn an instance of.</param>
		/// <returns>An active component instance of the specified prefab.</returns>
		public T Spawn(T prefab)
		{
			return Spawn(prefab, Vector3.zero);
		}

		/// <summary>
		/// Spawns an instance of a prefab at a specific position.
		/// </summary>
		/// <param name="prefab">The prefab to spawn an instance of.</param>
		/// <param name="position">The position to spawn the object at.</param>
		/// <param name="pIsWorldPosition">If true, the position is treated as world space; otherwise, local space.</param>
		/// <returns>An active component instance of the specified prefab.</returns>
		public T Spawn(T prefab, Vector3 position, bool pIsWorldPosition = true)
		{
			var pool = Get(prefab);
			var clone = pool.Spawn(position, pIsWorldPosition);
			//Keep the trace of clone
			if (!m_idOfAllClones.ContainsKey(clone.gameObject.GetInstanceID()))
				m_idOfAllClones.Add(clone.gameObject.GetInstanceID(), prefab.gameObject.GetInstanceID());
			return clone;
		}

		/// <summary>
		/// Spawns an instance of a prefab at the position of a given Transform.
		/// </summary>
		/// <param name="prefab">The prefab to spawn.</param>
		/// <param name="transform">The transform to match the position of.</param>
		/// <returns>An active component instance of the specified prefab.</returns>
		public T Spawn(T prefab, Transform transform)
		{
			var pool = Get(prefab);
			var clone = pool.Spawn(transform);
			//Keep the trace of clone
			if (!m_idOfAllClones.ContainsKey(clone.gameObject.GetInstanceID()))
				m_idOfAllClones.Add(clone.gameObject.GetInstanceID(), prefab.gameObject.GetInstanceID());
			return clone;
		}

		/// <summary>
		/// Explicitly adds a new, empty pool for a given prefab if it doesn't already exist.
		/// </summary>
		/// <param name="pPrefab">The prefab to create a pool for.</param>
		/// <returns>The new or existing pool for the prefab.</returns>
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

		/// <summary>
		/// Adds a pre-configured CustomPool to the container. If a pool for the same prefab exists, their contents are merged.
		/// </summary>
		/// <param name="pPool">The CustomPool instance to add.</param>
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

		/// <summary>
		/// Gets a list of all currently active objects from all pools in this container.
		/// </summary>
		/// <returns>A new list containing all active items.</returns>
		public List<T> GetActiveList()
		{
			var list = new List<T>();
			foreach (var pool in poolDict)
			{
				list.AddRange(pool.Value.ActiveList());
			}
			return list;
		}

		/// <summary>
		/// Gets a list of all objects (both active and inactive) from all pools in this container.
		/// </summary>
		/// <returns>A new list containing all items.</returns>
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

		/// <summary>
		/// Releases an object back to its correct pool. It uses the internal tracking dictionary to find the original prefab and its pool.
		/// </summary>
		/// <param name="pObj">The object instance to release.</param>
		public void Release(T pObj)
		{
			if (m_idOfAllClones.ContainsKey(pObj.gameObject.GetInstanceID()))
				Release(m_idOfAllClones[pObj.gameObject.GetInstanceID()], pObj);
			else
			{
				// Fallback: If not tracked, search all pools (less efficient).
				foreach (var pool in poolDict)
					pool.Value.Release(pObj);
			}
		}

		/// <summary>
		/// Releases an object back to the pool associated with a specific prefab.
		/// </summary>
		/// <param name="pPrefab">The prefab key for the pool.</param>
		/// <param name="pObj">The object instance to release.</param>
		public void Release(T pPrefab, T pObj)
		{
			Release(pPrefab.gameObject.GetInstanceID(), pObj);
		}

		/// <summary>
		/// Releases an object back to the pool associated with a specific prefab instance ID.
		/// </summary>
		/// <param name="pPrefabId">The instance ID of the prefab key.</param>
		/// <param name="pObj">The object instance to release.</param>
		public void Release(int pPrefabId, T pObj)
		{
			if (poolDict.ContainsKey(pPrefabId))
				poolDict[pPrefabId].Release(pObj);
			else
			{
#if UNITY_EDITOR
				UnityEngine.Debug.Log("We should not release obj by this way");
#endif
				// Fallback: If the prefab ID is not found, search all pools.
				foreach (var pool in poolDict)
					pool.Value.Release(pObj);
			}
		}

		/// <summary>
		/// Releases all active objects in all pools back to their inactive states.
		/// </summary>
		public void ReleaseAll()
		{
			foreach (var pool in poolDict)
			{
				pool.Value.ReleaseAll();
			}
		}

		/// <summary>
		/// Releases an object by its GameObject, searching through all pools. This is less efficient than releasing by component or with a prefab key.
		/// </summary>
		/// <param name="pObj">The GameObject to release.</param>
		public void Release(GameObject pObj)
		{
			foreach (var pool in poolDict)
			{
				pool.Value.Release(pObj);
			}
		}

		/// <summary>
		/// Finds the pooled component associated with a given GameObject by searching all pools.
		/// </summary>
		/// <param name="pObj">The GameObject to search for.</param>
		/// <returns>The component instance if found, otherwise null.</returns>
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
		/// A helper method to draw a debug view of all contained pools' states in a custom editor inspector.
		/// This should be called from the OnInspectorGUI method of a custom editor script.
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