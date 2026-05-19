using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RevCore
{
	/// <summary>
	/// Serializable Addressables reference that resolves to a component on a loaded prefab.
	/// </summary>
	/// <remarks>
	/// Use this reference when serialized fields should point at a prefab but runtime code needs a specific
	/// <see cref="Component"/> from that prefab. The reference owns its Addressables handle until <see cref="Release"/>.
	/// <see cref="LoadAsync"/> is idempotent while a component remains cached, and <see cref="Release"/> releases
	/// the owned handle before clearing cached state.
	/// </remarks>
	/// <typeparam name="TComponent">The component type expected on the referenced prefab.</typeparam>
	[Serializable]
	public class ComponentRef<TComponent> : AssetReference where TComponent : Component
	{
		[SerializeField] private string m_type;
		private AsyncOperationHandle<GameObject> m_handle;
		private TComponent m_asset;

		/// <summary>
		/// Gets the fully qualified component type name cached during asset validation for editor diagnostics.
		/// </summary>
		public string TypeName => m_type;

		/// <summary>
		/// Gets the cached loaded component, or <c>null</c> when no load has completed.
		/// </summary>
		public TComponent Asset => m_asset;

		/// <summary>
		/// Gets whether this reference currently owns a valid in-flight Addressables handle.
		/// </summary>
		public bool IsLoading => m_asset == null && m_handle.IsValid();

		/// <summary>
		/// Gets whether this reference has a cached loaded component.
		/// </summary>
		public bool IsLoaded => m_asset != null;

		/// <summary>
		/// Initializes a new instance of the <see cref="ComponentRef{TComponent}"/> class.
		/// </summary>
		/// <param name="guid">The Addressables asset GUID for the referenced prefab.</param>
		public ComponentRef(string guid) : base(guid)
		{
		}

		/// <summary>
		/// Validates that the supplied object is a prefab root containing <typeparamref name="TComponent"/>.
		/// </summary>
		/// <param name="obj">The object to validate.</param>
		/// <returns><c>true</c> when the object is a <see cref="GameObject"/> with the required component; otherwise <c>false</c>.</returns>
		public override bool ValidateAsset(Object obj)
		{
			if (obj is not GameObject go) return false;
			var component = go.GetComponent<TComponent>();
			if (component == null) return false;

			m_type = component.GetType().FullName;
			return true;
		}

		/// <summary>
		/// Validates that the asset at the supplied editor path is a prefab root containing <typeparamref name="TComponent"/>.
		/// </summary>
		/// <param name="path">The editor asset path to validate.</param>
		/// <returns><c>true</c> when the path resolves to a <see cref="GameObject"/> with the required component; otherwise <c>false</c>.</returns>
		public override bool ValidateAsset(string path)
		{
#if UNITY_EDITOR
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			return ValidateAsset((Object)go);
#else
			return false;
#endif
		}

		/// <summary>
		/// Loads the referenced prefab asynchronously and caches the required component from it.
		/// </summary>
		/// <param name="progress">Optional progress reporter receiving values from 0 to 1.</param>
		/// <param name="ct">Cancellation token used to cancel awaiting the load operation.</param>
		/// <returns>The loaded component from the referenced prefab.</returns>
		/// <exception cref="AddressableLoadException">
		/// Thrown when starting the Addressables load throws synchronously, the Addressables operation fails,
		/// or the loaded prefab does not contain <typeparamref name="TComponent"/>.
		/// </exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public async UniTask<TComponent> LoadAsync(IProgress<float> progress = null, CancellationToken ct = default)
		{
			if (m_asset != null) return m_asset;

			ct.ThrowIfCancellationRequested();

			try
			{
				if (!m_handle.IsValid()) m_handle = LoadAssetAsync<GameObject>();
			}
			catch (Exception ex)
			{
				throw new AddressableLoadException(RuntimeKey, AsyncOperationStatus.Failed, ex);
			}

			var capturedHandle = m_handle;
			try
			{
				var prefab = await capturedHandle.ToUniTask(progress, cancellationToken: ct);
				m_asset = prefab.GetComponent<TComponent>();
				if (m_asset == null)
				{
					throw new InvalidOperationException(
						$"Prefab loaded from '{RuntimeKey}' does not contain component '{typeof(TComponent).FullName}'.");
				}

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
				m_asset = null;
				throw new AddressableLoadException(RuntimeKey, status, ex);
			}
		}

		/// <summary>
		/// Releases the owned Addressables handle, if valid, and clears cached handle and component state.
		/// </summary>
		public void Release()
		{
			if (m_handle.IsValid()) Addressables.Release(m_handle);
			m_handle = default;
			m_asset = null;
		}

		/// <summary>
		/// Determines whether the configured component type can be assigned from the supplied type.
		/// </summary>
		/// <param name="checkType">The type to test against <typeparamref name="TComponent"/>.</param>
		/// <returns><c>true</c> when <paramref name="checkType"/> is assignable to <typeparamref name="TComponent"/>; otherwise <c>false</c>.</returns>
		public bool HasType(Type checkType)
		{
			return typeof(TComponent).IsAssignableFrom(checkType);
		}

		private static void ReleaseOnComplete(AsyncOperationHandle<GameObject> handle)
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