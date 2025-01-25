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
	public static class SheetXConstants
	{
#if !SX_LOCALIZATION
		public const string APPLICATION_NAME = "SheetX - Sheets Exporter";
#else
		public const string APPLICATION_NAME = "LocalizationX - Localization Exporter";
#endif
		public const string CONSTANTS_CS_TEMPLATE = "ConstantsTemplate";
		public const string IDS_CS_TEMPLATE = "IDsTemplate";
		public const string LOCALIZATION_MANAGER_TEMPLATE = "LocalizationsManagerTemplate";
		public const string LOCALIZATION_TEMPLATE = "LocalizationTemplateV2";
		public const string LOCALIZATION_TEXT_TEMPLATE = "LocalizationTextTemplate";
		public const string IDS_SHEET = "IDs";
		public const string CONSTANTS_SHEET = "Constants";
		public const string SETTINGS_SHEET = "Settings";
		public const string LOCALIZATION_SHEET = "Localization";
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

	public class SheetXSettings : ScriptableObject
	{
#if ASSETS_STORE && SX_LOCALIZATION
		private const string FILE_PATH = "Assets/LocalizationX/Editor/SheetXSettings.asset";
#elif ASSETS_STORE && !SX_LOCALIZATION
		private const string FILE_PATH = "Assets/SheetX/Editor/SheetXSettings.asset";
#else
		private const string FILE_PATH = "Assets/RCore.SheetX/Editor/SheetXSettings.asset";
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
		public bool onlyEnumAsIDs;
		public string persistentFields;
		public string langCharSets;
		public string googleClientId;
		public string googleClientSecret;
		[HideInInspector] public bool encryptJson;
		[HideInInspector] public string encryptionKey;
		private Encryption m_encryption;

		public static SheetXSettings Init()
		{
			var settings = AssetDatabase.LoadAssetAtPath(FILE_PATH, typeof(SheetXSettings)) as SheetXSettings;
			if (settings != null)
				return settings;
			string[] guids = AssetDatabase.FindAssets($"t:SheetXSettings");
			var assets = guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>).ToArray();
			if (assets.Length > 0)
			{
				settings = assets[0] as SheetXSettings;
				return settings;
			}
			settings = EditorHelper.CreateScriptableAsset<SheetXSettings>(FILE_PATH);
			settings.ResetToDefault();
			return settings;
		}

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
			onlyEnumAsIDs = false;
			persistentFields = "id, key";
			langCharSets = "jp, ko, cn";
			googleClientId = "";
			googleClientSecret = "";
			encryptJson = false;
			encryptionKey =
				"168, 220, 184, 133, 78, 149, 8, 249, 171, 138, 98, 170, 95, 15, 211, 200, 51, 242, 4, 193, 219, 181, 232, 99, 16, 240, 142, 128, 29, 163, 245, 24, 204, 73, 173, 32, 214, 76, 31, 99, 91, 239, 232, 53, 138, 195, 93, 195, 185, 210, 155, 184, 243, 216, 204, 42, 138, 101, 100, 241, 46, 145, 198, 66, 11, 17, 19, 86, 157, 27, 132, 201, 246, 112, 121, 7, 195, 148, 143, 125, 158, 29, 184, 67, 187, 100, 31, 129, 64, 130, 26, 67, 240, 128, 233, 129, 63, 169, 5, 211, 248, 200, 199, 96, 54, 128, 111, 147, 100, 6, 185, 0, 188, 143, 25, 103, 211, 18, 17, 249, 106, 54, 162, 188, 25, 34, 147, 3, 222, 61, 218, 49, 164, 165, 133, 12, 65, 92, 48, 40, 129, 76, 194, 229, 109, 76, 150, 203, 251, 62, 54, 251, 70, 224, 162, 167, 183, 78, 103, 28, 67, 183, 23, 80, 156, 97, 83, 164, 24, 183, 81, 56, 103, 77, 112, 248, 4, 168, 5, 72, 109, 18, 75, 219, 99, 181, 160, 76, 65, 16, 41, 175, 87, 195, 181, 19, 165, 172, 138, 172, 84, 40, 167, 97, 214, 90, 26, 124, 0, 166, 217, 97, 246, 117, 237, 99, 46, 15, 141, 69, 4, 245, 98, 73, 3, 8, 161, 98, 79, 161, 127, 19, 55, 158, 139, 247, 39, 59, 72, 161, 82, 158, 25, 65, 107, 173, 5, 255, 53, 28, 179, 182, 65, 162, 17";
		}

		public string[] GetPersistentFields()
		{
			string[] splits = { ",", ";" };
			string[] result = persistentFields.Split(splits, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
			return result;
		}

		public Encryption GetEncryption()
		{
			m_encryption ??= SheetXHelper.CreateEncryption(encryptionKey);
			return m_encryption ?? Encryption.Singleton;
		}

		public void CreateFileIDs(string pFileName, string pContent)
		{
			if (string.IsNullOrEmpty(pContent))
				return;
			string fileContent = Resources.Load<TextAsset>(SheetXConstants.IDS_CS_TEMPLATE).text;
			fileContent = fileContent.Replace("_IDS_CLASS_NAME_", pFileName);
			fileContent = fileContent.Replace("public const int _FIELDS_ = 0;", pContent);
			fileContent = SheetXHelper.AddNamespace(fileContent, @namespace);

			SheetXHelper.WriteFile(constantsOutputFolder, $"{pFileName}.cs", fileContent);
			UnityEngine.Debug.Log($"Exported {pFileName}.cs!");
		}

		public void CreateFileConstants(string pContent, string pFileName)
		{
			if (string.IsNullOrEmpty(pContent))
				return;
			string fileContent = Resources.Load<TextAsset>(SheetXConstants.CONSTANTS_CS_TEMPLATE).text;
			fileContent = fileContent.Replace("_CONST_CLASS_NAME_", pFileName);
			fileContent = fileContent.Replace("public const int _FIELDS_ = 0;", pContent);
			fileContent = SheetXHelper.AddNamespace(fileContent, @namespace);

			SheetXHelper.WriteFile(constantsOutputFolder, pFileName + ".cs", fileContent);
			UnityEngine.Debug.Log($"Exported {pFileName}.cs!");
		}

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

		private string m_obfGoogleClientId;
		public string ObfGoogleClientId
		{
			get
			{
				try
				{
					m_obfGoogleClientId = GetEncryption().Decrypt(googleClientId);
				}
				catch
				{
					m_obfGoogleClientId = "";
				}
				return m_obfGoogleClientId;
			}
			set
			{
				if (value == m_obfGoogleClientId)
					return;
				m_obfGoogleClientId = value;
				googleClientId = string.IsNullOrEmpty(value) ? "" : GetEncryption().Encrypt(value);
			}
		}

		private string m_obfGoogleClientSecret;
		public string ObfGoogleClientSecret
		{
			get
			{
				try
				{
					m_obfGoogleClientSecret = GetEncryption().Decrypt(googleClientSecret);
				}
				catch
				{
					m_obfGoogleClientSecret = "";
				}
				return m_obfGoogleClientSecret;
			}
			set
			{
				if (value == m_obfGoogleClientSecret)
					return;
				m_obfGoogleClientSecret = value;
				googleClientSecret = string.IsNullOrEmpty(value) ? "" : GetEncryption().Encrypt(value);
			}
		}
	}
}