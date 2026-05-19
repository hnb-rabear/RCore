# RevCore — Tài Liệu Giới Thiệu Chi Tiết

> Cập nhật: 2026-05-19  
> Audience: team đang cân nhắc dùng RevCore cho project Unity mới hoặc migrate dần từ RCore.  
> Mục tiêu: giải thích RevCore là gì, giải quyết vấn đề gì, gồm module nào, dùng ra sao, và cần lưu ý gì trước khi adopt.

---

## 1. RevCore là gì?

RevCore là thế hệ kế tiếp của RCore — framework Unity nội bộ đang được tách lại thành nhiều UPM package độc lập, có contract rõ, dễ test hơn, dễ review hơn, và phù hợp để dùng cho nhiều team.

RCore cũ vẫn tồn tại trong repo tại `Assets/RCore/` và vẫn có thể tiếp tục dùng cho project đang production. RevCore không yêu cầu flag-day migration. Project có thể dùng RCore và RevCore song song vì namespace khác nhau:

- RCore: namespace dạng `RCore.*`
- RevCore: namespace phẳng `RevCore`

Điểm quan trọng: RevCore không phải bản copy lại toàn bộ RCore. RCore là nguồn tham chiếu legacy; RevCore chọn lại phần đáng giữ, bỏ phần không còn phù hợp, rewrite API để phù hợp hơn với hệ modular, testable, semver-stable.

---

## 2. RevCore giải quyết vấn đề gì?

### 2.1 RCore là monolith khó kiểm soát

RCore nhiều năm phát triển theo nhu cầu project thật, nên có rất nhiều helper, service, editor tool, wrapper, và integration nằm chung. Khi project chỉ cần một phần nhỏ, vẫn dễ kéo theo nhiều thứ không cần.

RevCore giải quyết bằng cách tách framework thành UPM packages:

- cài đúng module cần dùng;
- dependency khai báo rõ trong `package.json` + asmdef;
- package nào không cần thì không cài;
- package build độc lập hơn, dễ test hơn.

### 2.2 API contract của RCore chưa đủ chặt

RCore là framework legacy, không có quy trình public API paper trail, XML doc coverage 100%, deprecation policy, semver rõ ràng cho mọi public member.

RevCore đặt các gate này làm mặc định:

- public API additions đi qua `PublicAPI.Unshipped.txt`;
- release cut promote sang `PublicAPI.Shipped.txt`;
- public members phải có XML docs;
- breaking change cần deprecation window;
- root `CHANGELOG.md` là nguồn thông tin release chính;
- benchmark có baseline + tolerance cho hot path.

### 2.3 Migration cần an toàn, không ép project đang ship

RevCore cho phép migrate từng phần:

- tính năng mới viết bằng RevCore;
- tính năng cũ giữ RCore;
- module nào sẵn sàng thì chuyển trước;
- không cần rewrite toàn project một lần.

Tài liệu migration chính nằm ở [migration README](migration/README.md), map chi tiết nằm ở [migration CSV](migration/rcore-to-revcore-api-map.csv).

---

## 3. Trạng thái hiện tại

| Hạng mục | Trạng thái |
| --- | --- |
| Unity version | 2022.3 LTS |
| Runtime packages | 9 |
| Editor-only packages | 1 (`Tools`) |
| XML doc coverage | 1015 / 1015 (100%) |
| Public API entries sealed | 1404 |
| Test baseline | 160 EditMode tests trên core v1.0 baseline |
| Addressables package | v1.0.0 branch ready for PR |
| UniTask async API | Timer/Audio v1.1.0 + Addressables v1.0.0 |
| RCore status | Legacy, frozen by default, still supported for current consumers |

---

## 4. Package overview

### 4.1 Foundation

Path: `Assets/RevCore/Foundation`  
UPM ID: `com.rabear.revcore.foundation`  
Dependency: none

Foundation là package gốc của RevCore. Nó chứa các primitive và contract dùng chung:

