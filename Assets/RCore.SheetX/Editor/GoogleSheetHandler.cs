/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System.Collections.Generic;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	public class GoogleSheetHandler
	{
		private SheetXSettings m_settings;
		private Dictionary<string, StringBuilder> m_idsBuilderDict = new Dictionary<string, StringBuilder>();
		private Dictionary<string, StringBuilder> m_constantsBuilderDict = new Dictionary<string, StringBuilder>();
		private Dictionary<string, int> m_allIds = new Dictionary<string, int>();
		private Dictionary<string, int> m_allIDsSorted;
		private SheetsService m_service;
		private Dictionary<string, LocalizationBuilder> m_localizationsDict;
		private List<string> m_localizedSheetsExported;
		private List<string> m_localizedLanguages;
		private Dictionary<string, string> m_langCharSets;
		private StringBuilder m_langCharSetsAll;
		private Dictionary<string, Spreadsheet> m_cachedSpreadsheet = new Dictionary<string, Spreadsheet>();

		public GoogleSheetHandler(SheetXSettings settings)
		{
			m_settings = settings;
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

		//======================================

#region Export IDs

		public void ExportIDs()
		{
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Constants Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(m_settings.googleClientId) || string.IsNullOrEmpty(m_settings.googleClientSecret))
			{
				UnityEngine.Debug.LogError("Please setup the Client Id and Client Secret!");
				return;
			}
			var service = GetService();
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath.id);

			m_idsBuilderDict = new Dictionary<string, StringBuilder>();
			m_allIds = new Dictionary<string, int>();

			foreach (var sheet in m_settings.googleSheetsPath.sheets)
			{
				if (!sheet.selected || !sheet.name.EndsWith(SheetXConstants.IDS_SHEET))
					continue;

				var sheetInfo = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheet.name);
				if (sheetInfo == null)
					continue;

				var columnCount = sheetInfo.Properties.GridProperties.ColumnCount;

				// Construct the range dynamically based on row and column counts
				var range = $"{sheet.name}!A1:{GetColumnLetter(columnCount.Value)}";

				// Create a request to get the sheet data
				var request = service.Spreadsheets.Values.Get(m_settings.googleSheetsPath.id, range);
				var response = request.Execute();
				var values = response.Values;

				//Load All IDs
				BuildContentOfFileIDs(sheet.name, values);

				//Create IDs Files
				if (m_settings.separateConstants)
				{
					var content = m_idsBuilderDict[sheet.name].ToString();
					m_settings.CreateFileIDs(sheet.name, content);
				}

				if (!m_settings.separateConstants)
				{
					var iDsBuilder = new StringBuilder();
					foreach (var builder in m_idsBuilderDict)
					{
						var content = builder.Value.ToString();
						iDsBuilder.Append(content);
						iDsBuilder.AppendLine();
					}
					m_settings.CreateFileIDs("IDs", iDsBuilder.ToString());
				}
			}
		}

		private bool BuildContentOfFileIDs(string pSheetName, IList<IList<object>> rowsData)
		{
			if (rowsData == null || rowsData.Count == 0)
			{
				UnityEngine.Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return false;
			}

			var idsBuilders = new List<StringBuilder>();
			var idsEnumBuilders = new List<StringBuilder>();
			var idsEnumBuilderNames = new List<string>();
			var idsEnumBuilderIndexes = new List<int>();
			for (int row = 0; row < rowsData.Count; row++)
			{
				var rowData = rowsData[row];
				if (rowData == null)
					continue;
				for (int col = 0; col < rowData.Count; col += 3)
				{
					var cellKey = rowData[col];
					if (cellKey == null)
						continue;
					int index = col / 3;
					var sb = index < idsBuilders.Count ? idsBuilders[index] : new StringBuilder();
					if (!idsBuilders.Contains(sb))
					{
						idsBuilders.Add(sb);
					}
					//Values row
					if (row > 0)
					{
						string key = cellKey.ToString().Trim();
						if (string.IsNullOrEmpty(key))
							continue;

						//Value
						var cellValue = rowData[col + 1];
						if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
						{
							EditorUtility.DisplayDialog("Warning", $"Sheet {pSheetName}: Key {key} doesn't have value!", "OK");
							continue;
						}

						string valueStr = cellValue.ToString().Trim();
						int.TryParse(valueStr, out int value);
						sb.Append("\tpublic const int ");
						sb.Append(key);
						sb.Append(" = ");
						sb.Append(value);
						sb.Append(";");

						//Comment
						if (col + 2 < rowData.Count)
						{
							var cellComment = rowData[col + 2];
							if (cellComment != null && !string.IsNullOrEmpty(cellComment.ToString().Trim()))
								sb.Append(" /* ").Append(cellComment).Append(" */");
						}

						if (m_allIds.TryGetValue(key, out int val))
						{
							if (val != value)
								EditorUtility.DisplayDialog("Duplicated ID!", $"ID {key} is duplicated in sheet {pSheetName}", "OK");
						}
						else
						{
							m_allIds[key] = value;
						}
					}
					//Header row
					else
					{
						if (cellKey.ToString().EndsWith("[enum]"))
						{
							idsEnumBuilders.Add(sb);
							idsEnumBuilderNames.Add(cellKey.ToString().Replace("[enum]", ""));
							idsEnumBuilderIndexes.Add(index);
						}

						sb.Append("\t#region ")
							.Append(cellKey);
					}
					sb.Append(Environment.NewLine);
				}
			}

			m_allIds = m_allIds.OrderBy(m => m.Key).ToDictionary(x => x.Key, x => x.Value);

			//Build Ids Enum
			if (idsEnumBuilders.Count > 0)
			{
				for (int i = 0; i < idsEnumBuilders.Count; i++)
				{
					string str = SheetXHelper.RemoveComments(idsEnumBuilders[i].ToString())
						.Replace("  ", " ")
						.Replace(Environment.NewLine + "\tpublic const int ", "")
						.Replace(Environment.NewLine, "")
						.Replace(";", ", ")
						.Trim();

					int enumIndex = str.IndexOf("[enum]", StringComparison.Ordinal);
					if (enumIndex >= 0)
						str = str[(enumIndex + 6)..];

					string enumName = idsEnumBuilderNames[i].Replace(" ", "_");

					var enumBuilder = new StringBuilder()
						.Append("\tpublic enum ")
						.Append(enumName)
						.Append(" { ")
						.Append(str)
						.Append(" }\n");
					if (m_settings.onlyEnumAsIDs)
					{
						var tempSb = new StringBuilder()
							.Append("\t#region ")
							.Append(enumName)
							.Append(Environment.NewLine)
							.Append(enumBuilder);
						idsBuilders[idsEnumBuilderIndexes[i]] = tempSb;
					}
					else
						idsBuilders[idsEnumBuilderIndexes[i]].Append(enumBuilder);
				}
			}

			//Add end region and add to final dictionary
			var builder = new StringBuilder();
			for (int i = 0; i < idsBuilders.Count; i++)
			{
				string str = idsBuilders[i].ToString();
				if (!string.IsNullOrEmpty(str))
				{
					builder.Append(str);
					builder.Append("\t#endregion");
					if (i < idsBuilders.Count - 1)
						builder.Append(Environment.NewLine);
				}
			}

			if (m_idsBuilderDict.ContainsKey(pSheetName))
			{
				m_idsBuilderDict[pSheetName].AppendLine();
				m_idsBuilderDict[pSheetName].Append(builder);
			}
			else
				m_idsBuilderDict.Add(pSheetName, builder);

			return true;
		}

		private Dictionary<string, IList<IList<object>>> GetSheetIDsValues()
		{
			var ids = new Dictionary<string, IList<IList<object>>>();
			var service = GetService();
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath.id);
			foreach (var sheet in m_settings.googleSheetsPath.sheets)
			{
				if (!sheet.selected || !sheet.name.EndsWith(SheetXConstants.IDS_SHEET))
					continue;

				var sheetInfo = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheet.name);
				if (sheetInfo == null)
					continue;

				var columnCount = sheetInfo.Properties.GridProperties.ColumnCount;

				// Construct the range dynamically based on row and column counts
				var range = $"{sheet.name}!A1:{GetColumnLetter(columnCount.Value)}";

				// Create a request to get the sheet data
				var request = service.Spreadsheets.Values.Get(m_settings.googleSheetsPath.id, range);
				var response = request.Execute();
				var values = response.Values;
				ids[sheet.name] = values;
			}
			return ids;
		}

		private void LoadSheetIDsValues(IList<IList<object>> rowsData, string pSheetName)
		{
			if (rowsData == null || rowsData.Count == 0)
			{
				UnityEngine.Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return;
			}

			for (int row = 0; row < rowsData.Count; row++)
			{
				var rowData = rowsData[row];
				if (rowData == null)
					continue;
				for (int col = 0; col < rowData.Count; col += 3)
				{
					var cellKey = rowData[col];
					if (cellKey == null)
						continue;
					string key = cellKey.ToString().Trim();
					if (row <= 0 || string.IsNullOrEmpty(key))
						continue;
					var cellValue = rowData[col + 1];
					if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
						continue;
					int value = int.Parse(cellValue.ToString().Trim());
					if (m_allIds.ContainsKey(key))
						EditorUtility.DisplayDialog("Duplicated ID!", $@"ID {key} is duplicated in sheet {pSheetName}", "Ok");
					m_allIds[key] = value;
				}
			}

			m_allIds = m_allIds.OrderBy(m => m.Key).ToDictionary(x => x.Key, x => x.Value);
		}

		private int GetReferenceId(string pKey, out bool pFound)
		{
			if (m_allIDsSorted == null || m_allIDsSorted.Count == 0)
			{
				m_allIDsSorted = m_allIds.OrderBy(x => x.Key.Length).ToDictionary(x => x.Key, x => x.Value);
			}

			if (!string.IsNullOrEmpty(pKey))
			{
				if (int.TryParse(pKey, out int value))
				{
					pFound = true;
					return value;
				}

				if (m_allIDsSorted.TryGetValue(pKey, out int id))
				{
					pFound = true;
					return id;
				}
			}
			pFound = false;
			return 0;
		}

		private bool CheckExistedId(string pKey)
		{
			foreach (var id in m_allIds)
				if (id.Key == pKey.Trim())
					return true;
			return false;
		}

