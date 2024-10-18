using RCore.Audio;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Audio
{
	[CustomEditor(typeof(BaseAudioManager), true)]
	public class BaseAudioManagerEditor : UnityEditor.Editor
	{
		private BaseAudioManager m_Script;
		private EditorPrefsString m_AudioCollectionPath;

		protected virtual void OnEnable()
		{
			m_Script = target as BaseAudioManager;
			m_AudioCollectionPath = new EditorPrefsString($"{typeof(AudioCollection).FullName}");

			if (m_Script.audioCollection != null)
				m_AudioCollectionPath.Value = AssetDatabase.GetAssetPath(m_Script.audioCollection);
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (m_Script.audioCollection == null)
				EditorGUILayout.HelpBox("AudioManager require AudioCollection. " + "To create AudioCollection, In project window select Create/RCore/Create Audio Collection", MessageType.Error);
			else if (GUILayout.Button("Open Audio Collection"))
				Selection.activeObject = m_Script.audioCollection;

			if (EditorHelper.ButtonColor("Add Music Audio Source", m_Script.m_musicSource == null ? Color.green : Color.grey))
			{
				if (m_Script.m_musicSource == null)
				{
					var obj = new GameObject("Music");
					obj.transform.SetParent(m_Script.transform);
					obj.AddComponent<AudioSource>();
					m_Script.m_musicSource = obj.GetComponent<AudioSource>();
				}
				if (m_Script.m_sfxSourceUnlimited == null)
				{
					var obj = new GameObject("SFX_Unlimited");
					obj.transform.SetParent(m_Script.transform);
					obj.AddComponent<AudioSource>();
					m_Script.m_musicSource = obj.GetComponent<AudioSource>();
				}
			}
			if (EditorHelper.ButtonColor("Add SFX Audio Source"))
				m_Script.CreateMoreSFXSource();
			if (EditorHelper.ButtonColor("Create Audio Sources", Color.green))
				m_Script.CreateAudioSources();
			if (EditorHelper.Button("Stop Music"))
				m_Script.StopMusic(1f);
			if (EditorHelper.Button("Play Music"))
				m_Script.PlayMusic();

			if (GUI.changed)
				EditorUtility.SetDirty(m_Script);
		}
	}
}