- `IEventBus`, `EventBus`, `Events`
- `IServiceLocator`, `ServiceLocator`, `Services`
- `IRevLogger`, `Log`, `RevLog`
- `Result`, `Result<T>`
- `BigNumber`
- `SerializableDictionary<TKey,TValue>`
- helper dependency-free cho math, time, color, transform, component, string, collection
- `IRevDiagnostics` observability hooks

Dùng Foundation khi project cần lớp nền nhẹ, không phụ thuộc Unity scene nhiều, dễ unit test.

Ví dụ EventBus:

```csharp
using RevCore;

public readonly struct DamageEvent : IEvent
{
	public readonly int Amount;

	public DamageEvent(int amount)
	{
		Amount = amount;
	}
}

Events.Subscribe<DamageEvent>(evt => Log.Info($"Damage: {evt.Amount}"));
Events.Publish(new DamageEvent(10));
```

Ví dụ Result:

```csharp
using RevCore;

public Result<int> GetLevel(string id)
{
	if (string.IsNullOrEmpty(id))
		return Result<int>.Fail("Level id is empty.");

	return Result<int>.Ok(1);
}
```

### 4.2 Inspector

Path: `Assets/RevCore/Inspector`  
UPM ID: `com.rabear.revcore.inspector`  
Dependency: none

Inspector gom các attribute và drawer hỗ trợ authoring trong Unity Inspector:

- `[ReadOnly]`
- `[ShowIf]`
- `[AutoFill]`
- `[InspectorButton]`
- `[CreateScriptableObject]`
- `[ExposeScriptableObject]`
- tag/layer/folder selectors

Dùng Inspector khi team muốn giảm boilerplate editor script nhưng vẫn giữ runtime sạch.

### 4.3 Timer

Path: `Assets/RevCore/Timer`  
UPM ID: `com.rabear.revcore.timer`  
Dependencies: Foundation, UniTask (hard dependency từ v1.1.0)

Timer cung cấp scheduler plain C# và Unity drivers:

- `TimerScheduler`
- `ITimerScheduler`
- `ITimerHandle`
- `Timers`
- `TimerDriver`
- `GlobalTimers`
- `SceneTimers`

Use cases:

- delayed callback;
- wait for condition;
- debounce event;
- scheduler unit-testable không cần scene;
- async delay/wait API qua UniTask.

Ví dụ callback:

```csharp
using RevCore;

Timers.WaitForSeconds(3f, SpawnWave);
```

Ví dụ manual tick:

```csharp
var scheduler = new TimerScheduler();
scheduler.WaitForSeconds(1f, OnDone);
scheduler.Tick(deltaTime, unscaledDeltaTime);
```

### 4.4 Prefs

Path: `Assets/RevCore/Prefs`  
UPM ID: `com.rabear.revcore.prefs`  
Dependency: Foundation

Prefs là wrapper typed quanh `PlayerPrefs` và `EditorPrefs`:

- `PlayerPrefBool`
- `PlayerPrefInt`
- `PlayerPrefFloat`
- `PlayerPrefString`
- list/dictionary/object wrappers
- `PlayerPrefContainer.SaveChanges()` để flush dirty values

Prefs không cố mã hóa dữ liệu. Đây là quyết định thiết kế: hardcoded-key obfuscation không bảo vệ được dữ liệu nhạy cảm. Sensitive state phải validate server-side.

Ví dụ:

```csharp
using RevCore;

private PlayerPrefBool m_musicEnabled = new("music_enabled", true);
private PlayerPrefInt m_highScore = new("high_score", 0);

m_musicEnabled.Value = false;
m_highScore.Value = 100;
PlayerPrefContainer.SaveChanges();
```

### 4.5 Pool

Path: `Assets/RevCore/Pool`  
UPM ID: `com.rabear.revcore.pool`  
Dependencies: Foundation, Timer

Pool cung cấp pooling cho `Component` và `GameObject`:

