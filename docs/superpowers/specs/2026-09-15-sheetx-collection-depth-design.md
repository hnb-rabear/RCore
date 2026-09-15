# SheetX Collection Depth Design

**Date:** 2026-09-15
**Status:** Approved for planning
**Scope:** `Assets/RCore.SheetX` — Data Config Collections

## Goal

Let a developer choose, per collection, whether its data is stored inside the Global
ScriptableObject or in its own ScriptableObject asset. Today every collection is its own asset;
this adds the inline option for collections that do not need a separate file.

The read path in game code does not change between the two modes.

## Background

As shipped in 1.8.0, `SheetXCollectionSettings.EnsureGlobal` re-homes every binding whose
collection is missing or blank onto the built-in `Global` collection, and
`SheetXCollectionGenerator.CollectionOrder` always inserts `Global` at index 0. Global is
therefore a real collection, not a special "inline" mode, and every non-Global collection today
becomes exactly one `.asset` file referenced from Global.

Generated output for a collection named `Player` currently looks like this:

```csharp
public partial class PlayerConfigCollection : SheetXConfigCollectionBase
{
	public CharactersSX[] Characters;
}

public partial class GlobalConfigCollection : GlobalConfigCollectionBase
{
	public PlayerConfigCollection player;
}
```

## Non-goals

- **Row-level assets (one `.asset` per spreadsheet row).** Considered and deliberately deferred.
  It requires a stable per-row identity contract that SheetX does not have: `persistentFields`
  (`SheetXCollectionSchema.cs:255`) only keeps empty fields from being dropped — it enforces
  neither presence nor uniqueness — and `SheetXCollectionExportSession.ResolveIds`
  (`:154-166`) rewrites symbolic ids such as `HERO_1` into `12` before the baker ever sees the
  JSON, so the human-readable key is already gone. Without that contract, a designer inserting a
  row above another silently repoints existing Prefab references at different data. Row assets
  need their own spec.
- Automatically rewriting references held by Prefabs or Scenes.
- Deleting assets or generated scripts that a depth change leaves behind.

## Design

### Model

One new field on `SheetXCollectionDefinition`:

```csharp
public enum SheetXCollectionDepth
{
	/// <summary>Data is serialized inside the Global asset. No separate asset is created.</summary>
	Inline = 1,
	/// <summary>Data lives in its own ScriptableObject asset, referenced from Global.</summary>
	SeparateAsset = 2,
}

public SheetXCollectionDepth depth = SheetXCollectionDepth.SeparateAsset;
```

`SeparateAsset` is the default, so every existing project keeps its current layout with no
migration. The Global collection has no depth: it is the composition root and is always an asset.

### Generated code

The field on Global keeps the same name and type in both modes. Only the group type's
declaration changes.

`SeparateAsset` (unchanged from today):

```csharp
public partial class PlayerConfigCollection : SheetXConfigCollectionBase
{
	public CharactersSX[] Characters;
}
```

`Inline`:

```csharp
[Serializable]
public partial class PlayerConfigCollection
{
	public CharactersSX[] Characters;
}
```

Both are read the same way:

```csharp
var global = GlobalConfigCollectionBase.Instance<GlobalConfigCollection>();
var characters = global.player.Characters;
```

### What the stable read path does and does not cover

Preserved: the group name, the field name, the row type, and ordinary field access through
Global.

Not preserved: code that treats the group as a `UnityEngine.Object`. Under `Inline` the group is
a plain serializable class, so it has no asset, cannot be dragged into a Prefab field, and does
not inherit `SheetXConfigCollectionBase`.

The two failure modes are not equally visible, and the difference matters:

```csharp
global.player.IsLoaded;                                   // compile error — visible
[SerializeField] private PlayerConfigCollection m_player; // still compiles — silent
```

The second line keeps compiling, because a `[Serializable]` class is a legal serialized field.
Unity reinterprets the field from an object reference to an inline value, drops the reference it
held, and the component wakes up with an empty group. **This is silent data loss, not a visible
failure**, and the migration warning must say so in those terms rather than implying the compiler
catches it.

