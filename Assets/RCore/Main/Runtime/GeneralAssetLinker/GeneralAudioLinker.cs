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

#if UNITY_EDITOR
		private void Reset()
		{
			CacheTarget();
			if (m_AudioSource == null || m_AudioSource.clip == null || AssetCatalog.Instance == null)
				return;

			var clip = m_AudioSource.clip;
			if (AssetCatalog.Instance.GetAudioClip(clip.name) == null)
				return;

			UnityEditor.Undo.RecordObject(this, "Initialize Audio Linker");
			UnityEditor.Undo.RecordObject(m_AudioSource, "Initialize Audio Linker");
			m_Key = clip.name;
			m_AudioSource.clip = null;
			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.EditorUtility.SetDirty(m_AudioSource);
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
