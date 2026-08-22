# SheetX R15 — Detached Batch Export Design

**Date:** 2026-08-22
**Package:** `com.rabear.rcore.sheetx` (1.3.0, editor-only)
**Base fingerprint:** `b7d399ebe40e2fb55257ff92cfd0f65e27027d64`
**Status:** approved design, ready for implementation plan

## 1. Goal

Export many Excel and Google spreadsheets in one detached run, with all sources
sharing one global ID namespace. Semantically the SheetX window's *Export Multi
Files*, but with the detached contract of the existing public API: no settings
asset access, no dialogs, no console logging, no `EditorPrefs`, caller-owned
`ISheetXOutput`.

Additive only. `ExportExcel`, `ExportGoogle`, `SheetXExportRequest`,
`SheetXExportResult`, `ISheetXOutput`, and the ordinal values of
`SheetXExportFileType` are unchanged.

`LocalizationAsObject` is **omitted**: the field does not exist on
`SheetXExportRequest` in this tree, so R14 has not landed.

## 2. Public API

```csharp
public enum SheetXSourceKind
{
    Excel,
    Google,
}

/// <summary>One spreadsheet inside a batch.</summary>
public sealed class SheetXBatchSource
{
    public SheetXSourceKind Kind;
    public string SpreadsheetPath;
    public List<string> Sheets;
    public string OutputName;
}

/// <summary>Everything one batch export needs. Shares every option across all sources.</summary>
public sealed class SheetXBatchExportRequest
{
    public List<SheetXBatchSource> Sources;

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

public static SheetXExportResult ExportBatch(SheetXBatchExportRequest request, ISheetXOutput output);
```

Contract notes:

- Membership in `Sources` means enabled. There is no `Enabled` field and no UI
  `selected` state in the public batch contract.
- One batch has one `SheetXExportContext`. `Files`, `Warnings`, `Errors`
  aggregate across the whole batch.
- One relative artifact path may be emitted at most once across the full batch.

## 3. Architecture

Looping the existing public single-source API cannot work: each call builds a
fresh `SheetXSettings.CreateTransient` and a fresh handler, so `m_allIds`,
`m_idsBuilderDict`, `m_constantsBuilderDict`, and `m_localizationsDict` reset
between sources. Cross-source ID resolution and batch-wide aggregate artifacts
are both impossible that way.

The legacy `ExcelSheetHandler.ExportAllFiles()` / `GoogleSheetHandler.ExportAllFiles()`
pair has the right multi-source shape but is unusable as-is: it reads the
settings asset's source lists, writes while building, uses dialogs and console
logs, re-fetches Google metadata per pass, and carries the aggregate-IDs defect
described in section 7.

So: a batch coordinator plus one shared state object, reusing the handlers'
mature parsers and generators in place.

Three decisions shape the diff:

**Staging is the preflight.** No separate path-prediction pass.
`SheetXExportContext` gains a staging buffer and a current origin
(`source`, `sheet`) that the coordinator sets before each sheet. Handlers keep
calling `m_writer.Write` unchanged. Collisions are detected at stage time and
name both origins. Nothing reaches `ISheetXOutput.Write` until the whole batch
succeeds. Predicting `characters_set_*`, `{name}Text.cs`, and
`LocalizationsManager.cs` paths ahead of emission would duplicate emission
logic and rot against it.

**Shared state, not extracted generators.** `BuildContentOfFileIDs`,
`LoadSheetConstantsData`, `LoadSheetLocalizationData`, `ConvertSheetToJson`,
`CreateLocalizationFile`, and `CreateLocalizationsManagerFile` are private
instance members. Reuse therefore lives inside the handler classes: an internal
`SheetXBatchState` is passed in, and the handler aliases it instead of resetting
its own fields. One batch = one Excel handler + one Google handler sharing one
state.

**`DeclaredIds` fixes the aggregate bug, and strict duplicate policy is
batch-only.** `AllIds` stays the lookup table. A separate `DeclaredIds` set
guards `public const int` emission. The strict duplicate policy is a flag on the
state, off for single-source exports, so existing single-source behavior — and
the test that locks it — is unchanged.

## 4. Components

**New: `Assets/RCore.SheetX/Editor/SheetXBatchExport.cs`**

