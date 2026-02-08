# Changelog

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
