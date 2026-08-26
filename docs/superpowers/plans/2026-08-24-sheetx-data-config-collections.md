# SheetX Data Config Collections Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add opt-in SheetX data-config collections: spreadsheet tables become typed row models and Editor-baked `ScriptableObject` assets, exposed by one Resources-loaded Global root with zero runtime JSON parsing.

**Architecture:** Add one UnityEngine-only SheetX Runtime assembly for collection bases. Keep all parsing, settings validation, code generation, JSON staging, baking, Play Mode interception, and UI inside `RCore.SheetX.Editor`; normal SheetX JSON remains unchanged unless a settings-backed sheet binding selects a Collection mode. Generated source is partial and data-only; Editor code validates all collection work before flushing generated JSON and `SheetXDataCollections.cs`, then bakes serialized collection fields after compilation.

**Tech Stack:** Unity 2022.3, C#, Unity Editor APIs, `ScriptableObject`, `SerializedObject`, `AssetDatabase`, `SessionState`, `DidReloadScripts`, `Newtonsoft.Json` editor dependency, existing SheetX NUnit EditMode suite, NPOI, Google Sheets API.

## Global Constraints

- Package: `Assets/RCore.SheetX/` (`com.rabear.rcore.sheetx`), currently 1.4.0; this release is a minor version bump.
- Feature is opt-in. `enableCollections` defaults to `false`; disabled path generates, bakes, hooks, and changes nothing.
- Do not modify frozen `Assets/RCore/`, including legacy `RCore.Data.ConfigCollection`.
- Runtime assembly references `UnityEngine` only. No `UnityEditor`, NPOI, Google APIs, Newtonsoft, reflection, JSON parsing, or TextAsset references.
- Editor assembly references runtime assembly. Generated code and developer partials compile in developer-owned asmdef; SheetX must not create or infer asmdefs.
- `JSON Only` preserves existing SheetX JSON inference and output behavior. Detached `SheetXExporter` and `SheetXBatchExportRequest`/`SheetXExporter.ExportBatch` stay collection-free in this release.
- Collection JSON may use any ordinary project-relative `Assets/` folder but must stay outside `Resources` and `StreamingAssets`; code, feature-asset, JSON, and Global Resources output folders must be distinct project paths. Global Resources folder ends exactly in `/Resources`.
- Global asset file name equals generated Global type name and lives directly in its Resources root. Runtime default lookup is `Resources.Load<T>(typeof(T).Name)`.
- Generated source uses `SheetXDataCollections.cs`, tabs, CRLF, and a SheetX generated-file banner. Successful export removes legacy `SheetXDataCollections.g.cs` and its `.meta`; SheetX never overwrites developer files.
- Generated source emits fields only: no lookups, dictionaries, getters, setters, IDs, business rules, runtime `LoadData()`, or runtime path lookup.
- Generated Model infers `int`, `float`, `bool`, or `string` from longest non-empty column cells. Optional annotations override inference. Sheet-local collisions and invalid explicit annotations log and skip offending sheet; later valid sheets continue.
- Existing Model accepts only concrete, non-generic, `[Serializable]` row types. It keeps legacy SheetX JSON shapes and Editor-only Newtonsoft deserialization.
- Every collection export/load validates and deserializes complete batch before asset mutation. On asset mutation failure, restore changed assets and delete newly-created assets from that batch.
- Export completeness is source-scoped: every saved Collection binding from a processed source must be processed, while bindings from unrelated, unprocessed sources do not block. Generated source contains accepted current-session bindings only; previous unrelated JSON remains untouched.
- Do not commit or push unless user explicitly asks. Before any later commit, update root `CHANGELOG.md`; omit `Co-Authored-By`.
- Work in main repository directory. Do not create a worktree.

---

## File Structure

