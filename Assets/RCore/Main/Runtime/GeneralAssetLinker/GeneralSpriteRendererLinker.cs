using UnityEngine;

namespace RCore.Config
{
	[RequireComponent(typeof(SpriteRenderer))]
	public class GeneralSpriteRendererLinker : MonoBehaviour
	{
		[SerializeField] private SpriteRenderer m_SpriteRenderer;
		[SerializeField] private string m_Key;
		[SerializeField] private bool m_AutoActive = true;

		public string Key
		{
			get => m_Key;
			set => m_Key = value;
		}

		public bool AutoActive
		{
			get => m_AutoActive;
			set => m_AutoActive = value;
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
			if (m_SpriteRenderer != null)
				m_SpriteRenderer.sprite = null;
		}

		private void CacheTarget()
		{
			if (m_SpriteRenderer == null)
				m_SpriteRenderer = GetComponent<SpriteRenderer>();
		}

		public void Refresh()
		{
			if (m_SpriteRenderer == null || !Application.isPlaying)
				return;
			var sprite = AssetCatalog.Instance != null ? AssetCatalog.Instance.GetSprite(m_Key) : null;
			if (sprite != null)
				m_SpriteRenderer.sprite = sprite;
		}

		// Deliberately no OnValidate/edit-mode assignment: SpriteRenderer.sprite has no
		// non-serialized override equivalent, so assigning it outside Play mode
		// would bake a hard reference into the prefab.

#if UNITY_EDITOR
		private void Reset()
		{
			CacheTarget();
			if (m_SpriteRenderer == null || m_SpriteRenderer.sprite == null || AssetCatalog.Instance == null)
				return;

			var sprite = m_SpriteRenderer.sprite;
			if (AssetCatalog.Instance.GetSprite(sprite.name) == null)
				return;

			UnityEditor.Undo.RecordObject(this, "Initialize Sprite Renderer Linker");
			UnityEditor.Undo.RecordObject(m_SpriteRenderer, "Initialize Sprite Renderer Linker");
			m_Key = sprite.name;
			m_SpriteRenderer.sprite = null;
			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.EditorUtility.SetDirty(m_SpriteRenderer);
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
