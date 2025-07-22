/**
 * Author HNB-RaBear - 2021
 **/

using RCore.Audio;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;

namespace RCore.Editor.Audio
{
	/// <summary>
	/// Custom editor for the AudioCollection ScriptableObject.
	/// Provides tools to automatically populate, sort, and generate ID scripts for audio clips.
	/// It also includes functionality to convert direct references to Addressable Asset References.
	/// </summary>
	[CustomEditor(typeof(AudioCollection))]
	public class AudioCollectionEditor : UnityEditor.Editor
	{
		private AudioCollection m_collection;

		/// <summary>
		/// Called when the editor is enabled. Caches the target AudioCollection instance.
		/// </summary>
		private void OnEnable()
		{
			m_collection = target as AudioCollection;
		}

		/// <summary>
		/// Draws the custom inspector GUI for the AudioCollection.
		/// </summary>
		public override void OnInspectorGUI()
		{
			// Draw the default inspector fields first.
			base.OnInspectorGUI();

			// Create a dedicated section for the generator tools.
			EditorHelper.BoxVertical("Audio IDs Generator", () =>
			{
				// --- Path Configuration ---
				m_collection.generator.inputMusicsFolder = EditorHelper.FolderField(m_collection.generator.inputMusicsFolder, "Musics Sources Path");
				m_collection.generator.inputSfxsFolder = EditorHelper.FolderField(m_collection.generator.inputSfxsFolder, "SFX Sources Path");
				m_collection.generator.outputIDsFolder = EditorHelper.FolderField(m_collection.generator.outputIDsFolder, "Audio Ids Export folder");

				// --- Sorting Button ---
				if (EditorHelper.Button("Sort clips"))
				{
					// Sorts both music and SFX clips alphabetically by name.
					m_collection.musicClips = m_collection.musicClips.OrderBy(x => x.name).ToArray();
					m_collection.sfxClips = m_collection.sfxClips.OrderBy(x => x.name).ToArray();
					Debug.Log("Audio clips sorted alphabetically.");
				}

				// --- Generation Button ---
				if (EditorHelper.Button("Generate"))
				{
					// --- Step 1: Populate clips from folders ---
					// Find all AudioClips in the specified music folder and add any new ones.
					if (!string.IsNullOrEmpty(m_collection.generator.inputMusicsFolder))
					{
						string musicSourcePath = Application.dataPath + m_collection.generator.inputMusicsFolder.Replace("Assets", "");
						var musicFiles = EditorHelper.GetObjects<AudioClip>(musicSourcePath, "t:AudioClip").ToArray();
						foreach (var clip in musicFiles)
						{
							if (!m_collection.musicClips.Contains(clip))
								ArrayUtility.Add(ref m_collection.musicClips, clip);
						}
					}
					// Find all AudioClips in the specified SFX folder and add any new ones.
					if (!string.IsNullOrEmpty(m_collection.generator.inputSfxsFolder))
					{
						string sfxSourcePath = Application.dataPath + m_collection.generator.inputSfxsFolder.Replace("Assets", "");
						var sfxFiles = EditorHelper.GetObjects<AudioClip>(sfxSourcePath, "t:AudioClip").ToArray();
						foreach (var clip in sfxFiles)
						{
							if (!m_collection.sfxClips.Contains(clip))
								ArrayUtility.Add(ref m_collection.sfxClips, clip);
						}
					}
					// Clean up any null (empty) entries in the arrays.
					m_collection.musicClips = m_collection.musicClips.Where(c => c != null).ToArray();
					m_collection.sfxClips = m_collection.sfxClips.Where(c => c != null).ToArray();

					// --- Step 2: Generate C# Code ---
					string exportedContent = GetConfigTemplate();

					// Generate code for music clips
					string stringKeys = "";
					string intKeys = "";
					string enumKeys = "";
					for (int i = 0; i < m_collection.musicClips.Length; i++)
					{
						// Sanitize the clip name to be a valid C# identifier.
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
					// Replace placeholders in the template with the generated strings.
					exportedContent = exportedContent.Replace("<M_CONSTANT_INT_KEYS>", intKeys)
											   .Replace("<M_CONSTANT_STRING_KEYS>", stringKeys)
											   .Replace("<M_CONSTANTS_ENUM_KEYS>", enumKeys);

					// Generate code for SFX clips
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
					exportedContent = exportedContent.Replace("<S_CONSTANT_INT_KEYS>", intKeys)
											   .Replace("<S_CONSTANT_STRING_KEYS>", stringKeys)
											   .Replace("<S_CONSTANTS_ENUM_KEYS>", enumKeys);

					// Optionally wrap the generated code in a namespace.
					if (!string.IsNullOrEmpty(m_collection.generator.@namespace))
					{
						exportedContent = AddTabToEachLine(exportedContent);
						exportedContent = $"namespace {m_collection.generator.@namespace}\n" + "{\n" + exportedContent + "\n}";
					}

					// --- Step 3: Write the file ---
					string exportConfigPath = Application.dataPath + m_collection.generator.outputIDsFolder.Replace("Assets", "");
					System.IO.File.WriteAllText(exportConfigPath + "/AudioIDs.cs", exportedContent);
					Debug.Log($"AudioIDs.cs generated successfully at {exportConfigPath}");
				}

				// --- Addressables Validation Button ---
				if (EditorHelper.Button("Validate Addressable Sounds"))
				{
					var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
					// Process SFX clips
					m_collection.abSfxClips = new AssetReferenceT<AudioClip>[m_collection.sfxClips.Length];
					for (int i = 0; i < m_collection.sfxClips.Length; i++)
					{
						var clip = m_collection.sfxClips[i];
						string path = AssetDatabase.GetAssetPath(clip);
						string guid = AssetDatabase.AssetPathToGUID(path);
						// If the asset has an Addressable entry, convert it.
						if (addressableSettings.FindAssetEntry(guid) != null)
						{
							m_collection.abSfxClips[i] = new AssetReferenceT<AudioClip>(guid);
							m_collection.sfxClips[i] = null; // Nullify the direct reference.
						}
					}
					// Process music clips
					m_collection.abMusicClips = new AssetReferenceT<AudioClip>[m_collection.musicClips.Length];
					for (int i = 0; i < m_collection.musicClips.Length; i++)
					{
						var clip = m_collection.musicClips[i];
						string path = AssetDatabase.GetAssetPath(clip);
						string guid = AssetDatabase.AssetPathToGUID(path);
						if (addressableSettings.FindAssetEntry(guid) != null)
						{
							m_collection.abMusicClips[i] = new AssetReferenceT<AudioClip>(guid);
							m_collection.musicClips[i] = null;
						}
					}
					Debug.Log("Validated Addressable sounds. Direct references were converted.");
				}

				// If any changes were made, mark the object as dirty and save assets.
				if (GUI.changed)
				{
					EditorUtility.SetDirty(m_collection);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
			}, default, true);
		}

		/// <summary>
		/// Loads the C# template file for the audio ID script.
		/// The template is loaded via its hardcoded asset GUID.
		/// </summary>
		/// <returns>The content of the template file as a string.</returns>
		private string GetConfigTemplate()
		{
			// GUID points to "AudioIDTemplate.txt"
			string guid = "8f872f9e1f6f8f444a1568810bc25883";
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
			if (textAsset == null)
			{
				Debug.LogError("Audio ID template not found. Make sure the asset with GUID " + guid + " exists.");
				return "";
			}
			return textAsset.ToString();
		}

		/// <summary>
		/// A utility function to add a tab character to the beginning of each line in a string.
		/// Used for formatting the generated code within a namespace block.
		/// </summary>
		/// <param name="content">The multi-line string to process.</param>
		/// <returns>The indented string.</returns>
		public static string AddTabToEachLine(string content)
		{
			if (string.IsNullOrEmpty(content)) return "";

			// Split the content into lines.
			string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			// Add a tab space to the beginning of each line.
			for (int i = 0; i < lines.Length; i++)
				lines[i] = "\t" + lines[i];

			// Join the lines back together.
			return string.Join(Environment.NewLine, lines);
		}
	}
}