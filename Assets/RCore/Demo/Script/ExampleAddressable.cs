//#define ADDRESSABLES

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
using RCore.Common;

namespace RCore.Demo
{
	public class ExampleAddressable : MonoBehaviour
	{
		public string label;
#if ADDRESSABLES
		public List<AssetReference> prefabReferences;
#endif
		public List<GameObject> objects;
		public List<GameObject> prefabs;

		private PoolsContainer<Transform> m_PoolContainer;

#if ADDRESSABLES
		private IEnumerator Start()
		{
			yield return StartCoroutine(IELoadPrefabReferences());
			yield return StartCoroutine(IEInstantiatePrefabReferences());
			yield return StartCoroutine(IEPoolPrefabReferences());
			yield return StartCoroutine(IELoadAndInstantiateLabel());
		}

		private IEnumerator IELoadPrefabReferences()
		{
			var uniTask = AddressableUtil.LoadAssetsAsync<GameObject>(prefabReferences);
			yield return uniTask;
			prefabs = uniTask.GetAwaiter().GetResult();
		}

		///Example of instantiate gameObject from references
		private IEnumerator IEInstantiatePrefabReferences()
		{
			var uniTask = AddressableUtil.InstantiateAsync(prefabReferences);
			yield return uniTask;
			objects = uniTask.GetAwaiter().GetResult();
			foreach (var obj in objects)
				obj.transform.position = new Vector2(Random.Range(-10, 10), Random.Range(-10, 10));
		}

		///Example of loading prefabs and pushing them into pools
		private IEnumerator IEPoolPrefabReferences()
		{
			m_PoolContainer = new PoolsContainer<Transform>(transform);

			var uniTask = AddressableUtil.LoadAssetsAsync<Transform>(prefabReferences);
			yield return uniTask;

			var prefabs = uniTask.GetAwaiter().GetResult();
			float time = 3;
			while (time > 0)
			{
				var prefab = prefabs[Random.Range(0, prefabs.Count)];
				var position = new Vector2(Random.Range(-10, 10), Random.Range(-10, 10));
				m_PoolContainer.Spawn(prefab, position);
				yield return new WaitForSeconds(0.25f);
				time -= 0.25f;
			}
		}

		private IEnumerator IELoadAndInstantiateLabel()
		{
			if (string.IsNullOrEmpty(label))
				yield break;

			var task = AddressableUtil.LoadResourceLocationAsync(label);
			yield return task;
			if (task.IsFaulted || task.IsCanceled)
				yield break;
			var locations = task.Result;
			yield return AddressableUtil.InstantiateAsync(locations);
		}
#endif
	}
}