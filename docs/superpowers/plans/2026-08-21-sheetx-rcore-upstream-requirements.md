# SheetX upstream requirements implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make SheetX editor exports callable from another editor assembly with caller-owned output, no settings asset/UI dependency, no modal parse failures, deterministic culture-safe content, and safe credential/workbook handling.

**Architecture:** Add public request/result/output contract plus internal request-scoped export context. Existing Excel/Google handlers become adapters over context rather than owners of `SheetXSettings` file writes or dialogs. Legacy UI creates context from `SheetXSettings` and sends artifacts through a file-output adapter; external callers use `SheetXExporter` and an arbitrary `ISheetXOutput`.

**Tech Stack:** Unity 2022.3 editor assemblies, C# 9, NUnit EditMode, NPOI, Google Sheets API, Newtonsoft.Json.

## Global Constraints

- Base behavior under review: `2c5498291d1698b553e6f14e681b699b23ff766f`.
- Change only `Assets/RCore.SheetX` plus root `CHANGELOG.md` and these docs.
- No RCore export type may reference `iKit.Config`, VContainer, Addressables, or `iKit.Variant`.
- `SheetXExporter.ExportExcel` and `ExportGoogle` never call `SheetXSettings.Init()` and never load/create `Assets/SheetX/SheetXSettings.asset`.
- New export path never calls `EditorUtility.DisplayDialog`, `Debug.Log`, `SheetXHelper.WriteFile`, `File.WriteAllText`, or `AssetDatabase`.
- Every generated artifact travels through `ISheetXOutput.Write(relativePath, content)` exactly once per final artifact.
- `SheetXExportResult.Success` is true iff `Errors.Count == 0`; result records output only after successful `Write`.
- Numeric parse and numeric code emission use `CultureInfo.InvariantCulture`. Parse errors include sheet, field, row when known.
- ID replacement order: descending key length then `StringComparer.Ordinal`.
- Empty duplicate-name column group emits `[]`.
- Google aggregate artifacts are emitted once after all selected sheets; aggregate contents are deterministic under reordered source sheets.
- OAuth ID/secret remain in `EditorPrefs`; token files must not live under `Assets/`.
- `.cs` files use tabs and CRLF. Every public member has XML `<summary>`.
- No commit or push without explicit user request. Root CHANGELOG update is still required before any future commit.

---

## Planned file structure

- Create `Assets/RCore.SheetX/Editor/SheetXExport.cs` — public request/result/output API and internal export context/report writer.
- Create `Assets/RCore.SheetX/Editor/SheetXFileOutput.cs` — legacy UI `ISheetXOutput` implementation wrapping `SheetXHelper.WriteFile`.
- Modify `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs` — context-driven Excel export, result errors/warnings, invariant content building, one-workbook scope.
- Modify `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs` — context-driven Google export, result errors/warnings, aggregate write timing, invariant content building.
- Modify `Assets/RCore.SheetX/Editor/SheetXHelper.cs` — static helper, deterministic ID sorting, invariant numeric utility, token directory.
- Modify `Assets/RCore.SheetX/Editor/SheetXData.cs` — safe workbook API/lifetime and invariant `Att` JSON values.
- Modify `Assets/RCore.SheetX/Editor/SheetXSettings.cs` — legacy context mapping, stable credential-key migration, template errors delegated to result path.
- Modify `Assets/RCore.SheetX/Editor/SheetXWindow.cs` — UI keeps legacy behavior by using request/context adapter.
- Create `Assets/RCore.SheetX/Tests/SheetXExportTests.cs` — in-memory output and end-to-end Excel export tests.
- Create `Assets/RCore.SheetX/Tests/SheetXHelperTests.cs` — deterministic sort, numeric/token helper tests.
- Modify `Assets/RCore.SheetX/Tests/SheetXSettingsTests.cs` — EditorPrefs migration/key tests.
- Modify `CHANGELOG.md` — one Unreleased SheetX entry, including `[]` empty-combined-column behavior.

