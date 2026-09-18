# SheetX Structure Preview and Data Class Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preview an ordinary sheet's C# structure without exporting, and warn before collection export discards top-level JSON members unsupported by the selected Existing Data Class.

**Architecture:** Reuse the two existing producers: legacy JSON feeds a draft class generator; Generated Data Class feeds the existing schema-backed row emitter. A Newtonsoft-contract member checker serves preview and collection export; the existing deserialization check remains authoritative for value compatibility. Small handler entry points acquire preview data without invoking an export, while an IMGUI window owns a single disposable snapshot.

**Tech Stack:** Unity 2022.3 editor IMGUI, NPOI, Google Sheets API v4, Newtonsoft.Json supplied by Unity package `com.unity.nuget.newtonsoft-json` 3.2.1, existing NUnit/EditMode test assembly.

**Approved spec:** `docs/superpowers/specs/2026-09-16-sheetx-structure-preview-design.md` (approved 2026-09-16).

**Status:** Implementation plan; no implementation, tests, commit, or push performed by writing this document. Historical full-suite result was 379 passing tests; establish a fresh baseline before execution.

## Global Constraints

- One button per ordinary JSON sheet, across all three Output Modes.
- Excel and Google Sheets use the same preview window.
- Existing Data Class validation runs in preview and during export.
- Interactive export asks `Export Anyway` / `Cancel` when JSON contains unmatched members.
- Successful Newtonsoft deserialization defines type compatibility; do not invent a stricter numeric/type conversion policy.
- Member comparison is top-level only for this release.
- Preview retains its current window-local snapshot; `Refresh` rereads the source. No shared static cache.
- Export validation always uses that export's fresh JSON, never preview data.
- No implementation, version bump, commit, or push is authorized by this spec-writing request.
- Preview writes no JSON, scripts, settings, assets, or migration snapshots. It does not bake, refresh AssetDatabase, create bindings, or alter selections. Existing Google authorization may update its normal token cache; do not confuse authentication with exported artifacts.
- Use the primary checkout `E:/Projects/_/RCore`, never another worktree. Do not discard unrelated settings, generated assets, editor layouts, or user changes.
- No new dependencies, runtime code, exporter rewrite, schema registry, or whole-export rollback mechanism.
- Keep new implementation types/internal entry points internal. Preserve existing public method signatures; do not expose internal source descriptors through a public signature.
- `.cs`: tabs and CRLF; markdown: spaces and LF. Private fields `m_camelCase`, private statics `s_camelCase`; NUnit methods `snake_case_descriptive`.
- A missing symbol/compiler error is not sufficient red proof. Add only compilable signatures/default-return stubs, then observe the new behavioral assertion fail before implementing it. Every task ends with focused green and regression checks.

## Grounded integration facts

Line references describe the pre-implementation code; locate symbols after edits.

- `SheetXCollectionExportSession.TryAddExistingTable` currently resolves a row type and calls `JsonConvert.DeserializeObject(json, rowType.MakeArrayType())` before adding the candidate (lines 190–216). Keep this rejection behavior.
- `Flush` admits candidates, emits source strings, then detects depth changes/captures migration state and commits staged files (lines 276–388). Insert member confirmation after successful final `EmitFiles`, before `SourcesChanged` and all migration/file mutations. `writer.Write` stages; `context.Flush` commits.
- Detached `SheetXExporter`/batch requests carry no collection bindings; do not invent class validation for those requests. A settings-backed collection session in Unity batch mode must warn rather than display the new dialog. Keep current depth-migration confirmation policy unchanged.
- Legacy conversion supports merged cells, reference IDs and Attribute System output. Its exact key is `Attributes`, capital A (`ExcelSheetHandler.cs:1976`), not a guessed header field.
- Single-source Excel JSON loads all listed `*IDs` sheets, regardless of checkbox; single-source Google JSON loads selected `*IDs` sheets. Preserve these existing rules.
- Preview is source-local. Show: `Uses IDs from this spreadsheet; multi-file export may resolve IDs differently.` Do not claim equivalence with cross-source batch ID preparation.
- `GoogleSheetHandler.GetCacheMetadata` may mutate selections; `ResolveSheets` marks supplied sheets selected. Neither belongs in preview acquisition.
- Existing enum `SheetXSourceKind` is declared in `SheetXBatchExport.cs`. Reuse it, do not redeclare it.
- Four table hosts: `EditExcelSheetsWindow`, `EditGoogleSheetsWindow`, `ExcelSheetXWindow`, `GoogleSheetXWindow`.
- Test internals are already visible through `Editor/AssemblyInfo.cs`. Test asmdef uses `overrideReferences`; add only installed Google DLL references needed for Task 5 DTO tests, not new packages.

