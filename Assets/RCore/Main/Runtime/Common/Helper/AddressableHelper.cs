/**
* Author RaBear - HNB - 2020
**/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using TMPro;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using Object = UnityEngine.Object;
using Cysharp.Threading.Tasks;
using UnityEngine.Serialization;

namespace RCore
{
#if ADDRESSABLES

	[Serializable]
	public class ComponentRef<TComponent> : AssetReference where TComponent : Component
	{
		public TComponent instance;
		public TComponent asset;
		private AsyncOperationHandle<GameObject> m_operation;
		public ComponentRef(string guid) : base(guid) { }
		
		public override bool ValidateAsset(Object obj)
		{
			var go = obj as GameObject;
			return go != null && go.GetComponent<TComponent>() != null;
		}
		public override bool ValidateAsset(string path)
		{
#if UNITY_EDITOR
			//this load can be expensive...
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			return go != null && go.GetComponent<TComponent>() != null;
#else
            return false;
#endif
		}
		public async UniTask<TComponent> InternalInstantiateAsync(bool pDefaultActive = false)
		{
			m_operation = Addressables.InstantiateAsync(this);
			var go = await m_operation;
			go.SetActive(pDefaultActive);
			go.TryGetComponent(out instance);
			Debug.Log($"Instantiate Asset Bundle {instance.name}");
			return instance;
		}
		public async UniTask<TComponent> InternalLoadAssetAsync()
		{
			if (asset != null)
				return asset;
			var operation = IsValid() ? OperationHandle.Convert<GameObject>() : LoadAssetAsync<GameObject>();
			await operation;
			asset = operation.Result.GetComponent<TComponent>();
			Debug.Log($"Load Asset Bundle {asset.name}");
			return asset;
		}
		public IEnumerator IEInternalLoadAssetAsync()
		{
			if (asset != null)
				yield break;
			var operation = IsValid() ? OperationHandle.Convert<GameObject>() : LoadAssetAsync<GameObject>();
			yield return operation;
			asset = operation.Result.GetComponent<TComponent>();
			Debug.Log($"Load Asset Bundle {asset.name}");
		}
		public void Unload()
		{
			try
			{
				if (instance != null)
				{
					string instanceName = instance.name;
					if (m_operation.IsValid() && Addressables.ReleaseInstance(m_operation))
						Debug.Log($"Unload asset bundle success {instanceName}");
				}
				if (asset != null)
				{
					string name = asset.name;
					asset = null;
					ReleaseAsset();
					Debug.Log($"Unload asset bundle success {name}");
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError(ex);
			}
		}
	}

	/// <summary>
	/// Example generic asset reference
	/// </summary>
	[Serializable]
	public class ComponentRef_SpriteRenderer : ComponentRef<SpriteRenderer>
	{
		public ComponentRef_SpriteRenderer(string guid) : base(guid) { }
	}

	/// <summary>
	/// Example generic asset reference
	/// </summary>
	[Serializable]
	public class AssetRef_SpriteAtlas : AssetReferenceT<SpriteAtlas>
	{
		public AssetRef_SpriteAtlas(string guid) : base(guid) { }
	}

	/// <summary>
	/// Example generic asset reference
	/// </summary>
	[Serializable]
	public class AssetRef_FontAsset : AssetReferenceT<TMP_FontAsset>
	{
		public AssetRef_FontAsset(string guid) : base(guid) { }
	}

	//================================================================================

	public static class AddressableUtil
	{
		public static AsyncOperationHandle<long> GetDownloadSizeAsync(object key, Action<long> pOnComplete)
		{
			var operation = Addressables.GetDownloadSizeAsync(key);
			operation.Completed += (op) =>
			{
				pOnComplete?.Invoke(op.Result);
			};
			return operation;
		}
		public static async Task<long> GetDownloadSizeAsync(object key)
		{
			//Clear all cached AssetBundles
			Addressables.ClearDependencyCacheAsync(key);

			//Check the download size
			var operation = Addressables.GetDownloadSizeAsync(key);
			await operation.Task;
			return operation.Result;
		}
		public static async Task<List<IResourceLocation>> LoadResourceLocationAsync(string pLabel)
		{
			var locations = new List<IResourceLocation>();
			var operation = Addressables.LoadResourceLocationsAsync(pLabel);
			await operation.Task;
			foreach (var location in operation.Result)
				locations.Add(location);
			return locations;
		}

