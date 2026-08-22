# Changelog

## [1.0.0] - 2026-08-21

Initial release. Extracted from `Assets/RCore/Main/Editor/Tool/AssetCleaner/` and renamed
from **Asset Cleaner** to **RAsset Filter**.

### Added

- `RAssetFilter` — reverse-reference graph and cache, unused-asset scan, reference lookup,
  GUID/text deep search, asset-size cache.
- `RAssetFilterWindow` — `RCore > RAsset Filter`, plus the `Assets > RAsset Filter` context menus.
- `RAssetFilterSettings`, `RAssetFilterOverlay`, `RAssetFilterLeak`, `RAssetFilterCacheInvalidator`.
- `AssetReferenceTextScanner` — parallel GUID/object-reference text scanner.
- Optional Addressables integration behind the `ADDRESSABLES` define.

### Changed

- Every public type renamed `RAssetCleaner*` → `RAssetFilter*`; namespace is now
  `RCore.RAssetFilter.Editor`. No compatibility aliases — old references must be updated.
- Settings move to the `RCore.RAssetFilter.Settings` EditorPrefs key. An existing
  `RCore.AssetCleaner.Settings` value is read once and copied forward; the old key is left in place.
  The `RAssetCleaner_AutoFindAddrChain` toggle migrates the same way to
  `RAssetFilter_AutoFindAddrChain`.
- Cache file is now `Library/RAssetFilterCache.json`. The old `Library/RAssetCleanerCache.json` is
  ignored and left alone — it is regenerable, so the first scan rebuilds it under the new name.
