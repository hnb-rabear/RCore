# Changelog

## [1.6.0] - 2026-11-26

### Changed

- **Replace Sprite → Write Old pivots to .tps…** now writes through the **RCore Atlas Migration** package (`com.rabear.rcore.atlasmigration`), found by reflection. It edits the `.tps` as XML text and keeps every other setting and the file's line endings, writes 9-slice borders too, and turns on `writePivotPoints`. The sprite-name match needs exactly one key (before, the first match won), and the error box lists every Old sprite with no key. Without the package, the button is disabled and shows the install URL.
- The Format dropdown and the "Match numbered frames" option are removed; the `.error.txt` file is no longer written. `SpriteReplacer.Tps`, `GetDirective` and the saved fields stay.

### Fixed

- Writing pivots no longer drops `.tps` settings the old parser did not know (`<true/>`, `<int>`, `<enum>`), and no longer fails on non-integer `<double>` values.
- Pivots written through the package are read from the full sprite rect, so Tight-mesh sprites with transparent margins keep the right pivot (before, the window read it from the mesh bounds).

## [1.5.0] - 2026-11-25

### Changed

- **Asset path in importer headers** is drawn through `Editor.finishedDefaultHeaderGUI` instead of a `TextureImporter` custom editor, so the importer's Apply/Revert footer is back. `TextureImporterPathPreviewEditor` is removed.

- **Find And Replace Assets**: Replace Sprite, Replace Object, Replace Font and Update Image Property share one Scope → Scan → Preview → Apply flow. Scan lists every asset that would change; untick rows to skip them. Apply is disabled when inputs change after a scan, and files open in a scene or prefab stage are locked.
- **Replace Sprite**: columns are now Old (in use) → New (replacement), in three steps: Pairs, Pivots & TexturePacker, Replace. Drop zones accept sprites, textures, prefabs and folders. New "Unmatched only" filter and name search.
- **`EditorAssetUtil.SearchAndReplaceGuid`** scans on every call. `BuildReferenceMapCache` is obsolete and does nothing.

### Added

- **EdgeAlignedHorizontalLayoutGroup — Middle Bias** (-1..1 slider): -1 packs the middle children against the first child, 1 against the last, using Spacing as the gap (capped at the equal gap); 0 keeps the equal spread.
- `EditorAssetUtil.ReplaceInText`, `FindGuidReferences`, `ReplaceGuidReferences`, `BuildGuidRefs`, `GetGuidRef`.
- **Spine Edit Mode preview** for `SkeletonAnimation` and `SkeletonGraphic` (`rcore.editor.spine`, needs `SPINE_ANIMATION`). `SpineDefineSymbolSetup` adds that define when Spine is in the project.

### Fixed

- Asset Catalog usage cache moves to `Library/AssetCatalog_Usages_<guid>.json`, so a catalog under `Resources/` no longer ships it in the build.
- `JustButton.CheckPerfectRatio` uses the Rect size instead of `sizeDelta`.
- `GeneralSpriteLinker` no longer crashes on a re-entrant import in batch mode.
- Sprite path preview: Transfer Pivots NRE and blank previews.

- Replace Object / Replace Font threw `KeyNotFoundException` after a Replace Sprite run (stale static cache).
- Replace counts in logs no longer add up across objects; the progress bar always closes.
- Write Old pivots to .tps finds the sprite list by key instead of a fixed index, and shows an error instead of throwing.
- Replace Font in the open scene supports Undo and marks the scene dirty.
- Update Image Property keeps its inputs after recompiling and skips Images without a sprite.
- Cut Sprite Sheet keeps row edits and no longer throws on an empty row.
- Tab inputs are saved to `FindAndReplaceAssetToolkit.asset` when the window closes (before, they were lost when Unity restarted).

### Removed

- `GenericObjectsReplacer<T>` (unused).

## [1.4.0] - 2026-08-18

### Added

- **UI - PanelStack**: Added UniTask addressable helpers for pushing panels, pushing panels to the top, queueing panels, and pushing panels to a root.
- **General Asset Linker - `GeneralSpriteLinker`**: Added `PixelPerUnit` proxy for its linked `Image`.
- **UI - `AddressableImage`**: Added `AutoActive` option for controlling automatic sprite loading.
- **UI — AddressableImage inspector**: Supports multi-object editing with a shared **Preserve Aspect** toggle that dirties each `Image` and repaints canvases and scene views.
- **`RPlayerPrefDict<TKey, TVal>`**: Added `ContainsKey` and `TryGetValue`; `Contain` now delegates to `ContainsKey`.