		/// <summary>
		/// Update the catalogs to ensure that the package has the latest information
		/// </summary>
		public static async UniTask CheckForCatalogUpdates()
		{
			var catalogsToUpdate = new List<string>();
			var checkCatalogHandle = Addressables.CheckForCatalogUpdates();
			checkCatalogHandle.Completed += op =>
			{
				catalogsToUpdate.AddRange(op.Result);
			};
			await checkCatalogHandle;
			if (catalogsToUpdate.Count > 0)
			{
				var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate);
				await updateHandle;
			}
		}

#region Download Dependencies

		public static AsyncOperationHandle DownloadDependenciesAsync(object pKey, bool pAutoRelease, Action pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.DownloadDependenciesAsync(pKey, pAutoRelease);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask DownloadDependenciesAsync(object pKey, bool pAutoRelease)
		{
			var operation = Addressables.DownloadDependenciesAsync(pKey, pAutoRelease);
			await operation;
		}
		public static AsyncOperationHandle DownloadDependenciesAsync(IList<IResourceLocation> locations, bool pAutoRelease, Action pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.DownloadDependenciesAsync(locations, pAutoRelease);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask DownloadDependenciesAsync(IList<IResourceLocation> locations, bool pAutoRelease)
		{
			var operation = Addressables.DownloadDependenciesAsync(locations, pAutoRelease);
			await operation;
		}
		public static AsyncOperationHandle DownloadDependenciesAsync(string pAddress, bool pAutoRelease, Action pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.DownloadDependenciesAsync(pAddress, pAutoRelease);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask DownloadDependenciesAsync(string pAddress, bool pAutoRelease)
		{
			var operation = Addressables.DownloadDependenciesAsync(pAddress, pAutoRelease);
			await operation;
		}
		public static AsyncOperationHandle DownloadDependenciesAsync(AssetReference pReference, bool pAutoRelease, Action pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.DownloadDependenciesAsync(pReference, pAutoRelease);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask DownloadDependenciesAsync(AssetReference pReference, bool pAutoRelease)
		{
			var operation = Addressables.DownloadDependenciesAsync(pReference, pAutoRelease);
			await operation;
		}

#endregion

#region Load/Unload Scene

		public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(string pAddress, LoadSceneMode pMode, Action<SceneInstance> pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.LoadSceneAsync(pAddress, pMode);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<SceneInstance> LoadSceneAsync(string pAddress, LoadSceneMode pMode)
		{
			var operation = Addressables.LoadSceneAsync(pAddress, pMode);
			var result = await operation;
			return result;
		}
		public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance pScene, Action<bool> pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.UnloadSceneAsync(pScene);
			WaitUnloadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<SceneInstance> UnloadSceneAsync(SceneInstance pScene)
		{
			var operation = Addressables.UnloadSceneAsync(pScene);
			var result = await operation;
			return result;
		}

#endregion

#region Load Assets Generic

		public static async UniTask<List<TObject>> LoadAssetsAsync<TObject>(List<string> pAddresses) where TObject : Object
		{
			var tasks = new Task<TObject>[pAddresses.Count];
			for (int i = 0; i < pAddresses.Count; i++)
			{
				string address = pAddresses[i];
				var operation = Addressables.LoadAssetAsync<TObject>(address);
				tasks[i] = operation.Task;
			}
			await Task.WhenAll(tasks);
			var results = new List<TObject>();
			foreach (var task in tasks)
				results.Add(task.Result);
			return results;
		}
		public static async UniTask<List<TObject>> LoadAssetsAsync<TObject>(List<AssetReference> pReferences) where TObject : Object
		{
			var tasks = new Task<TObject>[pReferences.Count];
			for (int i = 0; i < pReferences.Count; i++)
			{
				var reference = pReferences[i];
				var operation = reference.IsValid() ? reference.OperationHandle.Convert<TObject>() : reference.LoadAssetAsync<TObject>();
				tasks[i] = operation.Task;
			}
			await Task.WhenAll(tasks);
			var results = new List<TObject>();
			foreach (var task in tasks)
				results.Add(task.Result);
			return results;
		}
		public static async UniTask<List<TObject>> LoadAssetsAsync<TObject, TReference>(List<TReference> pReferences) where TObject : Object where TReference : AssetReference
		{
			var tasks = new Task<TObject>[pReferences.Count];
			for (int i = 0; i < pReferences.Count; i++)
			{
				var r = pReferences[i];
				var operation = r.LoadAssetAsync<TObject>();
				tasks[i] = operation.Task;
			}
			await Task.WhenAll();
			var results = new List<TObject>();
			foreach (var task in tasks)
				results.Add(task.Result);
			return results;
		}

#endregion

#region Instantiate

		public static AsyncOperationHandle<GameObject> InstantiateAsync<TReference>(TReference pReference, Transform parent, Action<GameObject> pOnComplete, Action<float> pProgress = null)
			where TReference : AssetReference
		{
			var operation = Addressables.InstantiateAsync(pReference, parent);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static AsyncOperationHandle<GameObject> InstantiateAsync<TComponent, TReference>(TReference pReference, Transform parent, Action<TComponent> pOnComplete, Action<float> pProgress = null)
			where TComponent : Component where TReference : AssetReference
		{
			var operation = Addressables.InstantiateAsync(pReference, parent);
			WaitLoadTask(operation, (result) =>
			{
				result.TryGetComponent(out TComponent component);
				pOnComplete?.Invoke(component);
			}, pProgress);
			return operation;
		}
		public static async UniTask<GameObject> InstantiateAsync<TReference>(TReference pReference, Transform pParent) where TReference : AssetReference
		{
			var operation = Addressables.InstantiateAsync(pReference, pParent);
			var result = await operation;
			return result;
		}
		public static async UniTask<GameObject> InstantiateAsync(AssetReference pReference, Transform pParent)
		{
			var operation = Addressables.InstantiateAsync(pReference, pParent);
			var result = await operation.Task;
			return result;
		}
		public static async UniTask<TComponent> InstantiateAsync<TComponent, TReference>(TReference pReference, Transform pParent) where TComponent : Component where TReference : AssetReference
		{
			var operation = Addressables.InstantiateAsync(pReference, pParent);
			var result = await operation;
			if (result != null && result.TryGetComponent(out TComponent com))
				return com;
			return null;
		}
		public static async UniTask<List<GameObject>> InstantiateAsync(IList<IResourceLocation> pLocations)
		{
			var tasks = new Task<GameObject>[pLocations.Count];
			for (int i = 0; i < pLocations.Count; i++)
			{
				var location = pLocations[i];
				var operation = Addressables.InstantiateAsync(location);
				tasks[i] = operation.Task;
			}
			await Task.WhenAll(tasks);
			var results = new List<GameObject>();
			foreach (var task in tasks)
				results.Add(task.Result);
			return results;
		}
		public static async UniTask<List<GameObject>> InstantiateAsync(List<AssetReference> pReferences, Transform pParent)
		{
			var tasks = new Task<GameObject>[pReferences.Count];
			for (int i = 0; i < pReferences.Count; i++)
			{
				var preference = pReferences[i];
				var operation = Addressables.InstantiateAsync(preference, pParent);
				tasks[i] = operation.Task;
			}
			await Task.WhenAll(tasks);
			var results = new List<GameObject>();
			foreach (var task in tasks)
				results.Add(task.Result);
			return results;
		}
		public static async UniTask<List<GameObject>> InstantiateAsync<TReference>(List<TReference> pReferences, Transform pParent) where TReference : AssetReference
		{
			var tasks = new Task<GameObject>[pReferences.Count];
			for (int i = 0; i < pReferences.Count; i++)
			{
				var preference = pReferences[i];
				var operation = Addressables.InstantiateAsync(preference, pParent);
				tasks[i] = operation.Task;
			}
			await Task.WhenAll(tasks);
			var results = new List<GameObject>();
			foreach (var task in tasks)
				results.Add(task.Result);
			return results;
		}

#endregion

#region Load Asset Generic

		public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(string pAddress, Action<TObject> pOnComplete, Action<float> pProgress = null) where TObject : Object
		{
			var operation = Addressables.LoadAssetAsync<TObject>(pAddress);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<TObject> LoadAssetAsync<TObject>(string pAddress) where TObject : Object
		{
			var operation = Addressables.LoadAssetAsync<TObject>(pAddress);
			var result = await operation;
			return result;
		}
		public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject, TReference>(TReference pReference, Action<TObject> pOnComplete, Action<float> pProgress = null) where TObject : Object
			where TReference : AssetReference
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync<TObject>();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<TObject> LoadAssetAsync<TObject, TReference>(TReference pReference) where TObject : Object where TReference : AssetReference
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync<TObject>();
			var result = await operation;
			return result;
		}
		public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(AssetReferenceT<TObject> pReference, Action<TObject> pOnComplete, Action<float> pProgress = null) where TObject : Object
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<TObject> LoadAssetAsync<TObject>(AssetReferenceT<TObject> pReference) where TObject : Object
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}
		public static async UniTask<TObject> LoadAssetAsync<TObject>(AssetReference pReference) where TObject : Object
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync<TObject>();
			var result = await operation.Task;
			return result;
		}

#endregion

#region Load Prefab

		public static async UniTask<TComponent> LoadPrefabAsync<TComponent>(AssetReference pReference) where TComponent : Component
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<GameObject>() : pReference.LoadAssetAsync<GameObject>();
			var result = await operation;
			if (result != null && result.TryGetComponent(out TComponent component))
				return component;
			return null;
		}
		public static async UniTask<TComponent> LoadPrefabAsync<TComponent>(ComponentRef<TComponent> pReference) where TComponent : Component
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<GameObject>() : pReference.LoadAssetAsync<GameObject>();
			var result = await operation;
			if (result != null && result.TryGetComponent(out TComponent component))
				return component;
			return null;
		}
		public static async UniTask<GameObject> LoadGameObjectAsync(AssetReferenceGameObject pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<GameObject>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}

#endregion

#region Load Asset

		public static AsyncOperationHandle<TextAsset> LoadTextAssetAsync(string pAddress, Action<TextAsset> pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.LoadAssetAsync<TextAsset>(pAddress);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<TextAsset> LoadTextAssetAsync(string pAddress)
		{
			var operation = Addressables.LoadAssetAsync<TextAsset>(pAddress);
			var result = await operation;
			return result;
		}
		public static AsyncOperationHandle<Sprite> LoadSpriteAsync(AssetReferenceSprite pReference, Action<Sprite> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Sprite>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<Sprite> LoadSpriteAsync(AssetReferenceSprite pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Sprite>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}
		public static async Task<Sprite[]> LoadSpriteAsync(AssetReferenceSprite[] pReferences)
		{
			var results = new Sprite[pReferences.Length];
			for (int i = 0; i < pReferences.Length; i++)
			{
				var operation = pReferences[i].IsValid()
					? pReferences[i].OperationHandle.Convert<Sprite>()
					: pReferences[i].LoadAssetAsync();
				var result = await operation;
				results[i] = result;
			}
			return results;
		}
		public static AsyncOperationHandle<IList<Sprite>> LoadSpritesAsync(AssetReference pReference, Action<IList<Sprite>> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<IList<Sprite>>() : pReference.LoadAssetAsync<IList<Sprite>>();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<IList<Sprite>> LoadSpritesAsync(AssetReference pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<IList<Sprite>>() : pReference.LoadAssetAsync<IList<Sprite>>();
			var result = await operation;
			return result;
		}
		public static AsyncOperationHandle<Texture> LoadTextureAsync(AssetReferenceTexture pReference, Action<Texture> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<Texture> LoadTextureAsync(AssetReferenceTexture pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}
		public static AsyncOperationHandle<Texture2D> LoadTexture2DAsync(AssetReferenceTexture2D pReference, Action<Texture2D> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture2D>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<Texture2D> LoadTexture2DAsync(AssetReferenceTexture2D pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture2D>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}
		public static AsyncOperationHandle<Texture3D> LoadTexture3DAsync(AssetReferenceTexture3D pReference, Action<Texture3D> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture3D>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		public static async UniTask<Texture3D> LoadTexture3DAsync(AssetReferenceTexture3D pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture3D>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}
		public static async Task<IList<TObject>> LoadAssetsAsync<TObject>(IList<IResourceLocation> pLocations, Action<TObject> callback) where TObject : Object
		{
			var operation = Addressables.LoadAssetsAsync(pLocations, callback);
			var result = await operation;
			return result;
		}

#endregion

#region Tasks Handle

		private static async UniTask WaitLoadTask(AsyncOperationHandle operation, Action pOnComplete)
		{
			await operation;
			pOnComplete?.Invoke();

			if (operation.Status == AsyncOperationStatus.Failed)
				Debug.LogError("Failed to load asset: " + operation.OperationException);
		}
		private static void WaitLoadTask(AsyncOperationHandle operation, Action pOnComplete, Action<float> pProgress)
		{
			if (pProgress == null)
			{
				WaitLoadTask(operation, pOnComplete);
				return;
			}
            TimerEventsGlobal.Instance.WaitForCondition(new ConditionEvent()
			{
				triggerCondition = () => operation.IsDone,
				onUpdate = () =>
				{
					pProgress(operation.PercentComplete);
				},
				onTrigger = () =>
				{
					pOnComplete?.Invoke();

					if (operation.Status == AsyncOperationStatus.Failed)
						Debug.LogError("Failed to load asset: " + operation.OperationException);
				},
			});
		}
		private static async UniTask WaitLoadTask<T>(AsyncOperationHandle<T> operation, Action<T> pOnComplete)
		{
			var result = await operation;
			pOnComplete?.Invoke(result);

			if (operation.Status == AsyncOperationStatus.Failed)
				Debug.LogError("Failed to load asset: " + operation.OperationException);
		}
		private static void WaitLoadTask<T>(AsyncOperationHandle<T> operation, Action<T> pOnComplete, Action<float> pProgress)
		{
			if (pProgress == null)
			{
				WaitLoadTask(operation, pOnComplete);
				return;
			}

            TimerEventsGlobal.Instance.WaitForCondition(new ConditionEvent()
			{
				triggerCondition = () => operation.IsDone,
				onUpdate = () =>
				{
					pProgress(operation.PercentComplete);
				},
				onTrigger = () =>
				{
					try
					{
						pOnComplete?.Invoke(operation.Result);

						if (operation.Status == AsyncOperationStatus.Failed)
							Debug.LogError("Failed to load asset: " + operation.OperationException);
					}
					catch
					{
						// ignored
					}
				},
			});
		}
		private static async UniTask WaitUnloadTask<T>(AsyncOperationHandle<T> operation, Action<bool> pOnComplete)
		{
			await operation;
			pOnComplete?.Invoke(operation.Status == AsyncOperationStatus.Succeeded);

			if (operation.Status == AsyncOperationStatus.Failed)
				Debug.LogError("Failed to unload asset: " + operation.OperationException);
		}
		private static void WaitUnloadTask<T>(AsyncOperationHandle<T> operation, Action<bool> pOnComplete, Action<float> pProgress)
		{
			if (pProgress == null)
			{
				WaitUnloadTask(operation, pOnComplete);
				return;
			}
			TimerEventsGlobal.Instance.WaitForCondition(new ConditionEvent()
			{
				triggerCondition = () => operation.IsDone,
				onUpdate = () =>
				{
					pProgress(operation.PercentComplete);
				},
				onTrigger = () =>
				{
					pOnComplete?.Invoke(operation.Status == AsyncOperationStatus.Succeeded);

					if (operation.Status == AsyncOperationStatus.Failed)
						Debug.LogError("Failed to unload asset: " + operation.OperationException);
				},
			});
		}

#endregion
	}

	[Serializable]
	public class AssetBundleWrap<T> where T : Component
	{
		public Transform parent;
		public ComponentRef<T> reference;
		public bool loading { get; private set; }
		public T asset { get; private set; }
		public T instance { get; private set; }
		private AsyncOperationHandle<GameObject> m_operation;
		public async UniTask<T> InstantiateAsync(bool defaultActive = false)
		{
			UnityEngine.Debug.Assert(parent != null, "parent != null");
			if (instance != null) return instance;
			if (asset != null)
			{
				instance = Object.Instantiate(asset, parent);
				instance.SetActive(defaultActive);
				instance.name = asset.name;
			}
			else
			{
				loading = true;
				m_operation = Addressables.InstantiateAsync(reference, parent);
				var go = await m_operation;
				loading = false;
				go.SetActive(defaultActive);
				go.name = go.name.Replace("(Clone)", "");
				instance = go.GetComponent<T>();
				Debug.Log($"Instantiate Asset Bundle {instance.name}");
				return instance;
			}
			return instance;
		}
		public async UniTask<T> LoadAsync()
		{
			if (asset != null) return asset;
			if (asset == null)
			{
				loading = true;
				m_operation = Addressables.LoadAssetAsync<GameObject>(reference);
				var obj = await m_operation;
				if (obj)
					asset = obj.GetComponent<T>();
				loading = false;

				if (asset != null)
					Debug.Log($"Load Asset Bundle {asset.name}");
			}
			return asset;
		}
		//NOTE: Coroutine doesn't wait UniTask
		public IEnumerator IEInstantiate(bool defaultActive = false)
		{
			if (instance != null)
			{
				yield break;
			}
			if (asset != null)
			{
				instance = Object.Instantiate(asset, parent);
				instance.SetActive(defaultActive);
				instance.name = asset.name;
			}
			else
			{
				loading = true;
				m_operation = Addressables.InstantiateAsync(reference, parent);
				yield return m_operation;
				var go = m_operation.Result;
				loading = false;
				go.SetActive(defaultActive);
				go.name = go.name.Replace("(Clone)", "");
				instance = go.GetComponent<T>();
				Debug.Log($"Instantiate Asset Bundle {instance.name}");
			}
		}
		public IEnumerator IELoad()
		{
			if (asset != null)
				yield break;
			loading = true;
			m_operation = Addressables.LoadAssetAsync<GameObject>(reference);
			yield return m_operation;
			if (m_operation.Result)
				asset = m_operation.Result.GetComponent<T>();
			loading = false;

			if (asset != null)
				Debug.Log($"Load Asset Bundle {asset.name}");
		}
		public bool InstanceLoaded(bool active = false)
		{
			if (instance == null && asset != null)
			{
				instance = Object.Instantiate(asset, parent);
				instance.SetActive(active);
				instance.name = asset.name;
			}
			if (instance == null)
				InstantiateAsync(active);
			return instance != null;
		}
		public void Unload()
		{
			try
			{
				if (asset != null)
				{
					Debug.Log($"Unload Asset Bundle {asset.name}");
					if (instance != null)
						Object.Destroy(instance.gameObject);
					if (m_operation.IsValid())
						Addressables.Release(m_operation);
				}
				else if (instance != null)
				{
					string instanceName = instance.name;
					if (m_operation.IsValid() && Addressables.ReleaseInstance(m_operation))
						Debug.Log($"Unload Asset Bundle {instanceName}");
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError(ex);
			}
		}
	}

	[Serializable]
	public class AssetBundleRef<M> where M : Object
	{
		public AssetReferenceT<M> reference;
		private AsyncOperationHandle<M> m_operation;
		public M asset { get; set; }
		public async UniTask<M> LoadAsync() //NOTE: this function should be awaited in an async, Coroutine does not working correctly
		{
			if (asset != null)
				return asset;
			m_operation = Addressables.LoadAssetAsync<M>(reference);
			await m_operation;
			asset = m_operation.Result;
			// Debug.Log($"Load Asset Bundle {asset.name}");
			return asset;
		}
		public IEnumerator IELoad()
		{
			if (asset != null)
				yield break;
			m_operation = Addressables.LoadAssetAsync<M>(reference);
			yield return m_operation;
			asset = m_operation.Result;
		}
		public void Unload()
		{
			if (m_operation.IsValid())
			{
				// Debug.Log($"Unload Asset Bundle {asset.name}");
				Addressables.Release(m_operation);
			}
		}
	}

	[Serializable]
	public class AssetBundleWithEnumKey<T, M> : AssetBundleRef<M> where T : Enum where M : Object
	{
		[FormerlySerializedAs("id")] public T key;
	}

	[Serializable]
	public class AssetBundleWith2EnumKeys<T1, T2, M> : AssetBundleRef<M>
		where T1 : Enum
		where T2 : Enum
		where M : Object
	{
		public T1 key1;
		public T2 key2;
	}

	[Serializable]
	public class AssetBundleWithIntKey<M> : AssetBundleRef<M> where M : Object
	{
		[FormerlySerializedAs("id")] public int key;
	}

#endif
}