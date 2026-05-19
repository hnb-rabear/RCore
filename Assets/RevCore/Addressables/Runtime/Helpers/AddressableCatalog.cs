using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RevCore
{
	/// <summary>
	/// UniTask-first wrappers over <see cref="Addressables"/> catalog check and update operations.
	/// </summary>
	public static class AddressableCatalog
	{
		/// <summary>Asynchronously checks for Addressables catalog updates.</summary>
		/// <param name="autoReleaseHandle">Whether Addressables should automatically release the operation handle.</param>
		/// <param name="ct">Cancellation token used before starting and while awaiting the operation.</param>
		/// <returns>Catalog identifiers that have updates available.</returns>
		/// <exception cref="AddressableLoadException">Thrown when checking for catalog updates fails.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public static async UniTask<List<string>> CheckForCatalogUpdatesAsync(bool autoReleaseHandle = true, CancellationToken ct = default)
		{
			const string label = "CheckForCatalogUpdates";
			ct.ThrowIfCancellationRequested();
			AsyncOperationHandle<List<string>> handle;
			try
			{
				handle = Addressables.CheckForCatalogUpdates(autoReleaseHandle);
			}
			catch (Exception ex)
			{
				throw new AddressableLoadException(label, AsyncOperationStatus.Failed, ex);
			}

			try
			{
				var updates = await handle.ToUniTask(cancellationToken: ct);
				if (!autoReleaseHandle && handle.IsValid()) Addressables.Release(handle);
				return updates;
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
				throw new AddressableLoadException(label, status, ex);
			}
		}

		/// <summary>Asynchronously updates Addressables catalogs.</summary>
		/// <param name="catalogIds">Optional catalog identifiers to update; when <c>null</c>, Addressables updates all pending catalogs.</param>
		/// <param name="autoReleaseHandle">Whether Addressables should automatically release the operation handle.</param>
		/// <param name="ct">Cancellation token used before starting and while awaiting the operation.</param>
		/// <returns>Resource locators produced by the updated catalogs.</returns>
		/// <exception cref="AddressableLoadException">Thrown when updating catalogs fails.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public static async UniTask<List<IResourceLocator>> UpdateCatalogsAsync(IEnumerable<string> catalogIds = null, bool autoReleaseHandle = true, CancellationToken ct = default)
		{
			const string label = "UpdateCatalogs";
			ct.ThrowIfCancellationRequested();
			AsyncOperationHandle<List<IResourceLocator>> handle;
			try
			{
				handle = Addressables.UpdateCatalogs(catalogIds, autoReleaseHandle);
			}
			catch (Exception ex)
			{
				throw new AddressableLoadException(label, AsyncOperationStatus.Failed, ex);
			}

			try
			{
				var locators = await handle.ToUniTask(cancellationToken: ct);
				if (!autoReleaseHandle && handle.IsValid()) Addressables.Release(handle);
				return locators;
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
				throw new AddressableLoadException(label, status, ex);
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
	}
}