## File map and task order

All paths below are repository-relative.

| Task | Production files | Test/document files |
| --- | --- | --- |
| 1 | Create `Assets/RCore.SheetX/Editor/Preview/SheetXSheetStructure.cs` | Create `Assets/RCore.SheetX/Tests/SheetStructureTests.cs` |
| 2 | Create `Assets/RCore.SheetX/Editor/Preview/SheetXRowTypeMatch.cs` | Create `Assets/RCore.SheetX/Tests/RowTypeMatchTests.cs` |
| 3 | Modify `Editor/Collection/SheetXCollectionGenerator.cs`, `SheetXCollectionExportSession.cs` under `Assets/RCore.SheetX/` | Modify `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs` |
| 4 | Modify `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs`; create `Editor/Preview/SheetXSheetJsonSource.cs` | Create `Assets/RCore.SheetX/Tests/SheetPreviewSourceTests.cs` |
| 5 | Modify `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs`, `Editor/Preview/SheetXSheetJsonSource.cs` | Extend `SheetPreviewSourceTests.cs`; adjust `Tests/RCore.SheetX.Tests.asmdef` only for installed SDK DLLs |
| 6 | Modify `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs` | Extend `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs` |
| 7 | Create `Assets/RCore.SheetX/Editor/Preview/SheetXSheetStructureWindow.cs`; modify `Editor/SheetXHelper.cs` and four hosts | Create `Assets/RCore.SheetX/Tests/SheetStructureWindowTests.cs` |
| 8 | No new production subsystem; correct only demonstrated defects | Add compiler/integration checks to the new fixtures; manual matrix |
| 9 | No production changes | English/Vietnamese manuals; root and SheetX `[Unreleased]` changelogs |

Unity generates `.meta` files for new directories/scripts/tests. Include only those belonging to this feature; never fabricate/reuse GUIDs.

## Execution gates

Before execution, inspect `git status`, record user changes and ProjectVersion contents, and run the current SheetX tests. Do not reuse old XML as evidence.

Use the installed 2022.3.62f3 editor — the user approved it on 2026-09-16 as equivalent to the project's f2 for this work. Close this project's interactive instance first. A headless f3 run rewrites `ProjectSettings/ProjectVersion.txt`; restore it after every run with `git checkout -- ProjectSettings/ProjectVersion.txt` so the patch bump never reaches a diff.

Example focused run, PowerShell:

```powershell
& "D:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -projectPath "E:/Projects/_/RCore" -runTests -testPlatform EditMode -testFilter "RCore.SheetX.Tests.SheetStructureTests" -testResults "E:/Projects/_/RCore/Temp/structure-red.xml" -logFile "E:/Projects/_/RCore/Temp/structure-red.log"
```

Omit `-quit`. Wait for process completion, inspect the log and the XML from that exact run. A compile failure may produce no XML; it is not a passing or behavioral-red test result. Use distinct red/green output filenames. Check XML counts/results with stdlib:

```powershell
python -c "import xml.etree.ElementTree as E; r=E.parse('Temp/structure-red.xml').getroot(); print(r.attrib); [print(x.attrib) for x in r.iter('test-case') if x.get('result') == 'Failed']"
```

After Unity regenerates project files for newly added scripts, also run:

```powershell
dotnet build RCore.SheetX.Tests.csproj --nologo -v q -clp:ErrorsOnly
git diff --check
```

A stale `.csproj` may omit new scripts; do not treat its successful build as coverage. Baseline warnings must be recorded separately from new warnings. `ProjectVersion.txt` is restored after each headless run because the f3 editor rewrites it; never let that bump enter a diff.

### Task 1: Infer a deterministic legacy JSON draft

**Files:** Create `Editor/Preview/SheetXSheetStructure.cs` and `Tests/SheetStructureTests.cs` under `Assets/RCore.SheetX/`.

