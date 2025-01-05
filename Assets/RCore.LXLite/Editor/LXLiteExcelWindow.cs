using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using UnityEditor;
using UnityEngine;

namespace RCore.LXLite.Editor
{
	public class LXLiteExcelWindow
	{
		private Dictionary<string, LocalizationBuilder> m_localizationsDict = new();
		private List<string> m_localizedLanguages = new();
		private Dictionary<string, string> m_langCharSets = new();
		private StringBuilder m_langCharSetsAll;

		public void OnEnable() { }

		public void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();
			LXLiteConfig.ExcelFilePath = EditorHelper.TextField(LXLiteConfig.ExcelFilePath, "Excel File Path", 150);
			if (EditorHelper.Button("Select File", 100))
			{
				string directory = string.IsNullOrEmpty(LXLiteConfig.ExcelFilePath) ? null : Path.GetDirectoryName(LXLiteConfig.ExcelFilePath);
				string path = EditorHelper.OpenFilePanel("Select File", "xlsx", directory);
				if (!string.IsNullOrEmpty(path))
				{
					if (path.StartsWith(Application.dataPath))
						path = EditorHelper.FormatPathToUnityPath(path);
					LXLiteConfig.ExcelFilePath = path;
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(5);
			if (GUILayout.Button("Export"))
			{
				if (!ValidateExcelPath(LXLiteConfig.ExcelFilePath))
				{
					Debug.LogError("Excel file path is invalid");
					return;
				}
				ExportLocalizations();
			}
		}

		private bool ValidateExcelPath(string path)
		{
			string extension = Path.GetExtension(path)?.ToLower();
			if (extension != ".xlsx")
			{
				return false;
			}
			if (!File.Exists(path))
			{
				return false;
			}
			return true;
		}

		private IWorkbook GetWorkBook(string path)
		{
			if (!File.Exists(path))
			{
				Debug.LogError($"{path} does not exist.");
				return null;
			}

			using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			return WorkbookFactory.Create(file);
		}

		private void ExportLocalizations()
		{
			if (string.IsNullOrEmpty(LXLiteConfig.ScriptsOutputFolder))
			{
				Debug.LogError("Please setup the Constants Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(LXLiteConfig.LocalizationOutputFolder))
			{
				Debug.LogError("Please setup the Localization Output folder!");
				return;
			}

			var workBook = GetWorkBook(LXLiteConfig.ExcelFilePath);
			if (workBook == null)
				return;

			m_localizationsDict = new Dictionary<string, LocalizationBuilder>();
			m_localizedLanguages = new List<string>();
			m_langCharSets = new Dictionary<string, string>();
			m_langCharSetsAll = new StringBuilder();

			for (int i = 0; i < workBook.NumberOfSheets; i++)
			{
				var sheet = workBook.GetSheetAt(i);
				if (!sheet.SheetName.StartsWith(LXLiteConfig.LOCALIZATION_SHEET))
					continue;

				LoadSheetLocalizationData(workBook, sheet.SheetName);
			}

			var builder = new LocalizationBuilder();
			foreach (var b in m_localizationsDict)
			{
				builder.idsString.AddRange(b.Value.idsString);
				foreach (var t in b.Value.languageTextDict)
				{
					var language = t.Key;
					var texts = t.Value;
					if (!builder.languageTextDict.ContainsKey(language))
						builder.languageTextDict.Add(language, new List<string>());
					builder.languageTextDict[language].AddRange(texts);
				}
			}
			CreateLocalizationFile(builder.idsString, builder.languageTextDict, "Localization");

			//Create language character sets
			if (m_langCharSets != null && m_langCharSets.Count > 0)
			{
				var maps = LXLiteHelper.GenerateCharacterSets(m_langCharSets);
				foreach (var map in maps)
				{
					LXLiteHelper.WriteFile(LXLiteConfig.LocalizationOutputFolder, $"characters_set_{map.Key}.txt", map.Value);
					Debug.Log($"Exported characters_set_{map.Key}.txt!");
				}
			}
			if (!string.IsNullOrEmpty(m_langCharSetsAll.ToString()))
			{
				var characterSet = LXLiteHelper.GenerateCharacterSet(m_langCharSetsAll.ToString());
				LXLiteHelper.WriteFile(LXLiteConfig.LocalizationOutputFolder, $"characters_set_all.txt", characterSet);
				Debug.Log($"Exported characters_set_all.txt!");
			}
		}

		private void LoadSheetLocalizationData(IWorkbook pWorkBook, string pSheetName)
		{
			var sheet = pWorkBook.GetSheet(pSheetName);
			if (sheet == null || sheet.LastRowNum == 0)
			{
				Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return;
			}

			var idStrings = new List<string>();
			var textDict = new Dictionary<string, List<string>>();
			var firstRow = sheet.GetRow(0);
			int maxCellNum = firstRow.LastCellNum;

			string mergeCellValue = "";
			for (int row = 0; row <= sheet.LastRowNum; row++)
			{
				var rowData = sheet.GetRow(row);
				if (rowData == null)
					continue;
				for (int col = 0; col < maxCellNum; col++)
				{
					var cell = rowData.GetCell(col);
					var fieldValue = cell.ToCellString();
					var fieldName = sheet.GetRow(0).GetCell(col).ToString();
					if (cell != null && cell.IsMergedCell && !string.IsNullOrEmpty(fieldValue))
						mergeCellValue = fieldValue;
					if (cell != null && cell.IsMergedCell && string.IsNullOrEmpty(fieldValue))
						fieldValue = mergeCellValue;
					if (!string.IsNullOrEmpty(fieldName))
					{
						//idString
						if (col == 0 && row > 0)
						{
							if (string.IsNullOrEmpty(fieldValue))
								break;
							idStrings.Add(fieldValue);
						}
						//relativeId
						else if (col == 1 && row > 0)
						{
							if (string.IsNullOrEmpty(fieldValue))
								continue;

							idStrings[idStrings.Count - 1] = $"{idStrings[idStrings.Count - 1]}_{fieldValue}";
						}
						//languages
						else if (col > 1 && row > 0)
						{
							if (!textDict.ContainsKey(fieldName))
								textDict.Add(fieldName, new List<string>());
							textDict[fieldName].Add(fieldValue);
						}
					}
				}
			}

			if (m_localizationsDict.ContainsKey(pSheetName))
			{
				var builder = m_localizationsDict[pSheetName];
				idStrings.AddRange(builder.idsString);
				foreach (var b in builder.languageTextDict)
				{
					var language = b.Key;
					var texts = b.Value;
					if (textDict.ContainsKey(language))
						textDict[language].AddRange(texts);
					else
						textDict.Add(b.Key, b.Value);
				}
				m_localizationsDict[pSheetName] = new LocalizationBuilder()
				{
					idsString = idStrings,
					languageTextDict = textDict,
				};
			}
			else
				m_localizationsDict.Add(pSheetName, new LocalizationBuilder()
				{
					idsString = idStrings,
					languageTextDict = textDict,
				});
		}

		private void CreateLocalizationFile(List<string> pIdsString, Dictionary<string, List<string>> pLanguageTextDict, string pFileName)
		{
			if (pLanguageTextDict.Count == 0 || pLanguageTextDict.Count == 0)
				return;

			//Build id integer array
			var idBuilder = new StringBuilder();
			if (pIdsString.Count > 0)
			{
				idBuilder.Append("\tpublic const int");
				idBuilder.Append($"{Environment.NewLine}\t\t");
				for (int i = 0; i < pIdsString.Count; i++)
				{
					if (i > 0 && i % 100 == 0)
						idBuilder.Append($"{Environment.NewLine}\t\t");

					var idString = pIdsString[i].Replace(" ", "_").RemoveSpecialCharacters();
					if (i < pIdsString.Count - 1)
						idBuilder.Append($"{idString} = {i}, ");
					else
						idBuilder.Append($"{idString} = {i};");
				}
			}

			//Build id enum array
			var idBuilder2 = new StringBuilder();
			idBuilder2.Append($"\tpublic enum ID {Environment.NewLine}\t{{{Environment.NewLine}\t\tNONE = -1,");
			idBuilder2.Append($"{Environment.NewLine}\t\t");
			for (int i = 0; i < pIdsString.Count; i++)
			{
				var idString = pIdsString[i].Replace(" ", "_").RemoveSpecialCharacters();
				if (i > 0 && i % 100 == 0)
				{
					idBuilder2.Append($"{Environment.NewLine}\t\t");
					idBuilder2.Append($"{idString},");
				}
				else
				{
					if (i == 0)
						idBuilder2.Append($"{idString} = {i},");
					else
						idBuilder2.Append($" {idString},");
				}
			}
			idBuilder2.Append($"{Environment.NewLine}\t}}");

			//Build id string array
			var idStringDictBuilder = new StringBuilder();
			idStringDictBuilder.Append($"\tpublic static readonly string[] idString = new string[]{Environment.NewLine}\t{{{Environment.NewLine}\t\t");
			for (int i = 0; i < pIdsString.Count; i++)
			{
				if (i > 0 && i % 100 == 0)
				{
					idStringDictBuilder.Append($"{Environment.NewLine}\t\t");
					idStringDictBuilder.Append($"\"{pIdsString[i]}\",");
				}
				else if (i == 0)
					idStringDictBuilder.Append($"\"{pIdsString[i]}\",");
				else
					idStringDictBuilder.Append($" \"{pIdsString[i]}\",");
			}
			idStringDictBuilder.Append($"{Environment.NewLine}\t}};");

			//Build language json data
			foreach (var listText in pLanguageTextDict)
			{
				string json = JsonConvert.SerializeObject(listText.Value);
				LXLiteHelper.WriteFile(LXLiteConfig.LocalizationOutputFolder, $"{pFileName}_{listText.Key}.txt", json);
				Debug.Log($"Exported Localization content to {pFileName}_{listText.Key}.txt!");

				if (LXLiteConfig.LangCharSets != null && LXLiteConfig.LangCharSets.Contains(listText.Key))
				{
					if (m_langCharSets.ContainsKey(listText.Key))
						m_langCharSets[listText.Key] += json;
					else
						m_langCharSets[listText.Key] = json;
				}
				m_langCharSetsAll.Append(json);
			}

			//Build language dictionary
			var languagesDictBuilder = new StringBuilder();
			languagesDictBuilder.Append("\tpublic static readonly Dictionary<string, string> LanguageFiles = new Dictionary<string, string>() { ");
			foreach (var textsList in pLanguageTextDict)
			{
				languagesDictBuilder.Append($" {"{"} \"{textsList.Key}\", {$"\"{pFileName}_{textsList.Key}\""} {"},"}");

				if (!m_localizedLanguages.Contains(textsList.Key))
					m_localizedLanguages.Add(textsList.Key);
			}
			languagesDictBuilder.Append($" }};{Environment.NewLine}");
			languagesDictBuilder.Append($"\tpublic static readonly string DefaultLanguage = \"{pLanguageTextDict.First().Key}\";");

			//Write file localization constants
			string fileContent = Resources.Load<TextAsset>(LXLiteConfig.LOCALIZATION_TEMPLATE).text;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_ENUM", idBuilder2.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_CONST", idBuilder.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_STRING", idStringDictBuilder.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY", languagesDictBuilder.ToString());
			fileContent = fileContent.Replace("LOCALIZATION_FOLDER", GetLocalizationFolder(LXLiteConfig.LocalizationOutputFolder, out bool isAddressable));
			fileContent = fileContent.Replace("IS_ADDRESSABLE", isAddressable.ToString().ToLower());
			LXLiteHelper.WriteFile(LXLiteConfig.ScriptsOutputFolder, $"{pFileName}.cs", fileContent);
			Debug.Log($"Exported {pFileName}.cs!");

			//Write file localized text component
			fileContent = Resources.Load<TextAsset>(LXLiteConfig.LOCALIZATION_TEXT_TEMPLATE).text;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			LXLiteHelper.WriteFile(LXLiteConfig.ScriptsOutputFolder, $"{pFileName}Text.cs", fileContent);
			Debug.Log($"Exported {pFileName}Text.cs!");
		}

		private string GetLocalizationFolder(string path, out bool isAddressableAsset)
		{
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
	}
}