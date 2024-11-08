using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace RCore.SheetX
{
	[Serializable]
	public class SheetPath
	{
		public string name;
		public bool selected;
	}

	[Serializable]
	public class ExcelSheetsPath
	{
		public bool selected = true;
		public string path;
		public List<SheetPath> sheets = new();
		public string name;
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
				sheets.Add(new SheetPath()
				{
					name = sheet.SheetName,
					selected = true,
				});
			}
			name = Path.GetFileNameWithoutExtension(path);
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
			name = Path.GetFileNameWithoutExtension(path);
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
	
	[Serializable]
	public class GoogleSheetsPath : IComparable<GoogleSheetsPath>
	{
		public bool selected = true;
		public string id;
		public string name;
		public List<SheetPath> sheets = new List<SheetPath>();
		public void AddSheet(string name)
		{
			for (int i = 0; i < sheets.Count; i++)
				if (sheets[i].name == name)
					return;
			sheets.Add(new SheetPath { name = name, selected = true });
		}
		public void RemoveSheet(string name)
		{
			for (int i = 0; i < sheets.Count; i++)
				if (sheets[i].name == name)
				{
					sheets.RemoveAt(i);
					break;
				}
		}
		public int CompareTo(GoogleSheetsPath other)
		{
			return name.CompareTo(other.name);
		}
	}
	
	public class ConstantBuilder : IComparable<ConstantBuilder>
	{
		public string name;
		public string value;
		public string valueType;
		public string comment;

		public int CompareTo(ConstantBuilder other)
		{
			return string.Compare(name, other.name, StringComparison.Ordinal);
		}
	}
	
	public class LocalizationBuilder
	{
		public List<string> idsString = new List<string>();
		public Dictionary<string, List<string>> languageTextDict = new Dictionary<string, List<string>>();
	}
	
	public class FieldValueType
	{
		public string name;
		public string type;

		public FieldValueType(string name)
		{
			this.name = name;
		}

		public FieldValueType(string name, string type)
		{
			this.name = name;
			this.type = type;
		}
	}
	
	public class ID
	{
		public string Key { get; set; }
		public int Value { get; set; }

		public ID(string key, int value)
		{
			Key = key;
			Value = value;
		}
	}
	
	/// <summary>
	/// Define field name and value type
	/// </summary>
	public class RowContent
	{
		public List<string> fieldNames = new List<string>();
		public List<string> fieldValues = new List<string>();
	}
	
	[Serializable]
	public class Att
	{
		//Main setup
		public int id;
		public float value;
		public float increase;
		public float unlock;
		public float max;

		//Optional setup
		public string idString; //Sometime id is not integer because of error in Excel Data, this field use to point out which data is error
		public float[] values; //Sometime value is defined by multi values which are used in diffirent cases 
		public float[] increases; //Sometime increases is define by multi values which are used for diffirent purposes eg. level and rarity
		public float[] maxes; //Sometime maxes is defined by multi values which are used for diffirent purposes eg. max-level, max-rarity
		public float[] unlocks;
		public string valueString; //Sometime value is not number because of error in Excel Data or you want it like that

		public string GetJsonString()
		{
			var list = new List<string>();
			list.Add($"\"id\":{id}");
			if (value != 0) list.Add($"\"value\":{value}");
			if (increase != 0) list.Add($"\"increase\":{increase}");
			if (unlock != 0) list.Add($"\"unlock\":{unlock}");
			if (max != 0) list.Add($"\"max\":{max}");
			if (!string.IsNullOrEmpty(valueString) && valueString != value.ToString()) list.Add($"\"valueString\":\"{valueString}\"");
			if (id == -1 && !string.IsNullOrEmpty(idString)) list.Add($"\"idString\":\"{idString}\"");
			if (values != null) list.Add($"\"values\":{JsonConvert.SerializeObject(values)}");
			if (increases != null) list.Add($"\"increases\":{JsonConvert.SerializeObject(increases)}");
			if (maxes != null) list.Add($"\"maxes\":{JsonConvert.SerializeObject(maxes)}");
			if (unlocks != null) list.Add($"\"unlocks\":{JsonConvert.SerializeObject(unlocks)}");
			return "{" + string.Join(",", list.ToArray()) + "}";
			
			// Json IDs Constants Localization
			// JICL For Unity
		}
	}
}