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
	/// <summary>
	/// Provides utility methods for parsing, formatting, and handling various operations related to SheetX data processing.
	/// </summary>
	public static class SheetXHelper
	{
		/// <summary>
		/// Converts a cell's formula result into a string representation based on its cached result type (Numeric, String, Boolean).
		/// </summary>
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

		/// <summary>
		/// Writes content to a file at the specified folder path. Creates the directory and file if they don't exist.
		/// </summary>
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

		/// <summary>
		/// Parses a spreadsheet cell as a decimal using the invariant culture. Cell text is data, not
		/// UI: under a comma-decimal culture the machine's own <c>decimal.TryParse</c> accepts "1,5" as
		/// a number, and that raw comma then lands in generated JSON as invalid syntax.
		/// </summary>
		public static bool TryParseDecimal(string value, out decimal result)
		{
			return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
		}

		/// <summary>
		/// Splits a string value into an array using separators like colon, pipe, and newlines.
		/// </summary>
		public static string[] SplitValueToArray(string pValue, bool pIncludeColon = true)
		{
			string[] splits = { ":", "|", Environment.NewLine, "\n" };
			if (!pIncludeColon)
				splits = new[] { "|", Environment.NewLine, "\n" };

			string[] result = pValue.Split(splits, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.ToArray();
			return result;
		}

		/// <summary>
		/// Analyzes a Google Sheet to determine the data types of its columns (Text, Number, Bool, Json, Arrays).
		/// </summary>
		public static List<FieldValueType> GetFieldValueTypes(Sheet sheet, IList<IList<object>> pValues)
		{
			if (pValues == null || pValues.Count == 0)
				return null;
			var firstRowValues = pValues[0];
			if (pValues.Count > 1)
			{
				var secondRowValues = pValues[1];
				if (secondRowValues.Count > firstRowValues.Count) // Probably has merged cells
					for (var i = firstRowValues.Count; i < secondRowValues.Count; i++)
						firstRowValues.Add("");
			}
			var fieldsName = new string[firstRowValues.Count];
			var fieldsValue = new string[firstRowValues.Count];
			var mergedCellValue = "";
			for (int col = 0; col < firstRowValues.Count; col++)
			{
				var cell = firstRowValues[col];
				var value = cell.ToString().Trim();
				
				if (!string.IsNullOrEmpty(value))
					fieldsName[col] = value.Replace(" ", "_");
				else
					fieldsName[col] = "";
				
				// Check merged cells
				bool isMergedCell = IsMergedCell(sheet, 0, col);
				if (isMergedCell && !string.IsNullOrEmpty(fieldsName[col]))
					mergedCellValue = fieldsName[col];
				else if (isMergedCell && string.IsNullOrEmpty(fieldsName[col]))
					fieldsName[col] = mergedCellValue;

				fieldsValue[col] = "";
			}

			for (int row = 1; row < pValues.Count; row++)
			{
				firstRowValues = pValues[row];
				if (firstRowValues != null)
				{
					//Find longest value, and use it to check value type
					for (int col = 0; col < fieldsName.Length; col++)
					{
						var cellStr = "";
						if (col < firstRowValues.Count)
							cellStr = firstRowValues[col].ToString();
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
				if (string.IsNullOrEmpty(fieldName) || fieldName.EndsWith("[x]"))
					continue;
				string filedValue = fieldsValue[i].Trim();
				bool isArray = fieldName.EndsWith("[]");
				var fieldValueType = new FieldValueType(fieldName);
				if (!isArray)
				{
					if (string.IsNullOrEmpty(filedValue))
						fieldValueType.type = ValueType.Text;
					else
					{
						if (TryParseDecimal(filedValue, out decimal _))
							fieldValueType.type = ValueType.Number;
						else if (bool.TryParse(filedValue.ToLower(), out bool _))
							fieldValueType.type = ValueType.Bool;
						else if (fieldName.EndsWith("{}"))
							fieldValueType.type = ValueType.Json;
						else
							fieldValueType.type = ValueType.Text;
						fieldValueTypes.Add(fieldValueType);
					}
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
							if (TryParseDecimal(longestValue, out decimal _))
								fieldValueType.type = ValueType.ArrayNumber;
							else if (bool.TryParse(longestValue.ToLower(), out bool _))
								fieldValueType.type = ValueType.ArrayBool;
							else
								fieldValueType.type = ValueType.ArrayText;
							fieldValueTypes.Add(fieldValueType);
						}
					}
					else
					{
						fieldValueType.type = ValueType.ArrayText;
						if (!string.IsNullOrEmpty(longestValue))
							fieldValueTypes.Add(fieldValueType);
					}
				}
			}

			return fieldValueTypes;
		}

		/// <summary>
		/// Analyzes an Excel NPOI Workbook Sheet to determine the data types of its columns.
		/// </summary>
		public static List<FieldValueType> GetFieldValueTypes(IWorkbook pWorkBook, string pSheetName)
		{
			var sheet = pWorkBook.GetSheet(pSheetName);
			var firstRowData = sheet?.GetRow(0);
			if (firstRowData == null)
				return null;

			int lastCellNum = firstRowData.LastCellNum;
			var fieldsName = new string[lastCellNum];
			var fieldsValue = new string[lastCellNum];
			var mergedCellValue = "";
			for (int col = 0; col < firstRowData.LastCellNum; col++)
			{
				var cell = firstRowData.GetCell(col);
				if (cell == null || !cell.IsMergedCell && cell.CellType != CellType.String)
					continue;

				if (!string.IsNullOrEmpty(cell.StringCellValue))
					fieldsName[col] = cell.ToString().Replace(" ", "_");
				else
					fieldsName[col] = "";

				// Check merged cells
				if (cell.IsMergedCell && !string.IsNullOrEmpty(fieldsName[col]))
					mergedCellValue = fieldsName[col];
				else if (cell.IsMergedCell && string.IsNullOrEmpty(fieldsName[col]))
					fieldsName[col] = mergedCellValue;

				fieldsValue[col] = "";
			}

			// Get the standard value of the column to verify its data type.
			for (int row = 1; row <= sheet.LastRowNum; row++)
			{
				firstRowData = sheet.GetRow(row);
				if (firstRowData != null)
				{
					// Find the longest value, and use it to check value type
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
				if (string.IsNullOrEmpty(fieldName) || fieldName.EndsWith("[x]"))
					continue;
				string fieldValue = fieldsValue[i].Trim();
				bool isArray = fieldName.EndsWith("[]");
				var fieldValueType = new FieldValueType(fieldName);
				if (!isArray)
				{
					if (string.IsNullOrEmpty(fieldValue))
						fieldValueType.type = ValueType.Text;
					else
					{
						if (TryParseDecimal(fieldValue, out decimal _))
							fieldValueType.type = ValueType.Number;
						else if (bool.TryParse(fieldValue.ToLower(), out bool _))
							fieldValueType.type = ValueType.Bool;
						else if (fieldName.EndsWith("{}"))
							fieldValueType.type = ValueType.Json;
						else
							fieldValueType.type = ValueType.Text;
						fieldValueTypes.Add(fieldValueType);
					}
				}
				else
				{
					string[] values = SplitValueToArray(fieldValue, false);
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
							if (TryParseDecimal(longestValue, out decimal _))
								fieldValueType.type = ValueType.ArrayNumber;
							else if (bool.TryParse(longestValue.ToLower(), out bool _))
								fieldValueType.type = ValueType.ArrayBool;
							else
								fieldValueType.type = ValueType.ArrayText;
							fieldValueTypes.Add(fieldValueType);
						}
					}
					else
					{
						fieldValueType.type = ValueType.ArrayText;
						if (!string.IsNullOrEmpty(longestValue))
							fieldValueTypes.Add(fieldValueType);
					}
				}
			}

			return fieldValueTypes;
		}

		/// <summary>
		/// Removes the last occurrence of a specified character from the text.
		/// </summary>
		public static string RemoveLast(string text, string character)
		{
			if (text.Length < 1) return text;
			int index = text.LastIndexOf(character, StringComparison.Ordinal);
			return index >= 0 ? text.Remove(index, character.Length) : text;
		}

		/// <summary>
		/// Checks if a string is a valid JSON object or array.
		/// </summary>
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
				catch (JsonReaderException)
				{
					return false;
				}
				catch (Exception)
				{
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Orders IDs so that string replacement never rewrites a longer key's prefix first: longest
		/// keys come first, and equal-length keys are ordered by <see cref="StringComparer.Ordinal"/>
		/// so the result is culture-independent.
		/// </summary>
		public static Dictionary<string, int> SortIDsByLength(Dictionary<string, int> dict)
		{
			return dict
				.OrderByDescending(x => x.Key.Length)
				.ThenBy(x => x.Key, StringComparer.Ordinal)
				.ToDictionary(x => x.Key, x => x.Value);
		}

		/// <summary>
		/// Closes a duplicate-name column group and returns the finished JSON member. A group that
		/// collected no value still ends with its opening bracket, so the trailing comma may only be
		/// trimmed when one exists — trimming unconditionally eats the '[' and emits a malformed
		/// member instead of an empty array.
		/// </summary>
		public static string CloseCombinedColumn(string pOpenGroup)
		{
			if (string.IsNullOrEmpty(pOpenGroup))
				return pOpenGroup;
			return pOpenGroup.EndsWith("[", StringComparison.Ordinal)
				? pOpenGroup + "]"
				: pOpenGroup.Substring(0, pOpenGroup.Length - 1) + "]";
		}

		/// <summary>
		/// Serializes the per-sheet JSON of one spreadsheet into a single combined document, keyed by
		/// sheet file name. Keys are ordinally sorted so the output does not depend on the order the
		/// source sheets happened to be read in.
		/// </summary>
		public static string MergeJsonContents(Dictionary<string, string> pJsonBySheet)
		{
			return JsonConvert.SerializeObject(pJsonBySheet
				.OrderBy(pair => pair.Key, StringComparer.Ordinal)
				.ToDictionary(pair => pair.Key, pair => pair.Value));
		}

		/// <summary>
		/// Parses an integer with invariant culture so exports do not depend on the editor's locale.
		/// </summary>
		public static bool TryParseInt(string value, out int result)
		{
			return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
		}

		/// <summary>
		/// Parses a float with invariant culture so exports do not depend on the editor's locale.
		/// </summary>
		public static bool TryParseFloat(string value, out float result)
		{
			return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
		}

		/// <summary>
		/// Formats a float as a round-trippable invariant literal, so generated C# and JSON are
		/// byte-identical on locales that use ',' as the decimal separator.
		/// </summary>
		public static string FormatFloat(float value)
		{
			return value.ToString("R", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts a raw spreadsheet cell into an invariant C# float literal.
		/// </summary>
		public static string FormatFloatLiteral(string raw)
		{
			string trimmed = (raw ?? "").Trim();
			return TryParseFloat(trimmed, out float parsed) ? FormatFloat(parsed) + "f" : trimmed + "f";
		}

		/// <summary>
		/// Formats an integer for generated source and JSON without editor-culture variation.
		/// </summary>
		public static string FormatInt(int value)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts a flat list of JObjects with dot-notation keys into a nested JSON string.
		/// </summary>
		public static string ConvertToNestedJson(List<JObject> original)
		{
			// Parse the original JSON into a JArray
			// var original = JArray.Parse(json);

			// Create a new JArray for the converted JSON
			var converted = new List<JObject>();

			// Iterate over all JObjects in the original JArray
			foreach (var obj in original)
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

		/// <summary>
		/// Merges a list of JObjects into a single JObject.
		/// </summary>
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

		/// <summary>
		/// Creates an Encryption object from a comma-separated string of byte values.
		/// </summary>
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

		/// <summary>
		/// Wraps the file content in a C# namespace block.
		/// </summary>
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

		/// <summary>
		/// Determines if a sheet should be treated as a JSON data sheet based on its name (excluding IDs, Constants, Settings, Localization).
		/// </summary>
		public static bool IsJsonSheet(string pName)
		{
			return !pName.EndsWith(SheetXConstants.IDS_SHEET)
				&& !pName.EndsWith(SheetXConstants.CONSTANTS_SHEET)
				&& !pName.EndsWith(SheetXConstants.SETTINGS_SHEET)
				&& !pName.StartsWith(SheetXConstants.LOCALIZATION_SHEET);
		}

		/// <summary>
		/// Creates an EditorTableView for displaying sheet paths with a toggle column.
		/// </summary>
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

		/// <summary>
		/// Generates unique character sets for multiple content groups.
		/// </summary>
		public static Dictionary<string, string> GenerateCharacterSets(Dictionary<string, string> pContentGroups)
		{
			var output = new Dictionary<string, string>();
			foreach (var map in pContentGroups)
				output.Add(map.Key, GenerateCharacterSet(map.Value));
			return output;
		}

		/// <summary>
		/// Generates a string containing all unique characters found in the input content, sorted.
		/// </summary>
		public static string GenerateCharacterSet(string pContent)
		{
			string charactersSet = "";
			var unique = new HashSet<char>(pContent);
			foreach (char c in unique)
				charactersSet += c;
			charactersSet = string.Concat(charactersSet.OrderBy(c => c));
			return charactersSet;
		}

		/// <summary>
		/// Gets the directory holding the Google OAuth token cache, creating it if it doesn't exist.
		/// Lives in 'Library/SheetX', outside the asset pipeline: 'Library' is never committed and is
		/// not included when 'Assets' is zipped, so a cached OAuth token cannot leak with the project.
		/// </summary>
		/// <param name="pWarn">Receives a best-effort migration failure. Null logs to the console.</param>
		public static string GetTokenStoreDirectory(Action<string> pWarn = null)
		{
			var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "SheetX"));
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			MigrateLegacyTokenStore(path, pWarn);
			return path;
		}

		/// <summary>
		/// Gets the directory holding the Google OAuth token cache.
		/// </summary>
		[Obsolete("Renamed to GetTokenStoreDirectory. The token cache no longer lives under Assets/Editor.")]
		public static string GetSaveDirectory() => GetTokenStoreDirectory();

		// A pre-existing token cache under Assets/Editor is a committed-secret risk, so move rather
		// than leave it behind. Best-effort: a failure here only costs the user one re-authentication.
		private static void MigrateLegacyTokenStore(string destination, Action<string> pWarn)
		{
			var legacyDir = Path.Combine(Application.dataPath, "Editor");
			if (!Directory.Exists(legacyDir))
				return;
			foreach (string file in Directory.GetFiles(legacyDir, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-*"))
			{
				try
				{
					string target = Path.Combine(destination, Path.GetFileName(file));
					if (!File.Exists(target))
						File.Move(file, target);
					else
						File.Delete(file);
				}
				catch (Exception ex)
				{
					string message = $"SheetX: could not move legacy token cache '{file}' out of Assets: {ex.Message}";
					if (pWarn != null)
						pWarn(message);
					else
						Debug.LogWarning(message);
				}
			}
		}

		/// <summary>
		/// Authenticates with Google and fetches the spreadsheet metadata, updating the SheetPath list.
		/// </summary>
		public static void DownloadGoogleSheet(string googleClientId, string googleClientSecret, GoogleSheetsPath pGoogleSheetsPath)
		{
			if (string.IsNullOrEmpty(pGoogleSheetsPath.id))
			{
				Debug.LogError("Key can not be empty");
				return;
			}

			AuthenticateGoogleSheet(googleClientId, googleClientSecret, pGoogleSheetsPath);
		}

		private static void AuthenticateGoogleSheet(string googleClientId, string googleClientSecret, GoogleSheetsPath pGoogleSheetsPath)
		{
			if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
			{
				Debug.LogError("Invalid Google Client ID and Client Secret");
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
				Debug.LogError(ex);
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

		/// <summary>
		/// Authenticates the user with Google using Client ID and Secret, requesting SpreadsheetsReadonly scope.
		/// </summary>
		/// <param name="pWarn">Receives token-cache migration failures. Null logs to the console.</param>
		public static UserCredential AuthenticateGoogleUser(string googleClientId, string googleClientSecret, Action<string> pWarn = null)
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
				new FileDataStore(GetTokenStoreDirectory(pWarn), true)).Result;

			return credential;
		}

		/// <summary>
		/// Removes C-style block comments (/* ... */) from the input string.
		/// </summary>
		public static string RemoveComments(string input)
		{
			return Regex.Replace(input, @"/\*.*?\*/", string.Empty);
		}
		
		/// <summary>
		/// Checks if a cell at a specific row and column index matches any merged region in the Google Sheet.
		/// </summary>
		public static bool IsMergedCell(Sheet sheet, int row, int col)
		{
			var mergedCells = sheet.Merges;
			if (mergedCells == null)
				return false;
			bool isMerged = mergedCells.Any(m =>
				row >= m.StartRowIndex && row < m.EndRowIndex
				&& col >= m.StartColumnIndex && col < m.EndColumnIndex);
			return isMerged;
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

		public static string RemoveSpecialCharacters(this string str)
		{
			var sb = new StringBuilder();
			foreach (char c in str)
			{
				if (c == ' ')
					sb.Append('_');
				else if (c >= '0' && c <= '9' || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '.' || c == '_')
					sb.Append(c);
			}
			return sb.ToString();
		}
	}
}