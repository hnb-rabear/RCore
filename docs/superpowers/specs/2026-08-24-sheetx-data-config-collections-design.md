# SheetX Data Config Collections — Design

Date: 2026-08-24
Status: Approved design, self-reviewed, not yet planned
Package: `Assets/RCore.SheetX/` (com.rabear.rcore.sheetx, currently 1.4.0)

## 1. Purpose

SheetX today exports spreadsheets to JSON, IDs, Constants, Localization, and a
narrow key/value Config ScriptableObject. It does not give a project a
ready-made way to load exported table data at runtime. Every consuming project
writes that layer itself.

This design adds an **optional** data-config framework to SheetX that:

- generates strongly typed row models from inferred or annotated sheet headers,
- groups tables into feature-scoped `ScriptableObject` collections,
- exposes those collections through one Global collection,
- bakes JSON into the collection assets at edit time so runtime does zero JSON
  parsing and ships no JSON,
- stays entirely opt-in: with the feature disabled, SheetX behaves exactly as it
  does today, and a project may keep its own loading system with no SheetX
  runtime dependency.

The reference implementation this generalises is the Goods-Jam
`DataConfigCollection` architecture (`Docs/data_config_collection_architecture.md`
in that project): editor-time JSON load, data baked into a serialized asset,
runtime reads fields only. Goods-Jam uses a single monolithic collection; this
design splits per feature.

The legacy `RCore.Data.ConfigCollection` in the frozen `Assets/RCore/` monolith
is **not** reused, extended, or modified. This framework is independent.

### Non-goals

- Runtime JSON loading or remote config.
- Encryption or obfuscation of baked data. Baked ScriptableObject data is still
  extractable from a build with AssetRipper/AssetStudio. Editor-only JSON gives
  zero runtime parse cost and no plaintext config file in the build — it is not
  a secrecy mechanism. Sensitive state validates server-side.
- Generated business APIs (lookups, dictionaries, getters, setters, validation
  rules). SheetX cannot know which column is a key.
- Automatic cleanup of orphaned generated code or assets.
- Automatic migration of hand-written partial code on rename.

## 2. Assembly and runtime architecture

### 2.1 Package layout

```text
Assets/RCore.SheetX/
├── Runtime/
│   ├── RCore.SheetX.Runtime.asmdef
│   ├── SheetXConfigCollectionBase.cs
│   └── GlobalConfigCollectionBase.cs
├── Editor/
│   ├── RCore.SheetX.Editor.asmdef
│   ├── Collection/
│   │   ├── SheetXCollectionSettings.cs
│   │   ├── SheetXCollectionGenerator.cs
│   │   ├── SheetXCollectionBaker.cs
│   │   └── SheetXCollectionPlayModeLoader.cs
│   └── ... existing exporter files
├── Tests/
│   └── RCore.SheetX.Tests.asmdef
└── Samples~/DataConfigCollections/
```

SheetX gains a Runtime assembly for the first time. `package.json` must stop
describing itself as editor-only.

Constraints:

- The Runtime assembly references `UnityEngine` only. No NPOI, no Google API,
  no `UnityEditor`, no Newtonsoft.
- The Editor assembly references the Runtime assembly.
- With the feature disabled, no generation, no baking, and no Play Mode hook
  runs. The existing export path is untouched.
- Generated runtime classes contain no `UnityEditor` code.
- Generated files and developer partial files must compile into the same
  assembly. SheetX does not create or infer project asmdefs; the developer owns
  the asmdef covering the generated code folder.

### 2.2 Runtime base API

```csharp
public abstract class SheetXConfigCollectionBase : ScriptableObject
{
	public bool IsLoaded { get; }

	public void SetLoaded();
	public void ResetLoaded();
}

public abstract class GlobalConfigCollectionBase
	: SheetXConfigCollectionBase
{
	public static T Instance<T>()
		where T : GlobalConfigCollectionBase;

	public static void SetInstance<T>(T collection)
		where T : GlobalConfigCollectionBase;
}
```

`Instance<T>()` loads `Resources.Load<T>(typeof(T).Name)` when no override
exists. Therefore `globalResourcesFolder` must be an `Assets/**/Resources/`
root — no child folder — and SheetX creates the Global asset with its generated
type name, e.g. `Assets/Game/Resources/GlobalConfigCollection.asset`.
`SetInstance<T>()` lets a project inject a root loaded any other way
(Addressables, custom bootstrap) before first access. No runtime dictionary,
reflection, or string registry.