| Path | Change | Responsibility |
| --- | --- | --- |
| `Assets/RCore.SheetX/Runtime/RCore.SheetX.Runtime.asmdef` | Create | UnityEngine-only runtime assembly. |
| `Assets/RCore.SheetX/Runtime/SheetXConfigCollectionBase.cs` | Create | Loaded-state base for generated feature collections. |
| `Assets/RCore.SheetX/Runtime/GlobalConfigCollectionBase.cs` | Create | Generic Resources root lookup and explicit root override. |
| `Assets/RCore.SheetX/Editor/RCore.SheetX.Editor.asmdef` | Modify | Reference runtime assembly. |
| `Assets/RCore.SheetX/Editor/SheetXSettings.cs` | Modify | Persist collection settings and binding data; initialize defaults. |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSettings.cs` | Create | Identifier/path/type validation plus Global/CRUD/binding operations. |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSchema.cs` | Create | Generated-header parser, normalized naming, row JSON model. |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionGenerator.cs` | Create | Deterministic partial row/collection/Global/path C# source emission. |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs` | Create | Interactive collection preflight, staged artifact ownership, Excel/Google-neutral row inputs. |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionBaker.cs` | Create | Compile-reload pending store, JSON deserialize, collection asset create/update/rollback. |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionPlayModeLoader.cs` | Create | Pre-Play Auto Load interception. |
| `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSheetGUI.cs` | Create | Managed dropdown/type-picker cells for one source's sheets. |
| `Assets/RCore.SheetX/Editor/SheetXSettingsWindow.cs` | Modify | Opt-in controls, folders, namespace, defaults, Collection CRUD, Load Data actions. |
| `Assets/RCore.SheetX/Editor/SheetXHelper.cs` | Modify | Optional collection columns in existing sheet table factory. |
| `Assets/RCore.SheetX/Editor/{Excel,Google}SheetXWindow.cs` | Modify | Pass source identity into collection-aware sheet table. |
| `Assets/RCore.SheetX/Editor/Edit{Excel,Google}SheetsWindow.cs` | Modify | Same binding controls for multi-file source sheet editors. |
| `Assets/RCore.SheetX/Editor/{Excel,Google}SheetHandler.cs` | Modify | Route interactive bound sheets through collection session; preserve normal and batch paths. |
| `Assets/RCore.SheetX/Tests/CollectionPathTests.cs` | Create | Path and framework validation tests. |
| `Assets/RCore.SheetX/Tests/CollectionSettingsTests.cs` | Create | CRUD, binding identity, orphan and type-picker validation tests. |
| `Assets/RCore.SheetX/Tests/CollectionSchemaTests.cs` | Create | Annotation parser and generated-name collision tests. |
| `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs` | Create | C# and JSON snapshots plus output-mode characterization. |
| `Assets/RCore.SheetX/Tests/CollectionBakeTests.cs` | Create | Asset bake, root assignment, rollback, no-TextAsset tests. |
| `Assets/RCore.SheetX/Tests/CollectionPlayModeTests.cs` | Create | Auto-load selection and play transition decision tests. |
| `Assets/RCore.SheetX/package.json` | Modify | Minor version and runtime-capable package description. |
| `Assets/RCore.SheetX/CHANGELOG.md` | Modify | Collection release notes. |
| `Assets/RCore.SheetX/Document/Document.md` | Modify | Setup, binding, schema, lifecycle, limitations. |
| `CHANGELOG.md` | Modify | Canonical SheetX release entry. |
| `Assets/RCore.SheetX/Samples~/DataConfigCollections/README.md` | Create | Minimal consumer setup/sample layout, no compiled project code. |

## Shared Internal Interfaces

Create interfaces below before handler/UI work. Keep all Editor types `internal` unless settings serialization requires `public` Unity-visible types.

```csharp
public enum SheetXSheetOutputMode
{
    JsonOnly = 0,
    CollectionGeneratedModel = 1,
    CollectionExistingModel = 2,
}

[Serializable]
public sealed class SheetXCollectionDefinition
{
    public string name;
    public bool autoLoad = true;
    public bool builtInGlobal;
}

[Serializable]
public sealed class SheetXSheetBinding
{
    public string sourceId;
    public string sheetName;
    public SheetXSheetOutputMode outputMode;
    public string collectionName;
    public string rowTypeName;
    public string fieldName;
}
```

```csharp
internal sealed class SheetXCollectionDiagnostic
{
    internal string Message;
    internal string SourceId;
    internal string SheetName;
    internal string Path;
}

internal sealed class SheetXCollectionSchema
{
    internal string RowTypeName;
    internal IReadOnlyList<SheetXCollectionColumn> Columns;
    internal IReadOnlyList<SheetXCollectionObject> Objects;
}

internal static class SheetXCollectionSettings
{
    internal const string GlobalName = "Global";

    internal static void EnsureGlobal(SheetXSettings settings);
    internal static SheetXSheetBinding GetOrCreateBinding(
        SheetXSettings settings, string sourceId, string sheetName);
    internal static bool RenameCollection(
        SheetXSettings settings, string oldName, string newName, out string error);
    internal static bool DeleteCollection(
        SheetXSettings settings, string name, out string error);
    internal static List<SheetXCollectionDiagnostic> Validate(
        SheetXSettings settings, IEnumerable<SheetXSheetBinding> activeBindings);
}
```

```csharp
internal static class SheetXCollectionSchemaParser
{
    internal static bool TryParse(
        IReadOnlyList<string> headers,
        string rowTypeName,
        out SheetXCollectionSchema schema,
        out string error);

    internal static bool TryBuildRows(
        SheetXCollectionSchema schema,
        IReadOnlyList<IReadOnlyList<string>> rows,
        out string json,
        out string error);
}

internal static class SheetXCollectionGenerator
{
    internal static string Emit(
        SheetXSettings settings,
        IReadOnlyList<SheetXCollectionGeneratedTable> tables);
}
```

```csharp
internal sealed class SheetXCollectionExportSession
{
    internal SheetXCollectionExportSession(SheetXSettings settings);

    internal bool TryAddGeneratedTable(
        string sourceId, string sheetName, string fieldName,
        IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows,
        out string error);

    internal bool TryAddExistingTable(
        string sourceId, string sheetName, string fieldName, string rowTypeName,
        string legacyJson, out string error);

    internal bool Flush(out string error);
}
```

