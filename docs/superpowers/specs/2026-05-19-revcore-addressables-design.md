# RevCore.Addressables v1.0.0 — Design

**Date:** 2026-05-19
**Status:** Awaiting spec review (brainstorming sections approved)
**Anchor:** `Assets/RCore/Main/Runtime/Common/Helper/Addressable/*` (RCore legacy reference, not copied verbatim)

---

## 1. Goal

Ship a new standalone RevCore module — `com.rabear.revcore.addressables` — that gives consumers a clean, UniTask-first wrapper over Unity Addressables. Replaces RCore's `AddressableUtil` + `AssetBundleRef` + `AssetBundleWrap` + `ComponentRef` + `AssetRef_*` family.

RCore is the anchor for behaviour parity. The shape is rewritten to match RevCore conventions: zero cross-package deps, UniTask-only, throw-on-failure, `IProgress<float>` + `CancellationToken`, no hidden global state.

## 2. Non-goals (v1.0.0)

- **Editor tooling.** `AddressableEditorHelper` and `PreBuildProcessor` are NOT ported. The existing `RevCore.Tools` colorizer and `RevCore.Audio` validator stay where they are. Deferred to a later release if consumer demand arises.
- **`Action<T>` callback overloads.** Cut entirely. UniTask-only.
- **Specialized type helpers.** `LoadSpriteAsync`, `LoadTextureAsync`, `LoadTexture2DAsync`, `LoadTexture3DAsync`, `LoadTextAssetAsync`, `LoadSpritesAsync` are not provided — consumers call `LoadAssetAsync<Sprite>(...)` etc.
- **TMP / U2D shortcut refs** (`AssetRef_SpriteAtlas`, `AssetRef_FontAsset`, `ComponentRef_SpriteRenderer`). Consumers write one-liner subclasses if they want them — keeps the package free of TMP / U2D / external deps.
- **Coroutine (`IEnumerator`) load paths.** UniTask-only.
- **Foundation `Result<T>` adoption.** Package stays zero-dep on other RevCore packages; standard `try/catch` is the public contract.
- **Asset→handle internal tracking map.** No hidden registry. Consumers either keep the handle or call `Addressables.Release(asset)` directly.

## 3. Package shape

```text
Assets/RevCore/Addressables/
├── package.json                 # com.rabear.revcore.addressables@1.0.0
├── README.md
├── CHANGELOG.md
├── Runtime/
│   ├── RevCore.Addressables.Runtime.asmdef
│   ├── csc.rsp
│   ├── PublicAPI.Shipped.txt    # empty until release-cut
│   ├── PublicAPI.Unshipped.txt
│   ├── Helpers/
│   │   ├── AddressableLoader.cs
│   │   ├── AddressableDownloader.cs
│   │   ├── AddressableCatalog.cs
│   │   └── AddressableScene.cs
│   ├── Wrappers/
│   │   ├── AssetRef.cs
│   │   ├── KeyedAssetRef.cs
│   │   ├── PrefabRef.cs
│   │   └── ComponentRef.cs
│   └── Exceptions/
│       └── AddressableLoadException.cs
└── Tests/
    └── RevCore.Addressables.Tests.asmdef
```

Convention matches Timer/Audio modules: package id `com.rabear.revcore.*`, asmdef name `RevCore.<Module>.Runtime`, asmdef + PublicAPI live in `Runtime/`, `csc.rsp` declares `PublicAPI.*.txt` as additional files for the analyzer.

### Dependencies

`package.json`:

- `com.unity.addressables` >= 1.22.0 (project currently pins `1.22.3` in `Packages/manifest.json`)
- `com.cysharp.unitask` 2.5.10

No internal RevCore dependencies. Self-contained per the zero-dependency-packages rule.

### AsmDef references

`Runtime/RevCore.Addressables.Runtime.asmdef` references:

