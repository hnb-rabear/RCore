using System;
using System.Collections.Generic;
using System.Text;

namespace RCore.SheetX.Editor
{
	/// <summary>Which kind of spreadsheet a batch source points at.</summary>
	public enum SheetXSourceKind
	{
		/// <summary>A local Excel workbook read from disk.</summary>
		Excel,

		/// <summary>A Google spreadsheet fetched through the Sheets API.</summary>
		Google,
	}

	/// <summary>One spreadsheet inside a batch.</summary>
	public sealed class SheetXBatchSource
	{
		/// <summary>Whether this source is an Excel workbook or a Google spreadsheet.</summary>
		public SheetXSourceKind Kind;

		/// <summary>Excel file path, or Google spreadsheet id.</summary>
		public string SpreadsheetPath;

		/// <summary>
		/// Sheets to export. Null selects every sheet in source order; an empty list
		/// selects none; a non-empty list selects that subset, still in source order.
		/// </summary>
		public List<string> Sheets;

		/// <summary>
		/// Name used for this source's combined JSON artifact. Defaults to the Excel
		/// file name with its extension intact, or the Google spreadsheet title.
		/// </summary>
		public string OutputName;
	}

	/// <summary>Everything one batch export needs. Shares every option across all sources.</summary>
	public sealed class SheetXBatchExportRequest
	{
		/// <summary>Spreadsheets to export. Membership means enabled.</summary>
		public List<SheetXBatchSource> Sources;

		/// <summary>Output folder, relative to the sink root, for generated constants.</summary>
		public string ConstantsOutputPath;

		/// <summary>Output folder, relative to the sink root, for generated JSON.</summary>
		public string JsonOutputPath;

		/// <summary>Output folder, relative to the sink root, for generated localization.</summary>
		public string LocalizationOutputPath;

		/// <summary>Emit one combined JSON file per source instead of one file per sheet.</summary>
		public bool CombineJson;

		/// <summary>Emit one IDs file per sheet instead of one aggregate file.</summary>
		public bool SeparateIDs;

		/// <summary>Emit one constants file per sheet instead of one aggregate file.</summary>
		public bool SeparateConstants;

		/// <summary>Emit one localization group per sheet instead of one aggregate group.</summary>
		public bool SeparateLocalizations;

		/// <summary>Emit IDs as enum members only, without integer constants.</summary>
		public bool OnlyEnumAsIDs;

		/// <summary>Namespace of every generated C# file.</summary>
		public string Namespace;

		/// <summary>Comma-separated JSON fields kept even when their value is default.</summary>
		public string PersistentFields;

		/// <summary>Obfuscate generated JSON with <see cref="EncryptionKey"/>.</summary>
		public bool EncryptJson;

		/// <summary>Key used when <see cref="EncryptJson"/> is set. Empty keeps the built-in key.</summary>
		public string EncryptionKey;

		/// <summary>OAuth client id for Google sources. Never persisted.</summary>
		public string GoogleClientId;

		/// <summary>OAuth client secret for Google sources. Never persisted.</summary>
		public string GoogleClientSecret;
	}

	/// <summary>Where a symbolic ID was first defined.</summary>
	internal readonly struct SheetXIdOrigin
	{
		/// <summary>Source that first defined ID.</summary>
		public readonly string Source;

		/// <summary>Sheet that first defined ID.</summary>
		public readonly string Sheet;

		/// <summary>Integer value first assigned to ID.</summary>
		public readonly int Value;

		/// <summary>Initializes first-definition origin.</summary>
		public SheetXIdOrigin(string source, string sheet, int value)
		{
			Source = source;
			Sheet = sheet;
			Value = value;
		}
	}

	/// <summary>Builder key that keeps two sources' same-named sheets apart.</summary>
	internal readonly struct SheetXBatchSheetKey : IEquatable<SheetXBatchSheetKey>
	{
		/// <summary>Zero-based source position in batch.</summary>
		public readonly int SourceIndex;

		/// <summary>Sheet name within source.</summary>
		public readonly string Sheet;

		/// <summary>Initializes source-qualified sheet key.</summary>
		public SheetXBatchSheetKey(int sourceIndex, string sheet)
		{
			SourceIndex = sourceIndex;
			Sheet = sheet;
		}

		/// <summary>Checks source index and ordinal sheet-name equality.</summary>
		public bool Equals(SheetXBatchSheetKey other)
			=> SourceIndex == other.SourceIndex
				&& string.Equals(Sheet, other.Sheet, StringComparison.Ordinal);

		/// <summary>Checks value equality against another object.</summary>
		public override bool Equals(object obj)
			=> obj is SheetXBatchSheetKey other && Equals(other);

		/// <summary>Returns hash code using source index and ordinal sheet name.</summary>
		public override int GetHashCode()
		{
			int hash = SourceIndex * 397;
			return hash ^ (Sheet == null ? 0 : StringComparer.Ordinal.GetHashCode(Sheet));
		}
	}

