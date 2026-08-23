using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetXBatchExportTests
	{
		private sealed class MemoryOutput : ISheetXOutput
		{
			public readonly List<string> WriteOrder = new List<string>();
			public readonly Dictionary<string, string> Writes =
				new Dictionary<string, string>(StringComparer.Ordinal);

			public void Write(string relativePath, string content)
			{
				WriteOrder.Add(relativePath);
				Writes.Add(relativePath, content);
			}
		}

		private sealed class FailAtOutput : ISheetXOutput
		{
			private readonly string m_failPath;
			public readonly List<string> WriteOrder = new List<string>();
			public readonly Dictionary<string, string> Writes =
				new Dictionary<string, string>(StringComparer.Ordinal);

			public FailAtOutput(string failPath)
			{
				m_failPath = failPath;
			}

			public void Write(string relativePath, string content)
			{
				if (StringComparer.Ordinal.Equals(relativePath, m_failPath))
					throw new IOException($"Disk full writing {relativePath}");
				WriteOrder.Add(relativePath);
				Writes.Add(relativePath, content);
			}
		}

		private readonly List<string> m_temp = new List<string>();

		[TearDown]
		public void TearDown()
		{
			foreach (string path in m_temp)
			{
				if (File.Exists(path))
					File.Delete(path);
			}

			m_temp.Clear();
		}

		private string SaveWorkbook(XSSFWorkbook workbook)
		{
			string path = Path.Combine(
				Path.GetTempPath(),
				$"sheetx-batch-{Guid.NewGuid():N}.xlsx");

			using (var stream = File.Create(path))
			{
				workbook.Write(stream);
			}

			workbook.Close();
			m_temp.Add(path);
			return path;
		}

		private static void WriteId(NPOI.SS.UserModel.IRow row, string key, string value)
		{
			row.CreateCell(0).SetCellValue(key);
			row.CreateCell(1).SetCellValue(value);
		}

		private string CreateIdsWorkbook(
			string sheetName, params (string key, string value)[] ids)
		{
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet(sheetName);
			sheet.CreateRow(0).CreateCell(0).SetCellValue("Hero");

			for (int i = 0; i < ids.Length; i++)
			{
				var row = sheet.CreateRow(i + 1);
				WriteId(row, ids[i].key, ids[i].value);
			}

			return SaveWorkbook(workbook);
		}

		private static string CreateWorkbookWithDataSheets(
			List<string> temp, params string[] sheetNames)
		{
			var workbook = new XSSFWorkbook();
			foreach (string name in sheetNames)
			{
				var sheet = workbook.CreateSheet(name);
				sheet.CreateRow(0).CreateCell(0).SetCellValue("id");
				sheet.CreateRow(1).CreateCell(0).SetCellValue("1");
			}

			string path = Path.Combine(
				Path.GetTempPath(),
				$"sheetx-batch-{Guid.NewGuid():N}.xlsx");

			using (var stream = File.Create(path))
			{
				workbook.Write(stream);
			}

			workbook.Close();
			temp.Add(path);
			return path;
		}

		[Test]
		public void cross_source_id_resolves_in_json()
		{
			string a = CreateIdsWorkbook("HeroIDs", ("HERO_1", "7"));
			var workbookB = new XSSFWorkbook();
			var data = workbookB.CreateSheet("Data");
			var header = data.CreateRow(0);
			header.CreateCell(0).SetCellValue("id");
			header.CreateCell(1).SetCellValue("ref");
			var row = data.CreateRow(1);
			row.CreateCell(0).SetCellValue("hero");
			row.CreateCell(1).SetCellValue("HERO_1");
			string b = SaveWorkbook(workbookB);
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
						new SheetXBatchSource { SpreadsheetPath = b },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.Writes.ContainsKey("Generated/Data.txt"), Is.True);
			string json = output.Writes["Generated/Data.txt"];
			Assert.That(json, Does.Contain("7"));
			Assert.That(json, Does.Not.Contain("\"HERO_1\""));
		}

		[Test]
		public void aggregate_ids_carries_declarations_from_both_sources()
		{
			string a = CreateIdsWorkbook("HeroIDs", ("HERO_1", "1"));
			string b = CreateIdsWorkbook("MonsterIDs", ("MONSTER_1", "100"));
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
						new SheetXBatchSource { SpreadsheetPath = b },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.Writes.ContainsKey("Generated/IDs.cs"), Is.True);
			string ids = output.Writes["Generated/IDs.cs"];
			Assert.That(ids, Does.Contain("HERO_1 = 1;"));
			Assert.That(ids, Does.Contain("MONSTER_1 = 100;"));
		}

		[Test]
		public void aggregate_constants_carries_both_sources()
		{
			var workbookA = new XSSFWorkbook();
			var sheetA = workbookA.CreateSheet("GameConstants");
			var rowA0 = sheetA.CreateRow(0);
			rowA0.CreateCell(0).SetCellValue("MAX_HP");
			rowA0.CreateCell(1).SetCellValue("int");
			rowA0.CreateCell(2).SetCellValue("100");
			string a = SaveWorkbook(workbookA);

			var workbookB = new XSSFWorkbook();
			var sheetB = workbookB.CreateSheet("UIConstants");
			var rowB0 = sheetB.CreateRow(0);
			rowB0.CreateCell(0).SetCellValue("FONT_SIZE");
			rowB0.CreateCell(1).SetCellValue("int");
			rowB0.CreateCell(2).SetCellValue("24");
			string b = SaveWorkbook(workbookB);
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
						new SheetXBatchSource { SpreadsheetPath = b },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.Writes.ContainsKey("Generated/Constants.cs"), Is.True);
			string constants = output.Writes["Generated/Constants.cs"];
			Assert.That(constants, Does.Contain("MAX_HP"));
			Assert.That(constants, Does.Contain("FONT_SIZE"));
		}

		[Test]
		public void combine_json_produces_per_source_named_files()
		{
			string a = CreateWorkbookWithDataSheets(m_temp, "Weapons");
			string b = CreateWorkbookWithDataSheets(m_temp, "Armors");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					CombineJson = true,
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a, OutputName = "WeaponsData" },
						new SheetXBatchSource { SpreadsheetPath = b, OutputName = "ArmorsData" },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.Writes.ContainsKey("Generated/WeaponsData.txt"), Is.True);
			Assert.That(output.Writes.ContainsKey("Generated/ArmorsData.txt"), Is.True);
		}

		[Test]
		public void combine_json_duplicate_output_name_fails_before_any_write()
		{
			string a = CreateWorkbookWithDataSheets(m_temp, "Weapons");
			string b = CreateWorkbookWithDataSheets(m_temp, "Armors");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					CombineJson = true,
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a, OutputName = "SameName" },
						new SheetXBatchSource { SpreadsheetPath = b, OutputName = "SameName" },
					},
				},
				output);

			Assert.That(result.Errors, Has.Count.GreaterThan(0));
			Assert.That(output.WriteOrder, Is.Empty);
			Assert.That(result.Files, Is.Empty);
		}

		[Test]
		public void same_json_sheet_name_no_combine_collides()
		{
			string a = CreateWorkbookWithDataSheets(m_temp, "Data");
			string b = CreateWorkbookWithDataSheets(m_temp, "Data");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
						new SheetXBatchSource { SpreadsheetPath = b },
					},
				},
				output);

			Assert.That(result.Errors, Has.Exactly(1).Contains(
				"Artifact 'Generated/Data.txt' collision:"));
			Assert.That(output.WriteOrder, Is.Empty);
			Assert.That(result.Files, Is.Empty);
		}

		[Test]
		public void separate_ids_emits_per_sheet_and_no_aggregate()
		{
			string a = CreateIdsWorkbook("HeroIDs", ("HERO_1", "1"));
			string b = CreateIdsWorkbook("MonsterIDs", ("MONSTER_1", "100"));
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					SeparateIDs = true,
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
						new SheetXBatchSource { SpreadsheetPath = b },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.Writes.ContainsKey("Generated/HeroIDs.cs"), Is.True);
			Assert.That(output.Writes.ContainsKey("Generated/MonsterIDs.cs"), Is.True);
			Assert.That(output.Writes.ContainsKey("Generated/IDs.cs"), Is.False);
		}

		[Test]
		public void output_name_fallback_preserves_extension()
		{
			string a = CreateWorkbookWithDataSheets(m_temp, "Data");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					CombineJson = true,
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			string expectedName = Path.GetFileName(a).Trim().Replace(" ", "_");
			Assert.That(
				output.Writes.ContainsKey($"Generated/{expectedName}.txt"),
				Is.True,
				$"Expected combined JSON under Generated/{expectedName}.txt");
		}

		[Test]
		public void sink_failure_reports_error_before_any_accepted_write()
		{
			string a = CreateIdsWorkbook("HeroIDs", ("HERO_1", "1"));
			string b = CreateIdsWorkbook("MonsterIDs", ("MONSTER_1", "100"));
			var output = new FailAtOutput("Generated/IDs.cs");

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
						new SheetXBatchSource { SpreadsheetPath = b },
					},
				},
				output);

			Assert.That(result.Errors, Has.Count.GreaterThan(0));
			Assert.That(output.WriteOrder, Is.Empty);
		}

		[Test]
		public void warning_does_not_block_flush()
		{
			var workbook = new XSSFWorkbook();
			var ids = workbook.CreateSheet("HeroIDs");
			ids.CreateRow(0).CreateCell(0).SetCellValue("KEY");
			var data = workbook.CreateSheet("Data");
			data.CreateRow(0).CreateCell(0).SetCellValue("id");
			data.CreateRow(1).CreateCell(0).SetCellValue("hero");
			string path = SaveWorkbook(workbook);
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = path },
					},
				},
				output);

			Assert.That(result.Warnings, Has.Count.GreaterThan(0));
			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.Writes.ContainsKey("Generated/Data.txt"), Is.True);
		}

		[Test]
		public void settings_sheet_is_skipped()
		{
			var workbook = new XSSFWorkbook();
			var settings = workbook.CreateSheet("Settings");
			settings.CreateRow(0).CreateCell(0).SetCellValue("key");
			settings.CreateRow(1).CreateCell(0).SetCellValue("value");
			string path = SaveWorkbook(workbook);
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = path },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.WriteOrder, Is.Empty);
		}

		[Test]
		public void no_asset_created_under_assets_sheetx()
		{
			string a = CreateIdsWorkbook("HeroIDs", ("HERO_1", "1"));
			var output = new MemoryOutput();

			SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
					},
				},
				output);

			Assert.That(
				output.Writes.Keys.Where(
					k => k.StartsWith("Assets/SheetX/", StringComparison.OrdinalIgnoreCase)),
				Is.Empty);
		}

		[Test]
		public void flush_order_follows_source_order_then_native_sheet_order()
		{
			var workbookA = new XSSFWorkbook();
			var s1 = workbookA.CreateSheet("Zebra");
			s1.CreateRow(0).CreateCell(0).SetCellValue("id");
			s1.CreateRow(1).CreateCell(0).SetCellValue("z");
			var s2 = workbookA.CreateSheet("Alpha");
			s2.CreateRow(0).CreateCell(0).SetCellValue("id");
			s2.CreateRow(1).CreateCell(0).SetCellValue("a");
			string a = SaveWorkbook(workbookA);
			string b = CreateWorkbookWithDataSheets(m_temp, "Beta");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
						new SheetXBatchSource { SpreadsheetPath = b },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			var jsonWrites = output.WriteOrder.Where(p => p.EndsWith(".txt")).ToList();
			Assert.That(jsonWrites, Has.Count.EqualTo(3));
			Assert.That(jsonWrites[0], Is.EqualTo("Generated/Zebra.txt"));
			Assert.That(jsonWrites[1], Is.EqualTo("Generated/Alpha.txt"));
			Assert.That(jsonWrites[2], Is.EqualTo("Generated/Beta.txt"));
		}

		[Test]
		public void batch_missing_id_value_warns_and_does_not_block_flush()
		{
			var workbook = new XSSFWorkbook();
			var ids = workbook.CreateSheet("HeroIDs");
			ids.CreateRow(0).CreateCell(0).SetCellValue("Hero");
			var row = ids.CreateRow(1);
			row.CreateCell(0).SetCellValue("HERO_1");
			row.CreateCell(1).SetCellValue("");

			var data = workbook.CreateSheet("Data");
			data.CreateRow(0).CreateCell(0).SetCellValue("id");
			data.CreateRow(1).CreateCell(0).SetCellValue("1");

			string path = SaveWorkbook(workbook);
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = path },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			Assert.That(result.Warnings, Has.Count.GreaterThan(0));
			Assert.That(result.Warnings, Has.Some.Contains("Key HERO_1 doesn't have value!"));
			Assert.That(output.Writes.ContainsKey("Generated/Data.txt"), Is.True);
		}

		[Test]
		public void batch_invalid_json_reports_error_and_discards_all_writes()
		{
			var workbook = new XSSFWorkbook();
			var data = workbook.CreateSheet("Data");
			data.CreateRow(0).CreateCell(0).SetCellValue("Payload{}");
			data.CreateRow(1).CreateCell(0).SetCellValue("{broken");

			string path = SaveWorkbook(workbook);
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = path },
					},
				},
				output);

			Assert.That(result.Errors, Has.Count.GreaterThan(0));
			Assert.That(result.Errors, Has.Some.Contains("Invalid Json string"));
			Assert.That(output.WriteOrder, Is.Empty);
		}

		[Test]
		public void empty_localization_sheet_does_not_crash_and_emits_no_manager()
		{
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet("LocalizationEmpty");
			var header = sheet.CreateRow(0);
			header.CreateCell(0).SetCellValue("idString");
			header.CreateCell(1).SetCellValue("relativeId");

			string path = SaveWorkbook(workbook);
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					LocalizationOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = path },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.Writes.ContainsKey("Generated/LocalizationsManager.cs"), Is.False);
		}

		[Test]
		public void aggregate_ids_boundary_well_formed_between_regions()
		{
			string a = CreateIdsWorkbook("HeroIDs", ("HERO_1", "1"));
			string b = CreateIdsWorkbook("MonsterIDs", ("MONSTER_1", "100"));
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a },
						new SheetXBatchSource { SpreadsheetPath = b },
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			string idsContent = output.Writes["Generated/IDs.cs"];
			Assert.That(idsContent, Does.Contain("#endregion" + Environment.NewLine + "\t#region"));
		}

		[Test]
		public void normalized_output_name_collision_fails_in_validation()
		{
			string a = CreateWorkbookWithDataSheets(m_temp, "Weapons");
			string b = CreateWorkbookWithDataSheets(m_temp, "Armors");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					CombineJson = true,
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = a, OutputName = "foo bar" },
						new SheetXBatchSource { SpreadsheetPath = b, OutputName = "foo_bar" },
					},
				},
				output);

			Assert.That(result.Errors, Has.Count.GreaterThan(0));
			Assert.That(result.Errors, Has.Some.Contains("both resolve to OutputName 'foo_bar'"));
			Assert.That(output.WriteOrder, Is.Empty);
		}
	}
}