**Interfaces:**

- Consumes Newtonsoft tokens and existing `SheetXCollectionNaming` identifier helpers.
- Produces `SheetXSheetStructure.TryFromJson(string json, string rootTypeName, out SheetXSheetStructure structure, out string error)`, instance `ToClassCode(string namespaceName)`, `IReadOnlyList<string> RootMemberNames`, and `IReadOnlyList<string> Diagnostics`.
- Keep inference-tree types private. Validator does not depend on draft field types. Do not create a public schema API.

- [ ] **Step 1: Add behavioral tests, then compilable signatures only.**

Put these tests in `RCore.SheetX.Tests.SheetStructureTests` with NUnit and LINQ imports:

```csharp
[Test]
public void keys_from_later_rows_and_valid_legacy_spelling_survive()
{
    Assert.That(SheetXSheetStructure.TryFromJson(
        "[{\"id\":1},{\"bot_ids\":[4,7],\"Attributes\":[{\"id\":3}]}]",
        "PoolsSX", out var draft, out string error), Is.True, error);
    Assert.That(draft.RootMemberNames,
        Is.EqualTo(new[] { "id", "bot_ids", "Attributes" }));
    string code = draft.ToClassCode("");
    Assert.That(code, Does.Contain("public int[] bot_ids;"));
    Assert.That(code, Does.Contain("public Attributes[] Attributes;"));
}

[TestCase("[]")]
[TestCase("[{}]")]
public void empty_observations_are_not_a_complete_class(string json)
{
    Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out _, out string error), Is.False);
    Assert.That(error, Is.EqualTo("Cannot infer structure from empty exported data"));
}

[TestCase("{}")]
[TestCase("[1]")]
[TestCase("[{\"id\":1},false]")]
[TestCase("[{\"id\":1}] []")]
public void unsupported_roots_rows_and_trailing_documents_are_rejected(string json)
{
    Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out _, out string error), Is.False);
    Assert.That(error, Is.Not.Null.And.Not.Empty);
}

[Test]
public void observations_widen_numbers_and_label_unknown_shapes()
{
    const string json = "[{\"large\":3000000000,\"ratio\":1,\"empty\":[],\"unknown\":null},"
        + "{\"ratio\":2.5,\"mixed\":false},{\"mixed\":\"text\"}]";
    Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);
    string code = draft.ToClassCode("");
    Assert.That(code, Does.Contain("public long large;"));
    Assert.That(code, Does.Contain("public double ratio;"));
    Assert.That(code, Does.Contain("public object[] empty;"));
    Assert.That(code, Does.Contain("public object unknown;"));
    Assert.That(code, Does.Contain("public object mixed;"));
    Assert.That(draft.Diagnostics, Is.Not.Empty);
}
```

Add a test for names using `JObject` to avoid hand-escaped JSON: keys `class`, `_class`, `a-b`, `aB`, `RowSX`, `quote"\nkey`, and nested keys `reward`/`Reward`. Require distinct legal field names, no member named exactly its enclosing type, exact JSON-name attributes on renames, and deterministic code on repeat calls. Task 8 compiles this generated draft to verify escaping and name collisions, not merely substring presence.

- [ ] **Step 2: Observe behavioral red** with filter `RCore.SheetX.Tests.SheetStructureTests`. Record the failed assertion. Do not stop at missing-symbol errors.

- [ ] **Step 3: Implement the small private inference tree and emitter.**

Parse without automatic date conversion, reject duplicate properties and extra documents:

```csharp
using var input = new StringReader(json ?? "");
using var reader = new JsonTextReader(input)
{
    DateParseHandling = DateParseHandling.None,
    MaxDepth = 64,
};
var root = JToken.ReadFrom(reader, new JsonLoadSettings
{
    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
});
if (!(root is JArray rows) || rows.Any(row => !(row is JObject)))
{
    error = "JSON root must be an array of objects.";
    return false;
}
while (reader.Read())
{
    if (reader.TokenType == JsonToken.Comment)
        continue;
    error = "JSON contains more than one root value.";
    return false;
}
```

Catch parse/overflow failures and return an error; never discard non-object rows through `OfType<JObject>()`. Validate root type name before emitting. Reject an invalid nonempty namespace with `ArgumentException` from `ToClassCode`; the source facade converts it to a preview error.