#endregion

#region Export Constants

		public void ExportConstants()
		{
#if !SX_LOCALIZATION
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Constants Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(m_settings.googleClientId) || string.IsNullOrEmpty(m_settings.googleClientSecret))
			{
				UnityEngine.Debug.LogError("Please setup the Client Id and Client Secret!");
				return;
			}

			if (m_allIds == null || m_allIds.Count == 0)
			{
				var sheetIDsValues = GetSheetIDsValues();
				foreach (var sheetIDs in sheetIDsValues)
					LoadSheetIDsValues(sheetIDs.Value, sheetIDs.Key);
			}

			m_constantsBuilderDict = new Dictionary<string, StringBuilder>();

			var service = GetService();
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath.id);
			foreach (var sheet in m_settings.googleSheetsPath.sheets)
			{
				if (!sheet.selected || !sheet.name.EndsWith(SheetXConstants.CONSTANTS_SHEET))
					continue;

				var sheetInfo = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheet.name);
				if (sheetInfo == null)
					continue;

				// Construct the range dynamically based on row and column counts
				var range = $"{sheet.name}!A1:D";

				// Create a request to get the sheet data
				var request = service.Spreadsheets.Values.Get(m_settings.googleSheetsPath.id, range);
				var response = request.Execute();
				var values = response.Values;

				LoadSheetConstantsData(sheet.name, values);

				if (m_constantsBuilderDict.ContainsKey(sheet.name) && m_settings.separateConstants)
					m_settings.CreateFileConstants(m_constantsBuilderDict[sheet.name].ToString(), sheet.name);
			}

			if (!m_settings.separateConstants)
			{
				var builder = new StringBuilder();
				foreach (var b in m_constantsBuilderDict)
				{
					builder.Append(b.Value);
					builder.AppendLine();
				}
				m_settings.CreateFileConstants(builder.ToString(), "Constants");
			}