- `RevPool<T>` quản lý một prefab type;
- `PoolsContainer<T>` quản lý nhiều pool theo prefab;
- delayed release qua Timer;
- zero-alloc iteration APIs;
- editor debug drawer tách khỏi runtime.

Ví dụ:

```csharp
using RevCore;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
	[SerializeField] private PoolObject bulletPrefab;
	private PoolsContainer<PoolObject> m_pools;

	private void Awake()
	{
		m_pools = new PoolsContainer<PoolObject>("BulletPools", 10, transform);
	}

	public void Fire(Vector3 position)
	{
		var bullet = m_pools.Spawn(bulletPrefab, position);
		m_pools.Get(bulletPrefab).Release(bullet, 2f);
	}
}
```

### 4.6 Audio

Path: `Assets/RevCore/Audio`  
UPM ID: `com.rabear.revcore.audio`  
Dependencies: Foundation, Inspector, Prefs, UniTask. DOTween và Addressables là optional integrations.

Audio quản lý clip repository, SFX pooling, music playback, fading, và UI SFX event:

- `AudioCollection`
- `BaseAudioManager`
- `AudioManager`
- `SfxSource`
- `UISfxTriggeredEvent`
- Audio ID generator editor tool

Optional integrations:

- Addressables: enable addressable clip fields/load-unload.
- DOTween: smooth volume fading; fallback coroutine nếu không có.
- UniTask: async fade API v1.1.0, hard dependency của package Audio.

Ví dụ flow:

```csharp
AudioManager.Instance.PlaySFX("button_click");
AudioManager.Instance.PlayMusicById(0);
Events.Publish(new UISfxTriggeredEvent("button_click"));
```

### 4.7 Data

Path: `Assets/RevCore/Data`  
UPM ID: `com.rabear.revcore.data`  
Dependencies: Foundation, Timer, Prefs, Inspector, Newtonsoft JSON

Data chứa JObjectDB V2 — hệ persistence kiểu Model–Collection–Manager:

- `JObjectData`: POCO raw data;
- `JObjectModel<T>`: ScriptableObject chứa data + business lifecycle;
- `JObjectModelCollection`: aggregator và `[Inject]` cross-model;
- `JObjectDBManager<T>`: MonoBehaviour wire lifecycle với app pause/quit;
- auto-save throttling, forced save on pause/quit;
- PlayerPrefs JSON backend.

Dùng Data khi project cần save local có lifecycle rõ, dễ test logic model, và không cần database backend lớn.

Ví dụ data:

```csharp
[Serializable]
public class PlayerData : JObjectData
{
	public int coins;
	public int level = 1;
}
```

Ví dụ model:

```csharp
[CreateAssetMenu(menuName = "Game/PlayerModel")]
public class PlayerModel : JObjectModel<PlayerData>
{
	public override void Init() { }
	public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds) { }
	public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds) { }
	public override void OnUpdate(float deltaTime) { }
	public override void OnPreSave(int utcNowTimestamp) { }
	public override void OnRemoteConfigFetched() { }
}
```

### 4.8 UI

Path: `Assets/RevCore/UI`  
UPM ID: `com.rabear.revcore.ui`  
Dependencies: Foundation, Inspector, Pool, TextMeshPro; DOTween optional

UI chứa runtime UI components và helpers:

- `PanelStack`, `PanelController`, `PanelRoot`
- `JustButton`, `SimpleTMPButton`
- `JustToggle`, `CustomToggleGroup`
- `OptimizedScrollView` family
- layout/safe-area helpers
- `HoledLayerMask` tutorial focus mask

UI tích hợp với Foundation Events cho panel routing và button SFX.

Dùng UI khi project cần panel navigation, reusable buttons/toggles, optimized scroll views, hoặc tutorial mask.

### 4.9 Addressables

Path: `Assets/RevCore/Addressables`  
UPM ID: `com.rabear.revcore.addressables`  
Dependencies: Unity Addressables, UniTask  
RevCore dependencies: none

