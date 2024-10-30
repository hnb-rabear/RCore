using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NPOI.SS.UserModel;
using UnityEngine;

namespace RCore.E2U
{
	public class E2UHelper : MonoBehaviour
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
	}
	
	public static class E2UExtension
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