For each object scope, keep a first-seen ordered key list plus a dictionary of token observations. Infer once after collecting every row/array element:

- All numeric integer observations fitting Int32: `int`; otherwise fitting Int64: `long`; integer/fractional mix: `double`.
- Integers beyond Int64: explicit `object` fallback and diagnostic, not overflow.
- All booleans/strings: `bool`/`string`; ISO-looking strings remain `string`.
- All objects: one supporting class with the union of their keys.
- All arrays: infer from all elements. Empty-only arrays use `object[]` with diagnostic. Arrays of objects produce supporting classes.
- Null-only: `object`. Null plus nonnullable scalar: conservative `object` with diagnostic. Incompatible scalar/object/array kinds: `object`. Nested arrays use `object[]` with a diagnostic rather than pretending their leaves are scalar elements.

Naming: preserve legal keys verbatim, including `bot_ids` and `Attributes`. Reserve all legal keys and the enclosing type name before assigning sanitized alternatives, then append `2`, `3`, etc. when needed. Handle type names in one deterministic namespace-wide set seeded with the root name. A renamed field always carries its exact original JSON name.

Use fully qualified attributes in drafts to avoid a user key/type called `Serializable` or `JsonProperty` shadowing the attribute. Encode attribute string arguments with the installed Newtonsoft writer:

```csharp
string quotedName = JsonConvert.ToString(jsonName, '"', StringEscapeHandling.EscapeNonAscii);
source.Append("[global::Newtonsoft.Json.JsonProperty(")
    .Append(quotedName).Append(")]\r\n");
// Emit [global::System.Serializable] on each generated draft class.
```

Source output uses tabs/CRLF and no runtime loader, automatic binding, or collection class. A local inference fallback gets one `ponytail:` comment documenting the supported shape ceiling and future upgrade path.

- [ ] **Step 4: Run focused green and build.** Check malformed input, strings resembling dates, integer overflow, nested arrays, nulls, names/escaping, and deterministic ordering. No golden test count is promised.
- [ ] **Step 5: Record results; do not commit.**

### Task 2: Member coverage against the Newtonsoft contract

**Files:** Create `Editor/Preview/SheetXRowTypeMatch.cs` and `Tests/RowTypeMatchTests.cs` under `Assets/RCore.SheetX/`.

**Interfaces:**

- Consumes `DefaultContractResolver` and `JsonObjectContract` from the installed Newtonsoft package.
- Produces `SheetXRowTypeMatch.Compare(IReadOnlyList<string> jsonMemberNames, Type rowType)`, `SheetXRowTypeMatch.CompareJson(string json, Type rowType)`, `IReadOnlyList<string> UnmatchedJsonMembers`, `IReadOnlyList<string> UnobservedClassMembers`, `SheetXRowTypeMatchState State` (`Compared`, `EmptyData`, `Unverifiable`), `string Note`, and `bool HasUnmatched`.

Rules:

- User ruling (2026-09-18): follow the actual deserializer, not a separately frozen default contract. Obtain effective settings/contracts per comparison using `JsonSerializer.CreateDefault()` so current `JsonConvert.DefaultSettings` is honored, including changes after earlier comparisons. Do not capture this mutable global once in a static field. A root-level reading converter (attribute or serializer converter), non-object contract, or extension-data contract remains `Unverifiable` with a note beginning `Member coverage cannot be verified`; a member-level converter does not itself make its JSON name unknown.
- Accepted destinations include writable non-ignored properties and applicable creator parameters. A get-only initialized collection/object can be populated in place by Newtonsoft and must not automatically be classified as discarded. Where actual acceptance depends on an instance, getter, callback, or creation policy that cannot safely be established from the contract, report `Unverifiable` rather than a false unmatched verdict. Do not construct user objects or invoke their getters merely to inspect member coverage. Ignored members and provably non-populatable get-only scalar properties remain unmatched. This supersedes Task 2's blanket read-only rejection.
- Match exact serialized names first, then the deserializer's case-insensitive fallback (`GetClosestMatchProperty`); a `[JsonProperty("...")]` rename is matched by its serialized name.
- An empty member list returns `EmptyData` and no class-member complaints.
- `CompareJson` accepts only an array of objects and unions top-level keys in first-seen order. Malformed JSON returns `Unverifiable` with the parse reason.