`SheetXCollectionExportSession` owns only interactive, Collection-bound tables. It must never be constructed by transient/public/batch settings. `Flush` requires every saved Collection binding from each processed source to be processed, but ignores bindings from unrelated, unprocessed sources. It writes accepted current-session collection JSON and generated code, then registers pending bake work and calls `AssetDatabase.Refresh()` once.

### Task 1: Add runtime collection bases and assembly boundary

**Files:**
- Create: `Assets/RCore.SheetX/Runtime/RCore.SheetX.Runtime.asmdef`
- Create: `Assets/RCore.SheetX/Runtime/SheetXConfigCollectionBase.cs`
- Create: `Assets/RCore.SheetX/Runtime/GlobalConfigCollectionBase.cs`
- Modify: `Assets/RCore.SheetX/Editor/RCore.SheetX.Editor.asmdef`
- Modify: `Assets/RCore.SheetX/Tests/RCore.SheetX.Tests.asmdef`
- Test: `Assets/RCore.SheetX/Tests/CollectionBakeTests.cs`

**Consumes:** UnityEngine only.

**Produces:**

```csharp
public abstract class SheetXConfigCollectionBase : ScriptableObject
{
    public bool IsLoaded { get; }
    public void SetLoaded();
    public void ResetLoaded();
}

public abstract class GlobalConfigCollectionBase : SheetXConfigCollectionBase
{
    public static T Instance<T>() where T : GlobalConfigCollectionBase;
    public static void SetInstance<T>(T collection) where T : GlobalConfigCollectionBase;
}
```

- [ ] **Step 1: Write failing runtime-base tests**

Create a test-only `TestGlobalCollection : GlobalConfigCollectionBase`. Test loaded state and override without invoking Resources:

```csharp
[Test]
public void global_override_returns_injected_instance()
{
    var root = ScriptableObject.CreateInstance<TestGlobalCollection>();
    try
    {
        GlobalConfigCollectionBase.SetInstance(root);
        Assert.That(GlobalConfigCollectionBase.Instance<TestGlobalCollection>(), Is.SameAs(root));
    }
    finally
    {
        GlobalConfigCollectionBase.SetInstance<TestGlobalCollection>(null);
        Object.DestroyImmediate(root);
    }
}
```

Add test that `SetLoaded()` changes `IsLoaded`, then `ResetLoaded()` clears it.

- [ ] **Step 2: Run focused tests before implementation**

Run Unity EditMode filter `RCore.SheetX.Tests.CollectionBakeTests`.

Expected: compile failure because runtime base types do not exist.

- [ ] **Step 3: Add runtime asmdef and minimal base implementation**

Create `RCore.SheetX.Runtime.asmdef` with empty `references`, no platform restriction, `autoReferenced: true`, and `noEngineReferences: false`. Add `RCore.SheetX.Runtime` to Editor asmdef `references`; add runtime reference to test asmdef only if test compiler does not receive it transitively.

Implement `SheetXConfigCollectionBase` with `[NonSerialized] private bool m_isLoaded;`. Implement `GlobalConfigCollectionBase` with generic static holder nested as `private static class InstanceHolder<T> where T : GlobalConfigCollectionBase`; `Instance<T>()` returns injected value first, otherwise `Resources.Load<T>(typeof(T).Name)`. `SetInstance<T>(null)` clears override.

Do not add `CreateAssetMenu`, dictionaries, reflection, serialized JSON fields, or runtime file paths.

- [ ] **Step 4: Run focused tests and compile check**

Run Unity EditMode filter `RCore.SheetX.Tests.CollectionBakeTests`.

Expected: PASS. Unity Console has no assembly-reference errors.

### Task 2: Persist collection settings and validate paths/CRUD

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs`
- Create: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSettings.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionPathTests.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionSettingsTests.cs`

**Consumes:** `SheetXSettings`, `SheetXCollectionDefinition`, `SheetXSheetBinding`.

**Produces:** shared settings interfaces above; fields below on `SheetXSettings`:

```csharp
public bool enableCollections;
public List<SheetXCollectionDefinition> collections;
public List<SheetXSheetBinding> sheetBindings;
public string collectionCodeFolder;
public string collectionAssetFolder;
public string collectionJsonFolder;
public string collectionNamespace;
public string globalResourcesFolder;
public bool autoLoadAfterExport;
public bool autoLoadBeforePlay;
```

- [ ] **Step 1: Write failing settings tests**

Create settings using `ScriptableObject.CreateInstance<SheetXSettings>()`, call `ResetToDefault()`, and assert:

```csharp
Assert.That(settings.enableCollections, Is.False);
Assert.That(settings.collections.Single().name, Is.EqualTo("Global"));
Assert.That(settings.collections.Single().builtInGlobal, Is.True);
Assert.That(settings.autoLoadAfterExport, Is.True);
Assert.That(settings.autoLoadBeforePlay, Is.True);
```

Add focused cases:

```csharp
Assert.That(SheetXCollectionSettings.RenameCollection(settings, "Shop", "Store", out _), Is.True);
Assert.That(settings.sheetBindings.Single().collectionName, Is.EqualTo("Store"));

Assert.That(SheetXCollectionSettings.DeleteCollection(settings, "Shop", out _), Is.True);
Assert.That(settings.sheetBindings.Single().collectionName, Is.EqualTo("Global"));
```

