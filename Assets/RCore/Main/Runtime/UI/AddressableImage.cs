using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

// AddressableImageEditor (rcore.editor) writes the sprite reference via the internal setter below.
[assembly: InternalsVisibleTo("rcore.editor")]

namespace RCore.UI
{
	[RequireComponent(typeof(Image))]
	public class AddressableImage : MonoBehaviour
	{
		[SerializeField] private Image m_Image;
		[SerializeField] private AssetReferenceSprite m_SpriteReference = new AssetReferenceSprite(string.Empty);

		private AsyncOperationHandle<Sprite> m_Handle;
		private bool m_Loading;

		public AssetReferenceSprite SpriteReference => m_SpriteReference;

		/// <summary>
		/// Proxy for <see cref="Image.preserveAspect"/>. No local state — the Image component is the single source of truth.
		/// </summary>
		public bool PreserveAspect
		{
			get
			{
				CacheImage();
				return m_Image != null && m_Image.preserveAspect;
			}
			set
			{
				CacheImage();
				if (m_Image != null)
					m_Image.preserveAspect = value;
			}
		}

		public bool HasReference()
		{
			return m_SpriteReference != null && !string.IsNullOrEmpty(m_SpriteReference.AssetGUID);
		}

#if UNITY_EDITOR
		internal void SetSpriteReference(AssetReferenceSprite spriteReference)
		{
			m_SpriteReference = spriteReference;
		}
#endif

		private void Awake()
		{
			CacheImage();
		}

		private void OnEnable()
		{
			CacheImage();

			if (!HasReference())
			{
				Debug.LogWarning($"[AddressableImage] Missing sprite reference on {name}.", this);
				return;
			}

			if (m_Handle.IsValid() && m_Handle.IsDone)
			{
				if (m_Image != null)
					m_Image.overrideSprite = m_Handle.Result;
				return;
			}

			if (!m_Handle.IsValid() && !m_Loading)
				LoadAsync().Forget();
		}

		private void OnDestroy()
		{
			if (m_Image != null)
				m_Image.overrideSprite = null;

			if (m_Handle.IsValid())
			{
				Addressables.Release(m_Handle);
				m_Handle = default;
			}

			m_Handle = default;
			m_Loading = false;
		}

		private void CacheImage()
		{
			if (m_Image == null)
				m_Image = GetComponent<Image>();
		}

		private async UniTaskVoid LoadAsync()
		{
			if (m_Loading || !HasReference())
				return;

			m_Loading = true;
			try
			{
				m_Handle = m_SpriteReference.IsValid()
					? m_SpriteReference.OperationHandle.Convert<Sprite>()
					: m_SpriteReference.LoadAssetAsync();

				var sprite = await m_Handle;
				if (this == null || m_Image == null)
					return;

				if (m_Handle.Status == AsyncOperationStatus.Succeeded)
					m_Image.overrideSprite = sprite;
				else
					Debug.LogError($"[AddressableImage] Failed to load sprite on {name}: {m_Handle.OperationException}", this);
			}
			catch (Exception ex)
			{
				if (this != null)
					Debug.LogError($"[AddressableImage] Failed to load sprite on {name}: {ex.Message}", this);
			}
			finally
			{
				if (this != null)
					m_Loading = false;
			}
		}

#if UNITY_EDITOR
        // Sprite capture/preview lives in RCore.Editor.UI.AddressableImageEditor (rcore.editor assembly),
        // driven from the custom inspector. The runtime assembly must not reference UnityEditor.AddressableAssets.
        private void OnValidate()
        {
            m_Image = GetComponent<Image>();
        }
#endif
	}
}