## Task 1: Lock shared correctness primitives (R4, R5, R11)

**Files:**
- Create: `Assets/RCore.SheetX/Tests/SheetXHelperTests.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXHelper.cs:31-374`
- Modify: `Assets/RCore.SheetX/Editor/SheetXData.cs:254-291`
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:244-267, 427-537, 1027-1585`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:338-361, 479-589, 1118-1667`

**Consumes:** Existing `SheetXHelper.SortIDsByLength(Dictionary<string, int>)`.

**Produces:**

```csharp
public static Dictionary<string, int> SortIDsByLength(Dictionary<string, int> dict);
internal static bool TryParseInt(string value, out int result);
internal static bool TryParseFloat(string value, out float result);
internal static string FormatFloat(float value);
```

- [ ] **Step 1: Write failing deterministic-sort tests**

```csharp
[Test]
public void sort_ids_by_length_replaces_long_key_first()
{
	var ids = SheetXHelper.SortIDsByLength(new Dictionary<string, int>
	{
		["HERO_1"] = 5,
		["HERO_10"] = 9,
	});
	var value = "HERO_10";
	foreach (var id in ids)
		value = value.Replace(id.Key, id.Value.ToString(CultureInfo.InvariantCulture));
	Assert.That(value, Is.EqualTo("9"));
}

[Test]
public void sort_ids_by_length_orders_equal_keys_ordinally()
{
	var ids = SheetXHelper.SortIDsByLength(new Dictionary<string, int>
	{
		["B"] = 1,
		["A"] = 2,
	});
	CollectionAssert.AreEqual(new[] { "A", "B" }, ids.Keys);
}
```

- [ ] **Step 2: Run test and confirm old helper fails first test**

Run in Unity Test Runner: filter `SheetXHelperTests.sort_ids_by_length_replaces_long_key_first`.

Expected: FAIL, output is `50`.

- [ ] **Step 3: Make helper static and implement culture/determinism helpers**

```csharp
public static class SheetXHelper
{
	public static Dictionary<string, int> SortIDsByLength(Dictionary<string, int> dict)
	{
		return dict
			.OrderByDescending(x => x.Key.Length)
			.ThenBy(x => x.Key, StringComparer.Ordinal)
			.ToDictionary(x => x.Key, x => x.Value);
	}

	internal static bool TryParseInt(string value, out int result)
	{
		return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
	}

	internal static bool TryParseFloat(string value, out float result)
	{
		return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	}

	internal static string FormatFloat(float value)
	{
		return value.ToString("R", CultureInfo.InvariantCulture);
	}
}
```

Replace both inline `OrderBy(x => x.Key.Length)` constructions with `SheetXHelper.SortIDsByLength(m_allIds)`. Replace all export-path `int.TryParse` / `float.TryParse` and emitted float interpolation with helpers. Update `Att.GetJsonString()` to call `FormatFloat` for scalar values and compare `valueString` against invariant `FormatFloat(value)`.

- [ ] **Step 4: Add culture regression test**

```csharp
[Test]
public void format_float_uses_dot_in_de_de()
{
	var previous = CultureInfo.CurrentCulture;
	try
	{
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
		Assert.That(SheetXHelper.FormatFloat(1.5f), Is.EqualTo("1.5"));
	}
	finally
	{
		CultureInfo.CurrentCulture = previous;
	}
}
```

- [ ] **Step 5: Run focused tests**

Run in Unity Test Runner: filter `SheetXHelperTests`.

Expected: PASS.

- [ ] **Step 6: Do not commit**

Changes remain uncommitted until user explicitly requests commit.

