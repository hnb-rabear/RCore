# RAsset Filter and RHierarchy package extraction design

**Date:** 2026-08-21
**Status:** Approved design

## Goal

Extract two legacy editor tools into independent Unity 2022.3 packages:

- Rename Asset Cleaner to RAsset Filter.
- Move RHierarchy into its own package.
- Keep legacy `AssetCatalogWindow` compiling and working when RAsset Filter is absent.
- Keep each new package installable without legacy RCore.

## Package layout

```text
Assets/
  RCore.RAssetFilter/
    package.json          com.rabear.rcore.assetfilter
    CHANGELOG.md
    README.md
    Editor/
      RCore.RAssetFilter.Editor.asmdef
      (7 moved .cs files, flat)
    Tests/
      RCore.RAssetFilter.Tests.asmdef

  RCore.RHierarchy/
    package.json          com.rabear.rcore.rhierarchy
    CHANGELOG.md
    README.md
    Editor/
      RCore.RHierarchy.Editor.asmdef
      (3 moved .cs files, flat)
    Tests/
      RCore.RHierarchy.Tests.asmdef
```

Both start at `1.0.0`, target Unity `2022.3`, editor-only (`includePlatforms: ["Editor"]`), and declare no dependency on legacy RCore. Test asmdefs copy [`RCore.SheetX.Tests.asmdef`](../../../Assets/RCore.SheetX/Tests/RCore.SheetX.Tests.asmdef): Editor platform, `UNITY_INCLUDE_TESTS`, `nunit.framework.dll` precompiled reference.

Flat `Editor/` folders, matching RCore.SheetX. Ten files total do not need a subfolder taxonomy.

## RAsset Filter

### Identity

| Old | New |
| --- | --- |
| `RAssetCleaner*` types | `RAssetFilter*` |
| `AssetCleaner` in identifiers | `AssetFilter` |
| `RCore.Editor.AssetCleaner` | `RCore.RAssetFilter.Editor` |
| `RCore/Asset Cleaner` menu | `RCore/RAsset Filter` |
| `Assets/Asset Cleaner/...` menus | `Assets/RAsset Filter/...` |
| `Asset Cleaner` in dialogs/logs | `RAsset Filter` |

Assembly `RCore.RAssetFilter.Editor`. Menu priority becomes literal `23` (was `RMenu.GROUP_2 + 3`), removing the only legacy RCore dependency in the tool. Asset-context menu priorities `2000`/`2001` unchanged.

No compatibility type aliases for old names. The code leaves frozen legacy RCore, so old compile-time references break by design.

### Behavior

All seven files move otherwise unchanged: dependency graph and reverse-reference cache, unused scan, GUID/text deep search, Project window overlay, folder/prefab leak checker, `AssetPostprocessor` cache invalidation, settings.

### Addressables

Follow the existing repo pattern from [`RevCore.Tools.Editor.asmdef`](../../../Assets/RevCore/Tools/Editor/RevCore.Tools.Editor.asmdef): reference `Unity.Addressables` and `Unity.Addressables.Editor`, and declare a `versionDefines` entry for `com.unity.addressables` defining `ADDRESSABLES`. Addressables code stays behind `#if ADDRESSABLES`, as it already is.

