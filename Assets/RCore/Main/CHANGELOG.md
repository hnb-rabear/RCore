# Changelog

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