Addressables là package standalone, UniTask-first, thay thế nhóm RCore `AddressableUtil`, `AssetBundleRef<T>`, keyed asset refs, component/prefab refs.

Runtime surface:

- `AddressableLoadException`
- `AddressableLoader`
- `AddressableDownloader`
- `AddressableCatalog`
- `AddressableScene`
- `AssetRef<T>`
- `KeyedAssetRef<TKey, T>`
- `ComponentRef<TComponent>`
- `PrefabRef<TComponent>`

Contract chính:

- helpers stateless;
- wrappers serializable + cache runtime state;
- `CancellationToken ct = default` cho async methods;
- `IProgress<float> progress = null` cho load/download progress;
- terminal failure throw `AddressableLoadException`;
- cancellation không release handle ngay, mà release khi operation complete;
- `LoadAssetWithHandleAsync<T>` dành cho caller-owned lifetime.

Ví dụ:

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

### 4.10 Tools

Path: `Assets/RevCore/Tools`  
UPM ID: `com.rabear.revcore.tools`  
Dependency: none  
Build target: Editor-only

Tools là hub editor:

- Navigate;
- Search;
- Generators;
- UI tools;
- Utility tools;
- extension point qua `RevCoreTool`.

Tools không đi vào runtime build.

---

## 5. Dependency graph

```text
Foundation  ──────────►  (Inspector)        (Tools — editor-only, standalone)
   ▲   ▲   ▲   ▲                 ▲
   │   │   │   │                 │
 Timer  Prefs  │                 │
   ▲     ▲    │                 │
   │     │    │                 │
   │   Pool ──┘                  │
   │     ▲                       │
   │     │                       │
   ├── Audio ──────────► Inspector + Prefs
   ├── Data  ──────────► Timer + Prefs + Inspector + Newtonsoft JSON
   └── UI    ──────────► Inspector + Pool + TMP

Addressables ─────────► Unity Addressables + UniTask (standalone, zero RevCore deps)
```

Quy tắc đọc: `A → B` nghĩa là A phụ thuộc B.

Các package gốc:

- Foundation: base runtime package.
- Inspector: editor/inspector helpers nhưng runtime assembly không phụ thuộc package khác.
- Tools: editor-only, standalone.
- Addressables: standalone so với RevCore packages, nhưng phụ thuộc Unity Addressables + UniTask.

---

## 6. Khi nào dùng module nào?

| Nhu cầu | Module nên dùng |
| --- | --- |
| Event/message giữa systems | Foundation (`IEventBus`, `Events`) |
| Lightweight DI/service lookup | Foundation (`IServiceLocator`, `Services`) |
| Expected failure không muốn throw exception | Foundation (`Result<T>`) |
| Delay, condition wait, debounce | Timer |
| Save setting đơn giản | Prefs |
| Local game data model/save lifecycle | Data |
| Pool prefab/game object | Pool |
| SFX/music manager | Audio |
| Panel navigation/buttons/scroll views | UI |
| Addressables load/download/catalog/scene | Addressables |
| Inspector authoring helper | Inspector |
| Editor productivity tools | Tools |

---

## 7. Recommended adoption path

### 7.1 Project mới

Khuyến nghị bắt đầu bằng:

1. Foundation
2. Inspector
3. Timer
4. Prefs
5. Pool hoặc UI nếu project cần gameplay object reuse/UI navigation
6. Audio nếu project cần SFX/music stack
7. Data nếu project cần local save model
8. Addressables nếu project dùng remote/local addressable assets
9. Tools nếu team muốn editor productivity hub

Không cần cài full bộ ngay. Cài theo nhu cầu thật của project.

### 7.2 Project đang dùng RCore

Không migrate hàng loạt. Làm theo hướng an toàn:

