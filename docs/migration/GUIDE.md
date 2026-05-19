# Hướng Dẫn Migration Từ RCore Sang RevCore

> Cập nhật: 2026-05-19  
> Audience: team đang có Unity project dùng `Assets/RCore/` và muốn migrate dần sang `Assets/RevCore/`.  
> Nguyên tắc chính: migrate từng module, từng PR nhỏ, luôn giữ RCore cho tới khi chắc chắn không còn dependency thật.

---

## 1. Có nên migrate không?

Không migrate chỉ vì muốn “dọn sạch”. RCore vẫn được giữ cho project production. RevCore được thiết kế để **coexist** với RCore, nên project có thể dùng cả hai trong cùng repo.

Nên migrate khi có ít nhất một lý do rõ:

- Project mới muốn dùng framework modular từ đầu.
- Feature mới chưa bị legacy coupling, có thể viết bằng RevCore.
- Team cần API contract tốt hơn: semver, XML docs, PublicAPI paper trail, deprecation policy.
- RCore API đang dùng đã có replacement rõ trong RevCore.
- Muốn giảm dependency monolith, chỉ cài module cần thiết.

Không nên migrate khi:

- Project đang gần release.
- Không có test/smoke checklist để bắt regression.
- Code đang phụ thuộc sâu vào RCore service wrappers, Firebase/Ads/IAP, custom editor utilities, hoặc gameplay-specific helpers.
- Mục tiêu chỉ là đổi namespace cho “đẹp”.

---

## 2. Khác biệt tư duy giữa RCore và RevCore

| RCore | RevCore |
| --- | --- |
| Monolith, nhiều utility chung một chỗ | Nhiều UPM package độc lập |
| Namespace dạng `RCore.*` | Namespace runtime phẳng `RevCore` |
| Nhiều API sinh ra theo nhu cầu project cụ thể | Chỉ giữ API đủ framework-level |
| Ít public API governance | PublicAPI + XML docs + semver/deprecation |
| Có nhiều thin wrapper quanh vendor SDK | Vendor/service integrations để consumer tự quản |
| Migration thường là “copy framework mới vào” | Migration từng module, từng feature boundary |

RevCore không copy toàn bộ RCore. Một số RCore API được port, một số được đổi tên, một số được thay bằng API shape mới, một số bị drop.

---

## 3. Tài liệu cần đọc trước khi bắt đầu

- [Migration map CSV](rcore-to-revcore-api-map.csv) — source of truth cho tooling/mapping.
- [Migration map markdown](rcore-to-revcore-api-map.md) — bản dễ đọc theo status.
- [Gap categories](gap-categories.md) — giải thích nhóm GAP và hướng xử lý.
- [Migration playbook](PLAN.md) — playbook gốc bằng tiếng Anh, module-by-module.
- [RevCore introduction](../INTRODUCTION.md) — giới thiệu tổng quan RevCore.
- [Architecture](../ARCHITECTURE.md) — dependency graph + tradeoffs.

Lưu ý: `gap-categories.md` được viết trước khi RevCore.Addressables v1.0.0 hoàn tất. Với nhóm Addressables (`AddressableUtil`, `AssetBundleRef`, `ComponentRef`), ưu tiên mapping mới trong `rcore-to-revcore-api-map.csv` / `.md` vì đã có status `REPLACED` / `DROPPED` cụ thể.

---

## 4. Chuẩn bị migration branch

### 4.1 Điều kiện tối thiểu

Trước khi sửa code:

1. Tạo branch riêng, không làm trực tiếp trên `main` / `develop`.
2. Đảm bảo project hiện tại compile clean với RCore.
3. Chạy test suite hiện có nếu project có tests.
4. Lập danh sách scene/flow phải smoke test thủ công.
5. Giữ nguyên `Assets/RCore/` trong project. Không xóa RCore ở đầu migration.
6. Cài RevCore module cần dùng bằng local path hoặc git URL.

### 4.2 Vì sao không xóa RCore trước?

Vì RCore và RevCore có thể coexist. Xóa RCore sớm làm compile lỗi hàng loạt, khó biết lỗi nào là migration thật, lỗi nào do thiếu dependency. RCore chỉ nên xóa ở cuối, khi search toàn project không còn reference cần giữ.

---

## 5. Inventory usage RCore trong project

Chạy search để biết project đang dùng gì.

PowerShell:

