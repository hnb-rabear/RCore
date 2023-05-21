/**
 * Author RadBear - nbhung71711 @gmail.com - 2020
 **/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
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

namespace RCore.Common
{
#if ADDRESSABLES

    [Serializable]
    public class ComponentRef<TComponent> : AssetReference where TComponent : Component
    {
        public ComponentRef(string guid) : base(guid)
        {
        }

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
    }

    /// <summary>
    /// Example generic asset reference
    /// </summary>
    [Serializable]
    public class ComponentRef_SpriteRenderer : ComponentRef<SpriteRenderer>
    {
        public ComponentRef_SpriteRenderer(string guid) : base(guid)
        {
        }
    }

    /// <summary>
    /// Example generic asset reference
    /// </summary>
    [Serializable]
    public class AssetRef_SpriteAtlas : AssetReferenceT<SpriteAtlas>
    {
        public AssetRef_SpriteAtlas(string guid) : base(guid)
        {
        }
    }

    //================================================================================

    public static class AddressableUtil
    {
        public static AsyncOperationHandle<long> GetDownloadSizeAsync(object key, Action<long> pOnComplete)
        {
            var operation = Addressables.GetDownloadSizeAsync(key);
            operation.Completed += (op) => { pOnComplete?.Invoke(op.Result); };
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
            checkCatalogHandle.Completed += op => { catalogsToUpdate.AddRange(op.Result); };
            await checkCatalogHandle;
            if (catalogsToUpdate.Count > 0)
            {
                var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate);
                await updateHandle;
            }
        }

#region Download Dependencies

        public static AsyncOperationHandle DownloadDependenciesAsync(object pKey, bool pAutoRelease, Action pOnComplete,
            Action<float> pProgress = null)
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

        public static AsyncOperationHandle DownloadDependenciesAsync(IList<IResourceLocation> locations,
            bool pAutoRelease, Action pOnComplete, Action<float> pProgress = null)
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

        public static AsyncOperationHandle DownloadDependenciesAsync(string pAddress, bool pAutoRelease,
            Action pOnComplete, Action<float> pProgress = null)
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

        public static AsyncOperationHandle DownloadDependenciesAsync(AssetReference pReference, bool pAutoRelease,
            Action pOnComplete, Action<float> pProgress = null)
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

        public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(string pAddress, LoadSceneMode pMode,
            Action<SceneInstance> pOnComplete, Action<float> pProgress = null)
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

        public static AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance pScene,
            Action<bool> pOnComplete, Action<float> pProgress = null)
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

        public static async UniTask<List<TObject>> LoadAssetsAsync<TObject>(List<string> pAddresses)
            where TObject : Object
        {
            var tasks = new List<Task<TObject>>();
            foreach (var address in pAddresses)
            {
                var operation = Addressables.LoadAssetAsync<TObject>(address);
                tasks.Add(operation.Task);
            }
            await Task.WhenAll(tasks);
            var results = new List<TObject>();
            foreach (var task in tasks)
                results.Add(task.Result);
            return results;
        }

        public static async UniTask<List<TObject>> LoadAssetsAsync<TObject>(List<AssetReference> pReferences)
            where TObject : Object
        {
            var tasks = new List<Task<TObject>>();
            foreach (var reference in pReferences)
            {
                var operation = reference.LoadAssetAsync<TObject>();
                tasks.Add(operation.Task);
            }
            await Task.WhenAll(tasks);
            var results = new List<TObject>();
            foreach (var task in tasks)
                results.Add(task.Result);
            return results;
        }

        public static async UniTask<List<TObject>> LoadAssetsAsync<TObject, TReference>(List<TReference> pReferences)
            where TObject : Object where TReference : AssetReference
        {
            var tasks = new List<Task<TObject>>();
            foreach (var r in pReferences)
            {
                var operation = r.LoadAssetAsync<TObject>();
                tasks.Add(operation.Task);
            }
            await Task.WhenAll();
            var results = new List<TObject>();
            foreach (var task in tasks)
                results.Add(task.Result);
            return results;
        }

#endregion