Assert Global rename/delete returns `false`; same `sourceId` plus `sheetName` returns one binding; invalid `/Resources` ending, JSON outside `Assets/**/Editor/**`, and folder overlap each produce validation diagnostics.

- [ ] **Step 2: Run focused tests before implementation**

Run Unity EditMode filter `RCore.SheetX.Tests.CollectionPathTests|RCore.SheetX.Tests.CollectionSettingsTests`.

Expected: compile failure because collection settings types do not exist.

- [ ] **Step 3: Add serialized fields and deterministic defaults**

Add public serialized classes and enum near existing SheetX editor data types. In `ResetToDefault()` set:

```csharp
enableCollections = false;
collections = new List<SheetXCollectionDefinition>
{
    new SheetXCollectionDefinition { name = "Global", autoLoad = true, builtInGlobal = true },
};
sheetBindings = new List<SheetXSheetBinding>();
collectionCodeFolder = "";
collectionAssetFolder = "";
collectionJsonFolder = "";
collectionNamespace = "";
globalResourcesFolder = "";
autoLoadAfterExport = true;
autoLoadBeforePlay = true;
```

`CreateTransient(...)` must retain this disabled default and must not map collection values from public requests.

- [ ] **Step 4: Implement settings operations and full preflight**

Normalize only path separators and trailing slashes. Require project-relative paths starting `Assets/`; determine segment membership rather than substring matching: JSON path must not contain `Resources` or `StreamingAssets`, Global path final segment must be `Resources`, and code/asset/JSON roots cannot contain each other after normalization.

`EnsureGlobal` restores exactly one immutable `Global` definition and migrates null/empty/deleted binding collections to `Global`. `RenameCollection` rejects Global, invalid C# identifier, keyword, duplicate name; after success migrates matching binding names. `DeleteCollection` rejects Global, migrates bindings to Global, removes definition, never deletes files. `GetOrCreateBinding` keys ordinally on `sourceId` and `sheetName`, defaults `JsonOnly` and `Global`.

`Validate` returns all diagnostics, not first failure. Check missing namespace/folders when enabled, identifier and namespace grammar, duplicate definitions, folder paths, missing Global, dangling bindings, duplicate per-collection field names, and supplied orphan bindings. Format each issue as `[SheetX Collections] <collection> / <source> / <sheet>:\n<cause>\nPath: <path>`.

- [ ] **Step 5: Run settings test suite**

Run Unity EditMode filters `CollectionPathTests` and `CollectionSettingsTests`.

Expected: PASS. Existing `SheetXSettingsTests` passes unchanged.

### Task 3: Parse generated schema and emit data-only partial source

**Files:**
- Create: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSchema.cs`
- Create: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionGenerator.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionSchemaTests.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs`

**Consumes:** Task 2 settings and `SheetXSheetOutputMode`.

**Produces:** parser/generator shared interfaces and `SheetXCollectionGeneratedTable` containing source identity, collection, field, row type, JSON path, schema, and optional existing type name.

- [ ] **Step 1: Write parser tests first**

Use headers and string rows directly:

```csharp
[Test]
public void generated_schema_supports_scalars_arrays_and_nested_objects()
{
    bool ok = SheetXCollectionSchemaParser.TryParse(
        new[] { "id:int", "price:float", "enabled:bool", "tags[]:string", "reward.amount:int" },
        "ShopItemsSX", out var schema, out var error);

    Assert.That(ok, Is.True, error);
    Assert.That(schema.RowTypeName, Is.EqualTo("ShopItemsSX"));
    Assert.That(schema.Columns.Count, Is.EqualTo(5));
}
```

Add tests for plain-header inference, optional annotation override, unsupported type, non-keyword invalid names, `reward` with `reward.amount`, duplicate normalized names, leaf `[]` before a dotted child, and row-type collision after normalization (`Shop Items`, `ShopItems`). Add compatibility tests proving any header containing exact ordinal `[x]` is ignored, exact C# keyword path segments are skipped with actionable warnings, and ignored middle columns retain original source indexes. Add JSON row test proving headers are removed from field names and nested values form an object.

- [ ] **Step 2: Run schema tests before implementation**

Run Unity EditMode filter `RCore.SheetX.Tests.CollectionSchemaTests`.

Expected: compile failure because parser does not exist.

- [ ] **Step 3: Implement annotation grammar and JSON row builder**

Parse exact grammar:

```text
<header> ::= <path> [":" <type>]
<path> ::= <name> ("." <name>)* ("[]")?
<type> ::= int | float | bool | string
```

Infer unannotated fields as `int`, `float`, `bool`, or `string` from longest trimmed non-empty source cell; explicit annotations override inference. Ignore any raw header containing exact ordinal `[x]` before grammar parsing. Skip any path containing an exact C# keyword segment and emit actionable warning. Other malformed identifiers, malformed/unsupported annotations, duplicate normalized paths, and object/leaf conflicts log an actionable error and skip offending sheet. Preserve original source-column index for every accepted field so ignored columns never shift later values.