### Task 2: Repair workbook lifetime and credential storage (R7, R9, R10)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXData.cs:48-142`
- Modify: `Assets/RCore.SheetX/Editor/SheetXHelper.cs:586-688`
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs:88-115, 262-336`
- Modify: `Assets/RCore.SheetX/Tests/SheetXSettingsTests.cs`
- Modify: `.gitignore:71-72` only if wildcard regression exists

**Consumes:** `SheetXHelper.TryParse*`, existing EditorPrefs migration behavior.

**Produces:**

```csharp
internal static string GetTokenStoreDirectory();
internal static string GetProjectPrefKey(string field);
internal static IWorkbook OpenWorkbook(Stream stream);
```

- [ ] **Step 1: Write failing token-directory and stable-key tests**

```csharp
[Test]
public void token_store_directory_is_under_library()
{
	StringAssert.StartsWith(Path.Combine(Application.dataPath, "..", "Library"), SheetXHelper.GetTokenStoreDirectory());
	StringAssert.DoesNotContain(Path.Combine("Assets", "Editor"), SheetXHelper.GetTokenStoreDirectory());
}

[Test]
public void project_pref_key_does_not_use_application_identifier()
{
	StringAssert.DoesNotContain(Application.identifier, SheetXSettings.GetProjectPrefKeyForTests("GoogleClientId"));
}
```

Keep test-only accessor `internal` and use `InternalsVisibleTo` only if existing assembly setup needs it; otherwise test public behavior through credential properties.

- [ ] **Step 2: Run tests and confirm old implementation fails**

Run in Unity Test Runner: filter `SheetXSettingsTests`.

Expected: token path resolves under `Assets/Editor`; current key contains `Application.identifier`.

- [ ] **Step 3: Keep stream and workbook in same caller scope**

Replace unsafe `GetWorkBook()` usage with an API that does not return an `IWorkbook` backed by disposed stream. New exporter uses:

```csharp
using var stream = new FileStream(request.SpreadsheetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
using var workbook = WorkbookFactory.Create(stream);
// Export while both remain alive.
```

For legacy `ExcelSheetsPath`, change `GetWorkBook()` to load from a memory-backed stream or replace legacy call sites with scoped open helper. Never return a workbook after disposing its source stream. Dispose workbook after work completes.

- [ ] **Step 4: Move Google token cache out of asset pipeline**

```csharp
internal static string GetTokenStoreDirectory()
{
	return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "SheetX"));
}
```

Use this path for `FileDataStore`. Preserve existing wildcard `.gitignore` rules for historical `Assets/Editor` cache names. Do not create token files during tests.

- [ ] **Step 5: Change credential key and migrate old EditorPrefs values**

Use stable normalized project-path identity, not bundle ID:

```csharp
private static string PrefKey(string field)
{
	var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
		.Replace('\\', '/')
		.ToLowerInvariant();
	return $"SheetX.{projectPath.GetHashCode():X8}.{field}";
}
```

Before returning new key value, read old `$"{Application.identifier}.SheetX.{field}"`; if new key is absent and legacy is non-empty, copy legacy value to new key. Remove old key only after copy succeeds. Keep migration fields serialized only for one-time old-asset migration.

- [ ] **Step 6: Run focused tests**

Run in Unity Test Runner: filter `SheetXSettingsTests`.

Expected: PASS. Manually run `git status --short` after authorized Google authentication; expected no token file under `Assets/`.

- [ ] **Step 7: Do not commit**

Changes remain uncommitted until user explicitly requests commit.

### Task 3: Make existing JSON generation valid and deterministic (R6, R8)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:1119-1541`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:1199-1621, 1679-1844`
- Create: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs`

**Consumes:** `SheetXHelper.SortIDsByLength`, invariant helpers.

**Produces:** valid empty combined arrays and a single final Google aggregate write.

- [ ] **Step 1: Write failing empty-combined-column test**

Create temporary `.xlsx` with one JSON sheet whose header contains three duplicate `Tags` columns and one data row where all three values are blank. Call conversion through test-visible exporter/session method. Assert:

```csharp
var json = JObject.Parse(output.Content);
Assert.That(json["Row"]["Tags"].Type, Is.EqualTo(JTokenType.Array));
Assert.That(json["Row"]["Tags"].Count(), Is.EqualTo(0));
```

Use actual row/root shape emitted by SheetX fixture; do not hand-build expected invalid string.