#region Instantiate

        public static AsyncOperationHandle<GameObject> InstantiateAsync<TReference>(TReference pReference,
            Action<GameObject> pOnComplete, Action<float> pProgress = null) where TReference : AssetReference
        {
            var operation = pReference.InstantiateAsync();
            WaitLoadTask(operation, pOnComplete, pProgress);
            return operation;
        }

        public static AsyncOperationHandle<GameObject> InstantiateAsync<TComponent, TReference>(TReference pReference,
            Action<TComponent> pOnComplete, Action<float> pProgress = null) where TComponent : Component
            where TReference : AssetReference
        {
            var operation = pReference.InstantiateAsync();
            WaitLoadTask(operation,
            (result) =>
            {
                result.TryGetComponent(out TComponent component);
                pOnComplete?.Invoke(component);
            },
            pProgress);
            return operation;
        }

        public static async UniTask<GameObject> InstantiateAsync<TReference>(TReference pReference, Transform pParent)
            where TReference : AssetReference
        {
            if (pReference.OperationHandle.IsValid())
                return pReference.OperationHandle.Convert<GameObject>().Result;
            var operation = pReference.InstantiateAsync(pParent);
            var result = await operation;
            return result;
        }

        public static async UniTask<GameObject> InstantiateAsync(AssetReference pReference)
        {
            var operation = pReference.InstantiateAsync();
            var result = await operation.Task;
            return result;
        }

        public static async UniTask<TComponent> InstantiateAsync<TComponent, TReference>(TReference pReference)
            where TComponent : Component where TReference : AssetReference
        {
            var operation = pReference.InstantiateAsync();
            var result = await operation;
            if (result != null && result.TryGetComponent(out TComponent com))
                return com;
            return null;
        }

        public static async UniTask<List<GameObject>> InstantiateAsync(IList<IResourceLocation> pLocations)
        {
            var tasks = new List<Task<GameObject>>();
            foreach (var location in pLocations)
            {
                var operation = Addressables.InstantiateAsync(location);
                tasks.Add(operation.Task);
            }
            await Task.WhenAll(tasks);
            var results = new List<GameObject>();
            foreach (var task in tasks)
                results.Add(task.Result);
            return results;
        }

        public static async UniTask<List<GameObject>> InstantiateAsync(List<AssetReference> pReferences)
        {
            var tasks = new List<Task<GameObject>>();
            foreach (var preference in pReferences)
            {
                var operation = preference.InstantiateAsync();
                tasks.Add(operation.Task);
            }
            await Task.WhenAll(tasks);
            var results = new List<GameObject>();
            foreach (var task in tasks)
                results.Add(task.Result);
            return results;
        }

        public static async UniTask<List<GameObject>> InstantiateAsync<TReference>(List<TReference> pReferences)
            where TReference : AssetReference
        {
            var tasks = new List<Task<GameObject>>();
            foreach (var preference in pReferences)
            {
                var operation = preference.InstantiateAsync();
                tasks.Add(operation.Task);
            }
            await Task.WhenAll(tasks);
            var results = new List<GameObject>();
            foreach (var task in tasks)
                results.Add(task.Result);
            return results;
        }

#endregion

