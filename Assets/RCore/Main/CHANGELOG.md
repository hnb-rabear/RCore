# Changelog

## [1.0.3]
### Added
- **RCore Packages Manager**: Added a new editor tool (`RCore -> Packages Manager`) to easily install and uninstall RCore packages and UniTask via git URLs.

## [1.0.2]
- Maintenance update.

## [1.0.1] - 2025-12-24

### Added
- **AssetBundleRef**: Added `IsLoading` and `IsLoaded` properties.
- **AssetBundleRef**: Added `CancellationToken` support and `IProgress<float>` to `LoadAsync`.
- **AssetBundleWrap**: Added `IsLoading` and `IsLoaded` properties.
- **AssetBundleWrap**: Added `CancellationToken` support and `IProgress<float>` to `LoadAsync`.
- **AssetBundleWrap**: Added `CancellationToken` support to `InstantiateAsync`.
- **ComponentRef**: Added `IsLoading` property.
- **ComponentRef**: Added `CancellationToken` support to `LoadAssetAsync` and `InstantiateAsync`.

### Changed
- **AssetBundleRef**: `LoadAsync` now prevents duplicate loading operations; if called while loading, it awaits the existing operation.
- **AssetBundleWrap**: Refactored `InstantiateAsync` to use `LoadAsync` internally, ensuring the asset is loaded before instantiation and preventing concurrent load race conditions.
- **ComponentRef**: Refactored `InstantiateAsync` to use `LoadAssetAsync` internally for better concurrency handling.
- **General**: Improved `Unload` logic in all ref/wrap classes to robustly handle handle releasing and instance destruction, including cleanup of failed operations.

## [1.0.0]
- Initial release.
