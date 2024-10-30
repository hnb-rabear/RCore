using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using RCore.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.E2U
{
	public static class E2UConstants
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

	public class E2USettings : ScriptableObject
	{
		private const string FILE_PATH = "Assets/Editor/ExcelToUnitySettings.asset";

		public Excel excelFile;
		public List<ExcelFile> excelFiles;
		public string jsonOutputFolder;
		public string constantsOutputFolder;
		public string localizationOutputFolder;
		public string @namespace;
		public bool separateConstants;
		public bool separateIDs;
		public bool separateLocalizations;
		public bool combineJson;
		public bool enumTypeOnlyForIDs;
		public bool encryptJson;
		public string langCharSets;
		public string persistentFields;
		public string excludedSheets;
		public string googleClientId;
		public string googleClientSecret;
		public string encryptionKey;
		private Dictionary<string, StringBuilder> m_idsBuilderDict = new Dictionary<string, StringBuilder>();
		private Dictionary<string, StringBuilder> m_constantsBuilderDict = new Dictionary<string, StringBuilder>();
		private Dictionary<string, int> m_allIds = new Dictionary<string, int>();
		private Dictionary<string, int> m_allIDsSorted; //List sorted by length will be used for linked data, for IDs which have prefix that is exactly same with another ID
		private Dictionary<string, LocalizationBuilder> m_localizationsDict = new Dictionary<string, LocalizationBuilder>();
		private List<string> m_localizedSheetsExported = new List<string>();
		private List<string> m_localizedLanguages = new List<string>();
		private Dictionary<string, string> m_characterMaps = new Dictionary<string, string>();

		public static E2USettings Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(FILE_PATH, typeof(E2USettings)) as E2USettings;
			if (collection == null)
			{
				collection = EditorHelper.CreateScriptableAsset<E2USettings>(FILE_PATH);
				collection.ResetToDefault();
			}
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

		private string GetLocalizationFolder()
		{
			string path = localizationOutputFolder;
			string resourcesDirName = "Resources";

			// Find the index of the Resources directory
			int resourcesIndex = path.IndexOf(resourcesDirName, StringComparison.OrdinalIgnoreCase);
			if (resourcesIndex != -1)
			{
				int startAfterResources = resourcesIndex + resourcesDirName.Length;
				string pathAfterResources = path.Substring(startAfterResources).TrimStart(System.IO.Path.DirectorySeparatorChar);
				return pathAfterResources;
			}
			return string.Empty;
		}

		public void ExportAll() { }

		public void ExportJson() { }

#region Export IDs

		public void ExportIDs()
		{
			if (string.IsNullOrEmpty(constantsOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Constants Output Folder!");
				return;
			}

			var workBook = excelFile.GetWorkBook();
			if (workBook == null)
				return;

			m_idsBuilderDict = new Dictionary<string, StringBuilder>();
			m_allIds = new Dictionary<string, int>();

			foreach (var m in excelFile.sheets)
			{
				if (m.name.EndsWith(E2UConstants.IDS_SHEET) && m.selected)
				{
					//Load All IDs
					BuildContentOfFileIDs(workBook, m.name);

					//Create IDs Files
					if (separateConstants)
					{
						var content = m_idsBuilderDict[m.name].ToString();
						CreateFileIDs(m.name, content);
					}
				}
			}

			if (!separateConstants)
			{
				var iDsBuilder = new StringBuilder();
				foreach (var builder in m_idsBuilderDict)
				{
					var content = builder.Value.ToString();
					iDsBuilder.Append(content);
					iDsBuilder.AppendLine();
				}
				CreateFileIDs("IDs", iDsBuilder.ToString());
			}
		}

		private bool BuildContentOfFileIDs(IWorkbook pWorkBook, string pSheetName)
		{
			var sheet = pWorkBook.GetSheet(pSheetName);

			if (sheet == null || sheet.LastRowNum == 0)
			{
				Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return false;
			}

			var idsBuilders = new List<StringBuilder>();
			var idsEnumBuilders = new List<StringBuilder>();
			var idsEnumBuilderNames = new List<string>();
			var idsEnumBuilderIndexes = new List<int>();
			for (int row = 0; row <= sheet.LastRowNum; row++)
			{
				var rowData = sheet.GetRow(row);
				if (rowData != null)
				{
					for (int col = 0; col <= rowData.LastCellNum; col += 3)
					{
						var cellKey = rowData.GetCell(col);
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
							var cellValue = rowData.GetCell(col + 1);
							if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
							{
								EditorUtility.DisplayDialog("Warning", $"Sheet {sheet.SheetName}: Key {key} doesn't have value!", "OK");
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
							var cellComment = rowData.GetCell(col + 2);
							if (cellComment != null && !string.IsNullOrEmpty(cellComment.ToString().Trim()))
							{
								string cellCommentFormula = E2UHelper.ConvertFormulaCell(cellComment);
								if (cellCommentFormula != null)
									sb.Append(" /*").Append(cellCommentFormula).Append("*/");
								else
									sb.Append(" /*").Append(cellComment).Append("*/");
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
							if (cellKey.ToString().Contains("[enum]"))
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
			}

			m_allIds = m_allIds.OrderBy(m => m.Key).ToDictionary(x => x.Key, x => x.Value);

			//Build Ids Enum
			if (idsEnumBuilders.Count > 0)
			{
				for (int i = 0; i < idsEnumBuilders.Count; i++)
				{
					var str = idsEnumBuilders[i].ToString()
						.Replace("\r\n\tpublic const int ", "")
						.Replace("\r\n", "")
						.Replace(";", ",").Trim();
					str = str.Remove(str.Length - 1);
					var enumIndex = str.IndexOf("[enum]", StringComparison.Ordinal);
					str = str.Remove(0, enumIndex + 6).Replace(",", ", ");

					string enumName = idsEnumBuilderNames[i].Replace(" ", "_");

					var enumBuilder = new StringBuilder();
					enumBuilder.Append("\tpublic enum ")
						.Append(enumName)
						.Append(" { ")
						.Append(str)
						.Append(" }\n");
					if (enumTypeOnlyForIDs)
					{
						var tempSb = new StringBuilder();
						tempSb.Append("\t#region ")
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

		private void CreateFileIDs(string exportFileName, string content)
		{
			string fileContent = Resources.Load<TextAsset>(E2UConstants.IDS_CS_TEMPLATE).text;
			fileContent = fileContent.Replace("_IDS_CLASS_NAME_", exportFileName);
			fileContent = fileContent.Replace("public const int _FIELDS_ = 0;", content);
			fileContent = AddNamespace(fileContent);

			E2UHelper.WriteFile(constantsOutputFolder, $"{exportFileName}.cs", fileContent);
			UnityEngine.Debug.Log($"Exported {exportFileName}.cs!");
		}

		private int GetReferenceId(string pKey, out bool pFound)
		{
			if (m_allIds == null || m_allIds.Count == 0) { }

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

		private void LoadSheetIDsValues(IWorkbook pWorkBook, string pSheetName)
		{
			var sheet = pWorkBook.GetSheet(pSheetName);

			if (sheet == null || sheet.LastRowNum == 0)
			{
				UnityEngine.Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return;
			}

			for (int row = 0; row <= sheet.LastRowNum; row++)
			{
				var rowData = sheet.GetRow(row);
				if (rowData == null)
					continue;
				for (int col = 0; col <= rowData.LastCellNum; col += 3)
				{
					var cellKey = rowData.GetCell(col);
					if (cellKey == null)
						continue;
					if (row <= 0)
						continue;
					var cellValue = rowData.GetCell(col + 1);
					if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
						continue;
					string key = cellKey.ToString().Trim();
					int value = int.Parse(cellValue.ToString().Trim());

					if (m_allIds.ContainsKey(key))
						EditorUtility.DisplayDialog("Duplicated ID!", $@"ID {key} is duplicated in sheet {pSheetName}", "Ok");
					m_allIds[key] = value;
				}
			}

			m_allIds = m_allIds.OrderBy(m => m.Key).ToDictionary(x => x.Key, x => x.Value);
		}

#endregion

#region Export Constants

		public void ExportConstants()
		{
			if (string.IsNullOrEmpty(constantsOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Constants Output Folder!");
				return;
			}

			var workBook = excelFile.GetWorkBook();
			if (workBook == null)
				return;

			var sheets = excelFile.sheets;

			m_allIds = new Dictionary<string, int>();
			if (m_allIds == null || m_allIds.Count == 0)
				foreach (var sheet in sheets)
					if (sheet.name.EndsWith(E2UConstants.IDS_SHEET))
						LoadSheetIDsValues(workBook, sheet.name);

			m_constantsBuilderDict = new Dictionary<string, StringBuilder>();

			foreach (var sheet in sheets)
			{
				if (sheet.name.EndsWith(E2UConstants.CONSTANTS_SHEET) && sheet.selected)
				{
					LoadSheetConstantsData(workBook, sheet.name);

					if (m_constantsBuilderDict.ContainsKey(sheet.name) && separateConstants)
					{
						CreateFileConstants(m_constantsBuilderDict[sheet.name].ToString(), sheet.name);
					}
				}
			}

			if (!separateConstants)
			{
				var builder = new StringBuilder();
				foreach (var b in m_constantsBuilderDict)
				{
					builder.Append(b.Value);
					builder.AppendLine();
				}
				CreateFileConstants(builder.ToString(), "Constants");
			}
		}

		private void LoadSheetConstantsData(IWorkbook pWorkbook, string pSheetName)
		{
			var sheet = pWorkbook.GetSheet(pSheetName);
			if (sheet == null || sheet.LastRowNum == 0)
			{
				Debug.LogWarning($"Sheet {pSheetName} is empty!");
				return;
			}

			var constants = new List<ConstantBuilder>();
			for (int row = 0; row <= sheet.LastRowNum; row++)
			{
				var rowData = sheet.GetRow(row);
				if (rowData != null)
				{
					string name = null;
					string value = null;
					string valueType = null;
					string comment = null;
					var cell = rowData.GetCell(0); //Name
					if (cell != null)
						name = cell.ToString().Trim();

					cell = rowData.GetCell(1); //Type
					if (cell != null)
						valueType = cell.ToString().Trim();

					cell = rowData.GetCell(2); //Value
					if (cell != null)
					{
						string formulaCellValue = E2UHelper.ConvertFormulaCell(cell);
						if (formulaCellValue != null)
							value = formulaCellValue;
						else
							value = cell.ToString().Trim();
					}

					cell = rowData.GetCell(3); //Comment 
					if (cell != null)
					{
						string formulaCellValue = E2UHelper.ConvertFormulaCell(cell);
						if (formulaCellValue != null)
							comment = formulaCellValue;
						else
							comment = cell.ToString().Trim();
					}

					if (name == null || value == null || valueType == null)
						continue;

					constants.Add(new ConstantBuilder()
					{
						name = name,
						value = value,
						valueType = valueType.ToLower(),
						comment = comment,
					});
				}
			}
			constants.Sort();
			BuildContentOfFileConstants(constants, pSheetName);
		}

		private void BuildContentOfFileConstants(List<ConstantBuilder> constants, string constantsSheet)
		{
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
					string[] strValues = E2UHelper.SplitValueToArray(value);
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
						string[] floatValues = E2UHelper.SplitValueToArray(value);
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
						string[] intValues = E2UHelper.SplitValueToArray(value);
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
						string[] vector2Values = E2UHelper.SplitValueToArray(value);
						fieldStr = $"\tpublic static readonly Vector2 {name} = new Vector2({vector2Values[0].Trim()}f, {vector2Values[1].Trim()}f);";
						break;
					case "vector3":
						string[] vector3Values = E2UHelper.SplitValueToArray(value);
						fieldStr = $"\tpublic static readonly Vector3 {name} = new Vector3({vector3Values[0].Trim()}f, {vector3Values[1].Trim()}f, {vector3Values[2].Trim()}f);";
						break;
					case "string":
						fieldStr = $"\tpublic const string {name} = \"{value.Trim()}\";";
						break;
					case "string-array":
					{
						string arrayStr = "";
						string[] values = E2UHelper.SplitValueToArray(value);
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
		}

		private void CreateFileConstants(string pContent, string pExportFileName)
		{
			string fileContent = Resources.Load<TextAsset>(E2UConstants.CONSTANTS_CS_TEMPLATE).text;
			fileContent = fileContent.Replace("_CONST_CLASS_NAME_", pExportFileName);
			fileContent = fileContent.Replace("public const int _FIELDS_ = 0;", pContent);
			fileContent = AddNamespace(fileContent);

			E2UHelper.WriteFile(constantsOutputFolder, pExportFileName + ".cs", fileContent);
			UnityEngine.Debug.Log($"Exported {pExportFileName}.cs!");
		}

#endregion

#region Export Localizations

		public void ExportLocalizations()
		{
			if (string.IsNullOrEmpty(localizationOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the Localization Output folder!");
				return;
			}

			var workBook = excelFile.GetWorkBook();
			if (workBook == null)
				return;

			m_localizationsDict = new Dictionary<string, LocalizationBuilder>();
			m_localizedSheetsExported = new List<string>();
			m_localizedLanguages = new List<string>();
			m_characterMaps = new Dictionary<string, string>();
			var m_sheets = excelFile.sheets;
			for (int i = 0; i < m_sheets.Count; i++)
			{
				if (m_sheets[i].selected && m_sheets[i].name.StartsWith(E2UConstants.LOCALIZATION_SHEET))
				{
					LoadSheetLocalizationData(workBook, m_sheets[i].name);

					if (m_localizationsDict.ContainsKey(m_sheets[i].name) && separateLocalizations)
					{
						var builder = m_localizationsDict[m_sheets[i].name];
						CreateLocalizationFileV2(builder.idsString, builder.languageTextDict, m_sheets[i].name);
						m_localizedSheetsExported.Add(m_sheets[i].name);
					}
				}
			}

			if (!separateLocalizations)
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
				CreateLocalizationFileV2(builder.idsString, builder.languageTextDict, "Localization");
				m_localizedSheetsExported.Add("Localization");
			}

			CreateLocalizationsManagerFile();
		}

		private void LoadSheetLocalizationData(IWorkbook pWorkBook, string pSheetName)
		{
			var sheet = pWorkBook.GetSheet(pSheetName);
			if (sheet == null || sheet.LastRowNum == 0)
			{
				UnityEngine.Debug.LogWarning($"Sheet {pSheetName} is empty!");
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
					else
					{
						Console.Write(col);
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

		private void CreateLocalizationFileV2(List<string> pIdsString, Dictionary<string, List<string>> pLanguageTextDict, string pFileName)
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
				E2UHelper.WriteFile(localizationOutputFolder, $"{pFileName}_{listText.Key}.txt", json);
				UnityEngine.Debug.Log($"Exported Localization content to {pFileName}_{listText.Key}.txt!");

				if (langCharSets != null && langCharSets.Contains(listText.Key))
				{
					if (m_characterMaps.ContainsKey(listText.Key))
						m_characterMaps[listText.Key] += json;
					else
						m_characterMaps[listText.Key] = json;
				}
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
			string fileContent = Resources.Load<TextAsset>(E2UConstants.LOCALIZATION_TEMPLATE_V2).text;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_ENUM", idBuilder2.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_CONST", idBuilder.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_STRING", idStringDictBuilder.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY", languagesDictBuilder.ToString());
			fileContent = fileContent.Replace("LOCALIZATION_FOLDER", GetLocalizationFolder());
			fileContent = AddNamespace(fileContent);
			E2UHelper.WriteFile(constantsOutputFolder, $"{pFileName}.cs", fileContent);
			UnityEngine.Debug.Log($"Exported {pFileName}.cs!");

			//Write file localized text component
			fileContent = Resources.Load<TextAsset>(E2UConstants.LOCALIZATION_TEXT_TEMPLATE).text;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			fileContent = AddNamespace(fileContent);
			E2UHelper.WriteFile(constantsOutputFolder, $"{pFileName}Text.cs", fileContent);
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

				string fileContent = Resources.Load<TextAsset>(E2UConstants.LOCALIZATION_MANAGER_TEMPLATE).text;
				fileContent = fileContent.Replace("//LOCALIZATION_INIT_ASYNC", initAsynLines.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_INIT", initLines.ToString());
				fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY", languagesDictBuilder.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_SET_FOLDER", setFolder.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_USE_ADDRESSABLE", useAddressable.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_SYSTEM_LANGUAGE", systemLanguages.ToString());
				fileContent = fileContent.Replace("LOCALIZATION_FOLDER", GetLocalizationFolder());
				fileContent = AddNamespace(fileContent);
				E2UHelper.WriteFile(constantsOutputFolder, "LocalizationsManager.cs", fileContent);
				UnityEngine.Debug.Log($"Exported LocalizationsManager.cs!");
			}
		}

#endregion


		private string AddNamespace(string fileContent)
		{
			if (!string.IsNullOrEmpty(@namespace))
			{
				fileContent = fileContent.Replace(Environment.NewLine, "NEW_LINE");
				fileContent = fileContent.Replace("\n", "NEW_LINE");
				fileContent = fileContent.Replace("NEW_LINE", $"{Environment.NewLine}\t");
				fileContent = $"namespace {@namespace}\n{"{"}\n\t{fileContent}\n{"}"}";
			}
			return fileContent;
		}

		public void ResetToDefault()
		{
			excelFile = new Excel();
			excelFiles = new List<ExcelFile>();
			jsonOutputFolder = "";
			constantsOutputFolder = "";
			localizationOutputFolder = "";
			@namespace = "";
			separateConstants = false;
			separateIDs = false;
			separateLocalizations = true;
			combineJson = false;
			enumTypeOnlyForIDs = false;
			encryptJson = false;
			langCharSets = "korea (kr), japan (jp), china (cn)";
			persistentFields = "id, key";
			excludedSheets = "";
			googleClientId = "";
			googleClientSecret = "";
			encryptionKey =
				"168, 220, 184, 133, 78, 149, 8, 249, 171, 138, 98, 170, 95, 15, 211, 200, 51, 242, 4, 193, 219, 181, 232, 99, 16, 240, 142, 128, 29, 163, 245, 24, 204, 73, 173, 32, 214, 76, 31, 99, 91, 239, 232, 53, 138, 195, 93, 195, 185, 210, 155, 184, 243, 216, 204, 42, 138, 101, 100, 241, 46, 145, 198, 66, 11, 17, 19, 86, 157, 27, 132, 201, 246, 112, 121, 7, 195, 148, 143, 125, 158, 29, 184, 67, 187, 100, 31, 129, 64, 130, 26, 67, 240, 128, 233, 129, 63, 169, 5, 211, 248, 200, 199, 96, 54, 128, 111, 147, 100, 6, 185, 0, 188, 143, 25, 103, 211, 18, 17, 249, 106, 54, 162, 188, 25, 34, 147, 3, 222, 61, 218, 49, 164, 165, 133, 12, 65, 92, 48, 40, 129, 76, 194, 229, 109, 76, 150, 203, 251, 62, 54, 251, 70, 224, 162, 167, 183, 78, 103, 28, 67, 183, 23, 80, 156, 97, 83, 164, 24, 183, 81, 56, 103, 77, 112, 248, 4, 168, 5, 72, 109, 18, 75, 219, 99, 181, 160, 76, 65, 16, 41, 175, 87, 195, 181, 19, 165, 172, 138, 172, 84, 40, 167, 97, 214, 90, 26, 124, 0, 166, 217, 97, 246, 117, 237, 99, 46, 15, 141, 69, 4, 245, 98, 73, 3, 8, 161, 98, 79, 161, 127, 19, 55, 158, 139, 247, 39, 59, 72, 161, 82, 158, 25, 65, 107, 173, 5, 255, 53, 28, 179, 182, 65, 162, 17";
		}
	}
}