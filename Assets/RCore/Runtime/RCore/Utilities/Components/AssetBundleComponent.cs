#if ADDRESSABLES
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace RCore.Components
{
	public class AssetBundleComponent : MonoBehaviour
	{
		public bool auto;
		public Transform parent;
		public AssetReferenceGameObject reference;
		internal bool loading { get; private set; }
		internal GameObject cache { get; private set; }
		private void Start()
		{
			if (auto)
				InstantiateAsync();
		}
		public async UniTask<T> InstantiateAsync<T>() where T : Component
		{
			if (cache == null)
			{
				loading = true;
				cache = await Addressables.InstantiateAsync(reference, parent);
				loading = false;
				if (cache != null)
				{
					cache.transform.localPosition = Vector3.zero;
					Debug.Log($"Instantiate Asset Bundle {cache.name}");
				}
			}
			var component = cache.GetComponent<T>();
			return component;
		}
		public async UniTask<GameObject> InstantiateAsync()
		{
			if (cache == null)
			{
				loading = true;
				cache = await Addressables.InstantiateAsync(reference, parent);
				loading = false;
				if (cache != null)
				{
					cache.transform.localPosition = Vector3.zero;
					cache.name = cache.name;
					Debug.Log($"Instantiate Asset Bundle {cache.name}");
				}
			}
			return cache;
		}
		private void OnDestroy()
		{
			if (cache != null)
			{
				Debug.Log($"Unload Asset Bundle {cache.name}");
				Addressables.ReleaseInstance(cache.gameObject);
			}
		}
	}
}
#endif