1. Giữ RCore cho feature đang ổn định.
2. Viết feature mới bằng RevCore nếu không đụng legacy coupling sâu.
3. Chọn module dễ migrate trước: Foundation helpers, Prefs, Timer, Pool.
4. Với Data/UI/Audio, migrate theo feature boundary, không theo file-by-file random.
5. Với Addressables, dùng migration map để thay `AddressableUtil` và `AssetBundleRef` family bằng RevCore.Addressables wrappers/helpers.
6. Chạy Unity Test Runner + manual smoke trước khi thay thế trong production scene.

### 7.3 Team lớn hơn một người

Cần thêm convention:

- EventBus dùng cho fire-and-forget notification.
- ServiceLocator dùng cho query/service có return value.
- Không register service tùy tiện trong gameplay runtime; register ở composition root/startup.
- Public API addition phải có review.
- Module nào thêm dependency mới phải được review vì ảnh hưởng package graph.
- Không dùng RevCore package để gọi ngược vào RCore.

---

## 8. Install guide

### 8.1 Local path install

Trong Unity Package Manager, chọn **Add package from disk** hoặc khai báo local path trong `manifest.json`.

Ví dụ cài Foundation:

```json
"com.rabear.revcore.foundation": "file:Assets/RevCore/Foundation"
```

Ví dụ cài Addressables:

```json
"com.rabear.revcore.addressables": "file:Assets/RevCore/Addressables"
```

### 8.2 Git URL install

Khi package được publish qua git tag, consumer có thể pin bằng `?path=` + tag:

```json
"com.rabear.revcore.foundation": "https://github.com/hnb-rabear/RCore.git?path=Assets/RevCore/Foundation#v1.0.0"
```

Addressables (khi branch được merge vào main và tag được cut):

```json
"com.rabear.revcore.addressables": "https://github.com/hnb-rabear/RCore.git?path=Assets/RevCore/Addressables#v1.1.0"
```

> **Lưu ý:** Tag `v1.0.0` được cut trước khi Addressables package tồn tại. Pin Addressables bằng tag `v1.1.0` trở lên sau khi branch `feat/addressables-v1.0` được merge.

### 8.3 Dependency lưu ý

Một số package cần Unity/third-party packages:

| RevCore package | External dependency |
| --- | --- |
| Data | `com.unity.nuget.newtonsoft-json` |
| UI | TextMeshPro |
| Addressables | `com.unity.addressables`, `com.cysharp.unitask` |
| Timer | `com.cysharp.unitask` |
| Audio | `com.cysharp.unitask` |
| Audio optional fade | DOTween |
| Audio optional addressable clips | Unity Addressables |

---

## 9. Quality gates

RevCore dùng các gate sau để giữ framework ổn định:

### 9.1 XML docs

Mọi public member phải có XML documentation. Kiểm tra bằng:

```powershell
python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json
```

Expected hiện tại:

```text
Public members: 1015
Documented: 1015
Undocumented: 0
Coverage: 100.00%
```

### 9.2 Public API paper trail

Mỗi module có:

- `Runtime/PublicAPI.Shipped.txt`
- `Runtime/PublicAPI.Unshipped.txt`

Public API mới vào `Unshipped`. Khi release cut:

```powershell
python scripts/seal-public-api.py --root Assets/RevCore
```

### 9.3 API inventory

Sau khi thay đổi public surface:

```powershell
python scripts/extract-api-surface.py --out docs/api-inventory.csv
```

### 9.4 Benchmark

Sau Unity Performance run:

```powershell
python scripts/check-benchmark-regression.py --results Library/ --baseline scripts/benchmark-baseline.json
```

### 9.5 Tests

Unity Test Runner vẫn là nguồn chính cho EditMode/PlayMode tests. CLI environment hiện tại không luôn có Unity Editor, nên PR test plan phải ghi rõ test nào đã chạy, test nào skipped do Unity unavailable.

---

## 10. Design principles

### 10.1 Modular first

Mỗi package phải hiểu được độc lập. Nếu package cần dependency, dependency phải khai báo rõ.

Ví dụ đúng:

- Pool phụ thuộc Timer vì delayed release cần scheduler.
- Data phụ thuộc Prefs vì backend dùng PlayerPrefs wrapper.
- Addressables không phụ thuộc Foundation vì package cần standalone, chỉ wrap Unity Addressables + UniTask.

