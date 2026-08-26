/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	public class SheetXConfigSheetTests
	{
		[Test]
		public void config_script_artifact_type_is_appended_after_existing_types()
		{
			Assert.That(
				(int)SheetXExportFileType.ConfigScript,
				Is.GreaterThan((int)SheetXExportFileType.LocalizationComponent));
		}

		[Test]
		public void existing_artifact_type_ordinals_are_unchanged()
		{
			Assert.That((int)SheetXExportFileType.Ids, Is.EqualTo(0));
			Assert.That((int)SheetXExportFileType.Constants, Is.EqualTo(1));
			Assert.That((int)SheetXExportFileType.Json, Is.EqualTo(2));
			Assert.That((int)SheetXExportFileType.Localization, Is.EqualTo(3));
			Assert.That((int)SheetXExportFileType.CharacterSet, Is.EqualTo(4));
			Assert.That((int)SheetXExportFileType.LocalizationManager, Is.EqualTo(5));
			Assert.That((int)SheetXExportFileType.LocalizationConstants, Is.EqualTo(6));
			Assert.That((int)SheetXExportFileType.LocalizationComponent, Is.EqualTo(7));
		}

		[Test]
		public void parse_groups_and_root_fields_after_blank_separator()
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("economy", "startingCoins", "int", "1000"),
				Row("", "startingGems", "int", "50"),
				Row("", "", "", ""),
				Row("", "position2", "vector2", "1|2"),
			}, "Configuration", Fail, out var data);

			Assert.That(ok, Is.True);
			Assert.That(data.Groups, Has.Count.EqualTo(1));
			Assert.That(data.Groups[0].Key, Is.EqualTo("economy"));
			Assert.That(data.Groups[0].ClassName, Is.EqualTo("Economy"));
			Assert.That(data.Groups[0].Fields, Has.Count.EqualTo(2));
			Assert.That(data.RootFields[0].Name, Is.EqualTo("position2"));
		}

		[TestCase("int", "42")]
		[TestCase("float", "1.25")]
		[TestCase("boolean", "true")]
		[TestCase("boolean", "0")]
		[TestCase("boolean", "1")]
		[TestCase("string", "hello")]
		[TestCase("int-array", "1|2|3")]
		[TestCase("float-array", "1.5|2.25")]
		[TestCase("string-array", "one|two")]
		[TestCase("vector2", "1|2")]
		[TestCase("vector3", "1|2|3")]
		public void parse_accepts_every_supported_type(string type, string value)
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("group", "value", type, value),
			}, "Configuration", Fail, out _);

			Assert.That(ok, Is.True);
		}

		[Test]
		public void parse_excel_boolean_numbers_as_json_booleans()
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("feature", "maintenanceMode", "boolean", "0"),
				Row("", "enableShop", "boolean", "1"),
			}, "Configuration", Fail, out var data);

			Assert.That(ok, Is.True);
			Assert.That(SheetXConfigSheet.EmitJson(data), Is.EqualTo(
				"{\"feature\":{\"maintenanceMode\":false,\"enableShop\":true}}"));
		}

		[TestCase("INT", "7")]
		[TestCase("Vector3", "1|2|3")]
		[TestCase("FLOAT-ARRAY", "1|2")]
		public void parse_type_names_are_case_insensitive(string type, string value)
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("group", "value", type, value),
			}, "Configuration", Fail, out _);

			Assert.That(ok, Is.True);
		}

		[Test]
		public void parse_rejects_malformed_rows()
		{
			var errors = ParseErrors(new List<string[]>
			{
				new[] { "SubClass", "Field Name", "Type", "Value" },
				Row("group", "count", "int", "1"),
			});
			Assert.That(errors, Has.Some.Contains("Configuration header"));

			errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("group", "2bad", "int", "1"),
				Row("group", "count", "unknown", "1"),
				Row("group", "number", "float", "NaN"),
			});
			Assert.That(errors, Has.Some.Contains("valid C# identifier"));
			Assert.That(errors, Has.Some.Contains("not supported"));
			Assert.That(errors, Has.Some.Contains("invalid float"));
		}

		[Test]
		public void parse_rejects_orphan_field_before_first_group()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("", "count", "int", "1"),
			});

			Assert.That(errors, Has.Some.Contains("no Sub Class before a group"));
		}

		[Test]
		public void parse_preserves_duplicate_groups_fields_and_root_collisions()
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("economy", "value", "int", "1"),
				Row("", "value", "int", "2"),
				Row("economy", "value", "int", "3"),
				Row("", "", "", ""),
				Row("", "economy", "int", "4"),
				Row("", "economy", "int", "5"),
			}, "Configuration", Fail, out var data);

			Assert.That(ok, Is.True);
			Assert.That(data.Groups, Has.Count.EqualTo(2));
			Assert.That(data.Groups[0].Fields, Has.Count.EqualTo(2));
			Assert.That(data.RootFields, Has.Count.EqualTo(2));

			string json = SheetXConfigSheet.EmitJson(data);
			Assert.That(json, Is.EqualTo(
				"{\"economy\":{\"value\":1,\"value\":2},\"economy\":{\"value\":3},\"economy\":4,\"economy\":5}"));

			string source = SheetXConfigSheet.EmitCSharp(data, "Configuration", "");
			Assert.That(Count(source, "public class Economy"), Is.EqualTo(2));
			Assert.That(Count(source, "public int economy;"), Is.EqualTo(2));
		}

		[Test]
		public void emit_csharp_writes_nested_types_load_method_and_namespace()
		{
			string source = SheetXConfigSheet.EmitCSharp(ParseValidConfiguration(), "Configuration", "Game.Data");

			Assert.That(source, Does.Contain("namespace Game.Data"));
			Assert.That(source, Does.Contain("public class Configuration : ScriptableObject"));
			Assert.That(source, Does.Contain("public class Economy"));
			Assert.That(source, Does.Contain("public int startingCoins;"));
			Assert.That(source, Does.Contain("public float[] rates;"));
			Assert.That(source, Does.Contain("public Economy economy;"));
			Assert.That(source, Does.Contain("JsonUtility.FromJsonOverwrite(configJson.text, this);"));
			Assert.That(source, Does.Contain("Configuration JSON is not assigned."));
			Assert.That(source, Does.Not.Contain("Newtonsoft"));
			Assert.That(source.Replace("\r\n", ""), Does.Not.Contain("\n"));
		}

		[Test]
		public void emitted_configuration_json_loads_with_json_utility()
		{
			string json = SheetXConfigSheet.EmitJson(ParseValidConfiguration());
			var asset = ScriptableObject.CreateInstance<ConfigurationShape>();
			try
			{
				JsonUtility.FromJsonOverwrite(json, asset);
				Assert.That(asset.economy.startingCoins, Is.EqualTo(1000));
				Assert.That(asset.economy.rates, Is.EqualTo(new[] { 0.5f, 0.25f }));
				Assert.That(asset.position2, Is.EqualTo(new Vector2(1f, 2f)));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(asset);
			}
		}

		[Test]
		public void interactive_configuration_exports_when_unselected_and_missing_from_sheet_list()
		{
			var workbook = CreateConfigurationWorkbook();
			var output = new MemoryOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: true);
			var settings = InteractiveSettings(new SheetPath { name = "Configuration", selected = false });

			try
			{
				new ExcelSheetHandler(settings, context) { ConfigurationRouteEnabled = true }.ExportJson(workbook);
				context.Flush();

				Assert.That(context.ToResult().Errors, Is.Empty);
				Assert.That(output.Writes["Generated/Configuration.txt"], Does.StartWith("{"));
				Assert.That(output.Writes, Contains.Key("Generated/Configuration.cs"));
			}
			finally
			{
				workbook.Close();
			}
		}

		[Test]
		public void interactive_config_stays_selected_ordinary_json_and_joins_combined_json()
		{
			var workbook = CreateOrdinaryConfigWorkbook();
			var output = new MemoryOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: true);
			var settings = InteractiveSettings(new SheetPath { name = "Config", selected = true });
			settings.combineJson = true;

			try
			{
				new ExcelSheetHandler(settings, context) { ConfigurationRouteEnabled = true }.ExportJson(workbook);
				context.Flush();

				Assert.That(context.ToResult().Errors, Is.Empty);
				Assert.That(output.Writes["Generated/Game_Data.txt"], Does.Contain("\"Config\":["));
				Assert.That(output.Writes.Keys, Does.Not.Contain("Generated/Configuration.txt"));
				Assert.That(output.Writes.Keys.Any(key => key.EndsWith(".cs", StringComparison.Ordinal)), Is.False);
			}
			finally
			{
				workbook.Close();
			}
		}

		[Test]
		public void interactive_configuration_stays_plaintext_and_outside_combined_json()
		{
			var workbook = CreateConfigurationWorkbook();
			CreateOrdinaryJsonSheet(workbook, "Items");
			var output = new MemoryOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: true);
			var settings = InteractiveSettings(new SheetPath { name = "Items", selected = true });
			settings.combineJson = true;
			settings.encryptJson = true;

			try
			{
				new ExcelSheetHandler(settings, context) { ConfigurationRouteEnabled = true }.ExportJson(workbook);
				context.Flush();

				Assert.That(context.ToResult().Errors, Is.Empty);
				Assert.That(output.Writes["Generated/Configuration.txt"], Does.StartWith("{"));
				Assert.That(output.Writes, Contains.Key("Generated/Game_Data.txt"));
				string aggregate = settings.GetEncryption().Decrypt(output.Writes["Generated/Game_Data.txt"]);
				Assert.That(aggregate, Does.Contain("\"Items\":["));
				Assert.That(aggregate, Does.Not.Contain("\"Configuration\":"));
			}
			finally
			{
				workbook.Close();
			}
		}

		[Test]
		public void interactive_multi_file_configuration_merges_selected_sources_in_source_order()
		{
			string firstPath = CreateConfigurationWorkbookFile("economy", "startingCoins", "1000");
			string secondPath = CreateConfigurationWorkbookFile("visual", "uiScale", "2");
			try
			{
				var output = new MemoryOutput();
				var context = new SheetXExportContext(output, discardStagedOnError: true);
				var settings = InteractiveSettings();
				settings.combineJson = true;
				settings.excelSheetsPaths = new List<ExcelSheetsPath>
				{
					new ExcelSheetsPath { path = firstPath, selected = true },
					new ExcelSheetsPath { path = secondPath, selected = true },
				};

				new ExcelSheetHandler(settings, context) { ConfigurationRouteEnabled = true }.ExportAllFiles();
				context.Flush();

				Assert.That(context.ToResult().Errors, Is.Empty);
				string json = output.Writes["Generated/Configuration.txt"];
				Assert.That(json.IndexOf("\"economy\"", StringComparison.Ordinal), Is.LessThan(
					json.IndexOf("\"visual\"", StringComparison.Ordinal)));
				Assert.That(output.Writes, Contains.Key("Generated/Configuration.cs"));
				Assert.That(output.Writes.Keys.Any(key => key.Contains("ConfigurationConfiguration")), Is.False);
				Assert.That(output.Writes.Values.Any(value => value.Contains("\"Configuration\":")), Is.False);
			}
			finally
			{
				File.Delete(firstPath);
				File.Delete(secondPath);
			}
		}

		[TestCase("Config")]
		[TestCase("Configuration")]
		public void detached_sheets_stay_row_array_json(string sheetName)
		{
			string path = CreateOrdinaryConfigWorkbookFile(sheetName);
			try
			{
				var output = new MemoryOutput();
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					JsonOutputPath = "Generated",
				}, output);

				Assert.That(result.Success, Is.True);
				Assert.That(output.Writes[$"Generated/{sheetName}.txt"], Does.StartWith("["));
				Assert.That(output.Writes.Keys.Any(key => key.EndsWith(".cs", StringComparison.Ordinal)), Is.False);
			}
			finally
			{
				File.Delete(path);
			}
		}

		private static XSSFWorkbook CreateConfigurationWorkbook(
			string group = "economy", string field = "startingCoins", string value = "1000")
		{
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet("Configuration");
			WriteConfigurationRows(sheet, group, field, value);
			return workbook;
		}

		private static XSSFWorkbook CreateOrdinaryConfigWorkbook()
		{
			var workbook = new XSSFWorkbook();
			CreateOrdinaryJsonSheet(workbook, "Config");
			return workbook;
		}

		private static string CreateConfigurationWorkbookFile(string group, string field, string value)
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			var workbook = CreateConfigurationWorkbook(group, field, value);
			try
			{
				using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
				workbook.Write(stream);
				return path;
			}
			finally
			{
				workbook.Close();
			}
		}

		private static string CreateOrdinaryConfigWorkbookFile(string sheetName)
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			var workbook = new XSSFWorkbook();
			CreateOrdinaryJsonSheet(workbook, sheetName);
			try
			{
				using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
				workbook.Write(stream);
				return path;
			}
			finally
			{
				workbook.Close();
			}
		}

		private static void WriteConfigurationRows(NPOI.SS.UserModel.ISheet sheet, string group, string field, string value)
		{
			string[] headers = { "Sub Class", "Field Name", "Type", "Value" };
			string[] values = { group, field, "int", value };
			var header = sheet.CreateRow(0);
			var row = sheet.CreateRow(1);
			for (int i = 0; i < headers.Length; i++)
			{
				header.CreateCell(i).SetCellValue(headers[i]);
				row.CreateCell(i).SetCellValue(values[i]);
			}
		}

		private static void CreateOrdinaryJsonSheet(XSSFWorkbook workbook, string sheetName)
		{
			var sheet = workbook.CreateSheet(sheetName);
			sheet.CreateRow(0).CreateCell(0).SetCellValue("id");
			sheet.CreateRow(1).CreateCell(0).SetCellValue("item_1");
		}

		private static SheetXSettings InteractiveSettings(params SheetPath[] sheets)
		{
			var settings = SheetXSettings.CreateTransient(new SheetXExportRequest
			{
				ConstantsOutputPath = "Generated",
				JsonOutputPath = "Generated",
			});
			settings.silent = true;
			settings.excelSheetsPath = new ExcelSheetsPath
			{
				path = "Game Data.xlsx",
				sheets = sheets.ToList(),
			};
			return settings;
		}

		private static ConfigSheetData ParseValidConfiguration()
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("economy", "startingCoins", "int", "1000"),
				Row("", "rates[]", "float-array", "0.5|0.25"),
				Row("", "enabled", "boolean", "true"),
				Row("", "", "", ""),
				Row("", "position2", "vector2", "1|2"),
			}, "Configuration", Fail, out var data);

			Assert.That(ok, Is.True);
			return data;
		}

		private static List<string> ParseErrors(List<string[]> table, string rootTypeName = "Configuration")
		{
			var errors = new List<string>();
			bool ok = SheetXConfigSheet.TryParse(table, rootTypeName, errors.Add, out var data);
			Assert.That(ok, Is.False);
			Assert.That(data, Is.Null);
			return errors;
		}

		private static int Count(string text, string value)
		{
			int count = 0;
			int index = 0;
			while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
			{
				count++;
				index += value.Length;
			}
			return count;
		}

		private static string[] Header() => Row("Sub Class", "Field Name", "Type", "Value");
		private static string[] Row(string subClass, string fieldName, string type, string value)
			=> new[] { subClass, fieldName, type, value };
		private static void Fail(string message) => Assert.Fail(message);

		private sealed class MemoryOutput : ISheetXOutput
		{
			public readonly Dictionary<string, string> Writes = new Dictionary<string, string>();
			public void Write(string relativePath, string content) => Writes.Add(relativePath, content);
		}

		[Serializable]
		private class Economy
		{
			public int startingCoins;
			public float[] rates;
			public bool enabled;
		}

		private class ConfigurationShape : ScriptableObject
		{
			public Economy economy;
			public Vector2 position2;
		}
	}
}