- [ ] **Step 1: Write tests with fixture types.** Cover: an unmatched key is detected; an absent class member is informational only; exact-then-case-insensitive matching; `[JsonProperty]` rename honored; `[JsonIgnore]`, a read-only property, and a private field are all rejected as destinations; an inherited member is accepted; `[JsonExtensionData]` yields `Unverifiable`; a struct works; a `[JsonConstructor]` parameter named `legacyFlag` is accepted; a type with a custom `[JsonConverter]` yields `Unverifiable`; `CompareJson("[]")` is `EmptyData`; malformed `CompareJson` is `Unverifiable`.
- [ ] **Step 2: Observe behavioral red** with filter `RCore.SheetX.Tests.RowTypeMatchTests`.
- [ ] **Step 3: Implement.** `rowType == null` is `Unverifiable`. Keep first-seen order for unmatched names and unobserved accepted destinations. Never throw for a user type with an unusual contract; return `Unverifiable`.
- [ ] **Step 4: Focused green plus `dotnet build`.**
- [ ] **Step 5: Record results; do not commit.**

### Task 3: Schema-backed emission for one Generated Data Class row type

**Files:** Modify `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionGenerator.cs` and `SheetXCollectionExportSession.cs`; extend `Tests/CollectionGenerationTests.cs`.

**Interfaces:**

- Produce `internal static string SheetXCollectionGenerator.EmitRowTypes(SheetXSettings settings, SheetXCollectionSchema schema)`, reusing the existing private `BeginSource`, `SourceIndent`, `AppendRowTypes`, `EndSource`.
- Produce `internal bool SheetXCollectionExportSession.TryParseGeneratedSchema(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows, out SheetXCollectionSchema schema, out IReadOnlyList<string> warnings, out string error)` using the same ID resolution and `TryParse` path as `TryAddGeneratedTable`, with no binding or candidate side effects.

- [ ] **Step 1: Tests.** Emission contains only the row and nested partial classes with the banner, `using System;`, `using RCore.SheetX;`, and the namespace when configured; it excludes `SheetXCollectionPaths` and every collection class; two calls produce identical text; annotated headers with no rows still emit a class; `TryParseGeneratedSchema` resolves symbolic IDs from the session's ID dictionary and adds no binding or candidate (`settings.sheetBindings.Count` unchanged; a following `Flush` writes nothing new).
- [ ] **Step 2: Behavioral red** with filter `RCore.SheetX.Tests.CollectionGenerationTests`.
- [ ] **Step 3: Implement the two narrow wrappers**; do not change naming or existing emission text.
- [ ] **Step 4: Full `CollectionGenerationTests` green and build.**
- [ ] **Step 5: Record; do not commit.**

### Task 4: Excel preview acquisition without export side effects

**Files:** Modify `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs`; create `Editor/Preview/SheetXSheetJsonSource.cs`; create `Tests/SheetPreviewSourceTests.cs`.

**Interfaces:**

- Internal descriptor `SheetXSheetSource { SheetXSourceKind Kind; string Id; IReadOnlyList<SheetPath> Sheets; }` reusing the enum in `SheetXBatchExport.cs`.
- Internal result `SheetXSheetPreviewData { SheetXSheetOutputMode Mode; string Json; SheetXCollectionSchema Schema; string ClassCode; IReadOnlyList<string> RootMemberNames; IReadOnlyList<string> Diagnostics; DateTime FetchedAtUtc; }`.
- `internal static bool SheetXSheetJsonSource.TryLoadExcel(SheetXSettings settings, IWorkbook workbook, IReadOnlyList<SheetPath> sheets, string sourceId, string sheetName, out SheetXSheetPreviewData data, out string error)`.
- `internal static bool SheetXSheetJsonSource.TryLoad(SheetXSettings settings, SheetXSheetSource source, string sheetName, out SheetXSheetPreviewData data, out string error)`; the Excel branch opens the workbook from `source.Id`.
- `internal bool ExcelSheetHandler.TryPreviewSheet(IWorkbook workbook, IReadOnlyList<SheetPath> sheets, string sheetName, SheetXSheetOutputMode mode, out string json, out SheetXCollectionSchema schema, out IReadOnlyList<string> warnings, out string error)`.