Ví dụ sai:

- Foundation gọi Audio.
- UI gọi Data trực tiếp.
- Addressables gọi Timer/Pool/Foundation chỉ vì cùng namespace.

### 10.2 Runtime và Editor tách rõ

Editor code ở Editor asmdef. Runtime asmdef không chứa `UnityEditor` dependency.

### 10.3 Expected failure không lạm dụng exception

Business failure dự kiến dùng `Result<T>`. Exception dành cho lỗi bất thường hoặc external API failure.

Addressables là ngoại lệ hợp lý: terminal Addressables failure throw `AddressableLoadException` vì đây là async external operation failure cần propagate rõ.

### 10.4 No hidden magic

RevCore tránh pattern khó debug:

- không auto scan toàn project bằng reflection cho core service;
- không silent dependency;
- không encryption giả an toàn;
- không editor-only logic trong runtime.

### 10.5 RCore coexistence

RevCore không cố xóa RCore. Framework mới có thể sống cạnh framework cũ để giảm rủi ro migration.

---

## 11. Tradeoffs cần biết

### 11.1 PublicAPI analyzer đang dormant

`Microsoft.CodeAnalysis.PublicApiAnalyzers` có trong repo nhưng chưa bật live vì Unity load analyzer project-wide. Nếu bật, analyzer có thể bắn lỗi trên legacy RCore folders không track PublicAPI. Hiện tại PublicAPI files là paper trail cho review, chưa phải compile-time enforcement.

### 11.2 CI còn tối thiểu

Repo hiện thiên về local checks + release workflow. Team lớn hơn nên thêm PR-time gates cho:

- Unity compile;
- EditMode tests;
- PlayMode tests;
- XML docs;
- API inventory diff;
- benchmark regression nếu chạm hot path.

### 11.3 Data dùng PlayerPrefs JSON

Phù hợp local save vừa/nhỏ. Không phù hợp dữ liệu lớn hoặc server-authoritative data. Không có built-in schema versioning; migration data nên xử lý trong `OnPostLoad`.

### 11.4 Prefs không encryption

Không dùng Prefs cho dữ liệu nhạy cảm nếu không có server validation.

### 11.5 ServiceLocator dễ bị abuse

Nếu dùng như global mutable state bừa bãi, code sẽ khó trace. Cần convention rõ: register ở startup, unregister theo lifecycle, dùng scoped locator cho feature ngắn hạn.

---

## 12. Migration từ RCore

RevCore không ép migration. Khi cần migrate, dùng các tài liệu này:

- [Migration README](migration/README.md)
- [Migration plan](migration/PLAN.md)
- [API map CSV](migration/rcore-to-revcore-api-map.csv)
- [API map markdown](migration/rcore-to-revcore-api-map.md)
- [Gap categories](migration/gap-categories.md)

Status chính trong map:

| Status | Ý nghĩa |
| --- | --- |
| PORTED | Cùng type, chuyển namespace/module. Thường update `using`. |
| RENAMED | Có type RevCore tương ứng nhưng tên khác. Mechanical rename. |
| REPLACED | API RCore được thay bằng thiết kế RevCore mới. Cần chỉnh call-site theo contract mới. |
| DROPPED | Không mang sang RevCore. Dùng giải pháp khác hoặc giữ RCore. |
| GAP | Chưa có RevCore equivalent. Quyết định theo consumer demand. |

Nhóm Addressables đã có replacement map riêng trong CSV:

- `AddressableUtil` → `AddressableLoader`, `AddressableDownloader`, `AddressableCatalog`, `AddressableScene`
- `AssetBundleRef<T>` → `AssetRef<T>`
- keyed variants → `KeyedAssetRef<TKey,T>`
- component refs → `ComponentRef<TComponent>` / `PrefabRef<TComponent>`

---

## 13. Quickstart đề xuất cho team mới

