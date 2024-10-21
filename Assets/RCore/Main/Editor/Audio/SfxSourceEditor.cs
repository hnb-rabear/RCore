using RCore.Audio;
using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace RCore.Editor.Audio
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(SfxSource))]
	public class SfxSourceEditor : UnityEditor.Editor
	{
		private AudioCollection m_tempAudioCollection;
		private EditorPrefsString m_audioCollectionPath;
		private SfxSource m_script;
		private string m_search = "";
		private UnityEngine.UI.Button m_button;

		private void OnEnable()
		{
			m_script = target as SfxSource;

			m_script.mClips ??= Array.Empty<string>();

			m_button = m_script.GetComponent<UnityEngine.UI.Button>();
			m_audioCollectionPath = new EditorPrefsString($"{typeof(AudioCollection).FullName}");

			if (m_tempAudioCollection == null)
			{
				if (!string.IsNullOrEmpty(m_audioCollectionPath.Value))
					m_tempAudioCollection = (AudioCollection)AssetDatabase.LoadAssetAtPath(m_audioCollectionPath.Value, typeof(AudioCollection));
			}
			if (m_tempAudioCollection == null)
			{
				var audioManager = FindObjectOfType<BaseAudioManager>();
				if (audioManager != null)
				{
					m_tempAudioCollection = audioManager.audioCollection;
					m_audioCollectionPath.Value = AssetDatabase.GetAssetPath(m_tempAudioCollection);
				}
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorHelper.Separator("Editor");

			if (m_tempAudioCollection == null)
			{
				if (m_tempAudioCollection == null)
					EditorGUILayout.HelpBox("AudioSFX require AudioCollection. " + "To create AudioCollection, select Project windows RUtilities/Create Audio Collection", MessageType.Error);

				var asset = (AudioCollection)EditorHelper.ObjectField<AudioCollection>(m_tempAudioCollection, "Audio Collection", 120);
				if (asset != m_tempAudioCollection)
				{
					m_tempAudioCollection = asset;
					if (m_tempAudioCollection != null)
						m_audioCollectionPath.Value = AssetDatabase.GetAssetPath(m_tempAudioCollection);
				}
				return;
			}

			m_tempAudioCollection = (AudioCollection)EditorHelper.ObjectField<AudioCollection>(m_tempAudioCollection, "Audio Collection", 120);

			if (m_script.mClips.Length > 0)
				EditorHelper.BoxVertical(() =>
				{
					for (int i = 0; i < m_script.mClips.Length; i++)
					{
						int i1 = i;
						EditorHelper.BoxHorizontal(() =>
						{
							EditorHelper.TextField(m_script.mClips[i1], "");
							if (EditorHelper.ButtonColor("x", Color.red, 24))
							{
								var list = m_script.mClips.ToList();
								list.Remove(m_script.mClips[i1]);
								m_script.mClips = list.ToArray();
							}
						});
					}
				}, Color.yellow, true);

			EditorHelper.BoxVertical(() =>
			{
				m_search = EditorHelper.TextField(m_search, "Search");
				if (!string.IsNullOrEmpty(m_search))
				{
					var sfxNames = m_tempAudioCollection.GetSFXNames();
					if (sfxNames != null && sfxNames.Length > 0)
					{
						for (int i = 0; i < sfxNames.Length; i++)
						{
							if (sfxNames[i].ToLower().Contains(m_search.ToLower()))
							{
								if (GUILayout.Button(sfxNames[i]))
								{
									var list = m_script.mClips.ToList();
									if (!list.Contains(sfxNames[i]))
									{
										list.Add(sfxNames[i]);
										m_script.mClips = list.ToArray();
										m_search = "";
										EditorGUI.FocusTextInControl(null);
									}
								}
							}
						}
					}
					else
						EditorGUILayout.HelpBox("No results", MessageType.Warning);
				}
			}, Color.white, true);

			if (EditorHelper.ButtonColor("Open Sounds Collection"))
				Selection.activeObject = m_tempAudioCollection;

			if (m_button != null)
			{
				if (EditorHelper.ButtonColor("Add to OnClick event"))
				{
					UnityAction action = m_script.PlaySFX;
					UnityEventTools.AddVoidPersistentListener(m_button.onClick, action);
				}
			}

			if (GUI.changed)
				EditorUtility.SetDirty(m_script);
		}
	}
}