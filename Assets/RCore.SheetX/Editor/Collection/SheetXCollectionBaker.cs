/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	[Serializable]
	internal sealed class PendingCollectionBakeBinding
	{
		public string SourceId;
		public string SheetName;
	}

	[Serializable]
	internal sealed class PendingCollectionBakeEntry
	{
		public string SettingsAssetPath;
		public bool AutoLoadAfterExport;
		public bool HasAcceptedBindingFilter;
		public List<PendingCollectionBakeBinding> AcceptedBindings = new List<PendingCollectionBakeBinding>();
	}

	[Serializable]
	internal sealed class PendingCollectionBakeStore
	{
		public List<PendingCollectionBakeEntry> Entries = new List<PendingCollectionBakeEntry>();
	}

	/// <summary>
	/// Bakes editor-only collection JSON into serialized assets after generated collection types compile.
	/// </summary>
	internal static class SheetXCollectionBaker
	{
		internal const string PendingKey = "SheetX.PendingCollectionBakes";
		internal static Action<string> TestBeforeSave;

		private sealed class Table
		{
			internal SheetXSheetBinding Binding;
			internal Type RowType;
			internal Array Rows;
			internal string Json;
			internal string JsonPath;
		}

		private sealed class Collection
		{
			internal string Name;
			internal bool AutoLoad;
			internal Type Type;
			internal List<Table> Tables = new List<Table>();
		}

		private sealed class Configuration
		{
			internal string Json;
			internal string JsonPath;
		}

		/// <summary>
		/// Records one export for post-compilation bake, preserving its Auto Load intent.
		/// </summary>
		internal static void RegisterPendingBake(
			SheetXSettings settings,
			bool autoLoadAfterExport,
			IReadOnlyList<PendingCollectionBakeBinding> acceptedBindings)
		{
			string settingsPath = settings == null ? "" : AssetDatabase.GetAssetPath(settings);
			if (string.IsNullOrEmpty(settingsPath))
				return;

			var store = LoadPending();
			var entry = store.Entries.FirstOrDefault(candidate => string.Equals(
				candidate?.SettingsAssetPath, settingsPath, StringComparison.Ordinal));
			if (entry == null)
			{
				entry = new PendingCollectionBakeEntry { SettingsAssetPath = settingsPath };
				store.Entries.Add(entry);
			}
			entry.AutoLoadAfterExport = autoLoadAfterExport;
			entry.HasAcceptedBindingFilter = acceptedBindings != null;
			entry.AcceptedBindings = CopyBindings(acceptedBindings);
			SavePending(store);
		}

		[DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			var store = LoadPending();
			if (store.Entries.Count == 0)
				return;

			var remaining = new List<PendingCollectionBakeEntry>();
			foreach (var entry in store.Entries)
			{
				if (entry == null || string.IsNullOrEmpty(entry.SettingsAssetPath))
					continue;

				var settings = AssetDatabase.LoadAssetAtPath<SheetXSettings>(entry.SettingsAssetPath);
				if (settings == null)
				{
					Debug.LogError($"SheetX: pending collection bake settings were not found at '{entry.SettingsAssetPath}'.");
					continue;
				}

				if (TryFinishPendingBake(
					settings, entry.AutoLoadAfterExport, PendingBindings(entry), out string error))
					continue;

				if (error.IndexOf("was not found", StringComparison.Ordinal) >= 0)
				{
					Debug.LogError($"SheetX: Pending bake: compilation failed. {error}");
					remaining.Add(entry);
				}
				else
				{
					Debug.LogError(error);
				}
			}

			if (remaining.Count == 0)
				SessionState.EraseString(PendingKey);
			else
				SavePending(new PendingCollectionBakeStore { Entries = remaining });
		}

		/// <summary>
		/// Loads selected collection JSON and commits every affected asset as one rollback-capable transaction.
		/// </summary>
		internal static bool TryLoadData(SheetXSettings settings, bool autoLoadOnly, out string error)
			=> TryLoadData(settings, autoLoadOnly, null, null, out error);

		internal static bool TryLoadData(
			SheetXSettings settings,
			bool autoLoadOnly,
			IReadOnlyList<PendingCollectionBakeBinding> acceptedBindings,
			out string error)
			=> TryLoadData(settings, autoLoadOnly, null, acceptedBindings, out error);

		internal static bool TryLoadData(
			SheetXSettings settings,
			string collectionName,
			out string error)
			=> TryLoadData(settings, autoLoadOnly: false, collectionName, null, out error);

		/// <summary>
		/// Completes a post-compilation export by refreshing Global references and loading rows only when requested.
		/// </summary>
		internal static bool TryFinishPendingBake(
			SheetXSettings settings, bool autoLoadAfterExport, out string error)
			=> TryFinishPendingBake(settings, autoLoadAfterExport, null, out error);

		internal static bool TryFinishPendingBake(
			SheetXSettings settings,
			bool autoLoadAfterExport,
			IReadOnlyList<PendingCollectionBakeBinding> acceptedBindings,
			out string error)
			=> TryLoadData(
				settings, autoLoadAfterExport, null, refreshGlobalOnly: !autoLoadAfterExport,
				acceptedBindings, out error);

		private static bool TryLoadData(
			SheetXSettings settings,
			bool autoLoadOnly,
			string collectionName,
			IReadOnlyList<PendingCollectionBakeBinding> acceptedBindings,
			out string error)
			=> TryLoadData(
				settings, autoLoadOnly, collectionName, refreshGlobalOnly: false,
				acceptedBindings, out error);

		private static bool TryLoadData(
			SheetXSettings settings,
			bool autoLoadOnly,
			string collectionName,
			bool refreshGlobalOnly,
			IReadOnlyList<PendingCollectionBakeBinding> acceptedBindings,
			out string error)
		{
			error = null;
			if (settings == null)
			{
				error = "[SheetX Collections] - / - / -:\nSettings asset is missing.\nPath: -";
				return false;
			}

			var selectedBindings = SelectBindings(settings, acceptedBindings);
			var issues = SheetXCollectionSettings.Validate(
				settings, acceptedBindings == null ? null : selectedBindings);
			if (issues.Count > 0)
			{
				error = string.Join("\n", issues.Select(issue => issue.Message));
				return false;
			}

			if (!TryBuildCollections(settings, selectedBindings, out var collections, out error))
				return false;
			if (!string.IsNullOrEmpty(collectionName)
				&& !collections.Any(collection => string.Equals(
					collection.Name, collectionName, StringComparison.Ordinal)))
			{
				error = $"[SheetX Collections] {collectionName} / - / -:\nCollection is not defined.\nPath: -";
				return false;
			}
			if (collections.Count == 0)
				return true;
		if (!TryReadConfiguration(settings, collections, out var configuration, out error))
			return false;
		if (!refreshGlobalOnly
			&& !TryReadTables(settings, collections, autoLoadOnly, collectionName, out error))
		{
			return false;
		}

			var snapshots = new Dictionary<ScriptableObject, string>();
			var createdPaths = new List<string>();
			try
			{
				var assets = CreateOrLoadAssets(settings, collections, snapshots, createdPaths, out var global, out error);
				if (assets == null)
					throw new InvalidOperationException(error);

				if (!refreshGlobalOnly)
				{
					if (configuration != null)
						JsonConvert.PopulateObject(configuration.Json, global);

					foreach (var collection in collections)
					{
						if (autoLoadOnly && !collection.AutoLoad
							|| !string.IsNullOrEmpty(collectionName)
								&& !string.Equals(collection.Name, collectionName, StringComparison.Ordinal))
						{
							continue;
						}
						if (!ApplyRows(assets[collection.Name], collection, out error))
							throw new InvalidOperationException(error);
					}
				}
				if (!ApplyGlobalReferences(global, collections, assets, out error))
					throw new InvalidOperationException(error);

				var changedAssets = new[] { global }
					.Concat(assets.Values.Where(asset => asset != global)).ToList();
				foreach (var asset in changedAssets)
					EditorUtility.SetDirty(asset);
				foreach (var asset in changedAssets)
				{
					TestBeforeSave?.Invoke(AssetDatabase.GetAssetPath(asset));
					AssetDatabase.SaveAssetIfDirty(asset);
				}
				if (!refreshGlobalOnly)
				{
					foreach (var collection in collections)
					{
						if ((!autoLoadOnly || collection.AutoLoad)
							&& (string.IsNullOrEmpty(collectionName)
								|| string.Equals(collection.Name, collectionName, StringComparison.Ordinal)))
						{
							assets[collection.Name].SetLoaded();
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				Rollback(snapshots, createdPaths);
				error = $"[SheetX Collections] - / - / -:\n{(ex.InnerException ?? ex).Message}\nPath: -";
				return false;
			}
		}

		private static bool TryBuildCollections(
			SheetXSettings settings,
			IReadOnlyList<SheetXSheetBinding> bindings,
			out List<Collection> collections,
			out string error)
		{
			collections = new List<Collection>();
			error = null;
			foreach (var definition in settings.collections)
			{
				if (!TryFindCollectionType(settings, definition.name, out var collectionType, out error))
					return false;
				collections.Add(new Collection
				{
					Name = definition.name,
					AutoLoad = definition.autoLoad,
					Type = collectionType,
				});
			}

			foreach (var binding in bindings.Where(binding => binding.outputMode != SheetXSheetOutputMode.JsonOnly))
			{
				string collectionName = string.IsNullOrEmpty(binding.collectionName)
					? SheetXCollectionSettings.GlobalName : binding.collectionName;
				var collection = collections.First(candidate =>
					string.Equals(candidate.Name, collectionName, StringComparison.Ordinal));
				collection.Tables.Add(new Table { Binding = binding });
			}
			return true;
		}

		private static bool TryReadTables(
			SheetXSettings settings,
			IEnumerable<Collection> collections,
			bool autoLoadOnly,
			string collectionName,
			out string error)
		{
			error = null;
			foreach (var collection in collections)
			{
				if (autoLoadOnly && !collection.AutoLoad
					|| !string.IsNullOrEmpty(collectionName)
						&& !string.Equals(collection.Name, collectionName, StringComparison.Ordinal))
				{
					continue;
				}
				foreach (var table in collection.Tables)
				{
					table.JsonPath = SheetXCollectionGenerator.JsonPathFor(settings, table.Binding.sheetName);
					if (!TryFindRowType(settings, table.Binding, out table.RowType, out error))
						return false;
					try
					{
						table.Json = File.ReadAllText(table.JsonPath);
						if (!(JToken.Parse(table.Json) is JArray))
						{
							error = TableError(table, "JSON root must be an array.");
							return false;
						}
						table.Rows = JsonConvert.DeserializeObject(table.Json, table.RowType.MakeArrayType()) as Array;
						if (table.Rows == null)
						{
							error = TableError(table, "JSON did not deserialize into a row array.");
							return false;
						}
					}
					catch (Exception ex)
					{
						error = TableError(table, ex.Message);
						return false;
					}
				}
			}
			return true;
		}

		private static bool TryReadConfiguration(
			SheetXSettings settings,
			IReadOnlyList<Collection> collections,
			out Configuration configuration,
			out string error)
		{
			configuration = null;
			error = null;
			var global = collections.FirstOrDefault(collection => IsGlobal(collection.Name));
			if (global == null)
				return true;

			string typeName = settings.ResolveCollectionNamespace().Trim() + ".SheetXCollectionPaths";
			Type pathsType = FindType(typeName);
			if (pathsType == null)
				return true;

			FieldInfo marker = pathsType.GetField("Configuration",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (marker == null)
				return true;
			if (!marker.IsLiteral || marker.IsInitOnly || marker.FieldType != typeof(string))
			{
				error = $"[SheetX Collections] Global / - / Configuration:\nMarker '{typeName}.Configuration' must be an internal const string.\nPath: -\nFix: re-export Collections.";
				return false;
			}

			string path = marker.GetRawConstantValue() as string;
			if (string.IsNullOrWhiteSpace(path) || !SheetXCollectionSettings.IsProjectPath(path))
			{
				error = $"[SheetX Collections] Global / - / Configuration:\nMarker '{typeName}.Configuration' has invalid path '{path}'.\nPath: {path}\nFix: re-export Collections.";
				return false;
			}
			if (!File.Exists(path))
			{
				error = $"[SheetX Collections] Global / - / Configuration:\nConfiguration JSON was not found.\nPath: {path}\nFix: export Configuration before baking Collections.";
				return false;
			}

			string json;
			try
			{
				json = File.ReadAllText(path);
				if (!(JToken.Parse(json) is JObject))
				{
					error = $"[SheetX Collections] Global / - / Configuration:\nConfiguration JSON root must be an object.\nPath: {path}\nFix: export a valid Configuration sheet.";
					return false;
				}

				var transient = ScriptableObject.CreateInstance(global.Type);
				try
				{
					JsonConvert.PopulateObject(json, transient);
				}
				finally
				{
					UnityEngine.Object.DestroyImmediate(transient);
				}
			}
			catch (Exception ex)
			{
				error = $"[SheetX Collections] Global / - / Configuration:\n{ex.Message}\nPath: {path}\nFix: export valid Configuration JSON.";
				return false;
			}

			configuration = new Configuration { Json = json, JsonPath = path };
			return true;
		}

		private static Dictionary<string, SheetXConfigCollectionBase> CreateOrLoadAssets(
			SheetXSettings settings,
			IEnumerable<Collection> collections,
			IDictionary<ScriptableObject, string> snapshots,
			ICollection<string> createdPaths,
			out GlobalConfigCollectionBase global,
			out string error)
		{
			error = null;
			global = null;
			var assets = new Dictionary<string, SheetXConfigCollectionBase>(StringComparer.Ordinal);
			foreach (var collection in collections.Where(collection => !IsGlobal(collection.Name)))
			{
				string path = SheetXCollectionSettings.NormalizePath(settings.ResolveCollectionAssetFolder())
					+ "/" + collection.Type.Name + ".asset";
				if (!TryLoadOrCreate(path, collection.Type, snapshots, createdPaths, out var asset, out error))
					return null;
				assets.Add(collection.Name, asset);
			}

			var globalCollection = collections.First(collection => IsGlobal(collection.Name));
			string globalPath = SheetXCollectionSettings.NormalizePath(settings.ResolveGlobalResourcesFolder())
				+ "/" + globalCollection.Type.Name + ".asset";
			if (!TryLoadOrCreate(globalPath, globalCollection.Type, snapshots, createdPaths, out var globalAsset, out error))
				return null;
			global = globalAsset as GlobalConfigCollectionBase;
			if (global == null)
			{
				error = $"Global type '{globalCollection.Type.FullName}' must derive from GlobalConfigCollectionBase.";
				return null;
			}
			assets[globalCollection.Name] = global;
			return assets;
		}

		private static bool TryLoadOrCreate(
			string path,
			Type type,
			IDictionary<ScriptableObject, string> snapshots,
			ICollection<string> createdPaths,
			out SheetXConfigCollectionBase asset,
			out string error)
		{
			error = null;
			asset = AssetDatabase.LoadAssetAtPath<SheetXConfigCollectionBase>(path);
			if (asset != null)
			{
				if (asset.GetType() != type)
				{
					error = $"Asset '{path}' is '{asset.GetType().FullName}', not '{type.FullName}'.";
					return false;
				}
				snapshots.Add(asset, EditorJsonUtility.ToJson(asset));
				return TryEnsureMonoScript(asset, type, path, out error);
			}
			if (AssetDatabase.LoadMainAssetAtPath(path) != null)
			{
				error = $"Asset '{path}' exists but cannot load as '{type.FullName}'. SheetX will not overwrite it. "
					+ "Fix or remove the incompatible asset, then retry.";
				return false;
			}
			if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(path)?.Replace('\\', '/')))
			{
				error = $"Cannot create asset at '{path}': folder does not exist.";
				return false;
			}

			asset = ScriptableObject.CreateInstance(type) as SheetXConfigCollectionBase;
			if (asset == null)
			{
				error = $"Type '{type.FullName}' must derive from SheetXConfigCollectionBase.";
				return false;
			}
			AssetDatabase.CreateAsset(asset, path);
			createdPaths.Add(path);
			if (!TryEnsureMonoScript(asset, type, path, out error))
				return false;
			AssetDatabase.SaveAssetIfDirty(asset);
			return true;
		}

		private static bool TryEnsureMonoScript(
			SheetXConfigCollectionBase asset, Type type, string path, out string error)
		{
			error = null;
			var serializedObject = new SerializedObject(asset);
			var scriptProperty = serializedObject.FindProperty("m_Script");
			if (scriptProperty == null)
			{
				error = $"Asset '{path}' has no serialized m_Script property for collection type '{type.FullName}'.";
				return false;
			}

			MonoScript script = MonoScript.FromScriptableObject(asset);
			if (script == null || script.GetClass() != type)
				script = MonoImporter.GetAllRuntimeMonoScripts().FirstOrDefault(candidate => candidate.GetClass() == type);
			if (script == null || script.GetClass() != type)
			{
				error = $"Collection type '{type.FullName}' has no matching MonoScript for asset '{path}'. "
					+ $"Expected source file '{type.Name}.cs'. Re-export collections, wait for compilation, then retry.";
				return false;
			}

			if (scriptProperty.objectReferenceValue as MonoScript == script)
				return true;

			scriptProperty.objectReferenceValue = script;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			return true;
		}

		private static bool ApplyRows(SheetXConfigCollectionBase asset, Collection collection, out string error)
		{
			error = null;
			var serializedObject = new SerializedObject(asset);
			foreach (var table in collection.Tables)
			{
				var property = serializedObject.FindProperty(SheetXCollectionSettings.ResolveFieldName(table.Binding));
				if (property == null || !property.isArray || property.propertyType == SerializedPropertyType.String)
				{
					error = TableError(table, "Collection field is missing or is not an array.");
					return false;
				}
				var field = collection.Type.GetField(
					SheetXCollectionSettings.ResolveFieldName(table.Binding),
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (field == null || !field.FieldType.IsArray
					|| field.FieldType.GetElementType() != table.RowType)
				{
					error = TableError(table, "Collection field array type does not match the selected row type.");
					return false;
				}
				JsonConvert.PopulateObject(
					"{\"" + field.Name + "\":" + table.Json + "}", asset);
				serializedObject.UpdateIfRequiredOrScript();
			}
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			return true;
		}

		private static bool ApplyGlobalReferences(
			GlobalConfigCollectionBase global,
			IEnumerable<Collection> collections,
			IReadOnlyDictionary<string, SheetXConfigCollectionBase> assets,
			out string error)
		{
			error = null;
			var serializedObject = new SerializedObject(global);
			foreach (var collection in collections.Where(collection => !IsGlobal(collection.Name)))
			{
				string fieldName = SheetXCollectionNaming.ToCamelIdentifier(collection.Name);
				var property = serializedObject.FindProperty(fieldName);
				if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
				{
					error = $"Global collection has no feature reference '{fieldName}'.";
					return false;
				}

				var featureAsset = assets[collection.Name];
				var field = global.GetType().GetField(
					fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (field == null || !field.FieldType.IsAssignableFrom(featureAsset.GetType()))
				{
					error = $"Global collection feature reference '{fieldName}' has an incompatible type.";
					return false;
				}

				property.objectReferenceValue = featureAsset;
			}
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			foreach (var collection in collections.Where(collection => !IsGlobal(collection.Name)))
			{
				string fieldName = SheetXCollectionNaming.ToCamelIdentifier(collection.Name);
				var field = global.GetType().GetField(
					fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				field.SetValue(global, assets[collection.Name]);
			}
			return true;
		}

		private static void Rollback(
			IReadOnlyDictionary<ScriptableObject, string> snapshots,
			IEnumerable<string> createdPaths)
		{
			foreach (var snapshot in snapshots)
			{
				if (snapshot.Key == null)
					continue;
				EditorJsonUtility.FromJsonOverwrite(snapshot.Value, snapshot.Key);
				EditorUtility.SetDirty(snapshot.Key);
				AssetDatabase.SaveAssetIfDirty(snapshot.Key);
			}
			foreach (string path in createdPaths.Reverse())
				AssetDatabase.DeleteAsset(path);
		}

		private static bool TryFindCollectionType(
			SheetXSettings settings, string collectionName, out Type type, out string error)
		{
			error = null;
			string collectionNamespace = settings.ResolveCollectionNamespace();
			string name = string.IsNullOrEmpty(collectionNamespace)
				? SheetXCollectionNaming.CollectionTypeName(collectionName)
				: collectionNamespace.Trim() + "." + SheetXCollectionNaming.CollectionTypeName(collectionName);
			type = FindType(name);
			if (type == null)
			{
				error = $"Collection type '{name}' was not found after reload.";
				return false;
			}
			if (!typeof(SheetXConfigCollectionBase).IsAssignableFrom(type) || type.IsAbstract)
			{
				error = $"Collection type '{name}' must be a concrete SheetXConfigCollectionBase.";
				type = null;
				return false;
			}
			return true;
		}

		private static bool TryFindRowType(
			SheetXSettings settings, SheetXSheetBinding binding, out Type type, out string error)
		{
			error = null;
			bool generated = binding.outputMode == SheetXSheetOutputMode.GeneratedDataClass;
			string name = generated
				? settings.ResolveCollectionNamespace().Trim() + "." + SheetXCollectionNaming.RowTypeName(binding.sheetName)
				: binding.rowTypeName;
			type = Type.GetType(name, throwOnError: false) ?? FindType(name);
			if (type == null)
			{
				error = $"Row type '{name}' was not found after reload.";
				return false;
			}
			// Generated rows are emitted by SheetX itself and carry no [SheetXBindable].
			if (!generated && !SheetXRowType.Validate(type, out error))
			{
				type = null;
				return false;
			}
			return true;
		}

		private static Type FindType(string fullName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try { types = assembly.GetTypes(); }
				catch (ReflectionTypeLoadException ex) { types = ex.Types; }
				foreach (var type in types)
				{
					if (type != null && (string.Equals(type.FullName, fullName, StringComparison.Ordinal)
						|| string.Equals(type.AssemblyQualifiedName, fullName, StringComparison.Ordinal)))
						return type;
				}
			}
			return null;
		}

		private static List<SheetXSheetBinding> SelectBindings(
			SheetXSettings settings,
			IReadOnlyList<PendingCollectionBakeBinding> acceptedBindings)
		{
			var bindings = SheetXCollectionSettings.FilterActiveBindings(settings, settings.sheetBindings)
				.Where(binding => binding.outputMode != SheetXSheetOutputMode.JsonOnly);
			if (acceptedBindings == null)
				return bindings.ToList();

			var accepted = new HashSet<string>(acceptedBindings
				.Where(binding => binding != null)
				.Select(BindingKey), StringComparer.Ordinal);
			return bindings.Where(binding => accepted.Contains(BindingKey(binding))).ToList();
		}

		private static string BindingKey(PendingCollectionBakeBinding binding)
			=> binding.SourceId + "\0" + binding.SheetName;

		private static string BindingKey(SheetXSheetBinding binding)
			=> binding.sourceId + "\0" + binding.sheetName;

		private static List<PendingCollectionBakeBinding> CopyBindings(
			IReadOnlyList<PendingCollectionBakeBinding> bindings)
		{
			return bindings?.Where(binding => binding != null).Select(binding =>
				new PendingCollectionBakeBinding
				{
					SourceId = binding.SourceId,
					SheetName = binding.SheetName,
				}).ToList() ?? new List<PendingCollectionBakeBinding>();
		}

		private static IReadOnlyList<PendingCollectionBakeBinding> PendingBindings(
			PendingCollectionBakeEntry entry)
		{
			if (entry == null || !entry.HasAcceptedBindingFilter)
				return null;
			return entry.AcceptedBindings ?? new List<PendingCollectionBakeBinding>();
		}

		private static PendingCollectionBakeStore LoadPending()
		{
			string raw = SessionState.GetString(PendingKey, "");
			if (string.IsNullOrEmpty(raw))
				return new PendingCollectionBakeStore();
			try
			{
				return JsonUtility.FromJson<PendingCollectionBakeStore>(raw)
					?? new PendingCollectionBakeStore();
			}
			catch (ArgumentException)
			{
				return new PendingCollectionBakeStore();
			}
		}

		private static void SavePending(PendingCollectionBakeStore store)
		{
			SessionState.SetString(PendingKey, JsonUtility.ToJson(store));
		}

		private static bool IsGlobal(string collectionName)
			=> string.Equals(collectionName, SheetXCollectionSettings.GlobalName, StringComparison.Ordinal);

		private static string TableError(Table table, string cause)
		{
			return $"[SheetX Collections] {table.Binding.collectionName} / {table.Binding.sourceId} / {table.Binding.sheetName}:\n{cause}\nPath: {table.JsonPath}";
		}
	}
}
