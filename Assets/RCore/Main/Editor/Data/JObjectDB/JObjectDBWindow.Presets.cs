/**
 * Author HNB-RaBear - 2024
 * JObjectDBWindow — partial: Presets
 **/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RCore.Data.JObject;
using UnityEditor;
using UnityEngine;
using NJObject = Newtonsoft.Json.Linq.JObject;
using NJArray = Newtonsoft.Json.Linq.JArray;

namespace RCore.Editor.Data.JObject
{
	public partial class JObjectDBWindow
	{
		//==========================================================================
		// Presets (Feature 3)
		//==========================================================================

		private void DrawPresetSection()
		{
			GUILayout.Space(4);
			EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

			if (m_presetNames != null && m_presetNames.Length > 0)
			{
				m_selectedPresetIndex = EditorGUILayout.Popup(m_selectedPresetIndex, m_presetNames);

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Load", GUILayout.Height(20)))
				{
					if (m_selectedPresetIndex >= 0 && m_selectedPresetIndex < m_presetNames.Length)
					{
						string path = Path.Combine(GetPresetsDirectory(), m_presetNames[m_selectedPresetIndex] + ".json");
						if (File.Exists(path))
						{
							ImportFromFile(path);
							SetStatus($"✓ Loaded preset: {m_presetNames[m_selectedPresetIndex]}");
						}
					}
				}

				var prevBgColor = GUI.backgroundColor;
				GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
				if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(20)))
				{
					if (m_selectedPresetIndex >= 0 && m_selectedPresetIndex < m_presetNames.Length)
					{
						string presetName = m_presetNames[m_selectedPresetIndex];
						if (EditorUtility.DisplayDialog("Delete Preset", $"Delete preset '{presetName}'?", "Delete", "Cancel"))
						{
							string path = Path.Combine(GetPresetsDirectory(), presetName + ".json");
							if (File.Exists(path)) File.Delete(path);
							RefreshPresetList();
							SetStatus($"✓ Deleted preset: {presetName}");
						}
					}
				}
				GUI.backgroundColor = prevBgColor;
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				EditorGUILayout.LabelField("(no presets)", EditorStyles.centeredGreyMiniLabel);
			}

			if (GUILayout.Button("Save Current as Preset", GUILayout.Height(20)))
			{
				string presetName = EditorInputDialog.Show("Save Preset", "Enter preset name:", "");
				if (!string.IsNullOrEmpty(presetName))
				{
					SavePreset(presetName);
					SetStatus($"✓ Saved preset: {presetName}");
				}
			}
		}

		private string GetPresetsDirectory()
		{
			string dir = Path.Combine(Application.dataPath.Replace("Assets", "Saves"), "Presets");
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir;
		}

		private void RefreshPresetList()
		{
			string dir = GetPresetsDirectory();
			if (Directory.Exists(dir))
			{
				m_presetNames = Directory.GetFiles(dir, "*.json")
					.Select(Path.GetFileNameWithoutExtension)
					.OrderBy(n => n)
					.ToArray();
			}
			else
			{
				m_presetNames = Array.Empty<string>();
			}
			m_selectedPresetIndex = m_presetNames.Length > 0 ? 0 : -1;
		}

		private void SavePreset(string name)
		{
			// Sanitize filename
			foreach (char c in Path.GetInvalidFileNameChars())
				name = name.Replace(c, '_');

			string path = Path.Combine(GetPresetsDirectory(), name + ".json");
			string json = JsonConvert.SerializeObject(JObjectDB.GetAllData());
			File.WriteAllText(path, json);
			RefreshPresetList();
		}

	}
}