```powershell
Select-String -Path "Assets/**/*.cs" -Pattern "\bRCore\.|using RCore" | Set-Content rcore-usage.txt
Select-String -Path "Assets/**/*.cs" -Pattern "\bR(Player|Editor)Pref[A-Z]\w*\b" | Add-Content rcore-usage.txt
```

Bash:

```bash
grep -RIn --include='*.cs' -E '\b(RCore\.|using RCore)' Assets/ > rcore-usage.txt
grep -RIn --include='*.cs' -E '\bR(Player|Editor)Pref[A-Z]\w*\b' Assets/ >> rcore-usage.txt
```

Sau đó tổng hợp unique type/API đang dùng và đối chiếu với [rcore-to-revcore-api-map.csv](rcore-to-revcore-api-map.csv).

Phân loại từng usage:

| Bucket | Map status | Hành động |
| --- | --- | --- |
| Mechanical | `PORTED`, `RENAMED` | Đổi namespace/type name, compile, test. |
| Replacement | `REPLACED` | Rewrite theo API RevCore mới. Không regex bừa. |
| Dropped | `DROPPED` | Giữ RCore, fork vào project, hoặc dùng giải pháp khác. |
| Gap | `GAP` | Xem `gap-categories.md`; quyết định per feature. |

---

## 6. Thứ tự migration khuyến nghị

Migrate theo dependency order, mỗi nhóm một PR nhỏ.

1. **Foundation** — EventBus, ServiceLocator, Result, helpers, BigNumber, SerializableDictionary.
2. **Prefs** — PlayerPrefs/EditorPrefs typed wrappers.
3. **Timer** — scheduler, delay, condition, debounce.
4. **Pool** — `RevPool<T>`, `PoolsContainer<T>`.
5. **Audio** — audio collection, manager, SFX/music flow.
6. **Data** — JObjectDB V2, save/load lifecycle.
7. **Inspector** — attributes/drawers.
8. **UI** — PanelStack, buttons/toggles, scroll views, layout helpers.
9. **Addressables** — loader/downloader/catalog/scene helpers, asset/component/prefab wrappers.
10. **Tools** — editor productivity only; migrate only if team needs it.

Foundation nên đi trước vì nhiều module phụ thuộc. UI nên đi muộn vì thường chạm nhiều scene/prefab. Addressables nên migrate theo asset-loading boundary vì behavior lifetime/cancellation khác RCore.

---

## 7. Mechanical migration: PORTED và RENAMED

### 7.1 PORTED

`PORTED` nghĩa là type còn đó nhưng namespace/module đổi.

Ví dụ:

```diff
-using RCore;
-using RCore.Common;
-using RCore.UI;
+using RevCore;
```

RevCore runtime API dùng namespace phẳng `RevCore`, nên nhiều file chỉ cần đổi `using`.

### 7.2 RENAMED

`RENAMED` nghĩa là type mới có tên khác.

Ví dụ thường gặp:

| RCore | RevCore | Ghi chú |
| --- | --- | --- |
| `BigNumberD` | `BigNumber` | RevCore gom double/float variants. |
| `BigNumberF` | `BigNumber` | Kiểm tra precision nếu code nhạy cảm. |
| `RPlayerPrefBool` | `PlayerPrefBool` | Thuộc Prefs. |
| `RPlayerPrefInt` | `PlayerPrefInt` | Thuộc Prefs. |
| `RPlayerPrefFloat` | `PlayerPrefFloat` | Thuộc Prefs. |
| `RPlayerPrefString` | `PlayerPrefString` | Thuộc Prefs. |
| `JObjectDBManagerV2` | `JObjectDBManager<TCollection>` | Thêm generic collection type. |
| `JObjectHandler` | `IJObjectHandler` | Class → interface. |
| `HorizontalAlignment` | `HorizontalAlignmentUI` | UI layout. |
| `TableAlignment` | `TableAlignmentUI` | UI layout. |
| `VerticalAlignment` | `VerticalAlignmentUI` | UI layout. |

Sau mỗi batch mechanical:

1. Compile.
2. Fix lỗi namespace/type name.
3. Chạy tests/smoke nhỏ.
4. Commit riêng.

---

## 8. Replacement migration: API shape đã đổi

`REPLACED` nghĩa là RevCore có replacement, nhưng không đảm bảo source-compatible. Đây là phần cần review kỹ nhất.

### 8.1 EventDispatcher → EventBus

