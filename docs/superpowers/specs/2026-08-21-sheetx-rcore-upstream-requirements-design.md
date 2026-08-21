# SheetX upstream requirements design

## Goal

Make `com.ikit.sheetx` call SheetX export engine without `SheetXWindow`, `SheetXSettings.Init()`, settings asset, direct disk writes, or modal dialogs. Fix silent output-correctness defects before iKit uses engine.

## Scope

Change only `Assets/RCore.SheetX`. Engine remains generic: spreadsheet read, content build, output events, data correctness. No `iKit.Config`, VContainer, Addressables, `iKit.Variant`, iKit settings UI, artifact policy, or validation schema.

## Public editor API

Add `SheetXExport.cs` in `RCore.SheetX.Editor`:

```csharp
public interface ISheetXOutput
{
	void Write(string relativePath, string content);
}

public sealed class SheetXExportRequest
{
	public string SpreadsheetPath;
	public List<string> Sheets;
	public string ConstantsOutputPath;
	public string JsonOutputPath;
	public string LocalizationOutputPath;
	public bool CombineJson;
	public bool SeparateIDs;
	public bool SeparateConstants;
	public bool SeparateLocalizations;
	public bool OnlyEnumAsIDs;
	public string Namespace;
	public string PersistentFields;
	public bool EncryptJson;
	public string EncryptionKey;
	public string GoogleClientId;
	public string GoogleClientSecret;
}

public sealed class SheetXExportFile
{
	public string RelativePath;
	public SheetXExportFileType Type;
}

public sealed class SheetXExportResult
{
	public IReadOnlyList<SheetXExportFile> Files;
	public IReadOnlyList<string> Warnings;
	public IReadOnlyList<string> Errors;
	public bool Success;
}

public static class SheetXExporter
{
	public static SheetXExportResult ExportExcel(SheetXExportRequest request, ISheetXOutput output);
	public static SheetXExportResult ExportGoogle(SheetXExportRequest request, ISheetXOutput output);
}
```

`Sheets == null` selects all source sheets. Empty list selects none. Output paths are relative output roots. Emitted `relativePath` combines configured root and filename with `/`; no API decides physical disk location.

Request carries every setting needed by generic engine. It must never call `SheetXSettings.Init()` or read a `SheetXSettings` instance. Google caller supplies OAuth client ID/secret directly. Request has no output-writer policy; iKit owns compare-before-write and asset import.

## Export flow

`SheetXExporter` owns request-scoped state: ID map, sorted IDs, generated content, warnings/errors, output file list, source service/workbook lifetime. It validates request/output, opens Excel source once, reads all selected input, builds artifacts, then calls `ISheetXOutput.Write` once per artifact. It records file only after `Write` returns. Exceptions at source/auth/output boundaries become result errors with useful context; `Success` is true only with zero errors.

Excel keeps stream and `IWorkbook` in same export scope. `ExcelSheetsPath.GetWorkBook()` is repaired so no caller receives a workbook whose source stream has already closed; new exporter opens/owns stream directly and disposes workbook then stream after export. Existing UI callers continue working.

Google exporter fetches selected sheets once per request. OAuth can require interactive consent; batch callers must pre-authorize cache. Parse/content errors never open Unity dialogs. Result captures every malformed ID, duplicate ID, invalid JSON cell, missing sheet, missing template, and invalid numeric value with source sheet, field, row where applicable.

Legacy `ExcelSheetHandler` / `GoogleSheetHandler` remain UI adapters. They create a request from current `SheetXSettings` and use `SheetXFileOutput`, an `ISheetXOutput` adapter over existing `SheetXHelper.WriteFile`; existing UI may display result messages. Engine path never calls `DisplayDialog`, `Debug.Log`, or direct file write.

## Content correctness

All parsing/formatting in exporter uses `CultureInfo.InvariantCulture`:

- `int.TryParse` / `float.TryParse` use explicit `NumberStyles` and invariant culture.
- Numeric literal emission formats with invariant culture, including constants, arrays, vectors, attributes, and `Att.GetJsonString` values.
- Failed required integer/float parse adds contextual result error; no raw `Parse` exception.

`SheetXHelper.SortIDsByLength` returns IDs ordered by descending key length, then `StringComparer.Ordinal`. Both handlers stop inline sort construction and call helper. This ensures raw JSON ID replacements change `HERO_10` before `HERO_1`.

Combined duplicate-name columns emit `[]` when row has no values in group. This preserves property and produces valid JSON.

Google `ExportAllFiles` style flow writes each combined output after all selected sheets: merged JSON, IDs, Constants, Localization. Order of selected sheets cannot alter aggregate content. Deterministic ordinal ordering applies whenever dictionary aggregation becomes output.

Missing `TextAsset` templates produce exporter errors naming template constant and `Resources` lookup name; no null dereference.

## Security and cleanup

OAuth client ID/secret stay only in `EditorPrefs`; serialized migration fields remain migration-only and must be blank after migration. Token cache moves from `Assets/Editor` to `Library/SheetX` so it cannot enter source control or exported `Assets` zip. `.gitignore` keeps wildcard legacy token patterns. `PrefKey` changes from `Application.identifier` to stable project identity based on normalized project path hash, retaining a migration read from prior bundle-ID key before new key is written. Missing credential with legacy asset residue logs warning only in UI path.

`SheetXHelper` becomes `static class`. UI-only helper methods stay there; no runtime move. Existing `WriteFile` remains for legacy writer only.

## Tests

Editor NUnit tests use memory `ISheetXOutput` and temporary files outside `Assets`.

- ID replacement: `HERO_10` becomes `9`, not `50`; same-length ordering ordinal.
- `de-DE` and `en-US` exports produce byte-identical artifact content.
- Invalid numeric value returns contextual error, never exception/dialog.
- Three empty combined columns export parseable JSON with `[]`.
- Large multi-sheet `.xlsx` exports after source-open lifetime remains valid.
- Google aggregation logic uses fixture values / extracted deterministic content builder: reverse sheet order yields same combined artifact and write occurs once.
- Memory output receives artifacts and does not touch disk.
- Missing template produces named error.
- Token cache path begins under `Library/`, never `Assets/`.

## CHANGELOG

Root `CHANGELOG.md` receives one SheetX entry. It states new external editor export API, no-modal result behavior, output sink seam, invariant numeric behavior, long-ID replacement ordering, empty combined arrays emitted as `[]`, workbook lifetime repair, merged Google output write timing, and token-cache move.
