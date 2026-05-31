/**
 * Author HNB-RaBear - 2024
 * JObjectDBWindow — partial: Import
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
		// Import Save Data
		//==========================================================================

		/// <summary>
		/// Imports save data from a file, supporting both wrapped format (with metadata)
		/// and raw JObjectDB format. Handles Play mode safely.
		/// </summary>
		private void ImportFromFile(string filePath)
		{
			string content;
			try
			{
				content = File.ReadAllText(filePath);
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Import Error", $"Failed to read file:\n{ex.Message}", "OK");
				return;
			}

			if (string.IsNullOrEmpty(content))
			{
				EditorUtility.DisplayDialog("Import Error", "File is empty.", "OK");
				return;
			}

			ImportContent(content, Path.GetFileName(filePath));
		}

		/// <summary>
		/// Imports save data from the system clipboard.
		/// </summary>
		private void ImportFromClipboard()
		{
			string content = EditorGUIUtility.systemCopyBuffer;

			if (string.IsNullOrEmpty(content))
			{
				EditorUtility.DisplayDialog("Import Error", "Clipboard is empty.", "OK");
				return;
			}

			ImportContent(content, "Clipboard");
		}

		/// <summary>
		/// Core import logic shared by file and clipboard import.
		/// Detects wrapped/raw format, confirms with user, and handles Play mode safely.
		/// </summary>
		private void ImportContent(string content, string sourceName)
		{
			// Detect format and extract raw data
			string jsonData;
			try
			{
				jsonData = ExtractAndConfirmImport(content, sourceName);
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Import Error", $"Invalid JSON format:\n{ex.Message}", "OK");
				return;
			}

			if (string.IsNullOrEmpty(jsonData))
				return; // User cancelled

			// Import with Play mode safety
			if (Application.isPlaying)
			{
				if (!EditorUtility.DisplayDialog("Import Save Data",
					"Game is running.\nImport will write to PlayerPrefs and stop Play mode.\n\nNew data takes effect on next Play.",
					"Import & Stop Play", "Cancel"))
					return;

				DisableAllManagerAutoSave();
				JObjectDB.Import(jsonData);
				EditorApplication.isPlaying = false;
				SetStatus("✓ Imported & stopped Play mode. Press Play to load new data.");
			}
			else
			{
				JObjectDB.Import(jsonData);
				SetStatus($"✓ Imported from {sourceName}");
			}

			RefreshData();
		}

		/// <summary>
		/// Parses content, detects wrapped vs raw format, shows metadata confirmation if wrapped.
		/// Returns the raw JObjectDB JSON data string, or null if user cancels.
		/// </summary>
		private string ExtractAndConfirmImport(string content, string sourceName)
		{
			var parsed = NJObject.Parse(content);

			// Detect wrapped format: has "data" field that is an object (dict)
			if (parsed.TryGetValue("data", out var dataToken) && dataToken.Type == JTokenType.Object)
			{
				string device = parsed["device"]?.ToString() ?? "Unknown";
				string os = parsed["os"]?.ToString() ?? "Unknown";
				string appVersion = parsed["appVersion"]?.ToString() ?? "Unknown";
				string exportTime = parsed["exportTime"]?.ToString() ?? "Unknown";
				int collectionCount = ((NJObject)dataToken).Count;

				string message = $"Device: {device}\n"
				                 + $"OS: {os}\n"
				                 + $"App Version: {appVersion}\n"
				                 + $"Export Time: {exportTime}\n"
				                 + $"Collections: {collectionCount}\n"
				                 + $"\nSource: {sourceName}";

				if (!EditorUtility.DisplayDialog("Import Save Data", message, "Import", "Cancel"))
					return null;

				return dataToken.ToString(Formatting.None);
			}

			// Raw format — validate it's a Dictionary<string, string>
			// (each value should be a string or parseable object)
			return content;
		}

		/// <summary>
		/// Disables auto-save on all active JObjectDBManagerV2 instances to prevent
		/// stale in-memory data from overwriting imported PlayerPrefs data on quit.
		/// Uses reflection since this editor code doesn't reference game-specific types.
		/// </summary>
		private static void DisableAllManagerAutoSave()
		{
			// Find all MonoBehaviours that inherit from JObjectDBManagerV2<>
			var allMonoBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
			foreach (var mb in allMonoBehaviours)
			{
				var type = mb.GetType();
				while (type != null && type != typeof(MonoBehaviour))
				{
					if (type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("JObjectDBManagerV2"))
					{
						var method = type.GetMethod("EnableAutoSave", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
						method?.Invoke(mb, new object[] { false });
						break;
					}
					type = type.BaseType;
				}
			}
		}

	}
}
