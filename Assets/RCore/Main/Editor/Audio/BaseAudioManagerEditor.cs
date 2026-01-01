using RCore.Audio;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Audio
{
	/// <summary>
	/// Custom editor for the BaseAudioManager class, providing inspector controls for playback and collection management.
	/// </summary>
	[CustomEditor(typeof(BaseAudioManager), true)]
	public class BaseAudioManagerEditor : UnityEditor.Editor
	{
		private BaseAudioManager m_script;
		private REditorPrefString m_audioCollectionPath;
		private int m_sfxIndex;

		protected virtual void OnEnable()
		{
			m_script = target as BaseAudioManager;
			m_audioCollectionPath = new REditorPrefString($"{typeof(AudioCollection).FullName}");

			if (m_script.audioCollection != null)
				m_audioCollectionPath.Value = AssetDatabase.GetAssetPath(m_script.audioCollection);
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (m_script.audioCollection == null)
				EditorGUILayout.HelpBox("AudioManager require AudioCollection. " + "To create AudioCollection, In project window select Create/RCore/Create Audio Collection", MessageType.Error);
			else if (GUILayout.Button("Open Audio Collection"))
				Selection.activeObject = m_script.audioCollection;

			EditorGUILayout.BeginHorizontal();
			if (EditorHelper.Button("Stop Music"))
				m_script.StopMusic(1f);
			if (EditorHelper.Button("Play Music"))
				m_script.PlayMusic();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			if (EditorHelper.Button("Play Sfx"))
				m_script.PlaySFX(m_sfxIndex, 0);
			if (EditorHelper.Button("Play Next Sfx"))
			{
				m_sfxIndex++;
				m_script.PlaySFX(m_sfxIndex, 0);
			}
			EditorGUILayout.EndHorizontal();

			if (GUI.changed)
				EditorUtility.SetDirty(m_script);
		}
	}
}