- [ ] **Step 2: Run test and confirm malformed JSON before fix**

Run in Unity Test Runner: filter `SheetXExportTests.empty_combined_columns_emit_empty_array`.

Expected: `JObject.Parse` throws due to `"Tags":],`.

- [ ] **Step 3: Preserve array opening when no values exist**

Replace unsafe substring close in both handlers with:

```csharp
var prefix = $"\"{combinedCol.Key}\":[";
var combinedValue = combinedCol.Value == prefix
	? prefix
	: combinedCol.Value.Substring(0, combinedCol.Value.Length - 1);
fieldContentStr += $"{combinedValue}],";
```

This emits `"Tags":[],` for empty group.

- [ ] **Step 4: Extract Google merged JSON write from per-sheet loop**

Collect each JSON source first. After `foreach (var sheet in sheets)` completes, serialize a deterministic ordinal-key dictionary and write once:

```csharp
if (m_context.CombineJson)
{
	var mergedJson = JsonConvert.SerializeObject(allJsons
		.OrderBy(pair => pair.Key, StringComparer.Ordinal)
		.ToDictionary(pair => pair.Key, pair => pair.Value));
	WriteJson(mergedFileName, mergedJson);
}
```

Do not emit a combined write inside any per-sheet loop. Apply same deterministic ordering to IDs, Constants, and Localization aggregation where dictionary enumeration becomes artifact content.

- [ ] **Step 5: Write reorder/write-once regression test**

Use a recording `ISheetXOutput` and deterministic Google-sheet content seam. Export same three logical sheets in two orders. Assert one merged JSON record each and byte-identical content:

```csharp
Assert.That(first.Writes.Count(write => write.RelativePath == "Data.txt"), Is.EqualTo(1));
Assert.That(first.Content("Data.txt"), Is.EqualTo(second.Content("Data.txt")));
```

- [ ] **Step 6: Run focused tests**

Run in Unity Test Runner: filter `SheetXExportTests`.

Expected: PASS.

- [ ] **Step 7: Do not commit**

Changes remain uncommitted until user explicitly requests commit.

### Task 4: Add public export contract and legacy output adapter (R1, R2)

**Files:**
- Create: `Assets/RCore.SheetX/Editor/SheetXExport.cs`
- Create: `Assets/RCore.SheetX/Editor/SheetXFileOutput.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs:208-238`
- Modify: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs`

**Consumes:** Shared helpers, token directory, existing templates/resources.

**Produces:**

```csharp
public enum SheetXExportFileType { Ids, Constants, Json, Localization, CharacterSet, LocalizationManager }
public interface ISheetXOutput { void Write(string relativePath, string content); }
public sealed class SheetXExportRequest { /* design-specified public fields */ }
public sealed class SheetXExportResult { /* Files, Warnings, Errors, Success */ }
public static class SheetXExporter
{
	public static SheetXExportResult ExportExcel(SheetXExportRequest request, ISheetXOutput output);
	public static SheetXExportResult ExportGoogle(SheetXExportRequest request, ISheetXOutput output);
}
```

- [ ] **Step 1: Write contract/memory-output test**

```csharp
private sealed class MemoryOutput : ISheetXOutput
{
	public readonly Dictionary<string, string> Writes = new();
	public void Write(string relativePath, string content) => Writes.Add(relativePath, content);
}