### Fixed

- **General Asset Linker - `GeneralSpriteLinker`**: `Reset()` now caches its `Image` before reading `Image.sprite`, so a newly added component picks up the matching catalog key.
- **UI — AddressableImage inspector**: No longer captures sprite references on enable and repaint; it only refreshes the editor preview, leaving capture to the explicit editor capture/migrate tooling.
- **UI — `AddressableImage`**: Auto-load moved back to `OnEnable` so pooled or re-enabled objects retry their sprite load.
- **UI — PanelStack**: Addressable panel loads now report ordinary failures as `null` instead of throwing, while cancellation still propagates to the caller.
- **General Asset Linker - `GeneralSpriteLinker`**: `Refresh()` no longer clears the serialized `Image.sprite` without an undo entry, and only adopts a key the catalog knows.
- **Inspector - `[FolderPath]`**: Folders picked outside the project's `Assets` folder are rejected with an error instead of producing a corrupt path.
- **`RPlayerPrefDict<TKey, TVal>`**: Dirty flag now tracks real mutations only — equal assignments are skipped, a null collection assignment falls back to an empty dictionary, `Clear()` no-ops when already empty, and `SaveChange()` returns early when unchanged so an untouched empty collection no longer deletes its `PlayerPrefs` key.
- `[AutoFill]` now fills null references and empty arrays/lists automatically after Inspector drawing, processing each selected object independently and avoiding repeated scene or AssetDatabase searches.

## [1.3.0]

### Added

- **General Asset Linker — Catalog workflow**: Searchable, sortable asset grid with multi-select category editing, path display, quick add, sprite activation control, direct-reference usage indexing, and prefab linker usage scanning.
- **General Asset Linker — Restore workflow**: Restore catalog-linked audio, sprite, texture, and `SpriteRenderer` references in prefabs, preview affected linkers before changes, and report skipped or failed assets.
- **General Asset Linker — `GeneralSpriteRendererLinker`**: New runtime component resolves `SpriteRenderer.sprite` by catalog key; supports automatic component activation and exposes `Refresh()`.
- **Inspector previews**: Show source asset paths and sprite regions for sprites, textures, and UI Images.
- **UI — AddressableImage**: `Image` companion component that loads its sprite from an `AssetReferenceSprite` via UniTask and releases the handle on destroy. Editor tooling captures the assigned `Image.sprite` into Addressables, previews it in the inspector, migrates existing `Image` components, and strips serialized sprite references at build time.
- **Asset Cleaner — Incremental cache**: Detect imports, moves, and deletions through an asset postprocessor; remap cached references on moves; persist versioned cache state; and scan serialized text references for direct usage results.

### Changed

- **AddressableImage editor tools**: Moved sprite capture and migration code into `rcore.editor`, preserving runtime/editor assembly separation while retaining Addressables editor integration.
- **Asset Cleaner**: Replaced duplicate RevCore editor implementation with RCore.Main tooling.

## [1.2.0]

### Added

- **General Asset Linker**: Key-based asset catalog (`AssetCatalog` ScriptableObject) for sprites, textures, and audio clips with lazy dictionary cache and no hard prefab references. Runtime linker components (`GeneralSpriteLinker`, `GeneralTextureLinker`, `GeneralAudioLinker`) resolve assets by string key at runtime. Editor tooling includes a catalog management window (add/delete/rename/categorize/Addressables marking), a relink panel for batch-migrating direct asset references in prefabs to catalog-key linker components, a usage report panel, and an asset picker window.

## [1.1.9]

### Added

- **Asset Cleaner — Leak Checker**: New tab that detects cross-boundary asset dependencies — assets inside a selection referenced from outside (Leaked In) and external assets pulled in as dependencies (Leaked Out). Configurable ignore extensions in settings.
- **UI — MirrorFlipImage**: Image subclass supporting horizontal flip and edge-preserving mirror (Horizontal / Vertical / Both) with sliced-sprite support and a context-menu swap between Image and MirrorFlipImage.
- **UI — EdgeAlignedHorizontalLayoutGroup**: Layout group that pins first and last children flush to container edges with equal gaps between remaining children. Rebuilds immediately in Edit mode.
- **UI — ProgressBar marker**: Optional marker RectTransform that tracks the current fill ratio along all four fill directions.

