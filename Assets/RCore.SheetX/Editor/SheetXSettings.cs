/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Defines constants used throughout the SheetX application.
	/// </summary>
	public static class SheetXConstants
	{
		public const string APPLICATION_NAME = "SheetX - Sheets Exporter";
		public const string CONSTANTS_CS_TEMPLATE = "ConstantsTemplate";
		public const string IDS_CS_TEMPLATE = "IDsTemplate";
		public const string LOCALIZATION_MANAGER_TEMPLATE = "LocalizationsManagerTemplate";
		public const string LOCALIZATION_TEMPLATE = "LocalizationTemplateV2";
		public const string LOCALIZATION_TEXT_TEMPLATE = "LocalizationTextTemplate";
		public const string IDS_SHEET = "IDs";
		public const string CONSTANTS_SHEET = "Constants";
		public const string SETTINGS_SHEET = "Settings";
		public const string LOCALIZATION_SHEET = "Localization";
		public const string CONFIG_SHEET = "Config";
	}

	public enum ValueType
	{
		Text,
		Number,
		Bool,
		Json,
		ArrayText,
		ArrayNumber,
		ArrayBool,
	}

	/// <summary>
	/// Stores configuration settings for the SheetX exporter, including paths, flags, and encryption keys.
	/// </summary>
	public class SheetXSettings : ScriptableObject
	{
#if ASSETS_STORE
		private const string FILE_PATH = "Assets/SheetX/Editor/SheetXSettings.asset";
#else
		// Must live in the consuming project's Assets/, never inside the package. When SheetX is
		// installed via UPM git URL the package resolves into Library/PackageCache, which is
		// gitignored and rebuilt from git on every re-resolve — any settings stored there are lost.
		private const string FILE_PATH = "Assets/SheetX/SheetXSettings.asset";
#endif

		public ExcelSheetsPath excelSheetsPath;
		public List<ExcelSheetsPath> excelSheetsPaths = new List<ExcelSheetsPath>();
		public GoogleSheetsPath googleSheetsPath;
		public List<GoogleSheetsPath> googleSheetsPaths = new List<GoogleSheetsPath>();
		public string constantsOutputFolder;
		public string jsonOutputFolder;
		public string localizationOutputFolder;
		public string @namespace;
		public bool separateConstants;
		public bool separateIDs;
		public bool separateLocalizations;
		public bool combineJson;
		public bool generateConfigScriptableObject;
		public bool onlyEnumAsIDs;
		public string persistentFields;
		public string langCharSets;
		[HideInInspector, Obsolete("Legacy storage. Credentials live in EditorPrefs; this field is migration-only and is cleared on load.")]
		public string googleClientId;
		[HideInInspector, Obsolete("Legacy storage. Credentials live in EditorPrefs; this field is migration-only and is cleared on load.")]
		public string googleClientSecret;
		[HideInInspector] public bool encryptJson;
		[HideInInspector] public string encryptionKey;
		private Encryption m_encryption;
		private static bool s_warnedDefaultKey;

		// EditorPrefs is machine-global, so the key needs a project discriminator or two projects on
		// one machine overwrite each other. The discriminator is the project path, not
		// Application.identifier: the bundle identifier is a shipping setting that changes per build
		// flavor and defaults to the same "com.DefaultCompany.*" in every fresh project, so it both
		// loses credentials on an unrelated edit and collides across projects.
		private static string PrefKey(string field) => $"SheetX.{ProjectKey()}.{field}";

		// The normalized project path itself, not a hash of it: string.GetHashCode carries no
		// cross-process stability guarantee, and a hash that shifts between editor launches would
		// silently orphan the stored credentials.
		private static string ProjectKey()
		{
			return Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
				.Replace('\\', '/')
				.TrimEnd('/')
				.ToLowerInvariant();
		}

		private static string LegacyPrefKey(string field) => $"{Application.identifier}.SheetX.{field}";

		// Reads through the current key, falling back once to the retired Application.identifier key so
		// an existing install does not have to re-enter its credentials. Copy first, delete second —
		// an interrupted migration must never lose the only copy.
		private static string GetPref(string field)
		{
			string key = PrefKey(field);
			// An empty stored value is indistinguishable from an unset one for a credential, so it
			// falls through to the legacy key rather than shadowing it.
			string current = EditorPrefs.GetString(key, "");
			if (!string.IsNullOrEmpty(current))
				return current;

			string legacyKey = LegacyPrefKey(field);
			string legacy = EditorPrefs.GetString(legacyKey, "");
			if (string.IsNullOrEmpty(legacy))
				return current;

			EditorPrefs.SetString(key, legacy);
			EditorPrefs.DeleteKey(legacyKey);
			return legacy;
		}

		private static void SetPref(string field, string value)
		{
			EditorPrefs.SetString(PrefKey(field), value ?? "");
			EditorPrefs.DeleteKey(LegacyPrefKey(field));
		}

		/// <summary>
		/// Builds an in-memory settings object from one export request. Never loads, creates, or dirties
		/// the settings asset, and never reads EditorPrefs — an external caller's export must not depend
		/// on, or disturb, the consuming project's SheetX window state. Credentials stay in the request.
		/// </summary>
		internal static SheetXSettings CreateTransient(SheetXExportRequest request)
		{
			var settings = CreateBlankTransient();
			Map(
				settings,
				request.ConstantsOutputPath,
				request.JsonOutputPath,
				request.LocalizationOutputPath,
				request.Namespace,
				request.PersistentFields,
				request.SeparateConstants,
				request.SeparateIDs,
				request.SeparateLocalizations,
				request.CombineJson,
				request.OnlyEnumAsIDs,
				request.EncryptJson,
				request.EncryptionKey);
			return settings;
		}

		internal static SheetXSettings CreateTransient(SheetXBatchExportRequest request)
		{
			var settings = CreateBlankTransient();
			settings.silent = true;
			Map(
				settings,
				request.ConstantsOutputPath,
				request.JsonOutputPath,
				request.LocalizationOutputPath,
				request.Namespace,
				request.PersistentFields,
				request.SeparateConstants,
				request.SeparateIDs,
				request.SeparateLocalizations,
				request.CombineJson,
				request.OnlyEnumAsIDs,
				request.EncryptJson,
				request.EncryptionKey);
			return settings;
		}

		private static SheetXSettings CreateBlankTransient()
		{
			var settings = CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			return settings;
		}

		private static void Map(
			SheetXSettings settings,
			string constantsOutputPath,
			string jsonOutputPath,
			string localizationOutputPath,
			string @namespace,
			string persistentFields,
			bool separateConstants,
			bool separateIDs,
			bool separateLocalizations,
			bool combineJson,
			bool onlyEnumAsIDs,
			bool encryptJson,
			string encryptionKey)
		{
			settings.constantsOutputFolder = constantsOutputPath ?? "";
			settings.jsonOutputFolder = jsonOutputPath ?? "";
			settings.localizationOutputFolder = localizationOutputPath ?? "";
			settings.@namespace = @namespace ?? "";
			settings.persistentFields = persistentFields ?? "";
			settings.separateConstants = separateConstants;
			settings.separateIDs = separateIDs;
			settings.separateLocalizations = separateLocalizations;
			settings.combineJson = combineJson;
			settings.onlyEnumAsIDs = onlyEnumAsIDs;
			settings.encryptJson = encryptJson;

			if (!string.IsNullOrEmpty(encryptionKey))
				settings.encryptionKey = encryptionKey;
		}

		public static SheetXSettings Init()
		{
			var settings = AssetDatabase.LoadAssetAtPath(FILE_PATH, typeof(SheetXSettings)) as SheetXSettings;
			if (settings != null)
			{
				settings.MigrateCredentialsToEditorPrefs();
				return settings;
			}
			// Scoped to "Assets" on purpose. An unscoped FindAssets also searches Packages/, where it
			// would find the copy shipped inside the SheetX package itself — that one lives in
			// Library/PackageCache and is discarded whenever UPM re-resolves the package.
			string[] guids = AssetDatabase.FindAssets($"t:SheetXSettings", new[] { "Assets" });
			var assets = guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>).ToArray();
			if (assets.Length > 0)
			{
				settings = assets[0] as SheetXSettings;
				if (settings != null)
					settings.MigrateCredentialsToEditorPrefs();
				return settings;
			}
			settings = EditorHelper.CreateScriptableAsset<SheetXSettings>(FILE_PATH);
			settings.ResetToDefault();
			return settings;
		}

		/// <summary>
		/// Writes any in-memory edits back to the settings asset on disk. The exporter windows mutate
		/// this object directly (Excel paths, Google sheet lists, sheet selections), so without this
		/// the edits live only until the next domain reload.
		/// </summary>
		public void SaveToDisk()
		{
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssetIfDirty(this);
		}

		/// <summary>
		/// Gets the relative path to the localization folder, determining if it's within a Resources directory.
		/// </summary>
		/// <param name="isAddressableAsset">True if the path is NOT within Resources (and presumably addressable).</param>
		public string GetLocalizationFolder(out bool isAddressableAsset)
		{
			string path = localizationOutputFolder;
			string resourcesDirName = "Resources";
			isAddressableAsset = false;

			// Find the index of the Resources directory
			int resourcesIndex = path.IndexOf(resourcesDirName, StringComparison.OrdinalIgnoreCase);
			if (resourcesIndex != -1)
			{
				int startAfterResources = resourcesIndex + resourcesDirName.Length;
				string pathAfterResources = path.Substring(startAfterResources);
				// Ensure the path does not start with a directory separator
				pathAfterResources = pathAfterResources.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				return pathAfterResources;
			}
			isAddressableAsset = true;
			return "Localizations";
		}

		/// <summary>
		/// Resets all settings to their default values.
		/// </summary>
		public void ResetToDefault()
		{
			constantsOutputFolder = "";
			jsonOutputFolder = "";
			localizationOutputFolder = "";
			@namespace = "";
			separateConstants = false;
			separateIDs = false;
			separateLocalizations = true;
			combineJson = false;
			generateConfigScriptableObject = false;
			onlyEnumAsIDs = false;
			persistentFields = "id, key";
			langCharSets = "jp, ko, cn";
#pragma warning disable 618
			googleClientId = "";
			googleClientSecret = "";
#pragma warning restore 618
			encryptJson = false;
			encryptionKey = DEFAULT_ENCRYPTION_KEY;
		}

		// Published in this repository, so it protects nothing — GetEncryption() warns when it is
		// still in use with encryptJson on.
		private const string DEFAULT_ENCRYPTION_KEY =
			"168, 220, 184, 133, 78, 149, 8, 249, 171, 138, 98, 170, 95, 15, 211, 200, 51, 242, 4, 193, 219, 181, 232, 99, 16, 240, 142, 128, 29, 163, 245, 24, 204, 73, 173, 32, 214, 76, 31, 99, 91, 239, 232, 53, 138, 195, 93, 195, 185, 210, 155, 184, 243, 216, 204, 42, 138, 101, 100, 241, 46, 145, 198, 66, 11, 17, 19, 86, 157, 27, 132, 201, 246, 112, 121, 7, 195, 148, 143, 125, 158, 29, 184, 67, 187, 100, 31, 129, 64, 130, 26, 67, 240, 128, 233, 129, 63, 169, 5, 211, 248, 200, 199, 96, 54, 128, 111, 147, 100, 6, 185, 0, 188, 143, 25, 103, 211, 18, 17, 249, 106, 54, 162, 188, 25, 34, 147, 3, 222, 61, 218, 49, 164, 165, 133, 12, 65, 92, 48, 40, 129, 76, 194, 229, 109, 76, 150, 203, 251, 62, 54, 251, 70, 224, 162, 167, 183, 78, 103, 28, 67, 183, 23, 80, 156, 97, 83, 164, 24, 183, 81, 56, 103, 77, 112, 248, 4, 168, 5, 72, 109, 18, 75, 219, 99, 181, 160, 76, 65, 16, 41, 175, 87, 195, 181, 19, 165, 172, 138, 172, 84, 40, 167, 97, 214, 90, 26, 124, 0, 166, 217, 97, 246, 117, 237, 99, 46, 15, 141, 69, 4, 245, 98, 73, 3, 8, 161, 98, 79, 161, 127, 19, 55, 158, 139, 247, 39, 59, 72, 161, 82, 158, 25, 65, 107, 173, 5, 255, 53, 28, 179, 182, 65, 162, 17";

		/// <summary>
		/// Parses the PersistentFields string into an array of field names.
		/// </summary>
		public string[] GetPersistentFields()
		{
			string[] splits = { ",", ";" };
			string[] result = persistentFields.Split(splits, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
			return result;
		}

		/// <summary>
		/// Gets the encryption object, creating it with the configured key if necessary.
		/// </summary>
		internal bool UsesDefaultEncryptionKey => encryptionKey == DEFAULT_ENCRYPTION_KEY;

		// Set for a detached export: the console belongs to the editor windows, and a batch or CI
		// caller learns the same things from its result instead.
		internal bool silent;

		public Encryption GetEncryption()
		{
			if (!silent && encryptJson && UsesDefaultEncryptionKey && !s_warnedDefaultKey)
			{
				s_warnedDefaultKey = true;
				UnityEngine.Debug.LogWarning(
					"SheetX: encryptJson is on but encryptionKey is still the key shipped with this "
					+ "package, which is published in a public repository. Anyone can decrypt the output. "
					+ "Set your own key before shipping encrypted data.");
			}
			m_encryption ??= SheetXHelper.CreateEncryption(encryptionKey);
			return m_encryption ?? Encryption.Singleton;
		}

		/// <summary>
		/// Generates and saves a C# file containing ID constants based on the provided content.
		/// </summary>
		public void CreateFileIDs(string pFileName, string pContent) => new SheetXWriter(this, null).CreateFileIDs(pFileName, pContent);

		/// <summary>
		/// Generates and saves a C# file containing general constants based on the provided content.
		/// </summary>
		public void CreateFileConstants(string pContent, string pFileName) => new SheetXWriter(this, null).CreateFileConstants(pContent, pFileName);

		/// <summary>
		/// Adds a new Excel file to the list of tracked Excel files if it exists and isn't already added.
		/// </summary>
		public ExcelSheetsPath AddExcelFileFile(string path)
		{
			if (!File.Exists(path))
				return null;
			foreach (var _excelSheetsPath in excelSheetsPaths)
			{
				if (_excelSheetsPath.path == path)
					return null;
			}
			var newPath = new ExcelSheetsPath()
			{
				path = path,
				selected = true,
			};
			newPath.Load();
			excelSheetsPaths.Add(newPath);
			return newPath;
		}

		/// <summary>
		/// The Google OAuth client ID, stored per machine in EditorPrefs. Never written to the
		/// settings asset — a serialized field on a committed asset is a published secret.
		/// </summary>
		public string ObfGoogleClientId
		{
			get => GetPref("GoogleClientId");
			set => SetPref("GoogleClientId", value);
		}

		/// <summary>
		/// The Google OAuth client secret, stored per machine in EditorPrefs. See <see cref="ObfGoogleClientId"/>.
		/// </summary>
		public string ObfGoogleClientSecret
		{
			get => GetPref("GoogleClientSecret");
			set => SetPref("GoogleClientSecret", value);
		}

		/// <summary>
		/// Moves credentials from the legacy encrypted serialized fields into EditorPrefs, then
		/// clears them from the asset. No-op once the fields are empty. A field whose decryption
		/// does not yield a plausible credential is left in place, not blanked.
		/// </summary>
		internal void MigrateCredentialsToEditorPrefs()
		{
#pragma warning disable 618
			if (string.IsNullOrEmpty(googleClientId) && string.IsNullOrEmpty(googleClientSecret))
				return;

			// '|' not '||' — both fields must be attempted even when the first one fails.
			bool migrated = TryMigrate("GoogleClientId", ref googleClientId)
				| TryMigrate("GoogleClientSecret", ref googleClientSecret);
#pragma warning restore 618
			if (!migrated)
				return;

			// Init() runs from OnEnable handlers, i.e. during assembly reload, where an AssetDatabase
			// write is unsafe. Mark dirty and defer, same as SheetXWindow.FlushSettings().
			EditorUtility.SetDirty(this);
			var self = this;
			EditorApplication.delayCall += () =>
			{
				if (self != null)
					AssetDatabase.SaveAssetIfDirty(self);
			};
			UnityEngine.Debug.Log("SheetX: Google credentials moved to EditorPrefs and cleared from the settings asset.");
		}

		private bool TryMigrate(string field, ref string legacy)
		{
			if (string.IsNullOrEmpty(legacy))
				return false;

			string plain;
			try { plain = GetEncryption().Decrypt(legacy); }
			catch { plain = null; }

			// Decrypt only throws on malformed Base64. A wrong encryptionKey yields garbage bytes that
			// UTF8.GetString turns into U+FFFD instead of throwing, so the result must be checked, not
			// just the exception. A real OAuth credential is printable ASCII. Never blank a field whose
			// migration did not clearly succeed — the user can re-run, but cannot un-delete.
			if (string.IsNullOrEmpty(plain) || plain.Any(c => c < 0x20 || c > 0x7E))
			{
				UnityEngine.Debug.LogWarning(
					$"SheetX: could not decrypt {field} — the encryptionKey does not match the one it was "
					+ "saved with. The field has been left untouched; re-enter the credential in Settings, "
					+ "which will overwrite it.");
				return false;
			}

			SetPref(field, plain);
			legacy = "";
			return true;
		}
	}
}