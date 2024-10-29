using RCore.Audio;
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;

namespace RCore.Editor.Audio
{
	[CustomEditor(typeof(AudioCollection))]
	public class AudioCollectionEditor : UnityEditor.Editor
	{
		private AudioCollection m_Script;

		private void OnEnable()
		{
			m_Script = target as AudioCollection;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorHelper.BoxVertical(() =>
			{
				m_Script.m_MusicsPath = EditorHelper.FolderField(m_Script.m_MusicsPath, "Musics Sources Path");
				m_Script.m_SfxsPath = EditorHelper.FolderField(m_Script.m_SfxsPath, "SFX Sources Path");
				m_Script.m_ConfigPath = EditorHelper.FolderField(m_Script.m_ConfigPath, "Export Config Path");

				if (EditorHelper.Button("Build"))
				{
					string musicSourcePath = Application.dataPath + m_Script.m_MusicsPath;
					string sfxSourcePath = Application.dataPath + m_Script.m_SfxsPath;
					string exportConfigPath = Application.dataPath + m_Script.m_ConfigPath;

					var musicFiles = m_Script.musicClips;
					var sfxFiles = m_Script.sfxClips;
					if (m_Script.m_ImportFromFolder)
					{
						musicFiles = EditorHelper.GetObjects<AudioClip>(musicSourcePath, "t:AudioClip").ToArray();
						sfxFiles = EditorHelper.GetObjects<AudioClip>(sfxSourcePath, "t:AudioClip").ToArray();
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
						intKeys += $"{musicFiles[i].name} = {i}";
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
						intKeys += $"{sfxFiles[i].name} = {i}";
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

					if (EditorHelper.Button("Validate Asset Bundle Sounds"))
					{
						var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
						m_Script.abSfxClips = new AssetReferenceT<AudioClip>[m_Script.sfxClips.Length];
						for (int i = 0; i < m_Script.sfxClips.Length; i++)
						{
							var clip = m_Script.sfxClips[i];
							string path = AssetDatabase.GetAssetPath(clip);
							string guid = AssetDatabase.AssetPathToGUID(path);
							var entry = addressableSettings.FindAssetEntry(guid);
							if (entry != null)
							{
								m_Script.abSfxClips[i] = new AssetReferenceT<AudioClip>(guid);
								m_Script.sfxClips[i] = null;
							}
						}

						m_Script.abMusicClips = new AssetReferenceT<AudioClip>[m_Script.musicClips.Length];
						for (int i = 0; i < m_Script.musicClips.Length; i++)
						{
							var clip = m_Script.musicClips[i];
							string path = AssetDatabase.GetAssetPath(clip);
							string guid = AssetDatabase.AssetPathToGUID(path);
							var entry = addressableSettings.FindAssetEntry(guid);
							if (entry != null)
							{
								m_Script.abMusicClips[i] = new AssetReferenceT<AudioClip>(guid);
								m_Script.musicClips[i] = null;
							}
						}
					}

					if (GUI.changed)
					{
						m_Script.m_MusicsPath = musicSourcePath.Replace(Application.dataPath, "");
						m_Script.m_SfxsPath = sfxSourcePath.Replace(Application.dataPath, "");
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
				$"public static class {m_Script.m_NameClassMusic}\n"
				+ "{\n"
				+ "\tpublic const int <M_CONSTANT_INT_KEYS>;\n"
				+ "\tpublic static readonly string[] idStrings = new string[] { <M_CONSTANT_STRING_KEYS> };\n"
				+ "\tpublic enum Music { <M_CONSTANTS_ENUM_KEYS> };\n"
				+ "}";
			string sfxTemplate =
				$"public static class {m_Script.m_NameClassSFX}\n"
				+ "{\n"
				+ "\tpublic const int <S_CONSTANT_INT_KEYS>;\n"
				+ "\tpublic static readonly string[] idStrings = new string[] { <S_CONSTANT_STRING_KEYS> };\n"
				+ "\tpublic enum Sfx { <S_CONSTANTS_ENUM_KEYS> };\n"
				+ "}";
			string content = "";
			if (!string.IsNullOrEmpty(m_Script.m_Namespace))
			{
				content = $"namespace {m_Script.m_Namespace}" + "\n{" + $"\n\t{musicTemplate}" + $"\n\t{sfxTemplate}" + "\n}";
			}
			else
			{
				content = $"\n{musicTemplate}\n{sfxTemplate}";
			}
			return content;
		}
	}
}