Use invariant parsing. Empty cells emit default scalar values only when current SheetX legacy exporter would preserve them through configured persistent fields; otherwise omit field from JSON row. For arrays split cell values with existing `SheetXHelper.SplitValueToArray(value, false)`. Parse booleans strictly with `bool.TryParse`; error on malformed int/float/bool. Every blocking schema/value diagnostic includes source, sheet, 1-based source column, raw header, cause, and `Fix:` repair guidance. Build `JArray`/`JObject` with Newtonsoft only in Editor parser code; output must be bare JSON row array.

- [ ] **Step 4: Write generator snapshot tests**

Create a generated table set: Global `GameSettings`, Shop `ShopItems`, Existing Model `MissionData`. Assert generated source contains:

```csharp
public partial class ShopItemsSX
public int id;
public string[] tags;
public Reward reward;
public partial class ShopConfigCollection : SheetXConfigCollectionBase
public ShopItemsSX[] shopItems;
public MissionData[] missions;
public partial class GlobalConfigCollection : GlobalConfigCollectionBase
public ShopConfigCollection shop;
internal const string ShopItems = "Assets/Game/DataConfig/Json/ShopItems.txt";
```

Assert it contains no `UnityEditor`, `Newtonsoft`, `LoadData`, `Dictionary`, `get;`, or `set;`. Assert JSON Only produces no generated table declaration.

- [ ] **Step 5: Implement deterministic generator**

Emit one `SheetXDataCollections.cs` to `collectionCodeFolder`, with `/*** This script is automatically generated by SheetX. ***/`, `using System;`, `using UnityEngine;`, namespace `collectionNamespace`, `partial` row/nested/collection/global types, and `static partial class SheetXCollectionPaths` constants. Feature collection class name is `<Collection>ConfigCollection`; Global is `GlobalConfigCollection`; sheet `ShopItems` derives `ShopItemsSX` and field `shopItems` unless binding overrides `fieldName`.

Order collections ordinally with Global first; order tables by collection then binding source/sheet. Reject collisions before emission; never suffix a name. Existing Model emits only its collection field using stored type name. Global emits its own table fields and one serialized feature collection reference per non-Global collection. Paths use `collectionJsonFolder/<normalized-sheet>.txt` with `/` separators.

- [ ] **Step 6: Run parser and generator tests**

Run Unity EditMode filters `CollectionSchemaTests` and `CollectionGenerationTests`.

Expected: PASS. Generated snapshot has only runtime-safe imports.

### Task 4: Stage interactive collection outputs and route Excel

**Files:**
- Create: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs`
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs`

**Consumes:** Tasks 2–3, existing `SheetXWriter`, `SheetXExportContext`, `SheetXFileOutput`, `ExcelSheetHandler.ConvertSheetToJson`.

**Produces:** collection-only interactive export transaction. Existing Excel public/detached/batch JSON behavior unchanged.

- [ ] **Step 1: Write collection session tests**

Use fake row/header input and temporary `Assets/SheetXTestsTemp/Editor/` folders. Verify a session with one Generated table and one Existing table writes only after `Flush`; Generated output contains typed JSON; Existing output receives legacy JSON unchanged; `JsonOnly` is not added. Add an invalid sheet between valid sheets; assert it logs once, skips only itself, preserves previous skipped JSON, and later valid output still writes. Assert a saved binding missing from a processed source aborts flush, while a binding from an unrelated source does not block and is omitted from generated source.

- [ ] **Step 2: Run tests before implementation**

Run Unity EditMode filter `RCore.SheetX.Tests.CollectionGenerationTests`.

Expected: compile failure because export session does not exist.

- [ ] **Step 3: Implement staged transaction and pending registration**

`SheetXCollectionExportSession` tracks processed sources, processed binding identities, accepted candidates, and skipped identities. `TryAddGeneratedTable` parses header plus rows. `TryAddExistingTable` validates selected type with `Type.GetType`, then loaded assemblies; require `[Serializable]`, class, concrete, non-generic. Sheet-local schema, value, binding, type, JSON-mapping, and candidate-collision failures log once and skip only that sheet. Before `Flush`, validate global settings and require every saved Collection binding from each processed source to be present in processed identities. Unrelated, unprocessed sources do not participate in that guard.

On success, use a private `SheetXExportContext(new SheetXFileOutput(), true)` and `SheetXWriter` to stage accepted current-session collection JSON plus one `SheetXDataCollections.cs`; call context `Flush` only when global/source completeness checks pass. Register pending bake with accepted binding identities only after flush. Refresh AssetDatabase once. Never direct-write collection artifacts with `File` or `SheetXHelper.WriteFile`.

- [ ] **Step 4: Route Excel only for settings-backed interactive export**

In `ExportJson(IWorkbook)` and `ExportAllFiles()`, build one session only when `m_settings.enableCollections` and writer is not detached. Resolve binding identity with Excel workbook `path` and sheet name. For `CollectionGeneratedModel`, read header and rows via a private Excel cell reader that preserves sheet row order and formula values; add to session and skip ordinary JSON. For `CollectionExistingModel`, call existing `ConvertSheetToJson(..., pAutoWriteFile: false)` to retain legacy structures, add returned JSON to session, and skip ordinary JSON. For `JsonOnly` or missing/default binding, retain existing branch exactly.

