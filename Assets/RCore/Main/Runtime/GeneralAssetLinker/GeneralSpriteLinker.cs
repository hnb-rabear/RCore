using System;
using RCore.Config;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.Config
{
	[RequireComponent(typeof(Image))]
	public class GeneralSpriteLinker : MonoBehaviour
	{
		[SerializeField] private Image m_Image;
		[SerializeField] private string m_Key;
		[SerializeField] private bool m_AutoActive = true;

		public string Key
		{
			get => m_Key;
			set => m_Key = value;
		}
		public bool AutoActive
		{
			set => m_AutoActive = value;
			get => m_AutoActive;
		}

		public bool PreserveAspect
		{
			get => m_Image.preserveAspect;
			set => m_Image.preserveAspect = value;
		}

		private void CacheTarget()
		{
			if (m_Image == null)
				m_Image = GetComponent<Image>();
		}

		private void Awake()
		{
			if (m_AutoActive)
			{
				CacheTarget();
				var sprite = AssetCatalog.Instance != null ? AssetCatalog.Instance.GetSprite(m_Key) : null;
				if (sprite != null)
					m_Image.sprite = sprite;
			}
		}

#if UNITY_EDITOR
		private void OnDestroy()
		{
			if (Application.isPlaying || m_Image == null)
				return;
			m_Image.overrideSprite = null;
			UnityEditor.EditorUtility.SetDirty(m_Image);
		}

		private void Reset()
		{
			CacheTarget();
			if (m_Image == null || m_Image.sprite == null || AssetCatalog.Instance == null)
				return;

			var sprite = m_Image.sprite;
			if (AssetCatalog.Instance.GetSprite(sprite.name) == null)
				return;

			UnityEditor.Undo.RecordObject(this, "Initialize Sprite Linker");
			UnityEditor.Undo.RecordObject(m_Image, "Initialize Sprite Linker");
			m_Key = sprite.name;
			m_Image.sprite = null;
			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.EditorUtility.SetDirty(m_Image);
		}

		private void OnValidate()
		{
			Refresh();
		}

		public void Refresh()
		{
			if (Application.isPlaying)
				return;
			CacheTarget();
			if (m_Image == null)
				return;
			var sprite = AssetCatalog.Instance != null ? AssetCatalog.Instance.GetSprite(m_Key) : null;
			if (sprite != null)
				m_Image.overrideSprite = sprite;
			if (string.IsNullOrEmpty(m_Key))
				Debug.LogError($"{GetAssetLocation()} is missing key for General Linker!", this);
		}

		private string GetAssetLocation()
		{
			var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
			var hierarchyPath = GetHierarchyPath(transform);
			return string.IsNullOrEmpty(assetPath) ? hierarchyPath : $"{assetPath} ({hierarchyPath})";
		}

		private static string GetHierarchyPath(Transform pTransform)
		{
			var path = pTransform.name;
			while (pTransform.parent != null)
			{
				pTransform = pTransform.parent;
				path = pTransform.name + "/" + path;
			}
			return path;
		}
#endif
	}
}
