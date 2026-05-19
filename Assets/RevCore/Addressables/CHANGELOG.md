# Changelog

## [Unreleased]

### Added

- Package scaffold (`package.json`, `Runtime` asmdef, `PublicAPI` baselines, `csc.rsp`).
- `AddressableLoadException` exception type with `Key` and `Status` properties.
- `AddressableLoader.LoadAssetAsync<T>(string address, IProgress<float>, CancellationToken)` — single-address load with throw-on-failure semantics.
- `AddressableLoader.LoadAssetAsync` overloads for `AssetReference` / `AssetReferenceT<T>` and `LoadAssetWithHandleAsync` for caller-owned lifetimes.
- `AddressableLoader.LoadAssetsAsync`, `InstantiateAsync`, `LoadResourceLocationsAsync`, `Release`, `ReleaseInstance`.
- `AddressableDownloader` with `GetDownloadSizeAsync`, `DownloadDependenciesAsync`, `ClearDependencyCacheAsync`.