- `Unity.Addressables`
- `Unity.ResourceManager`
- `UniTask`
- `UniTask.Addressables` (UniTask's Addressables integration assembly — provides `AsyncOperationHandle.ToUniTask(...)`)

`rootNamespace` = `RevCore` (matches Timer/Audio convention; types declare `namespace RevCore { ... }`).

`Tests/RevCore.Addressables.Tests.asmdef` additionally references:

- `RevCore.Addressables.Runtime`
- `UnityEngine.TestRunner`
- `UnityEditor.TestRunner`
- `UniTask`

Test asmdef uses `overrideReferences: true` + `precompiledReferences: ["nunit.framework.dll"]` and `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, matching `Tests/RevCore.Audio.Tests.asmdef`.

### Namespace

All public types — helpers, wrappers, exception — live in a single `RevCore.Addressables` namespace. The `Exceptions/` folder is organisational only; the type stays top-level in the namespace.

## 4. Helpers (public API)

All async methods return `UniTask` / `UniTask<T>`. All take `CancellationToken ct = default`. Where progress is meaningful: `IProgress<float> progress = null`. All throw `AddressableLoadException` on operation failure (cancellation throws `OperationCanceledException` per .NET convention).

### 4.1 `AddressableLoader`

```csharp
public static class AddressableLoader
{
    // Single asset
    public static UniTask<T> LoadAssetAsync<T>(string address, IProgress<float> progress = null, CancellationToken ct = default) where T : Object;
    public static UniTask<T> LoadAssetAsync<T>(AssetReference reference, IProgress<float> progress = null, CancellationToken ct = default) where T : Object;
    public static UniTask<T> LoadAssetAsync<T>(AssetReferenceT<T> reference, IProgress<float> progress = null, CancellationToken ct = default) where T : Object;

    // Handle-returning variants (advanced consumers managing release explicitly)
    public static UniTask<AsyncOperationHandle<T>> LoadAssetWithHandleAsync<T>(string address, IProgress<float> progress = null, CancellationToken ct = default) where T : Object;
    public static UniTask<AsyncOperationHandle<T>> LoadAssetWithHandleAsync<T>(AssetReference reference, IProgress<float> progress = null, CancellationToken ct = default) where T : Object;
    public static UniTask<AsyncOperationHandle<T>> LoadAssetWithHandleAsync<T>(AssetReferenceT<T> reference, IProgress<float> progress = null, CancellationToken ct = default) where T : Object;

    // Batch
    public static UniTask<IList<T>> LoadAssetsAsync<T>(IList<string> addresses, CancellationToken ct = default) where T : Object;
    public static UniTask<IList<T>> LoadAssetsAsync<T>(IList<AssetReference> references, CancellationToken ct = default) where T : Object;
    public static UniTask<IList<T>> LoadAssetsAsync<T>(IList<IResourceLocation> locations, CancellationToken ct = default) where T : Object;

    // Instantiate
    public static UniTask<GameObject> InstantiateAsync(AssetReference reference, Transform parent = null, IProgress<float> progress = null, CancellationToken ct = default);
    public static UniTask<TComponent> InstantiateAsync<TComponent>(AssetReference reference, Transform parent = null, IProgress<float> progress = null, CancellationToken ct = default) where TComponent : Component;
    public static UniTask<IList<GameObject>> InstantiateAsync(IList<AssetReference> references, Transform parent = null, CancellationToken ct = default);

    // Resource locations
    public static UniTask<IList<IResourceLocation>> LoadResourceLocationsAsync(object key, CancellationToken ct = default);

    // Release
    public static void Release(AsyncOperationHandle handle);
    public static void Release<T>(AsyncOperationHandle<T> handle);
    public static void ReleaseInstance(GameObject instance);
}
```

### 4.2 `AddressableDownloader`

```csharp
public static class AddressableDownloader
{
    public static UniTask<long> GetDownloadSizeAsync(object key, CancellationToken ct = default);
    public static UniTask DownloadDependenciesAsync(object key, bool autoRelease = true, IProgress<float> progress = null, CancellationToken ct = default);
    public static UniTask DownloadDependenciesAsync(IList<IResourceLocation> locations, bool autoRelease = true, IProgress<float> progress = null, CancellationToken ct = default);
    public static UniTask<bool> ClearDependencyCacheAsync(object key, CancellationToken ct = default);
}
```

### 4.3 `AddressableCatalog`

```csharp
public static class AddressableCatalog
{
    public static UniTask<IList<string>> CheckForCatalogUpdatesAsync(CancellationToken ct = default);
    public static UniTask UpdateCatalogsAsync(IList<string> catalogIds = null, CancellationToken ct = default);
}
```

### 4.4 `AddressableScene`

```csharp
public static class AddressableScene
{
    public static UniTask<SceneInstance> LoadSceneAsync(string address, LoadSceneMode mode = LoadSceneMode.Single, bool activateOnLoad = true, IProgress<float> progress = null, CancellationToken ct = default);
    public static UniTask<SceneInstance> UnloadSceneAsync(SceneInstance scene, IProgress<float> progress = null, CancellationToken ct = default);
}
```

## 5. Wrappers (serializable refs)

All `[Serializable]` so Unity inspector can hold them.

### Unity generic serialization caveat

`AssetRef<T>`, `KeyedAssetRef<TKey, T>`, and `PrefabRef<TComponent>` are open generic `[Serializable]` types. Unity's serializer only persists open-generic types when the field is declared with a **concrete** generic instantiation. Two supported usage patterns:

1. **Concrete field on a non-generic owner (works as-is):**

   ```csharp
   public class Enemy : MonoBehaviour
   {
       [SerializeField] private AssetRef<AudioClip> m_hitSfx;          // works
       [SerializeField] private KeyedAssetRef<EnemyKind, GameObject> m_prefab;  // works
   }
   ```

2. **Concrete subclass when the consumer wants to reuse a typed ref widely:**

   ```csharp
   [Serializable]
   public class AudioClipRef : AssetRef<AudioClip> { }                 // works
   ```

Pure generic fields on a generic owner (e.g., `class MyHolder<T> { public AssetRef<T> r; }`) will NOT serialize — same limitation that already applied to RCore's `AssetBundleRef<M>`. Document both patterns in the package README.

### 5.1 `AssetRef<T>`

```csharp
[Serializable]
public class AssetRef<T> where T : Object
{
    public AssetReferenceT<T> reference;

    public T Asset { get; }            // null until loaded
    public bool IsLoading { get; }
    public bool IsLoaded { get; }

    public UniTask<T> LoadAsync(IProgress<float> progress = null, CancellationToken ct = default);
    public void Release();             // idempotent; nulls Asset and releases handle
}
```

Replaces RCore `AssetBundleRef<M>`. Internally owns one `AsyncOperationHandle<T>`. `LoadAsync` is idempotent (returns existing Asset if already loaded).

### 5.2 `KeyedAssetRef<TKey, T>`

```csharp
[Serializable]
public class KeyedAssetRef<TKey, T> : AssetRef<T> where T : Object
{
    public TKey key;
}
```

Collapses RCore's three keyed variants (`AssetBundleWithEnumKey`, `AssetBundleWith2EnumKeys`, `AssetBundleWithIntKey`) into one generic. Two-key consumers either tuple `KeyedAssetRef<(MyA, MyB), T>` or subclass.

### 5.3 `ComponentRef<TComponent>`

```csharp
[Serializable]
public class ComponentRef<TComponent> : AssetReference where TComponent : Component
{
    public TComponent Asset { get; }
    public bool IsLoading { get; }
    public bool IsLoaded { get; }

    public ComponentRef(string guid) : base(guid) { }

    public override bool ValidateAsset(Object obj);
    public override bool ValidateAsset(string path);

    public UniTask<TComponent> LoadAssetAsync(CancellationToken ct = default);
    public void Release();              // idempotent
}
```

Kept from RCore. API cleaned:

- Drop public mutable `instance` and `asset` fields → properties.
- Keep the `type` string as a **private serialized field** (`[SerializeField] private string m_type;`). Set inside `ValidateAsset` so the editor can sanity-check the bound asset's component type without loading it. Not exposed publicly.
- Rename `ReleaseAsset()` → `Release()` for parity with `AssetRef<T>`. Base `AssetReference.ReleaseAsset()` still accessible via cast.

### 5.4 `PrefabRef<TComponent>`

```csharp
[Serializable]
public class PrefabRef<TComponent> where TComponent : Component
{
    public Transform parent;
    public ComponentRef<TComponent> reference;

    public TComponent Asset { get; }
    public TComponent Instance { get; }       // [NonSerialized]
    public bool IsLoading { get; }
    public bool IsLoaded { get; }
    public bool IsInstantiated { get; }

    public UniTask<TComponent> LoadAsync(IProgress<float> progress = null, CancellationToken ct = default);
    public UniTask<TComponent> InstantiateAsync(bool defaultActive = false, CancellationToken ct = default);
    public void Release();                    // destroys instance + releases handle
}
```

Replaces RCore `AssetBundleWrap<T>`. `InstantiateAsync` always awaits load first. Drop RCore's `InstanceLoaded(bool)` fire-and-forget — caller awaits `InstantiateAsync` explicitly.

## 6. Error handling

```csharp
public sealed class AddressableLoadException : Exception
{
    public object Key { get; }
    public AsyncOperationStatus Status { get; }

    public AddressableLoadException(object key, AsyncOperationStatus status, Exception inner)
        : base($"Addressable load failed: {key} (status={status})", inner)
    {
        Key = key;
        Status = status;
    }
}
```

### Semantics

- Helpers await `handle.ToUniTask(progress, ct)`. On failure status, helpers wrap the operation's `OperationException` in `AddressableLoadException` and throw. `Key` carries the address / `AssetReference` / location passed in.
- Cancellation: `OperationCanceledException` propagates unwrapped from `ToUniTask(ct)`. The underlying `AsyncOperationHandle` is NOT eagerly released — Addressables warns against releasing in-flight handles. Instead, helpers attach a `handle.Completed += op => Addressables.Release(op);` continuation so the handle frees itself once the operation finishes (success or failure) after a cancellation. For asset-returning helpers, this means the asset is never delivered to the caller and the handle cleans up; for `LoadAssetWithHandleAsync`, the caller already holds the handle and is responsible for release.
- No swallowing. No `Debug.LogError` inside helpers. Consumer logs at the boundary if it wants.
- Wrappers: same semantics. `LoadAsync` may throw `AddressableLoadException`. `Release()` never throws (try/catch internally, no log spam). On cancellation mid-load, the wrapper's internal handle uses the same auto-release-on-completion pattern; `IsLoaded` stays `false`.

### Lifetime contract

| Returns | Caller owns | How to release |
| --- | --- | --- |
| `LoadAssetAsync<T>` → `T` | The handle is tracked by Addressables but not exposed. | `Addressables.Release(asset)` or use the handle-returning overload. |
| `LoadAssetWithHandleAsync<T>` → `AsyncOperationHandle<T>` | Handle. | `AddressableLoader.Release(handle)`. |
| `InstantiateAsync` → `GameObject` / `TComponent` | Instance. | `AddressableLoader.ReleaseInstance(go)`. |
| `LoadAsync` on a wrapper | Wrapper. | `wrapper.Release()`. |

### Threading

All continuations run on Unity's main thread via UniTask's default `PlayerLoopTiming.Update`. Addressables is main-thread by design — no explicit thread switches inside helpers.

## 7. Testing

### Test layout

```text
Tests/
├── RevCore.Addressables.Tests.asmdef
├── Editor/
│   ├── AddressableLoaderTests.cs
│   ├── AddressableDownloaderTests.cs
│   ├── AddressableCatalogTests.cs
│   ├── AssetRefTests.cs
│   ├── KeyedAssetRefTests.cs
│   ├── ComponentRefTests.cs
│   └── PrefabRefTests.cs
└── Runtime/                  # PlayMode (scene / instance ops)
    ├── AddressableSceneTests.cs
    └── InstantiateAsyncTests.cs
```

### Approach

- Use a fake `IResourceLocator` + in-memory `IResourceProvider` for EditMode unit tests. Avoids needing a real Addressables build to run tests.
- A `AddressableLoadFixture` test helper builds the locator and registers it via `Addressables.AddResourceLocator(...)` per test; tears down in teardown.
- Use `[UnityTest]` + `UniTask.ToCoroutine` for tests that need to await UniTask.
- PlayMode tests run for scene load / instance instantiation paths only — everything else stays EditMode.

### Coverage targets per area

- **Loader:** load by address / by `AssetReference` / by `AssetReferenceT<T>`; batch load preserves order; cancellation mid-load throws `OperationCanceledException`; missing key throws `AddressableLoadException`; progress reported strictly monotonic 0→1.
- **Downloader:** size returns expected long; download with autoRelease cleans up; progress reported; cancellation releases handle.
- **Catalog:** check returns empty list when no updates; update accepts catalog IDs.
- **Scene:** load Single/Additive; unload returns succeeded; activateOnLoad=false leaves scene inactive.
- **Wrappers:** load idempotence (second `LoadAsync` returns same Asset, no re-acquire); release idempotence; `IsLoading`/`IsLoaded` transitions correct; cancellation mid-load leaves `IsLoaded == false`; `KeyedAssetRef<TKey, T>` survives JSON / Unity serialization round-trip; `PrefabRef.InstantiateAsync` parents correctly; `PrefabRef.Release` destroys instance.

Target: ~30 tests at first ship.

## 8. Migration map (RCore → RevCore)

Added to `docs/migration/rcore-to-revcore-api-map.csv`:

| RCore symbol | RevCore replacement | Notes |
| --- | --- | --- |
| `AddressableUtil.LoadAssetAsync<T>(string, Action<T>, Action<float>)` | `AddressableLoader.LoadAssetAsync<T>(string, IProgress<float>, CancellationToken)` | Drop callback. `await` the result. |
| `AddressableUtil.LoadAssetAsync<T>(string)` (UniTask form) | `AddressableLoader.LoadAssetAsync<T>(string, ..., ct)` | Direct port. |
| `AddressableUtil.LoadAssetAsync<TObject, TReference>(TReference)` | `AddressableLoader.LoadAssetAsync<T>(AssetReference, ..., ct)` | Two-generic constraint relaxed to single. |
| `AddressableUtil.LoadAssetAsync<TObject>(AssetReferenceT<TObject>)` | `AddressableLoader.LoadAssetAsync<T>(AssetReferenceT<T>, ..., ct)` | Direct port. |
| `AddressableUtil.LoadAssetsAsync<T>(List<string>)` | `AddressableLoader.LoadAssetsAsync<T>(IList<string>, ct)` | Direct port. |
| `AddressableUtil.InstantiateAsync<TComponent, TReference>(TReference, Transform)` | `AddressableLoader.InstantiateAsync<TComponent>(AssetReference, Transform, ..., ct)` | Generic constraint relaxed. |
| `AddressableUtil.DownloadDependenciesAsync(key, bool, Action, Action<float>)` | `AddressableDownloader.DownloadDependenciesAsync(key, bool, IProgress<float>, ct)` | Drop callback. |
| `AddressableUtil.GetDownloadSizeAsync(key, Action<long>)` | `AddressableDownloader.GetDownloadSizeAsync(key, ct)` | Returns `long`. |
| `AddressableUtil.CheckForCatalogUpdates()` | `AddressableCatalog.CheckForCatalogUpdatesAsync(ct)` | Returns IDs list; explicit `UpdateCatalogsAsync` follow-up. |
| `AddressableUtil.LoadSceneAsync(string, LoadSceneMode, Action<SceneInstance>, Action<float>)` | `AddressableScene.LoadSceneAsync(string, mode, activateOnLoad, IProgress<float>, ct)` | Drop callback. |
| `AddressableUtil.UnloadSceneAsync(SceneInstance, Action<bool>, Action<float>)` | `AddressableScene.UnloadSceneAsync(SceneInstance, IProgress<float>, ct)` | Drop callback. |
| `AddressableUtil.LoadSpriteAsync(...)` / `LoadTextureAsync` / `LoadTexture2DAsync` / `LoadTexture3DAsync` / `LoadTextAssetAsync` / `LoadSpritesAsync` | `AddressableLoader.LoadAssetAsync<Sprite>` / `<Texture>` / `<Texture2D>` / `<Texture3D>` / `<TextAsset>` / `<IList<Sprite>>` | Generic-only. |
| `AssetBundleRef<M>` | `AssetRef<T>` | Renamed. Same shape. |
| `AssetBundleWithEnumKey<T, M>` | `KeyedAssetRef<TKey, T>` | Collapsed. |
| `AssetBundleWithIntKey<M>` | `KeyedAssetRef<int, T>` | Collapsed. |
| `AssetBundleWith2EnumKeys<T1, T2, M>` | `KeyedAssetRef<(TKey1, TKey2), T>` or consumer subclass | Collapsed. |
| `AssetBundleWrap<T>` | `PrefabRef<T>` | Renamed. UniTask-only. No fire-and-forget `InstanceLoaded`. |
| `ComponentRef<T>` | `ComponentRef<T>` | Same name. Cleaner API. `ReleaseAsset()` → `Release()`. |
| `ComponentRef_SpriteRenderer` | Consumer subclass | Dropped. |
| `AssetRef_SpriteAtlas` | Consumer subclass | Dropped (TMP/U2D dep). |
| `AssetRef_FontAsset` | Consumer subclass | Dropped (TMP dep). |
| `*.IELoad()` / `IELoadAsset()` coroutines | Removed | UniTask-only. |

## 9. Release path

- **Branch:** `feat/addressables-v1.0` off `main`.
- **Not piggy-backed** on the in-flight `feat/timer-audio-unitask-v1.1` branch.
- **Commits:** one commit per scope (package scaffold, helpers, wrappers, tests, migration map, README/CHANGELOG). CHANGELOG entry per commit.
- **PublicAPI:** every public member added to `PublicAPI.Unshipped.txt`. Sealed at release-cut via `scripts/seal-public-api.py`.
- **XML doc:** 100% coverage required. Baseline updated in `scripts/xmldoc-baseline.json`.
- **Tests:** ~30 EditMode + a small PlayMode batch must pass.

## 10. Open items / follow-ups (post v1.0.0)

- **Editor tooling port.** `AddressableEditorHelper` (entry queries, group containment checks) and the auto-build `PreBuildProcessor` may be ported into either this package's `Editor/` subfolder or stay in `RevCore.Tools`. Decide on first consumer ask.
- **Specialized refs.** If a consumer requests `AssetRef_SpriteAtlas` etc, document the one-liner pattern in the README first; only ship if more than one consumer asks.
- **Two-key `KeyedAssetRef` ergonomics.** If `(TKey1, TKey2)` tuple proves clunky in Unity inspector, ship a `KeyedAssetRef2<TKey1, TKey2, T>` convenience class then.
