# RevCore.Addressables

UniTask-first wrapper over Unity Addressables for the RevCore framework. Zero dependencies on other RevCore packages.

## Install

Unity Package Manager, local path:

```text
Assets/RevCore/Addressables
```

Or via git URL once published:

```json
"com.rabear.revcore.addressables": "https://github.com/hnb-rabear/RCore.git?path=Assets/RevCore/Addressables#v1.0.0"
```

Requires:

```json
"com.unity.addressables": "1.22.0",
"com.cysharp.unitask": "2.5.10"
```

## 60-second quickstart

```csharp
using Cysharp.Threading.Tasks;
using RevCore;
using UnityEngine;

public class TitleMusic : MonoBehaviour
{
    public AssetRef<AudioClip> mainTheme;

    private async void Start()
    {
        var clip = await mainTheme.LoadAsync();
        GetComponent<AudioSource>().PlayOneShot(clip);
    }

    private void OnDestroy()
    {
        mainTheme.Release();
    }
}
```

Static helpers:

```csharp
var clip = await AddressableLoader.LoadAssetAsync<AudioClip>("MainTheme");
```

Prefab wrapper:

```csharp
var hero = await heroPrefab.InstantiateAsync(parent, defaultActive: true);
```

## Concepts

### Helpers

Helpers are stateless wrappers over Unity Addressables. They return `UniTask` / `UniTask<T>`, accept `CancellationToken ct = default`, and throw `AddressableLoadException` on terminal failure.

| Type | Purpose |
| --- | --- |
| `AddressableLoader` | Load assets, batches, instances, resource locations, and release owned objects |
| `AddressableDownloader` | Query download size, download dependencies, clear dependency cache |
| `AddressableCatalog` | Check and update Addressables catalogs |
| `AddressableScene` | Load and unload Addressables scenes |

### Wrappers

Wrappers add serialized references and cached runtime state.

| Type | Purpose |
| --- | --- |
| `AssetRef<T>` | Serializable wrapper over `AssetReferenceT<T>` with cached `Asset`, `IsLoading`, `IsLoaded`, `LoadAsync`, `Release` |
| `KeyedAssetRef<TKey, T>` | `AssetRef<T>` plus a user-defined key |
| `ComponentRef<TComponent>` | Reference to a `Component` on a prefab `GameObject` |
| `PrefabRef<TComponent>` | `ComponentRef<TComponent>` plus default parent and cached instance |
| `AddressableLoadException` | Failure exception carrying the originating key and `AsyncOperationStatus` |

## Unity serialization caveat

Unity only inspects concrete generic subclasses in many inspector scenarios. To expose wrapper fields reliably, declare a concrete subclass:

```csharp
[System.Serializable]
public class AudioClipRef : AssetRef<AudioClip>
{
}

[System.Serializable]
public class HeroPrefabRef : PrefabRef<Hero>
{
}
```

Composition also works inside another serializable type:

```csharp
[System.Serializable]
public class HeroDefinition
{
    public AssetRef<Sprite> icon;
    public PrefabRef<Hero> prefab;
}
```

## Lifetime contract

All async methods accept `CancellationToken ct = default`. Methods that report load/download progress accept `IProgress<float> progress = null`.

On terminal failure, helpers throw `AddressableLoadException` with:

- `Key`: address, reference, location, catalog operation name, or scene key that failed.
- `Status`: terminal `AsyncOperationStatus` when available.
- `InnerException`: Unity Addressables exception or wrapper validation exception.

Cancellation does not eagerly release in-flight handles. Helpers attach a `Completed += op => Addressables.Release(op)` continuation so resources unwind once the underlying operation finishes.

Need handle ownership? Use `AddressableLoader.LoadAssetWithHandleAsync<T>` and call `Addressables.Release(handle)` when finished.

## API reference

| Symbol | Purpose |
| --- | --- |
| `AddressableLoader.LoadAssetAsync<T>(string)` | Load one asset by address |
| `AddressableLoader.LoadAssetAsync<T>(AssetReference)` | Load one asset by reference |
| `AddressableLoader.LoadAssetWithHandleAsync<T>` | Load one asset and transfer handle ownership to caller |
| `AddressableLoader.LoadAssetsAsync<T>` | Load many addresses in parallel |
| `AddressableLoader.InstantiateAsync` | Instantiate Addressables prefab |
| `AddressableLoader.LoadResourceLocationsAsync` | Query resource locations |
| `AddressableDownloader.GetDownloadSizeAsync` | Query remote download bytes |
| `AddressableDownloader.DownloadDependenciesAsync` | Download remote dependencies |
| `AddressableDownloader.ClearDependencyCacheAsync` | Clear dependency cache |
| `AddressableCatalog.CheckForCatalogUpdatesAsync` | Find pending catalog updates |
| `AddressableCatalog.UpdateCatalogsAsync` | Apply catalog updates |
| `AddressableScene.LoadSceneAsync` | Load Addressables scene |
| `AddressableScene.UnloadSceneAsync` | Unload Addressables scene |

## Migration from RCore

See [`docs/migration/rcore-to-revcore-api-map.csv`](../../../docs/migration/rcore-to-revcore-api-map.csv) for the row-by-row map from `RCore.AddressableUtil`, `AssetBundleRef<T>`, and RCore component reference wrappers to the RevCore equivalents.

## License

Same license as the rest of the RevCore framework. See repository root `LICENSE.md`.