#endif
		}

		private void LoadSheetConstantsData(string pSheetName, IList<IList<object>> rowsData)
		{
#if !SX_LOCALIZATION
			if (rowsData == null || rowsData.Count == 0)
			{
				UnityEngine.Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return;
			}

			var constants = new List<ConstantBuilder>();
			for (int row = 0; row < rowsData.Count; row++)
			{
				var newConst = new ConstantBuilder();
				var rowValues = rowsData[row];

				if (rowValues.Count < 1)
					continue;
				newConst.name = rowValues[0].ToString().Trim();
				if (rowValues.Count < 2)
					continue;
				newConst.valueType = rowValues[1].ToString().Trim();
				if (rowValues.Count < 3)
					continue;
				newConst.value = rowValues[2].ToString().Trim();
				if (rowValues.Count >= 4)
					newConst.comment = rowValues[3].ToString().Trim();

				if (string.IsNullOrEmpty(newConst.name)
				    || string.IsNullOrEmpty(newConst.valueType)
				    || string.IsNullOrEmpty(newConst.value))
					continue;

				constants.Add(newConst);
			}
			constants.Sort();
			BuildContentOfFileConstants(constants, pSheetName);
#endif
		}

		private void BuildContentOfFileConstants(List<ConstantBuilder> constants, string constantsSheet)
		{
#if !SX_LOCALIZATION
			var constantsSB = new StringBuilder("");
			for (int i = 0; i < constants.Count; i++)
			{
				string name = constants[i].name;
				string value = constants[i].value;
				string valueType = constants[i].valueType;
				string comment = constants[i].comment;
				string fieldStr = "";

				//Try to find references in ids list
				if (valueType == "int" && !int.TryParse(value, out int _))
				{
					int outValue = GetReferenceId(value, out bool found);
					if (found)
						value = outValue.ToString();
				}
				if (valueType == "int-array")
				{
					string[] strValues = SheetXHelper.SplitValueToArray(value);
					for (int j = 0; j < strValues.Length; j++)
					{
						//Try to find references in ids list
						if (int.TryParse(strValues[j].Trim(), out int _))
							continue;

						int refVal = GetReferenceId(strValues[j], out bool found);
						if (found)
						{
							value = value.Replace(strValues[j], refVal.ToString());
							strValues[j] = refVal.ToString();
						}
					}
				}

				switch (valueType)
				{
					case "int":
						fieldStr = $"\tpublic const int {name} = {value.Trim()};";
						break;
					case "float":
						fieldStr = $"\tpublic const float {name} = {value.Trim()}f;";
						break;
					case "float-array":
						string floatArrayStr = "";
						string[] floatValues = SheetXHelper.SplitValueToArray(value);
						for (int j = 0; j < floatValues.Length; j++)
						{
							if (j == floatValues.Length - 1)
								floatArrayStr += floatValues[j] + "f";
							else
								floatArrayStr += floatValues[j] + "f, ";
						}
						fieldStr = $"\tpublic static readonly float[] {name} = new float[{floatValues.Length}] {"{"} {floatArrayStr} {"}"};";
						break;
					case "int-array":
						string intArrayStr = "";
						string[] intValues = SheetXHelper.SplitValueToArray(value);
						for (int j = 0; j < intValues.Length; j++)
						{
							if (j == intValues.Length - 1)
								intArrayStr += intValues[j].Trim();
							else
								intArrayStr += intValues[j].Trim() + ", ";
						}
						fieldStr = $"\tpublic static readonly int[] {name} = new int[{intValues.Length}] {"{"} {intArrayStr} {"}"};";
						break;
					case "vector2":
						string[] vector2Values = SheetXHelper.SplitValueToArray(value);
						fieldStr = $"\tpublic static readonly Vector2 {name} = new Vector2({vector2Values[0].Trim()}f, {vector2Values[1].Trim()}f);";
						break;
					case "vector3":
						string[] vector3Values = SheetXHelper.SplitValueToArray(value);
						fieldStr = $"\tpublic static readonly Vector3 {name} = new Vector3({vector3Values[0].Trim()}f, {vector3Values[1].Trim()}f, {vector3Values[2].Trim()}f);";
						break;
					case "string":
						fieldStr = $"\tpublic const string {name} = \"{value.Trim()}\";";
						break;
					case "string-array":
					{
						string arrayStr = "";
						string[] values = SheetXHelper.SplitValueToArray(value);
						for (int j = 0; j < values.Length; j++)
						{
							if (j == values.Length - 1)
								arrayStr += "\"" + values[j].Trim() + "\"";
							else
								arrayStr += "\"" + values[j].Trim() + "\", ";
						}
						fieldStr = $"\tpublic static readonly string[] {name} = new string[{values.Length}] {"{"} {arrayStr} {"}"};";
					}
						break;
				}

				if (fieldStr != "")
				{
					if (!string.IsNullOrEmpty(comment))
						fieldStr += $" /*{comment}*/";
					constantsSB.Append(fieldStr).AppendLine();
				}
			}

			if (m_constantsBuilderDict.ContainsKey(constantsSheet))
				m_constantsBuilderDict[constantsSheet].AppendLine();
			else
				m_constantsBuilderDict.Add(constantsSheet, new StringBuilder());
			m_constantsBuilderDict[constantsSheet].Append(constantsSB);
#endif
		}

#endregion