`AddressableEditorHelper.IncludedInBuild(string)` lives in legacy RCore. Its body is two Addressables API calls plus an "Excluded Content" group check ([AddressableEditorHelper.cs:170-181](../../../Assets/RCore/Main/Editor/AddressableEditorHelper.cs#L170-L181)). Copy that one method into the package as a private static helper rather than depend on legacy RCore for it.

Without the Addressables package, `ADDRESSABLES` is undefined and the Addressables selection controls, filters, and asset menus are excluded. Scan, reference finder, leak checker, and overlay still work.

### Settings migration

Settings are the only persisted state worth migrating: they are user config, unlike the cache.

New key: `RCore.RAssetFilter.Settings`, replacing `RCore.AssetCleaner.Settings`. New toggle key `RAssetFilter_AutoFindAddrChain`, replacing `RAssetCleaner_AutoFindAddrChain`.

On load, if the new key is absent and the old key exists, read the old value, write it under the new key, and leave the old key in place. Existing JSON parse failure handling is unchanged: invalid data falls back to defaults.

The cache file is **not** migrated. `Library/RAssetCleanerCache.json` is regenerable, gitignored, and rebuilt by the next scan. New paths are `Library/RAssetFilterCache.json` plus its existing `.tmp` and `.invalid` siblings; the old file is ignored and left alone.

## RHierarchy

Move `RHierarchy.cs`, `RHierarchySettings.cs`, `RHierarchySettingsWindow.cs` into the package. Namespace and assembly become `RCore.RHierarchy.Editor`. Menu path `RCore/RHierarchy Settings` unchanged; priority becomes literal `26` (was `RMenu.GROUP_2 + 6`).

All ten `RHierarchy_*` EditorPrefs keys stay as they are — the tool's public identity is not changing, so there is nothing to migrate.

Row drawers (separator, MonoBehaviour icon, visibility, vertices, child count, tag, layer, components, static) move unchanged. No dependency on legacy RCore, RevCore, Addressables, or RAsset Filter.

## Legacy AssetCatalog bridge

[AssetCatalogWindow.cs:606-622](../../../Assets/RCore/Main/Editor/GeneralAssetLinker/AssetCatalogWindow.cs#L606-L622) also constructs `AssetReferenceTextScanner.ObjectReferenceTarget` values and reads its `AllTargetScanResult`, besides calling `RAssetCleaner.BuildCache()` and `RAssetCleaner.GetCachedReferencingAssets(paths)`. That file stays in legacy RCore.

An asmdef reference cannot be used: Unity asmdef references are mandatory, so referencing an absent `RCore.RAssetFilter.Editor` breaks the whole `rcore.editor` assembly for every consumer project that does not install the package. `versionDefines` gates compilation symbols, not references, and cannot make a reference optional.

So one small reflection helper in legacy RCore resolves and caches the Filter assembly/types/methods once. It invokes existing `RAssetFilter.BuildCache()` and `GetCachedReferencingAssets(IEnumerable<string>)`. For the scanner, it creates the existing runtime `ObjectReferenceTarget[]` through its public constructor, invokes `ScanAllObjectReferences`, then reads existing `pathsByTargetId` and `skippedPaths` result fields. No wrapper type, duplicate scanner, or new Filter bridge API is added.

When the package is absent or any lookup/invocation fails, Direct Usage reports that RAsset Filter is required, returns `false`, and leaves its index unchanged. It must not throw and must not affect other AssetCatalog features.

## Verification

### Automated EditMode tests

`RCore.RAssetFilter.Tests`:

- old settings key migrates to the new key on load;
- `AssetReferenceTextScanner.ScanAllObjectReferences` returns exact and GUID-only matches, and reports unreadable input paths.

`RCore.RHierarchy.Tests`:

- component order persists and restores through settings.

Tests restore any EditorPrefs keys they touch and write scan fixtures to a temp folder they delete afterwards.

### Manual matrix

1. Empty Unity 2022.3 project, RAsset Filter only: compiles; window opens; scan, reference search, deep search, leak scan, overlay all work.
2. Same project with Addressables installed: Addressables controls and asset menus work.
3. Same project without Addressables: core features work; Addressables controls absent; no compile errors.
4. Empty project, RHierarchy only: compiles; settings window; hierarchy row decorations and toggles.
5. Existing RCore project *without* RAsset Filter: `rcore.editor` compiles; Direct Usage explains the package requirement; rest of AssetCatalog works.
6. Existing RCore project *with* RAsset Filter: Direct Usage returns reverse-reference candidates as before.
7. Project with old Asset Cleaner settings in EditorPrefs: values survive into RAsset Filter.

## Documentation and release scope

Each package gets its own README and CHANGELOG. Root `CHANGELOG.md` records the extraction. `CLAUDE.md` gains a row per package in the code-base table and a note that legacy AssetCatalog Direct Usage now depends on RAsset Filter at runtime.

`.github/workflows/release.yml` is unchanged. It validates `v*` tags against `Assets/RevCore/**/package.json` only, so neither package is taggable through the current workflow — the same known gap RCore.SheetX has.

Out of scope: RevCore conversion and its doc-coverage/PublicAPI/tag gates; release-workflow redesign; UI redesign; new features in either tool; extracting other legacy editor tools.