### Loaded state under Inline

`SheetXConfigCollectionBase.IsLoaded` does not exist on an inline group and is not recreated.
Inline data is serialized inside the Global asset, so it is present exactly when Global is
loaded; there is no window in which the group is loaded and Global is not.

`Auto Load` stays meaningful for `SeparateAsset`. For `Inline` the window shows it disabled with
the note "loaded with Global" — and the baker enforces the same rule, which the UI alone cannot.
`TryBuildCollections` sets an inline collection's `AutoLoad` from the Global definition, not from
the collection's own stored flag.

Without that, a collection carrying `autoLoad = false` from before the switch would be skipped by
the `autoLoadOnly` filters (`SheetXCollectionBaker.cs:242,268`) while Global itself is baked, and
Global would be saved with a stale `player` group. A disabled checkbox in the window does not
prevent this, because the stored value is what the baker reads.

## Editor UI

`SheetXCollectionsWindow` gains a per-collection dropdown:

- **Separate Asset (default)** — own `.asset`, referenced from Global.
- **Inline in Global** — serialized inside the Global asset.

Global shows no dropdown.

Changing the dropdown does not regenerate anything. The change takes effect on the next export.

**How a depth change is detected.** Settings store only the current depth, so "changed" has to be
derived. It is derived from the generated source already on disk: if `PlayerConfigCollection.cs`
declares `: SheetXConfigCollectionBase` and the setting now says `Inline`, that collection is
changing depth — and the reverse. No `lastExportedDepth` field is added; the generated file is
already the record of what was last exported, and keeping one source of truth avoids the two
drifting apart.

The confirmation prompt must be injectable so headless and test runs neither block nor rely on a
dialog.

## Changing a collection's depth

Before writing generated code for a collection whose depth changed, the export shows a
confirmation naming the collection and both modes, and stating that data will be re-baked from
the spreadsheet and that direct references to the old asset will not be migrated.

Order of operations:

1. **Preflight.** Run the existing `SheetXCollectionSettings.Validate` checks plus: every
   collection changing depth has the tables it needs to be re-baked. Abort before any write if
   not.
2. **Snapshot, durably.** Capture the Global asset and the generated sources being replaced.
   This snapshot must survive a domain reload — see "Rollback across the reload boundary" below.
   Global's GUID is never recreated.
3. **Generate, compile, bake.** Existing pending-bake flow: write sources, wait for the domain
   reload, then bake.
4. **Report honestly.** Success is claimed only after the bake succeeds. If compilation or the
   bake fails, the conversion is reported as incomplete, naming the error and the collection, and
   restore is offered from the durable snapshot. Writing the source files is not success.

### Rollback across the reload boundary

A depth change spans a domain reload, and the existing rollback machinery does not.

`SheetXCollectionExportSession.cs:318` captures file snapshots into a local variable consumed by
`RollbackFileOutput` within a single call. The only state crossing the reload is the
`PendingCollectionBakeEntry` in `SessionState` (`SheetXCollectionBaker.cs:134,136`), which holds
the settings asset path, the auto-load flag and the bindings — not the previous sources and not a
Global snapshot. `CreateOrLoadAssets` snapshots Global at `:484`, which runs *after* the reload,
so that snapshot already describes the new type.

Consequence: today's rollback is per-bake, not per-migration. A failure after the reload leaves
the new generated code in place with no route back.

This spec therefore requires a **durable migration snapshot**, written before any source file is
generated and keyed alongside the pending-bake entry, containing:

- the previous generated source files,
- the previous Global asset serialization,
- the depth values in force before the change.

Restore runs in that order — sources first, then let Unity recompile back to the previous types,
then restore the Global asset — because restoring asset data into a type that no longer matches
is what corrupts it. Until the durable snapshot exists, the UI must not offer restore.

