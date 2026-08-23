using System;
using System.Collections.Generic;
using System.IO;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetXBatchPhaseCTests
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

		private string CreateIdsWorkbook(
			string sheetName, params (string key, int value)[] ids)
		{
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet(sheetName);
			sheet.CreateRow(0).CreateCell(0).SetCellValue("Hero");

			for (int i = 0; i < ids.Length; i++)
			{
				var row = sheet.CreateRow(i + 1);
				row.CreateCell(0).SetCellValue(ids[i].key);
				row.CreateCell(1).SetCellValue(ids[i].value.ToString());
			}

			return SaveWorkbook(workbook);
		}

		[Test]
		public void duplicate_id_same_value_is_an_error_with_full_origin()
		{
			string a = CreateIdsWorkbook("HeroIDs", ("HERO_1", 1));
			string b = CreateIdsWorkbook("HeroIDs", ("HERO_1", 1));
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

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Duplicate ID 'HERO_1': first '{a}' sheet 'HeroIDs' value '1'; "
					+ $"second '{b}' sheet 'HeroIDs' value '1'."));
			Assert.That(output.WriteOrder, Is.Empty,
				"Phase C error must block all sink writes.");
			Assert.That(result.Files, Is.Empty);
		}

		[Test]
		public void duplicate_id_different_value_is_an_error_with_full_origin()
		{
			string a = CreateIdsWorkbook("HeroIDs", ("HERO_1", 1));
			string b = CreateIdsWorkbook("HeroIDs", ("HERO_1", 2));
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

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Duplicate ID 'HERO_1': first '{a}' sheet 'HeroIDs' value '1'; "
					+ $"second '{b}' sheet 'HeroIDs' value '2'."));
			Assert.That(output.WriteOrder, Is.Empty);
			Assert.That(result.Files, Is.Empty);
		}

		[Test]
		public void phase_c_visits_ids_sheets_only()
		{
			// A Data sheet must not be visited in Phase C. If it were, its
			// non-ID rows would hit the parser and likely error or pollute state.
			var workbook = new XSSFWorkbook();
			var ids = workbook.CreateSheet("HeroIDs");
			ids.CreateRow(0).CreateCell(0).SetCellValue("Hero");
			var idRow = ids.CreateRow(1);
			idRow.CreateCell(0).SetCellValue("HERO_1");
			idRow.CreateCell(1).SetCellValue("1");

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

			// The point is that Phase C did not error on the Data sheet and
			// never handed its non-ID rows to the ID parser. `Data` is a JSON
			// sheet: Phase D turns it into `Generated/Data.txt`, which is why
			// this test asserts on errors and not on an empty sink.
			Assert.That(result.Errors, Is.Empty);
		}

		[Test]
		public void phase_c_follows_source_order_then_native_sheet_order()
		{
			// Source A defines HERO_1=1 in "BetaIDs".
			// Source B defines HERO_1=2 in "AlphaIDs".
			// Phase C must visit A first (source index 0).
			// The diagnostic's "first" origin proves which source was visited
			// first: if the coordinator reversed order, "first" would name b.
			string a = CreateIdsWorkbook("BetaIDs", ("HERO_1", 1));
			string b = CreateIdsWorkbook("AlphaIDs", ("HERO_1", 2));
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

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Duplicate ID 'HERO_1': first '{a}' sheet 'BetaIDs' value '1'; "
					+ $"second '{b}' sheet 'AlphaIDs' value '2'."));
			Assert.That(output.WriteOrder, Is.Empty);
		}

		[Test]
		public void phase_c_follows_native_sheet_order_not_requested_sheet_order()
		{
			// Workbook order is BetaIDs, then AlphaIDs. Request order reverses it.
			// The batch must preserve native workbook order for the selected subset.
			var workbook = new XSSFWorkbook();
			var beta = workbook.CreateSheet("BetaIDs");
			beta.CreateRow(0).CreateCell(0).SetCellValue("Hero");
			var betaRow = beta.CreateRow(1);
			betaRow.CreateCell(0).SetCellValue("HERO_1");
			betaRow.CreateCell(1).SetCellValue("1");

			var alpha = workbook.CreateSheet("AlphaIDs");
			alpha.CreateRow(0).CreateCell(0).SetCellValue("Hero");
			var alphaRow = alpha.CreateRow(1);
			alphaRow.CreateCell(0).SetCellValue("HERO_1");
			alphaRow.CreateCell(1).SetCellValue("2");

			string path = SaveWorkbook(workbook);
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource
						{
							SpreadsheetPath = path,
							Sheets = new List<string> { "AlphaIDs", "BetaIDs" },
						},
					},
				},
				output);

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Duplicate ID 'HERO_1': first '{path}' sheet 'BetaIDs' value '1'; "
					+ $"second '{path}' sheet 'AlphaIDs' value '2'."));
			Assert.That(output.WriteOrder, Is.Empty);
		}

		[Test]
		public void phase_c_error_reports_every_duplicate_before_stopping()
		{
			// Phase C accumulates diagnostics rather than bailing on the first
			// one: two distinct duplicated keys produce two errors, and the
			// barrier still blocks everything downstream.
			string a = CreateIdsWorkbook(
				"HeroIDs", ("HERO_1", 1), ("HERO_2", 2));
			string b = CreateIdsWorkbook(
				"HeroIDs", ("HERO_1", 9), ("HERO_2", 8));
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

			Assert.That(result.Errors, Has.Count.EqualTo(2));
			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Duplicate ID 'HERO_1': first '{a}' sheet 'HeroIDs' value '1'; "
					+ $"second '{b}' sheet 'HeroIDs' value '9'."));
			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Duplicate ID 'HERO_2': first '{a}' sheet 'HeroIDs' value '2'; "
					+ $"second '{b}' sheet 'HeroIDs' value '8'."));
			Assert.That(output.WriteOrder, Is.Empty,
				"The Phase C barrier must block every sink write.");
			Assert.That(result.Files, Is.Empty);
		}
	}
}