### 2.3 Generated shapes

```csharp
namespace Game.DataConfig
{
	public partial class ShopConfigCollection
		: SheetXConfigCollectionBase
	{
		public ShopItemsSX[] shopItems;
		public ShopRewardsSX[] shopRewards;
	}
}
```

```csharp
namespace Game.DataConfig
{
	public partial class GlobalConfigCollection
		: GlobalConfigCollectionBase
	{
		public GameSettingsSX[] gameSettings;   // unassigned table
		public ShopConfigCollection shop;
		public MissionConfigCollection mission;
	}
}
```

The Global collection is both a holder for unassigned/global tables and the
serialized composition root referencing feature collections.

## 3. Settings and UI

### 3.1 Serialized data model

Added to `SheetXSettings`:

```csharp
public bool enableCollections;                        // default false
public List<SheetXCollectionDefinition> collections;
public List<SheetXSheetBinding> sheetBindings;
public string collectionCodeFolder;
public string collectionAssetFolder;
public string collectionJsonFolder;
public string collectionNamespace;
public string globalResourcesFolder;   // must end in /Resources
public bool autoLoadAfterExport = true;
public bool autoLoadBeforePlay = true;
```

```csharp
[Serializable]
public class SheetXCollectionDefinition
{
	public string name;              // "Shop"
	public bool autoLoad = true;
	public bool builtInGlobal;       // true only for Global
}

public enum SheetXSheetOutputMode
{
	JsonOnly = 0,                    // default; today's behaviour
	CollectionGeneratedModel = 1,
	CollectionExistingModel = 2,
}

[Serializable]
public class SheetXSheetBinding
{
	public string sourceId;          // workbook path / spreadsheet id
	public string sheetName;
	public SheetXSheetOutputMode outputMode;
	public string collectionName;    // "Global" when unassigned
	public string rowTypeName;       // assembly-qualified; ExistingModel only
	public string fieldName;         // optional field-name override
}
```

`enableCollections = false` is the default. All collection UI is hidden and no
collection code path executes.

### 3.2 Settings window, Collections tab

```text
[x] Enable Data Config Collections

Code Folder         Assets/Game/DataConfig/Generated            [Browse]
Asset Folder        Assets/Game/DataConfig/Collections          [Browse]
JSON Folder         Assets/Game/DataConfig/Json                 [Browse]
Global Resources    Assets/Game/Resources                       [Browse]
Namespace           Game.DataConfig

[x] Auto Load after export
[x] Auto Load before entering Play Mode

Collections                                        [+ Add]
─────────────────────────────────────────────────────────
Name       Generated Type              Auto Load
Global     GlobalConfigCollection      [x]        (built-in)
Shop       ShopConfigCollection        [x]        [Rename] [Delete]
Mission    MissionConfigCollection     [ ]        [Rename] [Delete]
```

Rules:

- `Global` cannot be renamed or deleted.
- A collection name must be a valid, unique, non-keyword C# identifier.
- Rename migrates every binding pointing at the old name.
- Delete reassigns that collection's sheets to `Global`, after a confirmation
  dialog.
- Rename and delete do **not** remove previously generated code or assets.
  SheetX reports orphans; the developer removes them. Deleting files
  automatically would destroy hand-written partial code.

### 3.3 Exporter window, sheet table

```text
✓  Sheet          Output Mode              Collection   Data Class
─────────────────────────────────────────────────────────────────
✓  ShopItems      Collection — Generated ▼ Shop      ▼  ShopItemsSX (auto)
✓  ShopRewards    Collection — Generated ▼ Shop      ▼  ShopRewardsSX (auto)
✓  Missions       Collection — Existing  ▼ Mission   ▼  [MissionData ▼]
✓  Analytics      JSON Only              ▼    —          —
✓  ItemIDs        (IDs sheet)                 —          —
✓  LocalizationVN (Localization sheet)        —          —
```

- The three extra columns appear only when `enableCollections` is on.
- IDs, Constants, Settings, and Localization sheets never get these columns;
  they are not data tables.
- `Collection` is a dropdown over the managed list, never free text.
- With `Generated`, `Data Class` is read-only and derived from the sheet name (`[Sheet]SX`).
- With `Existing`, `Data Class` is a searchable type picker limited to concrete,
  non-abstract, non-generic `[Serializable]` classes.
