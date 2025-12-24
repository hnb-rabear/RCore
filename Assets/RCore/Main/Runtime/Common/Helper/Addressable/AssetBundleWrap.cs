using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using Object = UnityEngine.Object;

namespace RCore
{
#if ADDRESSABLES
	/// <summary>
	/// A wrapper for handling the loading and instantiation of an addressable component asset.
	/// </summary>
	/// <typeparam name="T">The type of the component.</typeparam>
	[Serializable]
	public class AssetBundleWrap<T> where T : Component
	{
		/// <summary>
		/// The parent transform to which the instantiated asset will be parented.
		/// </summary>
		public Transform parent;
		
		/// <summary>
		/// The reference to the addressable component.
		/// </summary>
		public ComponentRef<T> reference;
		
		/// <summary>
		/// Indicates if the asset is currently being loaded.
		/// </summary>
		public bool IsLoading => m_operation.IsValid() && !m_operation.IsDone;
		public bool IsLoaded => asset != null;

		
		/// <summary>
		/// The loaded asset.
		/// </summary>
		public T asset { get; private set; }
		
		/// <summary>
		/// The instantiated instance of the component. This field is not serialized.
		/// </summary>
		[NonSerialized] public T instance;
		
		private AsyncOperationHandle<GameObject> m_operation;

		/// <summary>
		/// Instantiates the loaded asset.
		/// </summary>
		/// <param name="defaultActive">Whether the instantiated GameObject should be active by default.</param>
		public void Instantiate(bool defaultActive = false)
		{
			if (instance != null) return;
			instance = Object.Instantiate(asset, parent);
			instance.gameObject.SetActive(defaultActive);
			instance.name = asset.name;
		}

		/// <summary>
		/// Asynchronously instantiates the asset.
		/// </summary>
		/// <param name="defaultActive">Whether the instantiated GameObject should be active by default.</param>
		/// <returns>A <see cref="UniTask{T}"/> that completes with the instantiated component.</returns>
		public async UniTask<T> InstantiateAsync(bool defaultActive = false, CancellationToken pToken = default)
		{
			UnityEngine.Debug.Assert(parent != null, "parent != null");
			if (instance != null) return instance;

			var loadedAsset = await LoadAsync(null, pToken);
			if (instance != null) return instance;

			instance = Object.Instantiate(loadedAsset, parent);
			instance.gameObject.SetActive(defaultActive);
			instance.name = loadedAsset.name;
			
			Debug.Log($"Instantiate Asset Bundle {instance.name}");
			return instance;
		}

		/// <summary>
		/// Asynchronously loads the asset.
		/// </summary>
		/// <returns>A <see cref="UniTask{T}"/> that completes with the loaded asset.</returns>
		public async UniTask<T> LoadAsync(IProgress<float> progress = null, CancellationToken cancellationToken = default)
		{
			if (asset != null) return asset;

			if (m_operation.IsValid() && m_operation.Status == AsyncOperationStatus.Failed)
				Addressables.Release(m_operation);

			if (!m_operation.IsValid())
				m_operation = Addressables.LoadAssetAsync<GameObject>(reference);

			var obj = await m_operation.ToUniTask(progress: progress, cancellationToken: cancellationToken);
			if (obj)
				asset = obj.GetComponent<T>();

			if (asset != null)
				Debug.Log($"Load Asset Bundle {asset.name}");
			
			return asset;
		}

		/// <summary>
		/// Coroutine to instantiate the asset. Note that this does not wait for UniTask.
		/// </summary>
		/// <param name="defaultActive">Whether the instantiated GameObject should be active by default.</param>
		public IEnumerator IEInstantiate(bool defaultActive = false)
		{
			if (instance != null)
				yield break;

			yield return IELoad();

			if (asset != null && instance == null)
			{
				instance = Object.Instantiate(asset, parent);
				instance.gameObject.SetActive(defaultActive);
				instance.name = asset.name;
			}
		}

		/// <summary>
		/// Coroutine to load the asset.
		/// </summary>
		public IEnumerator IELoad()
		{
			if (asset != null)
				yield break;

			if (m_operation.IsValid() && m_operation.Status == AsyncOperationStatus.Failed)
				Addressables.Release(m_operation);

			if (!m_operation.IsValid())
				m_operation = Addressables.LoadAssetAsync<GameObject>(reference);

			yield return m_operation;
			if (m_operation.Result)
				asset = m_operation.Result.GetComponent<T>();

			if (asset != null)
				Debug.Log($"Load Asset Bundle {asset.name}");
		}

		/// <summary>
		/// Ensures that an instance of the asset is loaded and instantiated.
		/// </summary>
		/// <param name="active">Whether the instantiated GameObject should be active.</param>
		/// <returns>True if an instance is available, false otherwise.</returns>
		public bool InstanceLoaded(bool active = false)
		{
			if (instance == null && asset != null)
			{
				instance = Object.Instantiate(asset, parent);
				instance.gameObject.SetActive(active);
				instance.name = asset.name;
			}
			if (instance == null)
				InstantiateAsync(active);
			return instance != null;
		}

		/// <summary>
		/// Unloads the asset and destroys the instance.
		/// </summary>
		public void Unload()
		{
			try
			{
				if (instance != null)
					Object.Destroy(instance.gameObject);

				if (m_operation.IsValid())
					Addressables.Release(m_operation);
				
				asset = null;
				instance = null;
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError(ex);
			}
		}
	}
#endif
}