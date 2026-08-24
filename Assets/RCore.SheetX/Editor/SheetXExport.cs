/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Identifies what kind of artifact an export produced, so a caller can route each one without
	/// having to parse its path.
	/// </summary>
	public enum SheetXExportFileType
	{
		/// <summary>A C# file of ID constants generated from an 'IDs' sheet.</summary>
		Ids,

		/// <summary>A C# file of constants generated from a 'Constants' sheet.</summary>
		Constants,

		/// <summary>A JSON data file generated from a data sheet, or the combined JSON of a workbook.</summary>
		Json,

		/// <summary>A per-language localization JSON file.</summary>
		Localization,

		/// <summary>A character-set text file collected from localized text.</summary>
		CharacterSet,

		/// <summary>The generated localization manager C# file.</summary>
		LocalizationManager,

		/// <summary>Generated C# constants for one localization sheet's keys.</summary>
		LocalizationConstants,

		/// <summary>The generated localized-text component C# file for one localization sheet.</summary>
		LocalizationComponent,

		/// <summary>The generated config ScriptableObject C# file.</summary>
		ConfigScript,
	}

	/// <summary>
	/// Receives every artifact an export produces. The exporter never touches the disk itself, so the
	/// caller decides where content goes, whether to compare before writing, and whether to import it.
	/// </summary>
	public interface ISheetXOutput
	{
		/// <summary>
		/// Called once per finished artifact. <paramref name="relativePath"/> is forward-slashed and
		/// relative to the output root the request configured for that artifact kind.
		/// </summary>
		void Write(string relativePath, string content);
	}

	/// <summary>
	/// Everything one export needs. Carries its own settings so the exporter never reads
	/// 'Assets/SheetX/SheetXSettings.asset' or the editor windows' state.
	/// </summary>
	public sealed class SheetXExportRequest
	{
		/// <summary>Path of the .xlsx workbook, or the Google spreadsheet ID.</summary>
		public string SpreadsheetPath;

		/// <summary>Sheets to export. Null exports every sheet; an empty list exports none.</summary>
		public List<string> Sheets;

		/// <summary>Output root for generated C# (IDs, constants, localization manager).</summary>
		public string ConstantsOutputPath;

		/// <summary>Output root for JSON data files.</summary>
		public string JsonOutputPath;

		/// <summary>Output root for localization and character-set files.</summary>
		public string LocalizationOutputPath;

		/// <summary>Merge every JSON sheet of one spreadsheet into a single artifact.</summary>
		public bool CombineJson;

		/// <summary>Emit one IDs file per sheet instead of one combined file.</summary>
		public bool SeparateIDs;

		/// <summary>Emit one constants file per sheet instead of one combined file.</summary>
		public bool SeparateConstants;

		/// <summary>Emit one localization file group per sheet instead of one combined group.</summary>
		public bool SeparateLocalizations;

		/// <summary>Emit IDs marked '[enum]' only as enums, dropping their const int form.</summary>
		public bool OnlyEnumAsIDs;

		/// <summary>Namespace wrapped around generated C#. Empty leaves the code un-namespaced.</summary>
		public string Namespace;

		/// <summary>Comma or semicolon separated field names kept on every JSON row even when empty.</summary>
		public string PersistentFields;

		/// <summary>Encrypt JSON artifacts with <see cref="EncryptionKey"/>.</summary>
		public bool EncryptJson;

		/// <summary>Key used when <see cref="EncryptJson"/> is set.</summary>
		public string EncryptionKey;

		/// <summary>Google OAuth client ID. Used by <see cref="SheetXExporter.ExportGoogle"/> only.</summary>
		public string GoogleClientId;

		/// <summary>Google OAuth client secret. Used by <see cref="SheetXExporter.ExportGoogle"/> only.</summary>
		public string GoogleClientSecret;
	}

	/// <summary>
	/// One artifact an export handed to <see cref="ISheetXOutput.Write"/>.
	/// </summary>
	public sealed class SheetXExportFile
	{
		/// <summary>Forward-slashed path passed to the output, relative to that artifact's output root.</summary>
		public string RelativePath;

		/// <summary>What kind of artifact this is.</summary>
		public SheetXExportFileType Type;
	}

	/// <summary>
	/// What an export produced and what went wrong. Nothing is thrown at the caller and no dialog is
	/// shown — every failure the exporter can attribute lands in <see cref="Errors"/>.
	/// </summary>
	public sealed class SheetXExportResult
	{
		/// <summary>Artifacts the output accepted, in the order they were written.</summary>
		public IReadOnlyList<SheetXExportFile> Files { get; }

		/// <summary>Problems that did not stop the export, such as an empty sheet.</summary>
		public IReadOnlyList<string> Warnings { get; }

		/// <summary>Problems that make the output incomplete or wrong.</summary>
		public IReadOnlyList<string> Errors { get; }

		/// <summary>True when nothing went into <see cref="Errors"/>.</summary>
		public bool Success => Errors.Count == 0;

		internal SheetXExportResult(IReadOnlyList<SheetXExportFile> files, IReadOnlyList<string> warnings, IReadOnlyList<string> errors)
		{
			Files = new List<SheetXExportFile>(files).AsReadOnly();
			Warnings = new List<string>(warnings).AsReadOnly();
			Errors = new List<string>(errors).AsReadOnly();
		}
	}

	/// <summary>
	/// Collects artifacts and diagnostics for one export. The handlers hold one of these instead of
	/// writing files, logging, or opening dialogs, so the same code serves the editor windows and an
	/// external caller.
	/// </summary>
	internal sealed class SheetXExportContext
	{
		private struct StagedArtifact
		{
			public string RelativePath;
			public string Content;
			public SheetXExportFileType Type;
			public string OriginSource;
			public string OriginSheet;
		}

		private readonly ISheetXOutput m_output;
		private readonly bool m_discardStagedOnError;
		private readonly List<SheetXExportFile> m_files = new List<SheetXExportFile>();
		private readonly List<string> m_warnings = new List<string>();
		private readonly List<string> m_errors = new List<string>();
		private readonly List<StagedArtifact> m_staged = new List<StagedArtifact>();
		private readonly Dictionary<string, StagedArtifact> m_stagedByPath =
			new Dictionary<string, StagedArtifact>(StringComparer.Ordinal);
		private readonly HashSet<string> m_reserved =
			new HashSet<string>(StringComparer.Ordinal);
		private string m_originSource;
		private string m_originSheet;
		private bool m_flushed;

		internal SheetXExportContext(ISheetXOutput output, bool discardStagedOnError)
		{
			m_output = output;
			m_discardStagedOnError = discardStagedOnError;
		}

		internal void SetOrigin(string source, string sheet)
		{
			m_originSource = source;
			m_originSheet = sheet;
		}

		/// <summary>
		/// Stages one artifact. <see cref="Flush"/> hands staged artifacts to the output and records
		/// each one only after its write returns.
		/// </summary>
		public void Write(
			string pFolder,
			string pFileName,
			string pContent,
			SheetXExportFileType pType)
		{
			string relativePath = Combine(pFolder, pFileName);

			if (!m_reserved.Add(relativePath))
			{
				m_errors.Add(CollisionMessage(relativePath));
				return;
			}

			var artifact = new StagedArtifact
			{
				RelativePath = relativePath,
				Content = pContent,
				Type = pType,
				OriginSource = m_originSource,
				OriginSheet = m_originSheet,
			};

			m_stagedByPath[relativePath] = artifact;
			m_staged.Add(artifact);
		}

		private string CollisionMessage(string relativePath)
		{
			bool haveOrigins =
				!string.IsNullOrEmpty(m_originSource)
				&& m_stagedByPath.TryGetValue(relativePath, out var first)
				&& !string.IsNullOrEmpty(first.OriginSource);

			if (!haveOrigins)
				return $"Artifact '{relativePath}' was produced more than once.";

			var origin = m_stagedByPath[relativePath];
			return $"Artifact '{relativePath}' collision: "
				+ $"first '{origin.OriginSource}' sheet '{origin.OriginSheet}'; "
				+ $"second '{m_originSource}' sheet '{m_originSheet}'.";
		}

		public void Warn(string pMessage) => m_warnings.Add(pMessage);

		public void Error(string pMessage) => m_errors.Add(pMessage);

		internal bool HasErrors => m_errors.Count > 0;

		internal void Flush()
		{
			if (m_flushed)
				return;

			m_flushed = true;

			if (m_discardStagedOnError && m_errors.Count > 0)
			{
				m_staged.Clear();
				return;
			}

			foreach (var artifact in m_staged)
			{
				if (m_output == null)
				{
					m_errors.Add(
						$"Writing '{artifact.RelativePath}' failed: output is null.");
					break;
				}

				try
				{
					m_output.Write(artifact.RelativePath, artifact.Content);
				}
				catch (Exception ex)
				{
					m_errors.Add(
						$"Writing '{artifact.RelativePath}' failed: {ex.Message}");
					break;
				}

				m_files.Add(new SheetXExportFile
				{
					RelativePath = artifact.RelativePath,
					Type = artifact.Type,
				});
			}

			m_staged.Clear();
		}

		public SheetXExportResult ToResult() => new SheetXExportResult(m_files, m_warnings, m_errors);

		private static string Combine(string pFolder, string pFileName)
		{
			string folder = (pFolder ?? "").Replace('\\', '/').TrimEnd('/');
			string fileName = (pFileName ?? "").Replace('\\', '/').TrimStart('/');
			return folder.Length == 0 ? fileName : $"{folder}/{fileName}";
		}
	}

	/// <summary>
	/// Exports spreadsheets without the SheetX windows or settings asset. Every artifact travels
	/// through the supplied <see cref="ISheetXOutput"/>; every failure comes back in the result.
	/// </summary>
	public static class SheetXExporter
	{
		/// <summary>
		/// Exports many spreadsheets in one detached run. Every source shares one
		/// symbolic-ID namespace, and nothing reaches <paramref name="output"/>
		/// unless the whole batch succeeds.
		/// </summary>
		/// <param name="request">Sources and options for the batch.</param>
		/// <param name="output">Caller-owned sink for generated artifacts.</param>
		/// <returns>Artifacts written, plus every warning and error.</returns>
		public static SheetXExportResult ExportBatch(
			SheetXBatchExportRequest request,
			ISheetXOutput output)
			=> new SheetXBatchExporter(request, output).Export();

		/// <summary>
		/// Exports one .xlsx workbook. The workbook is opened once and closed before this returns.
		/// </summary>
		public static SheetXExportResult ExportExcel(SheetXExportRequest request, ISheetXOutput output)
		{
			var context = Validate(request, output, out var settings);
			if (settings == null)
				return context.ToResult();

			if (!System.IO.File.Exists(request.SpreadsheetPath))
			{
				context.Error($"Spreadsheet '{request.SpreadsheetPath}' does not exist.");
				return context.ToResult();
			}

			try
			{
				// The source stream must outlive every NPOI read: WorkbookFactory reads parts lazily, so a
				// stream closed before ExportAll finishes throws mid-export. Memory-backed rather than a
				// FileStream so a spreadsheet edited during a long export cannot change under the reader.
				using var stream = new System.IO.MemoryStream(
					System.IO.File.ReadAllBytes(request.SpreadsheetPath), writable: false);
				var workbook = NPOI.SS.UserModel.WorkbookFactory.Create(stream);
				settings.excelSheetsPath = new ExcelSheetsPath
				{
					path = request.SpreadsheetPath,
					selected = true,
					sheets = CreateExcelSheets(workbook, request.Sheets),
				};
				new ExcelSheetHandler(settings, context).ExportAll(workbook);
			}
			catch (Exception ex)
			{
				context.Error($"Could not read spreadsheet '{request.SpreadsheetPath}': {ex.Message}");
			}
			context.Flush();
			return context.ToResult();
		}

		/// <summary>
		/// Exports one Google spreadsheet. OAuth may need interactive consent, so a batch caller must
		/// have authorized the token cache beforehand.
		/// </summary>
		public static SheetXExportResult ExportGoogle(SheetXExportRequest request, ISheetXOutput output)
		{
			var context = Validate(request, output, out var settings);
			if (settings == null)
				return context.ToResult();

			// Empty means export none. No remote metadata or OAuth credentials are needed for no work.
			if (request.Sheets != null && request.Sheets.Count == 0)
				return context.ToResult();

			if (string.IsNullOrEmpty(request.GoogleClientId) || string.IsNullOrEmpty(request.GoogleClientSecret))
			{
				context.Error("GoogleClientId and GoogleClientSecret are required for a Google export.");
				return context.ToResult();
			}

			settings.googleSheetsPath = new GoogleSheetsPath
			{
				id = request.SpreadsheetPath,
				selected = true,
				sheets = CreateSheets(request.Sheets),
			};
			try
			{
				new GoogleSheetHandler(
					settings,
					context,
					request.GoogleClientId,
					request.GoogleClientSecret,
					request.Sheets == null).ExportAll();
			}
			catch (Exception ex)
			{
				context.Error($"Could not export Google spreadsheet '{request.SpreadsheetPath}': {ex.Message}");
			}
			context.Flush();
			return context.ToResult();
		}

		// Builds the in-memory settings the handlers read. CreateInstance, never SheetXSettings.Init():
		// the exporter must not load, create, or dirty an asset in the consuming project.
		private static SheetXExportContext Validate(SheetXExportRequest request, ISheetXOutput output, out SheetXSettings settings)
		{
			settings = null;
			// A null sink is a caller bug, but this API promises every failure comes back in the
			// result rather than as a throw, so it is reported the same way as any other.
			var context = new SheetXExportContext(output, discardStagedOnError: false);
			if (output == null)
			{
				context.Error("Output is null.");
				return context;
			}
			if (request == null)
			{
				context.Error("Request is null.");
				return context;
			}
			if (string.IsNullOrEmpty(request.SpreadsheetPath))
			{
				context.Error("SpreadsheetPath is empty.");
				return context;
			}

			settings = SheetXSettings.CreateTransient(request);
			settings.silent = true;
			if (settings.encryptJson && settings.UsesDefaultEncryptionKey)
				context.Warn("encryptJson is using SheetX's published default key. Set EncryptionKey before shipping encrypted data.");
			return context;
		}

		private static List<SheetPath> CreateExcelSheets(NPOI.SS.UserModel.IWorkbook workbook, List<string> sheets)
		{
			var requested = sheets == null ? null : new HashSet<string>(sheets, StringComparer.Ordinal);
			var result = new List<SheetPath>(workbook.NumberOfSheets);
			for (int i = 0; i < workbook.NumberOfSheets; i++)
			{
				string name = workbook.GetSheetAt(i).SheetName;
				result.Add(new SheetPath { name = name, selected = requested == null || requested.Contains(name) });
			}
			return result;
		}

		internal static List<SheetPath> CreateSheets(List<string> sheets)
		{
			if (sheets == null)
				return new List<SheetPath>();

			var seen = new HashSet<string>(StringComparer.Ordinal);
			var result = new List<SheetPath>(sheets.Count);
			foreach (string sheet in sheets)
			{
				if (!string.IsNullOrEmpty(sheet) && seen.Add(sheet))
					result.Add(new SheetPath { name = sheet, selected = true });
			}
			return result;
		}
	}
}