RCore pattern thường là dispatcher/event class riêng. RevCore dùng type-keyed event bus.

Before:

```csharp
EventDispatcher.Raise(new DamageEvent(10));
```

After:

```csharp
Events.Publish(new DamageEvent(10));
```

Subscribe/unsubscribe rõ theo lifecycle:

```csharp
private void OnEnable()
{
	Events.Subscribe<DamageEvent>(OnDamage);
}

private void OnDisable()
{
	Events.Unsubscribe<DamageEvent>(OnDamage);
}
```

### 8.2 RCore timer events → TimerScheduler / Timers

RCore timer event model khác RevCore handle model.

Before concept:

```csharp
var evt = new CountdownEvent
{
	time = 5f,
	onTimeOut = OnDone
};
TimerEventsGlobal.Instance.Register(evt);
```

After:

```csharp
ITimerHandle handle = GlobalTimers.Instance.WaitForSeconds(5f, OnDone);
```

Cancel:

```csharp
handle.Cancel();
```

Condition wait:

```csharp
SceneTimers.Instance.WaitForCondition(() => IsReady, OnReady);
```

### 8.3 CustomPool → RevPool

Before:

```csharp
private CustomPool<Bullet> m_bullets;
```

After:

```csharp
private RevPool<Bullet> m_bullets;
```

Spawn/release thường giống ý tưởng, nhưng cần test behavior:

- prewarm count;
- active/inactive state;
- delayed release;
- parent transform;
- callback side effects.

### 8.4 KeyValueDB → JObjectDB hoặc giữ RCore

RCore `KeyValueDB` không được port. Nếu project dùng nhiều `IntegerData`, `BoolData`, `StringData`, `InventoryData`, cần quyết định:

1. Giữ RCore cho subsystem đó.
2. Viết exporter một lần sang JObjectDB.
3. Rewrite data model sang `JObjectModel<T>`.

Sketch exporter:

```csharp
if (!PlayerPrefs.HasKey("revcore_migrated"))
{
	// Read old RCore KeyValueDB here.
	// Write equivalent RevCore JObjectDB model here.
	PlayerPrefs.SetInt("revcore_migrated", 1);
	PlayerPrefs.Save();
}
```

Không xóa data old cho tới khi build mới đã chạy đủ lâu trên production.

### 8.5 AddressableUtil → Addressables helpers

RCore `AddressableUtil` được thay bằng nhiều helper rõ trách nhiệm hơn:

| RCore | RevCore |
| --- | --- |
| `AddressableUtil.LoadAssetAsync<T>(string)` | `AddressableLoader.LoadAssetAsync<T>(string)` |
| `AddressableUtil.LoadAssetAsync<T>(AssetReference)` | `AddressableLoader.LoadAssetAsync<T>(AssetReference)` |
| `AddressableUtil.LoadAssetsAsync<T>` | `AddressableLoader.LoadAssetsAsync<T>` |
| `AddressableUtil.InstantiateAsync` | `AddressableLoader.InstantiateAsync` |
| `AddressableUtil.LoadSceneAsync` | `AddressableScene.LoadSceneAsync` |
| `AddressableUtil.UnloadSceneAsync` | `AddressableScene.UnloadSceneAsync` |
| `AddressableUtil.GetDownloadSizeAsync` | `AddressableDownloader.GetDownloadSizeAsync` |
| `AddressableUtil.DownloadDependenciesAsync` | `AddressableDownloader.DownloadDependenciesAsync` |
| `AddressableUtil.CheckForCatalogUpdatesAsync` | `AddressableCatalog.CheckForCatalogUpdatesAsync` |

Before:

```csharp
var clip = await AddressableUtil.LoadAssetAsync<AudioClip>("MainTheme");
```

After:

```csharp
var clip = await AddressableLoader.LoadAssetAsync<AudioClip>("MainTheme");
```

Important behavior:

- terminal failure throws `AddressableLoadException`;
- all async methods use UniTask;
- cancellation uses `CancellationToken ct = default`;
- progress uses `IProgress<float> progress = null`;
- caller-owned handle uses `LoadAssetWithHandleAsync<T>`.

### 8.6 AssetBundleRef family → Addressables wrappers