- A missing or renamed type shows `Missing: <old name>`; export logs an error and skips that sheet.

### 3.4 Binding identity

`sourceId + sheetName` is the key. A sheet renamed in the workbook leaves an
orphan binding, surfaced in the UI with `Remap` and `Remove` actions rather than
being silently dropped.

## 4. Schema and code generation

### 4.1 Header grammar (Generated Model only)

```text
<header> ::= <path> (":" <type>)?
<path>   ::= <name> ("." <name>)* ("[]")?
<type>   ::= int | float | bool | string
<name>   ::= valid non-keyword C# identifier
```

Examples:

```text
id
price
enabled
displayName
tags[]
reward.amount
id:string
price:float
```

- Plain headers infer `int`, `float`, `bool`, or `string` from longest non-empty
  cell in each column. Empty columns infer `string`.
- Optional `:type` annotations override inference for ambiguous or intentional
  types, such as leading-zero IDs or float fields whose current values are integers.
- `[]` applies to a leaf field only (`tags[]`, `reward.ids[]:int`).
- `.` produces a nested `[Serializable]` model.
- Any raw header containing exact, case-sensitive `[x]` text anywhere is ignored.
- A path with an exact C# keyword segment is skipped with an actionable warning.
  Other malformed identifiers and annotations log an actionable error and skip that sheet.
- Ignored columns retain their source indexes so later row values stay aligned.
- No enum, `long`, `double`, dictionary, object array, attribute heuristic, or
  repeated-column object array. Those structures require Existing Model or JSON
  Only.

### 4.2 Generated source

Naming is mechanical; no singular/plural guessing.

```text
Sheet: ShopItems
Data class: ShopItemsSX
Collection field: shopItems
```

```csharp
/***
 * This script is automatically generated by SheetX.
 ***/
using System;
using UnityEngine;

namespace Game.DataConfig
{
	[Serializable]
	public partial class ShopItemsSX
	{
		public int id;
		public float price;
		public string[] tags;
		public Reward reward;
	}

	[Serializable]
	public partial class Reward
	{
		public int amount;
		public string currency;
	}

	public partial class ShopConfigCollection
		: SheetXConfigCollectionBase
	{
		public ShopItemsSX[] shopItems;
	}

	public static partial class SheetXCollectionPaths
	{
		internal const string ShopItems =
			"Assets/Game/DataConfig/Json/ShopItems.txt";
	}
}
```

- SheetX only overwrites `SheetXDataCollections.cs`. Successful export removes legacy `SheetXDataCollections.g.cs` and its `.meta` file to prevent duplicate generated types.
- Developers extend the same types in ordinary files:

```csharp
namespace Game.DataConfig
{
	public partial class ShopItemsSX
	{
		public bool IsFree => price <= 0;
	}
}
```

- No generated getters, setters, dictionaries, ID lookups, or business
  validation.
- No generated runtime `LoadData()`. Data is baked into the `.asset`.
- The path constants live in generated code so the generated collection owns its
  source paths, but the baker does not read them. The baker reconstructs paths
  from the `SheetXSettings` bindings plus the JSON folder — no reflection, no
  parsing of generated C#.

### 4.3 Existing Model

- No row class is generated.
- Only the collection field is generated: `public MissionData[] missions;`
- The picker accepts `[Serializable]` concrete classes only.
- JSON property names must match that type's fields.
- Legacy SheetX header structures (`field{}`, repeated columns, legacy dotted
  behaviour) are allowed here.
- A missing or invalid type logs an error and skips that sheet before candidate output is staged.

### 4.4 Name collisions

Candidate sheet logs an error and skips on any of:

```text
Shop Items    and ShopItems  → same ShopItemsSX
item-id       and item_id    → same itemId
reward.amount and reward     → object/leaf conflict
Shop          → ShopConfigCollection already declared by developer code
```

No automatic `2` suffix, no silent rename. The generated public API must stay
stable.

## 5. Export, Load Data, and asset lifecycle

### 5.1 JSON source location

The collection JSON folder may use any ordinary project-relative `Assets/`
path, e.g. `Assets/Game/DataConfig/Json/`. It must not contain an exact
`Resources` or `StreamingAssets` path segment because Unity includes those
special folders in player builds. An `Editor` segment remains valid but is not
required.