Rules:

- Build the handler on a detached `SheetXExportContext` whose output ignores writes (`discardStagedOnError: true`). Any context error is a preview failure, even when the JSON parses; warnings become diagnostics.
- Reset ID caches, load every listed `*IDs` sheet present in the workbook, then convert with `pEncrypt: false, pWriteFile: false`. Missing sheet: `Sheet '<name>' was not found in the workbook.` A `null` conversion result: invalid-JSON error. `"{}"`: `Sheet '<name>' has no header row.` `"[]"` flows into the draft path and yields the empty-data error.
- Determine the output mode by a read-only lookup in `settings.sheetBindings`; no `GetOrCreateBinding`, no assignment to `settings.excelSheetsPath`, no `SaveToDisk`.
- For an Existing Data Class sheet with a resolvable row type, also run the export's array deserialization (`JsonConvert.DeserializeObject(json, rowType.MakeArrayType())`); a failure is reported as a conversion diagnostic, never as success.
- Legacy modes emit the Task 1 draft with `settings.ResolveCollectionNamespace()`; an invalid namespace becomes a preview error. Generated mode uses `TryParseGeneratedSchema` plus `EmitRowTypes`.

- [ ] **Step 1: Tests on an in-memory `XSSFWorkbook`.** Legacy preview unions later rows and resolves `HERO_1` through an unchecked `HeroIDs` sheet; legacy key `bot_ids` is kept verbatim; Generated preview emits schema code (`botIds`) and no JSON or paths; a header-only sheet yields the empty-data error; missing header and missing sheet are errors; a converter error rejects the snapshot even though the JSON parses (drive `Blocking` with a duplicated ID); preview writes nothing, creates no binding, alters no `selected`, and leaves `settings.excelSheetsPath` null; a missing file is an error through `TryLoad`.
- [ ] **Step 2: Behavioral red** with filter `RCore.SheetX.Tests.SheetPreviewSourceTests`.
- [ ] **Step 3: Implement**, keeping ID loading identical to `ExportOrdinaryJson`.
- [ ] **Step 4: Focused green, then the full `RCore.SheetX.Tests` run**; the batch tests must be unaffected.
- [ ] **Step 5: Record; do not commit.**

### Task 5: Google preview acquisition through an injected fetcher

**Files:** Modify `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs` and `Editor/Preview/SheetXSheetJsonSource.cs`; extend `Tests/SheetPreviewSourceTests.cs`; change `Tests/RCore.SheetX.Tests.asmdef` only to reference already-installed Google Sheets DTO DLLs.

**Interfaces:**

- `internal bool GoogleSheetHandler.TryPreviewSheet(Spreadsheet metadata, Func<string, IList<IList<object>>> fetchRange, string spreadsheetId, IReadOnlyList<SheetPath> sheets, string sheetName, SheetXSheetOutputMode mode, out string json, out SheetXCollectionSchema schema, out IReadOnlyList<string> warnings, out string error)`.
- Seam `internal static Func<SheetXSettings, string, (Spreadsheet metadata, Func<string, IList<IList<object>>> fetchRange, IDisposable service)> SheetXSheetJsonSource.GoogleConnector`, defaulting to real authentication through `SheetXHelper.AuthenticateGoogleUser` and a new `SheetsService`. Tests replace it with in-memory data; NUnit never touches the network.

Rules:

- Google preview loads only selected `*IDs` sheets, matching `GetSheetIDsValues`.
- Use the export range formula (`A1:` + column letter); a missing column count is an error, not an exception.
- Preview never calls `GetCacheMetadata`, `ValidateSheetPaths`, `ResolveSheets`, or `DownloadGoogleSheet`.
- Missing credentials: `Google Client ID or Client Secret is missing.` A caught fetch exception: `Could not read Google spreadsheet '<id>': <message>`.
- Dispose the created service in `finally`.

- [ ] **Step 1: Tests** with constructed `Spreadsheet`/`Sheet` DTOs and a dictionary-backed fetcher: selected IDs load, unselected IDs are ignored, missing sheet is an error, a fetcher exception is an error, no metadata or selection mutation, and the missing-credential path through `TryLoad`.
- [ ] **Step 2: Behavioral red.**
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Focused green; full suite.**
- [ ] **Step 5: Record. Live Google authentication stays a manual-matrix item.**

