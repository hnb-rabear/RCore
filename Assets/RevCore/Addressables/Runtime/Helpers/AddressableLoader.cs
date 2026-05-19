using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace RevCore
{
	/// <summary>
	/// UniTask-first wrappers over <see cref="Addressables"/> for loading single assets, batches, and instances.
	/// All methods throw <see cref="AddressableLoadException"/> on terminal failure and honour the supplied
	/// <see cref="CancellationToken"/>. Cancellation does not eagerly release in-flight handles; a
	/// <c>Completed</c> continuation releases each handle as soon as the underlying operation finishes.
	/// </summary>
	public static class AddressableLoader
	{
		/// <summary>Asynchronously loads an asset of type <typeparamref name="T"/> by string address.</summary>
		/// <typeparam name="T">The asset type to load.</typeparam>
		/// <param name="address">The Addressables key/address.</param>
		/// <param name="progress">Optional progress reporter (0–1).</param>
		/// <param name="ct">Cancellation token. On cancellation the handle is auto-released via a <c>Completed</c> continuation.</param>
		/// <returns>The loaded asset.</returns>
		/// <exception cref="AddressableLoadException">Thrown when the operation terminates in <see cref="AsyncOperationStatus.Failed"/>.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public static async UniTask<T> LoadAssetAsync<T>(string address, IProgress<float> progress = null, CancellationToken ct = default) where T : Object
		{
			ct.ThrowIfCancellationRequested();
			var handle = Addressables.LoadAssetAsync<T>(address);
			return await AwaitOrThrow(handle, address, progress, ct);
		}

		internal static async UniTask<T> AwaitOrThrow<T>(AsyncOperationHandle<T> handle, object key, IProgress<float> progress, CancellationToken ct)
		{
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