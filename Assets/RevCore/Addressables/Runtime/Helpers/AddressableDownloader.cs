using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RevCore
{
	/// <summary>
	/// UniTask-first wrappers over <see cref="Addressables"/> dependency download and cache operations.
	/// All methods throw <see cref="AddressableLoadException"/> on terminal failure and honour the supplied
	/// <see cref="CancellationToken"/>. Cancellation does not eagerly release in-flight handles; a
	/// <c>Completed</c> continuation releases each handle as soon as the underlying operation finishes.
	/// </summary>
	public static class AddressableDownloader
	{
		/// <summary>Asynchronously gets the dependency download size for an Addressables key.</summary>
		/// <param name="key">The Addressables key, label, or location query.</param>
		/// <param name="ct">Cancellation token. On cancellation the handle is auto-released when it completes.</param>
		/// <returns>The number of bytes that must be downloaded for dependencies matching <paramref name="key"/>.</returns>
		/// <exception cref="AddressableLoadException">Thrown when the size query operation fails.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public static async UniTask<long> GetDownloadSizeAsync(object key, CancellationToken ct = default)
		{
			ct.ThrowIfCancellationRequested();
			AsyncOperationHandle<long> handle;
			try
			{
				handle = Addressables.GetDownloadSizeAsync(key);
			}
			catch (Exception ex)
			{
				throw new AddressableLoadException(key, AsyncOperationStatus.Failed, ex);
			}

			try
			{
				var size = await handle.ToUniTask(cancellationToken: ct);
				if (handle.IsValid()) Addressables.Release(handle);
				return size;
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

		/// <summary>Asynchronously downloads dependencies for an Addressables key.</summary>
		/// <param name="key">The Addressables key, label, or location query.</param>
		/// <param name="progress">Optional progress reporter (0–1).</param>
		/// <param name="ct">Cancellation token. On cancellation the handle is auto-released when it completes.</param>
		/// <returns>A task that completes when dependencies matching <paramref name="key"/> have been downloaded.</returns>
		/// <exception cref="AddressableLoadException">Thrown when the download operation fails.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public static async UniTask DownloadDependenciesAsync(object key, IProgress<float> progress = null, CancellationToken ct = default)
		{
			ct.ThrowIfCancellationRequested();
			AsyncOperationHandle handle;
			try
			{
				handle = Addressables.DownloadDependenciesAsync(key, autoReleaseHandle: false);
			}
			catch (Exception ex)
			{
				throw new AddressableLoadException(key, AsyncOperationStatus.Failed, ex);
			}

			try
			{
				await handle.ToUniTask(progress, cancellationToken: ct);
				if (handle.IsValid()) Addressables.Release(handle);
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

		/// <summary>Asynchronously clears cached dependencies for an Addressables key.</summary>
		/// <param name="key">The Addressables key, label, or location query.</param>
		/// <param name="ct">Cancellation token. On cancellation the handle is auto-released when it completes.</param>
		/// <returns><c>true</c> when cached dependencies were cleared; otherwise, <c>false</c>.</returns>
		/// <exception cref="AddressableLoadException">Thrown when the cache clear operation fails.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public static async UniTask<bool> ClearDependencyCacheAsync(object key, CancellationToken ct = default)
		{
			ct.ThrowIfCancellationRequested();
			AsyncOperationHandle<bool> handle;
			try
			{
				handle = Addressables.ClearDependencyCacheAsync(key, autoReleaseHandle: false);
			}
			catch (Exception ex)
			{
				throw new AddressableLoadException(key, AsyncOperationStatus.Failed, ex);
			}

			try
			{
				var cleared = await handle.ToUniTask(cancellationToken: ct);
				if (handle.IsValid()) Addressables.Release(handle);
				return cleared;
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

		private static void ReleaseOnComplete<T>(AsyncOperationHandle<T> handle)
		{
			if (!handle.IsValid()) return;
			if (handle.IsDone)
			{
				Addressables.Release(handle);
				return;
			}
			handle.Completed += op => Addressables.Release(op);
		}

		private static void ReleaseOnComplete(AsyncOperationHandle handle)
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
