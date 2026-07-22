using UnityEngine;

namespace RCore.Config
{
	[RequireComponent(typeof(AudioSource))]
	public class GeneralAudioLinker : MonoBehaviour
	{
		[SerializeField] private AudioSource m_AudioSource;
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
			if (m_AudioSource != null)
				m_AudioSource.clip = null;
		}

		private void CacheTarget()
		{
			if (m_AudioSource == null)
				m_AudioSource = GetComponent<AudioSource>();
		}

		public void Refresh()
		{
			if (m_AudioSource == null || !Application.isPlaying)
				return;
			var clip = AssetCatalog.Instance != null ? AssetCatalog.Instance.GetAudioClip(m_Key) : null;
			if (clip != null)
				m_AudioSource.clip = clip;
		}

		// Deliberately no OnValidate/edit-mode assignment: AudioSource.clip has no
		// non-serialized override equivalent, so assigning it outside Play mode
		// would bake a hard reference into the prefab.
	}
}