Call `session.Flush(out error)` only after all selected data sheets; send error through `m_writer.Error`. `ExportAll` retains IDs/constants/localization behavior. `BatchBuildJson` and all `m_writer.Detached` paths must not construct a session.

- [ ] **Step 5: Run focused regression tests**

Run Unity EditMode filters `CollectionGenerationTests`, `ExcelSheetHandlerBatchTests`, `SheetXBatchExportTests`, and `SheetXExportTests`.

Expected: PASS. Existing detached and batch Excel Config/JSON behavior stays unchanged.

### Task 5: Route Google collection outputs without changing OAuth/public APIs

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs`

**Consumes:** Task 4 export session.

**Produces:** Google interactive route identical to Excel outcome.

- [ ] **Step 1: Add pure Google table conversion tests**

Add test input `IList<IList<object>>` with annotated header/rows. Feed it through one internal Google-neutral table conversion helper exposed to tests with `internal`; assert parsed rows preserve empty cells and header values. Keep OAuth/network out of tests.

- [ ] **Step 2: Run test before implementation**

Run Unity EditMode filter `RCore.SheetX.Tests.CollectionGenerationTests`.

Expected: compile failure because Google table helper does not exist.

- [ ] **Step 3: Implement Google interactive routing**

Add private reader from fetched Google values to header and row string lists. In `ExportJson()` and `ExportAllFiles()`, create/use collection session only for enabled, non-detached settings. Reuse already-fetched values; never add an extra Google request. Bind `sourceId` to spreadsheet ID. For Generated, add parsed rows. For Existing, call existing `ConvertSheetToJson(..., pWriteFile: false)` then add legacy JSON. Skip ordinary JSON only for Collection modes. Flush once after selected source sheets, before method returns.

Do not alter `BatchMaterialize`, `BatchBuildJson`, public request types, `EditorPrefs`, token handling, OAuth validation, or network test coverage.

- [ ] **Step 4: Source review and regression test**

Verify exact outcomes: settings off and JSON Only use legacy code; IDs/Constants/Settings/Localization never enter collection route; combined JSON excludes collection tables; each Collection mode produces one source path; one session flush per export operation; no duplicate Google fetch.

Run Unity EditMode filters `CollectionGenerationTests`, `SheetXBatchExportTests`, `SheetXExportTests`.

Expected: PASS.

### Task 6: Bake collection assets after compile and preserve asset transaction integrity

**Files:**
- Create: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionBaker.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionBakeTests.cs`

**Consumes:** Task 1 runtime bases, Tasks 2–4 pending collection metadata and generated code paths, `SessionState`, `AssetDatabase`, `SerializedObject`, Newtonsoft.

**Produces:**

```csharp
internal static class SheetXCollectionBaker
{
    internal static void RegisterPendingBake(SheetXSettings settings);
    internal static bool TryLoadData(SheetXSettings settings, bool autoLoadOnly, out string error);
}
```

- [ ] **Step 1: Write bake tests first**

Create test row classes and `ScriptableObject` collection types under test assembly. Create fixture `.asset` and JSON at `Assets/SheetXTestsTemp/Editor/`; test `TryLoadData` writes a typed row array into serialized field, leaves no `TextAsset` object reference, and sets collection `IsLoaded`.

Create Global plus feature assets, run load, assert Global serialized feature field references feature asset. Feed invalid JSON after recording existing array values, assert no field changed. Inject write failure through internal test-only asset writer seam, assert prior asset values restore and created asset path no longer exists.

- [ ] **Step 2: Run tests before implementation**

Run Unity EditMode filter `RCore.SheetX.Tests.CollectionBakeTests`.

Expected: compile failure because baker does not exist.

- [ ] **Step 3: Implement pending reload store**

Create serializable pending data containing settings asset path and intent flags. Store under `SessionState` key `SheetX.PendingCollectionBakes`; replace stale pending entry for same settings asset. `[DidReloadScripts]` resolves settings asset then calls `TryLoadData(settings, autoLoadOnly: true, out error)`. If generated collection/row type cannot resolve after compilation, retain entry and log `Pending bake: compilation failed` with type name. Never inspect generated C# source; resolve types by full name from collection namespace.

- [ ] **Step 4: Implement complete pre-deserialize and asset transaction**

Build expected generated/existing table list from settings bindings. Find or create each feature asset in `collectionAssetFolder`; find/create Global only at `<globalResourcesFolder>/GlobalConfigCollection.asset`. Before any mutation, load every JSON file as text with `File.ReadAllText`, require JSON root array, resolve declared row type, call `JsonConvert.DeserializeObject(json, rowType.MakeArrayType())`, and retain arrays in memory.

Snapshot every existing target asset with `EditorJsonUtility.ToJson(asset)` before modification. Create assets only after all parsing succeeds. Assign arrays and Global feature references via `SerializedObject.FindProperty(binding.fieldName)`; verify property is an array/object reference of compatible target. `ApplyModifiedPropertiesWithoutUndo`, `EditorUtility.SetDirty`, `AssetDatabase.SaveAssetIfDirty`. Call `SetLoaded()` only after complete successful commit.