## [1.1.8]
### Added
- **JObjectDB — `Reset(key)` API**: Clears a single collection's data back to its type's default values while keeping the key registered in the master index, then persists and flushes (`PlayerPrefs.Save`). Resets the live in-memory instance when the collection is loaded.
- **JObjectDB Editor — Per-collection Reset & Delete**: Added left-panel per-row buttons (with confirmation) to reset a collection to defaults or delete it outright; the collection tree refreshes afterward.
- **JObjectDB Editor — Global Search**: Added a search-scope toggle (Current / All). In *All* scope the window scans every collection, filters the left panel to those containing a match (the selected collection stays visible) and shows an `n/total match` count in the header; in-tree highlights are recomputed when a matching collection is selected.

### Changed
- **JObjectDB — Persistence flush**: `Delete` and `DeleteAll` now call `PlayerPrefs.Save()` so removals are committed to disk immediately.
- **JObjectDB Editor — Lazy parsing**: Collections are now parsed on demand rather than all upfront, and `OnFocus` no longer reloads the database over unsaved edits.
- **JObjectDB Editor — Internal refactor**: Split the monolithic `JObjectDBWindow` into focused partial classes (core + Diff / Import / Layout / Presets / Search / Tree) for maintainability; behavior preserving.
- **JObjectDB Editor — Unified asset-create drawer**: Merged three near-identical "Create asset" property drawers into a single `JObjectCreateAssetDrawer`.

## [1.1.7]
### Added
- **JObjectDB — Dependency Injection**: Introduced `[Inject]` attribute and reflection-based DI system in `JObjectModelCollection`. `InjectDependencies()` auto-wires model fields after creation; `Get<T>()` resolves models or their data by type/interface with O(1) cached lookups.
- **JObjectDB — ISessionModel Interface**: Extracted `ISessionModel` interface from `SessionModel` to allow decoupled access to session metrics (days, streaks, counters) via the DI system.
- **Unified Font Replacer**: Merged legacy `FontReplacer` and `TMPFontReplacer` into a single `UnifiedFontReplacer` editor window supporting both TMP and Legacy font replacement with folder-scoped project scanning and active scene replacement.
- **PanelController — Session Show Count**: Added per-session popup display counter with static `GetSessionShowCount<T>()` API for frequency-based display logic.
- **PanelRoot — Panel Lifecycle Events**: Added `onAnyPanelShow` and `onAnyPanelHide` action callbacks for global panel visibility tracking.

### Changed
- **EventDispatcher**: Replaced string-hash-based event keying (`GetStableHashCode`) with direct `System.Type` keys for type-safe, collision-free event dispatch.
- **ScreenSafeArea / IgnoreScreenSafe**: Fixed coordinate mismatch in Unity Editor Device Simulator by detecting resolution discrepancies; replaced `Screen.currentResolution` and `canvas.pixelRect` with `Screen.width/height` for consistent anchor calculations. `IgnoreScreenSafe` now respects parent `fullTop`/`fullBottom` flags.
- **TimeHelper**: Time-cheat offsets (day/hour/minute) now support negative values for backward time simulation.
- **SessionModel**: Fixed `OnApplicationPause` to save `lastActive` timestamp on pause (not resume), preventing stale offline-time calculations.
- **NameGenerator**: Disabled Arabic and Thai name generation to reduce build size for projects that don't embed those character sets.
- **OptimizedVerticalScrollView**: Unified initial scroll positioning to always use `ScrollToIndex` for consistent behavior.
- **JObjectDB Editor**: Improved field rendering with inline array display, configurable key-column width, and streamlined copy/paste JSON editing.
- **PanelController.Lock**: Changed to `virtual` to allow override in subclasses.

## [1.1.6]
### Added
- **JObjectDB Editor — Deep Search**: Search now recursively scans the entire JToken tree instead of only root-level field names. Matched paths are highlighted in yellow; all ancestor nodes are auto-expanded to reveal results.
- **JObjectDB Editor — Auto-backup on Play**: Subscribes to `EditorApplication.playModeStateChanged` and automatically creates a timestamped backup to `Saves/` before entering Play mode. Rotates to keep the latest 5 auto-backups.
- **JObjectDB Editor — Collection Presets**: Save and load named snapshots of the entire database to `Saves/Presets/`. Preset panel in the left column supports Save, Load, and Delete operations. Presets are stored as plain JObjectDB JSON (no metadata wrapper).
- **JObjectDB Editor — Diff / Compare**: Compare the active collection against any saved JSON file. Added/changed/removed fields are highlighted in green/orange/red respectively; parent nodes display a diff indicator when any descendant has changed. Diff state uses precomputed `HashSet` for O(1) per-node lookup.