### 5.2 Export flow

```text
Export
  1. Validate global collection configuration.
  2. Parse each collection-bound sheet; log once and skip sheet-local schema,
     value, binding, type, JSON-mapping, or generated-name errors.
  3. Require every saved Collection binding from each processed source to have
     been processed. Bindings from unrelated, unprocessed sources do not block.
  4. Stage accepted current-session JSON + matching SheetXDataCollections.cs through existing
     SheetXWriter/export-context staging.
  5. Flush accepted set atomically. Global validation, a missing binding from a
     processed source, or final staging failure aborts collection flush.
  5. AssetDatabase.Refresh().
  6. If generated types are new or changed: wait for compile, resume via
     [DidReloadScripts] (SessionState carries accepted binding identities).
  7. Baker runs Load Data for accepted bindings whose collections have Auto Load on.
```

### 5.3 Load Data

Each collection has a `Load Data` button; the Global collection also has
`Load All`.

```text
JSON .txt
  + compiled row type
  + field mapping from SheetXSettings
  → Newtonsoft JsonConvert.DeserializeObject(rowType[])
  → SerializedObject writes field array into collection .asset
  → SetDirty + SaveAssetIfDirty
```

Collection export JSON keeps existing SheetX support for legacy structures:
`field{}`, repeated columns, and legacy dotted values. `JsonUtility` cannot
reliably deserialize all supported Existing Model shapes, so the **Editor-only**
baker uses SheetX's existing Newtonsoft dependency. Runtime still has no
Newtonsoft dependency and parses no JSON.

- No `TextAsset` reference is serialized into any asset.
- No runtime JSON parsing.
- Feature collection assets live in the collection asset folder. The Global
  asset lives in the Global Resources folder so `Resources.Load` finds it. The
  Global asset assigns references to the feature collection assets.
- Per-collection `Auto Load` governs both post-export and pre-Play loading.
- A collection with Auto Load off keeps its previously baked data until the
  developer presses `Load Data`.
- The Global asset still refreshes its feature-collection references when the
  collection list changes, even with Global Auto Load off; it just does not
  reload its own table data.

### 5.4 Play Mode

- Runs only when `Auto Load Before Play` is on.
- Reloads only collections with `Auto Load` on.
- Never reloads while already in Play Mode.
- On any JSON, type, or property error, the Play Mode transition is cancelled
  and the error names the collection, sheet, header/property, and JSON path.

### 5.5 Failure boundaries

- Sheet-local schema, value, binding, type, JSON-mapping, or candidate-collision
  error: log once, skip only that sheet, and continue later sheets.
- Accepted JSON and matching `SheetXDataCollections.cs` write atomically. Previous JSON for a
  skipped sheet remains untouched and is excluded from current export-driven bake.
- Invalid namespace/folders/collection definitions, a saved Collection binding
  missing from a processed source, or final output/staging failure aborts collection
  flush. Bindings from unrelated, unprocessed sources do not abort the session.
- Generated source contains accepted current-session bindings only. Previous JSON
  from unprocessed sources remains untouched; stale declarations are not copied.
- Generated source fails to compile: accepted JSON and `SheetXDataCollections.cs` are already on disk;
  collection assets keep their last successful bake and the UI shows
  `Pending bake: compilation failed`.
- Baking multiple collections: validate and deserialize everything into memory
  before touching any asset.
- If an asset write fails mid-batch: restore the snapshot of every asset changed
  in that pass and delete assets newly created in that pass.
- Orphaned code and assets are never deleted automatically.

## 6. Validation

Runs before any export and before any `Load Data`.

```text
Framework
- Feature enabled but namespace/folder missing.
- JSON folder not under `Assets/`, or under `Resources` or `StreamingAssets`.
- Code, asset, or Global Resources folder not under Assets/.
- Global Resources folder does not end in `/Resources`.
- Code, asset, and JSON folders overlap.
- Invalid namespace.

Collection
- Global missing.
- Duplicate collection name.
- Collection name is not a C# identifier.
- Generated collection class collides with another type.
- Binding points at a deleted collection.
- Orphan binding: sourceId/sheetName no longer exists.

Sheet binding
- Collection output mode on a non-JSON-table sheet.
- Header containing `[x]` is ignored; exact C# keyword path segment is skipped with warning.
- Other invalid header grammar, type, identifier, duplicate path, object/leaf conflict, or invalid value logs an error and skips only that sheet.
- Generated field/type name collision.
- Existing Model type missing, abstract, generic, or not [Serializable].
- Existing Model JSON mapping/deserialization failure.
- Two sheets in one collection producing the same field name.
- Two different sheets writing the same JSON source path.

Asset/load
- Global or feature asset cannot be created or loaded.
- JSON .txt missing, unreadable, or its root is not an array.
- A JSON row does not deserialize into the declared row type.
- Global asset cannot assign a feature collection reference.
```