On mutation exception: restore every snapshot with `EditorJsonUtility.FromJsonOverwrite`, save restored assets, delete every asset created in this pass, then return one error naming collection/source/sheet/path. Do not catch and continue per table.

- [ ] **Step 5: Run asset tests and cleanup verification**

Run Unity EditMode filter `RCore.SheetX.Tests.CollectionBakeTests`.

Expected: PASS. Teardown removes `Assets/SheetXTestsTemp/Editor/` fixtures with `AssetDatabase.DeleteAsset`.

### Task 7: Add Play Mode guard and collection management UI

**Files:**
- Create: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionPlayModeLoader.cs`
- Create: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSheetGUI.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettingsWindow.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXHelper.cs`
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetXWindow.cs`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetXWindow.cs`
- Modify: `Assets/RCore.SheetX/Editor/EditExcelSheetsWindow.cs`
- Modify: `Assets/RCore.SheetX/Editor/EditGoogleSheetsWindow.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionPlayModeTests.cs`
- Test: `Assets/RCore.SheetX/Tests/CollectionSettingsTests.cs`

**Consumes:** Tasks 2, 4, 6.

**Produces:** controlled per-sheet mode/collection/type controls and pre-Play `Load Data` decision.

- [ ] **Step 1: Write play-mode decision tests**

Keep transition side effects behind internal pure method:

```csharp
internal static bool ShouldLoadBeforePlay(
    SheetXSettings settings, bool isPlayingOrWillChangePlaymode);
```

Test disabled feature, disabled global toggle, already-playing state, and enabled state. Test `autoLoadOnly: true` selects only `SheetXCollectionDefinition.autoLoad == true`; Global feature-reference refresh stays selected even when Global auto-load is false.

- [ ] **Step 2: Run tests before implementation**

Run Unity EditMode filters `CollectionPlayModeTests` and `CollectionSettingsTests`.

Expected: compile failure because play-mode loader and sheet GUI do not exist.

- [ ] **Step 3: Implement Play Mode interception**

Subscribe once to `EditorApplication.playModeStateChanged`. On `ExitingEditMode`, return when `Application.isPlaying`, feature disabled, or `autoLoadBeforePlay` false. Otherwise call baker `TryLoadData(settings, autoLoadOnly: true, out error)`. If it fails, set `EditorApplication.isPlaying = false` and log full diagnostic. Never reload on `EnteredPlayMode`, `ExitingPlayMode`, or `EnteredEditMode`.

- [ ] **Step 4: Implement Settings window collection panel**

Below existing general settings, draw `Enable Data Config Collections`. When enabled, draw folder fields, namespace, post-export toggle, pre-Play toggle, and collection list. Add button validates new default `Collection`; name edits use `RenameCollection`; Delete uses `EditorUtility.DisplayDialog` and only calls `DeleteCollection` after confirmation. Global row is disabled for rename/delete. Render generated collection type as read-only `<Name>ConfigCollection`, Auto Load toggle, `Load Data`, and Global `Load All` buttons. All mutations use existing dirty/save pattern.

- [ ] **Step 5: Implement sheet-table dropdown/type controls**

Extend existing `SheetXHelper.CreateSpreadsheetTable(...)` with optional collection settings and `Func<string> sourceId`; do not change normal callers. When feature enabled and sheet is an ordinary data sheet, append `Output Mode`, `Collection`, and `Row Type` columns through `SheetXCollectionSheetGUI`. Collection is `EditorGUI.Popup` over managed definitions only. Generated displays derived row type read-only. Existing uses type picker list from concrete non-generic `[Serializable]` classes. JSON Only hides collection/type values. Persist `SheetXSheetBinding` on change. Do not create bindings for IDs, Constants, Settings, or Localization.

Pass source identity from Excel path/Google ID in single windows and multi-file edit windows. Rebuild cached `EditorTableView` when feature toggle/source changes so added columns appear immediately.

- [ ] **Step 6: Run UI-adjacent tests and manual editor smoke**

Run Unity EditMode filters `CollectionPlayModeTests`, `CollectionSettingsTests`, `CollectionPathTests`.

Manual: enable feature; add/rename/delete Shop; bind Excel and Google sheet UI; verify dropdown has Global and Shop only; verify delete confirmation moves Shop bindings to Global; trigger invalid JSON then enter Play Mode and confirm Play Mode remains stopped.

