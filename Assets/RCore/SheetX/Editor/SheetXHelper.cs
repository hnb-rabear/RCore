using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using UnityEngine;

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

			using var sw = new StreamWriter(filePath);
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
			var rowData = sheet.GetRow(0);
			if (rowData == null)
				return null;

			int lastCellNum = rowData.LastCellNum;
			var fieldsName = new string[lastCellNum];
			var fieldsValue = new string[lastCellNum];
			for (int col = 0; col < rowData.LastCellNum; col++)
			{
				var cell = rowData.GetCell(col);
				if (cell != null && !string.IsNullOrEmpty(cell.StringCellValue))
					fieldsName[col] = cell.ToString().Replace(" ", "_");
				else
					fieldsName[col] = "";
				fieldsValue[col] = "";
			}

			for (int row = 1; row <= sheet.LastRowNum; row++)
			{
				rowData = sheet.GetRow(row);
				if (rowData != null)
				{
					//Find longest value, and use it to check value type
					for (int col = 0; col < fieldsName.Length; col++)
					{
						var cell = rowData.GetCell(col);
						if (cell != null)
						{
							string cellStr = cell.ToCellString();
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