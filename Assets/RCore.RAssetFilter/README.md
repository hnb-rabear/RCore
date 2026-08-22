# RAsset Filter

Editor-only asset auditing tool for Unity 2022.3+. Formerly *Asset Cleaner*.

## Install

Package Manager → **Add package from git URL**:

```text
https://github.com/hnb-rabear/RCore.git?path=Assets/RCore.RAssetFilter
```

## Open

`RCore > RAsset Filter`. Right-click context menus live under `Assets > RAsset Filter`.

## Tabs

- **Unused** — scans the project for assets nothing references, with size totals and type
  breakdown. Results are cached; the cache invalidates itself on asset import/move/delete.
- **References** — select an asset, see everything that references it. Enable
  **Deep Search** in Settings to also scan file contents for the asset's GUID, which catches
  indirect references that Unity's dependency graph misses.
- **Leaks** — pick folders or prefabs and see which assets cross the boundary in either
  direction (referenced from outside, or pulled in from outside).

## Settings

Stored per machine in `EditorPrefs` under `RCore.RAssetFilter.Settings`, or in a
`RAssetFilterSettings` asset if the project contains one. Ignore paths, deep-search extensions,
leak-ignore extensions, overlay colors and toggles.

Migrating from Asset Cleaner: an existing `RCore.AssetCleaner.Settings` value is read once and
copied to the new key on first load. The old key is left untouched.

## Addressables (optional)

With `com.unity.addressables` installed, the `ADDRESSABLES` define activates Addressable status
labels, the Addressable/non-Addressable result filter, reference-chain lookup, and
`Assets > RAsset Filter > Select Assets Used by Addressables`. Without the package, those
controls and menus are absent and everything else works unchanged.

## Cache

`Library/RAssetFilterCache.json`, regenerable — delete it and rescan at any time.
