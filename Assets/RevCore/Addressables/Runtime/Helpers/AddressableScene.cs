using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace RevCore
{
	/// <summary>
	/// UniTask-first wrappers over <see cref="Addressables"/> scene loading and unloading operations.
	/// Methods throw <see cref="AddressableLoadException"/> on terminal failure and honour the supplied
	/// <see cref="CancellationToken"/>. Cancellation does not eagerly release in-flight handles; a
	/// <c>Completed</c> continuation releases each handle as soon as the underlying operation finishes.
	/// </summary>
	public static class AddressableScene
	{
		/// <summary>Asynchronously loads an Addressables scene.</summary>
		/// <param name="key">The Addressables key, address, label, or location query for the scene.</param>
		/// <param name="loadMode">The Unity scene load mode.</param>
		/// <param name="activateOnLoad">Whether to activate the scene immediately after loading.</param>
		/// <param name="priority">The underlying async operation priority.</param>
		/// <param name="progress">Optional progress reporter (0–1).</param>
		/// <param name="ct">Cancellation token. On cancellation the scene handle is released when safe.</param>
		/// <returns>The loaded <see cref="SceneInstance"/>.</returns>
		/// <exception cref="AddressableLoadException">Thrown when the scene load operation fails.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public static async UniTask<SceneInstance> LoadSceneAsync(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100, IProgress<float> progress = null, CancellationToken ct = default)
		{
			ct.ThrowIfCancellationRequested();
			AsyncOperationHandle<SceneInstance> handle;
			try
			{
				handle = Addressables.LoadSceneAsync(key, loadMode, activateOnLoad, priority);
			}
			catch (Exception ex)
			{
				throw new AddressableLoadException(key, AsyncOperationStatus.Failed, ex);
			}

			try
			{
				return await handle.ToUniTask(progress, cancellationToken: ct);
			}
			catch (OperationCanceledException)
			{
				ReleaseOnComplete(handle);
				throw;
			}
			catch (Exception ex)
			{
				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
				if (handle.IsValid()) Addressables.Release(handle);
				throw new AddressableLoadException(key, status, ex);
			}
		}

		/// <summary>Asynchronously unloads an Addressables scene.</summary>
		/// <param name="scene">The loaded scene instance to unload.</param>
		/// <param name="autoReleaseHandle">Whether Addressables should automatically release the unload operation handle.</param>
		/// <param name="ct">Cancellation token. On cancellation caller-owned handles are released when safe.</param>
		/// <returns>The unloaded <see cref="SceneInstance"/>.</returns>
		/// <exception cref="AddressableLoadException">Thrown when the scene unload operation fails.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public static async UniTask<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true, CancellationToken ct = default)
		{
			ct.ThrowIfCancellationRequested();
			AsyncOperationHandle<SceneInstance> handle;
			try
			{
				handle = Addressables.UnloadSceneAsync(scene, autoReleaseHandle);
			}
			catch (Exception ex)
			{
				throw new AddressableLoadException(scene.Scene.name, AsyncOperationStatus.Failed, ex);
			}

			try
			{
				var result = await handle.ToUniTask(cancellationToken: ct);
				if (!autoReleaseHandle && handle.IsValid()) Addressables.Release(handle);
				return result;
			}
			catch (OperationCanceledException)
			{
				if (!autoReleaseHandle) ReleaseOnComplete(handle);
				throw;
			}
			catch (Exception ex)
			{
				var status = handle.IsValid() ? handle.Status : AsyncOperationStatus.Failed;
				if (!autoReleaseHandle && handle.IsValid()) Addressables.Release(handle);
				throw new AddressableLoadException(scene.Scene.name, status, ex);
			}
		}

		private static void ReleaseOnComplete(AsyncOperationHandle<SceneInstance> handle)
		{
			if (!handle.IsValid()) return;
			if (handle.IsDone)
			{
				Addressables.Release(handle);
				return;
			}
			handle.Completed += op => Addressables.Release(op);
		}
	}
}
