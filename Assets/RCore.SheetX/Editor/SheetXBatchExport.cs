using System.Collections.Generic;

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
}