### Task 6: Export confirmation before mutation

**Files:** Modify `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs`, `ExcelSheetHandler.cs`, `GoogleSheetHandler.cs`; extend `Tests/CollectionGenerationTests.cs`.

**Interfaces:**

- `internal static Func<string, bool> ConfirmMemberMismatch`, defaulting to `EditorUtility.DisplayDialog("Existing Data Class members", message, "Export Anyway", "Cancel")`.
- `internal static Func<bool> IsHeadless`, defaulting to `() => Application.isBatchMode`. Tests override it to `false` for dialog tests and `true` for headless tests, because Unity's headless runner is itself batch mode.
- `internal bool SuppressDialogs`, set from `m_writer.Detached` in both `CreateCollectionSession` methods.
- The private `Candidate` stores the resolved `Type RowType` for Existing Data Class candidates.

Rules:

- Only admitted Existing Data Class candidates are checked, after the final successful `EmitFiles` and before `SourcesChanged`, depth-change confirmation, migration capture, snapshot capture, and staged writes.
- One aggregated message per `Flush`, listing source, sheet, class name, and unmatched members; every line also goes through `m_warn`.
- `SuppressDialogs || IsHeadless()` warns and continues. Otherwise `ConfirmMemberMismatch(message)`; `false` returns an error starting `Collection export cancelled` with `WroteArtifacts` and `FlushSucceeded` both false.
- The existing deserialization failure still skips the sheet before any member check; `Export Anyway` writes byte-identical JSON.

- [ ] **Step 1: Tests** (override both seams in `try/finally`): two mismatched sheets ask once and cancel writes nothing; export anyway writes unchanged JSON; matched or merely absent members do not ask; headless warns and continues; conversion failure skips before asking; member confirmation precedes depth-change confirmation (order test recording both callbacks).
- [ ] **Step 2: Behavioral red**, plus the placement proof: with the check temporarily moved after `context.Flush()`, the cancel test must fail on files existing; restore the placement.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: `CollectionGenerationTests` green; full suite.**
- [ ] **Step 5: Record the red-proof outcome; do not commit.**

### Task 7: Preview window and Structure column

**Files:** Create `Editor/Preview/SheetXSheetStructureWindow.cs`; modify `Assets/RCore.SheetX/Editor/SheetXHelper.cs`, `EditExcelSheetsWindow.cs`, `EditGoogleSheetsWindow.cs`, `ExcelSheetXWindow.cs`, `GoogleSheetXWindow.cs`; create `Tests/SheetStructureWindowTests.cs`.

**Interfaces:**

- Internal `SheetXStructurePreviewState` with `Apply(bool ok, SheetXSheetPreviewData data, string error)`, `Revalidate(Type rowType)`, `Reset()`, `Snapshot`, `Error`, `Stale`, `Match`, `CanCopy`.
- `internal static void SheetXSheetStructureWindow.Open(SheetXSettings settings, SheetXSheetSource source, string sheetName)`.
- Keep the public `SheetXHelper.CreateSpreadsheetTable` signature unchanged. Add `internal static void SheetXHelper.AddStructureColumn(EditorTableView<SheetPath> table, SheetXSettings settings, Func<SheetXSheetSource> source)`, called by the four hosts right after table creation. The column exists regardless of `enableCollections` and skips IDs, Constants, Settings, and Localization sheets (`!SheetXHelper.IsJsonSheet`).
- **Configuration is skipped by exact name, NOT by `IsAutomaticConfiguration`.** That helper requires `enableCollections` (`SheetXCollectionSettings.cs:62-67`), but the interactive windows build their handler with a null context, so `ConfigurationRouteEnabled` is `!Detached` = true regardless (`ExcelSheetHandler.cs:47`), and `ExportOrdinaryJson` routes an exact `Configuration` sheet to the typed config exporter on that flag alone (`:1141-1142`). With Collections off, an `IsAutomaticConfiguration` filter would leave the button visible on a sheet whose preview shows a legacy row array while export emits a typed config class — a preview that lies. Skip when `string.Equals(item.name, SheetXConstants.CONFIGURATION_SHEET, StringComparison.Ordinal)`. A test must cover Collections-off + `Configuration`.

