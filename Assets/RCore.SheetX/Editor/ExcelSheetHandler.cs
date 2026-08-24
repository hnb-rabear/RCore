/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Handles reading data from Excel files using NPOI and exporting it to various formats (constants, IDs, localization).
	/// </summary>
	public class ExcelSheetHandler
	{
		private SheetXSettings m_settings;
		private Dictionary<string, StringBuilder> m_idsBuilderDict = new Dictionary<string, StringBuilder>();
		private Dictionary<string, StringBuilder> m_constantsBuilderDict = new Dictionary<string, StringBuilder>();
		private Dictionary<string, int> m_allIds = new Dictionary<string, int>();
		private Dictionary<string, int> m_allIDsSorted; //List sorted by length will be used for linked data, for IDs which have prefix that is exactly same with another ID
		private Dictionary<string, LocalizationBuilder> m_localizationsDict = new Dictionary<string, LocalizationBuilder>();
		private List<string> m_localizedSheetsExported = new List<string>();
		private List<string> m_localizedLanguages = new List<string>();
		private Dictionary<string, string> m_langCharSets;
		private StringBuilder m_langCharSetsAll;
		private SheetXWriter m_writer;
		private readonly SheetXBatchState m_batchState;

		public ExcelSheetHandler(SheetXSettings settings)
			: this(settings, null)
		{
		}

		internal ExcelSheetHandler(SheetXSettings settings, SheetXExportContext context)
		{
			m_settings = settings;
			m_writer = new SheetXWriter(settings, context);
		}

		internal ExcelSheetHandler(
			SheetXSettings settings,
			SheetXExportContext context,
			SheetXBatchState batchState)
			: this(settings, context)
		{
			m_batchState = batchState;
			m_allIds = batchState.AllIds;
			m_localizedSheetsExported = batchState.LocalizedSheetsExported;
			m_localizedLanguages = batchState.LocalizedLanguages;
			m_langCharSets = batchState.LangCharSets;
			m_langCharSetsAll = batchState.LangCharSetsAll;
		}

#region Export IDs

		/// <summary>
		/// Reads ID definitions from Excel sheets (ending with 'IDs') and generates C# constants files.
		/// </summary>
		public void ExportIDs()
		{
			if (!TryOpenWorkbook(out var workBook))
				return;
			ExportIDs(workBook);
		}

		/// <summary>
		/// Same as <see cref="ExportIDs()"/> against a workbook the caller already opened, so one
		/// request reads the file once instead of once per artifact kind.
		/// </summary>
		internal void ExportIDs(IWorkbook workBook)
		{
			var sheets = m_settings.excelSheetsPath.sheets;
			if (!sheets.Any(x => x.selected && x.name.EndsWith(SheetXConstants.IDS_SHEET)))
				return;
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				m_writer.Error("Please setup the Constants Output Folder!");
				return;
			}

			m_idsBuilderDict = new Dictionary<string, StringBuilder>();
			m_allIds = new Dictionary<string, int>();

			foreach (var m in sheets)
			{
				if (m.name.EndsWith(SheetXConstants.IDS_SHEET) && m.selected)
				{
					//Load All IDs
					// An empty or unreadable sheet builds nothing, so there is no builder to read back.
					if (BuildContentOfFileIDs(workBook, m.name) && m_settings.separateIDs)
					{
						var content = m_idsBuilderDict[m.name].ToString();
						m_writer.CreateFileIDs(m.name, content);
					}
				}
			}

			if (!m_settings.separateIDs)
			{
				var iDsBuilder = new StringBuilder();
				foreach (var builder in m_idsBuilderDict)
				{
					var content = builder.Value.ToString();
					iDsBuilder.Append(content);
					iDsBuilder.AppendLine();
				}
				m_writer.CreateFileIDs("IDs", iDsBuilder.ToString());
			}
		}

		private bool BuildContentOfFileIDs(IWorkbook pWorkBook, string pSheetName)
		{
			var sheet = pWorkBook.GetSheet(pSheetName);

			if (sheet == null || sheet.LastRowNum == 0)
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
				return false;
			}

			var idsBuilders = new List<StringBuilder>();
			var idsEnumBuilders = new List<StringBuilder>();
			var idsEnumBuilderNames = new List<string>();
			var idsEnumBuilderIndexes = new List<int>();
			for (int row = 0; row <= sheet.LastRowNum; row++)
			{
				var rowData = sheet.GetRow(row);
				if (rowData == null)
					continue;
				for (int col = 0; col < rowData.LastCellNum; col += 3)
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
							if (m_batchState != null)
								m_writer.Warn($"Sheet {sheet.SheetName}: Key {key} doesn't have value!");
							else
								m_writer.Blocking("Warning", $"Sheet {sheet.SheetName}: Key {key} doesn't have value!");
							continue;
						}

						string valueStr = cellValue.ToString().Trim();
						// The parse result used to be discarded, so a mistyped value generated "= 0" and
						// every consumer of that ID silently read the wrong number.
						if (!SheetXHelper.TryParseInt(valueStr, out int value))
						{
							m_writer.Error($"Sheet {pSheetName}: ID {key} has a non-integer value '{valueStr}'.");
							continue;
						}
						// The first definition wins. Appending a second "public const int" for the same key
						// produced C# that does not compile, so a conflict is reported and the row skipped.
						// Batch mode: Phase C preloaded all IDs into the shared table.
						// DeclaredIds gates whether this sheet declares the const.
						if (m_batchState != null)
						{
							if (!m_batchState.DeclaredIds.Add(key))
								continue;
							// Value already in m_allIds from Phase C — skip the
							// m_allIds[key] = value assignment below.
						}
						else if (m_allIds.TryGetValue(key, out int existing))
						{
							if (existing != value)
								m_writer.Blocking("Duplicated ID!", $"ID {key} is duplicated in sheet {pSheetName}");
							continue;
						}

						if (m_batchState == null)
							m_allIds[key] = value;
						sb.Append("\tpublic const int ");
						sb.Append(key);
						sb.Append(" = ");
						sb.Append(value);
						sb.Append(";");

						//Comment
						var cellComment = rowData.GetCell(col + 2);
						if (cellComment != null && !string.IsNullOrEmpty(cellComment.ToString().Trim()))
						{
							string cellCommentFormula = SheetXHelper.ConvertFormulaCell(cellComment);
							if (cellCommentFormula != null)
								sb.Append(" /* ").Append(cellCommentFormula).Append(" */ ");
							else
								sb.Append(" /* ").Append(cellComment).Append(" */ ");
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

			if (m_batchState == null)
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
						.Append($" }}{Environment.NewLine}");
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

		private int GetReferenceId(string pKey, out bool pFound)
		{
			if (m_allIDsSorted == null || m_allIDsSorted.Count == 0)
			{
				m_allIDsSorted = SheetXHelper.SortIDsByLength(m_allIds);
			}

			if (!string.IsNullOrEmpty(pKey))
			{
				if (SheetXHelper.TryParseInt(pKey, out int value))
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
				m_writer.Warn($"Sheet {pSheetName} is empty!");
				return;
			}

			for (int row = 0; row <= sheet.LastRowNum; row++)
			{
				var rowData = sheet.GetRow(row);
				if (rowData == null)
					continue;
				for (int col = 0; col < rowData.LastCellNum; col += 3)
				{
					var cellKey = rowData.GetCell(col);
					if (cellKey == null)
						continue;
					string key = cellKey.ToString().Trim();
					if (row <= 0 || string.IsNullOrEmpty(key))
						continue;
					var cellValue = rowData.GetCell(col + 1);
					if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
						continue;
					if (!SheetXHelper.TryParseInt(cellValue.ToString().Trim(), out int value))
					{
						m_writer.Error($"Sheet {pSheetName}: ID {key} has a non-integer value '{cellValue}'.");
						continue;
					}
					if (m_allIds.ContainsKey(key))
						m_writer.Blocking("Duplicated ID!", $"ID {key} is duplicated in sheet {pSheetName}");
					m_allIds[key] = value;
				}
			}

			if (m_batchState == null)
				m_allIds = m_allIds.OrderBy(m => m.Key).ToDictionary(x => x.Key, x => x.Value);
		}

		internal void BatchLoadIds(
			IWorkbook workbook, string sourcePath, string sheetName)
		{
			var sheet = workbook.GetSheet(sheetName);
			if (sheet == null || sheet.LastRowNum == 0)
			{
				m_writer.Warn($"Sheet {sheetName} is empty!");
				return;
			}
			for (int row = 0; row <= sheet.LastRowNum; row++)
			{
				var rowData = sheet.GetRow(row);
				if (rowData == null)
					continue;
				for (int col = 0; col < rowData.LastCellNum; col += 3)
				{
					var cellKey = rowData.GetCell(col);
					if (cellKey == null)
						continue;
					string key = cellKey.ToString().Trim();
					if (row <= 0 || string.IsNullOrEmpty(key))
						continue;
					var cellValue = rowData.GetCell(col + 1);
					if (cellValue == null
						|| string.IsNullOrEmpty(cellValue.ToString()))
						continue;
					if (!SheetXHelper.TryParseInt(
						cellValue.ToString().Trim(), out int value))
					{
						m_writer.Error(
							$"Sheet {sheetName}: ID {key} has a "
							+ $"non-integer value '{cellValue}'.");
						continue;
					}
					if (!m_batchState.TryAddId(
						key, value, sourcePath, sheetName, out string error))
					{
						if (error != null)
							m_writer.Error(error);
					}
				}
			}
		}

#endregion

#region Export Constants

		/// <summary>
		/// Reads constant definitions from Excel sheets (ending with 'Constants') and generates C# constants files.
		/// Supports various types like int, float, string, arrays, and vectors.
		/// </summary>
		public void ExportConstants()
		{
			if (!TryOpenWorkbook(out var workBook))
				return;
			ExportConstants(workBook);
		}

		/// <summary>Exports constants from a workbook the caller already opened.</summary>
		internal void ExportConstants(IWorkbook workBook)
		{
			var sheets = m_settings.excelSheetsPath.sheets;
			if (!sheets.Any(x => x.selected && x.name.EndsWith(SheetXConstants.CONSTANTS_SHEET)))
				return;
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				m_writer.Error("Please setup the Constants Output Folder!");
				return;
			}

			if (m_allIds == null || m_allIds.Count == 0)
			{
				m_allIds = new Dictionary<string, int>();
				foreach (var sheet in sheets)
					if (sheet.name.EndsWith(SheetXConstants.IDS_SHEET))
						LoadSheetIDsValues(workBook, sheet.name);
			}

			m_constantsBuilderDict = new Dictionary<string, StringBuilder>();

			foreach (var sheet in sheets)
			{
				if (sheet.name.EndsWith(SheetXConstants.CONSTANTS_SHEET) && sheet.selected)
				{
					LoadSheetConstantsData(workBook, sheet.name);

					if (m_constantsBuilderDict.ContainsKey(sheet.name) && m_settings.separateConstants)
						m_writer.CreateFileConstants(m_constantsBuilderDict[sheet.name].ToString(), sheet.name);
				}
			}

			if (!m_settings.separateConstants)
			{
				var builder = new StringBuilder();
				foreach (var b in m_constantsBuilderDict)
				{
					builder.Append(b.Value);
					builder.AppendLine();
				}
				m_writer.CreateFileConstants(builder.ToString(), "Constants");
			}
		}

		private void LoadSheetConstantsData(IWorkbook pWorkbook, string pSheetName)
		{
			var sheet = pWorkbook.GetSheet(pSheetName);
			if (sheet == null || sheet.LastRowNum == 0)
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
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
						string formulaCellValue = SheetXHelper.ConvertFormulaCell(cell);
						if (formulaCellValue != null)
							value = formulaCellValue;
						else
							value = cell.ToString().Trim();
					}

					cell = rowData.GetCell(3); //Comment 
					if (cell != null)
					{
						string formulaCellValue = SheetXHelper.ConvertFormulaCell(cell);
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
				if (valueType == "int" && !SheetXHelper.TryParseInt(value, out int _))
				{
					int outValue = GetReferenceId(value, out bool found);
					if (found)
						value = SheetXHelper.FormatInt(outValue);
				}
				if (valueType == "int-array")
				{
					string[] strValues = SheetXHelper.SplitValueToArray(value);
					for (int j = 0; j < strValues.Length; j++)
					{
						//Try to find references in ids list
						if (SheetXHelper.TryParseInt(strValues[j].Trim(), out int _))
							continue;

						int refVal = GetReferenceId(strValues[j], out bool found);
						if (found)
						{
							value = value.Replace(strValues[j], SheetXHelper.FormatInt(refVal));
							strValues[j] = SheetXHelper.FormatInt(refVal);
						}
					}
				}

				switch (valueType)
				{
					case "int":
						fieldStr = $"\tpublic const int {name} = {value.Trim()};";
						break;
					case "float":
						fieldStr = $"\tpublic const float {name} = {SheetXHelper.FormatFloatLiteral(value)};";
						break;
					case "float-array":
						string[] floatValues = SheetXHelper.SplitValueToArray(value);
						string floatArrayStr = string.Join(", ", floatValues.Select(SheetXHelper.FormatFloatLiteral));
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
						fieldStr = $"\tpublic static readonly Vector2 {name} = new Vector2({SheetXHelper.FormatFloatLiteral(vector2Values[0])}, {SheetXHelper.FormatFloatLiteral(vector2Values[1])});";
						break;
					case "vector3":
						string[] vector3Values = SheetXHelper.SplitValueToArray(value);
						fieldStr = $"\tpublic static readonly Vector3 {name} = new Vector3({SheetXHelper.FormatFloatLiteral(vector3Values[0])}, {SheetXHelper.FormatFloatLiteral(vector3Values[1])}, {SheetXHelper.FormatFloatLiteral(vector3Values[2])});";
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
		}

#endregion

#region Export Localizations

		/// <summary>
		/// Reads localization data from Excel sheets (starting with 'Localization') and exports it to JSON files and C# dictionaries.
		/// </summary>
		public void ExportLocalizations()
		{
			if (!TryOpenWorkbook(out var workBook))
				return;
			ExportLocalizations(workBook);
		}

		/// <summary>Exports localizations from a workbook the caller already opened.</summary>
		internal void ExportLocalizations(IWorkbook workBook)
		{
			var sheets = m_settings.excelSheetsPath.sheets;
			if (!sheets.Any(x => x.selected && x.name.StartsWith(SheetXConstants.LOCALIZATION_SHEET)))
				return;
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				m_writer.Error("Please setup the Constants Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(m_settings.localizationOutputFolder))
			{
				m_writer.Error("Please setup the Localization Output folder!");
				return;
			}

			if (m_allIds == null || m_allIds.Count == 0)
			{
				m_allIds = new Dictionary<string, int>();
				foreach (var sheet in sheets)
					if (sheet.name.EndsWith(SheetXConstants.IDS_SHEET))
						LoadSheetIDsValues(workBook, sheet.name);
			}

			m_localizationsDict = new Dictionary<string, LocalizationBuilder>();
			m_localizedSheetsExported = new List<string>();
			m_localizedLanguages = new List<string>();
			m_langCharSets = new Dictionary<string, string>();
			m_langCharSetsAll = new StringBuilder();

			for (int i = 0; i < sheets.Count; i++)
			{
				if (!sheets[i].selected || !sheets[i].name.StartsWith(SheetXConstants.LOCALIZATION_SHEET))
					continue;

				LoadSheetLocalizationData(workBook, sheets[i].name);

				if (m_localizationsDict.ContainsKey(sheets[i].name) && m_settings.separateLocalizations)
				{
					var builder = m_localizationsDict[sheets[i].name];
					CreateLocalizationFile(builder.idsString, builder.languageTextDict, sheets[i].name);
					m_localizedSheetsExported.Add(sheets[i].name);
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

			//Create localization manager file
			CreateLocalizationsManagerFile();
		}

		private void LoadSheetLocalizationData(IWorkbook pWorkBook, string pSheetName)
		{
			var sheet = pWorkBook.GetSheet(pSheetName);
			if (sheet == null || sheet.LastRowNum == 0)
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
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
					var fieldName = firstRow.GetCell(col).ToCellString();
					var fieldValue = cell.ToCellString();
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
									fieldValue = SheetXHelper.FormatInt(id.Value);
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
				idBuilder.Append($"{Environment.NewLine}\t\t");
				for (int i = 0; i < pIdsString.Count; i++)
				{
					if (i > 0 && i % 100 == 0)
						idBuilder.Append($"{Environment.NewLine}\t\t");

					var idString = pIdsString[i].RemoveSpecialCharacters();
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
				var idString = pIdsString[i].RemoveSpecialCharacters();
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
				m_writer.Write(m_settings.localizationOutputFolder, $"{pFileName}_{listText.Key}.txt", json,
					SheetXExportFileType.Localization, $"Exported Localization content to {pFileName}_{listText.Key}.txt!");

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
			languagesDictBuilder.Append($" }};{Environment.NewLine}");
			languagesDictBuilder.Append($"\tpublic static readonly string DefaultLanguage = \"{pLanguageTextDict.First().Key}\";");

			//Write file localization constants
			if (!m_writer.TryLoadTemplate(SheetXConstants.LOCALIZATION_TEMPLATE, out string fileContent))
				return;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_ENUM", idBuilder2.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_CONST", idBuilder.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY_KEY_STRING", idStringDictBuilder.ToString());
			fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY", languagesDictBuilder.ToString());
			fileContent = fileContent.Replace("LOCALIZATION_FOLDER", m_settings.GetLocalizationFolder(out bool isAddressable));
			fileContent = fileContent.Replace("IS_ADDRESSABLE", isAddressable.ToString().ToLower());
			fileContent = SheetXHelper.AddNamespace(fileContent, m_settings.@namespace);
			m_writer.Write(m_settings.constantsOutputFolder, $"{pFileName}.cs", fileContent,
				SheetXExportFileType.LocalizationConstants, $"Exported {pFileName}.cs!");

			//Write file localized text component
			if (!m_writer.TryLoadTemplate(SheetXConstants.LOCALIZATION_TEXT_TEMPLATE, out fileContent))
				return;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			fileContent = SheetXHelper.AddNamespace(fileContent, m_settings.@namespace);
			m_writer.Write(m_settings.constantsOutputFolder, $"{pFileName}Text.cs", fileContent,
				SheetXExportFileType.LocalizationComponent, $"Exported {pFileName}Text.cs!");
		}

		private void CreateLocalizationsManagerFile()
		{
			//Create language character sets
			if (m_langCharSets != null && m_langCharSets.Count > 0)
			{
				var maps = SheetXHelper.GenerateCharacterSets(m_langCharSets);
				foreach (var map in maps)
				{
					m_writer.Write(m_settings.localizationOutputFolder, $"characters_set_{map.Key}.txt", map.Value,
						SheetXExportFileType.CharacterSet, $"Exported characters_set_{map.Key}.txt!");
				}
			}
			if (!string.IsNullOrEmpty(m_langCharSetsAll.ToString()))
			{
				var characterSet = SheetXHelper.GenerateCharacterSet(m_langCharSetsAll.ToString());
				m_writer.Write(m_settings.localizationOutputFolder, "characters_set_all.txt", characterSet,
					SheetXExportFileType.CharacterSet, "Exported characters_set_all.txt!");
			}

			if (m_localizedSheetsExported.Count > 0
				&& m_localizedLanguages.Count > 0)
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
					else if (langLower.Contains("vietnam") || langLower == "vn" || langLower == "vi")
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
					else if (langLower.Contains("korea") || langLower.Contains("korean") || langLower == "kr" || langLower == "ko")
						systemLanguages.Append($"\t\t\tSystemLanguage.Korean => \"{lang}\",").AppendLine();
					else if (langLower.Contains("japan") || langLower == "jp")
						systemLanguages.Append($"\t\t\tSystemLanguage.Japanese => \"{lang}\",").AppendLine();
					else if (langLower.Contains("french") || langLower == "fr")
						systemLanguages.Append($"\t\t\tSystemLanguage.French => \"{lang}\",").AppendLine();
					else if (langLower.Contains("italian") || langLower == "it")
						systemLanguages.Append($"\t\t\tSystemLanguage.Italian => \"{lang}\",").AppendLine();
					else if (langLower.Contains("turk") || langLower.Contains("turkish") || langLower == "tr")
						systemLanguages.Append($"\t\t\tSystemLanguage.Turkish => \"{lang}\",").AppendLine();
					else if (langLower.Contains("chinese") && (langLower.Contains("traditional") || langLower.Contains("tw")))
						systemLanguages.Append($"\t\t\tSystemLanguage.ChineseTraditional => \"{lang}\",").AppendLine();
					else if (langLower.Contains("chinese") || langLower == "cn" || langLower == "zh")
						systemLanguages.Append($"\t\t\tSystemLanguage.ChineseSimplified => \"{lang}\",").AppendLine();
					else if (langLower.Contains("czech") || langLower == "cs")
						systemLanguages.Append($"\t\t\tSystemLanguage.Czech => \"{lang}\",").AppendLine();
					else if (langLower.Contains("danish") || langLower == "da")
						systemLanguages.Append($"\t\t\tSystemLanguage.Danish => \"{lang}\",").AppendLine();
					else if (langLower.Contains("dutch") || langLower == "nl")
						systemLanguages.Append($"\t\t\tSystemLanguage.Dutch => \"{lang}\",").AppendLine();
					else if (langLower.Contains("finnish") || langLower == "fi")
						systemLanguages.Append($"\t\t\tSystemLanguage.Finnish => \"{lang}\",").AppendLine();
					else if (langLower.Contains("greek") || langLower == "el")
						systemLanguages.Append($"\t\t\tSystemLanguage.Greek => \"{lang}\",").AppendLine();
					else if (langLower.Contains("hebrew") || langLower == "he")
						systemLanguages.Append($"\t\t\tSystemLanguage.Hebrew => \"{lang}\",").AppendLine();
					else if (langLower.Contains("hungarian") || langLower == "hu")
						systemLanguages.Append($"\t\t\tSystemLanguage.Hungarian => \"{lang}\",").AppendLine();
					else if (langLower.Contains("icelandic") || langLower == "is")
						systemLanguages.Append($"\t\t\tSystemLanguage.Icelandic => \"{lang}\",").AppendLine();
					else if (langLower.Contains("norwegian") || langLower == "no")
						systemLanguages.Append($"\t\t\tSystemLanguage.Norwegian => \"{lang}\",").AppendLine();
					else if (langLower.Contains("polish") || langLower == "pl")
						systemLanguages.Append($"\t\t\tSystemLanguage.Polish => \"{lang}\",").AppendLine();
					else if (langLower.Contains("romanian") || langLower == "ro")
						systemLanguages.Append($"\t\t\tSystemLanguage.Romanian => \"{lang}\",").AppendLine();
					else if (langLower.Contains("slovak") || langLower == "sk")
						systemLanguages.Append($"\t\t\tSystemLanguage.Slovak => \"{lang}\",").AppendLine();
					else if (langLower.Contains("swedish") || langLower == "sv")
						systemLanguages.Append($"\t\t\tSystemLanguage.Swedish => \"{lang}\",").AppendLine();
					else if (langLower.Contains("ukrainian") || langLower == "uk")
						systemLanguages.Append($"\t\t\tSystemLanguage.Ukrainian => \"{lang}\",").AppendLine();
					else if (langLower.Contains("arabic") || langLower == "ar")
						systemLanguages.Append($"\t\t\tSystemLanguage.Arabic => \"{lang}\",").AppendLine();
				}
				systemLanguages.Append($"\t\t\t_ => \"{m_localizedLanguages[0]}\",").AppendLine();
				languagesDictBuilder.Append($"}};{Environment.NewLine}");
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

				if (!m_writer.TryLoadTemplate(SheetXConstants.LOCALIZATION_MANAGER_TEMPLATE, out string fileContent))
					return;
				fileContent = fileContent.Replace("//LOCALIZATION_INIT_ASYNC", initAsynLines.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_INIT", initLines.ToString());
				fileContent = fileContent.Replace("//LOCALIZED_DICTIONARY", languagesDictBuilder.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_SET_FOLDER", setFolder.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_USE_ADDRESSABLE", useAddressable.ToString());
				fileContent = fileContent.Replace("//LOCALIZATION_SYSTEM_LANGUAGE", systemLanguages.ToString());
				fileContent = fileContent.Replace("LOCALIZATION_FOLDER", m_settings.GetLocalizationFolder(out bool isAddressable));
				fileContent = fileContent.Replace("IS_ADDRESSABLE", isAddressable.ToString().ToLower());
				fileContent = SheetXHelper.AddNamespace(fileContent, m_settings.@namespace);
				m_writer.Write(m_settings.constantsOutputFolder, "LocalizationsManager.cs", fileContent,
					SheetXExportFileType.LocalizationManager, "Exported LocalizationsManager.cs!");
			}
		}

#endregion

#region Export Json

		public void ExportJson()
		{
			if (!TryOpenWorkbook(out var workBook))
				return;
			ExportJson(workBook);
		}

		/// <summary>Exports JSON from a workbook the caller already opened.</summary>
		internal void ExportJson(IWorkbook workBook)
		{
			var sheets = m_settings.excelSheetsPath.sheets;
			if (!sheets.Any(x => x.selected && SheetXHelper.IsJsonSheet(x.name)))
				return;
			if (string.IsNullOrEmpty(m_settings.jsonOutputFolder))
			{
				bool selectedConfig = m_settings.generateConfigScriptableObject
					&& sheets.Any(sheet => sheet.selected
						&& string.Equals(sheet.name, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal));
				if (selectedConfig && m_settings.encryptJson)
					m_writer.Error("Generate Config ScriptableObject cannot be used with Encrypt Json.");
				else
				{
					m_writer.Error("Please setup the Json Output folder!");
					if (selectedConfig && string.IsNullOrEmpty(m_settings.constantsOutputFolder))
						m_writer.Error("Please setup the Constants Output Folder!");
				}
				return;
			}
			if (m_allIds == null || m_allIds.Count == 0)
			{
				m_allIds = new Dictionary<string, int>();
				foreach (var sheet in sheets)
					if (sheet.name.EndsWith(SheetXConstants.IDS_SHEET))
						LoadSheetIDsValues(workBook, sheet.name);
			}

			bool writeJsonFileForSingleSheet = !m_settings.combineJson;
			bool configWritten = false;
			string configBaseName = Path.GetFileNameWithoutExtension(m_settings.excelSheetsPath.path).Trim().Replace(" ", "_");
			var allJsons = new Dictionary<string, string>();
			foreach (var sheet in sheets)
			{
				if (!sheet.selected || !SheetXHelper.IsJsonSheet(sheet.name))
					continue;
				// The Config route owns this sheet end to end, so it never reaches the row-array
				// conversion below nor the combined aggregate.
				if (m_settings.generateConfigScriptableObject
					&& string.Equals(sheet.name, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal))
				{
					TryExportConfig(workBook, sheet.name, configBaseName, out bool wroteConfig);
					configWritten |= wroteConfig;
					continue;
				}
				string fileName = sheet.name.Trim().Replace(" ", "_");
				string json = ConvertSheetToJson(workBook, sheet.name, fileName, m_settings.encryptJson, writeJsonFileForSingleSheet);

				//Merge all json into a single file
				if (m_settings.combineJson && json != null)
				{
					if (allJsons.ContainsKey(fileName))
					{
						m_writer.Error($"Could not create single json file {fileName}, because file {fileName} is already exists!");
						continue;
					}
					allJsons.Add(fileName, json);
				}
			}
			// No selected Json sheet means no Json data at all — writing an empty aggregate would hand
			// the caller an artifact it never asked for and overwrite a good one from a prior run.
			if (m_settings.combineJson && allJsons.Count > 0)
			{
				//Build json file for all jsons content. Key-sorted so the output does not depend on sheet order.
				string mergedJson = SheetXHelper.MergeJsonContents(allJsons);
				string mergedFileName = Path.GetFileNameWithoutExtension(m_settings.excelSheetsPath.path).Trim().Replace(" ", "_");
				m_writer.Write(m_settings.jsonOutputFolder, $"{mergedFileName}.txt", mergedJson,
					SheetXExportFileType.Json, m_settings.encryptJson
						? $"Exported encrypted Json data to {mergedFileName}.txt."
						: $"Exported Json data to {mergedFileName}.txt.");
			}
			if (configWritten)
				AssetDatabase.Refresh();
		}

		private bool TryExportConfig(IWorkbook workbook, string sheetName, string baseName, out bool wroteArtifacts)
		{
			wroteArtifacts = false;
			if (!m_settings.generateConfigScriptableObject
				|| !string.Equals(sheetName, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal))
			{
				return false;
			}
			if (m_settings.encryptJson)
			{
				m_writer.Error("Generate Config ScriptableObject cannot be used with Encrypt Json.");
				return true;
			}

			bool foldersValid = true;
			if (string.IsNullOrEmpty(m_settings.jsonOutputFolder))
			{
				m_writer.Error("Please setup the Json Output folder!");
				foldersValid = false;
			}
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				m_writer.Error("Please setup the Constants Output Folder!");
				foldersValid = false;
			}
			if (!foldersValid)
				return true;

			string typeName = baseName + "Config";
			if (!SheetXConfigSheet.TryParse(ReadConfigTable(workbook, sheetName), typeName, m_writer.Error, out var data))
				return true;

			m_writer.Write(m_settings.jsonOutputFolder, $"{typeName}.txt", SheetXConfigSheet.EmitJson(data),
				SheetXExportFileType.Json, $"Exported Config data to {typeName}.txt.");
			m_writer.Write(m_settings.constantsOutputFolder, $"{typeName}.cs",
				SheetXConfigSheet.EmitCSharp(data, typeName, m_settings.@namespace),
				SheetXExportFileType.ConfigScript, $"Exported {typeName}.cs!");

			string fullTypeName = string.IsNullOrEmpty(m_settings.@namespace)
				? typeName
				: $"{m_settings.@namespace}.{typeName}";
			SheetXConfigAssetBuilder.RegisterPendingAsset(
				fullTypeName,
				$"{m_settings.jsonOutputFolder.TrimEnd('/', '\\')}/{typeName}.txt",
				m_settings.constantsOutputFolder.Replace('\\', '/'));
			wroteArtifacts = true;
			return true;
		}

		private static List<string[]> ReadConfigTable(IWorkbook workbook, string sheetName)
		{
			var table = new List<string[]>();
			var sheet = workbook.GetSheet(sheetName);
			if (sheet == null)
				return table;

			for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
			{
				var values = new string[4];
				var row = sheet.GetRow(rowIndex);
				for (int columnIndex = 0; columnIndex < values.Length; columnIndex++)
				{
					var cell = row?.GetCell(columnIndex);
					if (cell == null)
					{
						values[columnIndex] = "";
						continue;
					}
					string value = SheetXHelper.ConvertFormulaCell(cell);
					if (value == null)
						value = cell.ToCellString();
					values[columnIndex] = value?.Trim() ?? "";
				}
				table.Add(values);
			}
			return table;
		}

		private string ConvertSheetToJson(IWorkbook pWorkBook, string pSheetName, string pFileName, bool pEncrypt, bool pWriteFile)
		{
			var fieldValueTypes = SheetXHelper.GetFieldValueTypes(pWorkBook, pSheetName);
			if (fieldValueTypes == null)
				return "{}";
			return ConvertSheetToJson(pWorkBook, pSheetName, pFileName, fieldValueTypes, pEncrypt, pWriteFile);
		}

		private string ConvertSheetToJson(IWorkbook pWorkBook, string pSheetName, string pOutputFile, List<FieldValueType> pFieldValueTypes, bool pEncrypt, bool pAutoWriteFile)
		{

			var sheet = pWorkBook.GetSheet(pSheetName);
			if (sheet == null || sheet.LastRowNum == 0)
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
				return null;
			}

			var persistentFields = m_settings.GetPersistentFields();
			int lastCellNum = 0;
			string[] fields = null;
			string[] mergeValues = null;
			var rowContents = new List<RowContent>();

			for (int row = 0; row <= sheet.LastRowNum; row++)
			{
				var rowValues = sheet.GetRow(row);
				if (rowValues == null)
					continue;

				if (row == 0) // Set column header
				{
					lastCellNum = rowValues.LastCellNum;
					fields = new string[lastCellNum];
					mergeValues = new string[lastCellNum];
					string mergedCell = "";
					//Find valid columns
					for (int col = 0; col < lastCellNum; col++)
					{
						var cell = rowValues.GetCell(col);
						if (cell == null)
							continue;
						var cellValue = cell.ToString().Trim();
						if (cell.IsMergedCell && !string.IsNullOrEmpty(cellValue))
							mergedCell = cellValue;
						else if (cell.IsMergedCell && string.IsNullOrEmpty(cellValue))
							cellValue = mergedCell;
						if ((!string.IsNullOrEmpty(cellValue) || cell.IsMergedCell) && !cellValue.EndsWith("[x]"))
						{
							fields[col] = cellValue;
						}
						else
						{
							fields[col] = "";
						}
						mergeValues[col] = "";
					}
				}
				else // Set column value
				{
					var rowContent = new RowContent();
					for (int col = 0; col < lastCellNum; col++)
					{
						var cell = rowValues.GetCell(col);
						if (cell == null)
							continue;
						if (fields != null)
						{
							string fieldName = fields[col];
							if (string.IsNullOrEmpty(fieldName))
								continue;
							string fieldValue = cell.ToCellString().Trim();

							if (cell != null && cell.IsMergedCell && !string.IsNullOrEmpty(fieldValue))
								mergeValues[col] = fieldValue;
							if (cell != null && cell.IsMergedCell && string.IsNullOrEmpty(fieldValue))
								fieldValue = mergeValues[col];

							fieldName = fieldName.Replace(" ", "_");
							rowContent.fieldNames.Add(fieldName);
							rowContent.fieldValues.Add(fieldValue);
						}
					}
					rowContents.Add(rowContent);
				}
			}

			// Initialize array columns from columns with identical names. All columns sharing the same name will be combined into a single array column.
			var combinedCols = new Dictionary<string, string>();
			var sameNameCols = new Dictionary<string, int>();
			foreach (var fieldValueType in pFieldValueTypes)
			{
				string fieldName = fieldValueType.name.Replace("[]", "").Replace("{}", "");
				sameNameCols.TryAdd(fieldName, 0);
				sameNameCols[fieldName] += 1;
				// Initialize the list in the dictionary if count exceeds 1
				if (sameNameCols[fieldName] == 2) // Only initialize when count reaches 2
					combinedCols.Add(fieldName, $"\"{fieldName}\":[");
			}

			string content = "[";
			for (int i = 0; i < rowContents.Count; i++) // Rows
			{
				var rowContent = rowContents[i];

				var attributes = new List<Att>();
				string fieldContentStr = "";
				bool rowIsEmpty = true; //Because Loading sheet sometime includes the empty rows, I don't know why it happen
				var nestedObjects = new List<JObject>();
				foreach (var key in combinedCols.Keys.ToList())
					combinedCols[key] = $"\"{key}\":[";
				for (int j = 0; j < rowContent.fieldNames.Count; j++) // Columns
				{
					string fieldName = rowContent.fieldNames[j];
					var filedValueType = pFieldValueTypes.Find(x => x.name == fieldName);
					if (filedValueType == null)
						continue;
					var fieldType = filedValueType.type;
					string fieldValue = rowContent.fieldValues[j];
					string fieldNameTrim = fieldName.Replace("[]", "").Replace("{}", "");
					bool isAttribute = fieldNameTrim.ToLower().Contains("attribute") && fieldNameTrim.Length <= 11;

					// Encountered a situation where the data contains an attribute field like "value". This causes confusion for the Exporter, 
					// which mistakes it for an attribute from the Attribute System.
					// Solution: Add a condition to ensure the next field is a value.
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

					// The Attributes System contains the following fields: attribute, value/value[], increase/increase[], max/max[], unlock/unlock[].
					// To ensure proper functionality, all these fields must be positioned at the end of the data sheet.
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
								bool isArray = fieldName.EndsWith("[]");
								j++;
								if (!isArray)
								{
									if (!SheetXHelper.TryParseFloat(fieldValue, out att.unlock))
										att.unlock = GetReferenceId(fieldValue, out found);
								}
								else
								{
									string[] inValues = SheetXHelper.SplitValueToArray(fieldValue, false);
									float[] outValues = new float[inValues.Length];
									for (int t = 0; t < inValues.Length; t++)
									{
										if (!SheetXHelper.TryParseFloat(inValues[t].Trim(), out outValues[t]))
											outValues[t] = GetReferenceId(inValues[t].Trim(), out found);
									}
									att.unlocks = outValues;
								}
							}
							else if (fieldName.ToLower().Contains("increase"))
							{
								bool isArray = fieldName.EndsWith("[]");
								j++;
								if (!isArray)
								{
									if (!SheetXHelper.TryParseFloat(fieldValue, out att.increase))
										att.increase = GetReferenceId(fieldValue, out found);
								}
								else
								{
									string[] inValues = SheetXHelper.SplitValueToArray(fieldValue, false);
									float[] outValues = new float[inValues.Length];
									for (int t = 0; t < inValues.Length; t++)
									{
										if (!SheetXHelper.TryParseFloat(inValues[t].Trim(), out outValues[t]))
											outValues[t] = GetReferenceId(inValues[t].Trim(), out found);
									}
									att.increases = outValues;
								}
							}
							else if (fieldName.ToLower().Contains("value"))
							{
								bool isArray = fieldName.EndsWith("[]"); //If attribute value is array
								j++;
								if (!isArray)
								{
									if (!SheetXHelper.TryParseFloat(fieldValue, out att.value))
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
										if (!SheetXHelper.TryParseFloat(inValues[t].Trim(), out outValues[t]))
											outValues[t] = GetReferenceId(inValues[t].Trim(), out found);
									}
									if (outValues.Length == 1 && outValues[0] == 0)
										outValues = null;
									att.values = outValues;
								}
							}
							else if (fieldName.ToLower().Contains("max"))
							{
								bool isArray = fieldName.EndsWith("[]");
								j++;
								if (!isArray)
								{
									if (!SheetXHelper.TryParseFloat(fieldValue, out att.max))
										att.max = GetReferenceId(fieldValue, out found);
								}
								else
								{
									string[] inValues = SheetXHelper.SplitValueToArray(fieldValue, false);
									float[] outValues = new float[inValues.Length];
									for (int t = 0; t < inValues.Length; t++)
									{
										if (!SheetXHelper.TryParseFloat(inValues[t].Trim(), out outValues[t]))
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
						bool importantField = persistentFields.Contains(fieldNameTrim);

						// Exclude empty cell
						if (string.IsNullOrEmpty(fieldValue) && !importantField)
							continue;

						bool nestedField = fieldNameTrim.Contains(".");
						bool referencedId = false;
						if (fieldType == ValueType.Text) //Find and replace string value with referenced ID
						{
							if (CheckExistedId(fieldValue))
							{
								fieldType = ValueType.Number;
								referencedId = true;
							}
							else if (SheetXHelper.TryParseInt(fieldValue, out int _))
							{
								fieldType = ValueType.Number;
								referencedId = true;
							}
						}
						if (fieldType == ValueType.ArrayText) //Find and replace string value with referenced ID
						{
							string[] arrayValue = SheetXHelper.SplitValueToArray(fieldValue, false);
							foreach (string val in arrayValue)
							{
								if (CheckExistedId(val.Trim()))
								{
									fieldType = ValueType.ArrayNumber;
									referencedId = true;
									break;
								}
							}
						}

						void AppendCombinedCols(string value, ValueType type)
						{
							var splits = SheetXHelper.SplitValueToArray(value);
							if (splits.Length > 1)
							{
								var arrayStr = "[";
								switch (type)
								{
									case ValueType.Number:
										for (int k = 0; k < splits.Length; k++)
										{
											string val = splits[k].Trim();
											if (referencedId)
												val = SheetXHelper.FormatInt(GetReferenceId(val, out bool _));
											if (k == 0) arrayStr += val;
											else arrayStr += "," + val;
										}
										arrayStr += "]";
										break;
									case ValueType.Text:
										for (int k = 0; k < splits.Length; k++)
										{
											if (k == 0) arrayStr += $"\"{splits[k]}\"";
											else arrayStr += $",\"{splits[k]}\"";
										}
										arrayStr += "]";
										break;
									case ValueType.Bool:
										for (int k = 0; k < splits.Length; k++)
										{
											if (k == 0) arrayStr += splits[k].ToLower();
											else arrayStr += "," + splits[k].ToLower();
										}
										arrayStr += "]";
										break;
								}
								combinedCols[fieldNameTrim] += $"{arrayStr},";
							}
							else
							{
								if (type == ValueType.Text)
									value = $"\"{value}\"";
								combinedCols[fieldNameTrim] += $"{value},";
							}
						}

						var jsonObject = new JObject();
						switch (fieldType)
						{
							case ValueType.Number:
								if (referencedId)
								{
									int intValue = GetReferenceId(fieldValue, out bool _);
									if (!combinedCols.ContainsKey(fieldNameTrim))
									{
										if (!nestedField)
											fieldContentStr += $"\"{fieldNameTrim}\":{intValue},";
									}
									else
										AppendCombinedCols(fieldValue, fieldType);
									if (nestedField)
										jsonObject[fieldNameTrim] = intValue;
								}
								else
								{
									if (!combinedCols.ContainsKey(fieldNameTrim))
									{
										if (!nestedField)
											fieldContentStr += $"\"{fieldNameTrim}\":{fieldValue},";
									}
									else
										AppendCombinedCols(fieldValue, fieldType);
									if (nestedField)
										jsonObject[fieldNameTrim] = fieldValue;
								}
								break;

							case ValueType.Text:
								fieldValue = fieldValue.Replace("\n", "\\n").Replace("\"", "\\\"");
								if (!combinedCols.ContainsKey(fieldNameTrim))
								{
									if (!nestedField)
										fieldContentStr += $"\"{fieldNameTrim}\":\"{fieldValue}\",";
								}
								else
									AppendCombinedCols(fieldValue, fieldType);
								if (nestedField)
									jsonObject[fieldNameTrim] = fieldValue;
								break;

							case ValueType.Bool:
								fieldValue = fieldValue.ToLower();
								if (!combinedCols.ContainsKey(fieldNameTrim))
								{
									if (!nestedField)
										fieldContentStr += $"\"{fieldNameTrim}\":{fieldValue},";
								}
								else
									AppendCombinedCols(fieldValue, fieldType);
								if (nestedField)
									jsonObject[fieldNameTrim] = fieldValue;
								break;

							case ValueType.ArrayNumber:
							{
								var splits = SheetXHelper.SplitValueToArray(fieldValue, false);
								var arrayStr = "[";
								for (int k = 0; k < splits.Length; k++)
								{
									string val = splits[k].Trim();
									if (referencedId)
										val = SheetXHelper.FormatInt(GetReferenceId(val, out bool _));
									if (k == 0) arrayStr += val;
									else arrayStr += "," + val;
								}
								arrayStr += "]";
								if (!combinedCols.ContainsKey(fieldNameTrim))
								{
									if (!nestedField)
										fieldContentStr += $"\"{fieldNameTrim}\":{arrayStr},";
									if (nestedField)
									{
										int[] array = JsonConvert.DeserializeObject<int[]>(arrayStr);
										jsonObject[fieldNameTrim] = JArray.FromObject(array);
									}
								}
								else
								{
									combinedCols[fieldNameTrim] += $"{arrayStr},";
								}
								break;
							}

							case ValueType.ArrayText:
							{
								var splits = SheetXHelper.SplitValueToArray(fieldValue, false);
								var arrayStr = "[";
								for (int k = 0; k < splits.Length; k++)
								{
									if (k == 0) arrayStr += $"\"{splits[k]}\"";
									else arrayStr += $",\"{splits[k]}\"";
								}
								arrayStr += "]";
								if (!combinedCols.ContainsKey(fieldNameTrim))
								{
									if (!nestedField)
										fieldContentStr += $"\"{fieldNameTrim}\":{arrayStr},";
									if (nestedField)
									{
										string[] array = JsonConvert.DeserializeObject<string[]>(arrayStr);
										jsonObject[fieldNameTrim] = JArray.FromObject(array);
									}
								}
								else
								{
									combinedCols[fieldNameTrim] += $"{arrayStr},";
								}
								break;
							}

							case ValueType.ArrayBool:
							{
								var splits = SheetXHelper.SplitValueToArray(fieldValue, false);
								var arrayStr = "[";
								for (int k = 0; k < splits.Length; k++)
								{
									if (k == 0) arrayStr += splits[k].ToLower();
									else arrayStr += "," + splits[k].ToLower();
								}
								arrayStr += "]";
								if (!combinedCols.ContainsKey(fieldNameTrim))
								{
									if (!nestedField)
										fieldContentStr += $"\"{fieldNameTrim}\":{arrayStr},";
									if (nestedField)
									{
										bool[] array = JsonConvert.DeserializeObject<bool[]>(arrayStr);
										jsonObject[fieldNameTrim] = JArray.FromObject(array);
									}
								}
								else
								{
									combinedCols[fieldNameTrim] += $"{arrayStr},";
								}
								break;
							}

							case ValueType.Json:
							{
								//Search Id in field value
								if (m_allIDsSorted == null || m_allIDsSorted.Count == 0)
								{
									m_allIDsSorted = SheetXHelper.SortIDsByLength(m_allIds);
								}
								foreach (var id in m_allIDsSorted)
								{
									if (fieldValue.Contains(id.Key))
										fieldValue = fieldValue.Replace(id.Key, SheetXHelper.FormatInt(id.Value));
								}
								if (!SheetXHelper.IsValidJson(fieldValue))
								{
									// Skip the field instead of falling through: DeserializeObject would throw on
									// the same text, taking down the whole export instead of one cell.
									if (m_batchState != null)
										m_writer.Error($"Invalid Json string at Sheet: {pSheetName} Field: {fieldNameTrim} Row: {i + 1}");
									else
										m_writer.Blocking("Error", $"Invalid Json string at Sheet: {pSheetName} Field: {fieldNameTrim} Row: {i + 1}");
									break;
								}
								var tempObj = JsonConvert.DeserializeObject(fieldValue);
								var tempJsonStr = JsonConvert.SerializeObject(tempObj);
								if (!combinedCols.ContainsKey(fieldNameTrim))
								{
									if (!nestedField)
										fieldContentStr += $"\"{fieldNameTrim}\":{tempJsonStr},";
								}
								else
									combinedCols[fieldNameTrim] += $"{tempJsonStr},";
								if (nestedField)
									jsonObject[fieldNameTrim] = JObject.Parse(tempJsonStr);
								break;
							}
						}

						// Nested Object
						if (nestedField)
							nestedObjects.Add(jsonObject);
					}
				}
				foreach (var combinedCol in combinedCols)
					fieldContentStr += $"{SheetXHelper.CloseCombinedColumn(combinedCol.Value)},";
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
					content += $"{{{fieldContentStr}}},";
			}
			content = SheetXHelper.RemoveLast(content, ",");
			content += "]";
			if (content == "[]")
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
				return null;
			}
			string finalContent = content;
			if (pEncrypt)
				finalContent = m_settings.GetEncryption().Encrypt(content);

			if (pAutoWriteFile)
			{
				m_writer.Write(m_settings.jsonOutputFolder, $"{pOutputFile}.txt", finalContent,
					SheetXExportFileType.Json, pEncrypt
						? $"Exported encrypted Json data to {pOutputFile}.txt."
						: $"Exported Json data to {pOutputFile}.txt.");
			}
			return finalContent;
		}

#endregion

		/// <summary>
		/// Exports every artifact kind of the configured Excel file. The file is read once — each
		/// artifact kind used to re-open it, so a workbook edited mid-export could produce IDs from
		/// one revision and JSON from another.
		/// </summary>
		public void ExportAll()
		{
			if (!TryOpenWorkbook(out var workBook))
				return;
			ExportAll(workBook);
		}

		/// <summary>Exports every artifact kind from a workbook the caller already opened.</summary>
		internal void ExportAll(IWorkbook workBook)
		{
			ExportIDs(workBook);
			ExportConstants(workBook);
			ExportJson(workBook);
			ExportLocalizations(workBook);
		}

		// One place to turn a missing or unreadable file into a diagnostic. ExcelSheetsPath cannot
		// report either — it has no writer — so it returns null and the reason is reconstructed here.
		private bool TryOpenWorkbook(out IWorkbook pWorkbook)
		{
			pWorkbook = null;
			string path = m_settings.excelSheetsPath?.path;
			if (string.IsNullOrEmpty(path))
			{
				m_writer.Error("No Excel file is selected.");
				return false;
			}
			if (!File.Exists(path))
			{
				m_writer.Error($"{path} does not exist.");
				return false;
			}
			try
			{
				pWorkbook = m_settings.excelSheetsPath.GetWorkBook();
			}
			catch (Exception ex)
			{
				m_writer.Error($"Could not read '{path}': {ex.Message}");
				return false;
			}
			if (pWorkbook == null)
			{
				m_writer.Error($"Could not read '{path}'.");
				return false;
			}
			return true;
		}

		public void ExportAllFiles()
		{
			m_idsBuilderDict = new Dictionary<string, StringBuilder>();
			m_constantsBuilderDict = new Dictionary<string, StringBuilder>();
			m_localizationsDict = new Dictionary<string, LocalizationBuilder>();
			m_allIDsSorted = null;
			m_allIds = new Dictionary<string, int>();
			m_localizedSheetsExported = new List<string>();
			m_localizedLanguages = new List<string>();
			m_langCharSets = new Dictionary<string, string>();
			m_langCharSetsAll = new StringBuilder();
			bool configWritten = false;
			bool configEncryptionErrorReported = false;

			//Process all IDs sheets first
			foreach (var file in m_settings.excelSheetsPaths)
			{
				var workBook = file.GetWorkBook();
				if (workBook == null)
					continue;

				//Load and write Ids
				foreach (var sheet in file.sheets)
				{
					string sheetName = sheet.name;
					if (sheetName.EndsWith(SheetXConstants.IDS_SHEET) && workBook.GetSheet(sheetName) != null)
						LoadSheetIDsValues(workBook, sheetName);
				}
			}

			//Then process other type of sheets
			foreach (var file in m_settings.excelSheetsPaths)
			{
				if (!file.selected)
					continue;

				var workBook = file.GetWorkBook();
				if (workBook == null)
					continue;

				//Load and write Ids
				foreach (var sheet in file.sheets)
				{
					if (!sheet.selected || !sheet.name.EndsWith(SheetXConstants.IDS_SHEET) || workBook.GetSheet(sheet.name) == null)
						continue;
					if (BuildContentOfFileIDs(workBook, sheet.name) && m_settings.separateIDs)
						m_writer.CreateFileIDs(sheet.name, m_idsBuilderDict[sheet.name].ToString());
				}

				//Load and write json file
				var allJsons = new Dictionary<string, string>();
				string configBaseName = Path.GetFileNameWithoutExtension(file.path).Trim().Replace(" ", "_");
				foreach (var sheet in file.sheets)
				{
					if (!sheet.selected || !SheetXHelper.IsJsonSheet(sheet.name) || workBook.GetSheet(sheet.name) == null)
						continue;

					// The Config route owns this sheet end to end, so it never reaches the row-array
					// conversion below nor the combined aggregate.
					if (m_settings.generateConfigScriptableObject
						&& string.Equals(sheet.name, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal))
					{
						if (m_settings.encryptJson && configEncryptionErrorReported)
							continue;
						TryExportConfig(workBook, sheet.name, configBaseName, out bool wroteConfig);
						configWritten |= wroteConfig;
						configEncryptionErrorReported |= m_settings.encryptJson;
						continue;
					}

					string fileName = sheet.name.Trim().Replace(" ", "_");
					string json = ConvertSheetToJson(workBook, sheet.name, fileName, m_settings.encryptJson, !m_settings.combineJson);

					if (m_settings.combineJson && json != null)
					{
						if (allJsons.ContainsKey(fileName))
						{
							m_writer.Error($"Could not create single Json file {fileName}, because key {fileName} is already exists!");
							continue;
						}
						allJsons.Add(fileName, json);
					}
				}

				if (m_settings.combineJson && allJsons.Count > 0)
				{
					//Build json file for all jsons content
					string mergedJson = SheetXHelper.MergeJsonContents(allJsons);
					string mergedFileName = Path.GetFileNameWithoutExtension(file.path).Trim().Replace(" ", "_");
					m_writer.Write(m_settings.jsonOutputFolder, $"{mergedFileName}.txt", mergedJson,
						SheetXExportFileType.Json, m_settings.encryptJson
							? $"Exported encrypted Json data to {mergedFileName}.txt."
							: $"Exported Json data to {mergedFileName}.txt.");
				}

				//Load and write constants
				foreach (var sheet in file.sheets)
				{
					if (!sheet.selected || !sheet.name.EndsWith(SheetXConstants.CONSTANTS_SHEET) || workBook.GetSheet(sheet.name) == null)
						continue;

					LoadSheetConstantsData(workBook, sheet.name);

					if (m_constantsBuilderDict.ContainsKey(sheet.name) && m_settings.separateConstants)
						m_writer.CreateFileConstants(m_constantsBuilderDict[sheet.name].ToString(), sheet.name);
				}
				//Load and write localizations
				foreach (var sheet in file.sheets)
				{
					if (!sheet.selected || !sheet.name.StartsWith(SheetXConstants.LOCALIZATION_SHEET) || workBook.GetSheet(sheet.name) == null)
						continue;

					LoadSheetLocalizationData(workBook, sheet.name);

					if (m_localizationsDict.ContainsKey(sheet.name) && m_settings.separateLocalizations)
					{
						var builder = m_localizationsDict[sheet.name];
						CreateLocalizationFile(builder.idsString, builder.languageTextDict, sheet.name);
						m_localizedSheetsExported.Add(sheet.name);
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
				m_writer.CreateFileIDs("IDs", builder.ToString());
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
				m_writer.CreateFileConstants(builder.ToString(), "Constants");
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

			//Create localization manager file
			CreateLocalizationsManagerFile();

			// One refresh for every workbook: the pending assets resolve after the single reload it triggers.
			if (configWritten)
				AssetDatabase.Refresh();

			m_writer.Info("Done!");
		}

		private bool CheckExistedId(string pKey)
		{
			foreach (var id in m_allIds)
				if (id.Key == pKey.Trim())
					return true;
			return false;
		}

		internal void BatchBuildIds(
			IWorkbook workbook, int sourceIndex, string sheetName)
		{
			m_idsBuilderDict.Clear();
			BuildContentOfFileIDs(workbook, sheetName);
			if (m_idsBuilderDict.TryGetValue(sheetName, out var builder))
			{
				m_batchState.IdsBuilders[
					new SheetXBatchSheetKey(sourceIndex, sheetName)] = builder;
			}
			if (m_settings.separateIDs
				&& m_idsBuilderDict.TryGetValue(sheetName, out var sepBuilder))
			{
				m_writer.CreateFileIDs(sheetName, sepBuilder.ToString());
			}
		}

		internal void BatchBuildConstants(
			IWorkbook workbook, int sourceIndex, string sheetName)
		{
			m_constantsBuilderDict.Clear();
			LoadSheetConstantsData(workbook, sheetName);
			if (m_constantsBuilderDict.TryGetValue(sheetName, out var builder))
			{
				m_batchState.ConstantsBuilders[
					new SheetXBatchSheetKey(sourceIndex, sheetName)] = builder;
			}
			if (m_settings.separateConstants
				&& m_constantsBuilderDict.TryGetValue(sheetName, out var sepBuilder))
			{
				m_writer.CreateFileConstants(sepBuilder.ToString(), sheetName);
			}
		}

		internal void BatchBuildLocalization(
			IWorkbook workbook, int sourceIndex, string sheetName)
		{
			m_localizationsDict.Clear();
			LoadSheetLocalizationData(workbook, sheetName);
			if (m_localizationsDict.TryGetValue(sheetName, out var builder))
			{
				m_batchState.Localizations[
					new SheetXBatchSheetKey(sourceIndex, sheetName)] = builder;
			}
			if (m_settings.separateLocalizations
				&& m_localizationsDict.TryGetValue(sheetName, out var sepBuilder)
				&& sepBuilder.languageTextDict.Count > 0)
			{
				CreateLocalizationFile(
					sepBuilder.idsString, sepBuilder.languageTextDict, sheetName);
				m_localizedSheetsExported.Add(sheetName);
			}
		}

		internal void BatchBuildJson(
			IWorkbook workbook, int sourceIndex, string sheetName,
			string outputName, bool combine)
		{
			string fileName = sheetName.Trim().Replace(" ", "_");
			string json = ConvertSheetToJson(
				workbook, sheetName, fileName,
				m_settings.encryptJson, !combine);
			if (combine && json != null)
			{
				if (!m_batchState.CombinedJsons.TryGetValue(
					sourceIndex, out var dict))
				{
					dict = new Dictionary<string, string>(StringComparer.Ordinal);
					m_batchState.CombinedJsons[sourceIndex] = dict;
				}
				dict[fileName] = json;
			}
		}

		internal void BatchEmitCombinedJson(int sourceIndex, string outputName)
		{
			if (!m_batchState.CombinedJsons.TryGetValue(sourceIndex, out var jsons)
				|| jsons.Count == 0)
				return;
			string merged = SheetXHelper.MergeJsonContents(jsons);
			string mergedFileName = outputName.Trim().Replace(" ", "_");
			m_writer.Write(m_settings.jsonOutputFolder,
				$"{mergedFileName}.txt", merged,
				SheetXExportFileType.Json,
				m_settings.encryptJson
					? $"Exported encrypted Json data to {mergedFileName}.txt."
					: $"Exported Json data to {mergedFileName}.txt.");
		}

		internal void BatchEmitAggregateIds(
			IReadOnlyList<SheetXBatchSheetKey> order)
		{
			var content = new StringBuilder();
			foreach (var key in order)
			{
				if (m_batchState.IdsBuilders.TryGetValue(key, out var builder))
					content.Append(builder).AppendLine();
			}
			m_writer.CreateFileIDs("IDs", content.ToString());
		}

		internal void BatchEmitAggregateConstants(
			IReadOnlyList<SheetXBatchSheetKey> order)
		{
			var content = new StringBuilder();
			int count = 0;
			foreach (var key in order)
			{
				if (!m_batchState.ConstantsBuilders.TryGetValue(
					key, out var builder))
					continue;
				content.Append(builder);
				if (count < order.Count - 1)
					content.AppendLine();
				count++;
			}
			m_writer.CreateFileConstants(content.ToString(), "Constants");
		}

		internal void BatchEmitAggregateLocalizations(
			IReadOnlyList<SheetXBatchSheetKey> order)
		{
			var combined = new LocalizationBuilder();
			foreach (var key in order)
			{
				if (!m_batchState.Localizations.TryGetValue(
					key, out var builder))
					continue;
				combined.idsString.AddRange(builder.idsString);
				foreach (var pair in builder.languageTextDict)
				{
					if (!combined.languageTextDict.ContainsKey(pair.Key))
						combined.languageTextDict.Add(pair.Key, new List<string>());
					combined.languageTextDict[pair.Key].AddRange(pair.Value);
				}
			}
			if (combined.languageTextDict.Count > 0)
			{
				CreateLocalizationFile(
					combined.idsString, combined.languageTextDict, "Localization");
				m_localizedSheetsExported.Add("Localization");
			}
		}

		internal void BatchEmitLocalizationsManager()
		{
			CreateLocalizationsManagerFile();
		}
	}
}