#region Wrap Prefab Handle

        public static async UniTask<WrapPrefab<TComponent>>
            InstantiateAsyncWrap<TComponent, TReference>(TReference pReference) where TComponent : Component
            where TReference : AssetReference
        {
            var wrap = new WrapPrefab<TComponent>();
            var operation = pReference.InstantiateAsync();
            await operation.Task;
            operation.Result.TryGetComponent(out TComponent component);
            wrap.operation = operation;
            wrap.prefab = component;
            return wrap;
        }

        public static async UniTask<List<WrapPrefab<TComponent>>> InstantiateAsyncWrap<TComponent>(
            IList<IResourceLocation> pLocations) where TComponent : Component
        {
            var tasks = new List<Task<GameObject>>();
            var operations = new List<AsyncOperationHandle>();
            foreach (var location in pLocations)
            {
                var operation = Addressables.InstantiateAsync(location);
                tasks.Add(operation.Task);
                operations.Add(operation);
            }
            await Task.WhenAll(tasks);
            var list = new List<WrapPrefab<TComponent>>();
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Result.TryGetComponent(out TComponent component);
                list.Add(new WrapPrefab<TComponent>()
                {
                    operation = operations[i],
                    prefab = component
                });
            }
            return list;
        }

        public static async UniTask<List<WrapPrefab<TComponent>>> InstantiateAsyncWrap<TComponent>(
            List<AssetReference> pReferences) where TComponent : Component
        {
            var tasks = new List<Task<GameObject>>();
            var operations = new List<AsyncOperationHandle>();
            foreach (var preference in pReferences)
            {
                var operation = preference.InstantiateAsync();
                tasks.Add(operation.Task);
                operations.Add(operation);
            }
            await Task.WhenAll(tasks);
            var list = new List<WrapPrefab<TComponent>>();
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Result.TryGetComponent(out TComponent component);
                list.Add(new WrapPrefab<TComponent>()
                {
                    operation = operations[i],
                    prefab = component
                });
            }
            return list;
        }

        public static async UniTask<List<WrapPrefab<TComponent>>>
            InstantiateAsyncWrap<TComponent, TReference>(List<TReference> pReferences) where TComponent : Component
            where TReference : AssetReference
        {
            var tasks = new List<Task<GameObject>>();
            var operations = new List<AsyncOperationHandle>();
            foreach (var preference in pReferences)
            {
                var operation = preference.InstantiateAsync();
                tasks.Add(operation.Task);
                operations.Add(operation);
            }
            await Task.WhenAll(tasks);
            var list = new List<WrapPrefab<TComponent>>();
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Result.TryGetComponent(out TComponent component);
                list.Add(new WrapPrefab<TComponent>()
                {
                    operation = operations[i],
                    prefab = component
                });
            }
            return list;
        }

        public static async UniTask<WrapPrefab<TComponent>> InstantiateAsyncWrap<TComponent>(AssetReference pReference)
            where TComponent : Component
        {
            var wrap = new WrapPrefab<TComponent>();
            var operation = pReference.InstantiateAsync();
            await operation.Task;
            operation.Result.TryGetComponent(out TComponent component);
            wrap.operation = operation;
            wrap.prefab = component;
            return wrap;
        }

        public static async UniTask<WrapPrefab<TComponent>> LoadPrefabAsyncWrap<TComponent>(AssetReference pReference)
            where TComponent : Component
        {
            var wrap = new WrapPrefab<TComponent>();
            var operation = pReference.LoadAssetAsync<GameObject>();
            await operation.Task;
            operation.Result.TryGetComponent(out TComponent component);
            wrap.operation = operation;
            wrap.prefab = component;
            return wrap;
        }

        public static async UniTask<WrapPrefab<TComponent>>
            LoadPrefabAsyncWrap<TComponent, TReference>(TReference pReference) where TComponent : Component
            where TReference : AssetReference
        {
            var wrap = new WrapPrefab<TComponent>();
            var operation = pReference.LoadAssetAsync<GameObject>();
            await operation.Task;
            operation.Result.TryGetComponent(out TComponent component);
            wrap.operation = operation;
            wrap.prefab = component;
            return wrap;
        }

        public static async UniTask<List<WrapPrefab<TComponent>>> LoadPrefabsAsyncWrap<TComponent>(
            List<AssetReference> pReferences) where TComponent : Component
        {
            var tasks = new List<Task<GameObject>>();
            var operations = new List<AsyncOperationHandle>();
            foreach (var reference in pReferences)
            {
                var operation = reference.LoadAssetAsync<GameObject>();
                tasks.Add(operation.Task);
                operations.Add(operation);
            }
            await Task.WhenAll(tasks);
            var list = new List<WrapPrefab<TComponent>>();
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Result.TryGetComponent(out TComponent component);
                list.Add(new WrapPrefab<TComponent>()
                {
                    operation = operations[i],
                    prefab = component
                });
            }
            return list;
        }

        public static async UniTask<List<WrapPrefab<TComponent>>>
            LoadPrefabsAsyncWrap<TComponent, TReference>(List<TReference> pReferences) where TComponent : Component
            where TReference : AssetReference
        {
            var tasks = new List<Task<GameObject>>();
            var operations = new List<AsyncOperationHandle>();
            foreach (var reference in pReferences)
            {
                var operation = reference.LoadAssetAsync<GameObject>();
                tasks.Add(operation.Task);
                operations.Add(operation);
            }
            await Task.WhenAll(tasks);
            var list = new List<WrapPrefab<TComponent>>();
            for (int i = 0; i < tasks.Count; i++)
            {
                tasks[i].Result.TryGetComponent(out TComponent component);
                list.Add(new WrapPrefab<TComponent>()
                {
                    operation = operations[i],
                    prefab = component
                });
            }
            return list;
        }

#endregion

