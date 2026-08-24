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
	/// <summary>
	/// Handles fetching data from Google Sheets via the API and exporting it to various formats (constants, IDs, localization).
	/// </summary>
	public class GoogleSheetHandler
	{
		private SheetXSettings m_settings;
		private readonly SheetXWriter m_writer;
		private readonly string m_googleClientId;
		private readonly string m_googleClientSecret;
		private readonly bool m_selectAllSheets;
		private List<SheetPath> m_sheets;
		private IEnumerable<SheetPath> Sheets => m_writer.Detached ? m_sheets ?? Enumerable.Empty<SheetPath>() : m_settings.googleSheetsPath.sheets;
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
		private readonly SheetXBatchState m_batchState;

		/// <summary>
		/// Interactive windows always emit typed Config artifacts for an exact "Config" sheet. Detached
		/// and batch requests carry no Config option, so Config stays ordinary row-array Json there.
		/// </summary>
		internal bool ConfigRouteEnabled { get; set; }

		public GoogleSheetHandler(SheetXSettings settings)
			: this(settings, null, null, null, false)
		{
		}

		internal GoogleSheetHandler(SheetXSettings settings, SheetXExportContext context, string googleClientId, string googleClientSecret, bool selectAllSheets)
		{
			m_settings = settings;
			m_writer = new SheetXWriter(settings, context);
			ConfigRouteEnabled = !m_writer.Detached;
			m_googleClientId = googleClientId;
			m_googleClientSecret = googleClientSecret;
			m_selectAllSheets = selectAllSheets;
		}

		internal GoogleSheetHandler(
			SheetXSettings settings,
			SheetXExportContext context,
			string clientId,
			string clientSecret,
			SheetXBatchState batchState)
			: this(settings, context, clientId, clientSecret, false)
		{
			m_batchState = batchState;
			m_allIds = batchState.AllIds;
			m_localizedSheetsExported = batchState.LocalizedSheetsExported;
			m_localizedLanguages = batchState.LocalizedLanguages;
			m_langCharSets = batchState.LangCharSets;
			m_langCharSetsAll = batchState.LangCharSetsAll;
			m_localizationsDict = new Dictionary<string, LocalizationBuilder>();
		}

		// A detached export takes its credentials from the request only: EditorPrefs holds the machine's
		// SheetX window state, which an external caller neither set nor should depend on.
		private string ClientId => m_writer.Detached ? m_googleClientId : m_settings.ObfGoogleClientId;

		private string ClientSecret => m_writer.Detached ? m_googleClientSecret : m_settings.ObfGoogleClientSecret;

		private Spreadsheet GetCacheMetadata(GoogleSheetsPath googleSheetsPath)
		{
			if (m_cachedSpreadsheet.TryGetValue(googleSheetsPath.id, out var metadata))
				return metadata;
			var service = GetService();
			var sheetMetadata = service.Spreadsheets.Get(googleSheetsPath.id).Execute();
			// ValidateSheetPaths adds and removes entries in the settings' sheet list. That is right for
			// the windows, which persist their selection, but a detached export must leave the caller's
			// request untouched — so its selection is resolved into a throwaway list instead.
			if (m_writer.Detached)
				m_sheets = ResolveSheets(sheetMetadata, googleSheetsPath);
			else
				ValidateSheetPaths(sheetMetadata, googleSheetsPath);
			m_cachedSpreadsheet[googleSheetsPath.id] = sheetMetadata;
			return sheetMetadata;
		}

		//======================================

#region Export IDs

		/// <summary>
		/// Fetches ID definitions from Google Sheets (ending with 'IDs') and generates C# constants files.
		/// </summary>
		public void ExportIDs()
		{
			if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
			{
				m_writer.Error("Please setup the Client Id and Client Secret!");
				return;
			}
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath);
			if (!Sheets.Any(x => x.selected && x.name.EndsWith(SheetXConstants.IDS_SHEET)))
				return;
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				m_writer.Error("Please setup the Constants Output Folder!");
				return;
			}
			var service = GetService();

			m_idsBuilderDict = new Dictionary<string, StringBuilder>();
			m_allIds = new Dictionary<string, int>();

			foreach (var sheet in Sheets)
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
				// An empty sheet builds nothing, so there is no builder to read back.
				if (BuildContentOfFileIDs(sheet.name, values) && m_settings.separateIDs)
				{
					var content = m_idsBuilderDict[sheet.name].ToString();
					m_writer.CreateFileIDs(sheet.name, content);
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

		private bool BuildContentOfFileIDs(string pSheetName, IList<IList<object>> rowsData)
		{
			if (rowsData == null || rowsData.Count <= 1)
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
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
						// col steps by 3 but the loop bound is rowData.Count, so col + 1 is out of
						// range whenever a row's trailing value cell is blank -- the API truncates
						// each row at its last non-empty cell.
						var cellValue = col + 1 < rowData.Count ? rowData[col + 1] : null;
						if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
						{
							if (m_batchState != null)
								m_writer.Warn($"Sheet {pSheetName}: Key {key} doesn't have value!");
							else
								m_writer.Blocking("Warning", $"Sheet {pSheetName}: Key {key} doesn't have value!");
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
						// The first definition wins. Appending a second "public const int" for the same
						// key produced C# that does not compile, so a conflict is reported and the row skipped.
						if (m_batchState != null)
						{
							if (!m_batchState.DeclaredIds.Add(key))
								continue;
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
						if (col + 2 < rowData.Count)
						{
							var cellComment = rowData[col + 2];
							if (cellComment != null && !string.IsNullOrEmpty(cellComment.ToString().Trim()))
								sb.Append(" /* ").Append(cellComment).Append(" */");
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

		private Dictionary<string, IList<IList<object>>> GetSheetIDsValues()
		{
			var ids = new Dictionary<string, IList<IList<object>>>();
			var service = GetService();
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath);
			foreach (var sheet in Sheets)
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
			if (rowsData == null || rowsData.Count <= 1)
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
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
					var cellValue = col + 1 < rowData.Count ? rowData[col + 1] : null;
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

			m_allIds = m_allIds.OrderBy(m => m.Key).ToDictionary(x => x.Key, x => x.Value);
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

		private bool CheckExistedId(string pKey)
		{
			foreach (var id in m_allIds)
				if (id.Key == pKey.Trim())
					return true;
			return false;
		}

#endregion

#region Export Constants

		/// <summary>
		/// Fetches constant definitions from Google Sheets (ending with 'Constants') and generates C# constants files.
		/// Supports various types like int, float, string, arrays, and vectors.
		/// </summary>
		public void ExportConstants()
		{
			if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
			{
				m_writer.Error("Please setup the Client Id and Client Secret!");
				return;
			}
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath);
			if (!Sheets.Any(x => x.selected && x.name.EndsWith(SheetXConstants.CONSTANTS_SHEET)))
				return;
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				m_writer.Error("Please setup the Constants Output Folder!");
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
			foreach (var sheet in Sheets)
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
					m_writer.CreateFileConstants(m_constantsBuilderDict[sheet.name].ToString(), sheet.name);
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

		private void LoadSheetConstantsData(string pSheetName, IList<IList<object>> rowsData)
		{
			if (rowsData == null || rowsData.Count == 0)
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
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
		/// Fetches localization data from Google Sheets (starting with 'Localization') and exports it to JSON files and C# dictionaries.
		/// </summary>
		public void ExportLocalizations()
		{
			if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
			{
				m_writer.Error("Please setup the Client Id and Client Secret!");
				return;
			}
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath);
			if (!Sheets.Any(x => x.selected && x.name.StartsWith(SheetXConstants.LOCALIZATION_SHEET)))
				return;
			if (string.IsNullOrEmpty(m_settings.constantsOutputFolder))
			{
				m_writer.Error("Please setup the Constants Output Folder!");
				return;
			}
			if (string.IsNullOrEmpty(m_settings.localizationOutputFolder))
			{
				m_writer.Error("Please setup the Localization Output Folder!");
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
			foreach (var sheet in Sheets)
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

				LoadSheetLocalizationData(sheetInfo, values, sheet.name);

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

		private void LoadSheetLocalizationData(Google.Apis.Sheets.v4.Data.Sheet sheet, IList<IList<object>> rowsData, string pSheetName)
		{
			if (rowsData == null || rowsData.Count == 0)
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
				return;
			}

			var idStrings = new List<string>();
			var textDict = new Dictionary<string, List<string>>();
			var firstRow = rowsData[0];
			int maxCellNum = firstRow.Count;

			string mergeCellValue = "";
			for (int row = 0; row < rowsData.Count; row++)
			{
				var rowData = rowsData[row];
				if (rowData == null || rowData.Count == 0)
					continue;
				for (int col = 0; col < maxCellNum; col++)
				{
					var fieldName = firstRow[col]?.ToString() ?? "";
					// The Sheets API truncates each row at its last non-empty cell, so a row shorter
					// than the header is the normal case, not malformed input.
					string fieldValue = col < rowData.Count ? rowData[col]?.ToString() ?? "" : "";
					bool isMergedCell = SheetXHelper.IsMergedCell(sheet, row, col);
					if (isMergedCell && !string.IsNullOrEmpty(fieldValue))
						mergeCellValue = fieldValue;
					if (isMergedCell && string.IsNullOrEmpty(fieldValue))
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

					if (i < pIdsString.Count - 1)
						idBuilder.Append($"{pIdsString[i].RemoveSpecialCharacters()} = {i}, ");
					else
						idBuilder.Append($"{pIdsString[i].RemoveSpecialCharacters()} = {i};");
				}
			}

			//Build id enum array
			var idBuilder2 = new StringBuilder();
			idBuilder2.Append($"\tpublic enum ID {Environment.NewLine}\t{{{Environment.NewLine}\t\tNONE = -1,");
			idBuilder2.Append($"{Environment.NewLine}\t\t");
			for (int i = 0; i < pIdsString.Count; i++)
			{
				if (i > 0 && i % 100 == 0)
				{
					idBuilder2.Append($"{Environment.NewLine}\t\t");
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
				m_writer.Write(m_settings.localizationOutputFolder, $"{pFileName}_{listText.Key}.txt", json, SheetXExportFileType.Localization);
				m_writer.Info($"Exported Localization content to {pFileName}_{listText.Key}.txt!");

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
			m_writer.Write(m_settings.constantsOutputFolder, $"{pFileName}.cs", fileContent, SheetXExportFileType.LocalizationConstants);
			m_writer.Info($"Exported {pFileName}.cs!");

			//Write file localized text component
			if (!m_writer.TryLoadTemplate(SheetXConstants.LOCALIZATION_TEXT_TEMPLATE, out fileContent))
					return;
			fileContent = fileContent.Replace("LOCALIZATION_CLASS_NAME", pFileName);
			fileContent = SheetXHelper.AddNamespace(fileContent, m_settings.@namespace);
			m_writer.Write(m_settings.constantsOutputFolder, $"{pFileName}Text.cs", fileContent, SheetXExportFileType.LocalizationComponent);
			m_writer.Info($"Exported {pFileName}Text.cs!");
		}

		private void CreateLocalizationsManagerFile()
		{
			//Create characters sets
			if (m_langCharSets != null && m_langCharSets.Count > 0)
			{
				var maps = SheetXHelper.GenerateCharacterSets(m_langCharSets);
				foreach (var map in maps)
				{
					m_writer.Write(m_settings.localizationOutputFolder, $"characters_set_{map.Key}.txt", map.Value, SheetXExportFileType.CharacterSet);
					m_writer.Info($"Exported characters_set_{map.Key}.txt");
				}
			}
			if (!string.IsNullOrEmpty(m_langCharSetsAll.ToString()))
			{
				var characterSet = SheetXHelper.GenerateCharacterSet(m_langCharSetsAll.ToString());
				m_writer.Write(m_settings.localizationOutputFolder, "characters_set_all.txt", characterSet, SheetXExportFileType.CharacterSet);
				m_writer.Info("Exported characters_set_all.txt!");
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
				m_writer.Write(m_settings.constantsOutputFolder, "LocalizationsManager.cs", fileContent, SheetXExportFileType.LocalizationManager);
				m_writer.Info("Exported LocalizationsManager.cs!");
			}
		}

#endregion

#region Export Json

		public void ExportJson()
		{
			if (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(ClientSecret))
			{
				m_writer.Error("Please setup the Client Id and Client Secret!");
				return;
			}
			var sheetMetadata = GetCacheMetadata(m_settings.googleSheetsPath);
			var service = GetService();
			string sourceId = m_settings.googleSheetsPath.id;
			string baseName = sheetMetadata.Properties.Title.Replace(" ", "_");
			bool configWritten = false;
			var session = CreateCollectionSession();
			if (ConfigRouteEnabled)
			{
				TryExportConfig(
					sheetMetadata, sourceId, service, baseName,
					out configWritten);
			}

			bool hasJson = Sheets.Any(x => x.selected
				&& SheetXHelper.IsJsonSheet(x.name)
				&& (!ConfigRouteEnabled || !string.Equals(
					x.name, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal)));
			if (hasJson)
				ExportOrdinaryJson(sheetMetadata, service, baseName, session, sourceId);
			FlushCollectionSession(session);
			if ((configWritten || session?.WroteArtifacts == true) && !m_writer.Detached)
				AssetDatabase.Refresh();
			BakeCollectionSession(session);
		}

		private void ExportOrdinaryJson(
			Spreadsheet sheetMetadata,
			SheetsService service,
			string baseName,
			SheetXCollectionExportSession session = null,
			string sourceId = null)
		{
			bool canWriteOrdinaryJson = !HasOrdinaryJsonSheets(sheetMetadata, session, sourceId)
				|| !string.IsNullOrEmpty(m_settings.jsonOutputFolder);
			if (!canWriteOrdinaryJson)
			{
				m_writer.Error("Please setup the Json Output Folder!");
				if (session == null)
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
			foreach (var sheet in Sheets)
			{
				if (!sheet.selected || !SheetXHelper.IsJsonSheet(sheet.name)
					|| ConfigRouteEnabled && string.Equals(
						sheet.name, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal))
				{
					continue;
				}

				var sheetInfo = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheet.name);
				if (sheetInfo == null)
					continue;

				var columnCount = sheetInfo.Properties.GridProperties.ColumnCount;
				var range = $"{sheet.name}!A1:{GetColumnLetter(columnCount.Value)}";
				var request = service.Spreadsheets.Values.Get(sourceId, range);
				var values = request.Execute().Values;
				string fileName = sheet.name.Trim().Replace(" ", "_");
				var mode = session?.ModeOf(sourceId, sheet.name) ?? SheetXSheetOutputMode.JsonOnly;
				if (mode != SheetXSheetOutputMode.JsonOnly)
				{
					AddCollectionSheet(session, sourceId, sheetInfo, values, sheet.name, fileName, mode);
					continue;
				}
				if (!canWriteOrdinaryJson)
					continue;
				string json = ConvertSheetToJson(
					sheetInfo, values, sheet.name, fileName, m_settings.encryptJson, writeJsonFileForSingleSheet);

				if (m_settings.combineJson && json != null)
				{
					if (allJsons.ContainsKey(fileName))
					{
						m_writer.Error($"Could not create single json file {fileName}, because file {fileName} already exists!");
						continue;
					}
					allJsons.Add(fileName, json);
				}
			}
			if (m_settings.combineJson && allJsons.Count > 0)
			{
				string mergedJson = SheetXHelper.MergeJsonContents(allJsons);
				m_writer.Write(m_settings.jsonOutputFolder, $"{baseName}.txt", mergedJson, SheetXExportFileType.Json);
				m_writer.Info(m_settings.encryptJson
					? $"Exported encrypted Json data to {baseName}.txt."
					: $"Exported Json data to {baseName}.txt.");
			}
		}

		// Detached and batch exports carry no settings-backed collection bindings, so they keep the
		// ordinary Json route untouched.
		private SheetXCollectionExportSession CreateCollectionSession()
			=> m_settings.enableCollections && !m_writer.Detached
				? new SheetXCollectionExportSession(m_settings)
				: null;

		private bool HasOrdinaryJsonSheets(
			Spreadsheet metadata,
			SheetXCollectionExportSession session,
			string sourceId)
		{
			foreach (var sheet in Sheets)
			{
				if (!sheet.selected || !SheetXHelper.IsJsonSheet(sheet.name)
					|| metadata.Sheets.All(candidate => !string.Equals(
						candidate.Properties.Title, sheet.name, StringComparison.Ordinal))
					|| ConfigRouteEnabled && string.Equals(
						sheet.name, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal))
				{
					continue;
				}
				if (session == null || session.ModeOf(sourceId, sheet.name) == SheetXSheetOutputMode.JsonOnly)
					return true;
			}
			return false;
		}

		private void AddCollectionSheet(
			SheetXCollectionExportSession session,
			string sourceId,
			Sheet sheet,
			IList<IList<object>> values,
			string sheetName,
			string fileName,
			SheetXSheetOutputMode mode)
		{
			string error;
			switch (mode)
			{
				case SheetXSheetOutputMode.CollectionGeneratedModel:
					ReadCollectionTable(values, out var headers, out var rows);
					if (!session.TryAddGeneratedTable(sourceId, sheetName, headers, rows, out error)
						&& error != null)
					{
						m_writer.Error(error);
					}
					break;

				case SheetXSheetOutputMode.CollectionExistingModel:
					// Collection Json is editor-only bake input, not encrypted output. The session owns its write.
					string legacyJson = ConvertSheetToJson(
						sheet, values, sheetName, fileName, pEncrypt: false, pWriteFile: false);
					if (!session.TryAddExistingTable(sourceId, sheetName, legacyJson, out error)
						&& error != null)
					{
						m_writer.Error(error);
					}
					break;
			}
		}

		private void FlushCollectionSession(SheetXCollectionExportSession session)
		{
			if (session != null && !session.Flush(out string error))
				m_writer.Error(error);
		}

		private void BakeCollectionSession(SheetXCollectionExportSession session)
		{
			if (session?.WroteArtifacts == true && !session.TryBakeAfterRefresh(out string error))
				m_writer.Error(error);
		}

		/// <summary>Converts Sheets API values to fixed-width collection rows.</summary>
		internal static void ReadCollectionTable(
			IList<IList<object>> values,
			out List<string> headers,
			out List<IReadOnlyList<string>> rows)
		{
			headers = new List<string>();
			rows = new List<IReadOnlyList<string>>();
			if (values == null || values.Count == 0 || values[0] == null)
				return;

			var headerRow = values[0];
			for (int col = 0; col < headerRow.Count; col++)
				headers.Add(headerRow[col]?.ToString()?.Trim() ?? "");

			for (int row = 1; row < values.Count; row++)
			{
				var sourceRow = values[row];
				var result = new string[headers.Count];
				for (int col = 0; col < result.Length; col++)
					result[col] = sourceRow != null && col < sourceRow.Count
						? sourceRow[col]?.ToString()?.Trim() ?? ""
						: "";
				rows.Add(result);
			}
		}

		private bool TryExportConfig(
			Spreadsheet metadata, string spreadsheetId, SheetsService service,
			string baseName, out bool wroteArtifacts)
		{
			wroteArtifacts = false;
			var sheet = metadata.Sheets.FirstOrDefault(candidate => string.Equals(
				candidate.Properties.Title, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal));
			if (sheet == null)
				return false;

			int columnCount = sheet.Properties.GridProperties.ColumnCount.Value;
			string range = $"{SheetXConstants.CONFIG_SHEET}!A1:{GetColumnLetter(columnCount)}";
			var values = service.Spreadsheets.Values.Get(spreadsheetId, range).Execute().Values;
			return TryExportConfig(values, SheetXConstants.CONFIG_SHEET, baseName, out wroteArtifacts);
		}

		private bool TryExportConfig(IList<IList<object>> values, string sheetName, string baseName, out bool wroteArtifacts)
		{
			wroteArtifacts = false;
			if (!string.Equals(sheetName, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal))
				return false;

			bool foldersValid = true;
			if (string.IsNullOrEmpty(m_settings.jsonOutputFolder))
			{
				m_writer.Error("Please setup the Json Output Folder!");
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
			if (!SheetXConfigSheet.TryParse(ReadConfigTable(values), typeName, m_writer.Error, out var data))
				return true;

			m_writer.Write(m_settings.jsonOutputFolder, $"{typeName}.txt", SheetXConfigSheet.EmitJson(data),
				SheetXExportFileType.Json, $"Exported Config data to {typeName}.txt.");
			m_writer.Write(m_settings.constantsOutputFolder, $"{typeName}.cs",
				SheetXConfigSheet.EmitCSharp(data, typeName, m_settings.@namespace),
				SheetXExportFileType.ConfigScript, $"Exported {typeName}.cs!");

			if (!m_writer.Detached)
			{
				string fullTypeName = string.IsNullOrEmpty(m_settings.@namespace)
					? typeName
					: $"{m_settings.@namespace}.{typeName}";
				SheetXConfigAssetBuilder.RegisterPendingAsset(
					fullTypeName,
					$"{m_settings.jsonOutputFolder.TrimEnd('/', '\\')}/{typeName}.txt",
					m_settings.constantsOutputFolder.Replace('\\', '/'));
			}
			wroteArtifacts = true;
			return true;
		}

		private static List<string[]> ReadConfigTable(IList<IList<object>> values)
		{
			var table = new List<string[]>();
			if (values == null)
				return table;

			foreach (var row in values)
			{
				var result = new string[4];
				for (int col = 0; col < result.Length; col++)
					result[col] = row != null && col < row.Count ? row[col]?.ToString()?.Trim() ?? "" : "";
				table.Add(result);
			}
			return table;
		}

		private string ConvertSheetToJson(Sheet sheet, IList<IList<object>> pValues, string pSheetName, string pFileName, bool pEncrypt, bool pWriteFile)
		{
			var fieldValueTypes = SheetXHelper.GetFieldValueTypes(sheet, pValues);
			if (fieldValueTypes == null)
				return "{}";
			return ConvertSheetToJson(sheet, pValues, pSheetName, pFileName, fieldValueTypes, pEncrypt, pWriteFile);
		}

		private string ConvertSheetToJson(Google.Apis.Sheets.v4.Data.Sheet sheet, IList<IList<object>> pValues, string pSheetName, string pOutputFile, List<FieldValueType> pFieldValueTypes, bool pEncrypt, bool pAutoWriteFile)
		{
			var persistentFields = m_settings.GetPersistentFields();

			if (pValues == null || pValues.Count == 0)
			{
				m_writer.Warn($"Sheet {pSheetName} is empty!");
				return null;
			}

			int lastCellNum = 0;
			string[] fields = null;
			string[] mergeValues = null;
			var rowContents = new List<RowContent>();

			for (int row = 0; row < pValues.Count; row++)
			{
				var rowValues = pValues[row];
				if (rowValues == null || rowValues.Count == 0)
					continue;

				if (row == 0) // Set column header
				{
					lastCellNum = rowValues.Count;
					fields = new string[lastCellNum];
					mergeValues = new string[lastCellNum];
					string mergedCell = "";
					//Find valid columns
					for (int col = 0; col < lastCellNum; col++)
					{
						var cell = rowValues[col];
						var cellValue = cell.ToString().Trim();

						bool isMergedCell = SheetXHelper.IsMergedCell(sheet, row, col);
						if (isMergedCell && !string.IsNullOrEmpty(cellValue))
							mergedCell = cellValue;
						else if (isMergedCell && string.IsNullOrEmpty(cellValue))
							cellValue = mergedCell;

						if (!string.IsNullOrEmpty(cellValue) && !cellValue.Contains("[x]"))
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
						var cellValue = "";
						if (col < rowValues.Count)
							cellValue = rowValues[col].ToString().Trim();
						if (fields != null)
						{
							string fieldName = fields[col];
							if (string.IsNullOrEmpty(fieldName))
								continue;
							string fieldValue = cellValue;

							bool isMergedCell = SheetXHelper.IsMergedCell(sheet, row, col);
							if (isMergedCell && !string.IsNullOrEmpty(fieldValue))
								mergeValues[col] = fieldValue;
							if (isMergedCell && string.IsNullOrEmpty(fieldValue))
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
			for (int i = 0; i < rowContents.Count; i++)
			{
				var rowContent = rowContents[i];

				var attributes = new List<Att>();
				string fieldContentStr = "";
				bool rowIsEmpty = true; //Because Loading sheet sometime includes the empty rows, I don't know why it happen
				var nestedObjects = new List<JObject>();
				foreach (var key in combinedCols.Keys.ToList())
					combinedCols[key] = $"\"{key}\":[";
				for (int j = 0; j < rowContent.fieldNames.Count; j++)
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

						//Ignore empty field or field have value which equal 0
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
									if (m_batchState != null)
										m_writer.Error($"Invalid Json string at Sheet: {pSheetName} Field: {fieldNameTrim} Row: {i + 1}");
									else
										m_writer.Blocking("Error", $"Invalid Json string at Sheet: {pSheetName} Field: {fieldNameTrim} Row: {i + 1}");
									continue;
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
				m_writer.Write(m_settings.jsonOutputFolder, $"{pOutputFile}.txt", finalContent, SheetXExportFileType.Json);
				if (pEncrypt)
					m_writer.Info($"Exported encrypted Json data to {pOutputFile}.txt.");
				else
					m_writer.Info($"Exported Json data to {pOutputFile}.txt.");
			}
			return finalContent;
		}

#endregion

		public void ExportAll()
		{
			if (m_writer.Detached && !m_selectAllSheets && m_settings.googleSheetsPath.sheets.Count == 0)
				return;

			ExportIDs();
			ExportConstants();
			ExportJson();
			ExportLocalizations();
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
			var session = CreateCollectionSession();

			var service = GetService();
			var googleSheetsPaths = m_settings.googleSheetsPaths;
			//Load and write Ids first
			foreach (var googleSheets in googleSheetsPaths)
			{
				// Get the sheet metadata to determine its dimensions
				var sheetMetadata = service.Spreadsheets.Get(googleSheets.id).Execute();
				ValidateSheetPaths(sheetMetadata, googleSheets);
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
						m_writer.CreateFileIDs(sheet.name, m_idsBuilderDict[sheet.name].ToString());
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
				string configBaseName = ggSheetsMetadata.Properties.Title.Replace(" ", "_");
				if (ConfigRouteEnabled)
				{
					TryExportConfig(
						ggSheetsMetadata, googleSheets.id, service, configBaseName,
						out bool wroteConfig);
					configWritten |= wroteConfig;
				}
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

					//Load and write json file. The Config route already owns an exact "Config" sheet, so it
					//never reaches the row-array conversion below nor the combined aggregate.
					bool ownedByConfigRoute = ConfigRouteEnabled && string.Equals(
						sheet.name, SheetXConstants.CONFIG_SHEET, StringComparison.Ordinal);
					if (!ownedByConfigRoute && SheetXHelper.IsJsonSheet(sheet.name))
					{
						string fileName = sheet.name.Trim().Replace(" ", "_");
						var mode = session?.ModeOf(googleSheets.id, sheet.name)
							?? SheetXSheetOutputMode.JsonOnly;
						if (mode != SheetXSheetOutputMode.JsonOnly)
						{
							AddCollectionSheet(
								session, googleSheets.id, sheetInfo, values, sheet.name, fileName, mode);
						}
						else
						{
							string json = ConvertSheetToJson(
								sheetInfo, values, sheet.name, fileName, m_settings.encryptJson, !m_settings.combineJson);
							if (m_settings.combineJson && json != null)
							{
								if (allJsons.ContainsKey(fileName))
								{
									m_writer.Error($"Could not create single json file {fileName}, because file {fileName} already exists!");
									continue;
								}
								allJsons.Add(fileName, json);
							}
						}
					}


					//Load and write constants
					if (sheet.name.EndsWith(SheetXConstants.CONSTANTS_SHEET))
					{
						LoadSheetConstantsData(sheet.name, values);

						if (m_constantsBuilderDict.ContainsKey(sheet.name) && m_settings.separateConstants)
							m_writer.CreateFileConstants(m_constantsBuilderDict[sheet.name].ToString(), sheet.name);
					}
					//Load and write localizations
					if (sheet.name.StartsWith(SheetXConstants.LOCALIZATION_SHEET))
					{
						LoadSheetLocalizationData(sheetInfo, values, sheet.name);

						if (m_localizationsDict.ContainsKey(sheet.name) && m_settings.separateLocalizations)
						{
							var builder = m_localizationsDict[sheet.name];
							CreateLocalizationFile(builder.idsString, builder.languageTextDict, sheet.name);
							m_localizedSheetsExported.Add(sheet.name);
						}
					}
				}

				if (m_settings.combineJson && allJsons.Count > 0)
				{
					//Build json file for all jsons content. Written once per spreadsheet after every
					//selected sheet is read, and key-sorted, so the output does not depend on sheet order.
					string mergedJson = SheetXHelper.MergeJsonContents(allJsons);
					string mergedFileName = ggSheetsMetadata.Properties.Title.Replace(" ", "_");
					m_writer.Write(m_settings.jsonOutputFolder, $"{mergedFileName}.txt", mergedJson, SheetXExportFileType.Json);

					m_writer.Info(m_settings.encryptJson
						? $"Exported encrypted Json data to {mergedFileName}.txt."
						: $"Exported Json data to {mergedFileName}.txt.");
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

			FlushCollectionSession(session);
			if ((configWritten || session?.WroteArtifacts == true) && !m_writer.Detached)
				AssetDatabase.Refresh();
			BakeCollectionSession(session);

			Debug.Log("Done!");
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
				HttpClientInitializer = SheetXHelper.AuthenticateGoogleUser(ClientId, ClientSecret, m_writer.Detached ? (Action<string>)m_writer.Warn : null),
				ApplicationName = SheetXConstants.APPLICATION_NAME,
			});
			return m_service;
		}

		// Builds the selection a detached export works from, without touching the request. A null sheet
		// list in the request means "every sheet in the spreadsheet"; an empty one means "none", and a
		// name that the spreadsheet does not have is reported rather than silently dropped.
		private List<SheetPath> ResolveSheets(Spreadsheet sheetMetadata, GoogleSheetsPath pGoogleSheetsPath)
		{
			var available = sheetMetadata.Sheets.Select(x => x.Properties.Title).ToList();
			if (m_selectAllSheets)
				return available.Select(x => new SheetPath { name = x, selected = true }).ToList();

			var result = new List<SheetPath>();
			foreach (var sheet in pGoogleSheetsPath.sheets)
			{
				if (!available.Contains(sheet.name))
				{
					m_writer.Error($"Spreadsheet '{pGoogleSheetsPath.id}' has no sheet named '{sheet.name}'.");
					continue;
				}
				result.Add(new SheetPath { name = sheet.name, selected = true });
			}
			return result;
		}

		private void ValidateSheetPaths(Spreadsheet sheetMetadata, GoogleSheetsPath pGoogleSheetsPath)
		{
			var sheetPaths = new List<SheetPath>();
			foreach (var sheet in sheetMetadata.Sheets)
			{
				var sheetName = sheet.Properties.Title;
				sheetPaths.Add(new SheetPath()
				{
					name = sheetName,
					selected = true,
				});
			}

			// Sync with current save
			for (int i = 0; i < pGoogleSheetsPath.sheets.Count; i++)
			{
				var sheetPath = pGoogleSheetsPath.sheets[i];
				if (!sheetPaths.Exists(x => x.name == sheetPath.name))
				{
					pGoogleSheetsPath.sheets.RemoveAt(i);
					i--;
				}
			}
			foreach (var sheetPath in sheetPaths)
			{
				var existedSheet = pGoogleSheetsPath.sheets.Find(x => x.name == sheetPath.name);
				if (existedSheet != null)
					sheetPath.selected = existedSheet.selected;
				else
					pGoogleSheetsPath.AddSheet(sheetPath.name);
			}
		}

		internal bool BatchMaterialize(
			SheetXBatchSourceState source,
			List<string> requestedSheets)
		{
			Spreadsheet metadata;
			try
			{
				metadata = GetService()
					.Spreadsheets.Get(source.SpreadsheetPath).Execute();
			}
			catch (Exception ex)
			{
				m_writer.Error(
					$"Could not read Google spreadsheet '{source.SpreadsheetPath}': "
					+ $"{ex.Message}");
				return false;
			}

			source.Metadata = metadata;

			if (string.IsNullOrEmpty(source.OutputName))
				source.OutputName = metadata.Properties?.Title;

			var requested = requestedSheets == null
				? null
				: new HashSet<string>(requestedSheets, StringComparer.Ordinal);
			var available = new HashSet<string>(StringComparer.Ordinal);

			if (metadata.Sheets != null)
			{
				foreach (var sheet in metadata.Sheets)
				{
					string name = sheet.Properties?.Title;
					if (string.IsNullOrEmpty(name))
						continue;

					available.Add(name);

					if (requested == null || requested.Contains(name))
						source.SelectedSheets.Add(name);
				}
			}

			if (requested == null)
				return true;

			bool ok = true;
			foreach (string name in requestedSheets)
			{
				if (available.Contains(name))
					continue;

				m_writer.Error(
					$"Google spreadsheet '{source.SpreadsheetPath}' "
					+ $"has no sheet '{name}'.");
				ok = false;
			}

			return ok;
		}

		private bool TryFindSheet(
			SheetXBatchSourceState source,
			string sheetName,
			out Sheet sheet)
		{
			sheet = null;
			if (source.Metadata?.Sheets == null)
				return false;

			foreach (var candidate in source.Metadata.Sheets)
			{
				if (string.Equals(
					candidate.Properties?.Title,
					sheetName,
					StringComparison.Ordinal))
				{
					sheet = candidate;
					return true;
				}
			}

			return false;
		}

		private bool TryGetGridRange(
			SheetXBatchSourceState source,
			string sheetName,
			out Sheet sheet,
			out string range)
		{
			range = null;
			if (!TryFindSheet(source, sheetName, out sheet))
			{
				m_writer.Error(
					$"Google spreadsheet '{source.SpreadsheetPath}' "
					+ $"has no sheet '{sheetName}'.");
				return false;
			}

			int? columns = sheet.Properties?.GridProperties?.ColumnCount;
			if (!columns.HasValue)
			{
				m_writer.Error(
					$"Google spreadsheet '{source.SpreadsheetPath}' sheet '{sheetName}' "
					+ "has no grid column count.");
				return false;
			}

			range = $"{sheetName}!A1:{GetColumnLetter(columns.Value)}";
			return true;
		}

		private IList<IList<object>> BatchFetchValues(
			SheetXBatchSourceState source,
			string range)
			=> GetService()
				.Spreadsheets.Values.Get(source.SpreadsheetPath, range)
				.Execute()
				.Values;

		internal void BatchLoadIds(
			SheetXBatchSourceState source, string sheetName)
		{
			if (!TryGetGridRange(source, sheetName, out _, out string range))
				return;

			var rowsData = BatchFetchValues(source, range);
			if (rowsData == null || rowsData.Count <= 1)
			{
				m_writer.Warn($"Sheet {sheetName} is empty!");
				return;
			}

			for (int row = 1; row < rowsData.Count; row++)
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
					if (string.IsNullOrEmpty(key))
						continue;
					var cellValue = col + 1 < rowData.Count ? rowData[col + 1] : null;
					if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
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
						key, value, source.SpreadsheetPath, sheetName, out string error)
						&& error != null)
					{
						m_writer.Error(error);
					}
				}
			}
		}

		internal void BatchBuildIds(
			SheetXBatchSourceState source, string sheetName)
		{
			if (!TryGetGridRange(source, sheetName, out _, out string range))
				return;

			m_idsBuilderDict.Clear();
			BuildContentOfFileIDs(sheetName, BatchFetchValues(source, range));

			if (m_idsBuilderDict.TryGetValue(sheetName, out var builder))
			{
				m_batchState.IdsBuilders[
					new SheetXBatchSheetKey(source.Index, sheetName)] = builder;

				if (m_settings.separateIDs)
					m_writer.CreateFileIDs(sheetName, builder.ToString());
			}
		}

		internal void BatchBuildConstants(
			SheetXBatchSourceState source, string sheetName)
		{
			if (!TryFindSheet(source, sheetName, out _))
			{
				m_writer.Error(
					$"Google spreadsheet '{source.SpreadsheetPath}' "
					+ $"has no sheet '{sheetName}'.");
				return;
			}

			m_constantsBuilderDict.Clear();
			LoadSheetConstantsData(
				sheetName, BatchFetchValues(source, $"{sheetName}!A1:D"));

			if (m_constantsBuilderDict.TryGetValue(sheetName, out var builder))
			{
				m_batchState.ConstantsBuilders[
					new SheetXBatchSheetKey(source.Index, sheetName)] = builder;

				if (m_settings.separateConstants)
					m_writer.CreateFileConstants(builder.ToString(), sheetName);
			}
		}

		internal void BatchBuildLocalization(
			SheetXBatchSourceState source, string sheetName)
		{
			if (!TryGetGridRange(
				source, sheetName, out var sheet, out string range))
				return;

			m_localizationsDict.Clear();
			LoadSheetLocalizationData(
				sheet, BatchFetchValues(source, range), sheetName);

			if (m_localizationsDict.TryGetValue(sheetName, out var builder))
			{
				m_batchState.Localizations[
					new SheetXBatchSheetKey(source.Index, sheetName)] = builder;

				if (m_settings.separateLocalizations
					&& builder.languageTextDict.Count > 0)
				{
					CreateLocalizationFile(
						builder.idsString, builder.languageTextDict, sheetName);
					m_localizedSheetsExported.Add(sheetName);
				}
			}
		}

		internal void BatchBuildJson(
			SheetXBatchSourceState source, string sheetName, bool combine)
		{
			if (!TryGetGridRange(
				source, sheetName, out var sheet, out string range))
				return;

			string fileName = sheetName.Trim().Replace(" ", "_");
			string json = ConvertSheetToJson(
				sheet,
				BatchFetchValues(source, range),
				sheetName,
				fileName,
				m_settings.encryptJson,
				!combine);

			if (!combine || json == null)
				return;

			if (!m_batchState.CombinedJsons.TryGetValue(source.Index, out var dict))
			{
				dict = new Dictionary<string, string>(StringComparer.Ordinal);
				m_batchState.CombinedJsons[source.Index] = dict;
			}

			dict[fileName] = json;
		}
	}
}