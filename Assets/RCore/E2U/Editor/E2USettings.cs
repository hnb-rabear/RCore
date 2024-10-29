using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NPOI.SS.UserModel;
using RCore.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.E2U
{
	public static class E2UConstants
	{
		public const string APPLICATION_NAME = "Excel to Unity - Data Converter";
		public const string CONSTANTS_CS_TEMPLATE = "ConstantsTemplate.txt";
		public const string IDS_CS_TEMPLATE = "IDsTemplate.txt";
		public const string LOCALIZATION_MANAGER_TEMPLATE = "LocalizationsManagerTemplate.txt";
		public const string LOCALIZATION_TEMPLATE = "LocalizationTemplate.txt";
		public const string LOCALIZATION_TEMPLATE_V2 = "LocalizationTemplateV2.txt";
		public const string LOCALIZATION_TEXT_TEMPLATE = "LocalizationTextTemplate.txt";
		public const string SETTINGS_CS_TEMPLATE = "SettingsTemplate.txt";
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
		public bool separateLocalizations = true;
		public bool combineJson;
		public bool enumTypeOnlyForIDs;
		public bool encryptJson;
		public List<string> languageMaps = new() { "korea (kr)", "japan (jp)", "china (cn)" };
		public List<string> persistentFields = new List<string>() { "id", "key" };
		public List<string> excludedSheets;
		public string googleClientId;
		public string googleClientSecret;
		public string encryptionKey =
			"168, 220, 184, 133, 78, 149, 8, 249, 171, 138, 98, 170, 95, 15, 211, 200, 51, 242, 4, 193, 219, 181, 232, 99, 16, 240, 142, 128, 29, 163, 245, 24, 204, 73, 173, 32, 214, 76, 31, 99, 91, 239, 232, 53, 138, 195, 93, 195, 185, 210, 155, 184, 243, 216, 204, 42, 138, 101, 100, 241, 46, 145, 198, 66, 11, 17, 19, 86, 157, 27, 132, 201, 246, 112, 121, 7, 195, 148, 143, 125, 158, 29, 184, 67, 187, 100, 31, 129, 64, 130, 26, 67, 240, 128, 233, 129, 63, 169, 5, 211, 248, 200, 199, 96, 54, 128, 111, 147, 100, 6, 185, 0, 188, 143, 25, 103, 211, 18, 17, 249, 106, 54, 162, 188, 25, 34, 147, 3, 222, 61, 218, 49, 164, 165, 133, 12, 65, 92, 48, 40, 129, 76, 194, 229, 109, 76, 150, 203, 251, 62, 54, 251, 70, 224, 162, 167, 183, 78, 103, 28, 67, 183, 23, 80, 156, 97, 83, 164, 24, 183, 81, 56, 103, 77, 112, 248, 4, 168, 5, 72, 109, 18, 75, 219, 99, 181, 160, 76, 65, 16, 41, 175, 87, 195, 181, 19, 165, 172, 138, 172, 84, 40, 167, 97, 214, 90, 26, 124, 0, 166, 217, 97, 246, 117, 237, 99, 46, 15, 141, 69, 4, 245, 98, 73, 3, 8, 161, 98, 79, 161, 127, 19, 55, 158, 139, 247, 39, 59, 72, 161, 82, 158, 25, 65, 107, 173, 5, 255, 53, 28, 179, 182, 65, 162, 17";

		private Dictionary<string, StringBuilder> m_idsBuilderDict = new Dictionary<string, StringBuilder>();
		private Dictionary<string, int> m_allIds = new Dictionary<string, int>();

		public static E2USettings Load()
		{
			var collection = AssetDatabase.LoadAssetAtPath(FILE_PATH, typeof(E2USettings)) as E2USettings;
			if (collection == null)
				collection = EditorHelper.CreateScriptableAsset<E2USettings>(FILE_PATH);
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

		public void ExportAll() { }

		public void ExportConstants() { }

		public void ExportIDs()
		{
			if (string.IsNullOrEmpty(constantsOutputFolder))
			{
				UnityEngine.Debug.LogError("Please setup the [constantsOutputFolder] first!");
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

		public void ExportLocalizations() { }

		public void ExportJson() { }

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
						if (cellKey != null)
						{
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
			var textAsset = Resources.Load<TextAsset>(E2UConstants.IDS_CS_TEMPLATE.Replace(".txt",""));
			string fileContent = textAsset.text;
			fileContent = fileContent.Replace("_IDS_CLASS_NAME_", exportFileName);
			fileContent = fileContent.Replace("public const int _FIELDS_ = 0;", content);
			fileContent = AddNamespace(fileContent);

			E2UHelper.WriteFile(constantsOutputFolder, $"{exportFileName}.cs", fileContent);
			UnityEngine.Debug.Log($"Exported {exportFileName}.cs!");
		}
		
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
	}
}