	/// <summary>State shared by every handler inside one batch.</summary>
	internal sealed class SheetXBatchState
	{
		/// <summary>Global symbolic-ID lookup table.</summary>
		public readonly Dictionary<string, int> AllIds =
			new Dictionary<string, int>(StringComparer.Ordinal);

		/// <summary>First-definition origins for global symbolic IDs.</summary>
		public readonly Dictionary<string, SheetXIdOrigin> IdOrigins =
			new Dictionary<string, SheetXIdOrigin>(StringComparer.Ordinal);

		/// <summary>Symbolic IDs already emitted into generated declarations.</summary>
		public readonly HashSet<string> DeclaredIds =
			new HashSet<string>(StringComparer.Ordinal);

		/// <summary>ID builders keyed by source-qualified sheet.</summary>
		public readonly Dictionary<SheetXBatchSheetKey, StringBuilder> IdsBuilders =
			new Dictionary<SheetXBatchSheetKey, StringBuilder>();

		/// <summary>Constants builders keyed by source-qualified sheet.</summary>
		public readonly Dictionary<SheetXBatchSheetKey, StringBuilder> ConstantsBuilders =
			new Dictionary<SheetXBatchSheetKey, StringBuilder>();

		/// <summary>Localization builders keyed by source-qualified sheet.</summary>
		public readonly Dictionary<SheetXBatchSheetKey, LocalizationBuilder> Localizations =
			new Dictionary<SheetXBatchSheetKey, LocalizationBuilder>();

		/// <summary>
		/// Combined-JSON payloads per source index. Each inner map is one source's
		/// sheet name to JSON content, so two sources sharing a sheet name never merge.
		/// </summary>
		public readonly Dictionary<int, Dictionary<string, string>> CombinedJsons =
			new Dictionary<int, Dictionary<string, string>>();

		/// <summary>Languages included in generated localizations.</summary>
		public readonly List<string> LocalizedLanguages = new List<string>();

		/// <summary>Sheets already included in generated localizations.</summary>
		public readonly List<string> LocalizedSheetsExported = new List<string>();

		/// <summary>Character sets by language.</summary>
		public readonly Dictionary<string, string> LangCharSets =
			new Dictionary<string, string>(StringComparer.Ordinal);

		/// <summary>Aggregate localized character set.</summary>
		public readonly StringBuilder LangCharSetsAll = new StringBuilder();

		/// <summary>Whether duplicate IDs produce errors.</summary>
		public bool StrictDuplicateIds = true;

		/// <summary>Adds ID if absent, preserving first definition and reporting strict duplicates.</summary>
		public bool TryAddId(
			string key,
			int value,
			string source,
			string sheet,
			out string error)
		{
			error = null;

			if (AllIds.ContainsKey(key))
			{
				if (StrictDuplicateIds)
				{
					var first = IdOrigins[key];
					error = $"Duplicate ID '{key}': "
						+ $"first '{first.Source}' sheet '{first.Sheet}' value '{first.Value}'; "
						+ $"second '{source}' sheet '{sheet}' value '{value}'.";
				}

				return false;
			}

			AllIds.Add(key, value);
			IdOrigins.Add(key, new SheetXIdOrigin(source, sheet, value));
			return true;
		}
	}

	/// <summary>One source after validation and materialization.</summary>
	internal sealed class SheetXBatchSourceState
	{
		/// <summary>Zero-based source position in batch.</summary>
		public int Index;

		/// <summary>Spreadsheet source kind.</summary>
		public SheetXSourceKind Kind;

		/// <summary>Excel path or Google spreadsheet ID.</summary>
		public string SpreadsheetPath;

		/// <summary>Combined JSON artifact name.</summary>
		public string OutputName;

		/// <summary>Selected sheets in source order.</summary>
		public readonly List<string> SelectedSheets = new List<string>();

		/// <summary>Excel only. Open for whole batch; coordinator disposes.</summary>
		public NPOI.SS.UserModel.IWorkbook Workbook;

