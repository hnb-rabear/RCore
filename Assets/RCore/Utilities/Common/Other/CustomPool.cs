/***
 * Author RadBear - nbhung71711@gmail.com - 2017
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
		public int limitNumber;
		private int m_InitialNumber;
		private Dictionary<int, int> m_IdOfAllClones; //Keep tracking the clone instance id and its prefab instance id

		public PoolsContainer(Transform pContainer)
		{
			container = pContainer;
			poolDict = new Dictionary<int, CustomPool<T>>();
			m_IdOfAllClones = new Dictionary<int, int>();
		}

		public PoolsContainer(int pInitialNumber = 1, Transform pParent = null)
		{
			var container = new GameObject("Pool_" + typeof(T).Name);
			container.transform.SetParent(pParent);
			container.transform.localPosition = Vector3.zero;
			container.transform.rotation = Quaternion.identity;
			this.container = container.transform;
			poolDict = new Dictionary<int, CustomPool<T>>();
			m_IdOfAllClones = new Dictionary<int, int>();
			m_InitialNumber = pInitialNumber;
		}

		public CustomPool<T> Get(T pPrefab)
		{
			if (poolDict.ContainsKey(pPrefab.GameObjectId()))
				return poolDict[pPrefab.GameObjectId()];
			else
			{
				var pool = new CustomPool<T>(pPrefab, m_InitialNumber, container.transform);
				pool.limitNumber = limitNumber;
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

		public T Spawn(T prefab, Vector3 position, bool pIsWorldPosition = true)
		{
			var pool = Get(prefab);
			var clone = pool.Spawn(position, pIsWorldPosition);
			//Keep the trace of clone
			if (!m_IdOfAllClones.ContainsKey(clone.GameObjectId()))
				m_IdOfAllClones.Add(clone.GameObjectId(), prefab.GameObjectId());
			return clone;
		}

		public T Spawn(T prefab, Transform transform)
		{
			var pool = Get(prefab);
			var clone = pool.Spawn(transform);
			//Keep the trace of clone
			if (!m_IdOfAllClones.ContainsKey(clone.GameObjectId()))
				m_IdOfAllClones.Add(clone.GameObjectId(), prefab.GameObjectId());
			return clone;
		}

		public CustomPool<T> Add(T pPrefab)
		{
			if (!poolDict.ContainsKey(pPrefab.GameObjectId()))
			{
				var pool = new CustomPool<T>(pPrefab, m_InitialNumber, container.transform);
				pool.limitNumber = limitNumber;
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
			if (m_IdOfAllClones.ContainsKey(pObj.GameObjectId()))
				Release(m_IdOfAllClones[pObj.GameObjectId()], pObj);
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
						if (EditorHelper.HeaderFoldout($"{pool.Prefab.name} Pool {pool.ActiveList().Count}/{pool.InactiveList().Count} (Key:{item.Key})", item.Key.ToString()))
							pool.DrawOnEditor();
					}
				}, Color.white, true);
			}
		}
#endif
	}

	[Serializable]
	public class CustomPool<T> where T : Component
	{
		#region Members

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
		public bool pushToLastSibling { get => m_PushToLastSibling;
			set => m_PushToLastSibling = value;
		}
		public int limitNumber { get => m_LimitNumber;
			set => m_LimitNumber = value;
		}
		protected bool m_Initialized;
		protected int m_InitialCount;

		public List<T> ActiveList() => m_ActiveList;
		public List<T> InactiveList() => m_InactiveList;

		#endregion

		//====================================

		#region Public

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
				var temp = new GameObject();
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
			if (m_LimitNumber > 0 && m_ActiveList.Count == m_LimitNumber)
			{
				var activeItem = m_ActiveList[0];
				m_InactiveList.Add(activeItem);
				m_ActiveList.Remove(activeItem);
			}

			int count = m_InactiveList.Count;
			if (m_AutoRelocate && count == 0)
				RelocateInactive();

			if (count > 0)
			{
				var item = m_InactiveList[m_InactiveList.Count - 1];
				if (pIsWorldPosition)
					item.transform.position = position;
				else
					item.transform.localPosition = position;
				Active(item, true, m_InactiveList.Count - 1);

				onSpawn?.Invoke(item);

				if (m_PushToLastSibling)
					item.transform.SetAsLastSibling();
				return item;
			}

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				var newItem = (T)UnityEditor.PrefabUtility.InstantiatePrefab(m_Prefab, m_Parent);
				newItem.name = m_Name;
				m_InactiveList.Add(newItem);
			}
			else
#endif
			{
				var newItem = Object.Instantiate(m_Prefab, m_Parent);
				newItem.name = m_Name;
				m_InactiveList.Add(newItem);
			}

			return Spawn(position, pIsWorldPosition);
		}

		public T Spawn(Vector3 position, bool pIsWorldPosition, ref bool pReused)
		{
			if (m_LimitNumber > 0 && m_ActiveList.Count == m_LimitNumber)
			{
				var activeItem = m_ActiveList[0];
				m_InactiveList.Add(activeItem);
				m_ActiveList.Remove(activeItem);
			}

			int count = m_InactiveList.Count;
			if (m_AutoRelocate && count == 0)
				RelocateInactive();

			if (count > 0)
			{
				var item = m_InactiveList[m_InactiveList.Count - 1];
				if (pIsWorldPosition)
					item.transform.position = position;
				else
					item.transform.localPosition = position;
				Active(item, true, m_InactiveList.Count - 1);
				pReused = true;

				onSpawn?.Invoke(item);

				if (m_PushToLastSibling)
					item.transform.SetAsLastSibling();
				return item;
			}

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				var newItem = (T)UnityEditor.PrefabUtility.InstantiatePrefab(m_Prefab, m_Parent);
				newItem.name = m_Name;
				m_InactiveList.Add(newItem);

			}
			else
#endif
			{
				var newItem = Object.Instantiate(m_Prefab, m_Parent);
				newItem.name = m_Name;
				m_InactiveList.Add(newItem);
			}
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
			m_InactiveList ??= new List<T>();
			m_ActiveList ??= new List<T>();

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
			CoroutineMediatorForScene.Instance.WaitForSecond(new WaitUtil.CountdownEvent()
			{
				id = pObj.GetInstanceID(),
				doSomething = (s) => { if (pObj != null) Release(pObj); },
				waitTime = pDelay
			});
		}

		public void Release(T pObj, ConditionalDelegate pCondition)
		{
			CoroutineMediatorForScene.Instance.WaitForCondition(new WaitUtil.ConditionEvent()
			{
				id = pObj.GetInstanceID(),
				onTrigger = () => { if (pObj != null) Release(pObj); },
				triggerCondition = pCondition
			});
		}

		public void Release(GameObject pObj)
		{
			for (int i = 0; i < m_ActiveList.Count; i++)
			{
				if (m_ActiveList[i].GameObjectId() == pObj.GetInstanceID())
				{
					Active(m_ActiveList[i], false, i);
					return;
				}
			}
		}

		public void Release(GameObject pObj, float pDelay)
		{
			CoroutineMediatorForScene.Instance.WaitForSecond(new WaitUtil.CountdownEvent()
			{
				id = pObj.GetInstanceID(),
				doSomething = (s) => { if (pObj != null) Release(pObj); },
				waitTime = pDelay
			});
		}

		public void Release(GameObject pObj, ConditionalDelegate pCondition)
		{
			CoroutineMediatorForScene.Instance.WaitForCondition(new WaitUtil.ConditionEvent()
			{
				id = pObj.GetInstanceID(),
				onTrigger = () => { if (pObj != null) Release(pObj); },
				triggerCondition = pCondition,
			});
		}

		public void ReleaseAll()
		{
			for (int i = 0; i < m_ActiveList.Count; i++)
			{
				var item = m_ActiveList[i];
				m_InactiveList.Add(item);
				item.SetActive(false);
			}
			m_ActiveList.Clear();
		}

		public void DestroyAll()
		{
			while (m_ActiveList.Count > 0)
			{
				int index = m_ActiveList.Count - 1;
				if (Application.isPlaying)
					Object.Destroy(m_ActiveList[index].gameObject);
				else
					Object.DestroyImmediate(m_ActiveList[index].gameObject);
				m_ActiveList.RemoveAt(index);
			}
			while (m_InactiveList.Count > 0)
			{
				int index = m_InactiveList.Count - 1;
				if (Application.isPlaying)
					Object.Destroy(m_InactiveList[index].gameObject);
				else
					Object.DestroyImmediate(m_InactiveList[index].gameObject);
				m_InactiveList.RemoveAt(index);
			}
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

		public void RelocateInactive()
		{
			for (int i = m_ActiveList.Count - 1; i >= 0; i--)
				if (!m_ActiveList[i].gameObject.activeSelf)
					Active(m_ActiveList[i], false, i);
		}

		public void SetParent(Transform pParent)
		{
			m_Parent = pParent;
		}

		public void SetName(string pName)
		{
			m_Name = pName;
		}

		#endregion

		//========================================

		#region Private

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

		#endregion

		//=========================================

#if UNITY_EDITOR
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
					if (EditorHelper.Button("Relocate"))
						RelocateInactive();
				}, Color.white, true);
			}
		}
#endif
	}
}