### Leftover assets when moving SeparateAsset → Inline

The old `PlayerConfigCollection.asset` is not deleted — consistent with
`SheetXCollectionSettings.DeleteCollection`, which already leaves generated output for a human to
remove.

Keeping the file alone is not enough: the asset's `m_Script` still points at the same MonoScript
GUID, but that script's class no longer derives from `ScriptableObject`, so `GetClass()` returns
null and the Inspector shows "associated script can not be loaded". The YAML data is intact but
unreadable. No crash, no log spam.

Not deleting it is what makes the move reversible. Going back `Inline` → `SeparateAsset` finds the
same file at the same path, and `TryLoadOrCreate` (`:476`) loads it with its original GUID — so
Prefab and Scene references that were never removed start resolving again. Deleting the asset
would forfeit that.

The warning must therefore state two things accurately: the leftover asset is no longer baked and
needs manual migration, and any `[SerializeField]` of the group type still compiles while
silently becoming an empty inline copy, so those fields have to be found and fixed by hand.
SheetX does not delete the asset and does not rewrite references.

## Implementation notes

Three assumptions in `SheetXCollectionBaker` are what `Inline` actually changes:

| Site | Assumption today | Under `Inline` |
| --- | --- | --- |
| `SheetXCollectionBaker.cs:642` | every collection type derives from `SheetXConfigCollectionBase` | inline types do not; this gate rejects them first |
| `SheetXCollectionBaker.cs:296-303` | `Collection` carries name, autoLoad, type | must also carry depth |
| `SheetXCollectionBaker.cs:443` | every non-Global collection gets an asset | inline collections must also be excluded |
| `SheetXCollectionBaker.cs:248` | each collection has its own asset to write rows into | rows are written into a nested property of Global |
| `SheetXCollectionBaker.cs:580,602,607` | every non-Global collection has an entry in `assets` | inline collections have none; `:607` would throw `KeyNotFoundException` |
| `SheetXCollectionBaker.cs:272` | `assets[name].SetLoaded()` exists | no per-group loaded state |

**The first gate is `TryFindCollectionType` (`:642`)**, which rejects any type that is not a
concrete `SheetXConfigCollectionBase`. `TryBuildCollections` (`:296`) calls it for every
definition, so an inline collection fails here before baking starts. It must branch on depth: an
inline group's type is validated as a concrete `[Serializable]` class instead.

`Collection` (`:298-303`) carries name, autoLoad and type but no depth, so every depth branch
below needs that field added first.

`CreateOrLoadAssets` (`:443`) filters `Where(c => !IsGlobal(c.Name))`. That excludes Global only
— an inline collection still reaches `TryLoadOrCreate`, where `ScriptableObject.CreateInstance`
(`:499`) returns null for a non-ScriptableObject type and the bake fails at `:502`. The filter
must exclude inline collections too. With that fixed, `assets` holds no entry under an inline
name, so `:248`, `:272`, and the three sites in `ApplyGlobalReferences` (`:580`, `:602`, `:607`)
must skip inline collections rather than index into `assets`.

`ApplyRows` currently takes the target asset and calls `FindProperty(fieldName)` at the root. It
gains a property-path prefix: empty for `SeparateAsset`, `"player."` for `Inline`.
`SerializedObject.FindProperty("player.Characters")` resolves nested paths as-is.

The `JsonConvert.PopulateObject` call at `SheetXCollectionBaker.cs:564-565` wraps one more level
for `Inline`:

```csharp
// SeparateAsset: {"Characters":[...]} applied to the feature asset
// Inline:        {"player":{"Characters":[...]}} applied to Global
```

Newtonsoft's default is `ObjectCreationHandling.Auto`, which reuses a non-null instance and
constructs one when the field is null. Both cases are needed here: an existing Global has
`player` populated and is updated field by field, while a freshly created Global has `player`
null and gets an instance built. Either way `PopulateObject` only writes the keys present in the
JSON, so sibling groups already serialized in Global are untouched.

