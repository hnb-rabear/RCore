using UnityEngine;
using UnityEngine.UI;

namespace RCore.Config
{
	[RequireComponent(typeof(RawImage))]
	public class GeneralTextureLinker : MonoBehaviour
	{
		[SerializeField] private RawImage m_RawImage;
		[SerializeField] private string m_Key;
		[SerializeField] private bool m_AutoActive = true;

		public string Key
		{
			get => m_Key;
			set => m_Key = value;
		}

		private void Awake()
		{
			CacheTarget();
			if (m_AutoActive || !Application.isPlaying)
				Refresh();
		}

		private void OnEnable()
		{
			CacheTarget();
			if (m_AutoActive || !Application.isPlaying)
				Refresh();
		}

		private void OnDestroy()
		{
			if (!Application.isPlaying)
				return;
			if (m_RawImage != null)
				m_RawImage.texture = null;
		}

		private void CacheTarget()
		{
			if (m_RawImage == null)
				m_RawImage = GetComponent<RawImage>();
		}

		public void Refresh()
		{
			if (m_RawImage == null || !Application.isPlaying)
				return;
			var texture = AssetCatalog.Instance != null ? AssetCatalog.Instance.GetTexture(m_Key) : null;
			if (texture != null)
				m_RawImage.texture = texture;
		}

		// Deliberately no OnValidate/edit-mode assignment: RawImage.texture has no
		// non-serialized override equivalent, so assigning it outside Play mode
		// would bake a hard reference into the prefab.

#if UNITY_EDITOR
		private void Reset()
		{
			CacheTarget();
			var texture = m_RawImage != null ? m_RawImage.texture as Texture2D : null;
			if (texture == null || AssetCatalog.Instance == null)
				return;

			if (AssetCatalog.Instance.GetTexture(texture.name) == null)
				return;

			UnityEditor.Undo.RecordObject(this, "Initialize Texture Linker");
			UnityEditor.Undo.RecordObject(m_RawImage, "Initialize Texture Linker");
			m_Key = texture.name;
			m_RawImage.texture = null;
			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.EditorUtility.SetDirty(m_RawImage);
		}

		private void OnValidate()
		{
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