		/// <summary>Excel only. Backing stream for <see cref="Workbook"/>.</summary>
		public System.IO.MemoryStream Stream;

		/// <summary>Google only. Fetched once and reused by every phase.</summary>
		public Google.Apis.Sheets.v4.Data.Spreadsheet Metadata;
	}

	/// <summary>Drives one batch export across its six phases.</summary>
	internal sealed class SheetXBatchExporter
	{
		private readonly SheetXBatchExportRequest m_request;
		private readonly SheetXExportContext m_context;
		private readonly SheetXBatchState m_state = new SheetXBatchState();
		private readonly List<SheetXBatchSourceState> m_sources =
			new List<SheetXBatchSourceState>();

		private SheetXSettings m_settings;
		private ExcelSheetHandler m_excel;
		private GoogleSheetHandler m_google;

		internal SheetXBatchExporter(
			SheetXBatchExportRequest request,
			ISheetXOutput output)
		{
			m_request = request;
			m_context = new SheetXExportContext(output, discardStagedOnError: true);

			if (output == null)
				m_context.Error("Output is null.");
		}

		internal SheetXExportResult Export()
		{
			try
			{
				if (!Validate())
					return m_context.ToResult();

				m_settings = SheetXSettings.CreateTransient(m_request);

				if (m_settings.encryptJson && m_settings.UsesDefaultEncryptionKey)
				{
					m_context.Warn(
						"encryptJson is using SheetX's published default key. "
						+ "Set EncryptionKey before shipping encrypted data.");
				}

				m_excel = new ExcelSheetHandler(m_settings, m_context, m_state);

				if (HasGoogleSource())
				{
					m_google = new GoogleSheetHandler(
						m_settings,
						m_context,
						m_request.GoogleClientId,
						m_request.GoogleClientSecret,
						m_state);
				}

				if (!Materialize())
					return m_context.ToResult();

				m_context.Flush();
				return m_context.ToResult();
			}
			catch (Exception ex)
			{
				m_context.Error($"Batch export failed: {ex.Message}");
				return m_context.ToResult();
			}
			finally
			{
				Release();
			}
		}

		private void Release()
		{
			foreach (var source in m_sources)
			{
				source.Workbook?.Close();
				source.Stream?.Dispose();
				source.Workbook = null;
				source.Stream = null;
			}
		}

		private bool Validate()
		{
			if (m_request == null)
			{
				m_context.Error("Request is null.");
				return false;
			}

			if (m_context.HasErrors)
				return false;

			if (m_request.Sources == null || m_request.Sources.Count == 0)
			{
				m_context.Error("Sources is empty.");
				return false;
			}

			bool ok = true;
			var seen = new Dictionary<string, int>(StringComparer.Ordinal);

			for (int i = 0; i < m_request.Sources.Count; i++)
			{
				var source = m_request.Sources[i];

				if (source == null)
				{
					m_context.Error($"Source {i} is null.");
					ok = false;
					continue;
				}

				if (string.IsNullOrEmpty(source.SpreadsheetPath))
				{
					m_context.Error($"Source {i} has an empty SpreadsheetPath.");
					ok = false;
					continue;
				}

				if (source.Kind != SheetXSourceKind.Excel
					&& source.Kind != SheetXSourceKind.Google)
				{
					m_context.Error(
						$"Source {i} has an invalid Kind '{(int)source.Kind}'.");
					ok = false;
					continue;
				}

				if (source.OutputName != null && !IsValidOutputName(source.OutputName))
				{
					m_context.Error(
						$"Source {i} has an invalid OutputName '{source.OutputName}'.");
					ok = false;
				}

				if (source.Kind == SheetXSourceKind.Excel
					&& !System.IO.File.Exists(source.SpreadsheetPath))
				{
					m_context.Error(
						$"Spreadsheet '{source.SpreadsheetPath}' does not exist.");
					ok = false;
				}

				if (source.Kind == SheetXSourceKind.Google
					&& (string.IsNullOrEmpty(m_request.GoogleClientId)
						|| string.IsNullOrEmpty(m_request.GoogleClientSecret)))
				{
					m_context.Error(
						"GoogleClientId and GoogleClientSecret are required "
						+ "for a Google export.");
					ok = false;
				}

				string key = $"{(int)source.Kind}:{source.SpreadsheetPath}";
				if (seen.TryGetValue(key, out int first))
				{
					m_context.Error(
						$"Sources {first} and {i} are the same spreadsheet "
						+ $"'{source.SpreadsheetPath}'.");
					ok = false;
				}
				else
				{
					seen.Add(key, i);
				}
			}

			return ok;
		}

