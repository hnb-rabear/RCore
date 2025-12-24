using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using Object = UnityEngine.Object;

namespace RCore
{
#if ADDRESSABLES
	/// <summary>
	/// A generic wrapper for an Addressable asset reference, providing async loading capabilities.
	/// </summary>
	/// <typeparam name="M">The type of the asset, which must be a subclass of <see cref="UnityEngine.Object"/>.</typeparam>
	[Serializable]
	public class AssetBundleRef<M> where M : Object
	{
		/// <summary>
		/// The Addressable asset reference.
		/// </summary>
		public AssetReferenceT<M> reference;
		private AsyncOperationHandle<M> m_operation;
		
		/// <summary>
		/// The loaded asset.
		/// </summary>
		public M asset { get; private set; }

		public bool IsLoading => m_operation.IsValid() && !m_operation.IsDone;
		public bool IsLoaded => asset != null;


		/// <summary>
		/// Asynchronously loads the asset.
		/// </summary>
		/// <remarks>
		/// This function should be awaited in an async method, as it does not work correctly within a Coroutine.
		/// </remarks>
		/// <returns>A <see cref="UniTask{M}"/> that completes with the loaded asset.</returns>
		public async UniTask<M> LoadAsync(IProgress<float> progress = null, CancellationToken cancellationToken = default)
		{
			if (asset != null)
				return asset;

			if (m_operation.IsValid() && m_operation.Status == AsyncOperationStatus.Failed)
				Addressables.Release(m_operation);

			if (!m_operation.IsValid())
				m_operation = Addressables.LoadAssetAsync<M>(reference);

			await m_operation.ToUniTask(progress: progress, cancellationToken: cancellationToken);
			asset = m_operation.Result;
			return asset;
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
				m_operation = Addressables.LoadAssetAsync<M>(reference);

			yield return m_operation;
			asset = m_operation.Result;
		}

		/// <summary>
		/// Unloads the asset, releasing the operation handle if it's valid.
		/// </summary>
		public void Unload()
		{
			if (m_operation.IsValid())
				Addressables.Release(m_operation);
			asset = null;
		}
	}

	/// <summary>
	/// An <see cref="AssetBundleRef{M}"/> that uses an enum as a key.
	/// </summary>
	/// <typeparam name="T">The enum type for the key.</typeparam>
	/// <typeparam name="M">The type of the asset.</typeparam>
	[Serializable]
	public class AssetBundleWithEnumKey<T, M> : AssetBundleRef<M> where T : Enum where M : Object
	{
		/// <summary>
		/// The enum key associated with this asset.
		/// </summary>
		public T key;
	}

	/// <summary>
	/// An <see cref="AssetBundleRef{M}"/> that uses two enums as keys.
	/// </summary>
	/// <typeparam name="T1">The first enum type for the key.</typeparam>
	/// <typeparam name="T2">The second enum type for the key.</typeparam>
	/// <typeparam name="M">The type of the asset.</typeparam>
	[Serializable]
	public class AssetBundleWith2EnumKeys<T1, T2, M> : AssetBundleRef<M>
		where T1 : Enum
		where T2 : Enum
		where M : Object
	{
		/// <summary>
		/// The first enum key.
		/// </summary>
		public T1 key1;
		
		/// <summary>
		/// The second enum key.
		/// </summary>
		public T2 key2;
	}

	/// <summary>
	/// An <see cref="AssetBundleRef{M}"/> that uses an integer as a key.
	/// </summary>
	/// <typeparam name="M">The type of the asset.</typeparam>
	[Serializable]
	public class AssetBundleWithIntKey<M> : AssetBundleRef<M> where M : Object
	{
		/// <summary>
		/// The integer key.
		/// </summary>
		public int key;
	}
#endif
}