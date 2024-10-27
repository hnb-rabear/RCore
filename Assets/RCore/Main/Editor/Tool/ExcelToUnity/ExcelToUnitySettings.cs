using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.Editor.Tool.ExcelToUnity
{
	[System.Serializable]
	public class ExcelFile
	{
		public string path;
		public bool selected;
		public bool exportConstants;
		public bool exportIDs;
	}

	[System.Serializable]
	public class Spreadsheet
	{
		public string name;
		public bool selected;
	}

	[System.Serializable]
	public class Excel
	{
		public string path;
		public List<Spreadsheet> sheets;
	}
	
	public class ExcelToUnitySettings : ScriptableObject
	{
		private const string FILE_PATH = "Assets/Editor/ExcelToUnitySettings.asset";

		public Excel excelFile;
		public List<ExcelFile> excelFiles;
		public string jsonOutputFolder;
		public string constantsOutputFolder;
		public string localizationOutputFolder;
		public string nameSpace;
		public bool separateConstants;
		public bool separateIDs;
		public bool separateLocalizations = true;
		public bool combineJson;
		public bool enumTypeOnlyForIDs;
		public bool encryptJson;
		public List<string> languageMaps = new() { "korea (kr)", "japan (jp)", "china (cn)"};
		public List<string> persistentFields = new List<string>() { "id", "key" };
		public List<string> excludedSheets;
		public string googleClientId;
		public string googleClientSecret;
		public string encryptionKey =
			"168, 220, 184, 133, 78, 149, 8, 249, 171, 138, 98, 170, 95, 15, 211, 200, 51, 242, 4, 193, 219, 181, 232, 99, 16, 240, 142, 128, 29, 163, 245, 24, 204, 73, 173, 32, 214, 76, 31, 99, 91, 239, 232, 53, 138, 195, 93, 195, 185, 210, 155, 184, 243, 216, 204, 42, 138, 101, 100, 241, 46, 145, 198, 66, 11, 17, 19, 86, 157, 27, 132, 201, 246, 112, 121, 7, 195, 148, 143, 125, 158, 29, 184, 67, 187, 100, 31, 129, 64, 130, 26, 67, 240, 128, 233, 129, 63, 169, 5, 211, 248, 200, 199, 96, 54, 128, 111, 147, 100, 6, 185, 0, 188, 143, 25, 103, 211, 18, 17, 249, 106, 54, 162, 188, 25, 34, 147, 3, 222, 61, 218, 49, 164, 165, 133, 12, 65, 92, 48, 40, 129, 76, 194, 229, 109, 76, 150, 203, 251, 62, 54, 251, 70, 224, 162, 167, 183, 78, 103, 28, 67, 183, 23, 80, 156, 97, 83, 164, 24, 183, 81, 56, 103, 77, 112, 248, 4, 168, 5, 72, 109, 18, 75, 219, 99, 181, 160, 76, 65, 16, 41, 175, 87, 195, 181, 19, 165, 172, 138, 172, 84, 40, 167, 97, 214, 90, 26, 124, 0, 166, 217, 97, 246, 117, 237, 99, 46, 15, 141, 69, 4, 245, 98, 73, 3, 8, 161, 98, 79, 161, 127, 19, 55, 158, 139, 247, 39, 59, 72, 161, 82, 158, 25, 65, 107, 173, 5, 255, 53, 28, 179, 182, 65, 162, 17";
		
		public static ExcelToUnitySettings Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(FILE_PATH, typeof(ExcelToUnitySettings)) as ExcelToUnitySettings;
			if (collection == null)
				collection = EditorHelper.CreateScriptableAsset<ExcelToUnitySettings>(FILE_PATH);
			return collection;
		}
		
		public void AddExcelFileFile(string path)
		{
			if (!File.Exists(path))
				return;
			for (int i = 0; i < excelFiles.Count; i++)
			{
				if (excelFiles[i].path == path)
					return;
			}
			excelFiles.Add(new ExcelFile()
			{
				path = path,
				selected = true,
				exportConstants = true,
				exportIDs = true,
			});
		}

		public void ExportAll()
		{
			
		}

		public void ExportConstants()
		{
			
		}

		public void ExportIDs()
		{
			
		}

		public void ExportLocalizations()
		{
			
		}

		public void ExportJson()
		{
			
		}
	}
}