`SheetXCollectionGenerator.AppendCollection` (`:416`) always emits
`: SheetXConfigCollectionBase`; `Inline` emits `[Serializable]` with no base type. The Global
feature field loop (`:464-469`) is unchanged — `public PlayerConfigCollection player;` is correct
for both modes. Type-name collision checks in `Validate` (`:207`) already cover inline groups
because `collectionTypes` is keyed on the generated type name regardless of base type.

Two generator details that a plan would otherwise get wrong:

**Keep one file per collection type in both modes.** `EmitFiles` (`:87-98`) writes
`<TypeName>.cs` per collection. If `Inline` instead folded the group into
`SheetXDataCollections.cs`, the previous `PlayerConfigCollection.cs` would stay on disk —
`DeleteLegacyGeneratedSource` (`SheetXCollectionExportSession.cs:427`) only removes the one
hardcoded legacy filename — and the project would fail to compile with CS0101, duplicate type.
The pending bake would then hang behind a compile error. So `Inline` keeps emitting
`PlayerConfigCollection.cs`; only the declaration inside it changes.

**`using System;` must be present for inline files.** `BeginSource` (`:91`) computes
`includeSystem = isGlobal && configuration != null && ...`, so a non-Global file never gets it
today and `[Serializable]` would not resolve. Either pass `includeSystem: true` for inline
collections or emit `[System.Serializable]`.

### Rollback: per-bake is covered, per-migration is not

Two different failures need two different mechanisms, and only the first is already built.

**A bake that fails without a depth change** needs no new machinery. `Inline` creates and deletes
no assets; it writes only into Global, which `CreateOrLoadAssets` already snapshots (`:484`), and
`Rollback` (`:616-623`) restores via `EditorJsonUtility.FromJsonOverwrite`. No `deletedPaths` list
and no `.meta` backup are needed — a direct consequence of deferring row assets.

**A bake that fails after a depth change** is not covered, because the failure is on the far side
of a domain reload from the state that must be restored. That is the durable migration snapshot
described under "Rollback across the reload boundary", and it is new work this spec requires.

## Testing

Existing `CollectionGenerationTests` and `CollectionBakeTests` cover `SeparateAsset`; they must
stay green unmodified, which is the regression gate for the default path.

New cases:

1. `Inline` still emits `PlayerConfigCollection.cs`, declaring a `[Serializable]` class with no
   base type, and the file carries `using System;` (or `[System.Serializable]`).
2. `Inline` bake populates `global.player.Characters` and creates no `PlayerConfigCollection.asset`.
3. `Inline` bake of one group leaves sibling groups already in Global byte-for-byte unchanged.
4. A failure mid-bake under `Inline` restores Global to its pre-bake state.
5. Moving `SeparateAsset` → `Inline` leaves the previous asset file on disk.
6. Preflight rejects a depth change when the collection has no tables to re-bake.
7. A depth change whose bake fails *after* the domain reload restores from the durable snapshot:
   the previous sources are back, the previous types recompile, and Global's data and GUID match
   their pre-migration state. This is the case the per-bake rollback cannot reach, so it is the
   one that proves the durable snapshot works.
8. **Mixed mode** — one `Inline` collection and one `SeparateAsset` collection in the same
   settings: Global ends up holding a nested group and an object reference side by side. This is
   the feature as it will actually be used, and no other case covers it.
9. Configuration and an inline group bake together without clobbering each other, in both orders
   relative to `PopulateObject(configuration.Json, global)` (`SheetXCollectionBaker.cs:238`).
10. A fresh project with no `GlobalConfigCollection.asset` and a collection set to `Inline`:
    the asset is created and `player` is built from null.
11. Round trip `SeparateAsset` → `Inline` → `SeparateAsset` reuses the original asset file and its
    GUID rather than creating a new one.
12. An inline collection whose stored `autoLoad` is `false` is still baked when Global's
    `autoLoad` is `true` under `autoLoadOnly`.