- Public: `SheetXSourceKind`, `SheetXBatchSource`, `SheetXBatchExportRequest`.
- Internal `SheetXBatchState`: `Dictionary<string,int> AllIds`,
  `Dictionary<string,IdOrigin> IdOrigins` (`IdOrigin` = spreadsheet path, sheet
  name, value — the first definition's), `HashSet<string> DeclaredIds`
  (`StringComparer.Ordinal`), builder dictionaries keyed by
  `(int sourceIndex, string sheet)`, localization dictionaries, character sets,
  `bool StrictDuplicateIds`.
- Internal `SheetXBatchSourceState`: resolved source — kind, path, resolved
  `OutputName`, ordered selected sheet names, and either the open `IWorkbook`
  or the fetched `Spreadsheet` metadata.
- Internal `SheetXBatchExporter`: validate, materialize, pass 1, pass 2,
  aggregates, emit.

**Modified: `Assets/RCore.SheetX/Editor/SheetXExport.cs`**

- `SheetXExporter.ExportBatch(request, output)`, delegating to the coordinator.
- `SheetXExportContext`: `Stage(...)` buffers instead of writing immediately,
  `SetOrigin(source, sheet)`, `Flush()`. `ExportExcel` / `ExportGoogle` call
  `Flush()` immediately after their handler returns, so their observable
  behavior does not change. Collision messages name both origins.

**Modified: `ExcelSheetHandler.cs`, `GoogleSheetHandler.cs`**

- Internal constructor overload accepting a `SheetXBatchState`.
- When supplied, state fields alias it and the per-call
  `m_allIds = new Dictionary...` resets are skipped.
- The ID declaration guard reads `DeclaredIds`, not `AllIds`.
- Builder dictionary keys carry the source index, so two sources with the same
  sheet name no longer merge silently.
- Batch-mode internal entry points per phase, so the coordinator drives order.
- No new public members. No `m_writer.Blocking` on the batch path.

**Modified:** `Assets/RCore.SheetX/Tests/SheetXExportTests.cs`, root
`CHANGELOG.md`, `Assets/RCore.SheetX/CHANGELOG.md`.

## 5. Flow

**Phase A — validate.** No network, no file read.

1. `output == null` → `"Output is null."`; `request == null` →
   `"Request is null."`; `Sources` null or empty → `"Sources is empty."`
2. Per source in index order: null entry, empty `SpreadsheetPath`, invalid
   `OutputName` (trim-empty, contains `/`, `\`, or a control character, or
   equals `.` or `..`). An extension is never stripped.
3. Excel source whose file does not exist → error naming the path.
4. Any Google source with missing credentials →
   `"GoogleClientId and GoogleClientSecret are required for a Google export."`
5. Duplicate source key `(Kind, SpreadsheetPath)` under
   `StringComparer.Ordinal` → error naming both indices.
6. Errors present → return with `Files` empty; nothing written.

**Phase B — materialize.** Source order.

- Excel: read bytes into a `MemoryStream`, `WorkbookFactory.Create`, and hold
  the workbook for the whole batch (both passes).
- Google: exactly one `Spreadsheets.Get` per spreadsheet, reused across both
  passes. `GridProperties.ColumnCount` null is an error, never a dereference.
- Sheet selection: `Sheets == null` selects every sheet in native
  workbook/metadata order; a non-null list selects that subset, still in native
  order; an empty list selects none. A requested name the source does not have
  is a deterministic error naming source and sheet.
- `OutputName` resolves to the explicit value, else the Excel filename, else the
  Google title.
- When `CombineJson`, duplicate resolved `OutputName` is an error naming both
  spreadsheet paths.
- A localization sheet is selected while `ConstantsOutputPath` is empty → error.
  (Deferred from Phase A because selection is only known here.)
- Errors → return before pass 1.

**Phase C — pass 1, global IDs.** Every source, every selected `*IDs` sheet, in
source order then native sheet order. Keys and values load into `AllIds` with
their origin in `IdOrigins`. No C# is emitted. A duplicate key is an error
regardless of whether the values match; the first value stays in the table.
Diagnostics keep accumulating, but nothing is emitted while any error stands.

**Phase D — pass 2, build.** Source order, then native sheet order. Per source:
IDs declarations, JSON, Constants, Localization — all resolving references from
the one global ID table. `DeclaredIds` guards `public const int` emission.
Artifacts are staged with origin `(source, sheet)`; a stage collision is an error
naming both origins. `CombineJson` produces one `{OutputName}.txt` per source;
otherwise one JSON artifact per JSON sheet. Separate modes stay per-sheet, but
the collision namespace is batch-wide.

**Phase E — aggregates, once per batch.** `IDs.cs` when `!SeparateIDs`,
`Constants.cs` when `!SeparateConstants`, the aggregate localization group when
`!SeparateLocalizations`, and `LocalizationsManager.cs` once.

**Phase F — emit.** With zero errors, staged artifacts flush in stage order to
`ISheetXOutput.Write`. With any error, nothing flushes and `Files` is empty.

Warnings never block emission.

## 6. Errors and side effects

Every expected fault lands in `SheetXExportResult.Errors`. Nothing is thrown at
the caller.

Forbidden throughout, per the R15 constraint:

> Không đọc, tạo, hay dirty `Assets/SheetX/SheetXSettings.asset`. Không đọc
> `EditorPrefs` — credential đến từ request. Không `EditorUtility.DisplayDialog`.
> Không `Debug.Log` (`settings.silent = true`). OAuth token cache hiện có dưới
> `Library/SheetX` giữ nguyên; không serialize credential/token vào asset,
> `.sx`, `ProjectSettings`, hay Git.

The batch path calls `m_writer.Error`, never `m_writer.Blocking`.

Sink failure is not transactional and is not claimed to be. `ISheetXOutput.Write`
is caller-owned and may fail after earlier writes were accepted; those side
effects cannot be rolled back. On failure the exporter records the artifacts the
sink accepted, stops writing the rest, and reports the error.

Diagnostics name both origins deterministically:

```text
Duplicate ID 'HERO_1': first 'a.xlsx' sheet 'HeroIDs' value '1'; second 'b.xlsx' sheet 'HeroIDs' value '2'.
Artifact 'Generated/Data.txt' collision: first 'a.xlsx' sheet 'Data'; second 'b.xlsx' sheet 'Data'.
```

The vague existing message — `"Artifact '{relativePath}' was produced more than
once."` — is not acceptable for the batch path.

## 7. Defects this fixes

**Aggregate `IDs.cs` is empty in multi-file export.** `ExportAllFiles()` loads
every ID sheet into `m_allIds` in pass 1, then calls `BuildContentOfFileIDs` in
pass 2. That builder treats an existing key in `m_allIds` as a duplicate and
`continue`s, so it appends `#region` markers and no declarations. The aggregate
file compiles to nothing useful. Fix: `AllIds` for resolution, `DeclaredIds` for
emission — never the lookup table as the emission guard.

**Inconsistent duplicate-ID handling.** `LoadSheetIDsValues` shows a modal and
then overwrites (last wins); `BuildContentOfFileIDs` skips (first wins). Batch
policy: any duplicate symbolic key is an error, the first value stays, the
diagnostic carries key, both sources, both sheets, and both values.

**Cross-source sheet-name merge.** `m_idsBuilderDict`, `m_constantsBuilderDict`,
and `m_localizationsDict` are keyed by sheet name alone, so two sources with a
`Data` sheet merge silently. Batch keys include the source index.

**Repeated Google metadata fetches.** Legacy multi-export calls
`Spreadsheets.Get` in both passes. Batch materializes once and reuses.

**Google selection order.** Detached `ResolveSheets` returns the caller's
request order and matches with default string comparison. Batch resolves the
subset in metadata order with ordinal semantics.

**Nullable dereference.** `GridProperties.ColumnCount.Value` is dereferenced at
five sites. The batch path guards instead of crashing.

**Localization needs the constants folder.** `ExportLocalizations` errors when
`constantsOutputFolder` is empty. Batch preflight checks it as soon as a
localization sheet is known to be selected.

## 8. Tests

All in `Assets/RCore.SheetX/Tests/SheetXExportTests.cs`, Excel-only workbooks
built with the existing `XSSFWorkbook` + `MemoryOutput` helpers. Google behavior
is source-reviewed; no OAuth or network test.

1. An ID defined in source A resolves a reference in source B's JSON.
2. Aggregate `IDs.cs` is non-empty and carries declarations from both sources.
3. Aggregate `Constants.cs` carries constants from both sources.
4. Duplicate IDs, same value and different value: full origin diagnostics, zero
   artifacts, zero writes.
5. `CombineJson` produces two named files; duplicate `OutputName` fails before
   any write.
6. The same JSON sheet name in two sources with `CombineJson == false` fails
   deterministically before any write.
7. Validation: null output, null request, empty sources, duplicate source,
   missing Excel path, missing Google credentials.
8. No asset is created under `Assets/SheetX/` by a batch call.
9. Also: a requested missing sheet, a malformed `OutputName`, a localization
   sheet with no `ConstantsOutputPath`, and source-order plus native-sheet-order
   determinism.

## 9. Changelog

One PR-sized commit with its own entry. Root `CHANGELOG.md` is canonical; the
package `CHANGELOG.md` gets the same entry. Both must record the batch API, the
shared global ID namespace, and the aggregate-IDs-empty multi-file fix.

Commit scope: `feat(sheetx):`.

## 10. Out of scope

- No `LocalizationAsObject` (R14 has not landed).
- No change to the SheetX editor windows or their multi-file buttons.
- No SheetX release tag: `.github/workflows/release.yml:31` validates a `v*` tag
  against `Assets/RevCore/**/package.json` only, so SheetX still cannot be
  tagged with that workflow.
- No Google OAuth automated test.
- No encryption work.