Schema diagnostic format:

```text
[SheetX Collections] Source '<source>', sheet '<sheet>':
Header '<raw header>' (column <1-based column>): <cause> Fix: <repair>
```

Other collection validation keeps collection/path context where relevant. Keyword diagnostics are warnings and skip only that column. Sheet-local errors skip one sheet. Global configuration, a saved Collection binding missing from a processed source, final output/staging, and manual Load Data failures stop their collection batch. Bindings from unprocessed sources do not stop the current export.

## 7. Tests

Extend SheetX's existing Editor-only NUnit suite with focused EditMode tests.
No scene is required.

```text
CollectionPathTests
- Global Resources folder must end in /Resources
- ordinary `Assets/` JSON folder accepted; `Resources` and `StreamingAssets` rejected
- overlapping code/asset/JSON folders rejected

CollectionSchemaTests
- plain-header scalar inference from longest cells
- optional annotation overrides
- scalar arrays
- dotted nested objects
- `[x]` ignored anywhere and keyword path warning
- ignored middle-column source-index alignment
- invalid grammar or explicit annotation
- duplicate path and object/leaf collision reject only offending sheet
- generated name collision

CollectionSettingsTests
- Global immutable
- rename migrates bindings
- delete migrates bindings to Global
- sourceId + sheetName identity
- orphan detection

CollectionGenerationTests
- generated model/collection/path output snapshot
- Existing Model emits a field and no row type
- JSON Only emits no collection source
- generated source never contains UnityEditor or Newtonsoft

CollectionBakeTests
- JSON array bakes into the existing asset field
- no TextAsset reference is serialized
- Global assigns feature asset references
- invalid JSON leaves existing assets unchanged
- a multi-asset write failure restores changed assets

CollectionPlayModeTests
- only Auto Load collections are selected
- no reload while already playing
- an error prevents the requested Play Mode transition
```

`AssetDatabase`-touching tests create fixtures under
`Assets/SheetXTestsTemp/Editor/` and delete them in teardown. They never write
to `Library/` and never touch user files.

## 8. Manual acceptance

1. Feature off. Export an existing workbook. JSON/IDs/Localization output is
   unchanged from today.
2. Feature on. Create `Shop`, bind two sheets as Generated Model, export.
   Editor-only JSON, `SheetXDataCollections.cs`, `ShopConfigCollection.asset`, and a Global
   reference all exist.
3. Enter Play Mode.
   `GlobalConfigCollectionBase.Instance<GlobalConfigCollection>()` returns the
   default Resources asset.
4. `SetInstance(customGlobal)` replaces the root without Resources.
5. Turn Auto Load off for `Mission`. Edit its JSON, export, play. The Mission
   asset keeps the old baked values until `Load Data`.
6. Rename `Shop`. Bindings follow, generated type and paths update, old
   generated files and assets are left in place.
7. Delete `Shop`. Its bindings move to `Global` after a confirmation.
8. Give a Generated Model sheet plain headers with `int`, `float`, `bool`, and
   text values. Export succeeds and generated fields match longest-cell inference.
9. Point an Existing Model binding at a missing type beside a later valid sheet.
   Missing-type sheet logs and skips; later valid sheet writes.
10. Inspect a player build report. No collection JSON `.txt` is present.

## 9. Release scope

- SheetX minor version bump; root `CHANGELOG.md` entry.
- SheetX still cannot be tagged by `.github/workflows/release.yml`, which
  validates a `v*` tag only against `Assets/RevCore/**/package.json`. This design
  does not change that; SheetX release tagging remains an open item.
- Collection metadata is **not** exposed through the detached or batch export
  APIs in the first release. The settings-backed UI workflow ships first; API
  support follows when a real consumer needs it.