Tests do not compile generated code at run time, so cases 2-4 and 8-12 need a hand-written
fixture — a Global type plus an inline group type in their own namespace, in the style of the
existing `Assets/RCore.SheetX/Tests/MarkerAbsent/` fixture.

## Files affected

- `Assets/RCore.SheetX/Editor/SheetXSettings.cs` — `SheetXCollectionDepth`, `depth` field.
- `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionGenerator.cs` — emit inline groups.
- `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionBaker.cs` — depth on `Collection`,
  depth-aware `TryFindCollectionType`, nested write path, skip asset creation, reference
  assignment and `SetLoaded` for inline groups, inline `AutoLoad` from Global.
- `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionExportSession.cs` — durable migration
  snapshot across the reload boundary.
- `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSettings.cs` — depth-change preflight.
- `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionsWindow.cs` — depth dropdown, disabled
  `Auto Load` for inline.
- `Assets/RCore.SheetX/Tests/CollectionGenerationTests.cs`,
  `Assets/RCore.SheetX/Tests/CollectionBakeTests.cs` — cases above, plus a new hand-written
  inline fixture type alongside them.
- `Assets/RCore.SheetX/Document/Document.md`, `Assets/RCore.SheetX/CHANGELOG.md`, root
  `CHANGELOG.md`.

Not touched: `SheetXCollectionExportSession` atomic flush, `ResolveIds`, and the runtime base
classes in `Assets/RCore.SheetX/Runtime`.

## Review record

Two independent critiques were run against the original three-tier sketch (Global → collection →
row).

**Fable** identified the single highest risk: row-level assets have no stable identity across
re-exports. A row asset named by position means inserting a spreadsheet row silently repoints an
existing Prefab reference at different data — valid reference, wrong item, no error. Deleting and
recreating the asset instead changes its GUID and breaks the reference outright.

**Sonnet**, reading the implementation, confirmed the identity problem at
`SheetXCollectionExportSession.cs:154-166` and added: the projection property needed to keep the
row-asset API stable allocates on every access; `SheetXCollectionBaker.cs:549,555,564` binds to
public fields by name, which a getter-only property defeats; and rollback
(`:612-626`) tracks created paths but has no way to restore a deleted asset's GUID.

One Sonnet claim was checked and rejected: it read depth 1 and 2 as already existing and selected
per-sheet via a blank `collectionName`. `EnsureGlobal` (`SheetXCollectionSettings.cs:187-188`)
rewrites blank names to `Global`, so everything today is one uniform mode. Per-collection depth
does not conflict with the settings model.

Both critiques converge on row assets as the risk, which is why this spec ships the inline/asset
choice and defers row assets to their own spec.

A second round reviewed this document itself and found four defects in it, all now fixed:

- **Fable:** the spec promised a restore path after a failed depth change, but rollback state does
  not cross the domain reload — `SheetXCollectionExportSession.cs:318` holds snapshots in a local,
  and the only state surviving the reload is the pending-bake entry
  (`SheetXCollectionBaker.cs:134,136`). Hence the durable migration snapshot section.
- **Sonnet:** `TryFindCollectionType` (`:642`) rejects any non-`SheetXConfigCollectionBase` type
  and runs for every definition via `TryBuildCollections` (`:296`) — the first gate an inline
  collection hits, and originally absent from this spec. The claim that the `:443` filter already
  excluded inline collections was also wrong; it excludes Global only.
- **Sonnet:** the spec claimed a `[SerializeField]` of the group type breaks at compile time. It
  compiles and loses data silently, which is the more dangerous case and is now described as such.
- **Sonnet:** folding inline groups out of their own `.cs` file would strand the previous
  `PlayerConfigCollection.cs` and fail the build with CS0101, since
  `DeleteLegacyGeneratedSource` (`SheetXCollectionExportSession.cs:427`) removes only one
  hardcoded filename.
