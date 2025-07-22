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
	/// A high-level manager for multiple `CustomPool<T>` instances.
	/// This class acts as a "pool of pools," organizing different object pools under a single container.
	/// It lazy-loads pools as new prefabs are requested and provides a unified interface for spawning and releasing objects.
	/// </summary>
	/// <typeparam name="T">The type of Component this container will manage pools for.</typeparam>
	public class PoolsContainer<T> where T : Component
	{
		/// <summary>
		/// The core data structure, mapping a prefab's instance ID to its dedicated CustomPool.
		/// </summary>
		public Dictionary<int, CustomPool<T>> poolDict = new Dictionary<int, CustomPool<T>>();
		/// <summary>
		/// The root Transform that parents the GameObjects of all managed pools, keeping the hierarchy clean.
		/// </summary>
		public Transform container;
		/// <summary>
		/// A global limit applied to any new pools created by this container.
		/// </summary>
		public int limitNumber;
		private int m_initialNumber;
		/// <summary>
		/// An optimization that maps a clone's instance ID back to its original prefab's instance ID.
		/// This allows for very fast `Release` operations without needing to know the prefab beforehand.
		/// </summary>
		private Dictionary<int, int> m_idOfAllClones = new Dictionary<int, int>();

		/// <summary>
		/// Initializes a new PoolsContainer with a pre-existing Transform as its root.
		/// </summary>
		/// <param name="pContainer">The Transform to use as the container.</param>
		public PoolsContainer(Transform pContainer)
		{
			container = pContainer;
		}

		/// <summary>
		/// Initializes a new PoolsContainer, creating a new GameObject to serve as its root.
		/// </summary>
		/// <param name="pContainerName">The name for the new container GameObject.</param>
		/// <param name="pInitialNumber">The default number of objects to pre-warm for any new pool created by this container.</param>
		/// <param name="pParent">An optional parent for the container GameObject.</param>
		public PoolsContainer(string pContainerName, int pInitialNumber = 1, Transform pParent = null)
		{
			var containerGO = new GameObject(pContainerName);
			containerGO.transform.SetParent(pParent);
			containerGO.transform.localPosition = Vector3.zero;
			containerGO.transform.rotation = Quaternion.identity;
			this.container = containerGO.transform;
			m_initialNumber = pInitialNumber;
		}

		/// <summary>
		/// Gets the pool associated with the specified prefab. If the pool doesn't exist, it is created automatically.
		/// </summary>
		/// <param name="pPrefab">The prefab whose pool is needed.</param>
		/// <returns>The CustomPool instance for the given prefab, or null if the prefab is null.</returns>
		public CustomPool<T> Get(T pPrefab)
		{
			if (pPrefab == null)
				return null;
			int prefabInstanceId = pPrefab.gameObject.GetInstanceID();
			if (poolDict.TryGetValue(prefabInstanceId, out var prefabPool))
				return prefabPool;
			
			// Pool doesn't exist, so create it, add it to the dictionary, and return it.
			var pool = new CustomPool<T>(pPrefab, m_initialNumber, container.transform);
			pool.limitNumber = limitNumber;
			poolDict.Add(prefabInstanceId, pool);
			return pool;
		}

		/// <summary>
		/// Creates a pool for a prefab and populates it with pre-existing objects from the scene.
		/// </summary>
		/// <param name="pPrefab">The prefab key for the pool.</param>
		/// <param name="pBuiltInObjs">The list of objects already in the scene to add to the pool.</param>
		public void CreatePool(T pPrefab, List<T> pBuiltInObjs)
		{
			var pool = Get(pPrefab);
			pool.AddOutsiders(pBuiltInObjs);
		}

		/// <summary>
		/// Spawns an instance of the specified prefab from its corresponding pool.
		/// </summary>
		/// <param name="prefab">The prefab to spawn.</param>
		/// <returns>An active component instance from the pool.</returns>
		public T Spawn(T prefab)
		{
			return Spawn(prefab, Vector3.zero, false);
		}

		/// <summary>
		/// Spawns an instance of the specified prefab at a given position.
		/// </summary>
		/// <param name="prefab">The prefab to spawn.</param>
		/// <param name="position">The position to spawn the object at.</param>
		/// <param name="pIsWorldPosition">Is the provided position in world space or local space?</param>
		/// <returns>An active component instance from the pool.</returns>
		public T Spawn(T prefab, Vector3 position, bool pIsWorldPosition = true)
		{
			var pool = Get(prefab);
			var clone = pool.Spawn(position, pIsWorldPosition);
			// Keep track of the clone's origin for fast releasing.
			if (clone != null && !m_idOfAllClones.ContainsKey(clone.gameObject.GetInstanceID()))
				m_idOfAllClones.Add(clone.gameObject.GetInstanceID(), prefab.gameObject.GetInstanceID());
			return clone;
		}

		/// <summary>
		/// Spawns an instance of the specified prefab at a given transform's position.
		/// </summary>
		/// <param name="prefab">The prefab to spawn.</param>
		/// <param name="transform">The transform to spawn the object at.</param>
		/// <returns>An active component instance from the pool.</returns>
		public T Spawn(T prefab, Transform transform)
		{
			var pool = Get(prefab);
			var clone = pool.Spawn(transform);
			// Keep track of the clone's origin for fast releasing.
			if (clone != null && !m_idOfAllClones.ContainsKey(clone.gameObject.GetInstanceID()))
				m_idOfAllClones.Add(clone.gameObject.GetInstanceID(), prefab.gameObject.GetInstanceID());
			return clone;
		}

		/// <summary>
		/// Explicitly creates and adds a new pool for the given prefab if it doesn't already exist.
		/// </summary>
		/// <param name="pPrefab">The prefab to create a pool for.</param>
		/// <returns>The newly created or existing pool for the prefab.</returns>
		public CustomPool<T> Add(T pPrefab)
		{
			int prefabId = pPrefab.gameObject.GetInstanceID();
			if (!poolDict.ContainsKey(prefabId))
			{
				var pool = new CustomPool<T>(pPrefab, m_initialNumber, container.transform);
				pool.limitNumber = limitNumber;
				poolDict.Add(prefabId, pool);
			}
			else
				Debug.LogWarning($"Pool for prefab '{pPrefab.name}' already exists in this container.");
			return poolDict[prefabId];
		}

		/// <summary>
		/// Adds a pre-configured CustomPool to the container. If a pool for the same prefab already exists,
		/// the objects from the new pool are merged into the existing one.
		/// </summary>
		/// <param name="pPool">The CustomPool instance to add.</param>
		public void Add(CustomPool<T> pPool)
		{
			int prefabId = pPool.Prefab.gameObject.GetInstanceID();
			if (!poolDict.ContainsKey(prefabId))
				poolDict.Add(prefabId, pPool);
			else
			{
				// A pool for this prefab already exists, so merge the contents.
				var existingPool = poolDict[prefabId];
				foreach (var obj in pPool.ActiveList())
					if (!existingPool.ActiveList().Contains(obj))
						existingPool.ActiveList().Add(obj);
				foreach (var obj in pPool.InactiveList())
					if (!existingPool.InactiveList().Contains(obj))
						existingPool.InactiveList().Add(obj);
			}
		}

		/// <summary>
		/// Gets a consolidated list of all active objects across all pools in this container.
		/// </summary>
		/// <returns>A new list containing all active objects.</returns>
		public List<T> GetActiveList()
		{
			var list = new List<T>();
			foreach (var pool in poolDict.Values)
				list.AddRange(pool.ActiveList());
			return list;
		}

		/// <summary>
		/// Gets a consolidated list of all objects (active and inactive) across all pools in this container.
		/// </summary>
		/// <returns>A new list containing all managed objects.</returns>
		public List<T> GetAllItems()
		{
			var list = new List<T>();
			foreach (var pool in poolDict.Values)
			{
				list.AddRange(pool.ActiveList());
				list.AddRange(pool.InactiveList());
			}
			return list;
		}

		/// <summary>
		/// Releases an active object back to its correct pool.
		/// This is the most efficient release method as it uses an ID-tracking dictionary.
		/// </summary>
		/// <param name="pObj">The object instance to release.</param>
		public void Release(T pObj)
		{
			if (pObj == null) return;
			
			if (m_idOfAllClones.TryGetValue(pObj.gameObject.GetInstanceID(), out int prefabId))
			{
				Release(prefabId, pObj);
			}
			else
			{
				// Fallback: If the object wasn't tracked, search all pools. This is slow.
				foreach (var pool in poolDict.Values)
					pool.Release(pObj);
			}
		}

		/// <summary>
		/// Releases an active object back to its pool, specifying the original prefab.
		/// </summary>
		/// <param name="pPrefab">The prefab that the object was cloned from.</param>
		/// <param name="pObj">The object instance to release.</param>
		public void Release(T pPrefab, T pObj)
		{
			if (pPrefab != null)
				Release(pPrefab.gameObject.GetInstanceID(), pObj);
		}

		/// <summary>
		/// Releases an active object back to its pool using the prefab's instance ID.
		/// </summary>
		/// <param name="pPrefabId">The instance ID of the prefab the object was cloned from.</param>
		/// <param name="pObj">The object instance to release.</param>
		public void Release(int pPrefabId, T pObj)
		{
			if (poolDict.TryGetValue(pPrefabId, out CustomPool<T> pool))
				pool.Release(pObj);
			else
			{
				// This case is unlikely if spawning and releasing through the container, but provided as a safeguard.
#if UNITY_EDITOR
				Debug.LogWarning($"Could not find pool with prefabId {pPrefabId} to release object '{pObj.name}'. Falling back to slow search.");
#endif
				// Fallback: search all pools.
				foreach (var fallbackPool in poolDict.Values)
					fallbackPool.Release(pObj);
			}
		}

		/// <summary>
		/// Releases all active objects across all managed pools.
		/// </summary>
		public void ReleaseAll()
		{
			foreach (var pool in poolDict.Values)
				pool.ReleaseAll();
		}

		/// <summary>
		/// Releases an active object via its GameObject. This is a convenience method and is less efficient
		/// than releasing by the Component instance, as it must search all pools.
		/// </summary>
		/// <param name="pObj">The GameObject to release.</param>
		public void Release(GameObject pObj)
		{
			foreach (var pool in poolDict.Values)
				pool.Release(pObj);
		}

		/// <summary>
		/// Finds the pooled component associated with a given GameObject by searching all pools.
		/// </summary>
		/// <param name="pObj">The GameObject to find the component for.</param>
		/// <returns>The found component, or null if it's not managed by this container.</returns>
		public T FindComponent(GameObject pObj)
		{
			foreach (var pool in poolDict.Values)
			{
				var component = pool.FindComponent(pObj);
				if (component != null)
					return component;
			}
			return null;
		}

#if UNITY_EDITOR
		/// <summary>
		/// An editor-only method intended to be called from a CustomEditor's `OnInspectorGUI`
		/// to display all managed pools and their contents for easy debugging while in play mode.
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
						if (pool.Prefab != null && EditorHelper.HeaderFoldout($"{pool.Prefab.name} Pool ({pool.ActiveList().Count}/{pool.InactiveList().Count + pool.ActiveList().Count}) (Key:{item.Key})", item.Key.ToString()))
						{
							// Draw the contents of the individual pool.
							pool.DrawOnEditor();
						}
					}
				}, Color.white, true);
			}
		}
#endif
	}
}