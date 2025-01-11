/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RCore.SheetX.Editor
{
	public class SheetXHelper : MonoBehaviour
	{
		public static string ConvertFormulaCell(ICell pCell)
		{
			if (pCell.CellType == CellType.Formula)
			{
				if (pCell.CachedFormulaResultType == CellType.Numeric)
					return pCell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
				if (pCell.CachedFormulaResultType == CellType.String)
					return pCell.StringCellValue;
				if (pCell.CachedFormulaResultType == CellType.Boolean)
					return pCell.BooleanCellValue.ToString();
			}
			return null;
		}

		public static void WriteFile(string pFolderPath, string pFileName, string pContent)
		{
			if (!Directory.Exists(pFolderPath))
				Directory.CreateDirectory(pFolderPath);

			string filePath = Path.Combine(pFolderPath, pFileName);
			if (!File.Exists(filePath))
				using (File.Create(filePath)) { }

			using var sw = new StreamWriter(filePath, false, Encoding.UTF8);
			sw.Write(pContent);
			sw.Close();
		}

		public static string[] SplitValueToArray(string pValue, bool pIncludeColon = true)
		{
			string[] splits = { ":", "|", Environment.NewLine, "\r", "\n" };
			if (!pIncludeColon)
				splits = new[] { "|", Environment.NewLine, "\r", "\n" };

			string[] result = pValue.Split(splits, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.ToArray();
			return result;
		}

		public static List<FieldValueType> GetFieldValueTypes(IList<IList<object>> pValues)
		{
			if (pValues == null || pValues.Count == 0)
				return null;
			var rowValues = pValues[0];
			var fieldsName = new string[rowValues.Count];
			var fieldsValue = new string[rowValues.Count];
			for (int col = 0; col < rowValues.Count; col++)
			{
				var cell = rowValues[col].ToString().Trim();
				if (!string.IsNullOrEmpty(cell))
					fieldsName[col] = cell.Replace(" ", "_");
				else
					fieldsName[col] = "";
				fieldsValue[col] = "";
			}

			for (int row = 1; row < pValues.Count; row++)
			{
				rowValues = pValues[row];
				if (rowValues != null)
				{
					//Find longest value, and use it to check value type
					for (int col = 0; col < fieldsName.Length; col++)
					{
						var cellStr = "";
						if (col < rowValues.Count)
							cellStr = rowValues[col].ToString();
						if (!string.IsNullOrEmpty(cellStr))
						{
							cellStr = cellStr.Trim();
							if (cellStr.Length > fieldsValue[col].Length)
								fieldsValue[col] = cellStr;
						}
					}
				}
			}

			var fieldValueTypes = new List<FieldValueType>();
			for (int i = 0; i < fieldsName.Length; i++)
			{
				string fieldName = fieldsName[i];
				string filedValue = fieldsValue[i].Trim();
				bool isArray = fieldName.Contains("[]");
				var fieldValueType = new FieldValueType(fieldName);
				if (!isArray)
				{
					if (string.IsNullOrEmpty(filedValue))
						fieldValueType.type = ValueType.Text;
					else
					{
						if (!filedValue.Contains(',') && decimal.TryParse(filedValue, out decimal _))
							fieldValueType.type = ValueType.Number;
						else if (bool.TryParse(filedValue.ToLower(), out bool _))
							fieldValueType.type = ValueType.Bool;
						else if (fieldName.Contains("{}"))
							fieldValueType.type = ValueType.Json;
						else
							fieldValueType.type = ValueType.Text;
					}
					fieldValueTypes.Add(fieldValueType);
				}
				else
				{
					string[] values = SplitValueToArray(filedValue, false);
					int lenVal = 0;
					string longestValue = "";
					foreach (string val in values)
					{
						if (lenVal < val.Length)
						{
							lenVal = val.Length;
							longestValue = val;
						}
					}
					if (values.Length > 0)
					{
						if (string.IsNullOrEmpty(longestValue))
							fieldValueType.type = ValueType.ArrayText;
						else
						{
							if (!longestValue.Contains(',') && decimal.TryParse(longestValue, out decimal _))
								fieldValueType.type = ValueType.ArrayNumber;
							else if (bool.TryParse(longestValue.ToLower(), out bool _))
								fieldValueType.type = ValueType.ArrayBool;
							else
								fieldValueType.type = ValueType.ArrayText;
						}
						fieldValueTypes.Add(fieldValueType);
					}
					else
					{
						fieldValueType.type = ValueType.ArrayText;
						fieldValueTypes.Add(fieldValueType);
					}
				}
			}

			return fieldValueTypes;
		}

		public static List<FieldValueType> GetFieldValueTypes(IWorkbook pWorkBook, string pSheetName)
		{
			var sheet = pWorkBook.GetSheet(pSheetName);
			var firstRowData = sheet?.GetRow(0);
			if (firstRowData == null)
				return null;

			int lastCellNum = firstRowData.LastCellNum;
			var fieldsName = new string[lastCellNum];
			var fieldsValue = new string[lastCellNum];
			for (int col = 0; col < firstRowData.LastCellNum; col++)
			{
				var cell = firstRowData.GetCell(col);
				if (cell == null || cell.CellType != CellType.String)
					continue;

				if (!string.IsNullOrEmpty(cell.StringCellValue))
					fieldsName[col] = cell.ToString().Replace(" ", "_");
				else
					fieldsName[col] = "";
				fieldsValue[col] = "";
			}

			for (int row = 1; row <= sheet.LastRowNum; row++)
			{
				firstRowData = sheet.GetRow(row);
				if (firstRowData != null)
				{
					//Find longest value, and use it to check value type
					for (int col = 0; col < fieldsName.Length; col++)
					{
						if (string.IsNullOrEmpty(fieldsName[col]))
							continue;
						var cell = firstRowData.GetCell(col);
						if (cell == null)
							continue;
						string cellStr = cell.ToCellString();
						if (cellStr.Length > fieldsValue[col].Length)
							fieldsValue[col] = cellStr;
					}
				}
			}

			var fieldValueTypes = new List<FieldValueType>();
			for (int i = 0; i < fieldsName.Length; i++)
			{
				string fieldName = fieldsName[i];
				if (string.IsNullOrEmpty(fieldName))
					continue;
				string filedValue = fieldsValue[i].Trim();
				bool isArray = fieldName.Contains("[]");
				var fieldValueType = new FieldValueType(fieldName);
				if (!isArray)
				{
					if (string.IsNullOrEmpty(filedValue))
						fieldValueType.type = ValueType.Text;
					else
					{
						if (!filedValue.Contains(',') && decimal.TryParse(filedValue, out decimal _))
							fieldValueType.type = ValueType.Number;
						else if (bool.TryParse(filedValue.ToLower(), out bool _))
							fieldValueType.type = ValueType.Bool;
						else if (fieldName.Contains("{}"))
							fieldValueType.type = ValueType.Json;
						else
							fieldValueType.type = ValueType.Text;
					}
					fieldValueTypes.Add(fieldValueType);
				}
				else
				{
					string[] values = SplitValueToArray(filedValue, false);
					int lenVal = 0;
					string longestValue = "";
					foreach (string val in values)
					{
						if (lenVal < val.Length)
						{
							lenVal = val.Length;
							longestValue = val;
						}
					}
					if (values.Length > 0)
					{
						if (string.IsNullOrEmpty(longestValue))
							fieldValueType.type = ValueType.ArrayText;
						else
						{
							if (!longestValue.Contains(',') && decimal.TryParse(longestValue, out decimal _))
								fieldValueType.type = ValueType.ArrayNumber;
							else if (bool.TryParse(longestValue.ToLower(), out bool _))
								fieldValueType.type = ValueType.ArrayBool;
							else
								fieldValueType.type = ValueType.ArrayText;
						}
						fieldValueTypes.Add(fieldValueType);
					}
					else
					{
						fieldValueType.type = ValueType.ArrayText;
						fieldValueTypes.Add(fieldValueType);
					}
				}
			}

			return fieldValueTypes;
		}

		public static string RemoveLast(string text, string character)
		{
			if (text.Length < 1) return text;
			int index = text.LastIndexOf(character, StringComparison.Ordinal);
			return index >= 0 ? text.Remove(index, character.Length) : text;
		}

		public static bool IsValidJson(string strInput)
		{
			strInput = strInput.Trim();
			if (strInput.StartsWith("{") && strInput.EndsWith("}")
			    || //For object
			    strInput.StartsWith("[") && strInput.EndsWith("]")) //For array
			{
				try
				{
					JToken.Parse(strInput);
					return true;
				}
				catch (JsonReaderException jex)
				{
					Console.WriteLine(jex.Message);
					return false;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
					return false;
				}
			}
			return false;
		}

		public static Dictionary<string, int> SortIDsByLength(Dictionary<string, int> dict)
		{
			var sortedDict = dict.OrderBy(x => x.Key.Length).ToDictionary(x => x.Key, x => x.Value);
			return sortedDict;
		}

		public static string ConvertToNestedJson(List<JObject> original)
		{
			// Parse the original JSON into a JArray
			// var original = JArray.Parse(json);

			// Create a new JArray for the converted JSON
			var converted = new List<JObject>();

			// Iterate over all JObjects in the original JArray
			foreach (JObject obj in original)
			{
				// Create a new JObject for the converted JSON
				var newObj = new JObject();
				string root = "";

				// Iterate over all properties of the original JObject
				foreach (var property in obj.Properties())
				{
					// Split the property name into parts
					var parts = property.Name.Split('.');

					// Create nested JObjects for each part except the last one
					var current = newObj;
					for (int i = 0; i < parts.Length - 1; i++)
					{
						if (current[parts[i]] == null)
						{
							current[parts[i]] = new JObject();
						}
						current = (JObject)current[parts[i]];
					}

					// Add the value to the last part
					current[parts[parts.Length - 1]] = property.Value;
					root = parts[0];
				}

				// Add the new JObject to the converted JArray
				converted.Add(newObj);
			}

			var combineJson = CombineJsonObjects(converted);

			return combineJson.ToString(Formatting.None);
		}

		public static JObject CombineJsonObjects(List<JObject> jsonArray)
		{
			var combined = new JObject();

			foreach (var obj in jsonArray)
			{
				foreach (var property in obj.Properties())
				{
					// Check if the property value is a JObject
					if (property.Value is JObject innerObj)
					{
						if (combined[property.Name] == null)
						{
							combined[property.Name] = new JObject();
						}

						foreach (var innerProperty in innerObj.Properties())
						{
							// Check if the inner property value is a JObject
							if (innerProperty.Value is JObject innerInnerObj)
							{
								if (((JObject)combined[property.Name])[innerProperty.Name] == null)
								{
									((JObject)combined[property.Name])[innerProperty.Name] = new JObject();
								}

								foreach (var innerInnerProperty in innerInnerObj.Properties())
								{
									((JObject)((JObject)combined[property.Name])[innerProperty.Name])[innerInnerProperty.Name] = innerInnerProperty.Value;
								}
							}
							else
							{
								// If the inner property value is not a JObject, just copy it
								((JObject)combined[property.Name])[innerProperty.Name] = innerProperty.Value;
							}
						}
					}
					else
					{
						// If the property value is not a JObject, just copy it
						combined[property.Name] = property.Value;
					}
				}
			}

			return combined;
		}

		public static Encryption CreateEncryption(string text)
		{
			string[] keysString = text.Trim().Replace(" ", "").Split(',');
			if (keysString.Length > 0)
			{
				bool validKey = true;
				byte[] keysByte = new byte[keysString.Length];
				for (int i = 0; i < keysString.Length; i++)
				{
					if (byte.TryParse(keysString[i], out byte output))
					{
						keysByte[i] = output;
					}
					else
					{
						validKey = false;
					}
				}
				if (validKey)
					return new Encryption(keysByte);
			}
			return null;
		}

		public static string AddNamespace(string fileContent, string @namespace)
		{
			if (!string.IsNullOrEmpty(@namespace))
			{
				fileContent = fileContent.Replace(Environment.NewLine, "NEW_LINE");
				fileContent = fileContent.Replace("\n", "NEW_LINE");
				fileContent = fileContent.Replace("NEW_LINE", $"{Environment.NewLine}\t");
				fileContent = $"namespace {@namespace}{Environment.NewLine}{"{"}{Environment.NewLine}\t{fileContent}{Environment.NewLine}{"}"}";
			}
			return fileContent;
		}

		public static bool IsJsonSheet(string pName)
		{
			return !pName.EndsWith(SheetXConstants.IDS_SHEET)
				&& !pName.EndsWith(SheetXConstants.CONSTANTS_SHEET)
				&& !pName.Contains(SheetXConstants.SETTINGS_SHEET)
				&& !pName.StartsWith(SheetXConstants.LOCALIZATION_SHEET);
		}

		public static EditorTableView<SheetPath> CreateSpreadsheetTable(EditorWindow editorWindow, string name, Action<bool> pOnTogSelected)
		{
			var table = new EditorTableView<SheetPath>(editorWindow, name);
			var labelGUIStyle = new GUIStyle(GUI.skin.label)
			{
				padding = new RectOffset(4, 4, 0, 0)
			};
			var disabledLabelGUIStyle = new GUIStyle(labelGUIStyle)
			{
				normal = new GUIStyleState
				{
					textColor = Color.gray
				}
			};
			table.AddColumn(null, 25, 25, (rect, item) =>
				{
					rect.xMin += 4;
					item.Selected = EditorGUI.Toggle(rect, item.selected);
				})
				.ShowToggle(true)
				.OnToggleChanged(pOnTogSelected);
			table.AddColumn("Sheet name", 200, 0, (rect, item) =>
			{
				var style = item.selected ? labelGUIStyle : disabledLabelGUIStyle;
				EditorGUI.LabelField(rect, item.name, style);
			}).SetSorting((a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
			return table;
		}

		public static Dictionary<string, string> GenerateCharacterSets(Dictionary<string, string> pContentGroups)
		{
			var output = new Dictionary<string, string>();
			foreach (var map in pContentGroups)
				output.Add(map.Key, GenerateCharacterSet(map.Value));
			return output;
		}

		public static string GenerateCharacterSet(string pContent)
		{
			string charactersSet = "";
			var unique = new HashSet<char>(pContent);
			foreach (char c in unique)
				charactersSet += c;
			charactersSet = string.Concat(charactersSet.OrderBy(c => c));
			return charactersSet;
		}

		public static string GetSaveDirectory()
		{
			var path = Path.Combine(Application.dataPath, "Editor");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			return path;
		}

		public static void DownloadGoogleSheet(string googleClientId, string googleClientSecret, GoogleSheetsPath pGoogleSheetsPath)
		{
			if (string.IsNullOrEmpty(pGoogleSheetsPath.id))
			{
				UnityEngine.Debug.LogError("Key can not be empty");
				return;
			}

			AuthenticateGoogleSheet(googleClientId, googleClientSecret, pGoogleSheetsPath);
		}

		private static void AuthenticateGoogleSheet(string googleClientId, string googleClientSecret, GoogleSheetsPath pGoogleSheetsPath)
		{
			if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
			{
				UnityEngine.Debug.LogError("Invalid Google Client ID and Client Secret");
				return;
			}

			var service = new SheetsService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = AuthenticateGoogleUser(googleClientId, googleClientSecret),
				ApplicationName = SheetXConstants.APPLICATION_NAME,
			});

			// Fetch metadata for the entire spreadsheet.
			Spreadsheet spreadsheet;
			try
			{
				spreadsheet = service.Spreadsheets.Get(pGoogleSheetsPath.id).Execute();
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError(ex);
				return;
			}

			var sheetPaths = new List<SheetPath>();
			foreach (var sheet in spreadsheet.Sheets)
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
			pGoogleSheetsPath.name = spreadsheet.Properties.Title;
		}

		public static UserCredential AuthenticateGoogleUser(string googleClientId, string googleClientSecret)
		{
			var clientSecrets = new ClientSecrets();
			clientSecrets.ClientId = googleClientId;
			clientSecrets.ClientSecret = googleClientSecret;

			// The file token.json stores the user's access and refresh tokens, and is created
			// automatically when the authorization flow completes for the first time.
			var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
				clientSecrets,
				new[] { SheetsService.Scope.SpreadsheetsReadonly },
				"user",
				CancellationToken.None,
				new FileDataStore(GetSaveDirectory(), true)).Result;

			UnityEngine.Debug.Log("Credential file saved to: " + GetSaveDirectory());
			return credential;
		}

		public static string RemoveComments(string input)
		{
			return Regex.Replace(input, @"/\*.*?\*/", string.Empty);
		}
	}

	public static class SheetXExtension
	{
		public static string ToCellString(this ICell cell, string pDefault = "")
		{
			if (cell == null)
				return pDefault;
			string cellStr;
			if (cell.CellType == CellType.Formula)
			{
				switch (cell.CachedFormulaResultType)
				{
					case CellType.Numeric:
						cellStr = cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);
						break;

					case CellType.String:
						cellStr = cell.StringCellValue;
						break;

					case CellType.Boolean:
						cellStr = cell.BooleanCellValue.ToString();
						break;

					default:
						cellStr = cell.ToString();
						break;
				}
			}
			else
				cellStr = cell.ToString();
			return cellStr;
		}

		public static string RemoveSpecialCharacters(this string str, string replace = "")
		{
			return Regex.Replace(str, "[^a-zA-Z0-9_.]+", replace, RegexOptions.Compiled);
		}
	}
}