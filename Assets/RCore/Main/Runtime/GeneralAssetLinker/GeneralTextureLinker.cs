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
	}
}
