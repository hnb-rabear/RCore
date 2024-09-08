using UnityEngine;
using RCore.Common;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
	/// <summary>
	/// Simple audio collection
	/// Simple enough for small game which does not require many sounds
	/// </summary>
	[CreateAssetMenu(fileName = "AudioCollection", menuName = "RCore/Audio Collection")]
	public class AudioCollection : ScriptableObject
	{
		[SerializeField] private bool m_ImportFromFolder = true;
		[SerializeField] private string m_Namespace;
		[SerializeField] private string m_NameClassMusic = "MusicIDs";
		[SerializeField] private string m_NameClassSFX = "SfxIDs";
		[SerializeField] private string m_MusicsPath;
		[SerializeField] private string m_SfxsPath;
		[SerializeField] private string m_ConfigPath;

		public AudioClip[] sfxClips;
		public AudioClip[] musicClips;

		public AudioClip GetMusicClip(int pKey)
		{
			if (pKey < musicClips.Length)
				return musicClips[pKey];
			return null;
		}

		public AudioClip GetMusicClip(string pKey)
		{
			for (int i = 0; i < musicClips.Length; i++)
			{
				if (musicClips[i].name == pKey)
					return musicClips[i];
			}
			return null;
		}

		public AudioClip GetMusicClip(string pKey, ref int pIndex)
		{
			for (int i = 0; i < musicClips.Length; i++)
			{
				if (musicClips[i].name == pKey)
				{
					pIndex = i;
					return musicClips[i];
				}
			}
			return null;
		}

		public AudioClip GetSFXClip(int pIndex)
		{
			if (pIndex < sfxClips.Length)
				return sfxClips[pIndex];
			return null;
		}

		public AudioClip GetSFXClip(string pName)
		{
			for (int i = 0; i < sfxClips.Length; i++)
			{
				if (sfxClips[i].name == pName)
					return sfxClips[i];
			}
			return null;
		}

		public AudioClip GetSFXClip(string pKey, ref int pIndex)
		{
			for (int i = 0; i < sfxClips.Length; i++)
			{
				if (sfxClips[i].name == pKey)
				{
					pIndex = i;
					return sfxClips[i];
				}
			}
			return null;
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(AudioCollection))]
		private class AudioCollectionEditor : UnityEditor.Editor
		{
			private AudioCollection m_Script;

			private void OnEnable()
			{
				m_Script = target as AudioCollection;
				UnityEngine.Debug.Log(m_Script.name);
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorHelper.BoxVertical(() =>
				{
					string musicsSourcePath = m_Script.m_MusicsPath;
					string sfxsSourcePath = m_Script.m_SfxsPath;
					string exportConfigPath = m_Script.m_ConfigPath;
					musicsSourcePath = EditorHelper.FolderSelector("Musics Sources Path", $"{m_Script.name}musicsSourcePath", m_Script.m_MusicsPath, true);
					sfxsSourcePath = EditorHelper.FolderSelector("SFX Sources Path", $"{m_Script.name}sfxsSourcePath", m_Script.m_SfxsPath, true);
					exportConfigPath = EditorHelper.FolderSelector("Export Config Path", $"{m_Script.name}exportConfigPath", m_Script.m_ConfigPath, true);

					if (EditorHelper.Button("Build"))
					{
						musicsSourcePath = Application.dataPath + musicsSourcePath;
						sfxsSourcePath = Application.dataPath + sfxsSourcePath;
						exportConfigPath = Application.dataPath + exportConfigPath;

						var musicFiles = m_Script.musicClips;
						var sfxFiles = m_Script.sfxClips;
						if (m_Script.m_ImportFromFolder)
						{
							musicFiles = EditorHelper.GetObjects<AudioClip>(musicsSourcePath, "t:AudioClip").ToArray();
							sfxFiles = EditorHelper.GetObjects<AudioClip>(sfxsSourcePath, "t:AudioClip").ToArray();
						}
						string result = GetConfigTemplate();

						//Musics
						m_Script.musicClips = new AudioClip[musicFiles.Length];
						string stringKeys = "";
						string intKeys = "";
						string enumKeys = "";
						for (int i = 0; i < musicFiles.Length; i++)
						{
							m_Script.musicClips[i] = musicFiles[i];

							stringKeys += $"\"{musicFiles[i].name}\"";
							intKeys += $"{musicFiles[i].name.ToUpper()} = {i}";
							enumKeys += $"{musicFiles[i].name} = {i}";
							if (i < musicFiles.Length - 1)
							{
								stringKeys += $",{Environment.NewLine}\t\t";
								intKeys += $",{Environment.NewLine}\t\t";
								enumKeys += $",{Environment.NewLine}\t\t";
							}
						}
						result = result.Replace("<M_CONSTANT_INT_KEYS>", intKeys).Replace("<M_CONSTANT_STRING_KEYS>", stringKeys).Replace("<M_CONSTANTS_ENUM_KEYS>", enumKeys);

						//SFXs
						m_Script.sfxClips = new AudioClip[sfxFiles.Length];
						stringKeys = "";
						intKeys = "";
						enumKeys = "";
						for (int i = 0; i < sfxFiles.Length; i++)
						{
							m_Script.sfxClips[i] = sfxFiles[i];

							stringKeys += $"\"{sfxFiles[i].name}\"";
							intKeys += $"{sfxFiles[i].name.ToUpper()} = {i}";
							enumKeys += $"{sfxFiles[i].name} = {i}";
							if (i < sfxFiles.Length - 1)
							{
								stringKeys += $",{Environment.NewLine}\t\t";
								intKeys += $",{Environment.NewLine}\t\t";
								enumKeys += $",{Environment.NewLine}\t\t";
							}
						}
						result = result.Replace("<S_CONSTANT_INT_KEYS>", intKeys).Replace("<S_CONSTANT_STRING_KEYS>", stringKeys).Replace("<S_CONSTANTS_ENUM_KEYS>", enumKeys);

						//Write result
						System.IO.File.WriteAllText(exportConfigPath + "/AudioIDs.cs", result);

						if (GUI.changed)
						{
							m_Script.m_MusicsPath = musicsSourcePath.Replace(Application.dataPath, "");
							m_Script.m_SfxsPath = sfxsSourcePath.Replace(Application.dataPath, "");
							m_Script.m_ConfigPath = exportConfigPath.Replace(Application.dataPath, "");
							EditorUtility.SetDirty(m_Script);
							AssetDatabase.SaveAssets();
							AssetDatabase.Refresh();
						}
					}
				}, ColorHelper.LightAzure, true);
			}

			private string GetConfigTemplate()
			{
				if (string.IsNullOrEmpty(m_Script.m_NameClassSFX))
					m_Script.m_NameClassSFX = "SfxIDs";
				if (string.IsNullOrEmpty(m_Script.m_NameClassMusic))
					m_Script.m_NameClassMusic = "MusicIDs";

				string musicTemplate =
					$"public class {m_Script.m_NameClassMusic}\n"
					+ "{\n"
					+ "\tpublic const int <M_CONSTANT_INT_KEYS>;\n"
					+ "\tpublic static readonly string[] idStrings = new string[] { <M_CONSTANT_STRING_KEYS> };\n"
					+ "\tpublic enum Music { <M_CONSTANTS_ENUM_KEYS> };\n"
					+ "}";
				string sfxTemplate =
					$"public class {m_Script.m_NameClassSFX}\n"
					+ "{\n"
					+ "\tpublic const int <S_CONSTANT_INT_KEYS>;\n"
					+ "\tpublic static readonly string[] idStrings = new string[] { <S_CONSTANT_STRING_KEYS> };\n"
					+ "\tpublic enum Sfx { <S_CONSTANTS_ENUM_KEYS> };\n"
					+ "}";
				string content = "";
				if (!string.IsNullOrEmpty(m_Script.m_Namespace))
				{
					content = $"namespace {m_Script.m_Namespace}" +
						"\n{" +
						$"\n\t{musicTemplate}" +
						$"\n\t{sfxTemplate}" +
						"\n}";
				}
				else
				{
					content = $"\n{musicTemplate}\n{sfxTemplate}";
				}
				return content;
			}
		}
#endif
	}
}