/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using UnityEditor;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Which export the preview is predicting, decided by the host that opened it rather than by where the
	/// path happens to appear in the settings. The same workbook can sit in both the single-file slot and the
	/// Export Multi Files list, and the two tabs run different exports over it, so membership alone cannot
	/// say which one a preview should mirror.
	/// </summary>
	internal enum SheetXPreviewScope
	{
		/// <summary>
		/// Predicts the single-source export: IDs resolve against this source alone. The conservative
		/// default, so a host that says nothing never silently widens a namespace.
		/// </summary>
		Local = 0,

		/// <summary>
		/// Predicts <c>ExportAllFiles</c>: IDs resolve across the Export Multi Files list, subject to that
		/// export's own membership and selection rules.
		/// </summary>
		Multi = 1,
	}

	/// <summary>One spreadsheet a preview can read, independent of how the windows store it.</summary>
	internal sealed class SheetXSheetSource
	{
		/// <summary>Whether <see cref="Id"/> names a workbook file or a Google spreadsheet.</summary>
		internal SheetXSourceKind Kind;

		/// <summary>Excel workbook path, or Google spreadsheet id.</summary>
		internal string Id;

		/// <summary>Every sheet of the source, selected or not: an unchecked IDs sheet still resolves references.</summary>
		internal IReadOnlyList<SheetPath> Sheets;

		/// <summary>
		/// Which export this preview predicts. Set by the window that opened it — the Export Multi Files
		/// tabs and the per-source Edit windows they launch set <see cref="SheetXPreviewScope.Multi"/>;
		/// every single-source host leaves the default <see cref="SheetXPreviewScope.Local"/>.
		/// </summary>
		internal SheetXPreviewScope Scope;
	}

	/// <summary>What one sheet would export, acquired without exporting anything.</summary>
	internal sealed class SheetXSheetPreviewData
	{
		/// <summary>Output mode the settings bind to this sheet.</summary>
		internal SheetXSheetOutputMode Mode;

		/// <summary>Converted row-array Json. Null in Generated Data Class mode, which has no legacy Json.</summary>
		internal string Json;

		/// <summary>Parsed Generated Data Class schema. Null in the legacy modes.</summary>
		internal SheetXCollectionSchema Schema;

		/// <summary>Row type source this sheet would produce.</summary>
		internal string ClassCode;

		/// <summary>Top-level Json keys in first-seen order. Empty in Generated Data Class mode.</summary>
		internal IReadOnlyList<string> RootMemberNames;

		/// <summary>
		/// Top-level inferred fields, carried straight off the inference that produced <see cref="ClassCode"/>
		/// so the window's table never re-parses the Json. Empty in Generated Data Class mode, whose fields
		/// are schema-backed and shown in the code itself.
		/// </summary>
		internal IReadOnlyList<SheetXSheetField> Fields;

		/// <summary>Problems that did not reject the snapshot, including converter warnings.</summary>
		internal IReadOnlyList<string> Diagnostics;

		/// <summary>
		/// Row-type compatibility findings, kept apart from <see cref="Diagnostics"/> because they belong to
		/// whichever Data Class was bound when this snapshot was taken. A window that lets the user change
		/// that binding re-derives them live and ignores this copy; a direct caller of the facade, which has
		/// no live binding, reads them here.
		/// </summary>
		internal IReadOnlyList<string> RowTypeDiagnostics;

		/// <summary>
		/// True when symbolic IDs were resolved against every source in the Export Multi Files list, the way
		/// <c>ExportAllFiles</c> resolves them, rather than against this one spreadsheet. Both branches can
		/// set it; each follows its own export's selection rule.
		/// </summary>
		internal bool MultiFileIds;

		/// <summary>When this snapshot was taken.</summary>
		internal DateTime FetchedAtUtc;
	}

	/// <summary>
	/// Acquires one sheet's structure the way an export would read it, and nothing else: no file is
	/// written, no binding created, no setting assigned, nothing baked. An error the converter reports
	/// rejects the snapshot even when the assembled Json parses, because a partial preview presented as
	/// this sheet's structure would be wrong in exactly the way a preview exists to prevent.
	/// </summary>
	internal static class SheetXSheetJsonSource
	{
		// The staged artifacts are never flushed, so this exists only to give the context a sink it can
		// hold. Nothing ever reaches it.
		private sealed class DiscardOutput : ISheetXOutput
		{
			public void Write(string relativePath, string content)
			{
			}
		}

		/// <summary>
		/// Opens one Google spreadsheet for reading: its metadata, a fetcher for one A1 range, and the
		/// service to dispose afterwards. A seam so tests exercise the preview without a network call —
		/// swap it, and restore it in a finally.
		/// </summary>
		internal static Func<SheetXSettings, string,
				(Spreadsheet metadata, Func<string, IList<IList<object>>> fetchRange, IDisposable service)>
			GoogleConnector = OpenGoogleSpreadsheet;

		/// <summary>
		/// Whether Google credentials are present, checked before the connector is ever invoked. A seam for
		/// the same reason <see cref="GoogleConnector"/> is one: the real credentials live in EditorPrefs, so
		/// a test that set them to exercise this precondition would overwrite the developer's own — and an
		/// interrupted run would never put them back. Swap it, and restore it in a finally. Never logged.
		/// </summary>
		internal static Func<SheetXSettings, bool> HasGoogleCredentials = DefaultHasGoogleCredentials;

		private static bool DefaultHasGoogleCredentials(SheetXSettings settings)
			=> settings != null
				&& !string.IsNullOrEmpty(settings.ObfGoogleClientId)
				&& !string.IsNullOrEmpty(settings.ObfGoogleClientSecret);

		// One preview across an N-spreadsheet list costs one Spreadsheets.Get plus one Values.Get per '*IDs'
		// sheet, per listed spreadsheet — real network round trips the user waits through on every click. So
		// the built map is kept for the session and reused.
		//
		// ONLY a complete build is cached. A walk that could not read something it was due to read produces
		// a map missing keys, and caching that made the failure outlive it: the next preview — including one
		// OF the spreadsheet that failed, with its connection now open and healthy — was served a map with
		// none of that spreadsheet's own IDs, which is strictly worse than reading it alone would have been.
		// An incomplete result is still shown, with its diagnostics, so the user sees what happened; it is
		// simply not remembered. Topping a cached map up afterwards was rejected: the walk is first-wins, so
		// adding a spreadsheet's keys out of list order silently hands a duplicated key to the wrong
		// definition. A rebuild in list order is the only way to reclaim correct precedence.
		//
		// Everything that could make it wrong invalidates it:
		//  - Refresh, the user's explicit 're-read the source' action, calls InvalidateGoogleIdCache. It is
		//    the escape hatch that makes serving a possibly-stale map acceptable, so it must genuinely
		//    rebuild.
		//  - A domain reload: these are plain statics, so Unity clears them. Nothing is serialized, and
		//    nothing is written to disk.
		//  - A different settings asset: matched by reference, never by name or instance id, so two assets
		//    can never share one map and a destroyed-then-recreated asset does not inherit another's.
		//  - Any change to the membership or selection of 'googleSheetsPaths' that would change what the map
		//    is built from: GoogleIdCacheSignature folds in each entry's id and 'selected', and each of its
		//    sheets' name and 'selected', in list order. A signature mismatch rebuilds.
		private static SheetXSettings s_googleIdCacheSettings;
		private static string s_googleIdCacheSignature;
		private static Dictionary<string, int> s_googleIdCacheMap;
		private static IReadOnlyList<string> s_googleIdCacheNotes;

		/// <summary>
		/// Drops the cached cross-spreadsheet ID map, so the next Google preview rebuilds it from the
		/// network. Called by the preview window's Refresh, which exists precisely to re-read the source.
		/// </summary>
		internal static void InvalidateGoogleIdCache()
		{
			s_googleIdCacheSettings = null;
			s_googleIdCacheSignature = null;
			s_googleIdCacheMap = null;
			s_googleIdCacheNotes = null;
		}

		/// <summary>
		/// Reads one sheet's preview from a workbook the caller already opened.
		/// </summary>
		/// <param name="settings">Settings read for mode, persistent fields, and namespace. Never written.</param>
		/// <param name="workbook">Open workbook. Not closed here — the caller owns it.</param>
		/// <param name="sheets">Every sheet of the source; each '*IDs' sheet loads regardless of selection.</param>
		/// <param name="sourceId">Workbook path used to look up this sheet's binding.</param>
		/// <param name="sheetName">Sheet to preview.</param>
		/// <param name="data">The snapshot, when this returns true.</param>
		/// <param name="error">Why no snapshot could be taken, when this returns false.</param>
		/// <returns>True when a complete, trustworthy snapshot was produced.</returns>
		internal static bool TryLoadExcel(
			SheetXSettings settings,
			IWorkbook workbook,
			IReadOnlyList<SheetPath> sheets,
			string sourceId,
			string sheetName,
			out SheetXSheetPreviewData data,
			out string error,
			SheetXPreviewScope scope = SheetXPreviewScope.Local)
		{
			data = null;
			if (settings == null)
			{
				error = "No SheetX settings were supplied.";
				return false;
			}

			var mode = ModeOf(settings, sourceId, sheetName);
			var context = new SheetXExportContext(new DiscardOutput(), discardStagedOnError: true);
			var handler = new ExcelSheetHandler(settings, context);
			if (!handler.TryPreviewSheet(
				workbook, sheets, sourceId, sheetName, mode, scope == SheetXPreviewScope.Multi,
				out string json, out var schema, out var warnings, out bool multiFileIds, out error))
			{
				return false;
			}

			return TryComplete(
				settings, context, sourceId, sheetName, mode, json, schema, warnings, multiFileIds,
				out data, out error);
		}

		/// <summary>
		/// Reads one sheet's preview from a Google spreadsheet the caller already opened.
		/// </summary>
		/// <param name="settings">Settings read for mode, persistent fields, and namespace. Never written.</param>
		/// <param name="metadata">Spreadsheet metadata. Only read — no sheet list or selection is synced from it.</param>
		/// <param name="fetchRange">Reads one A1 range. The caller owns the connection it uses.</param>
		/// <param name="sourceId">Spreadsheet id, used to look up this sheet's binding and to name errors.</param>
		/// <param name="sheets">Every sheet of the source; only a selected '*IDs' sheet loads, as export does.</param>
		/// <param name="sheetName">Sheet to preview.</param>
		/// <param name="data">The snapshot, when this returns true.</param>
		/// <param name="error">Why no snapshot could be taken, when this returns false.</param>
		/// <returns>True when a complete, trustworthy snapshot was produced.</returns>
		/// <remarks>
		/// When <paramref name="sourceId"/> is listed in 'googleSheetsPaths', IDs resolve against every
		/// selected listed spreadsheet's selected '*IDs' sheets, as 'ExportAllFiles' does. That walk costs
		/// one connection plus one range fetch per '*IDs' sheet per listed spreadsheet, so its result is
		/// cached for the session and rebuilt on Refresh — see <see cref="InvalidateGoogleIdCache"/>.
		/// </remarks>
		internal static bool TryLoadGoogle(
			SheetXSettings settings,
			Spreadsheet metadata,
			Func<string, IList<IList<object>>> fetchRange,
			string sourceId,
			IReadOnlyList<SheetPath> sheets,
			string sheetName,
			out SheetXSheetPreviewData data,
			out string error,
			SheetXPreviewScope scope = SheetXPreviewScope.Local)
		{
			data = null;
			if (settings == null)
			{
				error = "No SheetX settings were supplied.";
				return false;
			}

			// A spreadsheet previewed from an Export Multi Files host is exported by ExportAllFiles, which
			// loads every selected listed spreadsheet's IDs before converting anything. Resolving against
			// this one spreadsheet would make the preview disagree with the export it exists to predict.
			var multiSet = MultiSpreadsheetPreviewSet(settings, sourceId, scope);
			IReadOnlyDictionary<string, int> seedIds = null;
			var idNotes = Array.Empty<string>() as IReadOnlyList<string>;
			if (multiSet != null)
			{
				seedIds = GoogleMultiSpreadsheetIds(
					settings, multiSet, sourceId, metadata, fetchRange, out idNotes);
			}

			var mode = ModeOf(settings, sourceId, sheetName);
			var context = new SheetXExportContext(new DiscardOutput(), discardStagedOnError: true);
			var handler = new GoogleSheetHandler(settings, context, null, null, false);
			if (!handler.TryPreviewSheet(
				metadata, fetchRange, sourceId, sheets, sheetName, mode, seedIds,
				out string json, out var schema, out var warnings, out error))
			{
				return false;
			}

			return TryComplete(
				settings, context, sourceId, sheetName, mode, json, schema,
				MergeNotes(idNotes, warnings), multiFileIds: multiSet != null,
				out data, out error);
		}

		// The notes from the ID walk read first: they explain a type the fields table is about to show.
		private static IReadOnlyList<string> MergeNotes(
			IReadOnlyList<string> notes, IReadOnlyList<string> warnings)
		{
			if (notes == null || notes.Count == 0)
				return warnings ?? Array.Empty<string>();
			if (warnings == null || warnings.Count == 0)
				return notes;
			var merged = new List<string>(notes);
			merged.AddRange(warnings);
			return merged;
		}

		/// <summary>
		/// The Export Multi Files entries whose IDs an export of <paramref name="sourceId"/> would load, or
		/// null when the preview stays single-spreadsheet.
		/// <para>
		/// The host decides first. A spreadsheet can sit in the single-source slot AND the list, and the two
		/// tabs run different exports over it, so membership cannot say which export a preview predicts —
		/// only the window that opened it can. A Single-tab preview therefore stays local even for a listed
		/// id, and <see cref="SheetXPreviewScope.Local"/> is the default, so a host that says nothing never
		/// silently widens.
		/// </para>
		/// <para>
		/// Within a Multi host, membership must also be selected, because <c>ExportAllFiles</c> skips an
		/// unselected spreadsheet outright — both as a source of IDs and as something to export. Widening on
		/// bare membership would hand an unselected entry a map built without its own '*IDs' sheets, so
		/// previewing it would resolve strictly fewer IDs than reading it alone does, for a sheet that
		/// export never reaches. The gate belongs here rather than inside the walk: the cached map is keyed
		/// on the list, not on which spreadsheet is being previewed, so it must come out the same whichever
		/// one asked for it.
		/// </para>
		/// </summary>
		private static List<GoogleSheetsPath> MultiSpreadsheetPreviewSet(
			SheetXSettings settings, string sourceId, SheetXPreviewScope scope)
		{
			if (scope != SheetXPreviewScope.Multi)
				return null;
			var paths = settings?.googleSheetsPaths;
			if (paths == null || string.IsNullOrEmpty(sourceId))
				return null;
			// Ordinal on the id, the field the export's own Spreadsheets.Get call uses.
			bool member = paths.Any(path =>
				path != null && path.selected && string.Equals(path.id, sourceId, StringComparison.Ordinal));
			return member ? paths : null;
		}

		// Walks the list the way ExportAllFiles does — only a selected entry, only its selected '*IDs'
		// sheets, in list order — and caches the result for the session. A spreadsheet that will not open is
		// skipped with a note rather than failing a preview of a different spreadsheet: an expired token or a
		// revoked share on one entry must not take the whole window down.
		private static IReadOnlyDictionary<string, int> GoogleMultiSpreadsheetIds(
			SheetXSettings settings,
			List<GoogleSheetsPath> paths,
			string sourceId,
			Spreadsheet openMetadata,
			Func<string, IList<IList<object>>> openFetchRange,
			out IReadOnlyList<string> notes)
		{
			string signature = GoogleIdCacheSignature(paths);
			if (ReferenceEquals(s_googleIdCacheSettings, settings)
				&& string.Equals(s_googleIdCacheSignature, signature, StringComparison.Ordinal)
				&& s_googleIdCacheMap != null)
			{
				notes = s_googleIdCacheNotes ?? Array.Empty<string>();
				return s_googleIdCacheMap;
			}

			var ids = new Dictionary<string, int>();
			var collected = new List<string>();
			bool complete = true;
			foreach (var path in paths)
			{
				if (path == null || !path.selected || string.IsNullOrEmpty(path.id))
					continue;

				// The caller already holds this one open and owns disposing it; reconnecting would pay a
				// second round trip for bytes already in hand.
				bool isOpenSource = string.Equals(path.id, sourceId, StringComparison.Ordinal);
				if (isOpenSource)
				{
					complete &= GoogleSheetHandler.LoadPreviewIdsFromSpreadsheet(
						openMetadata, openFetchRange, path.id, path.sheets, ids, collected);
					continue;
				}

				// Every selected entry is worth a connection, even one whose saved list names no '*IDs'
				// sheet: the spreadsheet may have gained one on the web since, and the export would find it.
				IDisposable service = null;
				try
				{
					var (metadata, fetchRange, opened) = GoogleConnector(settings, path.id);
					service = opened;
					complete &= GoogleSheetHandler.LoadPreviewIdsFromSpreadsheet(
						metadata, fetchRange, path.id, path.sheets, ids, collected);
				}
				catch (Exception ex)
				{
					// Network error, revoked access, deleted spreadsheet, or an auth prompt that threw: the
					// preview of a different spreadsheet still has everything it needs from the rest. The
					// map is incomplete, though, so it is shown and then forgotten rather than cached.
					collected.Add($"IDs from Google spreadsheet '{path.id}' were skipped: {ex.Message}");
					complete = false;
				}
				finally
				{
					service?.Dispose();
				}
			}

			notes = collected;
			if (!complete)
			{
				// Deliberately leaves any previously cached COMPLETE map in place: this failed build is
				// simply not remembered, and the next preview walks the list again from the top.
				return ids;
			}

			s_googleIdCacheSettings = settings;
			s_googleIdCacheSignature = signature;
			s_googleIdCacheMap = ids;
			s_googleIdCacheNotes = collected;
			return ids;
		}

		// Everything the walk above reads to decide what to fetch, in the order it reads it. A change to any
		// of it would build a different map, so it must rebuild rather than serve the cache.
		private static string GoogleIdCacheSignature(List<GoogleSheetsPath> paths)
		{
			var builder = new StringBuilder();
			foreach (var path in paths)
			{
				if (path == null)
				{
					builder.Append("\u0000null\u0001");
					continue;
				}
				builder.Append(path.id).Append('\u0000').Append(path.selected ? '1' : '0').Append('\u0000');
				foreach (var sheet in path.sheets ?? new List<SheetPath>())
				{
					if (sheet == null)
						continue;
					builder.Append(sheet.name).Append('\u0002').Append(sheet.selected ? '1' : '0')
						.Append('\u0003');
				}
				builder.Append('\u0001');
			}
			return builder.ToString();
		}

		// Everything after the handler hands back Json or a schema is identical for both sources, so both
		// branches run this one body rather than two that could drift apart.
		private static bool TryComplete(
			SheetXSettings settings,
			SheetXExportContext context,
			string sourceId,
			string sheetName,
			SheetXSheetOutputMode mode,
			string json,
			SheetXCollectionSchema schema,
			IReadOnlyList<string> warnings,
			bool multiFileIds,
			out SheetXSheetPreviewData data,
			out string error)
		{
			data = null;

			// An error reported through the context rejects the snapshot even though the Json parsed:
			// the converter already knows this sheet is wrong, and export would refuse it too.
			var result = context.ToResult();
			if (result.Errors.Count > 0)
			{
				error = string.Join("\n", result.Errors);
				return false;
			}

			var diagnostics = new List<string>(result.Warnings);
			diagnostics.AddRange(warnings ?? Array.Empty<string>());
			var rowTypeDiagnostics = new List<string>();

			// Both branches emit into this namespace, but only the legacy one used to validate it: the
			// generator appends it unchecked, so an invalid namespace produced uncompilable code labelled as
			// the real generated class, for settings the export itself refuses. Validated once, above the
			// branch, with the same semantics export validation uses.
			string collectionNamespace = settings.ResolveCollectionNamespace();
			if (!IsValidCollectionNamespace(collectionNamespace))
			{
				error = $"'{collectionNamespace}' is not a valid namespace. "
					+ "Fix: correct the Collection namespace in SheetX Settings.";
				return false;
			}

			string classCode;
			IReadOnlyList<string> rootMemberNames = Array.Empty<string>();
			IReadOnlyList<SheetXSheetField> fields = Array.Empty<SheetXSheetField>();
			if (mode == SheetXSheetOutputMode.GeneratedDataClass)
			{
				classCode = SheetXCollectionGenerator.EmitRowTypes(settings, schema);
				json = null;
			}
			else
			{
				if (!SheetXSheetStructure.TryFromJson(
					json, SheetXCollectionNaming.RowTypeName(sheetName), out var structure, out error))
				{
					return false;
				}
				try
				{
					classCode = structure.ToClassCode(settings.ResolveCollectionNamespace());
				}
				catch (ArgumentException ex)
				{
					error = ex.Message;
					return false;
				}
				rootMemberNames = structure.RootMemberNames;
				fields = structure.Fields;
				diagnostics.AddRange(structure.Diagnostics);
				if (mode == SheetXSheetOutputMode.ExistingDataClass)
					AddRowTypeDiagnostics(settings, sourceId, sheetName, json, rowTypeDiagnostics);
			}

			data = new SheetXSheetPreviewData
			{
				Mode = mode,
				Json = json,
				Schema = schema,
				ClassCode = classCode,
				RootMemberNames = rootMemberNames,
				Fields = fields,
				Diagnostics = diagnostics,
				RowTypeDiagnostics = rowTypeDiagnostics,
				MultiFileIds = multiFileIds,
				FetchedAtUtc = DateTime.UtcNow,
			};
			error = null;
			return true;
		}

		/// <summary>
		/// Reads one sheet's preview, opening the spreadsheet <paramref name="source"/> names.
		/// </summary>
		/// <param name="settings">Settings read for mode, persistent fields, and namespace. Never written.</param>
		/// <param name="source">Which spreadsheet to open and which sheets it holds.</param>
		/// <param name="sheetName">Sheet to preview.</param>
		/// <param name="data">The snapshot, when this returns true.</param>
		/// <param name="error">Why no snapshot could be taken, when this returns false.</param>
		/// <returns>True when a complete, trustworthy snapshot was produced.</returns>
		internal static bool TryLoad(
			SheetXSettings settings,
			SheetXSheetSource source,
			string sheetName,
			out SheetXSheetPreviewData data,
			out string error)
		{
			data = null;
			if (source == null)
			{
				error = "No spreadsheet source was supplied.";
				return false;
			}

			switch (source.Kind)
			{
				case SheetXSourceKind.Excel:
					return TryLoadExcelFile(settings, source, sheetName, out data, out error);

				case SheetXSourceKind.Google:
					return TryLoadGoogleSpreadsheet(settings, source, sheetName, out data, out error);

				default:
					error = $"Preview does not support {source.Kind} sources.";
					return false;
			}
		}

		private static bool TryLoadExcelFile(
			SheetXSettings settings,
			SheetXSheetSource source,
			string sheetName,
			out SheetXSheetPreviewData data,
			out string error)
		{
			data = null;
			if (string.IsNullOrEmpty(source.Id))
			{
				error = "No Excel file is selected.";
				return false;
			}
			if (!File.Exists(source.Id))
			{
				error = $"{source.Id} does not exist.";
				return false;
			}

			IWorkbook workbook;
			try
			{
				// A throwaway descriptor, not settings.excelSheetsPath: a preview must not decide which
				// workbook the next export reads.
				workbook = new ExcelSheetsPath { path = source.Id }.GetWorkBook();
			}
			catch (Exception ex)
			{
				error = $"Could not read '{source.Id}': {ex.Message}";
				return false;
			}
			if (workbook == null)
			{
				error = $"Could not read '{source.Id}'.";
				return false;
			}

			try
			{
				return TryLoadExcel(
					settings, workbook, source.Sheets, source.Id, sheetName, out data, out error, source.Scope);
			}
			finally
			{
				workbook.Close();
			}
		}

		private static bool TryLoadGoogleSpreadsheet(
			SheetXSettings settings,
			SheetXSheetSource source,
			string sheetName,
			out SheetXSheetPreviewData data,
			out string error)
		{
			data = null;
			if (string.IsNullOrEmpty(source.Id))
			{
				error = "No Google spreadsheet is selected.";
				return false;
			}
			if (settings == null)
			{
				error = "No SheetX settings were supplied.";
				return false;
			}
			if (!HasGoogleCredentials(settings))
			{
				error = "Google Client ID or Client Secret is missing.";
				return false;
			}

			Spreadsheet metadata;
			Func<string, IList<IList<object>>> fetchRange;
			IDisposable service = null;
			try
			{
				(metadata, fetchRange, service) = GoogleConnector(settings, source.Id);
			}
			catch (Exception ex)
			{
				service?.Dispose();
				error = $"Could not read Google spreadsheet '{source.Id}': {ex.Message}";
				return false;
			}

			try
			{
				if (metadata == null || fetchRange == null)
				{
					error = $"Could not read Google spreadsheet '{source.Id}'.";
					return false;
				}
				return TryLoadGoogle(
					settings, metadata, fetchRange, source.Id, source.Sheets, sheetName, out data, out error,
					source.Scope);
			}
			finally
			{
				service?.Dispose();
			}
		}

		// The one place a preview reaches the network. GetCacheMetadata is deliberately not used: outside a
		// detached handler it runs ValidateSheetPaths, which adds, removes, and re-selects entries in the
		// settings' sheet list — an export side effect from merely looking at a sheet.
		private static (Spreadsheet, Func<string, IList<IList<object>>>, IDisposable) OpenGoogleSpreadsheet(
			SheetXSettings settings, string spreadsheetId)
		{
			var service = new SheetsService(new BaseClientService.Initializer
			{
				HttpClientInitializer = SheetXHelper.AuthenticateGoogleUser(
					settings.ObfGoogleClientId, settings.ObfGoogleClientSecret),
				ApplicationName = SheetXConstants.APPLICATION_NAME,
			});
			try
			{
				var metadata = service.Spreadsheets.Get(spreadsheetId).Execute();
				return (
					metadata,
					range => service.Spreadsheets.Values.Get(spreadsheetId, range).Execute().Values,
					service);
			}
			catch
			{
				service.Dispose();
				throw;
			}
		}

		// Read-only on purpose: GetOrCreateBinding would add a binding to the settings asset, so merely
		// looking at a sheet would change the project.
		private static SheetXSheetOutputMode ModeOf(SheetXSettings settings, string sourceId, string sheetName)
		{
			var binding = FindBinding(settings, sourceId, sheetName);
			return binding?.outputMode ?? SheetXSheetOutputMode.JsonOnly;
		}

		private static SheetXSheetBinding FindBinding(SheetXSettings settings, string sourceId, string sheetName)
		{
			return settings?.sheetBindings?.FirstOrDefault(binding =>
				binding != null
				&& string.Equals(binding.sourceId, sourceId, StringComparison.Ordinal)
				&& string.Equals(binding.sheetName, sheetName, StringComparison.Ordinal));
		}

		// Existing Data Class output is only as good as the mapping onto its row type, and the export
		// checks exactly this before it writes. Running it here keeps the preview from showing a clean
		// structure for a sheet the next export would skip.
		private static void AddRowTypeDiagnostics(
			SheetXSettings settings, string sourceId, string sheetName, string json, List<string> diagnostics)
		{
			string rowTypeName = FindBinding(settings, sourceId, sheetName)?.rowTypeName;
			if (string.IsNullOrWhiteSpace(rowTypeName))
			{
				diagnostics.Add("Existing Data Class needs a row type name. "
					+ "Fix: pick a type in the Data Class column of the Edit Spreadsheets window.");
				return;
			}

			var rowType = ResolveRowType(rowTypeName.Trim());
			if (rowType == null)
			{
				diagnostics.Add($"row type '{rowTypeName.Trim()}' was not found in any loaded assembly. "
					+ "Fix: re-pick the type in the Data Class column of the Edit Spreadsheets window; "
					+ "it may have been renamed, moved, or deleted.");
				return;
			}
			if (!SheetXRowType.Validate(rowType, out string validationError))
			{
				diagnostics.Add(validationError);
				return;
			}

			try
			{
				JsonConvert.DeserializeObject(string.IsNullOrEmpty(json) ? "[]" : json, rowType.MakeArrayType());
			}
			catch (Exception ex)
			{
				diagnostics.Add($"JSON does not map onto '{rowType.FullName}': {ex.Message} "
					+ "Fix: correct sheet values or select the matching row type.");
			}
		}

		// Segment rules match SheetXCollectionSettings.ValidateNamespace, which the export runs before it
		// writes, so both branches now refuse exactly what export refuses. Empty is the one deliberate
		// difference: it means 'emit no namespace', which both emitters already handle, whereas export's
		// validator reports it as a settings issue. Tested with IsNullOrEmpty, never IsNullOrWhiteSpace — a
		// whitespace-only namespace is NOT empty to the emitters, and must reach the segment check rather
		// than be waved through as blank.
		// Kept here rather than calling the global collection validation, which would also demand folders
		// and bindings this preview has no opinion about.
		private static bool IsValidCollectionNamespace(string ns)
		{
			if (string.IsNullOrEmpty(ns))
				return true;
			var segments = ns.Split('.');
			for (int i = 0; i < segments.Length; i++)
			{
				if (!SheetXCollectionNaming.IsValidIdentifier(segments[i]))
					return false;
			}
			return true;
		}

		// binding.rowTypeName stores an AssemblyQualifiedName, so Type.GetType resolves it directly;
		// the TypeCache pass covers a name whose assembly identity has since moved, the way the export's
		// own assembly scan does.
		private static Type ResolveRowType(string rowTypeName)
		{
			return Type.GetType(rowTypeName, throwOnError: false)
				?? TypeCache.GetTypesWithAttribute<SheetXBindableAttribute>().FirstOrDefault(type =>
					string.Equals(type.AssemblyQualifiedName, rowTypeName, StringComparison.Ordinal)
					|| string.Equals(type.FullName, rowTypeName, StringComparison.Ordinal)
					|| string.Equals(type.Name, rowTypeName, StringComparison.Ordinal));
		}
	}
}
