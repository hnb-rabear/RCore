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
			if (m_Image != null)
				m_Image.overrideSprite = null;
		}

		private void CacheTarget()
		{
			if (m_Image == null)
				m_Image = GetComponent<Image>();
		}

		public void Refresh()
		{
			if (m_Image == null)
				return;
			var sprite = AssetCatalog.Instance != null ? AssetCatalog.Instance.GetSprite(m_Key) : null;
			if (sprite != null)
				m_Image.overrideSprite = sprite;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			CacheTarget();
			Refresh();
		}
#endif
	}
}