#region Export Localizations

		public void ExportLocalizations()
		{
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Constants Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(m_settings.localizationOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Localization Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(m_settings.googleClientId) || string.IsNullOrEmpty(m_settings.googleClientSecret))
			{
				UnityEngine.Debug.LogError("Please setup the Client Id and Client Secret!");
				return;
			}

			if (m_allIds == null || m_allIds.Count == 0)
			{
				var sheetIDsValues = GetSheetIDsValues();
				foreach (var sheetIDs in sheetIDsValues)
					LoadSheetIDsValues(sheetIDs.Value, sheetIDs.Key);
			}

			m_localizationsDict = new Dictionary<string, LocalizationBuilder>();
			m_localizedSheetsExported = new List<string>();
			m_localizedLanguages = new List<string>();
			m_langCharSets = new Dictionary<string, string>();
			m_langCharSetsAll = new StringBuilder();

			var service = GetService();
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath.id);
			foreach (var sheet in m_settings.googleSheetsPath.sheets)
			{
				if (!sheet.selected || !sheet.name.StartsWith(SheetXConstants.LOCALIZATION_SHEET))
					continue;

				var sheetInfo = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheet.name);
				if (sheetInfo == null)
					continue;

				var columnCount = sheetInfo.Properties.GridProperties.ColumnCount;

				// Construct the range dynamically based on row and column counts
				var range = $"{sheet.name}!A1:{GetColumnLetter(columnCount.Value)}";

				// Create a request to get the sheet data
				var request = service.Spreadsheets.Values.Get(m_settings.googleSheetsPath.id, range);
				var response = request.Execute();
				var values = response.Values;

				LoadSheetLocalizationData(values, sheet.name);

				if (m_localizationsDict.ContainsKey(sheet.name) && m_settings.separateLocalizations)
				{
					var builder = m_localizationsDict[sheet.name];
					CreateLocalizationFile(builder.idsString, builder.languageTextDict, sheet.name);
					m_localizedSheetsExported.Add(sheet.name);
				}
			}

			if (!m_settings.separateLocalizations)
			{
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
				m_localizedSheetsExported.Add("Localization");
			}

			CreateLocalizationsManagerFile();
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
				SheetXHelper.WriteFile(m_settings.localizationOutputFolder, $"{pFileName}_{listText.Key}.txt", json);
				UnityEngine.Debug.Log($"Exported Localization content to {pFileName}_{listText.Key}.txt!");

				if (m_settings.langCharSets != null && m_settings.langCharSets.Contains(listText.Key))
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
			string fileContent = Resources.Load<TextAsset>(SheetXConstants.LOCALIZATION_TEMPLATE).text;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_ENUM", idBuilder2.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_CONST", idBuilder.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_STRING", idStringDictBuilder.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY", languagesDictBuilder.ToString());
			fileContent = fileContent.Replace("LOCALIZATION_FOLDER", m_settings.GetLocalizationFolder(out bool isAddressable));
			fileContent = fileContent.Replace("IS_ADDRESSABLE", isAddressable.ToString().ToLower());
			fileContent = SheetXHelper.AddNamespace(fileContent, m_settings.@namespace);
			SheetXHelper.WriteFile(m_settings.constantsOutputFolder, $"{pFileName}.cs", fileContent);
			UnityEngine.Debug.Log($"Exported {pFileName}.cs!");

			//Write file localized text component
			fileContent = Resources.Load<TextAsset>(SheetXConstants.LOCALIZATION_TEXT_TEMPLATE).text;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			fileContent = SheetXHelper.AddNamespace(fileContent, m_settings.@namespace);
			SheetXHelper.WriteFile(m_settings.constantsOutputFolder, $"{pFileName}Text.cs", fileContent);
			UnityEngine.Debug.Log($"Exported {pFileName}Text.cs!");
		}

		private void CreateLocalizationsManagerFile()
		{
			if (m_localizedSheetsExported.Count > 0)
			{
				//Build language dictionary
				var languagesDictBuilder = new StringBuilder();
				var systemLanguages = new StringBuilder();
				languagesDictBuilder.Append("\tpublic static readonly List<string> languages = new List<string>() { ");
				foreach (var lang in m_localizedLanguages)
				{
					languagesDictBuilder.Append($"\"{lang}\", ");

					string langLower = lang.ToLower();
					if (langLower.Contains("english") || langLower == "en")
						systemLanguages.Append($"\t\t\tSystemLanguage.English => \"{lang}\",").AppendLine();
					else if (langLower.Contains("vietnam") || langLower == "vn")
						systemLanguages.Append($"\t\t\tSystemLanguage.Vietnamese => \"{lang}\",").AppendLine();
					else if (langLower.Contains("spanish") || langLower == "es")
						systemLanguages.Append($"\t\t\tSystemLanguage.Spanish => \"{lang}\",").AppendLine();
					else if (langLower.Contains("portugal") || langLower.Contains("portuguese") || langLower == "pt")
						systemLanguages.Append($"\t\t\tSystemLanguage.Portuguese => \"{lang}\",").AppendLine();
					else if (langLower.Contains("russia") || langLower == "ru")
						systemLanguages.Append($"\t\t\tSystemLanguage.Russian => \"{lang}\",").AppendLine();
					else if (langLower.Contains("germany") || langLower.Contains("german") || langLower == "de")
						systemLanguages.Append($"\t\t\tSystemLanguage.German => \"{lang}\",").AppendLine();
					else if (langLower.Contains("indonesia") || langLower == "id")
						systemLanguages.Append($"\t\t\tSystemLanguage.Indonesian => \"{lang}\",").AppendLine();
					else if (langLower.Contains("thai") || langLower == "th")
						systemLanguages.Append($"\t\t\tSystemLanguage.Thai => \"{lang}\",").AppendLine();
					else if (langLower.Contains("korea") || langLower == "kr")
						systemLanguages.Append($"\t\t\tSystemLanguage.Korean => \"{lang}\",").AppendLine();
					else if (langLower.Contains("japan") || langLower == "jp")
						systemLanguages.Append($"\t\t\tSystemLanguage.Japanese => \"{lang}\",").AppendLine();
					else if (langLower.Contains("french") || langLower == "fr")
						systemLanguages.Append($"\t\t\tSystemLanguage.French => \"{lang}\",").AppendLine();
					else if (langLower.Contains("italian") || langLower == "it")
						systemLanguages.Append($"\t\t\tSystemLanguage.Italian => \"{lang}\",").AppendLine();
					else if (langLower.Contains("chinese") || langLower == "cn")
						systemLanguages.Append($"\t\t\tSystemLanguage.ChineseSimplified => \"{lang}\",").AppendLine();
					else if (langLower.Contains("arabic") || langLower == "ar")
						systemLanguages.Append($"\t\t\tSystemLanguage.Arabic => \"{lang}\",").AppendLine();
				}
				systemLanguages.Append($"\t\t\t_ => \"{m_localizedLanguages[0]}\",").AppendLine();
				languagesDictBuilder.Append("};\n");
				languagesDictBuilder.Append($"\tpublic static readonly string DefaultLanguage = \"{m_localizedLanguages.First()}\";");

				//Build initialization code
				var initLines = new StringBuilder();
				var initAsynLines = new StringBuilder();
				var setFolder = new StringBuilder();
				var useAddressable = new StringBuilder();

				for (int i = 0; i < m_localizedSheetsExported.Count; i++)
				{
					initLines.Append($"\t\t{m_localizedSheetsExported[i]}.Init();");
					if (i < m_localizedSheetsExported.Count - 1)
						initLines.Append(Environment.NewLine);

					initAsynLines.Append($"\t\tyield return {m_localizedSheetsExported[i]}.InitAsync();");
					if (i < m_localizedSheetsExported.Count - 1)
						initAsynLines.Append(Environment.NewLine);

					setFolder.Append($"\t\t{m_localizedSheetsExported[i]}.Folder = pFolder;");
					if (i < m_localizedSheetsExported.Count - 1)
						setFolder.Append(Environment.NewLine);

					useAddressable.Append($"\t\t{m_localizedSheetsExported[i]}.Addressable = pValue;");
					if (i < m_localizedSheetsExported.Count - 1)
						useAddressable.Append(Environment.NewLine);
				}

				string fileContent = Resources.Load<TextAsset>(SheetXConstants.LOCALIZATION_MANAGER_TEMPLATE).text;
				fileContent = fileContent.Replace("//LOCALIZATION_INIT_ASYNC", initAsynLines.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_INIT", initLines.ToString());
				fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY", languagesDictBuilder.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_SET_FOLDER", setFolder.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_USE_ADDRESSABLE", useAddressable.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_SYSTEM_LANGUAGE", systemLanguages.ToString());
				fileContent = fileContent.Replace("LOCALIZATION_FOLDER", m_settings.GetLocalizationFolder(out bool isAddressable));
				fileContent = fileContent.Replace("IS_ADDRESSABLE", isAddressable.ToString().ToLower());
				fileContent = SheetXHelper.AddNamespace(fileContent, m_settings.@namespace);
				SheetXHelper.WriteFile(m_settings.constantsOutputFolder, "LocalizationsManager.cs", fileContent);
				UnityEngine.Debug.Log($"Exported LocalizationsManager.cs!");
			}
		}

