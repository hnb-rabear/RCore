/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	[Serializable]
	internal sealed class PendingConfigAssetEntry
	{
		public string FullTypeName;
		public string JsonAssetPath;
		public string ScriptFolder;
	}

	[Serializable]
	internal sealed class PendingConfigAssetStore
	{
		public List<PendingConfigAssetEntry> Entries = new List<PendingConfigAssetEntry>();
	}

	/// <summary>
	/// Bridges a Configuration export to the typed asset it feeds. The generated C# does not exist yet when the
	/// export finishes, so each request is parked in <see cref="SessionState"/> and resolved after the
	/// domain reload that compiles it.
	/// </summary>
	internal static class SheetXConfigAssetBuilder
	{
		private const string PendingKey = "SheetX.PendingConfigAssets";

		/// <summary>
		/// Records the asset work one Configuration export left for the next reload. A second export of the same
		/// generated type replaces the earlier request; different types queue side by side.
		/// </summary>
		internal static void RegisterPendingAsset(string fullTypeName, string jsonAssetPath, string scriptFolder)
		{
			if (string.IsNullOrEmpty(fullTypeName) || string.IsNullOrEmpty(jsonAssetPath) || string.IsNullOrEmpty(scriptFolder))
			{
				Debug.LogError("SheetX: a pending Configuration asset needs a type name, a JSON path, and a script folder.");
				return;
			}

			var store = LoadStore(out bool malformed);
			if (malformed)
				Debug.LogError("SheetX: pending Configuration asset state was unreadable and has been replaced.");
			var entry = store.Entries.FirstOrDefault(
				e => string.Equals(e?.FullTypeName, fullTypeName, StringComparison.Ordinal));
			if (entry == null)
			{
				entry = new PendingConfigAssetEntry();
				store.Entries.Add(entry);
			}
			entry.FullTypeName = fullTypeName;
			entry.JsonAssetPath = jsonAssetPath;
			entry.ScriptFolder = scriptFolder;
			SaveStore(store);
		}

		[DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			var store = LoadStore(out _);
			if (store.Entries.Count == 0)
				return;

			// Snapshot: an entry that cannot be resolved yet stays pending so repairing the generated
			// script and reloading again finishes the job without re-exporting.
			var remaining = new List<PendingConfigAssetEntry>();
			foreach (var entry in store.Entries.ToList())
			{
				if (entry == null)
					continue;
				if (!Resolve(entry))
					remaining.Add(entry);
			}

			if (remaining.Count == 0)
				SessionState.EraseString(PendingKey);
			else
				SaveStore(new PendingConfigAssetStore { Entries = remaining });
		}

		/// <summary>Returns true when this entry is finished — either applied or terminally ambiguous.</summary>
		private static bool Resolve(PendingConfigAssetEntry entry)
		{
			var generatedType = FindType(entry.FullTypeName);
			if (generatedType == null)
			{
				Debug.LogError($"SheetX: generated Configuration type '{entry.FullTypeName}' was not found after reload.");
				return false;
			}

			var assets = FindAssetsOfExactType(generatedType);
			if (assets.Count > 1)
			{
				Debug.LogError($"SheetX: '{entry.FullTypeName}' has more than one asset, so none was updated: "
					+ string.Join(", ", assets.Select(AssetDatabase.GetAssetPath)));
				// Terminal: the export cannot pick for the user. A later export re-registers this entry.
				return true;
			}

			var asset = assets.Count == 1 ? assets[0] : CreateAsset(generatedType, entry.ScriptFolder);
			if (asset == null)
				return false;

			var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(entry.JsonAssetPath);
			if (jsonAsset == null)
			{
				Debug.LogError($"SheetX: Configuration JSON '{entry.JsonAssetPath}' was not found; '{entry.FullTypeName}' keeps its previous data.");
				return false;
			}

			return Apply(asset, generatedType, jsonAsset);
		}

		private static bool Apply(ScriptableObject asset, Type generatedType, TextAsset jsonAsset)
		{
			var serializedObject = new SerializedObject(asset);
			var configJson = serializedObject.FindProperty("configJson");
			var autoLoad = serializedObject.FindProperty("autoLoad");
			if (configJson == null || configJson.propertyType != SerializedPropertyType.ObjectReference
				|| autoLoad == null || autoLoad.propertyType != SerializedPropertyType.Boolean)
			{
				Debug.LogError($"SheetX: '{generatedType.FullName}' has no serialized 'configJson' TextAsset and 'autoLoad' bool.");
				return false;
			}

			configJson.objectReferenceValue = jsonAsset;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			AssetDatabase.SaveAssetIfDirty(asset);

			if (!autoLoad.boolValue)
				return true;

			// Reflection, not an interface: the generated type lives in the consuming project's assembly
			// and SheetX has no reference to it.
			var load = generatedType.GetMethod("Load", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
			if (load == null)
			{
				Debug.LogError($"SheetX: '{generatedType.FullName}' has no public Load() method.");
				return false;
			}
			try
			{
				load.Invoke(asset, null);
			}
			catch (Exception ex)
			{
				Debug.LogError($"SheetX: '{generatedType.FullName}'.Load() failed: {(ex.InnerException ?? ex).Message}", asset);
				return false;
			}
			return true;
		}

		private static ScriptableObject CreateAsset(Type generatedType, string scriptFolder)
		{
			string folder = (scriptFolder ?? "").Replace('\\', '/').TrimEnd('/');
			if (folder.Length == 0 || !AssetDatabase.IsValidFolder(folder))
			{
				Debug.LogError($"SheetX: cannot create '{generatedType.FullName}' asset — '{scriptFolder}' is not a project folder.");
				return null;
			}

			var asset = ScriptableObject.CreateInstance(generatedType);
			var serializedObject = new SerializedObject(asset);
			var autoLoad = serializedObject.FindProperty("autoLoad");
			if (autoLoad != null && autoLoad.propertyType == SerializedPropertyType.Boolean)
			{
				autoLoad.boolValue = true;
				serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}
			AssetDatabase.CreateAsset(asset, $"{folder}/{generatedType.Name}.asset");
			return asset;
		}

		private static List<ScriptableObject> FindAssetsOfExactType(Type generatedType)
		{
			// FindAssets matches by type name, which can also hit a same-named type from another assembly.
			var result = new List<ScriptableObject>();
			foreach (string guid in AssetDatabase.FindAssets($"t:{generatedType.Name}"))
			{
				var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as ScriptableObject;
				if (asset != null && asset.GetType() == generatedType)
					result.Add(asset);
			}
			result.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
			return result;
		}

		private static Type FindType(string fullTypeName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try { types = assembly.GetTypes(); }
				catch (ReflectionTypeLoadException ex) { types = ex.Types; }

				foreach (var type in types)
				{
					if (type != null
						&& string.Equals(type.FullName, fullTypeName, StringComparison.Ordinal)
						&& !type.IsAbstract
						&& typeof(ScriptableObject).IsAssignableFrom(type))
						return type;
				}
			}
			return null;
		}

		private static PendingConfigAssetStore LoadStore(out bool malformed)
		{
			malformed = false;
			string raw = SessionState.GetString(PendingKey, "");
			if (string.IsNullOrEmpty(raw))
				return new PendingConfigAssetStore();
			try
			{
				var store = JsonUtility.FromJson<PendingConfigAssetStore>(raw);
				if (store?.Entries != null)
					return store;
			}
			catch (ArgumentException) { }
			malformed = true;
			return new PendingConfigAssetStore();
		}

		private static void SaveStore(PendingConfigAssetStore store)
		{
			SessionState.SetString(PendingKey, JsonUtility.ToJson(store));
		}
	}
}
