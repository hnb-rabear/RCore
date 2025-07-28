using Cysharp.Threading.Tasks;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using Object = UnityEngine.Object;

namespace RCore
{
#if ADDRESSABLES
	/// <summary>
	/// A serializable reference to a Component of type <typeparamref name="TComponent"/> that can be loaded asynchronously using the Addressables system.
	/// </summary>
	/// <typeparam name="TComponent">The type of the component.</typeparam>
	[Serializable]
	public class ComponentRef<TComponent> : AssetReference where TComponent : Component
	{
		/// <summary>
		/// The instantiated instance of the component.
		/// </summary>
		public TComponent instance;

		/// <summary>
		/// Stores the string representation of the component's type (TComponent).
		/// This is primarily used for editor-side validation and debugging to identify the component type without loading the asset.
		/// </summary>
		public string type;

		/// <summary>
		/// The loaded asset of the component.
		/// </summary>
		public TComponent asset;

		/// <summary>
		/// Indicates whether the component is currently being loaded.
		/// </summary>
		public bool loading;

		private AsyncOperationHandle<GameObject> m_operation;

		/// <summary>
		/// Initializes a new instance of the <see cref="ComponentRef{TComponent}"/> class with the specified GUID.
		/// </summary>
		/// <param name="guid">The GUID of the addressable asset.</param>
		public ComponentRef(string guid) : base(guid) { }

		/// <summary>
		/// Validates the given Object to ensure it is a GameObject with the required component.
		/// </summary>
		/// <param name="obj">The object to validate.</param>
		/// <returns>True if the object is a valid GameObject with the component, otherwise false.</returns>
		public override bool ValidateAsset(Object obj)
		{
			var go = obj as GameObject;
			return go != null && go.GetComponent<TComponent>() != null;
		}

		/// <summary>
		/// Validates the asset at the specified path to ensure it is a GameObject with the required component.
		/// In the editor, it also caches the component's type name.
		/// </summary>
		/// <param name="path">The path to the asset.</param>
		/// <returns>True if the asset at the path is a valid GameObject with the component, otherwise false.</returns>
		public override bool ValidateAsset(string path)
		{
#if UNITY_EDITOR
			//this load can be expensive...
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (go == null) 
				return false;

			var component = go.GetComponent<TComponent>();
			if (component != null)
			{
				type = typeof(TComponent).FullName;
				return true;
			}
			return false;
#else
			return false;
#endif
		}

		/// <summary>
		/// Asynchronously instantiates the component and attaches it to the specified parent.
		/// </summary>
		/// <param name="parent">The parent Transform to attach the instantiated GameObject to.</param>
		/// <param name="pDefaultActive">Whether the instantiated GameObject should be active by default.</param>
		/// <returns>A UniTask that completes with the instantiated component.</returns>
		public async new UniTask<TComponent> InstantiateAsync(Transform parent, bool pDefaultActive = false)
		{
			m_operation = Addressables.InstantiateAsync(this, parent);
			loading = true;
			var go = await m_operation;
			go.SetActive(pDefaultActive);
			go.TryGetComponent(out instance);
			loading = false;
			Debug.Log($"Instantiate Asset Bundle {instance.name}");
			return instance;
		}

		/// <summary>
		/// Asynchronously loads the component asset.
		/// </summary>
		/// <returns>A UniTask that completes with the loaded component asset.</returns>
		public async UniTask<TComponent> LoadAssetAsync()
		{
			if (asset != null)
				return asset;
			var operation = IsValid() ? OperationHandle.Convert<GameObject>() : LoadAssetAsync<GameObject>();
			loading = true;
			await operation;
			loading = false;
			asset = operation.Result.GetComponent<TComponent>();
			Debug.Log($"Load Asset Bundle {asset.name}");
			return asset;
		}

		/// <summary>
		/// Instantiates the loaded component asset and attaches it to the specified parent.
		/// </summary>
		/// <param name="parent">The parent Transform to attach the instantiated GameObject to.</param>
		/// <param name="defaultActive">Whether the instantiated GameObject should be active by default.</param>
		public new void Instantiate(Transform parent, bool defaultActive = false)
		{
			if (instance != null) return;
			instance = Object.Instantiate(asset, parent);
			instance.gameObject.SetActive(defaultActive);
			instance.name = asset.name;
		}

		/// <summary>
		/// Coroutine to load the component asset.
		/// </summary>
		public IEnumerator IELoadAsset()
		{
			if (asset != null)
				yield break;
			var operation = IsValid() ? OperationHandle.Convert<GameObject>() : LoadAssetAsync<GameObject>();
			loading = true;
			yield return operation;
			loading = false;
			asset = operation.Result.GetComponent<TComponent>();
			Debug.Log($"Load Asset Bundle {asset.name}");
		}

		/// <summary>
		/// Unloads the instantiated component instance and/or the loaded asset.
		/// </summary>
		public void Unload()
		{
			try
			{
				if (instance != null)
				{
					string instanceName = instance.name;
					if (m_operation.IsValid() && Addressables.ReleaseInstance(m_operation))
						Debug.Log($"Unload asset bundle success {instanceName}");
				}
				if (asset != null)
				{
					string name = asset.name;
					asset = null;
					ReleaseAsset();
					Debug.Log($"Unload asset bundle success {name}");
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError(ex);
			}
		}
	}

	/// <summary>
	/// A specific implementation of <see cref="ComponentRef{TComponent}"/> for a <see cref="SpriteRenderer"/>.
	/// </summary>
	[Serializable]
	public class ComponentRef_SpriteRenderer : ComponentRef<SpriteRenderer>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ComponentRef_SpriteRenderer"/> class.
		/// </summary>
		/// <param name="guid">The GUID of the addressable asset.</param>
		public ComponentRef_SpriteRenderer(string guid) : base(guid) { }
	}
#endif
}