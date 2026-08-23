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
}
