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
		private AudioCollection m_collection;

		private void OnEnable()
		{
			m_collection = target as AudioCollection;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorHelper.BoxVertical("Audio IDs Generator", () =>
			{
				m_collection.generator.inputMusicsFolder = EditorHelper.FolderField(m_collection.generator.inputMusicsFolder, "Musics Sources Path");
				m_collection.generator.inputSfxsFolder = EditorHelper.FolderField(m_collection.generator.inputSfxsFolder, "SFX Sources Path");
				m_collection.generator.outputIDsFolder = EditorHelper.FolderField(m_collection.generator.outputIDsFolder, "Audio Ids Export folder");

				if (EditorHelper.Button("Sort clips"))
				{
					m_collection.musicClips = m_collection.musicClips.OrderBy(x => x.name).ToArray();
					m_collection.sfxClips = m_collection.sfxClips.OrderBy(x => x.name).ToArray();
				}
				if (EditorHelper.Button("Generate"))
				{
					if (!string.IsNullOrEmpty(m_collection.generator.inputMusicsFolder))
					{
						string musicSourcePath = Application.dataPath + m_collection.generator.inputMusicsFolder.Replace("Assets", "");
						var musicFiles = EditorHelper.GetObjects<AudioClip>(musicSourcePath, "t:AudioClip").ToArray();
						foreach (var clip in musicFiles)
						{
							if (!m_collection.musicClips.Contains(clip))
								m_collection.musicClips.Add(clip, out m_collection.musicClips);
						}
					}
					if (!string.IsNullOrEmpty(m_collection.generator.inputSfxsFolder))
					{
						string sfxSourcePath = Application.dataPath + m_collection.generator.inputSfxsFolder.Replace("Assets", "");
						var sfxFiles = EditorHelper.GetObjects<AudioClip>(sfxSourcePath, "t:AudioClip").ToArray();
						foreach (var clip in sfxFiles)
						{
							if (!m_collection.sfxClips.Contains(clip))
								m_collection.sfxClips.Add(clip, out m_collection.sfxClips);
						}
					}
					for (var i = m_collection.musicClips.Length - 1; i >= 0; i--)
					{
						if (m_collection.musicClips[i] == null)
							m_collection.musicClips.RemoveAt(i, out m_collection.musicClips);
					}
					for (var i = m_collection.sfxClips.Length - 1; i >= 0; i--)
					{
						if (m_collection.sfxClips[i] == null)
							m_collection.sfxClips.RemoveAt(i, out m_collection.sfxClips);
					}

					string exportedContent = GetConfigTemplate();

					//Musics
					string stringKeys = "";
					string intKeys = "";
					string enumKeys = "";
					for (int i = 0; i < m_collection.musicClips.Length; i++)
					{
						string clipName = m_collection.musicClips[i].name
							.Replace(" ", "_")
							.Replace("-", "_")
							.RemoveSpecialCharacters();
						stringKeys += $"\"{clipName}\"";
						intKeys += $"{clipName} = {i}";
						enumKeys += $"{clipName} = {i}";
						if (i < m_collection.musicClips.Length - 1)
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
					for (int i = 0; i < m_collection.sfxClips.Length; i++)
					{
						var clipName = m_collection.sfxClips[i].name
							.Replace(" ", "_")
							.Replace("-", "_")
							.RemoveSpecialCharacters();
						stringKeys += $"\"{clipName}\"";
						intKeys += $"{clipName} = {i}";
						enumKeys += $"{clipName} = {i}";
						if (i < m_collection.sfxClips.Length - 1)
						{
							stringKeys += $",{Environment.NewLine}\t\t";
							intKeys += $",{Environment.NewLine}\t\t";
							enumKeys += $",{Environment.NewLine}\t\t";
						}
					}
					exportedContent = exportedContent.Replace("<S_CONSTANT_INT_KEYS>", intKeys).Replace("<S_CONSTANT_STRING_KEYS>", stringKeys).Replace("<S_CONSTANTS_ENUM_KEYS>", enumKeys);

					if (!string.IsNullOrEmpty(m_collection.generator.@namespace))
					{
						exportedContent = AddTabToEachLine(exportedContent);
						exportedContent = $"namespace {m_collection.generator.@namespace}\n" + "{\n" + exportedContent + "\n}";
					}

					//Write result
					string exportConfigPath = Application.dataPath + m_collection.generator.outputIDsFolder.Replace("Assets", "");
					System.IO.File.WriteAllText(exportConfigPath + "/AudioIDs.cs", exportedContent);

					if (EditorHelper.Button("Validate Asset Bundle Sounds"))
					{
						var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
						m_collection.abSfxClips = new AssetReferenceT<AudioClip>[m_collection.sfxClips.Length];
						for (int i = 0; i < m_collection.sfxClips.Length; i++)
						{
							var clip = m_collection.sfxClips[i];
							string path = AssetDatabase.GetAssetPath(clip);
							string guid = AssetDatabase.AssetPathToGUID(path);
							var entry = addressableSettings.FindAssetEntry(guid);
							if (entry != null)
							{
								m_collection.abSfxClips[i] = new AssetReferenceT<AudioClip>(guid);
								m_collection.sfxClips[i] = null;
							}
						}

						m_collection.abMusicClips = new AssetReferenceT<AudioClip>[m_collection.musicClips.Length];
						for (int i = 0; i < m_collection.musicClips.Length; i++)
						{
							var clip = m_collection.musicClips[i];
							string path = AssetDatabase.GetAssetPath(clip);
							string guid = AssetDatabase.AssetPathToGUID(path);
							var entry = addressableSettings.FindAssetEntry(guid);
							if (entry != null)
							{
								m_collection.abMusicClips[i] = new AssetReferenceT<AudioClip>(guid);
								m_collection.musicClips[i] = null;
							}
						}
					}

					if (GUI.changed)
					{
						EditorUtility.SetDirty(m_collection);
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