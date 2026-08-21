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

namespace RCore.SheetX.Tests
{
	public class SheetXExportTests
	{
		[Test]
		public void export_excel_writes_only_to_caller_output()
		{
			string path = CreateWorkbook("Data", new[] { "id" }, new[] { "hero" });
			try
			{
				var output = new MemoryOutput();
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					JsonOutputPath = "Generated",
				}, output);

				Assert.That(result.Success, Is.True);
				Assert.That(output.Writes["Generated/Data.txt"], Is.EqualTo("[{\"id\":\"hero\"}]"));
				Assert.That(result.Files, Has.Count.EqualTo(1));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void export_excel_empty_sheet_selection_writes_nothing()
		{
			string path = CreateWorkbook("Data", new[] { "id" }, new[] { "hero" });
			try
			{
				var output = new MemoryOutput();
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					JsonOutputPath = "Generated",
					Sheets = new List<string>(),
				}, output);

				Assert.That(result.Success, Is.True);
				Assert.That(output.Writes, Is.Empty);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void export_excel_reports_invalid_json_without_throwing()
		{
			string path = CreateWorkbook("Data", new[] { "Payload{}" }, new[] { "{broken" });
			try
			{
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					JsonOutputPath = "Generated",
				}, new MemoryOutput());

				Assert.That(result.Success, Is.False);
				Assert.That(result.Errors, Has.Exactly(1).Contains("Sheet: Data Field: Payload Row: 1"));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void export_excel_reads_every_sheet_from_one_opened_workbook()
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			var workbook = new XSSFWorkbook();
			CreateDataSheet(workbook, "DataA", "dataa");
			CreateDataSheet(workbook, "DataB", "datab");
			CreateDataSheet(workbook, "DataC", "datac");
			using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
				workbook.Write(stream);

			try
			{
				var output = new MemoryOutput();
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					JsonOutputPath = "Generated",
				}, output);

				Assert.That(result.Success, Is.True);
				Assert.That(result.Errors, Is.Empty);
				Assert.That(output.Writes["Generated/DataA.txt"], Is.EqualTo("[{\"id\":\"dataa\"}]"));
				Assert.That(output.Writes["Generated/DataB.txt"], Is.EqualTo("[{\"id\":\"datab\"}]"));
				Assert.That(output.Writes["Generated/DataC.txt"], Is.EqualTo("[{\"id\":\"datac\"}]"));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void export_excel_tags_localization_code_apart_from_localization_data()
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet("LocalizationExample");
			var header = sheet.CreateRow(0);
			header.CreateCell(0).SetCellValue("idString");
			header.CreateCell(1).SetCellValue("relativeId");
			header.CreateCell(2).SetCellValue("english");
			header.CreateCell(3).SetCellValue("spanish");
			var row = sheet.CreateRow(1);
			row.CreateCell(0).SetCellValue("greeting");
			row.CreateCell(1).SetCellValue("");
			row.CreateCell(2).SetCellValue("Hello");
			row.CreateCell(3).SetCellValue("Hola");
			using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
				workbook.Write(stream);