### Task 8: Integrate post-export Auto Load, complete regression tests, docs, version, and release notes

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs`
- Modify: `Assets/RCore.SheetX/package.json`
- Modify: `Assets/RCore.SheetX/CHANGELOG.md`
- Modify: `Assets/RCore.SheetX/Document/Document.md`
- Modify: `CHANGELOG.md`
- Create: `Assets/RCore.SheetX/Samples~/DataConfigCollections/README.md`
- Test: all six new Collection test files plus existing SheetX EditMode suite.

**Consumes:** Tasks 1–7.

**Produces:** release-ready optional framework, documented limits, minor version bump.

- [ ] **Step 1: Add post-export loading gate**

After a collection session flush and AssetDatabase refresh, register pending bake when generated code changed. For Existing-only exports with already-compiled types, call `TryLoadData(settings, autoLoadOnly: true, out error)` after successful flush only when `autoLoadAfterExport` is true. For generated source, reload callback makes same decision after compilation. If `autoLoadAfterExport` is false, still create/update Global feature references after successful compile; leave table arrays unchanged until user invokes Load Data.

- [ ] **Step 2: Add release docs**

Document exact setup folders, Global Resources root requirement, generated output ownership, three Output Modes, Collection dropdown/CRUD rules, annotation grammar/examples, Existing Model rules, Auto Load behavior, Load Data buttons, `Instance<T>()`/`SetInstance<T>()`, player-build JSON exclusion, and extraction/security limit. State detached/batch APIs do not support Collections yet. Sample README contains folder tree and five-column sample table only; no extra package dependency.

- [ ] **Step 3: Update package metadata and changelogs**

Bump `package.json` minor version from `1.4.0` to `1.5.0`. Replace editor-only description with description stating optional runtime collection bases plus Editor exporter workflow. Do not add dependencies.

Add matching entries under root and package Unreleased sections: opt-in typed collection generation, Editor-only JSON bake, Global Resources root/override, no runtime JSON parsing, collection metadata excluded from detached/batch APIs. Do not tag release; existing release workflow remains RevCore-only.

- [ ] **Step 4: Run focused and full verification**

Run Unity EditMode filters individually:

```text
RCore.SheetX.Tests.CollectionPathTests
RCore.SheetX.Tests.CollectionSettingsTests
RCore.SheetX.Tests.CollectionSchemaTests
RCore.SheetX.Tests.CollectionGenerationTests
RCore.SheetX.Tests.CollectionBakeTests
RCore.SheetX.Tests.CollectionPlayModeTests
```

Then Unity Test Runner: EditMode, Run All.

Run:

```powershell
python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json
```

Expected: SheetX changes do not regress RevCore coverage. Do not add RevCore `PublicAPI.Unshipped.txt`; no RevCore runtime API changed.

- [ ] **Step 5: Run manual acceptance matrix**

1. Feature off: export current workbook; JSON/IDs/Localization unchanged.
2. Feature on: create Shop, bind two Generated sheets; export; verify Editor JSON, `SheetXDataCollections.cs`, Shop asset, Global Resources asset/reference.
3. Enter Play Mode; `GlobalConfigCollectionBase.Instance<GlobalConfigCollection>()` returns Resources asset.
4. Call `SetInstance(customGlobal)` before first access; verify override wins.
5. Disable Mission Auto Load, alter JSON, export and play; baked Mission values remain until Load Data.
6. Rename and delete Shop; bindings migrate; old code/assets remain.
7. Export legacy headers containing `fixed` and `note[x]`; verify `fixed` logs actionable keyword warning, both columns are omitted, and later field values keep original source alignment.
8. Add duplicate normalized Generated headers beside later valid sheet; verify one diagnostic identifies source, sheet, 1-based column, raw header, cause, and repair, invalid sheet JSON stays untouched, and later valid sheet writes.
9. Set missing Existing Model type beside later valid sheet; verify invalid sheet skips and later valid sheet writes.
10. Build player; inspect report: no collection `.txt` under collection JSON folder.

- [ ] **Step 6: Review diff and stop before commit**

Check: runtime source imports UnityEngine only; generated source contains no Editor/Newtonsoft; settings-off and detached/batch paths have no collection session; no `TextAsset` fields; no automatic orphan deletion; no package dependency added; fixture teardown removed all temporary Assets.

Do not commit or push.

## Implementation Completion Checklist

- [ ] Feature default-off leaves ordinary SheetX output untouched.
- [ ] Runtime collection base is UnityEngine-only and root override works.
- [ ] Global is immutable; collection rename/delete migrates bindings safely.
- [ ] Global collection path/definition validation and missing bindings from processed sources block collection flush; unrelated source bindings do not block; sheet-local binding validation skips only offending sheet.
- [ ] Generated Model infers plain headers, accepts optional type annotations, and supports scalar fields, scalar arrays, dotted nested objects only.
- [ ] Existing Model preserves legacy JSON behavior and validates row type/deserialization in Editor.
- [ ] Generated `SheetXDataCollections.cs` is partial/data-only and never overwrites developer source.
- [ ] Collection JSON uses an ordinary `Assets/` folder outside `Resources` and `StreamingAssets`, so Unity does not include it automatically in player builds.
- [ ] Baker writes serialized arrays with no TextAsset reference and no runtime JSON parse.
- [ ] Global asset references feature collection assets and supports default Resources lookup plus explicit override.
- [ ] Post-export and pre-Play loading honor per-collection Auto Load; no reload happens during Play Mode.
- [ ] Invalid JSON/type/property and asset mutation failures preserve prior baked data.
- [ ] All new focused tests and existing SheetX EditMode suite pass.
- [ ] Package/version/docs/changelogs updated; no release tag, commit, or push performed.