#endregion

#region Export Json

		public void ExportJson()
		{
#if !SX_LOCALIZATION
			if (string.IsNullOrEmpty(m_settings.jsonOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Json Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(m_settings.googleClientId) || string.IsNullOrEmpty(m_settings.googleClientSecret))
			{
				UnityEngine.Debug.LogError("Please setup the Client Id and Client Secret!");
				return;
			}

			if (m_allIds == null || m_allIds.Count == 0)
			{
				var sheetIDsValues = GetSheetIDsValues();
				foreach (var sheetIDs in sheetIDsValues)
					LoadSheetIDsValues(sheetIDs.Value, sheetIDs.Key);
			}

			bool writeJsonFileForSingleSheet = !m_settings.combineJson;
			var allJsons = new Dictionary<string, string>();
			var service = GetService();
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath.id);
			foreach (var sheet in m_settings.googleSheetsPath.sheets)
			{
				if (!sheet.selected || !SheetXHelper.IsJsonSheet(sheet.name))
					continue;

				var sheetInfo = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheet.name);
				if (sheetInfo == null)
					continue;

				var columnCount = sheetInfo.Properties.GridProperties.ColumnCount;

				// Construct the range dynamically based on row and column counts
				var range = $"{sheet.name}!A1:{GetColumnLetter(columnCount.Value)}";

				// Create a request to get the sheet data
				var request = service.Spreadsheets.Values.Get(m_settings.googleSheetsPath.id, range);
				var response = request.Execute();
				var values = response.Values;

				string fileName = sheet.name.Trim().Replace(" ", "_");
				string json = ConvertSheetToJson(values, sheet.name, fileName, m_settings.encryptJson, writeJsonFileForSingleSheet);

				//Merge all json into a single file
				if (m_settings.combineJson)
				{
					if (allJsons.ContainsKey(fileName))
					{
						UnityEngine.Debug.LogError($"Could not create single json file {fileName}, because file {fileName} is already exists!");
						continue;
					}
					allJsons.Add(fileName, json);
				}
			}
			if (m_settings.combineJson)
			{
				//Build json file for all jsons content
				string mergedJson = JsonConvert.SerializeObject(allJsons);
				string mergedFileName = sheetMetadata.Properties.Title.Replace(" ", "_");
				SheetXHelper.WriteFile(m_settings.jsonOutputFolder, $"{mergedFileName}.txt", mergedJson);

				if (m_settings.encryptJson)
					UnityEngine.Debug.Log($"Exported encrypted Json data to {mergedFileName}.txt.");
				else
					UnityEngine.Debug.Log($"Exported Json data to {mergedFileName}.txt.");
			}
