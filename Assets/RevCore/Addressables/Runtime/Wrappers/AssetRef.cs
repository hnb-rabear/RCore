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
	/// Serializable Addressables asset reference wrapper that caches a loaded asset and owns its Addressables handle.
	/// </summary>
	/// <remarks>
	/// Use this type on serialized fields when a component needs a strongly typed <see cref="AssetReferenceT{TObject}"/>
	/// plus runtime state for loading, loaded asset access, and release. A newly constructed wrapper has no asset,
	/// is not loading, and is not loaded. <see cref="LoadAsync"/> is idempotent while an asset remains cached,
	/// and <see cref="Release"/> releases the owned handle before clearing cached state.
	/// </remarks>
	/// <typeparam name="T">The Unity asset type referenced by this wrapper.</typeparam>
	[Serializable]
	public class AssetRef<T> where T : Object
	{
		[SerializeField] private AssetReferenceT<T> m_reference;
		private AsyncOperationHandle<T> m_handle;
		private T m_asset;

		/// <summary>
		/// Gets the serialized Addressables reference used by this wrapper.
		/// </summary>
		public AssetReferenceT<T> Reference => m_reference;

		/// <summary>
		/// Gets the cached loaded asset, or <c>null</c> when no load has completed.
		/// </summary>
		public T Asset => m_asset;

		/// <summary>
		/// Gets whether this wrapper currently owns a valid in-flight Addressables handle.
		/// </summary>
		public bool IsLoading => m_asset == null && m_handle.IsValid();

		/// <summary>
		/// Gets whether this wrapper has a cached loaded asset.
		/// </summary>
		public bool IsLoaded => m_asset != null;

		/// <summary>
		/// Loads the referenced asset asynchronously and caches it for later access.
		/// </summary>
		/// <param name="progress">Optional progress reporter receiving values from 0 to 1.</param>
		/// <param name="ct">Cancellation token used to cancel awaiting the load operation.</param>
		/// <returns>The loaded asset instance.</returns>
		/// <exception cref="AddressableLoadException">
		/// Thrown when the serialized reference is null, when starting the Addressables load throws synchronously,
		/// or when the Addressables operation fails.
		/// </exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public async UniTask<T> LoadAsync(IProgress<float> progress = null, CancellationToken ct = default)
		{
			if (m_asset != null) return m_asset;
			if (m_reference == null)
			{
				throw new AddressableLoadException(
					"<null AssetReference>",
					AsyncOperationStatus.Failed,
					new InvalidOperationException("AssetRef cannot load because its AssetReference is null."));
			}

			ct.ThrowIfCancellationRequested();

			try
			{
				if (!m_handle.IsValid()) m_handle = m_reference.LoadAssetAsync<T>();
			}
			catch (Exception ex)
			{
				throw new AddressableLoadException(m_reference.RuntimeKey, AsyncOperationStatus.Failed, ex);
			}

			var capturedHandle = m_handle;
			try
			{
				m_asset = await capturedHandle.ToUniTask(progress, cancellationToken: ct);
				return m_asset;
			}
			catch (OperationCanceledException)
			{
				if (m_handle.Equals(capturedHandle)) m_handle = default;
				ReleaseOnComplete(capturedHandle);
				throw;
			}
			catch (Exception ex)
			{
				var status = capturedHandle.IsValid() ? capturedHandle.Status : AsyncOperationStatus.Failed;
				if (capturedHandle.IsValid()) Addressables.Release(capturedHandle);
				if (m_handle.Equals(capturedHandle)) m_handle = default;
				throw new AddressableLoadException(m_reference.RuntimeKey, status, ex);
			}
		}

		/// <summary>
		/// Releases the owned Addressables handle, if valid, and clears cached handle and asset state.
		/// </summary>
		public void Release()
		{
			if (m_handle.IsValid()) Addressables.Release(m_handle);
			m_handle = default;
			m_asset = null;
		}

		private static void ReleaseOnComplete(AsyncOperationHandle<T> handle)
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
