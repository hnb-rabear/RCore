using System;
using System.Collections.Generic;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class ExcelSheetHandlerBatchTests
	{
		private sealed class MemoryOutput : ISheetXOutput
		{
			public readonly Dictionary<string, string> Writes =
				new Dictionary<string, string>(StringComparer.Ordinal);

			public void Write(string relativePath, string content)
				=> Writes.Add(relativePath, content);
		}

		private static IWorkbook IdsWorkbook(string sheetName, string key, string value)
		{
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet(sheetName);

			var header = sheet.CreateRow(0);
			header.CreateCell(0).SetCellValue("Hero");

			var row = sheet.CreateRow(1);
			row.CreateCell(0).SetCellValue(key);
			row.CreateCell(1).SetCellValue(value);
			return workbook;
		}

		private static SheetXSettings BatchSettings()
			=> SheetXSettings.CreateTransient(new SheetXBatchExportRequest
			{
				ConstantsOutputPath = "Generated",
				JsonOutputPath = "Generated",
				LocalizationOutputPath = "Generated",
			});

		private static string SaveWorkbook(IWorkbook workbook)
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			using (var stream = File.Create(path))
				workbook.Write(stream);
			workbook.Close();
			return path;
		}

		[Test]
		public void export_all_files_emits_preloaded_ids_constants()
		{
			string firstPath = SaveWorkbook(IdsWorkbook("HeroIDs", "HERO_1", "1"));
			string secondPath = SaveWorkbook(IdsWorkbook("MonsterIDs", "MONSTER_1", "100"));
			var output = new MemoryOutput();
			var settings = BatchSettings();
			settings.excelSheetsPaths = new List<ExcelSheetsPath>
			{
				new ExcelSheetsPath
				{
					path = firstPath,
					selected = true,
					sheets = new List<SheetPath>
					{
						new SheetPath { name = "HeroIDs", selected = true },
					},
				},
				new ExcelSheetsPath
				{
					path = secondPath,
					selected = true,
					sheets = new List<SheetPath>
					{
						new SheetPath { name = "MonsterIDs", selected = true },
					},
				},
			};
			var context = new SheetXExportContext(output, discardStagedOnError: true);

			try
			{
				new ExcelSheetHandler(settings, context).ExportAllFiles();
				context.Flush();

				string content = output.Writes["Generated/IDs.cs"];
				Assert.That(content, Does.Contain("public const int HERO_1 = 1;"));
				Assert.That(content, Does.Contain("public const int MONSTER_1 = 100;"));
			}
			finally
			{
				File.Delete(firstPath);
				File.Delete(secondPath);
			}
		}

		[Test]
		public void batch_constructor_aliases_shared_id_table()
		{
			var state = new SheetXBatchState();
			state.TryAddId("HERO_1", 1, "a.xlsx", "HeroIDs", out _);
			var handler = new ExcelSheetHandler(
				BatchSettings(),
				new SheetXExportContext(new MemoryOutput(), discardStagedOnError: true),
				state);

			Assert.That(handler, Is.Not.Null);
			Assert.That(state.AllIds["HERO_1"], Is.EqualTo(1));
		}

		[Test]
		public void batch_load_ids_reports_duplicate_and_keeps_first_value()
		{
			var state = new SheetXBatchState();
			var context = new SheetXExportContext(
				new MemoryOutput(), discardStagedOnError: true);
			var handler = new ExcelSheetHandler(BatchSettings(), context, state);
			var first = IdsWorkbook("HeroIDs", "HERO_1", "1");
			var second = IdsWorkbook("HeroIDs", "HERO_1", "2");
			try
			{
				handler.BatchLoadIds(first, "a.xlsx", "HeroIDs");
				handler.BatchLoadIds(second, "b.xlsx", "HeroIDs");

				Assert.That(state.AllIds["HERO_1"], Is.EqualTo(1));
				Assert.That(
					context.ToResult().Errors,
					Has.Exactly(1).EqualTo(
						"Duplicate ID 'HERO_1': first 'a.xlsx' sheet 'HeroIDs' value '1'; "
						+ "second 'b.xlsx' sheet 'HeroIDs' value '2'."));
			}
			finally
			{
				first.Close();
				second.Close();
			}
		}

		[Test]
		public void batch_build_ids_declares_preloaded_key_once()
		{
			var state = new SheetXBatchState();
			var context = new SheetXExportContext(
				new MemoryOutput(), discardStagedOnError: true);
			var handler = new ExcelSheetHandler(BatchSettings(), context, state);
			var workbook = IdsWorkbook("HeroIDs", "HERO_1", "1");
			try
			{
				handler.BatchLoadIds(workbook, "a.xlsx", "HeroIDs");
				context.SetOrigin("a.xlsx", "HeroIDs");
				handler.BatchBuildIds(workbook, 0, "HeroIDs");

				var key = new SheetXBatchSheetKey(0, "HeroIDs");
				Assert.That(state.DeclaredIds, Has.Member("HERO_1"));
				Assert.That(
					state.IdsBuilders[key].ToString(),
					Does.Contain("public const int HERO_1 = 1;"));
			}
			finally
			{
				workbook.Close();
			}
		}

		[Test]
		public void same_sheet_name_in_two_sources_keeps_separate_builders()
		{
			var state = new SheetXBatchState();
			var context = new SheetXExportContext(
				new MemoryOutput(), discardStagedOnError: true);
			var handler = new ExcelSheetHandler(BatchSettings(), context, state);
			var first = IdsWorkbook("HeroIDs", "HERO_1", "1");
			var second = IdsWorkbook("HeroIDs", "HERO_2", "2");
			try
			{
				handler.BatchLoadIds(first, "a.xlsx", "HeroIDs");
				handler.BatchLoadIds(second, "b.xlsx", "HeroIDs");
				context.SetOrigin("a.xlsx", "HeroIDs");
				handler.BatchBuildIds(first, 0, "HeroIDs");
				context.SetOrigin("b.xlsx", "HeroIDs");
				handler.BatchBuildIds(second, 1, "HeroIDs");

				Assert.That(state.IdsBuilders.Count, Is.EqualTo(2));
				Assert.That(
					state.IdsBuilders[new SheetXBatchSheetKey(0, "HeroIDs")].ToString(),
					Does.Contain("HERO_1"));
				Assert.That(
					state.IdsBuilders[new SheetXBatchSheetKey(1, "HeroIDs")].ToString(),
					Does.Contain("HERO_2"));
			}
			finally
			{
				first.Close();
				second.Close();
			}
		}

		[Test]
		public void aggregate_ids_follow_given_order()
		{
			var state = new SheetXBatchState();
			var output = new MemoryOutput();
			var context = new SheetXExportContext(output, discardStagedOnError: true);
			var handler = new ExcelSheetHandler(BatchSettings(), context, state);
			var first = IdsWorkbook("FirstIDs", "HERO_1", "1");
			var second = IdsWorkbook("SecondIDs", "HERO_2", "2");
			try
			{
				handler.BatchLoadIds(first, "a.xlsx", "FirstIDs");
				handler.BatchLoadIds(second, "b.xlsx", "SecondIDs");
				context.SetOrigin("a.xlsx", "FirstIDs");
				handler.BatchBuildIds(first, 0, "FirstIDs");
				context.SetOrigin("b.xlsx", "SecondIDs");
				handler.BatchBuildIds(second, 1, "SecondIDs");
				context.SetOrigin("<batch>", "<ids>");
				handler.BatchEmitAggregateIds(new[]
				{
					new SheetXBatchSheetKey(0, "FirstIDs"),
					new SheetXBatchSheetKey(1, "SecondIDs"),
				});
				context.Flush();

				string content = output.Writes["Generated/IDs.cs"];
				Assert.That(
					content.IndexOf("HERO_1", StringComparison.Ordinal),
					Is.LessThan(content.IndexOf("HERO_2", StringComparison.Ordinal)));
			}
			finally
			{
				first.Close();
				second.Close();
			}
		}

		[Test]
		public void aggregate_ids_collision_names_batch_origin()
		{
			var state = new SheetXBatchState();
			var context = new SheetXExportContext(
				new MemoryOutput(), discardStagedOnError: true);
			var handler = new ExcelSheetHandler(BatchSettings(), context, state);
			var workbook = IdsWorkbook("HeroIDs", "HERO_1", "1");
			try
			{
				handler.BatchLoadIds(workbook, "a.xlsx", "HeroIDs");
				context.SetOrigin("a.xlsx", "HeroIDs");
				handler.BatchBuildIds(workbook, 0, "HeroIDs");
				var order = new[] { new SheetXBatchSheetKey(0, "HeroIDs") };

				context.SetOrigin("<batch>", "<ids>");
				handler.BatchEmitAggregateIds(order);
				context.SetOrigin("<batch>", "<ids>");
				handler.BatchEmitAggregateIds(order);

				Assert.That(
					context.ToResult().Errors,
					Has.Exactly(1).EqualTo(
						"Artifact 'Generated/IDs.cs' collision: "
						+ "first '<batch>' sheet '<ids>'; second '<batch>' sheet '<ids>'."));
			}
			finally
			{
				workbook.Close();
			}
		}
	}
}
