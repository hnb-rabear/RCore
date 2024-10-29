using System.Collections;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine;

namespace RCore.E2U
{
	[System.Serializable]
	public class ExcelFile
	{
		public string path;
		public bool selected;
		public bool exportConstants;
		public bool exportIDs;
	}

	[System.Serializable]
	public class Spreadsheet
	{
		public string name;
		public bool selected;
	}

	[System.Serializable]
	public class Excel
	{
		public string path;
		public List<Spreadsheet> sheets = new();
		public void Load()
		{
			using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var workbook = new XSSFWorkbook(file);
			for (var i = 0; i < sheets.Count; i++)
			{
				var sheet = workbook.GetSheet(sheets[i].name);
				if (sheet == null)
				{
					sheets.RemoveAt(i);
					i--;
				}
			}
			for (int i = 0; i < workbook.NumberOfSheets; i++)
			{
				var sheet = workbook.GetSheetAt(i);
				if (sheets.Exists(x => x.name == sheet.SheetName))
					continue;
				sheets.Add(new Spreadsheet()
				{
					name = sheet.SheetName,
					selected = true,
				});
			}
		}
		public void Validate()
		{
			if (sheets.Count > 0)
			{
				using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var workbook = new XSSFWorkbook(file);
				for (var i = 0; i < sheets.Count; i++)
				{
					var sheet = workbook.GetSheet(sheets[i].name);
					if (sheet == null)
					{
						sheets.RemoveAt(i);
						i--;
					}
				}
			}
		}
		public IWorkbook GetWorkBook()
		{
			if (!File.Exists(path))
			{
				UnityEngine.Debug.LogError($"{path} does not exist.");
				return null;
			}

			using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			return WorkbookFactory.Create(file);
		}
	}
}