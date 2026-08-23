using System;
using System.Collections.Generic;
using System.IO;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetXBatchValidationTests
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

		/// <summary>Writes a workbook whose sheets each hold one data row.</summary>
		private string CreateWorkbookWithDataSheets(params string[] sheetNames)
		{
			var workbook = new XSSFWorkbook();
			foreach (string name in sheetNames)
			{
				var sheet = workbook.CreateSheet(name);
				sheet.CreateRow(0).CreateCell(0).SetCellValue("id");
				sheet.CreateRow(1).CreateCell(0).SetCellValue("1");
			}

			return SaveWorkbook(workbook);
		}

		/// <summary>Writes a workbook holding one IDs sheet with one key.</summary>
		private string CreateIdsWorkbook(string sheetName, string key, string value)
		{
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet(sheetName);

			var header = sheet.CreateRow(0);
			header.CreateCell(0).SetCellValue("Hero");

			var row = sheet.CreateRow(1);
			row.CreateCell(0).SetCellValue(key);
			row.CreateCell(1).SetCellValue(value);

			return SaveWorkbook(workbook);
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

		[Test]
		public void null_output_is_an_error()
		{
			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest(), null);

			Assert.That(result.Errors, Has.Exactly(1).EqualTo("Output is null."));
			Assert.That(result.Files, Is.Empty);
		}

		[Test]
		public void null_request_is_an_error()
		{
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(null, output);

			Assert.That(result.Errors, Has.Exactly(1).EqualTo("Request is null."));
			Assert.That(output.WriteOrder, Is.Empty);
		}

		[Test]
		public void null_sources_is_an_error()
		{
			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest(), new MemoryOutput());

			Assert.That(result.Errors, Has.Exactly(1).EqualTo("Sources is empty."));
		}

		[Test]
		public void empty_sources_is_an_error()
		{
			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					Sources = new List<SheetXBatchSource>(),
				},
				new MemoryOutput());

			Assert.That(result.Errors, Has.Exactly(1).EqualTo("Sources is empty."));
		}

		[Test]
		public void null_source_entry_is_an_error()
		{
			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					Sources = new List<SheetXBatchSource> { null },
				},
				new MemoryOutput());

			Assert.That(result.Errors, Has.Exactly(1).EqualTo("Source 0 is null."));
		}

		[Test]
		public void invalid_source_kind_is_an_error_before_dispatch()
		{
			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource
						{
							Kind = (SheetXSourceKind)99,
							SpreadsheetPath = "must-not-be-dispatched",
						},
					},
				},
				new MemoryOutput());

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo("Source 0 has an invalid Kind '99'."));
		}

		[Test]
		public void empty_spreadsheet_path_is_an_error()
		{
			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = "" },
					},
				},
				new MemoryOutput());

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo("Source 0 has an empty SpreadsheetPath."));
		}

		[Test]
		public void missing_excel_file_is_an_error()
		{
			string missing = Path.Combine(Path.GetTempPath(), "sheetx-absent.xlsx");

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = missing },
					},
				},
				new MemoryOutput());

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo($"Spreadsheet '{missing}' does not exist."));
		}

		[Test]
		public void missing_google_credentials_is_an_error()
		{
			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource
						{
							Kind = SheetXSourceKind.Google,
							SpreadsheetPath = "sheet-id",
						},
					},
				},
				new MemoryOutput());

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					"GoogleClientId and GoogleClientSecret are required "
					+ "for a Google export."));
		}

		[Test]
		public void duplicate_source_key_is_an_error()
		{
			string path = CreateWorkbookWithDataSheets("Data");

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = path },
						new SheetXBatchSource { SpreadsheetPath = path },
					},
				},
				new MemoryOutput());

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Sources 0 and 1 are the same spreadsheet '{path}'."));
		}

		[TestCase("")]
		[TestCase("   ")]
		[TestCase("a/b")]
		[TestCase("a\\b")]
		[TestCase(".")]
		[TestCase("..")]
		[TestCase("na\x01me")]
		public void malformed_output_name_is_an_error(string outputName)
		{
			string path = CreateWorkbookWithDataSheets("Data");

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource
						{
							SpreadsheetPath = path,
							OutputName = outputName,
						},
					},
				},
				new MemoryOutput());

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Source 0 has an invalid OutputName '{outputName}'."));
		}

		[Test]
		public void a_missing_requested_sheet_is_an_error()
		{
			string path = CreateWorkbookWithDataSheets("Data");

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource
						{
							SpreadsheetPath = path,
							Sheets = new List<string> { "Data", "Absent" },
						},
					},
				},
				new MemoryOutput());

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Spreadsheet '{path}' has no sheet 'Absent'."));
		}

		[Test]
		public void an_empty_sheet_list_selects_nothing_and_succeeds()
		{
			string path = CreateWorkbookWithDataSheets("Data");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource
						{
							SpreadsheetPath = path,
							Sheets = new List<string>(),
						},
					},
				},
				output);

			Assert.That(result.Errors, Is.Empty);
			Assert.That(output.WriteOrder, Is.Empty);
		}

		[Test]
		public void duplicate_combined_json_output_name_is_an_error()
		{
			string a = CreateWorkbookWithDataSheets("Data");
			string b = CreateWorkbookWithDataSheets("Other");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					JsonOutputPath = "Generated",
					CombineJson = true,
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource
						{
							SpreadsheetPath = a,
							OutputName = "Shared",
						},
						new SheetXBatchSource
						{
							SpreadsheetPath = b,
							OutputName = "Shared",
						},
					},
				},
				output);

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Sources '{a}' and '{b}' both resolve to OutputName 'Shared'."));
			Assert.That(output.WriteOrder, Is.Empty);
		}

		[Test]
		public void a_localization_sheet_without_a_constants_folder_is_an_error()
		{
			string path = CreateWorkbookWithDataSheets("LocalizationEN");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					LocalizationOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = path },
					},
				},
				output);

			Assert.That(
				result.Errors,
				Has.Exactly(1).EqualTo(
					$"Spreadsheet '{path}' selects localization sheet 'LocalizationEN' "
					+ "but ConstantsOutputPath is empty."));
			Assert.That(output.WriteOrder, Is.Empty);
		}

		[Test]
		public void a_validation_failure_writes_nothing()
		{
			string good = CreateIdsWorkbook("HeroIDs", "HERO_1", "1");
			string missing = Path.Combine(Path.GetTempPath(), "sheetx-absent.xlsx");
			var output = new MemoryOutput();

			var result = SheetXExporter.ExportBatch(
				new SheetXBatchExportRequest
				{
					ConstantsOutputPath = "Generated",
					JsonOutputPath = "Generated",
					Sources = new List<SheetXBatchSource>
					{
						new SheetXBatchSource { SpreadsheetPath = good },
						new SheetXBatchSource { SpreadsheetPath = missing },
					},
				},
				output);

			Assert.That(result.Errors, Is.Not.Empty);
			Assert.That(output.WriteOrder, Is.Empty);
			Assert.That(result.Files, Is.Empty);
		}
	}
}