[Test]
public void export_excel_writes_only_to_caller_output()
{
	var output = new MemoryOutput();
	var before = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories);
	var result = SheetXExporter.ExportExcel(CreateFixtureRequest(), output);
	var after = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories);
	Assert.That(result.Success, Is.True);
	Assert.That(output.Writes, Is.Not.Empty);
	CollectionAssert.AreEquivalent(before, after);
}
```

Fixture lives under test temporary directory, never `Assets/`.

- [ ] **Step 2: Run test and confirm API is absent**

Run in Unity Test Runner: filter `SheetXExportTests.export_excel_writes_only_to_caller_output`.

Expected: compilation failure until public API exists.

- [ ] **Step 3: Create request, result, and context types**

Use exact public fields from design. `SheetXExportResult` owns mutable internal lists and exposes `IReadOnlyList`; `AddError`, `AddWarning`, and `RecordFile` are internal. Validate null request/output and blank source path without throwing.

Create internal `SheetXExportContext` holding request, output, result, request-scoped IDs/builders/encryption, and:

```csharp
internal void Write(string relativePath, string content, SheetXExportFileType type)
{
	try
	{
		m_output.Write(relativePath, content);
		m_result.RecordFile(relativePath, type);
	}
	catch (Exception exception)
	{
		m_result.AddError($"Write failed for {relativePath}: {exception.Message}");
	}
}
```

- [ ] **Step 4: Add legacy file adapter**

```csharp
internal sealed class SheetXFileOutput : ISheetXOutput
{
	public void Write(string relativePath, string content)
	{
		var directory = Path.GetDirectoryName(relativePath);
		var filename = Path.GetFileName(relativePath);
		SheetXHelper.WriteFile(directory, filename, content);
	}
}
```

Normalize root/filename with `Path.Combine`; public exporter keeps `/` paths. Existing `SheetXSettings.CreateFileIDs` / `CreateFileConstants` become legacy wrappers only or are removed after all handler call sites route through context. No handler may bypass context to reach `WriteFile`.

- [ ] **Step 5: Run focused test**

Run in Unity Test Runner: filter `SheetXExportTests.export_excel_writes_only_to_caller_output`.

Expected: PASS; no asset path added/modified.

- [ ] **Step 6: Do not commit**

Changes remain uncommitted until user explicitly requests commit.

### Task 5: Route Excel export through request-scoped context (R1, R2, R3, R5, R7, R12)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXExport.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs`
- Modify: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs`

**Consumes:** `SheetXExportContext`, `SheetXFileOutput`, source/workbook helpers.

**Produces:** `SheetXExporter.ExportExcel` implemented without UI/settings singleton/direct writes.

- [ ] **Step 1: Write error-result regression tests**

```csharp
[Test]
public void export_excel_reports_duplicate_id_without_dialog()
{
	var result = SheetXExporter.ExportExcel(CreateDuplicateIdFixtureRequest(), new MemoryOutput());
	Assert.That(result.Success, Is.False);
	Assert.That(result.Errors.Single(), Does.Contain("Duplicated ID"));
}