| RCore | RevCore |
| --- | --- |
| `AssetBundleRef<T>` | `AssetRef<T>` |
| `AssetBundleWithEnumKey<T,M>` | `KeyedAssetRef<TKey,T>` |
| `AssetBundleWithIntKey<M>` | `KeyedAssetRef<int,T>` |
| `AssetBundleWrap<T>` | `PrefabRef<T>` |
| `ComponentRef<TComponent>` | `ComponentRef<TComponent>` in namespace `RevCore` |
| `ComponentRef_SpriteRenderer` | Use `RevCore.ComponentRef<SpriteRenderer>` or a concrete subclass if inspector needs it |

Unity generic serialization caveat vẫn áp dụng. Nếu inspector không hiển thị generic wrapper ổn định, tạo concrete subclass:

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

---

## 9. DROPPED: API không mang sang RevCore

`DROPPED` không phải bug. Nó thường là quyết định scope.

Nhóm thường bị drop:

- vendor SDK wrappers: Ads, Firebase, IAP, Notifications;
- SimpleJSON vendored parser;
- encryption/obfuscation PlayerPrefs;
- project-specific RPG stat/mod systems;
- game-specific UI message/bubble/notification;
- old editor utility libraries;
- binary formatter save helpers;
- tiny helpers dễ copy vào consumer namespace.

Cách xử lý:

1. **Giữ RCore cho type đó** — default an toàn nhất.
2. **Fork source vào project namespace** — khi type nhỏ và project cần own maintenance.
3. **Dùng vendor SDK trực tiếp** — với Firebase/Ads/IAP/Notifications.
4. **Thiết kế module riêng** — nếu nhiều project thật sự cần lại tính năng đó.

Không cố nhét mọi thứ vào RevCore để đạt “100% parity”. Mục tiêu là framework sạch, ổn định, dùng được cho nhiều team.

---

## 10. GAP: chưa có equivalent rõ

`GAP` nghĩa là map chưa có RevCore target. Xử lý theo [gap-categories.md](gap-categories.md):

- `DROP`: không migrate, giữ/fork/thay bằng vendor SDK.
- `REPLACE`: rewrite sang API khác.
- `DEFER`: giữ RCore, mở issue/spec khi consumer thật sự cần.
- `PORT`: chỉ port khi có nhu cầu rõ và design được approve.

Không tự port GAP vào RevCore trong cùng PR migration consumer. Tách thành work riêng:

1. viết spec;
2. review design;
3. implement package/API;
4. release RevCore;
5. quay lại migrate consumer.

---

## 11. Save-file compatibility

### 11.1 JObjectDB

RevCore.Data giữ lineage JObjectDB. Save compatibility phụ thuộc vào **data class của consumer**:

- field name giữ nguyên → cơ hội compatible cao;
- đổi field/type/list shape → cần migration logic;
- đổi key PlayerPrefs → mất data nếu không copy/migrate.

Nơi migrate data đúng: `JObjectModel<T>.OnPostLoad`.

Ví dụ:

```csharp
public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
{
	if (Data.level <= 0)
		Data.level = 1;
}
```

Phải test bằng save thật từ bản production cũ.

### 11.2 KeyValueDB

KeyValueDB không migrate tự động sang JObjectDB. Nếu project đang dùng KeyValueDB:

- giữ RCore cho KeyValueDB trong một thời gian;
- viết exporter một lần;
- deploy một production cycle;
- chỉ xóa exporter/RCore dependency khi telemetry hoặc support risk đã ổn.

---

## 12. UI migration checklist

UI thường rủi ro vì prefab/scene serialized references.

Checklist:

- Mở tất cả scene UI chính.
- Kiểm tra missing scripts trên prefab/scene.
- Kiểm tra button SFX event.
- Kiểm tra back navigation.
- Kiểm tra panel queue/dimmer/nested panels.
- Kiểm tra optimized scroll view với data thật.
- Kiểm tra inactive object show/hide flow.
- Chạy PlayMode smoke cho từng flow.

Không migrate toàn bộ UI bằng search-replace rồi merge ngay.

---

## 13. Addressables migration checklist

Addressables rủi ro vì asset lifetime.

Checklist:

- Verify Addressables package installed.
- Verify UniTask installed.
- Convert helper calls theo mapping.
- Convert serialized wrappers.
- Với wrapper generic, tạo concrete subclass nếu inspector cần.
- Kiểm tra `Release()` được gọi ở lifecycle phù hợp.
- Nếu caller cần giữ handle, dùng `LoadAssetWithHandleAsync<T>` và tự `Addressables.Release(handle)`.
- Test cancellation path nếu scene unload/cancel token xảy ra.
- Test download size/dependencies nếu project dùng remote content.
- Test scene load/unload nếu dùng addressable scenes.
- Dùng Addressables Profiler/manual memory check để bắt leak.

