/***
 * Author HNB-RaBear - 2020
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Cysharp.Threading.Tasks;

namespace RCore
{
#if ADDRESSABLES

	/// <summary>
	/// Provides a set of static helper methods for working with Unity's Addressable Assets system.
	/// Simplifies loading, instantiation, and management of addressable assets.
	/// </summary>
	public static class AddressableUtil
	{
		/// <summary>
		/// Gets the required download size for the specified addressable key.
		/// </summary>
		/// <param name="key">The key of the addressable asset (e.g., address or label).</param>
		/// <param name="pOnComplete">Action to be invoked with the download size in bytes.</param>
		/// <returns>An operation handle for tracking the request.</returns>
		public static AsyncOperationHandle<long> GetDownloadSizeAsync(object key, Action<long> pOnComplete)
		{
			var operation = Addressables.GetDownloadSizeAsync(key);
			operation.Completed += (op) =>
			{
				pOnComplete?.Invoke(op.Result);
			};
			return operation;
		}

		/// <summary>
		/// Asynchronously gets the required download size for the specified addressable key.
		/// This method clears the dependency cache before checking the size.
		/// </summary>
		/// <param name="key">The key of the addressable asset (e.g., address or label).</param>
		/// <returns>A task that returns the download size in bytes.</returns>
		public static async Task<long> GetDownloadSizeAsync(object key)
		{
			//Clear all cached AssetBundles
			Addressables.ClearDependencyCacheAsync(key);

			//Check the download size
			var operation = Addressables.GetDownloadSizeAsync(key);
			await operation.Task;
			return operation.Result;
		}

		/// <summary>
		/// Asynchronously loads the resource locations for a given addressable label.
		/// </summary>
		/// <param name="pLabel">The addressable label to look up.</param>
		/// <returns>A task that returns a list of resource locations.</returns>
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
		/// Checks for remote catalog updates and applies them if available.
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

		/// <summary>
		/// Downloads dependencies for a given addressable key.
		/// </summary>
		/// <param name="pKey">The key of the addressable asset.</param>
		/// <param name="pAutoRelease">If true, the handle will be released automatically upon completion.</param>
		/// <param name="pOnComplete">Action to be invoked upon completion.</param>
		/// <param name="pProgress">Action to be invoked with download progress (0.0 to 1.0).</param>
		/// <returns>An operation handle for tracking the download.</returns>
		public static AsyncOperationHandle DownloadDependenciesAsync(object pKey, bool pAutoRelease, Action pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.DownloadDependenciesAsync(pKey, pAutoRelease);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}

		/// <summary>
		/// Asynchronously downloads dependencies for a given addressable key.
		/// </summary>
		/// <param name="pKey">The key of the addressable asset.</param>
		/// <param name="pAutoRelease">If true, the handle will be released automatically upon completion.</param>
		public static async UniTask DownloadDependenciesAsync(object pKey, bool pAutoRelease)
		{
			var operation = Addressables.DownloadDependenciesAsync(pKey, pAutoRelease);
			await operation;
		}
		
		/// <summary>
		/// Downloads dependencies for a list of resource locations.
		/// </summary>
		/// <param name="locations">The list of resource locations.</param>
		/// <param name="pAutoRelease">If true, the handle will be released automatically upon completion.</param>
		/// <param name="pOnComplete">Action to be invoked upon completion.</param>
		/// <param name="pProgress">Action to be invoked with download progress (0.0 to 1.0).</param>
		/// <returns>An operation handle for tracking the download.</returns>
		public static AsyncOperationHandle DownloadDependenciesAsync(IList<IResourceLocation> locations, bool pAutoRelease, Action pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.DownloadDependenciesAsync(locations, pAutoRelease);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}

		/// <summary>
		/// Asynchronously downloads dependencies for a list of resource locations.
		/// </summary>
		/// <param name="locations">The list of resource locations.</param>
		/// <param name="pAutoRelease">If true, the handle will be released automatically upon completion.</param>
		public static async UniTask DownloadDependenciesAsync(IList<IResourceLocation> locations, bool pAutoRelease)
		{
			var operation = Addressables.DownloadDependenciesAsync(locations, pAutoRelease);
			await operation;
		}

		/// <summary>
		/// Downloads dependencies for a given address.
		/// </summary>
		/// <param name="pAddress">The address of the asset.</param>
		/// <param name="pAutoRelease">If true, the handle will be released automatically upon completion.</param>
		/// <param name="pOnComplete">Action to be invoked upon completion.</param>
		/// <param name="pProgress">Action to be invoked with download progress (0.0 to 1.0).</param>
		/// <returns>An operation handle for tracking the download.</returns>
		public static AsyncOperationHandle DownloadDependenciesAsync(string pAddress, bool pAutoRelease, Action pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.DownloadDependenciesAsync(pAddress, pAutoRelease);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		
		/// <summary>
		/// Asynchronously downloads dependencies for a given address.
		/// </summary>
		/// <param name="pAddress">The address of the asset.</param>
		/// <param name="pAutoRelease">If true, the handle will be released automatically upon completion.</param>
		public static async UniTask DownloadDependenciesAsync(string pAddress, bool pAutoRelease)
		{
			var operation = Addressables.DownloadDependenciesAsync(pAddress, pAutoRelease);
			await operation;
		}

		/// <summary>
		/// Downloads dependencies for a given AssetReference.
		/// </summary>
		/// <param name="pReference">The AssetReference of the asset.</param>
		/// <param name="pAutoRelease">If true, the handle will be released automatically upon completion.</param>
		/// <param name="pOnComplete">Action to be invoked upon completion.</param>
		/// <param name="pProgress">Action to be invoked with download progress (0.0 to 1.0).</param>
		/// <returns>An operation handle for tracking the download.</returns>
		public static AsyncOperationHandle DownloadDependenciesAsync(AssetReference pReference, bool pAutoRelease, Action pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.DownloadDependenciesAsync(pReference, pAutoRelease);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		
		/// <summary>
		/// Asynchronously downloads dependencies for a given AssetReference.
		/// </summary>
		/// <param name="pReference">The AssetReference of the asset.</param>
		/// <param name="pAutoRelease">If true, the handle will be released automatically upon completion.</param>
		public static async UniTask DownloadDependenciesAsync(AssetReference pReference, bool pAutoRelease)
		{
			var operation = Addressables.DownloadDependenciesAsync(pReference, pAutoRelease);
			await operation;
		}

#endregion

#region Load/Unload Scene

		/// <summary>
		/// Loads an addressable scene.
		/// </summary>
		/// <param name="pAddress">The address of the scene.</param>
		/// <param name="pMode">The scene loading mode.</param>
		/// <param name="pOnComplete">Action invoked with the loaded scene instance.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the scene load.</returns>
		public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(string pAddress, LoadSceneMode pMode, Action<SceneInstance> pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.LoadSceneAsync(pAddress, pMode);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		
		/// <summary>
		/// Asynchronously loads an addressable scene.
		/// </summary>
		/// <param name="pAddress">The address of the scene.</param>
		/// <param name="pMode">The scene loading mode.</param>
		/// <returns>A task that returns the loaded scene instance.</returns>
		public static async UniTask<SceneInstance> LoadSceneAsync(string pAddress, LoadSceneMode pMode)
		{
			var operation = Addressables.LoadSceneAsync(pAddress, pMode);
			var result = await operation;
			return result;
		}

		/// <summary>
		/// Unloads an addressable scene.
		/// </summary>
		/// <param name="pScene">The scene instance to unload.</param>
		/// <param name="pOnComplete">Action invoked with a boolean indicating success.</param>
		/// <param name="pProgress">Action invoked with unloading progress.</param>
		/// <returns>An operation handle for tracking the scene unload.</returns>
		public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance pScene, Action<bool> pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.UnloadSceneAsync(pScene);
			WaitUnloadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		
		/// <summary>
		/// Asynchronously unloads an addressable scene.
		/// </summary>
		/// <param name="pScene">The scene instance to unload.</param>
		/// <returns>A task that returns the unloaded scene instance.</returns>
		public static async UniTask<SceneInstance> UnloadSceneAsync(SceneInstance pScene)
		{
			var operation = Addressables.UnloadSceneAsync(pScene);
			var result = await operation;
			return result;
		}

#endregion

#region Load Assets Generic

		/// <summary>
		/// Asynchronously loads a list of assets from their addresses.
		/// </summary>
		/// <typeparam name="TObject">The type of assets to load.</typeparam>
		/// <param name="pAddresses">A list of asset addresses.</param>
		/// <returns>A task that returns a list of loaded assets.</returns>
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

		/// <summary>
		/// Asynchronously loads a list of assets from their AssetReferences.
		/// </summary>
		/// <typeparam name="TObject">The type of assets to load.</typeparam>
		/// <param name="pReferences">A list of asset references.</param>
		/// <returns>A task that returns a list of loaded assets.</returns>
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

		/// <summary>
		/// Asynchronously loads a list of assets from a list of typed AssetReferences.
		/// </summary>
		/// <typeparam name="TObject">The type of assets to load.</typeparam>
		/// <typeparam name="TReference">The type of AssetReference.</typeparam>
		/// <param name="pReferences">A list of typed asset references.</param>
		/// <returns>A task that returns a list of loaded assets.</returns>
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

		/// <summary>
		/// Instantiates a GameObject from an AssetReference.
		/// </summary>
		/// <typeparam name="TReference">The type of AssetReference.</typeparam>
		/// <param name="pReference">The reference to the asset to instantiate.</param>
		/// <param name="parent">The parent transform for the new instance.</param>
		/// <param name="pOnComplete">Action invoked with the instantiated GameObject.</param>
		/// <param name="pProgress">Action invoked with instantiation progress.</param>
		/// <returns>An operation handle for tracking the instantiation.</returns>
		public static AsyncOperationHandle<GameObject> InstantiateAsync<TReference>(TReference pReference, Transform parent, Action<GameObject> pOnComplete, Action<float> pProgress = null)
			where TReference : AssetReference
		{
			var operation = Addressables.InstantiateAsync(pReference, parent);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		
		/// <summary>
		/// Instantiates a GameObject from an AssetReference and gets a component from it.
		/// </summary>
		/// <typeparam name="TComponent">The component type to retrieve.</typeparam>
		/// <typeparam name="TReference">The type of AssetReference.</typeparam>
		/// <param name="pReference">The reference to the asset to instantiate.</param>
		/// <param name="parent">The parent transform for the new instance.</param>
		/// <param name="pOnComplete">Action invoked with the retrieved component.</param>
		/// <param name="pProgress">Action invoked with instantiation progress.</param>
		/// <returns>An operation handle for tracking the instantiation.</returns>
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
		
		/// <summary>
		/// Asynchronously instantiates a GameObject from a typed AssetReference.
		/// </summary>
		/// <typeparam name="TReference">The type of AssetReference.</typeparam>
		/// <param name="pReference">The reference to the asset to instantiate.</param>
		/// <param name="pParent">The parent transform for the new instance.</param>
		/// <returns>A task that returns the instantiated GameObject.</returns>
		public static async UniTask<GameObject> InstantiateAsync<TReference>(TReference pReference, Transform pParent) where TReference : AssetReference
		{
			var operation = Addressables.InstantiateAsync(pReference, pParent);
			var result = await operation;
			return result;
		}
		
		/// <summary>
		/// Asynchronously instantiates a GameObject from an AssetReference.
		/// </summary>
		/// <param name="pReference">The reference to the asset to instantiate.</param>
		/// <param name="pParent">The parent transform for the new instance.</param>
		/// <returns>A task that returns the instantiated GameObject.</returns>
		public static async UniTask<GameObject> InstantiateAsync(AssetReference pReference, Transform pParent)
		{
			var operation = Addressables.InstantiateAsync(pReference, pParent);
			var result = await operation.Task;
			return result;
		}
		
		/// <summary>
		/// Asynchronously instantiates a GameObject from an AssetReference and returns a specified component.
		/// </summary>
		/// <typeparam name="TComponent">The component type to retrieve.</typeparam>
		/// <typeparam name="TReference">The type of AssetReference.</typeparam>
		/// <param name="pReference">The reference to the asset to instantiate.</param>
		/// <param name="pParent">The parent transform for the new instance.</param>
		/// <returns>A task that returns the retrieved component, or null if not found.</returns>
		public static async UniTask<TComponent> InstantiateAsync<TComponent, TReference>(TReference pReference, Transform pParent) where TComponent : Component where TReference : AssetReference
		{
			var operation = Addressables.InstantiateAsync(pReference, pParent);
			var result = await operation;
			if (result != null && result.TryGetComponent(out TComponent com))
				return com;
			return null;
		}
		
		/// <summary>
		/// Asynchronously instantiates multiple GameObjects from a list of resource locations.
		/// </summary>
		/// <param name="pLocations">The list of resource locations to instantiate.</param>
		/// <returns>A task that returns a list of instantiated GameObjects.</returns>
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
		
		/// <summary>
		/// Asynchronously instantiates a list of GameObjects from AssetReferences.
		/// </summary>
		/// <param name="pReferences">The list of AssetReferences to instantiate.</param>
		/// <param name="pParent">The parent transform for the new instances.</param>
		/// <returns>A task that returns a list of instantiated GameObjects.</returns>
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
		
		/// <summary>
		/// Asynchronously instantiates a list of GameObjects from typed AssetReferences.
		/// </summary>
		/// <typeparam name="TReference">The type of the AssetReferences.</typeparam>
		/// <param name="pReferences">The list of typed AssetReferences to instantiate.</param>
		/// <param name="pParent">The parent transform for the new instances.</param>
		/// <returns>A task that returns a list of instantiated GameObjects.</returns>
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

		/// <summary>
		/// Loads an asset from an address.
		/// </summary>
		/// <typeparam name="TObject">The type of asset to load.</typeparam>
		/// <param name="pAddress">The address of the asset.</param>
		/// <param name="pOnComplete">Action invoked with the loaded asset.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the load.</returns>
		public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(string pAddress, Action<TObject> pOnComplete, Action<float> pProgress = null) where TObject : Object
		{
			var operation = Addressables.LoadAssetAsync<TObject>(pAddress);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}

		/// <summary>
		/// Asynchronously loads an asset from an address.
		/// </summary>
		/// <typeparam name="TObject">The type of asset to load.</typeparam>
		/// <param name="pAddress">The address of the asset.</param>
		/// <returns>A task that returns the loaded asset.</returns>
		public static async UniTask<TObject> LoadAssetAsync<TObject>(string pAddress) where TObject : Object
		{
			var operation = Addressables.LoadAssetAsync<TObject>(pAddress);
			var result = await operation;
			return result;
		}
		
		/// <summary>
		/// Loads an asset from a typed AssetReference.
		/// </summary>
		/// <typeparam name="TObject">The type of asset to load.</typeparam>
		/// <typeparam name="TReference">The type of the AssetReference.</typeparam>
		/// <param name="pReference">The typed reference to the asset.</param>
		/// <param name="pOnComplete">Action invoked with the loaded asset.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the load.</returns>
		public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject, TReference>(TReference pReference, Action<TObject> pOnComplete, Action<float> pProgress = null) where TObject : Object
			where TReference : AssetReference
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync<TObject>();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}

		/// <summary>
		/// Asynchronously loads an asset from a typed AssetReference.
		/// </summary>
		/// <typeparam name="TObject">The type of asset to load.</typeparam>
		/// <typeparam name="TReference">The type of the AssetReference.</typeparam>
		/// <param name="pReference">The typed reference to the asset.</param>
		/// <returns>A task that returns the loaded asset.</returns>
		public static async UniTask<TObject> LoadAssetAsync<TObject, TReference>(TReference pReference) where TObject : Object where TReference : AssetReference
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync<TObject>();
			var result = await operation;
			return result;
		}
		
		/// <summary>
		/// Loads an asset from an AssetReferenceT.
		/// </summary>
		/// <typeparam name="TObject">The type of asset to load.</typeparam>
		/// <param name="pReference">The strongly-typed reference to the asset.</param>
		/// <param name="pOnComplete">Action invoked with the loaded asset.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the load.</returns>
		public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(AssetReferenceT<TObject> pReference, Action<TObject> pOnComplete, Action<float> pProgress = null) where TObject : Object
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}

		/// <summary>
		/// Asynchronously loads an asset from an AssetReferenceT.
		/// </summary>
		/// <typeparam name="TObject">The type of asset to load.</typeparam>
		/// <param name="pReference">The strongly-typed reference to the asset.</param>
		/// <returns>A task that returns the loaded asset.</returns>
		public static async UniTask<TObject> LoadAssetAsync<TObject>(AssetReferenceT<TObject> pReference) where TObject : Object
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}

		/// <summary>
		/// Asynchronously loads an asset from an AssetReference.
		/// </summary>
		/// <typeparam name="TObject">The type of asset to load.</typeparam>
		/// <param name="pReference">The reference to the asset.</param>
		/// <returns>A task that returns the loaded asset.</returns>
		public static async UniTask<TObject> LoadAssetAsync<TObject>(AssetReference pReference) where TObject : Object
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<TObject>() : pReference.LoadAssetAsync<TObject>();
			var result = await operation.Task;
			return result;
		}

#endregion

#region Load Prefab

		/// <summary>
		/// Asynchronously loads a prefab from an AssetReference and returns a specified component from it.
		/// Note: This loads the asset, it does not instantiate it.
		/// </summary>
		/// <typeparam name="TComponent">The component type to retrieve from the loaded prefab.</typeparam>
		/// <param name="pReference">The reference to the prefab asset.</param>
		/// <returns>A task that returns the component from the loaded prefab, or null if not found.</returns>
		public static async UniTask<TComponent> LoadPrefabAsync<TComponent>(AssetReference pReference) where TComponent : Component
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<GameObject>() : pReference.LoadAssetAsync<GameObject>();
			var result = await operation;
			if (result != null && result.TryGetComponent(out TComponent component))
				return component;
			return null;
		}
		
		/// <summary>
		/// Asynchronously loads a prefab from a ComponentRef and returns its component.
		/// Note: This loads the asset, it does not instantiate it.
		/// </summary>
		/// <typeparam name="TComponent">The component type to retrieve from the loaded prefab.</typeparam>
		/// <param name="pReference">The component reference to the prefab asset.</param>
		/// <returns>A task that returns the component from the loaded prefab, or null if not found.</returns>
		public static async UniTask<TComponent> LoadPrefabAsync<TComponent>(ComponentRef<TComponent> pReference) where TComponent : Component
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<GameObject>() : pReference.LoadAssetAsync<GameObject>();
			var result = await operation;
			if (result != null && result.TryGetComponent(out TComponent component))
				return component;
			return null;
		}

		/// <summary>
		/// Asynchronously loads a prefab as a GameObject from an AssetReferenceGameObject.
		/// Note: This loads the asset, it does not instantiate it.
		/// </summary>
		/// <param name="pReference">The reference to the GameObject asset.</param>
		/// <returns>A task that returns the loaded GameObject asset.</returns>
		public static async UniTask<GameObject> LoadGameObjectAsync(AssetReferenceGameObject pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<GameObject>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}

#endregion

#region Load Asset

		/// <summary>
		/// Loads a TextAsset from an address.
		/// </summary>
		/// <param name="pAddress">The address of the TextAsset.</param>
		/// <param name="pOnComplete">Action invoked with the loaded TextAsset.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the load.</returns>
		public static AsyncOperationHandle<TextAsset> LoadTextAssetAsync(string pAddress, Action<TextAsset> pOnComplete, Action<float> pProgress = null)
		{
			var operation = Addressables.LoadAssetAsync<TextAsset>(pAddress);
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}

		/// <summary>
		/// Asynchronously loads a TextAsset from an address.
		/// </summary>
		/// <param name="pAddress">The address of the TextAsset.</param>
		/// <returns>A task that returns the loaded TextAsset.</returns>
		public static async UniTask<TextAsset> LoadTextAssetAsync(string pAddress)
		{
			var operation = Addressables.LoadAssetAsync<TextAsset>(pAddress);
			var result = await operation;
			return result;
		}

		/// <summary>
		/// Loads a Sprite from an AssetReferenceSprite.
		/// </summary>
		/// <param name="pReference">The reference to the Sprite asset.</param>
		/// <param name="pOnComplete">Action invoked with the loaded Sprite.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the load.</returns>
		public static AsyncOperationHandle<Sprite> LoadSpriteAsync(AssetReferenceSprite pReference, Action<Sprite> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Sprite>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}

		/// <summary>
		/// Asynchronously loads a Sprite from an AssetReferenceSprite.
		/// </summary>
		/// <param name="pReference">The reference to the Sprite asset.</param>
		/// <returns>A task that returns the loaded Sprite.</returns>
		public static async UniTask<Sprite> LoadSpriteAsync(AssetReferenceSprite pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Sprite>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}

		/// <summary>
		/// Asynchronously loads an array of Sprites from an array of AssetReferenceSprites.
		/// </summary>
		/// <param name="pReferences">The array of references to the Sprite assets.</param>
		/// <returns>A task that returns an array of loaded Sprites.</returns>
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

		/// <summary>
		/// Loads a list of Sprites from a single AssetReference (typically pointing to a Sprite Atlas).
		/// </summary>
		/// <param name="pReference">The reference to the asset (e.g., Sprite Atlas).</param>
		/// <param name="pOnComplete">Action invoked with the loaded list of Sprites.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the load.</returns>
		public static AsyncOperationHandle<IList<Sprite>> LoadSpritesAsync(AssetReference pReference, Action<IList<Sprite>> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<IList<Sprite>>() : pReference.LoadAssetAsync<IList<Sprite>>();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}

		/// <summary>
		/// Asynchronously loads a list of Sprites from a single AssetReference (typically pointing to a Sprite Atlas).
		/// </summary>
		/// <param name="pReference">The reference to the asset (e.g., Sprite Atlas).</param>
		/// <returns>A task that returns a list of loaded Sprites.</returns>
		public static async UniTask<IList<Sprite>> LoadSpritesAsync(AssetReference pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<IList<Sprite>>() : pReference.LoadAssetAsync<IList<Sprite>>();
			var result = await operation;
			return result;
		}
		
		/// <summary>
		/// Loads a Texture from an AssetReferenceTexture.
		/// </summary>
		/// <param name="pReference">The reference to the Texture asset.</param>
		/// <param name="pOnComplete">Action invoked with the loaded Texture.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the load.</returns>
		public static AsyncOperationHandle<Texture> LoadTextureAsync(AssetReferenceTexture pReference, Action<Texture> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		
		/// <summary>
		/// Asynchronously loads a Texture from an AssetReferenceTexture.
		/// </summary>
		/// <param name="pReference">The reference to the Texture asset.</param>
		/// <returns>A task that returns the loaded Texture.</returns>
		public static async UniTask<Texture> LoadTextureAsync(AssetReferenceTexture pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}

		/// <summary>
		/// Loads a Texture2D from an AssetReferenceTexture2D.
		/// </summary>
		/// <param name="pReference">The reference to the Texture2D asset.</param>
		/// <param name="pOnComplete">Action invoked with the loaded Texture2D.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the load.</returns>
		public static AsyncOperationHandle<Texture2D> LoadTexture2DAsync(AssetReferenceTexture2D pReference, Action<Texture2D> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture2D>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		
		/// <summary>
		/// Asynchronously loads a Texture2D from an AssetReferenceTexture2D.
		/// </summary>
		/// <param name="pReference">The reference to the Texture2D asset.</param>
		/// <returns>A task that returns the loaded Texture2D.</returns>
		public static async UniTask<Texture2D> LoadTexture2DAsync(AssetReferenceTexture2D pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture2D>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}

		/// <summary>
		/// Loads a Texture3D from an AssetReferenceTexture3D.
		/// </summary>
		/// <param name="pReference">The reference to the Texture3D asset.</param>
		/// <param name="pOnComplete">Action invoked with the loaded Texture3D.</param>
		/// <param name="pProgress">Action invoked with loading progress.</param>
		/// <returns>An operation handle for tracking the load.</returns>
		public static AsyncOperationHandle<Texture3D> LoadTexture3DAsync(AssetReferenceTexture3D pReference, Action<Texture3D> pOnComplete, Action<float> pProgress = null)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture3D>() : pReference.LoadAssetAsync();
			WaitLoadTask(operation, pOnComplete, pProgress);
			return operation;
		}
		
		/// <summary>
		/// Asynchronously loads a Texture3D from an AssetReferenceTexture3D.
		/// </summary>
		/// <param name="pReference">The reference to the Texture3D asset.</param>
		/// <returns>A task that returns the loaded Texture3D.</returns>
		public static async UniTask<Texture3D> LoadTexture3DAsync(AssetReferenceTexture3D pReference)
		{
			var operation = pReference.IsValid() ? pReference.OperationHandle.Convert<Texture3D>() : pReference.LoadAssetAsync();
			var result = await operation;
			return result;
		}
		
		/// <summary>
		/// Asynchronously loads multiple assets from a list of resource locations.
		/// </summary>
		/// <typeparam name="TObject">The type of assets to load.</typeparam>
		/// <param name="pLocations">The list of resource locations.</param>
		/// <param name="callback">A callback invoked for each asset loaded.</param>
		/// <returns>A task that returns a list of loaded assets.</returns>
		public static async Task<IList<TObject>> LoadAssetsAsync<TObject>(IList<IResourceLocation> pLocations, Action<TObject> callback) where TObject : Object
		{
			var operation = Addressables.LoadAssetsAsync(pLocations, callback);
			var result = await operation;
			return result;
		}

#endregion

#region Tasks Handle

		/// <summary>
		/// Internal helper to await an operation handle and invoke a completion callback.
		/// </summary>
		private static async UniTask WaitLoadTask(AsyncOperationHandle operation, Action pOnComplete)
		{
			await operation;
			pOnComplete?.Invoke();

			if (operation.Status == AsyncOperationStatus.Failed)
				Debug.LogError("Failed to load asset: " + operation.OperationException);
		}
		
		/// <summary>
		/// Internal helper to handle an operation with progress and completion callbacks.
		/// </summary>
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

		/// <summary>
		/// Internal helper to await a generic operation handle and invoke a completion callback with the result.
		/// </summary>
		private static async UniTask WaitLoadTask<T>(AsyncOperationHandle<T> operation, Action<T> pOnComplete)
		{
			var result = await operation;
			pOnComplete?.Invoke(result);

			if (operation.Status == AsyncOperationStatus.Failed)
				Debug.LogError("Failed to load asset: " + operation.OperationException);
		}
		
		/// <summary>
		/// Internal helper to handle a generic operation with progress and completion callbacks.
		/// </summary>
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
		
		/// <summary>
		/// Internal helper to await an unload operation and invoke a completion callback.
		/// </summary>
		private static async UniTask WaitUnloadTask<T>(AsyncOperationHandle<T> operation, Action<bool> pOnComplete)
		{
			await operation;
			pOnComplete?.Invoke(operation.Status == AsyncOperationStatus.Succeeded);

			if (operation.Status == AsyncOperationStatus.Failed)
				Debug.LogError("Failed to unload asset: " + operation.OperationException);
		}

		/// <summary>
		/// Internal helper to handle an unload operation with progress and completion callbacks.
		/// </summary>
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

#endif
}