		private static bool IsValidOutputName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return false;

			if (name == "." || name == "..")
				return false;

			foreach (char c in name)
			{
				if (c == '/' || c == '\\' || char.IsControl(c))
					return false;
			}

			return true;
		}

		private bool Materialize()
		{
			bool ok = true;

			for (int i = 0; i < m_request.Sources.Count; i++)
			{
				var request = m_request.Sources[i];
				var source = new SheetXBatchSourceState
				{
					Index = i,
					Kind = request.Kind,
					SpreadsheetPath = request.SpreadsheetPath,
					OutputName = request.OutputName,
				};

				m_sources.Add(source);

				bool materialized = request.Kind == SheetXSourceKind.Excel
					? MaterializeExcel(source, request.Sheets)
					: m_google.BatchMaterialize(source, request.Sheets);

				if (!materialized)
				{
					ok = false;
					continue;
				}

				if (!CheckLocalizationFolder(source))
					ok = false;
			}

			if (!CheckCombinedOutputNames())
				ok = false;

			return ok;
		}

		private bool MaterializeExcel(
			SheetXBatchSourceState source,
			List<string> requestedSheets)
		{
			try
			{
				source.Stream = new System.IO.MemoryStream(
					System.IO.File.ReadAllBytes(source.SpreadsheetPath),
					writable: false);
				source.Workbook = NPOI.SS.UserModel.WorkbookFactory.Create(source.Stream);
			}
			catch (Exception ex)
			{
				m_context.Error(
					$"Could not read spreadsheet '{source.SpreadsheetPath}': "
					+ $"{ex.Message}");
				return false;
			}

			if (string.IsNullOrEmpty(source.OutputName))
			{
				source.OutputName = System.IO.Path.GetFileName(
					source.SpreadsheetPath);
			}

			var requested = requestedSheets == null
				? null
				: new HashSet<string>(requestedSheets, StringComparer.Ordinal);
			var available = new HashSet<string>(StringComparer.Ordinal);

			for (int i = 0; i < source.Workbook.NumberOfSheets; i++)
			{
				string name = source.Workbook.GetSheetAt(i).SheetName;
				available.Add(name);

				if (requested == null || requested.Contains(name))
					source.SelectedSheets.Add(name);
			}

			if (requested == null)
				return true;

			bool ok = true;
			foreach (string name in requestedSheets)
			{
				if (available.Contains(name))
					continue;

				m_context.Error(
					$"Spreadsheet '{source.SpreadsheetPath}' has no sheet '{name}'.");
				ok = false;
			}

			return ok;
		}

		private bool CheckLocalizationFolder(SheetXBatchSourceState source)
		{
			if (!string.IsNullOrEmpty(m_request.ConstantsOutputPath))
				return true;

			bool ok = true;
			foreach (string sheet in source.SelectedSheets)
			{
				if (!IsLocalizationSheet(sheet))
					continue;

				m_context.Error(
					$"Spreadsheet '{source.SpreadsheetPath}' selects localization sheet "
					+ $"'{sheet}' but ConstantsOutputPath is empty.");
				ok = false;
			}

			return ok;
		}

		private bool CheckCombinedOutputNames()
		{
			if (!m_request.CombineJson)
				return true;

			bool ok = true;
			var seen = new Dictionary<string, string>(StringComparer.Ordinal);

			foreach (var source in m_sources)
			{
				if (string.IsNullOrEmpty(source.OutputName))
					continue;

				if (seen.TryGetValue(source.OutputName, out string first))
				{
					m_context.Error(
						$"Sources '{first}' and '{source.SpreadsheetPath}' both resolve "
						+ $"to OutputName '{source.OutputName}'.");
					ok = false;
				}
				else
				{
					seen.Add(source.OutputName, source.SpreadsheetPath);
				}
			}

			return ok;
		}

		private bool HasGoogleSource()
		{
			foreach (var source in m_request.Sources)
			{
				if (source.Kind == SheetXSourceKind.Google)
					return true;
			}

			return false;
		}

		private static bool IsLocalizationSheet(string sheetName)
			=> sheetName.StartsWith(
				SheetXConstants.LOCALIZATION_SHEET,
				StringComparison.Ordinal);
	}
}