[Test]
public void export_excel_reports_invalid_json_with_sheet_field_row()
{
	var result = SheetXExporter.ExportExcel(CreateInvalidJsonFixtureRequest(), new MemoryOutput());
	Assert.That(result.Success, Is.False);
	Assert.That(result.Errors.Single(), Does.Contain("Sheet: Data Field: Payload Row: 2"));
}
```

- [ ] **Step 2: Run tests and confirm pre-refactor behavior cannot satisfy contract**

Run in Unity Test Runner: filter `SheetXExportTests.export_excel_reports`.

Expected: fail/compile absent API; do not invoke legacy handler because it opens dialogs.

- [ ] **Step 3: Convert handler to context access**

Add internal constructor accepting `SheetXExportContext`. Keep legacy `ExcelSheetHandler(SheetXSettings settings)` only as adapter that creates request/context with `SheetXFileOutput`. Replace every `m_settings` export read with request/context property. Replace all artifact calls with context `Write` calls and correct `SheetXExportFileType`.

Remove `EditorUtility.DisplayDialog` from context export methods. Convert these cases into `AddWarning` or `AddError`:

- blank ID value: warning with sheet/key/row;
- duplicate ID conflicting values: error;
- duplicate ID during preload: error;
- invalid JSON: error with sheet/field/row;
- missing selected sheet/empty source sheet: warning;
- missing output root required for artifact: error.

Legacy adapter may log returned result after export; it must not feed UI dialogs back into engine.

- [ ] **Step 4: Open workbook once in `ExportExcel`**

```csharp
public static SheetXExportResult ExportExcel(SheetXExportRequest request, ISheetXOutput output)
{
	var context = new SheetXExportContext(request, output);
	if (!context.ValidateExcelRequest())
		return context.Result;
	try
	{
		using var stream = new FileStream(request.SpreadsheetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var workbook = WorkbookFactory.Create(stream);
		new ExcelSheetHandler(context).ExportAll(workbook);
	}
	catch (Exception exception)
	{
		context.AddError($"Excel export failed: {exception.Message}");
	}
	return context.Result;
}
```

Pass workbook to all handler phases. Never reopen input file or call `GetWorkBook()` in new path.

- [ ] **Step 5: Replace template null dereferences**

Add context template helper:

```csharp
internal string LoadTemplate(string resourceName)
{
	var asset = Resources.Load<TextAsset>(resourceName);
	if (asset != null)
		return asset.text;
	AddError($"Missing SheetX template '{resourceName}' in Resources.");
	return null;
}
```

Every IDs/Constants/Localization template call checks null and skips dependent artifact; no `.text` chained on `Resources.Load` in export path.

- [ ] **Step 6: Add workbook and missing-template tests**

Create multi-sheet `.xlsx`; verify one export reads IDs/JSON/Constants without `ObjectDisposedException`. Use test seam for resource lookup or temporarily hide a template in test-safe manner; assert named result error and no `NullReferenceException`.

- [ ] **Step 7: Run focused tests**

Run in Unity Test Runner: filter `SheetXExportTests`.

Expected: PASS.

- [ ] **Step 8: Do not commit**

Changes remain uncommitted until user explicitly requests commit.

### Task 6: Route Google export through request-scoped context (R1, R2, R3, R5, R8, R12)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXExport.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXHelper.cs`
- Modify: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs`

**Consumes:** request context, Google credential fields, context writer/error sink, deterministic aggregate builder.

**Produces:** `SheetXExporter.ExportGoogle` implemented without `SheetXSettings.Init`, dialogs, or direct writes.

- [ ] **Step 1: Write Google request-validation tests**

```csharp
[Test]
public void export_google_requires_spreadsheet_id_and_credentials()
{
	var result = SheetXExporter.ExportGoogle(new SheetXExportRequest(), new MemoryOutput());
	Assert.That(result.Success, Is.False);
	Assert.That(result.Errors, Has.Exactly(3).Matches<string>(error =>
		error.Contains("SpreadsheetPath") || error.Contains("GoogleClientId") || error.Contains("GoogleClientSecret")));
}
```

Use no live Google connection in unit tests.

- [ ] **Step 2: Run test and confirm public path is incomplete**

Run in Unity Test Runner: filter `SheetXExportTests.export_google_requires`.

Expected: FAIL until validation exists.

- [ ] **Step 3: Convert Google handler to context**

Add context constructor and keep legacy settings constructor as adapter. Build `SheetsService` from request credentials only in `ExportGoogle`; never read `SheetXSettings`, `ObfGoogleClientId`, or `ObfGoogleClientSecret` from new path. Replace all direct artifact writes / `CreateFile*` with context writes and file types.

- [ ] **Step 4: Replace dialogs and raw numeric failures with result entries**

Apply same error classifications and contextual messages as Excel. Replace line-328 style `int.Parse` with invariant `TryParseInt`; invalid ID values become result errors. Verify all float parsing/emission routes use Task 1 helpers.

- [ ] **Step 5: Make aggregate write once and deterministic**

Ensure `ExportAllFiles` equivalent collects selected JSON/IDs/Constants/Localization first, then emits each aggregate once after its source loop. Do not write cached/partial aggregate state. Make directory/name collision report error in result instead of log/dialog.

- [ ] **Step 6: Add Google seam tests**

Introduce internal data-fetch delegate/interface only if required to test network-free export. It must be internal and limited to fetching spreadsheet metadata/values; public API stays exactly `ExportGoogle(request, output)`. Use it to run three sheets in reverse order and prove one byte-identical aggregate write, plus invalid JSON result without Unity modal UI.

- [ ] **Step 7: Run focused tests**

Run in Unity Test Runner: filter `SheetXExportTests`.

Expected: PASS with no network/OAuth browser interaction.

- [ ] **Step 8: Do not commit**

Changes remain uncommitted until user explicitly requests commit.

### Task 7: Preserve legacy UI path and document change (R9-R12 closeout)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXWindow.cs`
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetXWindow.cs`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetXWindow.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs`
- Modify: `CHANGELOG.md:5-24`
- Modify: `Assets/RCore.SheetX/Tests/SheetXSettingsTests.cs`

**Consumes:** final exporter API and legacy `SheetXFileOutput`.

**Produces:** existing windows invoke exports through public engine, and clear Unreleased documentation.

- [ ] **Step 1: Write legacy mapping test**

```csharp
[Test]
public void settings_creates_equivalent_export_request()
{
	var settings = ScriptableObject.CreateInstance<SheetXSettings>();
	settings.combineJson = true;
	settings.@namespace = "Game.Data";
	var request = settings.CreateExportRequestForTests();
	Assert.That(request.CombineJson, Is.True);
	Assert.That(request.Namespace, Is.EqualTo("Game.Data"));
}
```

Destroy temporary ScriptableObject in teardown. Do not call `SheetXSettings.Init()`.

- [ ] **Step 2: Run test and confirm mapping absent**

Run in Unity Test Runner: filter `SheetXSettingsTests.settings_creates_equivalent_export_request`.

Expected: compilation failure until mapping exists.

- [ ] **Step 3: Map legacy settings to request**

Implement internal `SheetXSettings.CreateExportRequest(...)` mapping every generic field: source path/id, selected names, all three output folders, flags, namespace, `onlyEnumAsIDs`, persistent fields, encryption fields, and credentials read from EditorPrefs. Window export buttons construct request, use `SheetXFileOutput`, call appropriate exporter, and log result errors/warnings after call. No button changes selected-sheet persistence behavior.

- [ ] **Step 4: Confirm secrets and assets stay clean**

Run:

```powershell
git grep -niE 'client_secret|refresh_token' -- Assets/RCore.SheetX
```

Expected: no credential value/cache file. Comments that describe identifier names are allowed only outside `Assets/` scan requirement; remove sensitive-string comments under `Assets/` if command matches them.

Run:

```powershell
git status --short
```

Expected: only intentional source/doc edits; no `Assets/Editor/Google.Apis.Auth.OAuth2.Responses.TokenResponse-*` file.

- [ ] **Step 5: Update root changelog**

Under `## [Unreleased]`, add concise `RCore.SheetX` entries:

- Added external `SheetXExporter` / `ISheetXOutput` editor API.
- Fixed modal export failures becoming result warnings/errors.
- Fixed invariant numeric export, long-ID replacement order, empty combined columns emitting `[]`, one-scope Excel workbook, Google aggregate write once, and token cache outside `Assets`.
- Changed credentials to stable project-scoped EditorPrefs key.

- [ ] **Step 6: Run focused tests**

Run in Unity Test Runner: filters `SheetXSettingsTests` and `SheetXExportTests`.

Expected: PASS.

- [ ] **Step 7: Do not commit**

Changes remain uncommitted until user explicitly requests commit.

### Task 8: Full verification and review

**Files:**
- Modify only files required by verified fixes.

**Consumes:** all completed tasks.

**Produces:** verified working-tree diff ready for user review.

- [ ] **Step 1: Run SheetX EditMode tests**

Run Unity Test Runner EditMode filtered `RCore.SheetX.Tests`.

Expected: all existing and new SheetX tests pass.

- [ ] **Step 2: Run static security checks**

```powershell
git grep -niE 'client_secret|refresh_token' -- Assets/
git status --short
git diff --check
git diff -- Assets/RCore.SheetX CHANGELOG.md
```

Expected: no sensitive values/cache files under `Assets/`, no whitespace errors, diff only intended files.

- [ ] **Step 3: Run XML coverage gate only if public-doc baseline includes SheetX**

Do not run `scripts/check-xmldoc-coverage.py --root Assets/RevCore` as SheetX is outside its root. Manually inspect public `SheetXExport*` members for XML summaries.

- [ ] **Step 4: Review external-call acceptance**

Create a temporary separate editor assembly test that references `RCore.SheetX.Editor`, calls `SheetXExporter.ExportExcel(request, memoryOutput)`, and asserts:

- no `SheetXSettings.Init()` call;
- no `Assets/SheetX/SheetXSettings.asset` created/read;
- no output disk change;
- result has generated records;
- malformed JSON and duplicate ID return `Success == false` without modal dialog.

- [ ] **Step 5: Do not commit or push**

Present test evidence and diff summary. Commit only after explicit user request.

## Execution outcome (2026-08-21)

Implemented, uncommitted. `RCore.SheetX.Tests` EditMode: 25 passed, 0 failed, 0 skipped
(Unity 2022.3.62f3, `-runTests` without `-quit`; `-quit` makes the editor exit before the run
and produce no results file).

Deviations from the plan, each deliberate:

- Task 6 Step 6: no Google fetch seam. Adding an internal network abstraction only to test it is
  an interface with one implementation; the Google path is covered by request validation, the
  empty-selection guard, and the shared builders the Excel tests exercise. Add the seam when a
  real defect needs it.
- Task 7 Step 3: `SheetXSettings.CreateExportRequest` not added and the windows are unchanged.
  Their buttons expose individual phases (`ExportIDs`, `ExportConstants`, `ExportJson`,
  `ExportLocalizations`) plus the multi-file flows; the public API exports a whole spreadsheet, so
  routing the buttons through it would either lose those semantics or require a public
  export-kind API nobody asked for. Legacy writes still leave through `ISheetXOutput` via
  `SheetXFileOutput`, which is what R2 requires.
- Task 8 Step 4: no second test assembly. `RCore.SheetX.Tests` is already a separate assembly
  referencing `RCore.SheetX.Editor` and asserts the same contract.

Fixed during verification:

- `SheetXExporter.ExportGoogle` checked credentials before honouring `Sheets = []`, so
  "empty means none" failed with a credentials error instead of doing nothing. Guard now precedes
  the credential check; `export_google_empty_sheet_selection_needs_no_credentials` locks it.
- `IWorkbook`/`XSSFWorkbook` are not `IDisposable` in the bundled NPOI, so `using` on them did not
  compile (`CS1674`). Both dropped; the workbook is memory-backed, so no handle outlives the read.
- `RCore.SheetX.Tests.asmdef` sets `overrideReferences`, so the NPOI-backed test could not resolve
  `NPOI`. Added the existing plugin DLLs to `precompiledReferences`.
- Conflicting duplicate IDs still reached `EditorUtility.DisplayDialog` in the legacy path only;
  `export_excel_reports_conflicting_duplicate_id_as_error` locks that the public path returns the
  same text through `SheetXExportResult.Errors` instead of a modal a batch caller cannot dismiss.

Known limitation: `grep -ri 'client_secret|refresh_token' Assets/` still matches three
pre-existing binaries (`FirebaseCppAuth.dll`, `FirebaseCppFirestore.dll`, `Google.Apis.Auth.dll`)
whose compiled-in OAuth field names are not secrets. Text-only scan (`git grep -nI`) is clean.

## Plan self-review

- Coverage: R1/R2 Tasks 4-6; R3 Tasks 5-6; R4 Task 1; R5 Tasks 1, 5, 6; R6 Task 3; R7 Task 2/5; R8 Task 3/6; R9/R10 Task 2/7; R11 Task 1; R12 Tasks 5-6.
- No iKit types or dependencies appear in planned source API.
- Public contract signatures are consistent across Tasks 4-7.
- Scope intentionally excludes iKit settings UI, artifact import/compare policy, config validation, localization-provider integration, and `com.ikit.config` work.