Rules:

- Button tooltip: `Preview this sheet's C# structure without exporting files.`
- Window title `Sheet Structure`; header shows source, sheet, Output Mode, and the selected Data Class; toolbar has `Refresh` and `Copy Code` (disabled without valid code).
- Labels: `Inferred draft from exported JSON` for legacy modes, `Generated class preview` for Generated. Legacy help text says blank or omitted fields are absent, inferred types are suggestions, and IDs are source-local.
- Existing Data Class block: `Unmatched JSON member`, `Not present in exported data`, `No unmatched top-level JSON members`, `Cannot infer structure from empty exported data`, `Member coverage cannot be verified`; always `Top-level member check only; nested members are not compared.` A deserialization failure shows the conversion diagnostic, never a success line.
- Loading happens only from `Open` and `Refresh`, through `EditorApplication.delayCall` so `Loading…` paints first; repaint never fetches. A failure after a success keeps the old code, labelled `Stale — last refresh failed`. A mode change requires Refresh; a Data Class change revalidates the same snapshot. No `[SerializeField]` state survives a domain reload; a window without a source closes itself.

- [ ] **Step 1: State tests**: success clears the error and enables copy; a failed refresh keeps a stale snapshot; revalidate reuses the snapshot; revalidate with null clears the match; reset clears everything.
- [ ] **Step 2: Behavioral red.**
- [ ] **Step 3: Implement the window, the column helper, and the four host calls.**
- [ ] **Step 4: Compile, focused green, full suite.**
- [ ] **Step 5: Record; manual UI still pending.**

### Task 8: Verification, draft compile check, manual matrix

- [ ] Add a draft compile check if a C# compiler is reachable from the editor runtime (`Microsoft.CSharp.CSharpCodeProvider`); otherwise validate every emitted identifier with `SheetXCollectionNaming.IsValidIdentifier` and round-trip each `[JsonProperty]` literal through `JsonConvert.DeserializeObject<string>`. Record which path ran.
- [ ] Run the full `RCore.SheetX.Tests` filter, `dotnet build`, and `git diff --check`; compare counts against the fresh baseline.
- [ ] Manual matrix: Excel and Google; JSON Only, Generated, Existing; an unchecked sheet; Collections disabled; loading, error, and refresh states; Copy Code; the informative missing-data result; Export Anyway and Cancel; live Google authentication; the source-local IDs warning text.
- [ ] Record outcomes truthfully, including skipped and failed cases.

### Task 9: Documentation and changelogs

- [ ] Add `### 7.5. Structure preview and Data Class check` to `Assets/RCore.SheetX/Document/Document.md` and its Vietnamese counterpart in `Document_VN.md`, before section 8 in both. Cover: button location, modes and labels, inferred-draft limits, snapshot freshness and Refresh, top-level-only check, export confirmation semantics, headless warning behavior, the source-local ID limitation, and the cancellation scope (`Collection export cancelled`, no whole-export rollback).
- [ ] Add `[Unreleased]` entries to `Assets/RCore.SheetX/CHANGELOG.md` and the root `CHANGELOG.md`; no version bump.
- [ ] Do not commit or push.

## Plan self-review

- Spec coverage: button in all modes (Task 7); shared window for Excel and Google (Tasks 4, 5, 7); code display and copy (Task 7); validator in preview and export (Tasks 2, 6, 7); confirmation dialog and batch suppression (Task 6); window-local snapshot without a cache (Task 7); fresh export JSON (Task 6); docs (Task 9).
- Placeholders: none. Task 8 names its fallback when no compiler API is available.
- Type consistency: `SheetXSheetSource`, `SheetXSheetPreviewData`, `SheetXRowTypeMatch`, `SheetXStructurePreviewState`, `TryPreviewSheet`, `TryParseGeneratedSchema`, `EmitRowTypes`, `ConfirmMemberMismatch`, `IsHeadless`, `SuppressDialogs`, `AddStructureColumn` keep the same names in every task.
- Limitations carried forward: top-level-only validation; preview IDs are source-local; live Google auth is manual-only; inferred drafts are suggestions.