### Changed
- **JObjectDB Editor — Persist UI State**: Selected collection, left-panel width, and expanded tree nodes are now persisted across sessions via `EditorPrefs` (keys prefixed `JObjectDB_`). State is saved on `OnDisable` and restored on `OnEnable`.
- **JObjectDB Editor — Import button**: Icon updated from Reload to Download (`Download-Available`) for clearer affordance; opens file picker defaulting to the `Saves/` directory and supports both wrapped export format (with device/version metadata) and raw JObjectDB dictionary format.

## [1.1.5]
### Added
- **Core Hub**: Centralized, modular, icon-driven hub (`Ctrl+Alt+/`) encompassing legacy utilities (Scenes Navigator, Tools Collection, Asset Shortcuts, etc.) and categorized by purpose (Mesh, General, Navigation).
- **Asana Task Sync**: Built-in tool to automatically push markdown task lists to Asana as hierarchical subtasks.
- **OptimizedVerticalScrollView**: Added smooth scroll animation support powered by DOTween (`ScrollToIndex`, `ScrollToTop`, `ScrollToBot`).

### Changed
- **NameGenerator**: Cultural auto-localization updates to ensure immersion. Enforced two-part names for Asian regions to prevent display errors and restricted humorous placeholder names strictly to English and Latin language regions.
- **OptimizedVerticalScrollView**: Enhanced performance by caching `Vector3` array allocations for viewport and item corner geometry calculations to prevent GC spikes during rapid scrolling.
- **CustomPool**: Stabilized batch release operations by rigorously clearing null remnants and optimizing list capacity augmentations in `ReleaseAll()`.
- **Legacy Editor Tools**: Migrated older developer windows into the new generalized `RCoreHubWindow` to consolidate the UX and reduce menu strip clutter.

## [1.1.4]
### Fixed
- **TimeHelper**: Replaced `Time.unscaledTime` with `Time.realtimeSinceStartup` in `GetNowTimestamp` to ensure the cached timestamp refreshes correctly after the app is suspended/resumed. Added `ResetCache()` method to force an immediate timestamp refresh.
- **WebRequestHelper**: Improved error handling in `RequestUtcTime` with proper try-catch-finally to prevent silent failures and ensure `m_RequestingServerTime` flag is always reset.
- **JObjectDBManager**: Added `TimeHelper.ResetCache()` call on app resume (`OnApplicationPause`) so the real-time clock is re-synced immediately when users return to the app.
- **PanelStack**: Added null check in `PushPanelToTop` to prevent `NullReferenceException` when a null panel is pushed.

### Changed
- **IJObjectHandler**: Changed `OnRemoteConfigFetched` from `abstract` to a default interface method for simpler implementation.

## [1.1.3]
### Added
- **UI Enhancements**:
  - Implemented zebra striping for better readability in both Cleaner and Reference Finder tabs.
  - Added header rows with columns (Icon, Path, Size, Action).
  - Added "Delete" button to quickly remove unused assets directly from the list.
  - Added "Reload" button to manually refresh cache from disk.
- **Persistence**:
  - Auto-save: Scan results are automatically saved to `Library/RAssetCleanerCache.json`.
  - Auto-load: Results are restored when opening the window or after recompilation.
- **Performance**:
  - Optimized Type Statistics calculation with progress bar and cancellation support.
  - Implemented `SizeCache` to reduce disk I/O during scans.
  - Batched operations in list rendering to improve scrolling performance with large datasets.

### Changed
- **Reference Finder**:
  - Aligned UI style with the Cleaner tab.
  - "Scan Project" button now calls `FindUnusedAssets` to ensure full cache consistency and saves the result.

### Fixed
- **Pagination**: Fixed an issue where changing "Items per Page" did not refresh the list view.

