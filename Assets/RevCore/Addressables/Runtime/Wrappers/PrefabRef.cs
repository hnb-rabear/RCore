using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace RevCore
{
	/// <summary>
	/// Serializable Addressables prefab reference that loads a component, remembers a default parent, and caches one instance.
	/// </summary>
	/// <remarks>
	/// Use this wrapper when a prefab field should behave like a <see cref="ComponentRef{TComponent}"/> while also carrying
	/// a default parent transform for instantiation. <see cref="InstantiateAsync"/> is idempotent while the cached instance
	/// exists, and <see cref="Release"/> destroys the cached instance before releasing the wrapped component reference.
	/// </remarks>
	/// <typeparam name="TComponent">The component type expected on the referenced prefab and returned from instantiation.</typeparam>
	[Serializable]
	public class PrefabRef<TComponent> where TComponent : Component
	{
		[SerializeField] private ComponentRef<TComponent> m_reference;
		[SerializeField] private Transform m_parent;
		private TComponent m_instance;

		/// <summary>
		/// Gets the wrapped component reference used to load the prefab asset.
		/// </summary>
		public ComponentRef<TComponent> Reference => m_reference;

		/// <summary>
		/// Gets or sets the default parent transform used when <see cref="InstantiateAsync"/> is called without a parent.
		/// </summary>
		public Transform DefaultParent
		{
			get => m_parent;
			set => m_parent = value;
		}

		/// <summary>
		/// Gets the cached loaded prefab component from the wrapped reference, or <c>null</c> when no asset is loaded.
		/// </summary>
		public TComponent Asset => m_reference != null ? m_reference.Asset : null;

		/// <summary>
		/// Gets the cached instantiated component, or <c>null</c> when no instance exists.
		/// </summary>
		public TComponent Instance => m_instance;

		/// <summary>
		/// Gets whether this reference currently has a cached instantiated component.
		/// </summary>
		public bool IsInstantiated => m_instance != null;

		/// <summary>
		/// Initializes a new instance of the <see cref="PrefabRef{TComponent}"/> class.
		/// </summary>
		public PrefabRef()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PrefabRef{TComponent}"/> class using an Addressables asset GUID.
		/// </summary>
		/// <param name="guid">The Addressables asset GUID for the referenced prefab.</param>
		public PrefabRef(string guid)
		{
			m_reference = new ComponentRef<TComponent>(guid);
		}

		/// <summary>
		/// Loads the referenced prefab asynchronously and returns the required component from it.
		/// </summary>
		/// <param name="progress">Optional progress reporter receiving values from 0 to 1.</param>
		/// <param name="ct">Cancellation token used to cancel awaiting the load operation.</param>
		/// <returns>The loaded component from the referenced prefab.</returns>
		/// <exception cref="AddressableLoadException">Thrown when the wrapped component reference is missing or the load fails.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public UniTask<TComponent> LoadAsync(IProgress<float> progress = null, CancellationToken ct = default)
		{
			if (m_reference == null)
			{
				throw new AddressableLoadException(
					"<null ComponentRef>",
					AsyncOperationStatus.Failed,
					new InvalidOperationException("PrefabRef.Reference is null"));
			}

			return m_reference.LoadAsync(progress, ct);
		}

		/// <summary>
		/// Instantiates the loaded prefab component under the supplied parent or the default parent and caches the instance.
		/// </summary>
		/// <param name="parent">Optional parent transform overriding <see cref="DefaultParent"/> for this call.</param>
		/// <param name="defaultActive">Whether the instantiated GameObject should be active after creation.</param>
		/// <param name="ct">Cancellation token used to cancel awaiting the load operation.</param>
		/// <returns>The cached or newly instantiated component.</returns>
		/// <exception cref="AddressableLoadException">Thrown when the wrapped component reference is missing or the load fails.</exception>
		/// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
		public async UniTask<TComponent> InstantiateAsync(Transform parent = null, bool defaultActive = true, CancellationToken ct = default)
		{
			if (m_instance != null) return m_instance;

			var asset = await LoadAsync(null, ct);
			var p = parent ?? m_parent;
			m_instance = Object.Instantiate(asset, p);
			m_instance.name = asset.name;
			m_instance.gameObject.SetActive(defaultActive);
			return m_instance;
		}

		/// <summary>
		/// Destroys the cached instantiated GameObject, if present, and releases the wrapped component reference.
		/// </summary>
		public void Release()
		{
			if (m_instance != null) Object.Destroy(m_instance.gameObject);
			m_instance = null;
			m_reference?.Release();
		}
	}
}
