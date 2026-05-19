# RevCore — Tài Liệu Kiến Trúc

> Phiên bản: **v1.1.0** (cập nhật 2026-05-19 — thêm RevCore.Addressables v1.0.0 + UniTask async API)
> Dành cho team review để quyết định có nên áp dụng RevCore cho dự án hay không.
> Tài liệu trình bày ý tưởng, modules, cách thiết kế, luồng hoạt động, điểm mạnh và điểm yếu — không lặp lại chi tiết API (xem README từng package).

---

## Mục lục

1. [RevCore là gì](#1-revcore-là-gì)
2. [Mục tiêu thiết kế](#2-mục-tiêu-thiết-kế)
3. [Modules và sơ đồ phụ thuộc](#3-modules-và-sơ-đồ-phụ-thuộc)
4. [Triết lý thiết kế](#4-triết-lý-thiết-kế)
5. [Mẫu kiến trúc cốt lõi](#5-mẫu-kiến-trúc-cốt-lõi)
6. [Luồng hoạt động](#6-luồng-hoạt-động)
7. [Điểm mạnh](#7-điểm-mạnh)
8. [Điểm yếu và điều cần cân nhắc](#8-điểm-yếu-và-điều-cần-cân-nhắc)
9. [Khuyến nghị áp dụng](#9-khuyến-nghị-áp-dụng)
10. [Tài liệu liên quan](#10-tài-liệu-liên-quan)

---

## 1. RevCore là gì

RevCore là thế hệ kế tiếp của RCore — một Unity game framework nội bộ, được rebuild thành **bộ UPM packages độc lập** thay vì một monolith. Mỗi package cài riêng lẻ qua git URL hoặc local path, không kéo theo dependency thừa.

Trạng thái hiện tại:

| Hạng mục | Giá trị |
| --- | --- |
| Phiên bản | v1.1.0 docs line; RevCore.Addressables v1.0.0 package branch ready for PR |
| Số packages | 9 runtime + 1 editor-only (Tools) |
| Unity version | 2022.3 LTS (đã verify Unity 6 ở Phase 9) |
| EditMode tests | 160 baseline; Addressables test suite added but Unity CLI unavailable in shell |
| XML doc coverage | 1015 / 1015 (100%) |
| Public API entries | 1404 sealed entries after Addressables + Timer/Audio async promotion |
| Benchmark suite | Có (Unity.PerformanceTesting), tolerance 5% so với baseline |

RCore (legacy) vẫn được giữ nguyên trong cùng repo (`Assets/RCore/`), không có hạn chót migration.

---

## 2. Mục tiêu thiết kế

- **Cài đúng cái cần.** Không monolith. Dự án chỉ cần `Pool` thì không phải kéo `Audio`, `UI`, hay `Data`.
- **Học theo module.** Mỗi package có README + samples riêng. Onboarding tuần tự được, không phải nuốt cả framework.
- **Test plain C#.** Foundation / Timer / EventBus / Result chạy không cần Unity scene → headless CI khả thi.
- **Cùng tồn tại với RCore.** Khác namespace (`RevCore` flat vs `RCore.*` nested), không compile collision. Migrate dần được.
- **Contract ổn định.** Public surface có paper trail, breaks phải major bump kèm deprecation window.

---

## 3. Modules và sơ đồ phụ thuộc

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

Addressables  ──────────►  Unity Addressables + UniTask   (standalone, zero RevCore deps)
```

Mũi tên `A → B` đọc là "A phụ thuộc vào B". Foundation và Inspector không phụ thuộc package nào khác → đây là các package gốc (zero-dependency base). Tools đứng riêng.

### Bảng tóm tắt từng package

| Package | UPM ID | Mục đích | Phụ thuộc runtime |
| --- | --- | --- | --- |
| Foundation | `com.rabear.revcore.foundation` | EventBus, ServiceLocator, Result\<T\>, Log, BigNumber, IRevDiagnostics, helpers | none |
| Inspector | `com.rabear.revcore.inspector` | Attributes Inspector: `[ReadOnly]`, `[ShowIf]`, `[AutoFill]`, `[InspectorButton]`, ... | none |
| Timer | `com.rabear.revcore.timer` | Countdown / condition wait / debounce, plain C# scheduler + Unity driver + UniTask async API | Foundation, UniTask |
| Prefs | `com.rabear.revcore.prefs` | Typed wrappers PlayerPrefs / EditorPrefs (bool, List, Dictionary) | Foundation |
| Pool | `com.rabear.revcore.pool` | `RevPool<T>`, `PoolsContainer<T>`, zero-alloc iteration, delayed release | Foundation, Timer |
| Audio | `com.rabear.revcore.audio` | AudioCollection, BaseAudioManager, SFX pool, music fade. Addressables + DOTween optional. | Foundation, Inspector, Prefs, UniTask |
| Data | `com.rabear.revcore.data` | JObjectDB V2 — Model–Collection–Manager, `[Inject]` cross-model DI | Foundation, Timer, Prefs, Inspector, Newtonsoft JSON |
| UI | `com.rabear.revcore.ui` | PanelStack, JustButton / JustToggle, OptimizedScrollView, HoledLayerMask | Foundation, Inspector, Pool, TMP |
| Addressables | `com.rabear.revcore.addressables` | UniTask-first Addressables helpers, downloads/catalogs/scenes, serialisable asset/component/prefab wrappers | Unity Addressables, UniTask |
| Tools | `com.rabear.revcore.tools` | Editor hub: Navigate / Search / Generators / UI Tools / Utility | none (editor-only) |

---

## 4. Triết lý thiết kế

### 4.1 Zero cross-dependency

Mỗi package chỉ biết những phụ thuộc đã khai báo trong `package.json` + asmdef references. Vi phạm gây compile error chứ không qua review. `Pool` không được `using RevCore.Audio` dù cùng project.

### 4.2 Foundation là gốc

Foundation chứa interface và utility chung nhất. Mọi package khác xây trên Foundation; Foundation không biết gì về chúng. Đây là bất biến quan trọng nhất của dependency graph.

### 4.3 Interface trước, implementation sau

Mọi service public đều có interface (`IEventBus`, `ITimerScheduler`, `IAudioService`, ...). Cho phép mock/fake trong test, swap implementation trong production (ví dụ: logger gắn Crashlytics).

### 4.4 UniTask — bắt buộc cho Timer, Audio, Addressables

Foundation / Pool / Data / UI không phụ thuộc UniTask. Timer v1.1.0 và Audio v1.1.0 khai báo `com.cysharp.unitask` là hard dependency — consumer cần cài UniTask để dùng hai package này. Addressables v1.0.0 là UniTask-first vì API native của Addressables vốn dĩ async. Nếu project chỉ dùng Foundation/Pool/Data/UI và không cần async scheduler, có thể bỏ UniTask.

### 4.5 Addressables / DOTween là optional

Kích hoạt qua `versionDefines` trong asmdef + `#if ADDRESSABLES` / `#if DOTWEEN`. Không cài thì code vẫn compile, dùng fallback (coroutine lerp thay tween, direct reference thay address).

### 4.6 Editor tách Runtime

Mỗi package có asmdef riêng cho Runtime và Editor. Build player không bao giờ chứa Editor code. Không dùng `#if UNITY_EDITOR` trong runtime assembly.

### 4.7 No encryption cho PlayerPrefs

Quyết định cố ý — hardcoded-key obfuscation chỉ chặn PlayerPrefs editor, không chặn được ai có binary. Sensitive state phải validate server-side. Không reopen.

---

## 5. Mẫu kiến trúc cốt lõi

Phần này tóm tắt khái niệm. Chi tiết API + ví dụ code xem README từng package.

### 5.1 EventBus — pub/sub type-safe

Type là key, không string. Hỗ trợ global (`Events.Publish/Subscribe`) hoặc scoped (`new EventBus()` cho battle, level, ...). Post-v1.0 đã zero-alloc trên `Publish` hot path.

> Dùng khi: thông báo cross-system, decoupled sender/receiver.
> Không dùng khi: query có giá trị trả về (→ ServiceLocator), giao tiếp nội bộ một MonoBehaviour (→ method call thẳng).

### 5.2 ServiceLocator — DI nhẹ

Registry tra cứu service theo Type. Không reflection, không string key. Global hoặc scoped. Thay thế `FindObjectOfType<T>` đắt tiền và `Singleton<T>` cứng nhắc.

### 5.3 Result\<T\> — error model không exception

Trả về `Result<T>.Ok(value)` hoặc `Result<T>.Fail(message)` cho expected failure. Public read API là `TryGetValue(out T value)` hoặc `ValueOr(T fallback)`. Caller bắt buộc xử lý cả hai nhánh — không crash silent.

### 5.4 Timer — async bằng callback

Scheduler plain C# (`ITimerScheduler`) + Unity driver. Hỗ trợ countdown, condition wait, debounce. Không tự chạy — cần driver gọi `Tick(deltaTime, unscaledDeltaTime)`. `GlobalTimers.Instance` cho persistent, `SceneTimers` cho scene-scoped.

### 5.5 JObjectDB V2 — game data persistence

Model–Collection–Manager:

- `JObjectData` (POCO) ─ raw data
- `JObjectModel<T>` (ScriptableObject) ─ data + business logic + lifecycle hooks (`Init`, `OnPostLoad`, `OnUpdate`, `OnPause`, `OnPreSave`)
- `JObjectModelCollection` ─ aggregator, resolve `[Inject]` cross-model
- `JObjectDBManager<T>` (MonoBehaviour) ─ wire lifecycle với app pause/quit, auto-save throttled

Backend là PlayerPrefs lưu JSON (Newtonsoft). `SaveForced()` bypass throttle ở `OnApplicationPause/Quit` để không mất data cuối session.

### 5.6 Pool

- `RevPool<T>` ─ một loại prefab, LIFO, pre-warm + auto-relocate
- `PoolsContainer<T>` ─ pool-of-pools, tự tạo pool mỗi loại prefab
- Delayed release qua Timer
- Iteration zero-alloc: `ForEachActive` / `ForEachItem` (không cấp phát `List<T>` mỗi call)

### 5.7 IRevDiagnostics — observability opt-in

10 hooks ở hot path: Timer (scheduled / cancelled / completed), EventBus (subscribed / unsubscribed / published), Pool (spawn / release), Audio (PlaySFX / PlayMusic). Default không listener → cost = 1 null check per hook. Wire một implementation qua `RevDiagnostics.Listener` ở startup. Payload metadata-only (không giữ object ref → không kéo dài GC lifetime).

Ba reference listener kèm Foundation samples: `CrashBufferDiagnostics`, `DebugOverlayDiagnostics`, `UnityLogDiagnostics`.

---

## 6. Luồng hoạt động

### Khởi động game

```text
GameDataManager.Awake
    └─► JObjectDB.RegisterCollection(GameCollection)

GameDataManager.Start → Init
    └─► GameCollection.Load
            ├─► CreateModel(playerModel, "PlayerData")  ─ load JSON từ PlayerPrefs
            ├─► CreateModel(shopModel,   "ShopData")    ─ load JSON từ PlayerPrefs
            └─► CreateModel(sessionModel)               ─ tính offline time

PlayerModel.OnPostLoad(utcNow, offlineSec)
    └─► xử lý offline rewards, validate

Services.Global.Register<IDataService>(gameDataManager)
    └─► các hệ thống khác truy cập qua ServiceLocator
```

### Gameplay loop (per frame)

```text
TimerDriver.LateUpdate
    └─► TimerScheduler.Tick(dt, unscaledDt)
            ├─► countdown callbacks
            ├─► condition callbacks
            └─► debounce flush
```

### Event dispatch

```text
Events.Publish(new DamageEvent(10))
    └─► EventBus.Publish<DamageEvent>
            ├─► HealthPresenter.OnDamage
            ├─► AchievementTracker.OnDamage
            └─► AnalyticsReporter.OnDamage
```

### Save flow

```text
model.Data.coins += reward
model.Save()              ─ dirty + throttle 3s (hoặc Save(now: true) → debounce 200ms)

OnApplicationPause / OnApplicationQuit:
    JObjectDB.SaveAll          ─ flush mọi dirty model
    JObjectDBManager.SaveForced ─ bypass throttle, không mất data cuối
```

---

## 7. Điểm mạnh

1. **Modular UPM thực sự.** 9 packages, mỗi cái có README + samples + tests + CHANGELOG riêng. Cài đúng cái cần, không bị kéo dependency.
2. **Semver stable từ v1.0.0.** Breaks cần major bump + deprecation window 3-stage. Consumer pin được `?path=Assets/RevCore/{Module}#v1.0.0` ổn định.
3. **Public API có paper trail.** 1404 entries trong `PublicAPI.Shipped.txt` per-module (1337 lúc v1.0.0 + 67 từ Timer/Audio async API và Addressables). PR diff thấy mọi addition / removal — review bằng mắt khả thi.
4. **Plain C# core test-friendly.** Foundation / Timer / EventBus / Result / Pool scheduler chạy unit test không cần Unity scene → CI headless khả thi.
5. **Hot path zero-alloc.** `EventBus.Publish`, `Pool.Spawn`, `TimerScheduler.Cancel`, `ListenerCount` đã đo đạc và optimize. Benchmark suite + regression check (5% tolerance) ngăn quay đầu.
6. **Observability không xâm phạm.** `IRevDiagnostics` opt-in, default no-listener cost = 1 null check. Wire vào crash reporter hoặc HUD chỉ vài dòng.
7. **Coverage doc nghiêm.** 1015/1015 XML doc (100%). Gate `check-xmldoc-coverage.py` chặn regression ở local trước push.
8. **Coexist với RCore.** Khác namespace, không conflict. Migrate feature mới sang RevCore, giữ tính năng cũ ở RCore — không cần flag-day rewrite.
9. **Pattern decoupled.** EventBus + ServiceLocator + `[Inject]` cho phép tổ chức code low-coupling mà không kéo DI container nặng (Zenject / VContainer).
10. **Tooling Editor có sẵn.** Tools hub gom Navigate / Search / Generators / UI / Utility, mở rộng được qua kế thừa `RevCoreTool`.
11. **Addressables tách riêng.** `com.rabear.revcore.addressables` thay thế nhóm `AddressableUtil` / `AssetBundleRef` của RCore bằng package standalone, UniTask-first, không phụ thuộc package RevCore nào khác.

---

## 8. Điểm yếu và điều cần cân nhắc

1. **PublicAPI analyzer dormant.** Lý do: Unity load analyzer project-wide → RS0016 nổ trên legacy RCore folders không track PublicAPI. Editorconfig scoping không khắc phục được. Hậu quả: contract enforcement phụ thuộc human review, không phải compile-time. Reactivate chỉ trong audit pass qua hướng dẫn trong `Assets/RevCore/_Analyzers/README.md`.
2. **CI tối thiểu.** Chỉ `release.yml` (trigger tag `v*`). Tests, benchmark, doc coverage chạy local trước push. Hoạt động tốt với solo maintainer + team kỷ luật cao, nhưng team lớn cần thêm PR-time gates.
3. **UniTask không phải core-wide, nhưng là hard dependency của Timer/Audio/Addressables.** Foundation / Pool / Data / UI không cần UniTask. Timer v1.1.0, Audio v1.1.0 và Addressables v1.0.0 cần UniTask — consumer phải cài nếu chọn các package này.
4. **JObjectDB lưu PlayerPrefs.** Không phù hợp save lớn (GB-scale). Không schema versioning (declined chủ ý — `OnPostLoad` hook xử lý migration thủ công). Cross-version save corruption phải tự xử lý.
5. **No encryption.** Sensitive state (currency mua bằng tiền thật, anti-cheat) phải validate server-side. Không có lớp obfuscation in-box.
6. **Phụ thuộc 3rd-party có giới hạn.** Data cần Newtonsoft JSON, UI cần TMP — đều là Unity package chính thống. Optional: DOTween, Addressables.
7. **Tools chỉ Editor.** Không scriptable từ runtime build. Editor-only by design.
8. **Migration tool chưa ship.** Map mechanical (`docs/migration/rcore-to-revcore-api-map.csv`) sẵn sàng làm input. Addressables rows đã được bổ sung cho nhóm `AddressableUtil` / `AssetBundleRef`; GAP types còn lại đã phân loại trong `gap-categories.md`. Build tool khi consumer initiate — chấp nhận chi phí mỗi project tự migrate manual.
9. **Module-level CHANGELOG không sync.** Root `CHANGELOG.md` là canonical. Per-module CHANGELOG nhiều chỗ là scaffold cũ. Low-priority cleanup.
10. **Service locator anti-pattern risk.** ServiceLocator dễ bị abuse thành global state nếu team không kỷ luật. Cần convention: register ở startup, dùng scoped khi feature lifecycle ngắn.
11. **Học EventBus + ServiceLocator cùng lúc.** Hai pattern có overlap. Cần convention rõ: query có return value → ServiceLocator; fire-and-forget notification → EventBus.

---

## 9. Khuyến nghị áp dụng

| Tình huống | Khuyến nghị |
| --- | --- |
| Dự án mới | Dùng RevCore. Tận dụng full feature set + contract ổn định. |
| Dự án đang chạy RCore, vẫn ship được | Không bắt buộc migrate. Tính năng mới viết bằng RevCore, tính năng cũ giữ RCore. |
| Cần API contract ổn định, có deprecation policy | Migrate sang RevCore. RCore không có cam kết semver. |
| Cần encryption tích hợp / save backend khác PlayerPrefs | RevCore không phù hợp ngay. Cần custom layer trên Data, hoặc viết module riêng. |
| Team đã có DI container (Zenject / VContainer) | Cân nhắc dùng song song ServiceLocator + DI, hoặc thay ServiceLocator bằng adapter. |
| Team phụ thuộc nặng UniTask | Cân nhắc chi phí adapt sang callback pattern. |

### Adoption checklist

1. Đọc `Assets/RevCore/Foundation/README.md` → nắm EventBus + ServiceLocator + Result.
2. Review dependency graph ở Mục 3 → quyết định module nào cần.
3. Mở Unity Test Runner → chạy EditMode tests (~25s). Verify 160 baseline tests green (Addressables tests thêm vào khi package được merge vào main).
4. Chạy Performance category → feed `Library/PerformanceTestResults.json` vào `scripts/check-benchmark-regression.py` → verify 5% tolerance.
5. Pilot 1-2 feature nhỏ trước khi commit toàn dự án.
6. Thiết lập convention nội bộ cho EventBus vs ServiceLocator vs `[Inject]` để tránh inconsistency.

---

## 10. Tài liệu liên quan

- `docs/SESSION_HANDOFF.md` — trạng thái post-v1.0, future-work, branch/tag.
- `CHANGELOG.md` — keep-a-changelog format, `[1.0.0]` chi tiết.
- `docs/contributing/README.md` — quick rules, branches, issue templates.
- `docs/contributing/API_DESIGN_GUIDELINES.md` — naming, nullability, threading, error model.
- `docs/contributing/SEMVER_POLICY.md` — khi nào bump MAJOR / MINOR / PATCH.
- `docs/contributing/DEPRECATION_POLICY.md` — 3-stage deprecation flow.
- `docs/contributing/PUBLIC_API_GUIDE.md` — `Shipped.txt` / `Unshipped.txt` + analyzer wiring.
- `docs/contributing/RELEASE_CHECKLIST.md` — hard gate ở mỗi tag cut.
- `docs/contributing/CI_SETUP.md` — bảng local check, analyzer dormant rationale.
- `docs/migration/README.md` — kế hoạch RCore → RevCore (planning-only, no tooling).
- `Assets/RevCore/<Module>/README.md` — API reference + samples từng package.
- `Assets/RevCore/Addressables/README.md` — quickstart + lifetime contract cho package Addressables standalone.
- `Assets/RevCore/_Analyzers/README.md` — analyzer dormant + reactivate procedure.

### Lệnh hữu ích

```powershell
# Doc coverage gate (phải 0 regression)

python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json

# Refresh API inventory

python scripts/extract-api-surface.py --out docs/api-inventory.csv

# Benchmark regression check (sau Unity Performance run)

python scripts/check-benchmark-regression.py --results Library/ --baseline scripts/benchmark-baseline.json

# Promote PublicAPI Unshipped → Shipped ở release cut

python scripts/seal-public-api.py
```

### Menu Editor

- `RevCore > Tools Hub` — tất cả editor tools
- `RevCore > Audio > Generate IDs` — tạo constants cho audio clips
- `RevCore > Data > Log` — xem data đang lưu trong PlayerPrefs
- `RevCore > Prefs > Clear PlayerPrefs` — reset toàn bộ saved data