			try
			{
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					ConstantsOutputPath = "Generated",
					LocalizationOutputPath = "Generated",
					SeparateLocalizations = true,
				}, new MemoryOutput());
				var byType = result.Files.GroupBy(file => file.Type).ToDictionary(group => group.Key, group => group.ToList());

				Assert.That(result.Success, Is.True);
				Assert.That(byType[SheetXExportFileType.Localization], Has.Count.EqualTo(2));
				Assert.That(byType[SheetXExportFileType.LocalizationConstants], Has.Count.EqualTo(1));
				Assert.That(byType[SheetXExportFileType.LocalizationComponent], Has.Count.EqualTo(1));
				Assert.That(result.Files.Where(file => file.RelativePath.EndsWith(".cs")),
					Has.None.Matches<SheetXExportFile>(file => file.Type == SheetXExportFileType.Localization),
					"A generated .cs artifact is still tagged as localization data.");
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void export_google_requires_request_credentials()
		{
			var result = SheetXExporter.ExportGoogle(new SheetXExportRequest
			{
				SpreadsheetPath = "spreadsheet-id",
			}, new MemoryOutput());

			Assert.That(result.Success, Is.False);
			Assert.That(result.Errors, Has.Exactly(1).EqualTo("GoogleClientId and GoogleClientSecret are required for a Google export."));
		}

		[Test]
		public void export_google_empty_sheet_selection_needs_no_credentials()
		{
			// Empty means none: with no work to do the exporter must not reach OAuth or the network,
			// so it cannot fail on credentials it never needs.
			var output = new MemoryOutput();
			var result = SheetXExporter.ExportGoogle(new SheetXExportRequest
			{
				SpreadsheetPath = "spreadsheet-id",
				Sheets = new List<string>(),
			}, output);

			Assert.That(result.Success, Is.True);
			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.Writes, Is.Empty);
		}

		[Test]
		public void export_excel_reports_conflicting_duplicate_id_as_error()
		{
			// The windows raise this as a modal; a batch caller has nobody to click OK, so the same
			// text has to come back as a result error instead.
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			var workbook = new XSSFWorkbook();
			var ids = workbook.CreateSheet("HeroIDs");
			ids.CreateRow(0).CreateCell(0).SetCellValue("KEY");
			WriteId(ids.CreateRow(1), "HERO_1", "1");
			WriteId(ids.CreateRow(2), "HERO_1", "2");

			using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
				workbook.Write(stream);

			try
			{
				var output = new MemoryOutput();
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					ConstantsOutputPath = "Generated",
				}, output);

				Assert.That(result.Errors, Has.Exactly(1).EqualTo("ID HERO_1 is duplicated in sheet HeroIDs"));

				// The conflicting row must also be dropped from the artifact: two "public const int HERO_1"
				// lines in one class is C# that does not compile.
				string generated = output.Writes["Generated/IDs.cs"];
				Assert.That(generated, Does.Contain("public const int HERO_1 = 1;"));
				Assert.That(generated, Does.Not.Contain("HERO_1 = 2"));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void export_excel_empty_ids_sheet_with_separate_ids_warns_instead_of_throwing()
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			var workbook = new XSSFWorkbook();
			// Header only: nothing to build, so there is no per-sheet builder to read back.
			workbook.CreateSheet("HeroIDs").CreateRow(0).CreateCell(0).SetCellValue("KEY");

			using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
				workbook.Write(stream);

			try
			{
				var output = new MemoryOutput();
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					ConstantsOutputPath = "Generated",
					SeparateIDs = true,
				}, output);

				Assert.That(result.Errors, Is.Empty);
				Assert.That(result.Warnings, Has.Exactly(1).EqualTo("Sheet HeroIDs is empty!"));
				Assert.That(output.Writes, Is.Empty);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void create_sheets_deduplicates_repeated_sheet_names_ordinally()
		{
			var sheets = SheetXExporter.CreateSheets(new List<string> { "Heroes", "heroes", "Heroes" });

			Assert.That(sheets, Has.Count.EqualTo(2));
			Assert.That(sheets[0].name, Is.EqualTo("Heroes"));
			Assert.That(sheets[1].name, Is.EqualTo("heroes"));
		}

		[Test]
		public void export_excel_warns_when_encryption_key_is_omitted()
		{
			string path = CreateWorkbook("Data", new[] { "id" }, new[] { "hero" });
			try
			{
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					JsonOutputPath = "Generated",
					EncryptJson = true,
				}, new MemoryOutput());

				// Omitting the key falls back to the key published in this repository, which protects
				// nothing. Silence here would ship "encrypted" data anyone can read.
				Assert.That(result.Warnings, Has.Exactly(1).Contains("published default key"));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void export_excel_reports_non_integer_id_without_throwing()
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			var workbook = new XSSFWorkbook();
			var ids = workbook.CreateSheet("HeroIDs");
			ids.CreateRow(0).CreateCell(0).SetCellValue("KEY");
			var idRow = ids.CreateRow(1);
			idRow.CreateCell(0).SetCellValue("HERO_1");
			idRow.CreateCell(1).SetCellValue("not-a-number");

			var data = workbook.CreateSheet("Data");
			data.CreateRow(0).CreateCell(0).SetCellValue("id");
			data.CreateRow(1).CreateCell(0).SetCellValue("hero");

			using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
				workbook.Write(stream);

			try
			{
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					JsonOutputPath = "Generated",
				}, new MemoryOutput());

				Assert.That(result.Success, Is.False);
				Assert.That(result.Errors, Has.Exactly(1).Contains("HERO_1 has a non-integer value 'not-a-number'"));
			}
			finally
			{
				File.Delete(path);
			}
		}

		// Text cells, not numeric: a numeric cell round-trips through NPOI's formatter, so the parser
		// under test would be fed whatever that decides ("1" or "1.0") rather than what the test wrote.
		private static void WriteId(NPOI.SS.UserModel.IRow row, string key, string value)
		{
			row.CreateCell(0).SetCellValue(key);
			row.CreateCell(1).SetCellValue(value);
		}

		private static void CreateDataSheet(XSSFWorkbook workbook, string sheetName, string value)
		{
			var sheet = workbook.CreateSheet(sheetName);
			sheet.CreateRow(0).CreateCell(0).SetCellValue("id");
			sheet.CreateRow(1).CreateCell(0).SetCellValue(value);
		}

		private static string CreateWorkbook(string sheetName, string[] headers, string[] values)
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet(sheetName);
			var headerRow = sheet.CreateRow(0);
			var valueRow = sheet.CreateRow(1);
			for (int i = 0; i < headers.Length; i++)
			{
				headerRow.CreateCell(i).SetCellValue(headers[i]);
				valueRow.CreateCell(i).SetCellValue(values[i]);
			}
			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
			workbook.Write(stream);
			return path;
		}

		private sealed class MemoryOutput : ISheetXOutput
		{
			public readonly Dictionary<string, string> Writes = new Dictionary<string, string>();

			public void Write(string relativePath, string content)
			{
				Writes.Add(relativePath, content);
			}
		}
	}
}
