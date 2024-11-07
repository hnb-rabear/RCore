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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using RCore.Editor;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RCore.SheetX
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

			string filePath = $"{pFolderPath}\\{pFileName}";
			if (!File.Exists(filePath))
				using (File.Create(filePath)) { }

			using var sw = new StreamWriter(filePath, false, Encoding.UTF8);
			sw.WriteLine(pContent);
			sw.Close();
		}

		public static void WriteFile(string pFilePath, string pContent)
		{
			if (!File.Exists(pFilePath))
				using (File.Create(pFilePath)) { }

			using var sw = new StreamWriter(pFilePath);
			sw.WriteLine(pContent);
			sw.Close();
		}

		public static string[] SplitValueToArray(string pValue, bool pIncludeColon = true)
		{
			string[] splits = { ":", "|", "\r\n", "\r", "\n" };
			if (!pIncludeColon)
				splits = new[] { "|", "\r\n", "\r", "\n" };
			string[] result = pValue.Split(splits, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
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
						fieldValueType.type = "string";
					else
					{
						if (!filedValue.Contains(',') && decimal.TryParse(filedValue, out decimal _))
							fieldValueType.type = "number";
						else if (bool.TryParse(filedValue.ToLower(), out bool _))
							fieldValueType.type = "bool";
						else if (fieldName.Contains("{}"))
							fieldValueType.type = "json";
						else
							fieldValueType.type = "string";
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
							fieldValueType.type = "array-string";
						else
						{
							if (!longestValue.Contains(',') && decimal.TryParse(longestValue, out decimal _))
								fieldValueType.type = "array-number";
							else if (bool.TryParse(longestValue.ToLower(), out bool _))
								fieldValueType.type = "array-bool";
							else
								fieldValueType.type = "array-string";
						}
						fieldValueTypes.Add(fieldValueType);
					}
					else
					{
						fieldValueType.type = "array-string";
						fieldValueTypes.Add(fieldValueType);
					}
				}
			}

			return fieldValueTypes;
		}

		public static List<FieldValueType> GetFieldValueTypes(IWorkbook pWorkBook, string pSheetName)
		{
			var sheet = pWorkBook.GetSheet(pSheetName);
			var firstRowData = sheet.GetRow(0);
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
					{
						fieldValueType.type = "string";
					}
					else
					{
						if (!filedValue.Contains(',') && decimal.TryParse(filedValue, out decimal _))
						{
							fieldValueType.type = "number";
						}
						else if (bool.TryParse(filedValue.ToLower(), out bool _))
						{
							fieldValueType.type = "bool";
						}
						else if (fieldName.Contains("{}"))
						{
							fieldValueType.type = "json";
						}
						else
						{
							fieldValueType.type = "string";
						}
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
						{
							fieldValueType.type = "array-string";
						}
						else
						{
							if (!longestValue.Contains(',') && decimal.TryParse(longestValue, out decimal _))
							{
								fieldValueType.type = "array-number";
							}
							else if (bool.TryParse(longestValue.ToLower(), out bool _))
							{
								fieldValueType.type = "array-bool";
							}
							else
							{
								fieldValueType.type = "array-string";
							}
						}
						fieldValueTypes.Add(fieldValueType);
					}
					else
					{
						fieldValueType.type = "array-string";
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
			if (strInput.StartsWith("{") && strInput.EndsWith("}") || //For object
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

		public static IEnumerable<ID> SortIDsByLength(IEnumerable<ID> list)
		{
			// Use LINQ to sort the array received and return a copy.
			var sorted = from s in list
				orderby s.Key.Length descending
				select s;
			return sorted;
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
				fileContent = $"namespace {@namespace}\n{"{"}\n\t{fileContent}\n{"}"}";
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

		public static EditorTableView<SheetPath> CreateSpreadsheetTable(EditorWindow editorWindow)
		{
			var table = new EditorTableView<SheetPath>(editorWindow, "Spreadsheets");
			var labelGUIStyle = new GUIStyle(GUI.skin.label)
			{
				padding = new RectOffset(left: 10, right: 10, top: 2, bottom: 2)
			};
			var disabledLabelGUIStyle = new GUIStyle(labelGUIStyle)
			{
				normal = new GUIStyleState
				{
					textColor = Color.gray
				}
			};
			table.AddColumn("Selected", 60, 60, (rect, item) =>
			{
				rect.xMin += 10;
				item.selected = EditorGUI.Toggle(rect, item.selected);
			});
			table.AddColumn("Sheet name", 200, 300, (rect, item) =>
			{
				var style = item.selected ? labelGUIStyle : disabledLabelGUIStyle;
				EditorGUI.LabelField(rect, item.name, style);
			}).SetSorting((a, b) => String.Compare(a.name, b.name, StringComparison.Ordinal));
			return table;
		}

#region Google Spreadsheets

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
					pGoogleSheetsPath.sheets.Add(new SheetPath()
					{
						name = sheetPath.name,
						selected = true,
					});
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

#endregion
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
	}
}