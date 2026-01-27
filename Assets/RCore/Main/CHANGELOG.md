# Changelog

## [1.1.0]
### Added
- **RHierarchy**: Added a lightweight hierarchy enhancer tool
    - Features:
        - **Visibility**: Quick toggle for GameObject active state.
        - **Static**: specific "S" indicator; click to toggle static flags.
        - **Components**: Icons for attached scripts and components (with enable/disable toggle).
        - **Info**: Displays vertices count and children count.
        - **Tag & Layer**: Display and edit tags and layers directly in the hierarchy.
    - **Customization**:
        - Fully reorderable component display.
        - appearance settings (colors, shading, separator lines).
        - Global enable/disable toggle.

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