## [1.1.2]
### Changed
- **Asset Cleaner**: Major performance optimization.
    - Implemented high-performance Rect-based rendering and lightweight labels, achieving zero-overhead scrolling even with thousands of assets.
    - Added pagination (50 items/page) and caching for asset icons/sizes.
    - Applied optimization to both "Cleaner" and "Reference Finder" tabs.
    - Removed default settings for ignore paths.
    - Simplified UI by removing redundant navigation buttons in Reference Finder.
    - **Deep Search**: Optimized with Parallel Processing (multi-threaded scanning) for significantly faster result.
    - **Configuration**: Added `Deep Search Extensions` setting to allow configuring which file types to scan.
    - **Reference Finder**:
        - **Detail View**: Added foldout to show exact property usage (e.g. `MeshRenderer.m_Materials`).
        - **Scene Inspection**: Added support for inspecting references in unopened scenes via "Load Additively" button.
        - **Smart Filtering**: Automatically hides internal Prefab linkages (`CorrespondingSourceObject`) to reduce noise.
        - **Replacement**: Added inline object field to instantly replace references.
        - **Fixes**: Improved reference detection for Texture/Sprite mismatches and Sub-Assets.

## [1.1.1]
### Added
- **Asset Cleaner**: A toolset to optimize project assets by finding unused files and tracing dependencies.
    - **Project Cleaner**: Identifies unused assets, showing folder statistics and unused size. Added smart filtering (hides empty types), live count/size per type, and selectable paths.
    - **Reference Finder**: Traces incoming references with history navigation. Added **Deep Search** mode to find indirect references (Addressables, wrappers) by scanning file contents.
    - **Performance**: Optimized via persistent dependency caching. "Scan Project" now auto-calculates type stats.
    - **UX**: "Ping" button to locate assets. Separated "Red Overlay" and "Show Size" settings; overlays now auto-hide when the tool window is closed.

### Changed
- **RHierarchy**: Added detection and `[?]` icon for missing scripts on GameObjects.

## [1.1.0]
### Added
- **RHierarchy**: Added a lightweight hierarchy enhancer tool
    - Features:
        - **Visibility**: Quick toggle for GameObject active state.
        - **Static**: specific "S" indicator; click to toggle static flags.
        - **Components**: Icons for attached scripts and components (with enable/disable toggle). Deactivated components are visually grayed out.
        - **Info**: Displays vertices count and children count.
        - **Tag & Layer**: Display and edit tags and layers directly in the hierarchy.
    - **Customization**:
        - Fully reorderable component display.
        - appearance settings (colors, shading, separator lines).
        - Global enable/disable toggle.

### Changed
- **AddressableAssetsGroupsColorizer**: Replaced "Toggle Group Colorizer" menu item with a boolean field `enabled` in `AddressableAssetsGroupsColorizerSettings`.

## [1.0.9]
### Changed
- **NameGenerator**: Major refactor for performance and realism.
    - Optimized memory usage by moving local arrays to static readonly fields.
    - Updated name lists to use realistic names for all supported languages (EN, RU, CN, JP, KR, AR, VN, TH).
    - Added `LogAllCharacters` utility to log all unique characters used in name arrays for font generation/localization checks.

## [1.0.8]
### Added
- **AssetGUIDRegenerator**: Added a new editor tool (`Assets -> RCore -> Regenerate GUID`) to regenerate GUIDs for files and folders.

## [1.0.7]
### Changed
- **PanelController**: Refactored animation transition to be configurable via `Configuration` settings, removing hardcoded paths.
- **Examples**: Moved `RCore/Examples` to `Samples~/Examples` to reduce clutter and comply with UPM standards.
### Added
- **SamplesHelper**: Added editor tool to toggle `Samples~` folder visibility (requires `RCORE_DEV` define).

## [1.0.6]
### Changed
- **SpineAnimationHelper**: Moved to `Samples~` to support optional Spine dependency.

## [1.0.5]
### Changed
- **TimeHelper**: Optimized `GetNowTimestamp` for per-frame usage.
- **RCore Packages Manager**: Added `Update` button to easily update installed packages.

## [1.0.4]
### Changed
- **HorizontalSnapScrollView**: Added auto-refresh support when `SnapScrollItem` active state changes, with improved validation logic to preserve focus.

## [1.0.3]
### Added
- **RCore Packages Manager**: Added a new editor tool (`RCore -> Packages Manager`) to easily install and uninstall RCore packages and UniTask via git URLs.

## [1.0.2]
- Maintenance update.

## [1.0.1] - 2025-12-24
### Added
- **AssetBundleRef/Wrap/ComponentRef**: Added `IsLoading`/`IsLoaded` properties; added `CancellationToken` and `IProgress<float>` support to async methods.

### Changed
- **Async & Unload**: Improved concurrency handling in `LoadAsync`/`InstantiateAsync` and robust `Unload` logic across all ref/wrap classes.

## [1.0.0]
- Initial release.
