using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RCore.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX
{
	public static class SheetXConstants
	{
		public const string APPLICATION_NAME = "Excel to Unity - Data Converter";
		public const string CONSTANTS_CS_TEMPLATE = "ConstantsTemplate";
		public const string IDS_CS_TEMPLATE = "IDsTemplate";
		public const string LOCALIZATION_MANAGER_TEMPLATE = "LocalizationsManagerTemplate";
		public const string LOCALIZATION_TEMPLATE = "LocalizationTemplate";
		public const string LOCALIZATION_TEMPLATE_V2 = "LocalizationTemplateV2";
		public const string LOCALIZATION_TEXT_TEMPLATE = "LocalizationTextTemplate";
		public const string SETTINGS_CS_TEMPLATE = "SettingsTemplate";
		public const string IDS_SHEET = "IDs";
		public const string CONSTANTS_SHEET = "Constants";
		public const string SETTINGS_SHEET = "Settings";
		public const string LOCALIZATION_SHEET = "Localization";
	}

	public class SheetXSettings : ScriptableObject
	{
		private const string FILE_PATH = "Assets/Editor/SheetXSettings.asset";

		public ExcelSheetsPath excelSheetsPath;
		public GoogleSheetsPath googleSheetsPath;
		public List<ExcelSheetsPath> excelSheetsPaths;
		public List<GoogleSheetsPath> googleSheetsPaths;
		public string jsonOutputFolder;
		public string constantsOutputFolder;
		public string localizationOutputFolder;
		public string @namespace;
		public bool separateConstants;
		public bool separateIDs;
		public bool separateLocalizations;
		public bool combineJson;
		public bool onlyEnumAsIDs;
		public bool encryptJson;
		public string langCharSets;
		public string persistentFields;
		public string googleClientId;
		public string googleClientSecret;
		public string encryptionKey;
		private Encryption m_encryption;

		public static SheetXSettings Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(FILE_PATH, typeof(SheetXSettings)) as SheetXSettings;
			if (collection == null)
			{
				collection = EditorHelper.CreateScriptableAsset<SheetXSettings>(FILE_PATH);
				collection.ResetToDefault();
			}
			return collection;
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
				string pathAfterResources = path.Substring(startAfterResources).TrimStart(System.IO.Path.DirectorySeparatorChar);
				return pathAfterResources;
			}
			isAddressableAsset = true;
			return "Localizations";
		}

		public void ResetToDefault()
		{
			jsonOutputFolder = "";
			constantsOutputFolder = "";
			localizationOutputFolder = "";
			@namespace = "";
			separateConstants = false;
			separateIDs = false;
			separateLocalizations = true;
			combineJson = false;
			onlyEnumAsIDs = false;
			encryptJson = false;
			langCharSets = "japan, korean, chinese";
			persistentFields = "id, key";
			googleClientId = "";
			googleClientSecret = "";
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

		public void CreateFileIDs(string exportFileName, string content)
		{
			string fileContent = Resources.Load<TextAsset>(SheetXConstants.IDS_CS_TEMPLATE).text;
			fileContent = fileContent.Replace("_IDS_CLASS_NAME_", exportFileName);
			fileContent = fileContent.Replace("public const int _FIELDS_ = 0;", content);
			fileContent = SheetXHelper.AddNamespace(fileContent, @namespace);

			SheetXHelper.WriteFile(constantsOutputFolder, $"{exportFileName}.cs", fileContent);
			UnityEngine.Debug.Log($"Exported {exportFileName}.cs!");
		}
		
		public void CreateFileConstants(string pContent, string pExportFileName)
		{
			string fileContent = Resources.Load<TextAsset>(SheetXConstants.CONSTANTS_CS_TEMPLATE).text;
			fileContent = fileContent.Replace("_CONST_CLASS_NAME_", pExportFileName);
			fileContent = fileContent.Replace("public const int _FIELDS_ = 0;", pContent);
			fileContent = SheetXHelper.AddNamespace(fileContent, @namespace);

			SheetXHelper.WriteFile(constantsOutputFolder, pExportFileName + ".cs", fileContent);
			UnityEngine.Debug.Log($"Exported {pExportFileName}.cs!");
		}

		public void AddExcelFileFile(string path)
		{
			if (!File.Exists(path))
				return;
			foreach (var _excelSheetsPath in excelSheetsPaths)
			{
				if (_excelSheetsPath.path == path)
					return;
			}
			var newPath = new ExcelSheetsPath()
			{
				path = path,
				selected = true,
			};
			newPath.Load();
			excelSheetsPaths.Add(newPath);
		}
	}
}