#region Load Asset Generic

        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(string pAddress,
            Action<TObject> pOnComplete, Action<float> pProgress = null) where TObject : Object
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

        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject, TReference>(TReference pAsset,
            Action<TObject> pOnComplete, Action<float> pProgress = null)
            where TObject : Object where TReference : AssetReference
        {
            var operation = pAsset.LoadAssetAsync<TObject>();
            WaitLoadTask(operation, pOnComplete, pProgress);
            return operation;
        }

        public static async UniTask<TObject> LoadAssetAsync<TObject, TReference>(TReference pReference)
            where TObject : Object where TReference : AssetReference
        {
            if (pReference.OperationHandle.IsValid())
                return pReference.OperationHandle.Convert<TObject>().Result;

            var operation = pReference.LoadAssetAsync<TObject>();
            var result = await operation;
            return result;
        }

        public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(AssetReferenceT<TObject> pReference,
            Action<TObject> pOnComplete, Action<float> pProgress = null) where TObject : Object
        {
            var operation = pReference.LoadAssetAsync();
            WaitLoadTask(operation, pOnComplete, pProgress);
            return operation;
        }

        public static async UniTask<TObject> LoadAssetAsync<TObject>(AssetReferenceT<TObject> pReference)
            where TObject : Object
        {
            if (pReference.OperationHandle.IsValid())
                return pReference.OperationHandle.Convert<TObject>().Result;

            var operation = pReference.LoadAssetAsync();
            var result = await operation;
            return result;
        }

        public static async UniTask<TObject> LoadAssetAsync<TObject>(AssetReference pReference) where TObject : Object
        {
            if (pReference.OperationHandle.IsValid())
                return pReference.OperationHandle.Convert<TObject>().Result;

            var operation = pReference.LoadAssetAsync<TObject>();
            var result = await operation.Task;
            return result;
        }

#endregion

#region Load Prefab

        public static async UniTask<TComponent> LoadPrefabAsync<TComponent>(AssetReference pReference)
            where TComponent : Component
        {
            if (pReference.OperationHandle.IsValid())
            {
                var result = pReference.OperationHandle.Convert<GameObject>().Result;
                if (result != null && result.TryGetComponent(out TComponent component))
                    return component;
            }
            else
            {
                var operation = pReference.LoadAssetAsync<GameObject>();
                var result = await operation;
                if (result != null && result.TryGetComponent(out TComponent component))
                    return component;
            }
            return null;
        }

        public static async UniTask<TComponent> LoadPrefabAsync<TComponent>(ComponentRef<TComponent> pReference)
            where TComponent : Component
        {
            if (pReference.OperationHandle.IsValid())
            {
                var result = pReference.OperationHandle.Convert<GameObject>().Result;
                if (result != null && result.TryGetComponent(out TComponent component))
                    return component;
            }
            else
            {
                var operation = pReference.LoadAssetAsync<GameObject>();
                var result = await operation;
                if (result != null && result.TryGetComponent(out TComponent component))
                    return component;
            }
            return null;
        }

        public static async UniTask<GameObject> LoadGameObjectAsync(AssetReferenceGameObject pReference)
        {
            if (pReference.OperationHandle.IsValid())
                return pReference.OperationHandle.Convert<GameObject>().Result;

            var operation = pReference.LoadAssetAsync();
            var result = await operation;
            return result;
        }

#endregion