### Bước 1: Cài Foundation

Cài package Foundation trước. Tạo một event nhỏ và dùng `Events.Publish` / `Events.Subscribe` trong một scene test.

### Bước 2: Cài Timer

Thử delayed callback:

```csharp
Timers.WaitForSeconds(1f, () => Log.Info("Timer done"));
```

Nếu không dùng `GlobalTimers`, nhớ tick scheduler thủ công.

### Bước 3: Cài Prefs

Lưu setting đơn giản, verify value vẫn còn sau restart Play Mode.

### Bước 4: Chọn module theo feature

- Gameplay spawn nhiều object → Pool.
- UI có panel stack/scroll view → UI.
- SFX/music → Audio.
- Local save model → Data.
- Remote/local addressable assets → Addressables.

### Bước 5: Chạy test + smoke

- Unity EditMode tests.
- PlayMode tests nếu module có runtime scene behavior.
- Manual smoke trong scene thật.

---

## 14. FAQ

### RevCore có thay thế RCore hoàn toàn không?

Không bắt buộc. RevCore là hướng mới cho feature mới và project mới. RCore vẫn có thể giữ trong project production.

### Có nên migrate project đang ship không?

Chỉ migrate khi có lý do rõ: cần API contract tốt hơn, cần module RevCore cụ thể, hoặc đang làm feature mới tách được khỏi legacy. Không migrate vì “cho sạch”.

### RevCore có cần cài toàn bộ không?

Không. Cài theo module. Foundation thường là điểm bắt đầu, nhưng Addressables cũng có thể cài độc lập vì không phụ thuộc Foundation.

### RevCore có phụ thuộc UniTask không?

Foundation / Pool / Data / UI không bắt buộc UniTask. Timer v1.1.0, Audio v1.1.0 và Addressables v1.0.0 khai báo UniTask là package dependency.

### RevCore có encryption PlayerPrefs không?

Không. Đây là quyết định cố ý. Dữ liệu nhạy cảm phải validate server-side.

### Public API có được bảo vệ không?

Có paper trail qua `PublicAPI.Shipped.txt` / `Unshipped.txt`, XML docs gate, semver/deprecation policy. Roslyn PublicAPI analyzer đang dormant do limitation của Unity analyzer scope.

### Có migration tool tự động chưa?

Chưa. Migration map đã sẵn sàng làm input, nhưng chưa ship tool vì auto rewrite nửa đúng nửa sai nguy hiểm hơn manual migration có kiểm soát.

---

## 15. Tài liệu liên quan

- [Architecture](ARCHITECTURE.md) — kiến trúc, dependency graph, tradeoffs.
- [Session handoff](SESSION_HANDOFF.md) — trạng thái branch/work gần nhất.
- [Contributing README](contributing/README.md) — quy trình đóng góp.
- [API design guidelines](contributing/API_DESIGN_GUIDELINES.md) — naming, nullability, threading, error model.
- [SemVer policy](contributing/SEMVER_POLICY.md) — rule bump version.
- [Deprecation policy](contributing/DEPRECATION_POLICY.md) — deprecation window.
- [Public API guide](contributing/PUBLIC_API_GUIDE.md) — Shipped/Unshipped workflow.
- [Release checklist](contributing/RELEASE_CHECKLIST.md) — release gate.
- [Migration README](migration/README.md) — RCore → RevCore migration.
- [Foundation README](../Assets/RevCore/Foundation/README.md)
- [Timer README](../Assets/RevCore/Timer/README.md)
- [Prefs README](../Assets/RevCore/Prefs/README.md)
- [Pool README](../Assets/RevCore/Pool/README.md)
- [Audio README](../Assets/RevCore/Audio/README.md)
- [Data README](../Assets/RevCore/Data/README.md)
- [UI README](../Assets/RevCore/UI/README.md)
- [Addressables README](../Assets/RevCore/Addressables/README.md)
- [Tools README](../Assets/RevCore/Tools/README.md)