#endif
		}

		private string ConvertSheetToJson(IList<IList<object>> pValues, string pSheetName, string pFileName, bool pEncrypt, bool pWriteFile)
		{
#if !SX_LOCALIZATION
			var fieldValueTypes = SheetXHelper.GetFieldValueTypes(pValues);
			if (fieldValueTypes == null)
				return "{}";
			return ConvertSheetToJson(pValues, pSheetName, pFileName, fieldValueTypes, pEncrypt, pWriteFile);
#endif
			return "{}";
		}

		private string ConvertSheetToJson(IList<IList<object>> pValues, string pSheetName, string pOutputFile, List<FieldValueType> pFieldValueTypes, bool pEncrypt, bool pAutoWriteFile)
		{
#if !SX_LOCALIZATION
			var unminifiedFields = m_settings.GetPersistentFields();

			if (pValues == null || pValues.Count == 0)
			{
				UnityEngine.Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return null;
			}

			int lastCellNum = 0;
			string[] fields = null;
			bool[] validCols = null;
			var rowContents = new List<RowContent>();

			for (int row = 0; row < pValues.Count; row++)
			{
				var rowValues = pValues[row];
				if (rowValues == null || rowValues.Count == 0)
					continue;

				if (row == 0)
				{
					lastCellNum = rowValues.Count;
					fields = new string[lastCellNum];
					validCols = new bool[lastCellNum];

					for (int col = 0; col < lastCellNum; col++)
					{
						var cellValue = rowValues[col].ToString().Trim();
						if (!string.IsNullOrEmpty(cellValue) && !cellValue.Contains("[x]"))
						{
							validCols[col] = true;
							fields[col] = cellValue.Trim();
						}
						else
						{
							validCols[col] = false;
							fields[col] = "";
						}
					}
				}
				else
				{
					var rowContent = new RowContent();
					for (int col = 0; col < lastCellNum; col++)
					{
						var cellValue = "";
						if (col < rowValues.Count)
							cellValue = rowValues[col].ToString().Trim();
						if (fields != null)
						{
							string fieldName = fields[col];
							string fieldValue = cellValue;
							fieldName = fieldName.Replace(" ", "_");
							rowContent.fieldNames.Add(fieldName);
							rowContent.fieldValues.Add(fieldValue);
						}
					}
					rowContents.Add(rowContent);
				}
			}

			string content = "[";
			for (int i = 0; i < rowContents.Count; i++)
			{
				var rowContent = rowContents[i];

				var attributes = new List<Att>();
				string fieldContentStr = "";
				bool rowIsEmpty = true; //Because Loading sheet sometime includes the empty rows, I don't know why it happen
				var nestedObjects = new List<JObject>();

				for (int j = 0; j < rowContent.fieldNames.Count; j++)
				{
					bool valid = validCols[j];
					if (!valid)
						continue;
					string fieldName = rowContent.fieldNames[j];
					string fieldValue = rowContent.fieldValues[j];
					bool isAttribute = fieldName.ToLower().Contains("attribute") && fieldName.Length <= 11;

					//some weird situation, data has attribute field, therefore Converter will confuse it with attrbiute from Attribute System
					//To fix this problem I have to add one more condition, the next field must be value
					if (isAttribute)
					{
						if (j + 1 >= rowContent.fieldNames.Count)
							isAttribute = false;
						string nextFieldName = rowContent.fieldNames[j + 1];
						if (!nextFieldName.ToLower().Contains("value") || nextFieldName.Length > 9)
							isAttribute = false;
					}

					if (!string.IsNullOrEmpty(fieldValue))
						rowIsEmpty = false;

					//Attributes System includes fields: attribute, value/value[], increase/increase[], max/max[], unlock/unlock[]
					//All these fields must lie on last of data sheet
					if (isAttribute)
					{
						var att = new Att();
						att.id = GetReferenceId(fieldValue, out bool found);
						att.idString = fieldValue;
						while (j < rowContent.fieldNames.Count - 1)
						{
							fieldValue = rowContent.fieldValues[j + 1].Trim();
							fieldName = rowContent.fieldNames[j + 1].Trim();
							if (fieldName.ToLower().Contains("unlock"))
							{
								bool isArray = fieldName.Contains("[]");
								j++;
								if (!isArray)
								{
									if (!float.TryParse(fieldValue, out att.unlock))
										att.unlock = GetReferenceId(fieldValue, out found);
								}
								else
								{
									string[] inValues = SheetXHelper.SplitValueToArray(fieldValue, false);
									float[] outValues = new float[inValues.Length];
									for (int t = 0; t < inValues.Length; t++)
									{
										if (!float.TryParse(inValues[t].Trim(), out outValues[t]))
											outValues[t] = GetReferenceId(inValues[t].Trim(), out found);
									}
									att.unlocks = outValues;
								}
							}
							else if (fieldName.ToLower().Contains("increase"))
							{
								bool isArray = fieldName.Contains("[]");
								j++;
								if (!isArray)
								{
									if (!float.TryParse(fieldValue, out att.increase))
										att.increase = GetReferenceId(fieldValue, out found);
								}
								else
								{
									string[] inValues = SheetXHelper.SplitValueToArray(fieldValue, false);
									float[] outValues = new float[inValues.Length];
									for (int t = 0; t < inValues.Length; t++)
									{
										if (!float.TryParse(inValues[t].Trim(), out outValues[t]))
											outValues[t] = GetReferenceId(inValues[t].Trim(), out found);
									}
									att.increases = outValues;
								}
							}
							else if (fieldName.ToLower().Contains("value"))
							{
								bool isArray = fieldName.Contains("[]"); //If attribute value is array
								j++;
								if (!isArray)
								{
									if (!float.TryParse(fieldValue, out att.value))
										att.value = GetReferenceId(fieldValue, out found);
									if (!found)
										att.valueString = fieldValue;
								}
								else
								{
									string[] inValues = SheetXHelper.SplitValueToArray(fieldValue, false);
									float[] outValues = new float[inValues.Length];
									for (int t = 0; t < inValues.Length; t++)
									{
										if (!float.TryParse(inValues[t].Trim(), out outValues[t]))
											outValues[t] = GetReferenceId(inValues[t].Trim(), out found);
									}
									if (outValues.Length == 1 && outValues[0] == 0)
										outValues = null;
									att.values = outValues;
								}
							}
							else if (fieldName.ToLower().Contains("max"))
							{
								bool isArray = fieldName.Contains("[]");
								j++;
								if (!isArray)
								{
									if (!float.TryParse(fieldValue, out att.max))
										att.max = GetReferenceId(fieldValue, out found);
								}
								else
								{
									string[] inValues = SheetXHelper.SplitValueToArray(fieldValue, false);
									float[] outValues = new float[inValues.Length];
									for (int t = 0; t < inValues.Length; t++)
									{
										if (!float.TryParse(inValues[t].Trim(), out outValues[t]))
											outValues[t] = GetReferenceId(inValues[t].Trim(), out found);
									}
									att.maxes = outValues;
								}
							}
							else
								break;
						}
						if (att.idString != "ATT_NULL" && !string.IsNullOrEmpty(att.idString))
						{
							attributes.Add(att);
						}
					}
					else
					{
						bool importantField = unminifiedFields.Contains(fieldName.Replace("[]", "").ToLower());

						//Ignore empty field or field have value which equal 0
						if (string.IsNullOrEmpty(fieldValue) || fieldValue == "0" && !importantField)
							continue;

						bool nestedFiled = fieldName.Contains(".");
						foreach (var field in pFieldValueTypes)
						{
							//Find referenced Id in string and convert it to number
							if (field.name == fieldName)
							{
								string fieldType = field.type;
								bool referencedId = false;
								if (fieldType == "string") //Find and replace string value with referenced ID
								{
									if (CheckExistedId(fieldValue))
									{
										fieldType = "number";
										referencedId = true;
									}
									else if (int.TryParse(fieldValue, out int _))
									{
										fieldType = "number";
										referencedId = true;
									}
								}
								if (fieldType == "array-string") //Find and replace string value with referenced ID
								{
									string[] arrayValue = SheetXHelper.SplitValueToArray(fieldValue, false);
									foreach (string val in arrayValue)
									{
										if (CheckExistedId(val.Trim()))
										{
											fieldType = "array-number";
											referencedId = true;
											break;
										}
									}
								}

								var jsonObject = new JObject();
								switch (fieldType)
								{
									case "number":
										if (referencedId)
										{
											int intValue = GetReferenceId(fieldValue, out bool _);
											if (!nestedFiled)
												fieldContentStr += $"\"{fieldName}\":{intValue},";
											jsonObject[fieldName] = intValue;
										}
										else
										{
											if (!nestedFiled)
												fieldContentStr += $"\"{fieldName}\":{fieldValue},";
											jsonObject[fieldName] = fieldValue;
										}
										break;

									case "string":
										fieldValue = fieldValue.Replace("\n", "\\n");
										fieldValue = fieldValue.Replace("\"", "\\\"");
										if (!nestedFiled)
											fieldContentStr += $"\"{fieldName}\":\"{fieldValue}\",";
										else
											jsonObject[fieldName] = fieldValue;
										break;

									case "bool":
										if (!nestedFiled)
											fieldContentStr += $"\"{fieldName}\":{fieldValue.ToLower()},";
										else
											jsonObject[fieldName] = fieldValue;
										break;

									case "array-number":
									{
										fieldName = fieldName.Replace("[]", "");
										var arrayValue = SheetXHelper.SplitValueToArray(fieldValue, false);
										var arrayStr = "[";
										for (int k = 0; k < arrayValue.Length; k++)
										{
											string val = arrayValue[k].Trim();
											if (referencedId)
												val = GetReferenceId(val, out bool _).ToString();
											if (k == 0) arrayStr += val;
											else arrayStr += "," + val;
										}
										arrayStr += "]";
										if (!nestedFiled)
											fieldContentStr += $"\"{fieldName}\":{arrayStr},";
										else
										{
											int[] array = JsonConvert.DeserializeObject<int[]>(arrayStr);
											jsonObject[fieldName] = JArray.FromObject(array);
										}
									}
										break;

									case "array-string":
									{
										fieldName = fieldName.Replace("[]", "");
										var arrayValue = SheetXHelper.SplitValueToArray(fieldValue, false);
										var arrayStr = "[";
										for (int k = 0; k < arrayValue.Length; k++)
										{
											if (k == 0) arrayStr += $"\"{arrayValue[k].Trim()}\"";
											else arrayStr += $",\"{arrayValue[k].Trim()}\"";
										}
										arrayStr += "]";
										if (!nestedFiled)
											fieldContentStr += $"\"{fieldName}\":{arrayStr},";
										else
										{
											string[] array = JsonConvert.DeserializeObject<string[]>(arrayStr);
											jsonObject[fieldName] = JArray.FromObject(array);
										}
									}
										break;

									case "array-bool":
									{
										fieldName = fieldName.Replace("[]", "");
										var arrayValue = SheetXHelper.SplitValueToArray(fieldValue, false);
										var arrayStr = "[";
										for (int k = 0; k < arrayValue.Length; k++)
										{
											if (k == 0) arrayStr += arrayValue[k].Trim().ToLower();
											else arrayStr += "," + arrayValue[k].Trim().ToLower();
										}
										arrayStr += "]";
										if (!nestedFiled)
											fieldContentStr += $"\"{fieldName}\":{arrayStr},";
										else
										{
											bool[] array = JsonConvert.DeserializeObject<bool[]>(arrayStr);
											jsonObject[fieldName] = JArray.FromObject(array);
										}
									}
										break;

									case "json":
									{
										fieldName = fieldName.Replace("{}", "");

										//Search Id in field value
										if (m_allIDsSorted == null || m_allIDsSorted.Count == 0)
										{
											m_allIDsSorted = SheetXHelper.SortIDsByLength(m_allIds);
										}
										foreach (var id in m_allIDsSorted)
										{
											if (fieldValue.Contains(id.Key))
												fieldValue = fieldValue.Replace(id.Key, id.Value.ToString());
										}
										if (!SheetXHelper.IsValidJson(fieldValue))
										{
											EditorUtility.DisplayDialog("Error", $@"Invalid Json string at Sheet: {pSheetName} Field: {fieldName} Row: {i + 1}", "Ok");
											UnityEngine.Debug.LogError($"Invalid Json string at Sheet: {pSheetName} Field: {fieldName} Row: {i + 1}");
										}
										var tempObj = JsonConvert.DeserializeObject(fieldValue);
										var tempJsonStr = JsonConvert.SerializeObject(tempObj);
										if (!nestedFiled)
											fieldContentStr += $"\"{fieldName}\":{tempJsonStr},";
										else
										{
											jsonObject[fieldName] = JObject.Parse(tempJsonStr);
										}
									}
										break;
								}

								// Nested Object
								if (nestedFiled)
									nestedObjects.Add(jsonObject);
							}
						}
					}
				}
				if (nestedObjects.Count > 0)
				{
					var nestedObjectsJson = SheetXHelper.ConvertToNestedJson(nestedObjects);
					fieldContentStr += $"{nestedObjectsJson.Substring(1, nestedObjectsJson.Length - 2)}";
				}
				if (attributes.Count > 0)
				{
					fieldContentStr += "\"Attributes\":[";
					for (int a = 0; a < attributes.Count; a++)
					{
						fieldContentStr += attributes[a].GetJsonString();
						if (a < attributes.Count - 1)
							fieldContentStr += ",";
					}
					fieldContentStr += "],";
				}
				if (nestedObjects.Count == 0)
					fieldContentStr = SheetXHelper.RemoveLast(fieldContentStr, ",");


				if (!rowIsEmpty)
					content += $"{"{"}{fieldContentStr}{"},"}";
			}
			content = SheetXHelper.RemoveLast(content, ",");
			content += "]";

			if (content == "[]")
			{
				UnityEngine.Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return null;
			}
			string finalContent = content;
			if (pEncrypt)
				finalContent = m_settings.GetEncryption().Encrypt(content);

			if (pAutoWriteFile)
			{
				SheetXHelper.WriteFile(m_settings.jsonOutputFolder, $"{pOutputFile}.txt", finalContent);
				if (pEncrypt)
					UnityEngine.Debug.Log($"Exported encrypted Json data to {pOutputFile}.txt.");
				else
					UnityEngine.Debug.Log($"Exported Json data to {pOutputFile}.txt.");
			}
			return finalContent;
#endif
			return "{}";
		}