---

## 14. Validation trước khi merge mỗi PR

Mỗi module migration PR phải có checklist:

```markdown
## Test plan

- [ ] Unity compile clean.
- [ ] EditMode tests pass.
- [ ] PlayMode/manual smoke pass for affected feature.
- [ ] No unexpected `RCore.` references remain in migrated files.
- [ ] Serialized prefabs/scenes checked if module touches Unity objects.
- [ ] Save-file compatibility tested if module touches Data/Prefs.
- [ ] Addressables lifetime checked if module touches Addressables.
```

Search references sau mỗi PR:

PowerShell:

```powershell
Select-String -Path "Assets/**/*.cs" -Pattern "\bRCore\."
```

Bash:

```bash
grep -RIn --include='*.cs' -E '\bRCore\.' Assets/
```

Remaining references phải được phân loại:

- deliberate keep-on-RCore;
- not migrated yet;
- missed migration.

---

## 15. Rollback strategy

Mỗi PR phải revert được độc lập.

Quy tắc:

- Foundation revert cuối cùng, vì module khác phụ thuộc.
- UI/Data/Addressables revert trước nếu phát hiện bug runtime.
- Không partial revert một file nếu scene/prefab serialized state đã đổi nhiều; revert cả PR.
- Nếu migration branch quá lớn, dừng lại và tách PR nhỏ hơn.

Thứ tự revert thường là ngược thứ tự migrate:

1. Tools
2. Addressables
3. UI
4. Inspector
5. Data
6. Audio
7. Pool
8. Timer
9. Prefs
10. Foundation

---

## 16. PR strategy

Khuyến nghị PR split:

1. `migration/foundation-ported-renamed`
2. `migration/prefs`
3. `migration/timer-replace-events`
4. `migration/pool`
5. `migration/audio`
6. `migration/data-jobjectdb`
7. `migration/inspector`
8. `migration/ui`
9. `migration/addressables`
10. `migration/remove-rcore-if-safe`

Mỗi PR nên có:

- scope rõ;
- mapping rows liên quan;
- screenshots/video nếu UI;
- save-file test result nếu Data;
- memory/lifetime note nếu Addressables;
- list remaining RCore refs.

---

## 17. Khi nào được xóa Assets/RCore?

Chỉ cân nhắc xóa RCore khi tất cả đúng:

- Search `using RCore` không còn kết quả ngoài deliberately excluded files.
- Search `RCore.` không còn kết quả ngoài docs/comments deliberate.
- Project compile clean không cần RCore asmdefs/scripts.
- Scenes/prefabs không có missing script do RCore removal.
- Save migration đã chạy đủ lâu nếu có Data/Prefs change.
- Team đồng ý không cần giữ RCore làm fallback.

Nếu còn nghi ngờ, giữ RCore. Không có lợi ích lớn khi xóa sớm.

---

## 18. Quick migration worksheet

Copy block này vào issue/PR tracking:

```markdown
## RCore → RevCore migration worksheet

### Scope
- Module:
- Feature boundary:
- Owner:

### Inventory
- RCore types/APIs used:
- Map statuses: PORTED / RENAMED / REPLACED / DROPPED / GAP
- Remaining RCore refs expected after PR:

### Changes
- Namespace/type renames:
- Replacement rewrites:
- Serialized prefab/scene changes:
- Save/data changes:

### Risk
- Runtime behavior risk:
- Save compatibility risk:
- Addressables lifetime risk:
- Rollback plan:

### Test plan
- [ ] Compile clean
- [ ] EditMode tests
- [ ] PlayMode tests
- [ ] Manual smoke
- [ ] Save compatibility
- [ ] Remaining RCore refs reviewed
```

---

## 19. Tóm tắt quyết định quan trọng

- Migrate từng module, không rewrite toàn project.
- Giữ RCore đến cuối.
- PORTED/RENAMED có thể mechanical.
- REPLACED phải review behavior.
- DROPPED/GAP không tự port vội.
- Data migration phải test bằng save thật.
- UI migration phải test bằng scene/prefab thật.
- Addressables migration phải test lifetime/release/cancellation.
- Xóa `Assets/RCore/` là bước cuối, không phải bước đầu.
