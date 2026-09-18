/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Apis.Sheets.v4.Data;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	/// <summary>
	/// Stands in for a consuming project's row type. Its 'id' is an int, so a sheet holding text there
	/// proves the export's own array deserialization runs during preview.
	/// </summary>
	[Serializable, SheetXBindable]
	public sealed class PreviewStrictRow
	{
		public int id;
	}

	public class SheetPreviewSourceTests
	{
		private const string SourceId = "preview.xlsx";
		private const string OutputFolder = "Assets/SheetXPreviewTestsTemp";

		[Test]
		public void legacy_preview_unions_later_rows_and_resolves_ids_from_an_unchecked_sheet()
		{
			var settings = Settings();
			var workbook = LegacyWorkbook();
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, LegacySheets(), SourceId, "Heroes",
					out var data, out string error), Is.True, error);

				Assert.That(data.Mode, Is.EqualTo(SheetXSheetOutputMode.JsonOnly));
				// Union across rows: 'bot_ids' appears only in the second row.
				Assert.That(data.RootMemberNames, Is.EqualTo(new[] { "id", "name", "bot_ids" }));
				// HeroIDs is unchecked, yet HERO_1 still resolves — Excel loads every IDs sheet.
				Assert.That(data.Json, Does.Contain("\"id\":7"));
				Assert.That(data.Json, Does.Contain("\"bot_ids\":[7,7]"));
				// Legacy keys are kept verbatim, never re-cased into botIds.
				Assert.That(data.ClassCode, Does.Contain("public int[] bot_ids;"));
				Assert.That(data.FetchedAtUtc, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-5)));

				// The field metadata rides along on the one inference that produced ClassCode, so the window
				// never parses this Json a second time to fill its table.
				Assert.That(data.Fields.Select(f => f.JsonName), Is.EqualTo(data.RootMemberNames));
				foreach (var field in data.Fields)
				{
					Assert.That(data.ClassCode, Does.Contain($"public {field.FieldType} {field.FieldName};"),
						$"Field metadata for '{field.JsonName}' disagrees with the emitted code.");
				}
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void generated_preview_emits_row_type_code_without_json_or_paths()
		{
			var settings = Settings();
			settings.enableCollections = true;
			var workbook = GeneratedWorkbook();
			try
			{
				Bind(settings, "Bots", SheetXSheetOutputMode.GeneratedDataClass);

				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, GeneratedSheets(), SourceId, "Bots",
					out var data, out string error), Is.True, error);

				Assert.That(data.Mode, Is.EqualTo(SheetXSheetOutputMode.GeneratedDataClass));
				Assert.That(data.Schema, Is.Not.Null);
				Assert.That(data.ClassCode, Does.Contain("public int[] botIds;"));
				Assert.That(data.Json, Is.Null);
				Assert.That(data.ClassCode, Does.Not.Contain("SheetXCollectionPaths"));
				// Schema-backed code is the real emitted class, so it carries no inferred draft metadata and
				// no draft header commentary.
				Assert.That(data.Fields, Is.Empty);
				Assert.That(data.ClassCode, Does.Not.Contain("Inferred from this sheet's exported JSON"));
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void header_only_sheet_reports_the_empty_data_error()
		{
			var settings = Settings();
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet("Heroes");
			sheet.CreateRow(0).CreateCell(0).SetCellValue("id");
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, Sheets(("Heroes", true)), SourceId, "Heroes",
					out _, out string error), Is.False);
				Assert.That(error, Is.EqualTo("Cannot infer structure from empty exported data"));
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void missing_header_row_is_an_error()
		{
			var settings = Settings();
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet("Heroes");
			var row = sheet.CreateRow(1);
			row.CreateCell(0).SetCellValue("orphan");
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, Sheets(("Heroes", true)), SourceId, "Heroes",
					out _, out string error), Is.False);
				Assert.That(error, Is.EqualTo("Sheet 'Heroes' has no header row."));
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void missing_sheet_is_an_error()
		{
			var settings = Settings();
			var workbook = LegacyWorkbook();
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, LegacySheets(), SourceId, "Ghosts",
					out _, out string error), Is.False);
				Assert.That(error, Is.EqualTo("Sheet 'Ghosts' was not found in the workbook."));
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void converter_error_rejects_the_preview_even_when_json_parses()
		{
			var settings = Settings();
			var workbook = LegacyWorkbook();
			// A second HERO_1 row: the converter reports a duplicated ID while the sheet still
			// produces parseable Json. A partial snapshot presented as structure would be a lie.
			var ids = workbook.GetSheet("HeroIDs");
			var duplicate = ids.CreateRow(2);
			duplicate.CreateCell(0).SetCellValue("HERO_1");
			duplicate.CreateCell(1).SetCellValue("9");
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, LegacySheets(), SourceId, "Heroes",
					out _, out string error), Is.False);
				Assert.That(error, Does.Contain("ID HERO_1 is duplicated in sheet HeroIDs"));
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void preview_writes_nothing_and_leaves_settings_untouched()
		{
			var settings = Settings();
			var workbook = LegacyWorkbook();
			var sheets = LegacySheets();
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, sheets, SourceId, "Heroes",
					out _, out string error), Is.True, error);

				Assert.That(Directory.Exists(OutputFolder), Is.False, "Preview wrote an artifact.");
				Assert.That(settings.sheetBindings, Is.Empty, "Preview created a binding.");
				Assert.That(settings.excelSheetsPath, Is.Null, "Preview assigned excelSheetsPath.");
				Assert.That(sheets.Single(s => s.name == "HeroIDs").selected, Is.False,
					"Preview changed a sheet's selection.");
				Assert.That(sheets.Single(s => s.name == "Heroes").selected, Is.True,
					"Preview changed a sheet's selection.");
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void existing_data_class_mismatch_is_reported_as_a_conversion_diagnostic()
		{
			var settings = Settings();
			settings.enableCollections = true;
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet("Strict");
			sheet.CreateRow(0).CreateCell(0).SetCellValue("id");
			sheet.CreateRow(1).CreateCell(0).SetCellValue("abc");
			try
			{
				Bind(settings, "Strict", SheetXSheetOutputMode.ExistingDataClass,
					typeof(PreviewStrictRow).AssemblyQualifiedName);

				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, Sheets(("Strict", true)), SourceId, "Strict",
					out var data, out string error), Is.True, error);

				Assert.That(data.Mode, Is.EqualTo(SheetXSheetOutputMode.ExistingDataClass));
				Assert.That(data.Json, Does.Contain("\"id\":\"abc\""));
				// Read from the explicit row-type member, not the generic list: a direct facade caller has no
				// live binding to re-derive against, so this is where its compatibility story lives.
				Assert.That(data.RowTypeDiagnostics.Any(d => d.Contains("does not map onto")), Is.True,
					"Export would reject this sheet, so preview must say so: "
					+ string.Join(" | ", data.RowTypeDiagnostics));
				Assert.That(data.Diagnostics.Any(d => d.Contains("does not map onto")), Is.False,
					"A row-type finding must not also sit in the generic list, where a window would render "
					+ "it against whichever Data Class is bound later.");
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void invalid_collection_namespace_is_a_preview_error()
		{
			var settings = Settings();
			settings.collectionNamespace = "not a namespace";
			var workbook = LegacyWorkbook();
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, LegacySheets(), SourceId, "Heroes",
					out _, out string error), Is.False);
				Assert.That(error, Does.Contain("not a valid namespace"));
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void invalid_collection_namespace_is_a_preview_error_in_generated_mode_too()
		{
			var settings = Settings();
			settings.enableCollections = true;
			settings.collectionNamespace = "not a namespace";
			var workbook = GeneratedWorkbook();
			try
			{
				Bind(settings, "Bots", SheetXSheetOutputMode.GeneratedDataClass);

				// The generator appends the namespace unchecked, so this branch used to show uncompilable
				// code labelled 'Generated class preview' as a clean success — for settings under which the
				// export writes nothing at all.
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, GeneratedSheets(), SourceId, "Bots",
					out var data, out string error), Is.False);
				Assert.That(error, Does.Contain("not a valid namespace"));
				Assert.That(data, Is.Null, "An invalid namespace must not produce a snapshot to copy from.");
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void a_whitespace_only_namespace_is_rejected_rather_than_treated_as_absent()
		{
			var settings = Settings();
			settings.collectionNamespace = "   ";
			var workbook = LegacyWorkbook();
			try
			{
				// Not empty to the emitters, which would write 'namespace    { }'. A blank-ish check that
				// waved this through would emit uncompilable code.
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, LegacySheets(), SourceId, "Heroes",
					out _, out string error), Is.False);
				Assert.That(error, Does.Contain("not a valid namespace"));
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void a_valid_nested_namespace_is_accepted()
		{
			var settings = Settings();
			settings.collectionNamespace = "MyCompany.GameData.Tables";
			var workbook = LegacyWorkbook();
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, LegacySheets(), SourceId, "Heroes",
					out var data, out string error), Is.True, error);
				Assert.That(data.ClassCode, Does.Contain("namespace MyCompany.GameData.Tables"));
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void an_empty_namespace_is_accepted_and_emits_no_namespace()
		{
			var settings = Settings();
			settings.collectionNamespace = "";
			settings.@namespace = "";
			var workbook = LegacyWorkbook();
			try
			{
				// Empty means 'emit no namespace', which both emitters already handle. It is the one
				// deliberate difference from the export's own validator, which reports it as a settings issue.
				Assert.That(SheetXSheetJsonSource.TryLoadExcel(
					settings, workbook, LegacySheets(), SourceId, "Heroes",
					out var data, out string error), Is.True, error);
				Assert.That(data.ClassCode, Does.Not.Contain("namespace "));
			}
			finally
			{
				Cleanup(settings, workbook);
			}
		}

		[Test]
		public void try_load_opens_the_workbook_named_by_the_source()
		{
			var settings = Settings();
			string path = SaveWorkbook(LegacyWorkbook());
			try
			{
				var source = new SheetXSheetSource
				{
					Kind = SheetXSourceKind.Excel,
					Id = path,
					Sheets = LegacySheets(),
				};

				Assert.That(SheetXSheetJsonSource.TryLoad(settings, source, "Heroes", out var data, out string error),
					Is.True, error);
				Assert.That(data.Json, Does.Contain("\"id\":7"));
			}
			finally
			{
				File.Delete(path);
				Cleanup(settings, null);
			}
		}

		[Test]
		public void missing_workbook_file_is_an_error_through_try_load()
		{
			var settings = Settings();
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-missing-{Guid.NewGuid():N}.xlsx");
			try
			{
				var source = new SheetXSheetSource
				{
					Kind = SheetXSourceKind.Excel,
					Id = path,
					Sheets = Sheets(("Heroes", true)),
				};

				Assert.That(SheetXSheetJsonSource.TryLoad(settings, source, "Heroes", out _, out string error),
					Is.False);
				Assert.That(error, Does.Contain(path));
				Assert.That(error, Does.Contain("does not exist"));
			}
			finally
			{
				Cleanup(settings, null);
			}
		}

		[Test]
		public void google_preview_resolves_ids_from_a_selected_ids_sheet()
		{
			var settings = Settings();
			var fetcher = GoogleFetcher(
				("HeroIDs!A1:B", HeroIdRows()),
				("Heroes!A1:C", HeroRows()));
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadGoogle(
					settings, GoogleMetadata(("HeroIDs", 2), ("Heroes", 3)), fetcher.Fetch,
					GoogleSourceId, GoogleSheets(idsSelected: true), "Heroes",
					out var data, out string error), Is.True, error);

				Assert.That(data.Mode, Is.EqualTo(SheetXSheetOutputMode.JsonOnly));
				Assert.That(data.Json, Does.Contain("\"id\":7"));
				Assert.That(data.Json, Does.Contain("\"bot_ids\":[7,7]"));
				Assert.That(data.RootMemberNames, Is.EqualTo(new[] { "id", "name", "bot_ids" }));
				// The export's own range formula, column letter and all.
				Assert.That(fetcher.Requested, Is.EqualTo(new[] { "HeroIDs!A1:B", "Heroes!A1:C" }));
			}
			finally
			{
				Cleanup(settings, null);
			}
		}

		[Test]
		public void google_preview_ignores_an_unselected_ids_sheet()
		{
			var settings = Settings();
			var fetcher = GoogleFetcher(
				("HeroIDs!A1:B", HeroIdRows()),
				("Heroes!A1:C", HeroRows()));
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadGoogle(
					settings, GoogleMetadata(("HeroIDs", 2), ("Heroes", 3)), fetcher.Fetch,
					GoogleSourceId, GoogleSheets(idsSelected: false), "Heroes",
					out var data, out string error), Is.True, error);

				// Deliberately unlike Excel: GetSheetIDsValues skips an unchecked IDs sheet, so HERO_1
				// stays literal text. A preview that resolved it would show structure export does not produce.
				Assert.That(data.Json, Does.Contain("\"id\":\"HERO_1\""));
				Assert.That(data.Json, Does.Not.Contain("\"id\":7"));
				Assert.That(fetcher.Requested, Is.EqualTo(new[] { "Heroes!A1:C" }),
					"An unchecked IDs sheet must not even be fetched.");
			}
			finally
			{
				Cleanup(settings, null);
			}
		}

		[Test]
		public void google_missing_sheet_is_an_error()
		{
			var settings = Settings();
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadGoogle(
					settings, GoogleMetadata(("Heroes", 3)), GoogleFetcher().Fetch,
					GoogleSourceId, GoogleSheets(idsSelected: true), "Ghosts",
					out _, out string error), Is.False);
				Assert.That(error, Is.EqualTo(
					$"Google spreadsheet '{GoogleSourceId}' has no sheet 'Ghosts'."));
			}
			finally
			{
				Cleanup(settings, null);
			}
		}

		[Test]
		public void google_missing_column_count_is_an_error_not_an_exception()
		{
			var settings = Settings();
			try
			{
				// GridProperties.ColumnCount is a nullable the export dereferences with .Value.
				Assert.That(SheetXSheetJsonSource.TryLoadGoogle(
					settings, GoogleMetadata(("Heroes", null)), GoogleFetcher().Fetch,
					GoogleSourceId, Sheets(("Heroes", true)), "Heroes",
					out _, out string error), Is.False);
				Assert.That(error, Is.EqualTo(
					$"Google spreadsheet '{GoogleSourceId}' sheet 'Heroes' has no grid column count."));
			}
			finally
			{
				Cleanup(settings, null);
			}
		}

		[Test]
		public void google_fetch_failure_is_an_error_not_an_exception()
		{
			var settings = Settings();
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadGoogle(
					settings, GoogleMetadata(("Heroes", 3)),
					_ => throw new InvalidOperationException("network is down"),
					GoogleSourceId, Sheets(("Heroes", true)), "Heroes",
					out _, out string error), Is.False);
				Assert.That(error, Is.EqualTo(
					$"Could not read Google spreadsheet '{GoogleSourceId}': network is down"));
			}
			finally
			{
				Cleanup(settings, null);
			}
		}

		[Test]
		public void google_preview_leaves_metadata_selection_and_settings_untouched()
		{
			var settings = Settings();
			var metadata = GoogleMetadata(("HeroIDs", 2), ("Heroes", 3));
			var sheets = GoogleSheets(idsSelected: false);
			var fetcher = GoogleFetcher(
				("HeroIDs!A1:B", HeroIdRows()),
				("Heroes!A1:C", HeroRows()));
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoadGoogle(
					settings, metadata, fetcher.Fetch, GoogleSourceId, sheets, "Heroes",
					out _, out string error), Is.True, error);

				Assert.That(Directory.Exists(OutputFolder), Is.False, "Preview wrote an artifact.");
				Assert.That(settings.sheetBindings, Is.Empty, "Preview created a binding.");
				Assert.That(settings.googleSheetsPath, Is.Null, "Preview assigned googleSheetsPath.");
				// ValidateSheetPaths would have added, removed, and re-selected entries here.
				Assert.That(metadata.Sheets.Count, Is.EqualTo(2), "Preview changed the metadata.");
				Assert.That(sheets.Single(s => s.name == "HeroIDs").selected, Is.False,
					"Preview changed a sheet's selection.");
				Assert.That(sheets.Single(s => s.name == "Heroes").selected, Is.True,
					"Preview changed a sheet's selection.");
			}
			finally
			{
				Cleanup(settings, null);
			}
		}

		[Test]
		public void google_generated_preview_emits_row_type_code_without_json()
		{
			var settings = Settings();
			settings.enableCollections = true;
			var fetcher = GoogleFetcher(("Bots!A1:B", Rows(
				new[] { "id:int", "botIds[]:int" },
				new[] { "1", "2|3" })));
			try
			{
				Bind(settings, "Bots", SheetXSheetOutputMode.GeneratedDataClass, sourceId: GoogleSourceId);

				Assert.That(SheetXSheetJsonSource.TryLoadGoogle(
					settings, GoogleMetadata(("Bots", 2)), fetcher.Fetch,
					GoogleSourceId, Sheets(("Bots", true)), "Bots",
					out var data, out string error), Is.True, error);

				Assert.That(data.Mode, Is.EqualTo(SheetXSheetOutputMode.GeneratedDataClass));
				Assert.That(data.Schema, Is.Not.Null);
				Assert.That(data.ClassCode, Does.Contain("public int[] botIds;"));
				Assert.That(data.Json, Is.Null);
			}
			finally
			{
				Cleanup(settings, null);
			}
		}

		[Test]
		public void try_load_reads_google_through_the_injected_connector_and_disposes_it()
		{
			var settings = Settings();
			var original = SheetXSheetJsonSource.GoogleConnector;
			var originalCredentials = SheetXSheetJsonSource.HasGoogleCredentials;
			var service = new TrackedService();
			var fetcher = GoogleFetcher(
				("HeroIDs!A1:B", HeroIdRows()),
				("Heroes!A1:C", HeroRows()));
			string requestedId = null;
			try
			{
				SheetXSheetJsonSource.HasGoogleCredentials = _ => true;
				SheetXSheetJsonSource.GoogleConnector = (_, id) =>
				{
					requestedId = id;
					return (GoogleMetadata(("HeroIDs", 2), ("Heroes", 3)), fetcher.Fetch, service);
				};

				var source = new SheetXSheetSource
				{
					Kind = SheetXSourceKind.Google,
					Id = GoogleSourceId,
					Sheets = GoogleSheets(idsSelected: true),
				};

				Assert.That(SheetXSheetJsonSource.TryLoad(settings, source, "Heroes", out var data, out string error),
					Is.True, error);
				Assert.That(requestedId, Is.EqualTo(GoogleSourceId));
				Assert.That(data.Json, Does.Contain("\"id\":7"));
				Assert.That(service.Disposed, Is.True, "The Sheets service was left open.");
			}
			finally
			{
				SheetXSheetJsonSource.GoogleConnector = original;
				SheetXSheetJsonSource.HasGoogleCredentials = originalCredentials;
				Cleanup(settings, null);
			}
		}

		[Test]
		public void google_connector_failure_is_an_error_through_try_load()
		{
			var settings = Settings();
			var original = SheetXSheetJsonSource.GoogleConnector;
			var originalCredentials = SheetXSheetJsonSource.HasGoogleCredentials;
			try
			{
				SheetXSheetJsonSource.HasGoogleCredentials = _ => true;
				SheetXSheetJsonSource.GoogleConnector =
					(_, __) => throw new InvalidOperationException("access denied");

				var source = new SheetXSheetSource
				{
					Kind = SheetXSourceKind.Google,
					Id = GoogleSourceId,
					Sheets = GoogleSheets(idsSelected: true),
				};

				Assert.That(SheetXSheetJsonSource.TryLoad(settings, source, "Heroes", out _, out string error),
					Is.False);
				Assert.That(error, Is.EqualTo(
					$"Could not read Google spreadsheet '{GoogleSourceId}': access denied"));
			}
			finally
			{
				SheetXSheetJsonSource.GoogleConnector = original;
				SheetXSheetJsonSource.HasGoogleCredentials = originalCredentials;
				Cleanup(settings, null);
			}
		}

		[Test]
		public void missing_google_credentials_are_an_error_through_try_load()
		{
			var settings = Settings();
			var original = SheetXSheetJsonSource.GoogleConnector;
			var originalCredentials = SheetXSheetJsonSource.HasGoogleCredentials;
			try
			{
				SheetXSheetJsonSource.HasGoogleCredentials = _ => false;
				SheetXSheetJsonSource.GoogleConnector =
					(_, __) => throw new InvalidOperationException("the connector must not be reached");

				var source = new SheetXSheetSource
				{
					Kind = SheetXSourceKind.Google,
					Id = GoogleSourceId,
					Sheets = GoogleSheets(idsSelected: true),
				};

				Assert.That(SheetXSheetJsonSource.TryLoad(settings, source, "Heroes", out _, out string error),
					Is.False);
				Assert.That(error, Is.EqualTo("Google Client ID or Client Secret is missing."));
			}
			finally
			{
				SheetXSheetJsonSource.GoogleConnector = original;
				SheetXSheetJsonSource.HasGoogleCredentials = originalCredentials;
				Cleanup(settings, null);
			}
		}

		// The reported defect: 'NONE' lives in another workbook of the Export Multi Files list, exactly as
		// GameData.xlsx defines it for Challenges.xlsx.
		[Test]
		public void multi_file_preview_resolves_an_id_defined_only_in_another_listed_file()
		{
			var settings = Settings();
			string quests = SaveWorkbook(QuestsWorkbook());
			string shared = SaveWorkbook(SharedIdsWorkbook("NONE", "0"));
			try
			{
				ListMultiFile(settings, (quests, QuestSheets()), (shared, SharedIdsSheets(true)));

				Assert.That(SheetXSheetJsonSource.TryLoad(
					settings, ExcelMultiSource(quests, QuestSheets()), "DailyQuests",
					out var data, out string error), Is.True, error);

				// Resolved to the number, so the draft infers int rather than string.
				Assert.That(data.Json, Does.Contain("\"targetId\":0"));
				Assert.That(data.Json, Does.Not.Contain("NONE"));
				Assert.That(data.ClassCode, Does.Contain("public int targetId;"));
				Assert.That(data.MultiFileIds, Is.True);
			}
			finally
			{
				File.Delete(quests);
				File.Delete(shared);
				Cleanup(settings, null);
			}
		}

		[Test]
		public void preview_of_a_file_outside_the_multi_file_list_keeps_single_workbook_ids()
		{
			var settings = Settings();
			string quests = SaveWorkbook(QuestsWorkbook());
			string shared = SaveWorkbook(SharedIdsWorkbook("NONE", "0"));
			try
			{
				// Only the other file is listed, so previewing this one must not borrow its namespace.
				ListMultiFile(settings, (shared, SharedIdsSheets(true)));

				Assert.That(SheetXSheetJsonSource.TryLoad(
					settings, ExcelMultiSource(quests, QuestSheets()), "DailyQuests",
					out var data, out string error), Is.True, error);

				Assert.That(data.Json, Does.Contain("\"targetId\":\"NONE\""));
				Assert.That(data.ClassCode, Does.Contain("public string targetId;"));
				Assert.That(data.MultiFileIds, Is.False);
			}
			finally
			{
				File.Delete(quests);
				File.Delete(shared);
				Cleanup(settings, null);
			}
		}

		[Test]
		public void multi_file_preview_loads_an_unchecked_ids_sheet_in_another_file()
		{
			var settings = Settings();
			string quests = SaveWorkbook(QuestsWorkbook());
			string shared = SaveWorkbook(SharedIdsWorkbook("NONE", "0"));
			try
			{
				// Unchecked, as ExportAllFiles' IDs pass ignores the per-sheet checkbox.
				ListMultiFile(settings, (quests, QuestSheets()), (shared, SharedIdsSheets(false)));

				Assert.That(SheetXSheetJsonSource.TryLoad(
					settings, ExcelMultiSource(quests, QuestSheets()), "DailyQuests",
					out var data, out string error), Is.True, error);

				Assert.That(data.Json, Does.Contain("\"targetId\":0"));
				Assert.That(data.MultiFileIds, Is.True);
			}
			finally
			{
				File.Delete(quests);
				File.Delete(shared);
				Cleanup(settings, null);
			}
		}

		[Test]
		public void a_listed_file_missing_from_disk_is_skipped_and_the_preview_still_succeeds()
		{
			var settings = Settings();
			string quests = SaveWorkbook(QuestsWorkbook());
			string shared = SaveWorkbook(SharedIdsWorkbook("NONE", "0"));
			string absent = Path.Combine(Path.GetTempPath(), $"sheetx-absent-{Guid.NewGuid():N}.xlsx");
			try
			{
				ListMultiFile(settings,
					(absent, SharedIdsSheets(true)),
					(quests, QuestSheets()),
					(shared, SharedIdsSheets(true)));

				Assert.That(SheetXSheetJsonSource.TryLoad(
					settings, ExcelMultiSource(quests, QuestSheets()), "DailyQuests",
					out var data, out string error), Is.True, error);

				// The surviving files still resolved the ID.
				Assert.That(data.Json, Does.Contain("\"targetId\":0"));
			}
			finally
			{
				File.Delete(quests);
				File.Delete(shared);
				Cleanup(settings, null);
			}
		}

		// The decision behind this: LoadSheetIDsValues raises a duplicate through m_writer.Blocking, which
		// for ExcelSheetXWindow — the only caller of ExportAllFiles, built with no context — is a dialog the
		// user clicks through while the export finishes and writes its files, first definition winning.
		// Through the preview's detached context that same call is a result error, which would reject the
		// snapshot. Matching the export means a note, not a failure.
		[Test]
		public void a_duplicate_id_across_listed_files_warns_first_wins_and_does_not_fail_the_preview()
		{
			var settings = Settings();
			string quests = SaveWorkbook(QuestsWorkbook());
			string first = SaveWorkbook(SharedIdsWorkbook("NONE", "0"));
			string second = SaveWorkbook(SharedIdsWorkbook("NONE", "9"));
			try
			{
				ListMultiFile(settings,
					(quests, QuestSheets()),
					(first, SharedIdsSheets(true)),
					(second, SharedIdsSheets(true)));

				Assert.That(SheetXSheetJsonSource.TryLoad(
					settings, ExcelMultiSource(quests, QuestSheets()), "DailyQuests",
					out var data, out string error), Is.True, error);

				// First wins: 0 from the earlier file, never 9 from the later one.
				Assert.That(data.Json, Does.Contain("\"targetId\":0"));
				Assert.That(data.Diagnostics.Any(d => d.Contains("NONE") && d.Contains("duplicated")),
					Is.True, "The duplicate was not surfaced as a diagnostic.");
			}
			finally
			{
				File.Delete(quests);
				File.Delete(first);
				File.Delete(second);
				Cleanup(settings, null);
			}
		}

		// Guards the window's help text, whose ID line is chosen by this flag.
		[Test]
		public void the_multi_file_flag_tracks_which_id_namespace_was_used()
		{
			var settings = Settings();
			string quests = SaveWorkbook(QuestsWorkbook());
			try
			{
				Assert.That(SheetXSheetJsonSource.TryLoad(
					settings, ExcelMultiSource(quests, QuestSheets()), "DailyQuests",
					out var alone, out string error), Is.True, error);
				Assert.That(alone.MultiFileIds, Is.False, "An unlisted file claimed the multi-file namespace.");

				ListMultiFile(settings, (quests, QuestSheets()));
				Assert.That(SheetXSheetJsonSource.TryLoad(
					settings, ExcelMultiSource(quests, QuestSheets()), "DailyQuests",
					out var listed, out error), Is.True, error);
				Assert.That(listed.MultiFileIds, Is.True, "A listed file kept the source-local namespace.");
			}
			finally
			{
				File.Delete(quests);
				Cleanup(settings, null);
			}
		}

		// The Google counterpart of the reported Excel defect: 'NONE' is defined only in a second listed
		// spreadsheet, exactly as ExportAllFiles resolves it across the whole Google list.
		[Test]
		public void google_multi_preview_resolves_an_id_defined_only_in_another_listed_spreadsheet()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var data, out string error),
					Is.True, error);

				// Resolved to the number, so the draft infers int rather than string.
				Assert.That(data.Json, Does.Contain("\"targetId\":0"));
				Assert.That(data.Json, Does.Not.Contain("NONE"));
				Assert.That(data.ClassCode, Does.Contain("public int targetId;"));
				Assert.That(data.MultiFileIds, Is.True);
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// The Excel/Google asymmetry. ExportAllFiles skips 'if (!googleSheets.selected) continue;', which
		// Excel's own loop does not, so a test written under Excel's rule would pass on a bug.
		[Test]
		public void google_multi_preview_skips_an_unselected_spreadsheet()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, false, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var data, out string error),
					Is.True, error);

				// Never resolved, so it stays the literal string the sheet holds.
				Assert.That(data.Json, Does.Contain("\"targetId\":\"NONE\""));
				Assert.That(data.ClassCode, Does.Contain("public string targetId;"));
				Assert.That(world.Opened, Does.Not.Contain(SharedSpreadsheetId),
					"An unselected spreadsheet was opened.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// The second half of the asymmetry: ExportAllFiles also skips 'if (!sheet.selected ...) continue;'.
		[Test]
		public void google_multi_preview_skips_an_unselected_ids_sheet_in_a_selected_spreadsheet()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", false))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var data, out string error),
					Is.True, error);

				Assert.That(data.Json, Does.Contain("\"targetId\":\"NONE\""));
				Assert.That(world.RequestedRanges, Does.Not.Contain("IDs!A1:B"),
					"An unselected IDs sheet was fetched.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		[Test]
		public void google_preview_of_a_spreadsheet_outside_the_list_keeps_single_spreadsheet_ids()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				// Only the other spreadsheet is listed, so previewing this one must not borrow its namespace.
				ListGoogleMulti(settings, (SharedSpreadsheetId, true, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var data, out string error),
					Is.True, error);

				Assert.That(data.Json, Does.Contain("\"targetId\":\"NONE\""));
				Assert.That(data.MultiFileIds, Is.False);
				Assert.That(world.Opened, Does.Not.Contain(SharedSpreadsheetId),
					"An unlisted source reached across the list.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// ExportAllFiles skips an unselected spreadsheet outright, so previewing one must not adopt a map
		// built without its own IDs sheets — that would resolve strictly fewer IDs than reading it alone.
		[Test]
		public void google_preview_of_an_unselected_listed_spreadsheet_keeps_single_spreadsheet_ids()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, false, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var data, out string error),
					Is.True, error);

				Assert.That(data.MultiFileIds, Is.False);
				// Its own QuestIDs sheet still resolved, so 'id' is the number it always was.
				Assert.That(data.Json, Does.Contain("\"id\":5"));
				Assert.That(data.Json, Does.Contain("\"targetId\":\"NONE\""));
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// The whole point of the cache: the cross-spreadsheet walk costs one connection plus one range fetch
		// per '*IDs' sheet, per listed spreadsheet. Refresh must defeat it, or a stale map survives the one
		// action that exists to clear it.
		[Test]
		public void google_multi_preview_caches_the_id_map_and_refresh_rebuilds_it()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out _, out string error),
					Is.True, error);
				int afterFirst = world.CountOpened(SharedSpreadsheetId);
				Assert.That(afterFirst, Is.EqualTo(1), "The first preview did not fetch the other spreadsheet.");

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var second, out error),
					Is.True, error);
				Assert.That(world.CountOpened(SharedSpreadsheetId), Is.EqualTo(afterFirst),
					"The cache did not serve the second preview.");
				// Cached or not, the answer is the same one.
				Assert.That(second.Json, Does.Contain("\"targetId\":0"));

				SheetXSheetJsonSource.InvalidateGoogleIdCache();
				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var third, out error),
					Is.True, error);
				Assert.That(world.CountOpened(SharedSpreadsheetId), Is.EqualTo(afterFirst + 1),
					"Refresh served the cache instead of rebuilding it.");
				Assert.That(third.Json, Does.Contain("\"targetId\":0"));
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// Membership and selection decide what the map is built from, so a change to either must rebuild it
		// even without a Refresh — otherwise the user unticks a spreadsheet and the preview keeps its IDs.
		[Test]
		public void google_id_cache_rebuilds_when_the_list_selection_changes()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var first, out string error),
					Is.True, error);
				Assert.That(first.Json, Does.Contain("\"targetId\":0"));

				settings.googleSheetsPaths[1].selected = false;

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var second, out error),
					Is.True, error);
				Assert.That(second.Json, Does.Contain("\"targetId\":\"NONE\""),
					"A stale ID map survived a selection change.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		[Test]
		public void a_listed_spreadsheet_that_fails_to_open_is_skipped_and_the_preview_still_succeeds()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				world.FailOn("1BrokenSpreadsheetId", "access denied");
				ListGoogleMulti(settings,
					("1BrokenSpreadsheetId", true, Sheets(("IDs", true))),
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var data, out string error),
					Is.True, error);

				// The surviving spreadsheets still resolved the ID.
				Assert.That(data.Json, Does.Contain("\"targetId\":0"));
				Assert.That(data.Diagnostics.Any(d => d.Contains("1BrokenSpreadsheetId") && d.Contains("skipped")),
					Is.True, "The failed spreadsheet was not surfaced as a diagnostic.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// Task 11's ruling, applied to Google: LoadSheetIDsValues raises a duplicate through
		// m_writer.Blocking, which for GoogleSheetXWindow — the only caller of ExportAllFiles, built with no
		// context — is a dialog the user clicks through while the export finishes and writes its files, first
		// definition winning. Through the preview's detached context that same call is a result error, which
		// would reject the snapshot. Matching the export means a note, not a failure.
		[Test]
		public void a_duplicate_id_across_listed_spreadsheets_warns_first_wins_and_does_not_fail_the_preview()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				// A third spreadsheet redefines NONE as 9; the earlier definition of 0 must survive.
				world.AddSpreadsheet("1LateSpreadsheetId",
					("IDs", 2, Rows(new[] { "Shared" }, new[] { "NONE", "9" })));
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true))),
					("1LateSpreadsheetId", true, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var data, out string error),
					Is.True, error);

				// First wins: 0 from the earlier spreadsheet, never 9 from the later one.
				Assert.That(data.Json, Does.Contain("\"targetId\":0"));
				Assert.That(data.Diagnostics.Any(d => d.Contains("NONE") && d.Contains("duplicated")),
					Is.True, "The duplicate was not surfaced as a diagnostic.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// ValidateSheetPaths adds and removes entries in googleSheetsPath.sheets and rewrites selected flags.
		// That is an export side effect and must never run in a preview.
		[Test]
		public void google_multi_preview_does_not_mutate_the_listed_spreadsheet_paths()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true), ("Other", false))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out _, out string error),
					Is.True, error);

				Assert.That(settings.googleSheetsPaths.Count, Is.EqualTo(2), "Preview changed the list.");
				var shared = settings.googleSheetsPaths[1];
				Assert.That(shared.selected, Is.True, "Preview changed a spreadsheet's selection.");
				Assert.That(shared.sheets.Count, Is.EqualTo(2), "Preview added or removed a sheet entry.");
				Assert.That(shared.sheets[0].name, Is.EqualTo("IDs"));
				Assert.That(shared.sheets[0].selected, Is.True, "Preview changed a sheet's selection.");
				// The spreadsheet really has an 'Extra' sheet the list does not name; ValidateSheetPaths
				// would have added it here.
				Assert.That(shared.sheets.Any(s => s.name == "Extra"), Is.False,
					"Preview synced the sheet list from the spreadsheet.");
				Assert.That(shared.sheets[1].selected, Is.False, "Preview re-selected a sheet.");
				Assert.That(settings.googleSheetsPath, Is.Null, "Preview assigned googleSheetsPath.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// F1. ExportAllFiles fetches metadata, runs ValidateSheetPaths — which AddSheet's every new remote
		// sheet as selected=true — and only then iterates. So a sheet created on the web since the last Edit
		// Spreadsheets visit is already in the list, selected, by the time the export reads IDs. The preview
		// must discover it the same way, in memory, without writing anything back.
		[Test]
		public void google_multi_preview_discovers_an_ids_sheet_the_saved_list_does_not_name()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				// Saved list names no IDs sheet at all for the shared spreadsheet, so the old pre-skip would
				// never even connect to it; the remote spreadsheet has had 'IDs' added since.
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("Other", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var data, out string error),
					Is.True, error);

				Assert.That(data.Json, Does.Contain("\"targetId\":0"),
					"A newly added remote IDs sheet was not discovered.");
				Assert.That(data.ClassCode, Does.Contain("public int targetId;"));
				// Discovery is read-only: ValidateSheetPaths would have written the new name into the asset.
				Assert.That(settings.googleSheetsPaths[1].sheets.Any(s => s.name == "IDs"), Is.False,
					"Discovery wrote the new sheet back into the settings.");
				Assert.That(settings.googleSheetsPaths[1].sheets.Count, Is.EqualTo(1));
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// The other half of F1: discovery must not resurrect a sheet the user explicitly unticked.
		// ValidateSheetPaths preserves a saved entry's own checkbox, so an unticked IDs sheet stays out.
		[Test]
		public void google_multi_preview_still_ignores_an_explicitly_unchecked_ids_sheet()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", false))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var data, out string error),
					Is.True, error);

				Assert.That(data.Json, Does.Contain("\"targetId\":\"NONE\""),
					"Discovery overrode an explicit uncheck.");
				Assert.That(world.RequestedRanges, Does.Not.Contain("IDs!A1:B"),
					"An unchecked IDs sheet was fetched.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// F1 order. ValidateSheetPaths keeps surviving saved entries in SAVED order and appends newly
		// discovered names in metadata order. Iterating metadata instead would reverse these two IDs sheets
		// and hand the duplicate key to the wrong definition, silently changing first-wins.
		[Test]
		public void google_multi_discovery_keeps_saved_order_before_newly_discovered_sheets()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				// Metadata order is AlphaIDs (=1) then BetaIDs (=2); saved order names only BetaIDs.
				// Export order is therefore BetaIDs first, so DUP must resolve to 2, not 1.
				world.AddSpreadsheet("1OrderSpreadsheetId",
					("AlphaIDs", 2, Rows(new[] { "A" }, new[] { "DUP", "1" })),
					("BetaIDs", 2, Rows(new[] { "B" }, new[] { "DUP", "2" })));
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					("1OrderSpreadsheetId", true, Sheets(("BetaIDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DupTarget", out var data, out string error),
					Is.True, error);

				Assert.That(data.Json, Does.Contain("\"targetId\":2"),
					"Discovery order changed duplicate-key first-wins precedence.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// F2. A map built while one spreadsheet was unreachable is incomplete, and caching it made the
		// failure outlive the failure: previewing that very spreadsheet later served a map with none of its
		// own IDs, worse than the pre-Task-12 single-spreadsheet pass would have done.
		[Test]
		public void a_failed_spreadsheet_does_not_poison_its_own_later_preview()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true), ("SharedData", true))));

				world.FailOn(SharedSpreadsheetId, "expired token");
				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var first, out string error),
					Is.True, error);
				Assert.That(first.Json, Does.Contain("\"targetId\":\"NONE\""), "The failure did not bite.");

				// The spreadsheet is reachable now, and it is the one being previewed: its own IDs are in
				// hand on the already-open connection.
				world.Heal(SharedSpreadsheetId);
				Assert.That(world.Load(settings, SharedSpreadsheetId, "SharedData", out var second, out error),
					Is.True, error);
				Assert.That(second.Json, Does.Contain("\"ref\":0"),
					"A cached incomplete map hid the previewed spreadsheet's own IDs.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// F2 precedence. Recovery must rebuild in original list order, not top up an existing map, or the
		// recovered spreadsheet's definition would land after one it should have preceded.
		[Test]
		public void recovery_after_a_failure_rebuilds_first_wins_in_list_order()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				// Earlier in the list than the shared spreadsheet, and defines the same key with a different
				// value. Once it recovers, its 0 must win over the later 9.
				world.AddSpreadsheet("1EarlySpreadsheetId",
					("IDs", 2, Rows(new[] { "Early" }, new[] { "NONE", "0" })));
				world.AddSpreadsheet("1LateSpreadsheetId",
					("IDs", 2, Rows(new[] { "Late" }, new[] { "NONE", "9" })));
				ListGoogleMulti(settings,
					("1EarlySpreadsheetId", true, Sheets(("IDs", true))),
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					("1LateSpreadsheetId", true, Sheets(("IDs", true))));

				world.FailOn("1EarlySpreadsheetId", "transient network");
				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var first, out string error),
					Is.True, error);
				// Only the late definition was reachable.
				Assert.That(first.Json, Does.Contain("\"targetId\":9"));

				world.Heal("1EarlySpreadsheetId");
				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var second, out error),
					Is.True, error);
				Assert.That(second.Json, Does.Contain("\"targetId\":0"),
					"Recovery did not reclaim the earlier entry's first-wins precedence.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// F2 must detect an incomplete build from a failed IDs RANGE too, not only a failed connection.
		[Test]
		public void a_failed_ids_range_is_not_cached_as_a_complete_map()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				// Metadata lists 'IDs' but the range read throws, so the map is missing that spreadsheet's
				// keys even though the connection itself succeeded.
				world.AddSpreadsheet("1FlakySpreadsheetId", ("IDs", 2, null));
				world.FailRange("1FlakySpreadsheetId", "IDs!A1:B", "rate limited");
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					("1FlakySpreadsheetId", true, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out _, out string error),
					Is.True, error);
				int afterFirst = world.CountOpened("1FlakySpreadsheetId");

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out _, out error), Is.True, error);
				Assert.That(world.CountOpened("1FlakySpreadsheetId"), Is.GreaterThan(afterFirst),
					"A map missing a failed IDs range was cached as complete.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// F2 must not throw away the win: a clean build still caches, and Refresh still rebuilds.
		[Test]
		public void a_complete_map_is_still_cached_and_refresh_still_rebuilds()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true))));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out _, out string error),
					Is.True, error);
				int afterFirst = world.CountOpened(SharedSpreadsheetId);
				Assert.That(afterFirst, Is.EqualTo(1));

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out _, out error), Is.True, error);
				Assert.That(world.CountOpened(SharedSpreadsheetId), Is.EqualTo(afterFirst),
					"A complete map stopped being cached.");

				SheetXSheetJsonSource.InvalidateGoogleIdCache();
				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out _, out error), Is.True, error);
				Assert.That(world.CountOpened(SharedSpreadsheetId), Is.EqualTo(afterFirst + 1),
					"Refresh stopped rebuilding.");
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// F3, Google. The same spreadsheet sits in the single-source slot AND the multi list. The Single tab
		// runs ExportIDs/ExportJson over that one spreadsheet, so its preview must stay local even though
		// membership would widen it.
		[Test]
		public void google_single_host_keeps_local_ids_even_when_the_same_id_is_listed()
		{
			var settings = Settings();
			var world = GoogleWorld();
			try
			{
				ListGoogleMulti(settings,
					(QuestSpreadsheetId, true, Sheets(("QuestIDs", true), ("DailyQuests", true))),
					(SharedSpreadsheetId, true, Sheets(("IDs", true))));
				settings.googleSheetsPath = new GoogleSheetsPath
				{
					id = QuestSpreadsheetId,
					selected = true,
					sheets = Sheets(("QuestIDs", true), ("DailyQuests", true)),
				};

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var local, out string error,
					SheetXPreviewScope.Local), Is.True, error);
				Assert.That(local.MultiFileIds, Is.False, "A Single-tab preview claimed the multi namespace.");
				Assert.That(local.Json, Does.Contain("\"targetId\":\"NONE\""),
					"A Single-tab preview resolved a cross-source symbol its export cannot resolve.");
				Assert.That(world.Opened, Does.Not.Contain(SharedSpreadsheetId),
					"A Single-tab preview reached across the list.");

				Assert.That(world.Load(settings, QuestSpreadsheetId, "DailyQuests", out var multi, out error,
					SheetXPreviewScope.Multi), Is.True, error);
				Assert.That(multi.MultiFileIds, Is.True);
				Assert.That(multi.Json, Does.Contain("\"targetId\":0"));
			}
			finally
			{
				GoogleCleanup(world, settings);
			}
		}

		// F3, Excel. Same shape: a path in both excelSheetsPath and excelSheetsPaths previewed from the
		// Single tab must predict ExportAll, which reads that one workbook.
		[Test]
		public void excel_single_host_keeps_local_ids_even_when_the_same_path_is_listed()
		{
			var settings = Settings();
			string quests = SaveWorkbook(QuestsWorkbook());
			string shared = SaveWorkbook(SharedIdsWorkbook("NONE", "0"));
			try
			{
				ListMultiFile(settings, (quests, QuestSheets()), (shared, SharedIdsSheets(true)));
				settings.excelSheetsPath = new ExcelSheetsPath { path = quests, sheets = QuestSheets() };

				var local = ExcelSource(quests, QuestSheets());
				local.Scope = SheetXPreviewScope.Local;
				Assert.That(SheetXSheetJsonSource.TryLoad(settings, local, "DailyQuests",
					out var localData, out string error), Is.True, error);
				Assert.That(localData.MultiFileIds, Is.False, "A Single-tab preview claimed the multi namespace.");
				Assert.That(localData.Json, Does.Contain("\"targetId\":\"NONE\""),
					"A Single-tab preview resolved a cross-file symbol its export cannot resolve.");

				var multi = ExcelSource(quests, QuestSheets());
				multi.Scope = SheetXPreviewScope.Multi;
				Assert.That(SheetXSheetJsonSource.TryLoad(settings, multi, "DailyQuests",
					out var multiData, out error), Is.True, error);
				Assert.That(multiData.MultiFileIds, Is.True);
				Assert.That(multiData.Json, Does.Contain("\"targetId\":0"));
			}
			finally
			{
				File.Delete(quests);
				File.Delete(shared);
				Cleanup(settings, null);
			}
		}

		// The conservative default matters: a host that says nothing must not widen. Excel's default source
		// (Scope unset) over a listed path stays local.
		[Test]
		public void an_unscoped_source_defaults_to_local_ids()
		{
			var settings = Settings();
			string quests = SaveWorkbook(QuestsWorkbook());
			string shared = SaveWorkbook(SharedIdsWorkbook("NONE", "0"));
			try
			{
				ListMultiFile(settings, (quests, QuestSheets()), (shared, SharedIdsSheets(true)));

				// ExcelSource leaves Scope at its default.
				Assert.That(SheetXSheetJsonSource.TryLoad(
					settings, ExcelSource(quests, QuestSheets()), "DailyQuests",
					out var data, out string error), Is.True, error);
				Assert.That(data.MultiFileIds, Is.False, "The default scope widened the ID namespace.");
				Assert.That(data.Json, Does.Contain("\"targetId\":\"NONE\""));
			}
			finally
			{
				File.Delete(quests);
				File.Delete(shared);
				Cleanup(settings, null);
			}
		}

		private const string QuestSpreadsheetId = "1QuestSpreadsheetId";
		private const string SharedSpreadsheetId = "1SharedSpreadsheetId";

		/// <summary>
		/// An in-memory set of Google spreadsheets behind the connector seam. Nothing here touches the
		/// network; it records which spreadsheets were opened so a test can prove the cache served a second
		/// preview, and which ranges were fetched so a test can prove an unselected sheet was never read.
		/// </summary>
		private sealed class FakeGoogleWorld : IDisposable
		{
			private readonly Dictionary<string, (Spreadsheet metadata, RecordingFetcher fetcher)> m_spreadsheets =
				new Dictionary<string, (Spreadsheet, RecordingFetcher)>(StringComparer.Ordinal);
			private readonly Dictionary<string, string> m_failures =
				new Dictionary<string, string>(StringComparer.Ordinal);
			private readonly Dictionary<string, string> m_rangeFailures =
				new Dictionary<string, string>(StringComparer.Ordinal);
			private readonly List<string> m_opened = new List<string>();
			private readonly Func<SheetXSettings, string,
				(Spreadsheet, Func<string, IList<IList<object>>>, IDisposable)> m_originalConnector;
			private readonly Func<SheetXSettings, bool> m_originalCredentials;

			internal FakeGoogleWorld()
			{
				m_originalConnector = SheetXSheetJsonSource.GoogleConnector;
				m_originalCredentials = SheetXSheetJsonSource.HasGoogleCredentials;
				SheetXSheetJsonSource.HasGoogleCredentials = _ => true;
				SheetXSheetJsonSource.GoogleConnector = (_, id) => Open(id);
			}

			internal IReadOnlyList<string> Opened => m_opened;

			internal IEnumerable<string> RequestedRanges
				=> m_spreadsheets.Values.SelectMany(entry => entry.fetcher.Requested);

			internal int CountOpened(string spreadsheetId)
				=> m_opened.Count(id => string.Equals(id, spreadsheetId, StringComparison.Ordinal));

			internal void AddSpreadsheet(
				string spreadsheetId, params (string name, int? columns, IList<IList<object>> values)[] sheets)
			{
				var metadata = GoogleMetadata(sheets.Select(s => (s.name, s.columns)).ToArray());
				var ranges = sheets
					.Where(s => s.columns.HasValue && s.values != null)
					.ToDictionary(
						s => $"{s.name}!A1:{GoogleSheetHandler.GetColumnLetter(s.columns.Value)}",
						s => s.values,
						StringComparer.Ordinal);
				m_spreadsheets[spreadsheetId] = (metadata, new RecordingFetcher(ranges));
			}

			internal void FailOn(string spreadsheetId, string message) => m_failures[spreadsheetId] = message;

			/// <summary>Makes one range read throw although the connection itself succeeds.</summary>
			internal void FailRange(string spreadsheetId, string range, string message)
				=> m_rangeFailures[spreadsheetId + "\u0000" + range] = message;

			/// <summary>Clears a scheduled failure, so a later preview can prove recovery.</summary>
			internal void Heal(string spreadsheetId) => m_failures.Remove(spreadsheetId);

			/// <summary>Replaces a spreadsheet's sheets, standing in for an edit made on the web.</summary>
			internal void ReplaceSpreadsheet(
				string spreadsheetId, params (string name, int? columns, IList<IList<object>> values)[] sheets)
				=> AddSpreadsheet(spreadsheetId, sheets);

			// Mirrors how the window loads: through TryLoad, so the connector seam and the cross-spreadsheet
			// walk are both exercised exactly as the real window exercises them. Scope is what the opening
			// host would have set; Multi is the Export Multi Files tabs and the Edit windows they launch.
			internal bool Load(
				SheetXSettings settings,
				string spreadsheetId,
				string sheetName,
				out SheetXSheetPreviewData data,
				out string error,
				SheetXPreviewScope scope = SheetXPreviewScope.Multi)
			{
				var source = new SheetXSheetSource
				{
					Kind = SheetXSourceKind.Google,
					Id = spreadsheetId,
					Scope = scope,
					Sheets = settings.googleSheetsPaths
						.FirstOrDefault(p => p.id == spreadsheetId)?.sheets
						?? Sheets(("QuestIDs", true), ("DailyQuests", true)),
				};
				return SheetXSheetJsonSource.TryLoad(settings, source, sheetName, out data, out error);
			}

			private (Spreadsheet, Func<string, IList<IList<object>>>, IDisposable) Open(string spreadsheetId)
			{
				m_opened.Add(spreadsheetId);
				if (m_failures.TryGetValue(spreadsheetId, out string message))
					throw new InvalidOperationException(message);
				if (!m_spreadsheets.TryGetValue(spreadsheetId, out var entry))
					throw new InvalidOperationException($"no such spreadsheet '{spreadsheetId}'");
				return (entry.metadata, range => FetchOrFail(spreadsheetId, entry.fetcher, range), new TrackedService());
			}

			private IList<IList<object>> FetchOrFail(
				string spreadsheetId, RecordingFetcher fetcher, string range)
			{
				if (m_rangeFailures.TryGetValue(spreadsheetId + "\u0000" + range, out string message))
					throw new InvalidOperationException(message);
				return fetcher.Fetch(range);
			}

			public void Dispose()
			{
				SheetXSheetJsonSource.GoogleConnector = m_originalConnector;
				SheetXSheetJsonSource.HasGoogleCredentials = m_originalCredentials;
			}
		}

		// Mirrors the Excel fixtures: the quest spreadsheet's own IDs sheet defines TASK_* only, and its data
		// sheet references 'NONE', which only the shared spreadsheet defines. 'Extra' exists in the shared
		// spreadsheet but is deliberately absent from every list, so a ValidateSheetPaths-style sync shows up.
		private static FakeGoogleWorld GoogleWorld()
		{
			// Every preview starts from a clean cache, exactly as a domain reload leaves it.
			SheetXSheetJsonSource.InvalidateGoogleIdCache();
			var world = new FakeGoogleWorld();
			world.AddSpreadsheet(QuestSpreadsheetId,
				("QuestIDs", 2, Rows(new[] { "Quest" }, new[] { "TASK_1", "5" })),
				("DailyQuests", 2, Rows(
					new[] { "id", "targetId" },
					new[] { "TASK_1", "NONE" })),
				// References a key two IDs sheets define with different values, so a test can pin which
				// definition won.
				("DupTarget", 2, Rows(
					new[] { "id", "targetId" },
					new[] { "TASK_1", "DUP" })));
			world.AddSpreadsheet(SharedSpreadsheetId,
				("IDs", 2, Rows(new[] { "Shared" }, new[] { "NONE", "0" })),
				("Other", 1, Rows(new[] { "id" }, new[] { "1" })),
				// A data sheet in the shared spreadsheet itself, referencing its own IDs, so a test can
				// prove a cached incomplete map never hides a spreadsheet's own keys from its own preview.
				("SharedData", 2, Rows(
					new[] { "id", "ref" },
					new[] { "1", "NONE" })),
				("Extra", 1, Rows(new[] { "id" }, new[] { "1" })));
			return world;
		}

		private static void GoogleCleanup(FakeGoogleWorld world, SheetXSettings settings)
		{
			world.Dispose();
			SheetXSheetJsonSource.InvalidateGoogleIdCache();
			Cleanup(settings, null);
		}

		private static void ListGoogleMulti(
			SheetXSettings settings, params (string id, bool selected, List<SheetPath> sheets)[] paths)
		{
			settings.googleSheetsPaths = paths
				.Select(p => new GoogleSheetsPath { id = p.id, name = p.id, selected = p.selected, sheets = p.sheets })
				.ToList();
		}

		private static SheetXSheetSource ExcelSource(string path, List<SheetPath> sheets)
			=> new SheetXSheetSource { Kind = SheetXSourceKind.Excel, Id = path, Sheets = sheets };

		// What the Export Multi Files tab's Edit window builds: the same source, scoped to predict
		// ExportAllFiles rather than ExportAll.
		private static SheetXSheetSource ExcelMultiSource(string path, List<SheetPath> sheets)
			=> new SheetXSheetSource
			{
				Kind = SheetXSourceKind.Excel,
				Id = path,
				Sheets = sheets,
				Scope = SheetXPreviewScope.Multi,
			};

		private static void ListMultiFile(
			SheetXSettings settings, params (string path, List<SheetPath> sheets)[] files)
		{
			settings.excelSheetsPaths = files
				.Select(f => new ExcelSheetsPath { path = f.path, selected = true, sheets = f.sheets })
				.ToList();
		}

		private static List<SheetPath> QuestSheets()
			=> Sheets(("QuestIDs", true), ("DailyQuests", true));

		private static List<SheetPath> SharedIdsSheets(bool selected)
			=> Sheets(("IDs", selected), ("Other", true));

		// Mirrors Challenges.xlsx: its own IDs sheet defines TASK_* only, and the data sheet references a
		// symbolic id it does not define.
		private static IWorkbook QuestsWorkbook()
		{
			var workbook = new XSSFWorkbook();
			var ids = workbook.CreateSheet("QuestIDs");
			ids.CreateRow(0).CreateCell(0).SetCellValue("Quest");
			var idRow = ids.CreateRow(1);
			idRow.CreateCell(0).SetCellValue("TASK_1");
			idRow.CreateCell(1).SetCellValue("5");

			var quests = workbook.CreateSheet("DailyQuests");
			var header = quests.CreateRow(0);
			header.CreateCell(0).SetCellValue("id");
			header.CreateCell(1).SetCellValue("targetId");

			var row = quests.CreateRow(1);
			row.CreateCell(0).SetCellValue("TASK_1");
			row.CreateCell(1).SetCellValue("NONE");
			return workbook;
		}

		// Mirrors GameData.xlsx: a separate workbook whose 'IDs' sheet defines the missing key.
		private static IWorkbook SharedIdsWorkbook(string key, string value)
		{
			var workbook = new XSSFWorkbook();
			var ids = workbook.CreateSheet("IDs");
			ids.CreateRow(0).CreateCell(0).SetCellValue("Shared");
			var row = ids.CreateRow(1);
			row.CreateCell(0).SetCellValue(key);
			row.CreateCell(1).SetCellValue(value);

			var other = workbook.CreateSheet("Other");
			other.CreateRow(0).CreateCell(0).SetCellValue("id");
			other.CreateRow(1).CreateCell(0).SetCellValue("1");
			return workbook;
		}

		private static SheetXSettings Settings()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.silent = true;
			settings.jsonOutputFolder = OutputFolder;
			settings.constantsOutputFolder = OutputFolder;
			return settings;
		}

		private static void Bind(
			SheetXSettings settings, string sheetName, SheetXSheetOutputMode mode, string rowTypeName = null,
			string sourceId = SourceId)
		{
			var binding = SheetXCollectionSettings.GetOrCreateBinding(settings, sourceId, sheetName);
			binding.outputMode = mode;
			binding.rowTypeName = rowTypeName;
		}

		/// <summary>Stands in for the Sheets service so a test can prove the preview disposed it.</summary>
		private sealed class TrackedService : IDisposable
		{
			internal bool Disposed;

			public void Dispose() => Disposed = true;
		}

		// Records every range asked for, so a test can assert both the export's range formula and that an
		// unchecked IDs sheet was never fetched at all.
		private sealed class RecordingFetcher
		{
			private readonly Dictionary<string, IList<IList<object>>> m_ranges;
			private readonly List<string> m_requested = new List<string>();

			internal RecordingFetcher(Dictionary<string, IList<IList<object>>> ranges) => m_ranges = ranges;

			internal IReadOnlyList<string> Requested => m_requested;

			internal IList<IList<object>> Fetch(string range)
			{
				m_requested.Add(range);
				return m_ranges.TryGetValue(range, out var values) ? values : new List<IList<object>>();
			}
		}

		private const string GoogleSourceId = "1PreviewSpreadsheetId";

		private static RecordingFetcher GoogleFetcher(params (string range, IList<IList<object>> values)[] ranges)
			=> new RecordingFetcher(ranges.ToDictionary(r => r.range, r => r.values, StringComparer.Ordinal));

		// A null column count is the case the export dereferences with .Value.
		private static Spreadsheet GoogleMetadata(params (string name, int? columns)[] sheets)
		{
			return new Spreadsheet
			{
				Sheets = sheets.Select(s => new Sheet
				{
					Properties = new SheetProperties
					{
						Title = s.name,
						GridProperties = new GridProperties { ColumnCount = s.columns },
					},
				}).ToList(),
			};
		}

		private static IList<IList<object>> Rows(params string[][] rows)
			=> rows.Select(row => (IList<object>)row.Cast<object>().ToList()).ToList();

		private static IList<IList<object>> HeroIdRows()
			=> Rows(new[] { "Hero" }, new[] { "HERO_1", "7" });

		// Mirrors the Excel fixture: 'bot_ids' is absent from the first row and symbolic in the second.
		private static IList<IList<object>> HeroRows()
			=> Rows(
				new[] { "id", "name", "bot_ids[]" },
				new[] { "HERO_1", "Alpha" },
				new[] { "HERO_1", "Beta", "HERO_1|HERO_1" });

		private static List<SheetPath> GoogleSheets(bool idsSelected)
			=> Sheets(("HeroIDs", idsSelected), ("Heroes", true));

		private static List<SheetPath> Sheets(params (string name, bool selected)[] entries)
			=> entries.Select(e => new SheetPath { name = e.name, selected = e.selected }).ToList();

		private static List<SheetPath> LegacySheets()
			=> Sheets(("HeroIDs", false), ("Heroes", true));

		private static List<SheetPath> GeneratedSheets()
			=> Sheets(("Bots", true));

		// 'bot_ids' is absent from the first row and symbolic in the second, so one preview covers
		// both the row union and symbolic ID resolution.
		private static IWorkbook LegacyWorkbook()
		{
			var workbook = new XSSFWorkbook();
			var ids = workbook.CreateSheet("HeroIDs");
			ids.CreateRow(0).CreateCell(0).SetCellValue("Hero");
			var idRow = ids.CreateRow(1);
			idRow.CreateCell(0).SetCellValue("HERO_1");
			idRow.CreateCell(1).SetCellValue("7");

			var heroes = workbook.CreateSheet("Heroes");
			var header = heroes.CreateRow(0);
			header.CreateCell(0).SetCellValue("id");
			header.CreateCell(1).SetCellValue("name");
			header.CreateCell(2).SetCellValue("bot_ids[]");

			var first = heroes.CreateRow(1);
			first.CreateCell(0).SetCellValue("HERO_1");
			first.CreateCell(1).SetCellValue("Alpha");

			var second = heroes.CreateRow(2);
			second.CreateCell(0).SetCellValue("HERO_1");
			second.CreateCell(1).SetCellValue("Beta");
			second.CreateCell(2).SetCellValue("HERO_1|HERO_1");
			return workbook;
		}

		private static IWorkbook GeneratedWorkbook()
		{
			var workbook = new XSSFWorkbook();
			var bots = workbook.CreateSheet("Bots");
			var header = bots.CreateRow(0);
			header.CreateCell(0).SetCellValue("id:int");
			header.CreateCell(1).SetCellValue("botIds[]:int");

			var row = bots.CreateRow(1);
			row.CreateCell(0).SetCellValue("1");
			row.CreateCell(1).SetCellValue("2|3");
			return workbook;
		}

		private static string SaveWorkbook(IWorkbook workbook)
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-preview-{Guid.NewGuid():N}.xlsx");
			using (var stream = File.Create(path))
				workbook.Write(stream);
			workbook.Close();
			return path;
		}

		private static void Cleanup(SheetXSettings settings, IWorkbook workbook)
		{
			workbook?.Close();
			ScriptableObject.DestroyImmediate(settings);
			if (Directory.Exists(OutputFolder))
				Directory.Delete(OutputFolder, true);
			if (File.Exists(OutputFolder + ".meta"))
				File.Delete(OutputFolder + ".meta");
		}
	}
}