#endregion

		public void ExportAll()
		{
			ExportIDs();
			ExportConstants();
			ExportJson();
			ExportLocalizations();
		}
		
		public void ExportAllFiles()
		{
#if !SX_LITE
			m_idsBuilderDict = new Dictionary<string, StringBuilder>();
			m_constantsBuilderDict = new Dictionary<string, StringBuilder>();
			m_localizationsDict = new Dictionary<string, LocalizationBuilder>();

			m_allIDsSorted = null;
			m_allIds = new Dictionary<string, int>();

			m_localizedSheetsExported = new List<string>();
			m_localizedLanguages = new List<string>();
			m_langCharSets = new Dictionary<string, string>();
			m_langCharSetsAll = new StringBuilder();

			var service = GetService();
			var googleSheetsPaths = m_settings.googleSheetsPaths;
			//Load and write Ids first
			foreach (var googleSheets in googleSheetsPaths)
			{
				// Get the sheet metadata to determine its dimensions
				var sheetMetadata = service.Spreadsheets.Get(googleSheets.id).Execute();
				foreach (var sheet in googleSheets.sheets)
				{
					if (!sheet.selected || !sheet.name.EndsWith(SheetXConstants.IDS_SHEET))
						continue;

					var sheetInfo = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheet.name);
					if (sheetInfo == null)
						continue;

					var columnCount = sheetInfo.Properties.GridProperties.ColumnCount;

					// Construct the range dynamically based on row and column counts
					var range = $"{sheet.name}!A1:{GetColumnLetter(columnCount.Value)}";

					// Create a request to get the sheet data
					var request = service.Spreadsheets.Values.Get(googleSheets.id, range);
					var response = request.Execute();
					var values = response.Values;

					// Build contents of file IDs and export to file if seperateIDs = true
					if (BuildContentOfFileIDs(sheet.name, values) && m_settings.separateIDs)
						m_settings.CreateFileIDs(sheet.name, m_idsBuilderDict[sheet.name].ToString());
				}
			}

			// 2. Read and write other data type
			foreach (var googleSheets in googleSheetsPaths)
			{
				var sheets = new List<SheetPath>();
				foreach (var sheet in googleSheets.sheets)
				{
					if (sheet.selected)
						sheets.Add(sheet);
				}

				// Get the sheet metadata to determine its dimensions
				var ggSheetsMetadata = service.Spreadsheets.Get(googleSheets.id).Execute();
				var allJsons = new Dictionary<string, string>();
				foreach (var sheet in sheets)
				{
					var sheetInfo = ggSheetsMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheet.name);
					if (sheetInfo == null)
						continue;

					var columnCount = sheetInfo.Properties.GridProperties.ColumnCount;

					// Construct the range dynamically based on row and column counts
					var range = $"{sheet.name}!A1:{GetColumnLetter(columnCount.Value)}";
					if (sheet.name.EndsWith(SheetXConstants.CONSTANTS_SHEET))
						range = $"{sheet.name}!A1:D";

					// Create a request to get the sheet data
					var request = service.Spreadsheets.Values.Get(googleSheets.id, range);
					var response = request.Execute();
					var values = response.Values;

					//Load and write json file
					if (SheetXHelper.IsJsonSheet(sheet.name))
					{
						string fileName = sheet.name.Trim().Replace(" ", "_");
						string json = ConvertSheetToJson(values, sheet.name, fileName, m_settings.encryptJson, !m_settings.combineJson);
						if (m_settings.combineJson)
						{
							if (allJsons.ContainsKey(fileName))
							{
								UnityEngine.Debug.LogError($"Can not merge sheet {fileName}, because key {fileName} is already exists!");
								continue;
							}
							allJsons.Add(fileName, json);
						}
					}

					if (m_settings.combineJson)
					{
						//Build json file for all jsons content
						string mergedJson = JsonConvert.SerializeObject(allJsons);
						string mergedFileName = ggSheetsMetadata.Properties.Title;
						SheetXHelper.WriteFile(m_settings.jsonOutputFolder, $"{mergedFileName}.txt", mergedJson);

						if (m_settings.encryptJson)
							Debug.Log($"Exported encrypted Json data to {mergedFileName}.txt.");
						else
							Debug.Log($"Exported Json data to {mergedFileName}.txt.");
					}

					//Load and write constants
					if (sheet.name.EndsWith(SheetXConstants.CONSTANTS_SHEET))
					{
						LoadSheetConstantsData(sheet.name, values);

						if (m_constantsBuilderDict.ContainsKey(sheet.name) && m_settings.separateConstants)
							m_settings.CreateFileConstants(m_constantsBuilderDict[sheet.name].ToString(), sheet.name);
					}

					//Load and write localizations
					if (sheet.name.StartsWith(SheetXConstants.LOCALIZATION_SHEET))
					{
						LoadSheetLocalizationData(values, sheet.name);

						if (m_localizationsDict.ContainsKey(sheet.name) && m_settings.separateLocalizations)
						{
							var builder = m_localizationsDict[sheet.name];
							CreateLocalizationFile(builder.idsString, builder.languageTextDict, sheet.name);
							m_localizedSheetsExported.Add(sheet.name);
						}
					}
				}
			}

			//Create file contain all IDs
			if (!m_settings.separateIDs)
			{
				var builder = new StringBuilder();
				int count = 0;
				int length = m_idsBuilderDict.Count;
				foreach (var b in m_idsBuilderDict)
				{
					builder.Append(b.Value);
					if (count < length - 1)
						builder.AppendLine();
					count++;
				}
				m_settings.CreateFileIDs("IDs", builder.ToString());
			}

			//Create file contain all Constants
			if (!m_settings.separateConstants)
			{
				var builder = new StringBuilder();
				int count = 0;
				int length = m_constantsBuilderDict.Count;
				foreach (var b in m_constantsBuilderDict)
				{
					builder.Append(b.Value);
					if (count < length - 1)
						builder.AppendLine();
					count++;
				}
				m_settings.CreateFileConstants(builder.ToString(), "Constants");
			}

			//Create file contain all Localizations
			if (!m_settings.separateLocalizations)
			{
				var localizationBuilder = new LocalizationBuilder();
				foreach (var b in m_localizationsDict)
				{
					localizationBuilder.idsString.AddRange(b.Value.idsString);
					foreach (var t in b.Value.languageTextDict)
					{
						var language = t.Key;
						var texts = t.Value;
						if (!localizationBuilder.languageTextDict.ContainsKey(language))
							localizationBuilder.languageTextDict.Add(language, new List<string>());
						localizationBuilder.languageTextDict[language].AddRange(texts);
					}
				}
				CreateLocalizationFile(localizationBuilder.idsString, localizationBuilder.languageTextDict, "Localization");
				m_localizedSheetsExported.Add("Localization");
			}

			//Create characters sets
			if (m_langCharSets != null && m_langCharSets.Count > 0)
			{
				var maps = SheetXHelper.GenerateCharacterSets(m_langCharSets);
				foreach (var map in maps)
				{
					SheetXHelper.WriteFile(m_settings.localizationOutputFolder, $"characters_set_{map.Key}.txt", map.Value);
					Debug.Log($"Exported characters_set_{map.Key}.txt");
				}
			}
			if (!string.IsNullOrEmpty(m_langCharSetsAll.ToString()))
			{
				var characterSet = SheetXHelper.GenerateCharacterSet(m_langCharSetsAll.ToString());
				SheetXHelper.WriteFile(m_settings.localizationOutputFolder, $"characters_set_all.txt", characterSet);
				UnityEngine.Debug.Log($"Exported characters_set_all.txt!");
			}

			//Create localization manager file
			CreateLocalizationsManagerFile();

			Debug.Log("Done!");
#endif
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

		private SheetsService GetService()
		{
			m_service ??= new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = SheetXHelper.AuthenticateGoogleUser(m_settings.ObfGoogleClientId, m_settings.ObfGoogleClientSecret),
				ApplicationName = SheetXConstants.APPLICATION_NAME,
			});
			return m_service;
		}
	}
}