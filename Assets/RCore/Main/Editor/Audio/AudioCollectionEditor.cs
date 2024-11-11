using RCore.Audio;
using System;
using System.Linq;
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
				// m_Script.m_MusicsPath = EditorHelper.FolderField(m_Script.m_MusicsPath, "Musics Sources Path");
				// m_Script.m_SfxsPath = EditorHelper.FolderField(m_Script.m_SfxsPath, "SFX Sources Path");
				// m_Script.m_ConfigPath = EditorHelper.FolderField(m_Script.m_ConfigPath, "Export Config Path");

				if (EditorHelper.Button("Build Audio IDs"))
				{
					if (!string.IsNullOrEmpty(m_Script.m_MusicsPath))
					{
						string musicSourcePath = Application.dataPath + m_Script.m_MusicsPath.Replace("Assets", "");
						var musicFiles = EditorHelper.GetObjects<AudioClip>(musicSourcePath, "t:AudioClip").ToArray();
						foreach (var clip in musicFiles)
						{
							if (!m_Script.musicClips.Contains(clip))
								m_Script.musicClips.Add(clip, out m_Script.musicClips);
						}
					}
					if (!string.IsNullOrEmpty(m_Script.m_SfxsPath))
					{
						string sfxSourcePath = Application.dataPath + m_Script.m_SfxsPath.Replace("Assets", "");
						var sfxFiles = EditorHelper.GetObjects<AudioClip>(sfxSourcePath, "t:AudioClip").ToArray();
						foreach (var clip in sfxFiles)
						{
							if (!m_Script.sfxClips.Contains(clip))
								m_Script.sfxClips.Add(clip, out m_Script.sfxClips);
						}
					}
					for (var i = m_Script.musicClips.Length - 1; i >= 0; i--)
					{
						if (m_Script.musicClips[i] == null)
							m_Script.musicClips.RemoveAt(i, out m_Script.musicClips);
					}
					for (var i = m_Script.sfxClips.Length - 1; i >= 0; i--)
					{
						if (m_Script.sfxClips[i] == null)
							m_Script.sfxClips.RemoveAt(i, out m_Script.sfxClips);
					}

					string exportedContent = GetConfigTemplate();

					//Musics
					string stringKeys = "";
					string intKeys = "";
					string enumKeys = "";
					for (int i = 0; i < m_Script.musicClips.Length; i++)
					{
						stringKeys += $"\"{m_Script.musicClips[i].name}\"";
						intKeys += $"{m_Script.musicClips[i].name} = {i}";
						enumKeys += $"{m_Script.musicClips[i].name} = {i}";
						if (i < m_Script.musicClips.Length - 1)
						{
							stringKeys += $",{Environment.NewLine}\t\t";
							intKeys += $",{Environment.NewLine}\t\t";
							enumKeys += $",{Environment.NewLine}\t\t";
						}
					}
					exportedContent = exportedContent.Replace("<M_CONSTANT_INT_KEYS>", intKeys).Replace("<M_CONSTANT_STRING_KEYS>", stringKeys).Replace("<M_CONSTANTS_ENUM_KEYS>", enumKeys);

					//SFXs
					stringKeys = "";
					intKeys = "";
					enumKeys = "";
					for (int i = 0; i < m_Script.sfxClips.Length; i++)
					{
						stringKeys += $"\"{m_Script.sfxClips[i].name}\"";
						intKeys += $"{m_Script.sfxClips[i].name} = {i}";
						enumKeys += $"{m_Script.sfxClips[i].name} = {i}";
						if (i < m_Script.sfxClips.Length - 1)
						{
							stringKeys += $",{Environment.NewLine}\t\t";
							intKeys += $",{Environment.NewLine}\t\t";
							enumKeys += $",{Environment.NewLine}\t\t";
						}
					}
					exportedContent = exportedContent.Replace("<S_CONSTANT_INT_KEYS>", intKeys).Replace("<S_CONSTANT_STRING_KEYS>", stringKeys).Replace("<S_CONSTANTS_ENUM_KEYS>", enumKeys);

					if (!string.IsNullOrEmpty(m_Script.m_Namespace))
					{
						exportedContent = AddTabToEachLine(exportedContent);
						exportedContent = $"namespace {m_Script.m_Namespace}\n" + "{\n" + exportedContent + "\n}";
					}
					
					//Write result
					string exportConfigPath = Application.dataPath + m_Script.m_AudioIdsPath.Replace("Assets", "");
					System.IO.File.WriteAllText(exportConfigPath + "/AudioIDs.cs", exportedContent);

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
						EditorUtility.SetDirty(m_Script);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}
				}
			}, default, true);
		}

		private string GetConfigTemplate()
		{
			// if (string.IsNullOrEmpty(m_Script.m_NameClassSFX))
			// 	m_Script.m_NameClassSFX = "SfxIDs";
			// if (string.IsNullOrEmpty(m_Script.m_NameClassMusic))
			// 	m_Script.m_NameClassMusic = "MusicIDs";
			//
			// string musicTemplate =
			// 	$"public static class {m_Script.m_NameClassMusic}\n"
			// 	+ "{\n"
			// 	+ "\tpublic const int <M_CONSTANT_INT_KEYS>;\n"
			// 	+ "\tpublic static readonly string[] idStrings = new string[] { <M_CONSTANT_STRING_KEYS> };\n"
			// 	+ "\tpublic enum Music { <M_CONSTANTS_ENUM_KEYS> };\n"
			// 	+ "}";
			// string sfxTemplate =
			// 	$"public static class {m_Script.m_NameClassSFX}\n"
			// 	+ "{\n"
			// 	+ "\tpublic const int <S_CONSTANT_INT_KEYS>;\n"
			// 	+ "\tpublic static readonly string[] idStrings = new string[] { <S_CONSTANT_STRING_KEYS> };\n"
			// 	+ "\tpublic enum Sfx { <S_CONSTANTS_ENUM_KEYS> };\n"
			// 	+ "}";
			//
			// string content = $"{musicTemplate}\n{sfxTemplate}";
			// return content;

			string guid = "8f872f9e1f6f8f444a1568810bc25883";
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
			return textAsset.ToString();
		}
		
		public static string AddTabToEachLine(string content)
		{
			// Split the content into lines
			string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			// Add a tab space to the beginning of each line
			for (int i = 0; i < lines.Length; i++)
				lines[i] = "\t" + lines[i];

			// Join the lines back together
			return string.Join(Environment.NewLine, lines);
		}
	}
}