using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace RCore.LXLite.Editor
{
	public class LXLiteGoogleWindow
	{
		private Dictionary<string, StringBuilder> m_idsBuilderDict = new Dictionary<string, StringBuilder>();
		private Dictionary<string, StringBuilder> m_constantsBuilderDict = new Dictionary<string, StringBuilder>();
		private Dictionary<string, int> m_allIds = new Dictionary<string, int>();
		private Dictionary<string, int> m_allIDsSorted;
		private SheetsService m_service;
		private Dictionary<string, LocalizationBuilder> m_localizationsDict;
		private List<string> m_localizedLanguages;
		private Dictionary<string, string> m_langCharSets;
		private StringBuilder m_langCharSetsAll;
		private Dictionary<string, Spreadsheet> m_cachedSpreadsheet = new Dictionary<string, Spreadsheet>();

		
		public void OnEnable()
		{
		}

		public void OnGUI()
		{
			LXLiteConfig.ClientId = EditorHelper.TextField(LXLiteConfig.ClientId, "Google Client Id", 200);
			LXLiteConfig.ClientSecret = EditorHelper.TextField(LXLiteConfig.ClientSecret, "Google Client Secret", 200);
			LXLiteConfig.GoogleSpreadsheetsId = EditorHelper.TextField(LXLiteConfig.GoogleSpreadsheetsId, "Google Spreadsheets Id", 200);

			EditorGUILayout.Space(5);
			if (GUILayout.Button("Export"))
				ExportLocalizations();
		}
		
		public void ExportLocalizations()
		{
			if (string.IsNullOrEmpty(LXLiteConfig.ScriptsOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Constants Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(LXLiteConfig.LocalizationOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Localization Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(LXLiteConfig.ClientId) || string.IsNullOrEmpty(LXLiteConfig.ClientSecret))
			{
				UnityEngine.Debug.LogError("Please setup the Client Id and Client Secret!");
				return;
			}

			m_localizationsDict = new Dictionary<string, LocalizationBuilder>();
			m_localizedLanguages = new List<string>();
			m_langCharSets = new Dictionary<string, string>();
			m_langCharSetsAll = new StringBuilder();

			var service = GetService();
			var sheetMetadata = GetCacheMetadata(LXLiteConfig.GoogleSpreadsheetsId);
			foreach (var sheet in sheetMetadata.Sheets)
			{
				if (!sheet.Properties.Title.StartsWith(LXLiteConfig.LOCALIZATION_SHEET))
					continue;

				var columnCount = sheet.Properties.GridProperties.ColumnCount;

				// Construct the range dynamically based on row and column counts
				var range = $"{sheet.Properties.Title}!A1:{GetColumnLetter(columnCount.Value)}";

				// Create a request to get the sheet data
				var request = service.Spreadsheets.Values.Get(LXLiteConfig.GoogleSpreadsheetsId, range);
				var response = request.Execute();
				var values = response.Values;

				LoadSheetLocalizationData(values, sheet.Properties.Title);
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
		}
		
		private Spreadsheet GetCacheMetadata(string googleSpreadsheetsId)
		{
			if (m_cachedSpreadsheet.TryGetValue(googleSpreadsheetsId, out var metadata))
				return metadata;
			var service = GetService();
			var spreadsheet = service.Spreadsheets.Get(googleSpreadsheetsId).Execute();
			m_cachedSpreadsheet[googleSpreadsheetsId] = spreadsheet;
			return spreadsheet;
		}
		
		public static string GetColumnLetter(int columnNumber)
		{
			int dividend = columnNumber;
			string columnLetter = string.Empty;

			while (dividend > 0)
			{
				int modulo = (dividend - 1) % 26;
				columnLetter = (char)(65 + modulo) + columnLetter; // 65 is the ASCII value for 'A'
				dividend = (dividend - modulo) / 26;
			}

			return columnLetter;
		}
		
		private void LoadSheetLocalizationData(IList<IList<object>> rowsData, string pSheetName)
		{
			if (rowsData == null || rowsData.Count == 0)
			{
				UnityEngine.Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return;
			}

			var idStrings = new List<string>();
			var textDict = new Dictionary<string, List<string>>();
			var firstRow = rowsData[0];
			int maxCellNum = firstRow.Count;

			for (int row = 0; row < rowsData.Count; row++)
			{
				var rowData = rowsData[row];
				if (rowData == null || rowData.Count == 0)
					continue;
				for (int col = 0; col < maxCellNum; col++)
				{
					string fieldValue = rowData[col].ToString();
					var fieldName = rowsData[0][col].ToString();
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
							if (string.IsNullOrEmpty(fieldValue) || m_allIds == null)
								continue;
							bool existId = false;
							foreach (var id in m_allIds)
								if (id.Key.Trim() == fieldValue.Trim())
								{
									fieldValue = id.Value.ToString();
									idStrings[idStrings.Count - 1] = $"{idStrings[idStrings.Count - 1]}_{id.Value}";
									existId = true;
									break;
								}

							if (!existId)
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
				idBuilder.Append("\n\t\t");
				for (int i = 0; i < pIdsString.Count; i++)
				{
					if (i > 0 && i % 100 == 0)
						idBuilder.Append("\n\t\t");

					if (i < pIdsString.Count - 1)
						idBuilder.Append($"{pIdsString[i].RemoveSpecialCharacters()} = {i}, ");
					else
						idBuilder.Append($"{pIdsString[i].RemoveSpecialCharacters()} = {i};");
				}
			}

			//Build id enum array
			var idBuilder2 = new StringBuilder();
			idBuilder2.Append("\tpublic enum ID \n\t{\n\t\tNONE = -1,");
			idBuilder2.Append("\n\t\t");
			for (int i = 0; i < pIdsString.Count; i++)
			{
				if (i > 0 && i % 100 == 0)
				{
					idBuilder2.Append("\n\t\t");
					idBuilder2.Append($"{pIdsString[i].RemoveSpecialCharacters()},");
				}
				else
				{
					if (i == 0)
						idBuilder2.Append($"{pIdsString[i].RemoveSpecialCharacters()} = {i},");
					else
						idBuilder2.Append($" {pIdsString[i].RemoveSpecialCharacters()},");
				}
			}
			idBuilder2.Append("\n\t}");

			//Build id string array
			var idStringDictBuilder = new StringBuilder();
			idStringDictBuilder.Append("\tpublic static readonly string[] idString = new string[]\n\t{\n\t\t");
			for (int i = 0; i < pIdsString.Count; i++)
			{
				if (i > 0 && i % 100 == 0)
				{
					idStringDictBuilder.Append("\n\t\t");
					idStringDictBuilder.Append($"\"{pIdsString[i]}\",");
				}
				else if (i == 0)
					idStringDictBuilder.Append($"\"{pIdsString[i]}\",");
				else
					idStringDictBuilder.Append($" \"{pIdsString[i]}\",");
			}
			idStringDictBuilder.Append("\n\t};");

			//Build language json data
			foreach (var listText in pLanguageTextDict)
			{
				string json = JsonConvert.SerializeObject(listText.Value);
				LXLiteHelper.WriteFile(LXLiteConfig.LocalizationOutputFolder, $"{pFileName}_{listText.Key}.txt", json);
				UnityEngine.Debug.Log($"Exported Localization content to {pFileName}_{listText.Key}.txt!");

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
			languagesDictBuilder.Append(" };\n");
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
			UnityEngine.Debug.Log($"Exported {pFileName}.cs!");

			//Write file localized text component
			fileContent = Resources.Load<TextAsset>(LXLiteConfig.LOCALIZATION_TEXT_TEMPLATE).text;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			LXLiteHelper.WriteFile(LXLiteConfig.ScriptsOutputFolder, $"{pFileName}Text.cs", fileContent);
			UnityEngine.Debug.Log($"Exported {pFileName}Text.cs!");
		}

		public string GetLocalizationFolder(string path, out bool isAddressableAsset)
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
		
		private SheetsService GetService()
		{
			m_service ??= new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = LXLiteHelper.AuthenticateGoogleUser(LXLiteConfig.ClientId, LXLiteConfig.ClientSecret),
				ApplicationName = LXLiteConfig.APPLICATION_NAME,
			});
			return m_service;
		}
	}
}