#region Load Asset

        public static AsyncOperationHandle<TextAsset> LoadTextAssetAsync(string pAddress, Action<TextAsset> pOnComplete,
            Action<float> pProgress = null)
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

        public static AsyncOperationHandle<Sprite> LoadSpriteAsync(AssetReferenceSprite pReference,
            Action<Sprite> pOnComplete, Action<float> pProgress = null)
        {
            var operation = pReference.LoadAssetAsync();
            WaitLoadTask(operation, pOnComplete, pProgress);
            return operation;
        }

        public static async UniTask<Sprite> LoadSpriteAsync(AssetReferenceSprite pReference)
        {
            var operation = pReference.LoadAssetAsync();
            var result = await operation;
            return result;
        }

        public static AsyncOperationHandle<IList<Sprite>> LoadSpritesAsync(AssetReference pReference,
            Action<IList<Sprite>> pOnComplete, Action<float> pProgress = null)
        {
            var operation = pReference.LoadAssetAsync<IList<Sprite>>();
            WaitLoadTask(operation, pOnComplete, pProgress);
            return operation;
        }

        public static async UniTask<IList<Sprite>> LoadSpritesAsync(AssetReference pReference)
        {
            var operation = pReference.LoadAssetAsync<IList<Sprite>>();
            var result = await operation;
            return result;
        }

        public static AsyncOperationHandle<Texture> LoadTextureAsync(AssetReferenceTexture pReference,
            Action<Texture> pOnComplete, Action<float> pProgress = null)
        {
            var operation = pReference.LoadAssetAsync();
            WaitLoadTask(operation, pOnComplete, pProgress);
            return operation;
        }

        public static async UniTask<Texture> LoadTextureAsync(AssetReferenceTexture pReference)
        {
            var operation = pReference.LoadAssetAsync();
            var result = await operation;
            return result;
        }

        public static AsyncOperationHandle<Texture2D> LoadTexture2DAsync(AssetReferenceTexture2D pReference,
            Action<Texture2D> pOnComplete, Action<float> pProgress = null)
        {
            var operation = pReference.LoadAssetAsync();
            WaitLoadTask(operation, pOnComplete, pProgress);
            return operation;
        }

        public static async UniTask<Texture2D> LoadTexture2DAsync(AssetReferenceTexture2D pReference)
        {
            var operation = pReference.LoadAssetAsync();
            var result = await operation;
            return result;
        }

        public static AsyncOperationHandle<Texture3D> LoadTexture3DAsync(AssetReferenceTexture3D pReference,
            Action<Texture3D> pOnComplete, Action<float> pProgress = null)
        {
            var operation = pReference.LoadAssetAsync();
            WaitLoadTask(operation, pOnComplete, pProgress);
            return operation;
        }

        public static async UniTask<Texture3D> LoadTexture3DAsync(AssetReferenceTexture3D pReference)
        {
            var operation = pReference.LoadAssetAsync();
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
            WaitUtil.Start(new WaitUtil.ConditionEvent()
            {
                triggerCondition = () => operation.IsDone,
                onUpdate = () => { pProgress(operation.PercentComplete); },
                onTrigger = () =>
                {
                    pOnComplete?.Invoke();

                    if (operation.Status == AsyncOperationStatus.Failed)
                        Debug.LogError("Failed to load asset: " + operation.OperationException);
                },
            });
        }

        public static async UniTask WaitLoadTask<T>(AsyncOperationHandle<T> operation, Action<T> pOnComplete)
        {
            var result = await operation;
            pOnComplete?.Invoke(result);

            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogError("Failed to load asset: " + operation.OperationException);
        }

        public static void WaitLoadTask<T>(AsyncOperationHandle<T> operation, Action<T> pOnComplete, Action<float> pProgress)
        {
            if (pProgress == null)
            {
                WaitLoadTask(operation, pOnComplete);
                return;
            }

            WaitUtil.Start(new WaitUtil.ConditionEvent()
            {
                triggerCondition = () => operation.IsDone,
                onUpdate = () => { pProgress(operation.PercentComplete); },
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

        private static void WaitUnloadTask<T>(AsyncOperationHandle<T> operation, Action<bool> pOnComplete,
            Action<float> pProgress)
        {
            if (pProgress == null)
            {
                WaitUnloadTask(operation, pOnComplete);
                return;
            }
            WaitUtil.Start(new WaitUtil.ConditionEvent()
            {
                triggerCondition = () => operation.IsDone,
                onUpdate = () => { pProgress(operation.PercentComplete); },
                onTrigger = () =>
                {
                    pOnComplete?.Invoke(operation.Status == AsyncOperationStatus.Succeeded);

                    if (operation.Status == AsyncOperationStatus.Failed)
                        Debug.LogError("Failed to unload asset: " + operation.OperationException);
                },
            });
        }

#endregion

#region Extensions

        public static List<TComponent> GetPrefabsWrap<TComponent>(this List<WrapPrefab<TComponent>> wraps)
            where TComponent : Component
        {
            var list = new List<TComponent>();
            foreach (var wrap in wraps)
                list.Add(wrap.prefab);
            return list;
        }

        public static List<TObject> GetObjects<TObject>(this List<AsyncOperationHandle<TObject>> operations)
            where TObject : Object
        {
            var list = new List<TObject>();
            foreach (var operation in operations)
                list.Add(operation.Result);
            return list;
        }

#endregion
    }

    public class WrapPrefab<TComponent> where TComponent : Component
    {
        public TComponent prefab;
        public AsyncOperationHandle operation;

        public void Release()
        {
            Addressables.Release(operation);
        }
    }
#endif
}