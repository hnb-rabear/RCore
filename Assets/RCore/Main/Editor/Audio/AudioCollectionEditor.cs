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
		private AudioCollection m_audioCollection;
		private REditorPrefString m_namespace;
		private REditorPrefString m_musicsPath;
		private REditorPrefString m_sfxsPath;
		private REditorPrefString m_audioIdsPath;

		private void OnEnable()
		{
			string assetPath = AssetDatabase.GetAssetPath(target);
			string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

			m_audioCollection = target as AudioCollection;
			m_namespace = new REditorPrefString(assetGUID + nameof(m_namespace));
			m_musicsPath = new REditorPrefString(assetGUID + nameof(m_musicsPath));
			m_sfxsPath = new REditorPrefString(assetGUID + nameof(m_sfxsPath));
			m_audioIdsPath = new REditorPrefString(assetGUID + nameof(m_audioIdsPath));
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorHelper.BoxVertical(() =>
			{
				m_musicsPath.Value = EditorHelper.FolderField(m_musicsPath.Value, "Musics Sources Path");
				m_sfxsPath.Value = EditorHelper.FolderField(m_sfxsPath.Value, "SFX Sources Path");
				m_audioIdsPath.Value = EditorHelper.FolderField(m_audioIdsPath.Value, "Audio Ids Export folder");

				if (EditorHelper.Button("Build Audio IDs"))
				{
					if (!string.IsNullOrEmpty(m_musicsPath.Value))
					{
						string musicSourcePath = Application.dataPath + m_musicsPath.Value.Replace("Assets", "");
						var musicFiles = EditorHelper.GetObjects<AudioClip>(musicSourcePath, "t:AudioClip").ToArray();
						foreach (var clip in musicFiles)
						{
							if (!m_audioCollection.musicClips.Contains(clip))
								m_audioCollection.musicClips.Add(clip, out m_audioCollection.musicClips);
						}
					}
					if (!string.IsNullOrEmpty(m_sfxsPath.Value))
					{
						string sfxSourcePath = Application.dataPath + m_sfxsPath.Value.Replace("Assets", "");
						var sfxFiles = EditorHelper.GetObjects<AudioClip>(sfxSourcePath, "t:AudioClip").ToArray();
						foreach (var clip in sfxFiles)
						{
							if (!m_audioCollection.sfxClips.Contains(clip))
								m_audioCollection.sfxClips.Add(clip, out m_audioCollection.sfxClips);
						}
					}
					for (var i = m_audioCollection.musicClips.Length - 1; i >= 0; i--)
					{
						if (m_audioCollection.musicClips[i] == null)
							m_audioCollection.musicClips.RemoveAt(i, out m_audioCollection.musicClips);
					}
					for (var i = m_audioCollection.sfxClips.Length - 1; i >= 0; i--)
					{
						if (m_audioCollection.sfxClips[i] == null)
							m_audioCollection.sfxClips.RemoveAt(i, out m_audioCollection.sfxClips);
					}

					string exportedContent = GetConfigTemplate();

					//Musics
					string stringKeys = "";
					string intKeys = "";
					string enumKeys = "";
					for (int i = 0; i < m_audioCollection.musicClips.Length; i++)
					{
						string clipName = m_audioCollection.musicClips[i].name
							.Replace(" ", "_")
							.Replace("-", "_")
							.RemoveSpecialCharacters();
						stringKeys += $"\"{clipName}\"";
						intKeys += $"{clipName} = {i}";
						enumKeys += $"{clipName} = {i}";
						if (i < m_audioCollection.musicClips.Length - 1)
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
					for (int i = 0; i < m_audioCollection.sfxClips.Length; i++)
					{
						var clipName = m_audioCollection.sfxClips[i].name
							.Replace(" ", "_")
							.Replace("-", "_")
							.RemoveSpecialCharacters();
						stringKeys += $"\"{clipName}\"";
						intKeys += $"{clipName} = {i}";
						enumKeys += $"{clipName} = {i}";
						if (i < m_audioCollection.sfxClips.Length - 1)
						{
							stringKeys += $",{Environment.NewLine}\t\t";
							intKeys += $",{Environment.NewLine}\t\t";
							enumKeys += $",{Environment.NewLine}\t\t";
						}
					}
					exportedContent = exportedContent.Replace("<S_CONSTANT_INT_KEYS>", intKeys).Replace("<S_CONSTANT_STRING_KEYS>", stringKeys).Replace("<S_CONSTANTS_ENUM_KEYS>", enumKeys);

					if (!string.IsNullOrEmpty(m_namespace.Value))
					{
						exportedContent = AddTabToEachLine(exportedContent);
						exportedContent = $"namespace {m_namespace.Value}\n" + "{\n" + exportedContent + "\n}";
					}

					//Write result
					string exportConfigPath = Application.dataPath + m_audioIdsPath.Value.Replace("Assets", "");
					System.IO.File.WriteAllText(exportConfigPath + "/AudioIDs.cs", exportedContent);

					if (EditorHelper.Button("Validate Asset Bundle Sounds"))
					{
						var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
						m_audioCollection.abSfxClips = new AssetReferenceT<AudioClip>[m_audioCollection.sfxClips.Length];
						for (int i = 0; i < m_audioCollection.sfxClips.Length; i++)
						{
							var clip = m_audioCollection.sfxClips[i];
							string path = AssetDatabase.GetAssetPath(clip);
							string guid = AssetDatabase.AssetPathToGUID(path);
							var entry = addressableSettings.FindAssetEntry(guid);
							if (entry != null)
							{
								m_audioCollection.abSfxClips[i] = new AssetReferenceT<AudioClip>(guid);
								m_audioCollection.sfxClips[i] = null;
							}
						}

						m_audioCollection.abMusicClips = new AssetReferenceT<AudioClip>[m_audioCollection.musicClips.Length];
						for (int i = 0; i < m_audioCollection.musicClips.Length; i++)
						{
							var clip = m_audioCollection.musicClips[i];
							string path = AssetDatabase.GetAssetPath(clip);
							string guid = AssetDatabase.AssetPathToGUID(path);
							var entry = addressableSettings.FindAssetEntry(guid);
							if (entry != null)
							{
								m_audioCollection.abMusicClips[i] = new AssetReferenceT<AudioClip>(guid);
								m_audioCollection.musicClips[i] = null;
							}
						}
					}

					if (GUI.changed)
					{
						EditorUtility.SetDirty(m_audioCollection);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}
				}
			}, ColorHelper.LightAzure, true);
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