# RevCore — Tài Liệu Kiến Trúc

> Tài liệu này mô tả kiến trúc, triết lý thiết kế, và cách làm việc với RevCore.
> Dành cho developer mới tham gia và developer đang đóng góp vào framework.

---

## Mục lục

1. [Tổng quan](#1-tổng-quan)
2. [Triết lý thiết kế](#2-triết-lý-thiết-kế)
3. [Cấu trúc gói và sơ đồ phụ thuộc](#3-cấu-trúc-gói-và-sơ-đồ-phụ-thuộc)
4. [Quy ước thư mục và Assembly](#4-quy-ước-thư-mục-và-assembly)
5. [Các mẫu kiến trúc cốt lõi](#5-các-mẫu-kiến-trúc-cốt-lõi)
6. [Luồng hoạt động theo tầng](#6-luồng-hoạt-động-theo-tầng)
7. [Onboarding — Học RevCore theo thứ tự](#7-onboarding--học-revcore-theo-thứ-tự)
8. [Hướng dẫn đóng góp](#8-hướng-dẫn-đóng-góp)
9. [Di chuyển từ RCore](#9-di-chuyển-từ-rcore)

---

## 1. Tổng quan

RevCore là thế hệ kế tiếp của RCore — một Unity game framework nội bộ. Thay vì một monolith duy nhất, RevCore được thiết kế như một bộ **UPM packages độc lập**, mỗi package có thể được cài đặt riêng lẻ mà không kéo theo phụ thuộc không cần thiết.

**Mục tiêu:**
- Đưa vào dự án chỉ đúng thứ cần dùng, không hơn.
- Developer mới học được từng phần riêng lẻ theo thứ tự.
- Có thể test từng package không cần Unity scene.
- Hỗ trợ cùng tồn tại với RCore trong quá trình chuyển đổi dần.

**Unity version:** 2022.3 LTS  
**Ngôn ngữ:** C#, tab width 4  
**Package prefix:** `com.rabear.revcore.*`

---

## 2. Triết lý thiết kế

### 2.1 Zero cross-dependency

Mỗi package phải hoạt động hoàn toàn độc lập. Không có package nào được phép biết về package khác ngoài những phụ thuộc đã được khai báo rõ trong `package.json`. Điều này được thực thi qua asmdef — Unity sẽ báo lỗi compile nếu vi phạm.

> **Ví dụ vi phạm:** `Pool` không được `using RevCore.Audio`, dù cả hai đều nằm trong cùng một project.

### 2.2 Tầng Foundation là lá cây

`Foundation` là package duy nhất không phụ thuộc vào ai. Tất cả các package khác đều có thể phụ thuộc vào Foundation, nhưng Foundation không biết gì về chúng. Đây là nơi chứa các giao thức (interface) và tiện ích chung nhất: EventBus, ServiceLocator, Result, Logger.

### 2.3 Interface trước, implementation sau

Mọi service đều được trừu tượng hóa qua interface trước khi implement. Điều này cho phép:
- Test bằng mock/fake không cần Unity.
- Swap implementation trong production (ví dụ: custom logger cho Firebase Crashlytics).
- Các package cấp trên phụ thuộc vào interface của Foundation, không phụ thuộc vào concrete class.

### 2.4 Không UniTask

RevCore không dùng UniTask (đây là phụ thuộc của RCore cũ). Async được giải quyết bằng callback pattern thông qua Timer, hoặc coroutine thuần nếu cần. Điều này giảm rào cản cài đặt.

### 2.5 Addressables là tùy chọn

Audio package hỗ trợ Addressables qua `#if ADDRESSABLES` — define này được kích hoạt tự động khi `com.unity.addressables` có mặt trong project, thông qua `versionDefines` trong asmdef. Nếu không cài Addressables, code vẫn compile bình thường.

### 2.6 Editor code tách biệt runtime

Mỗi package có asmdef riêng cho Runtime và Editor. Drawers, custom editors, tool windows không bao giờ được đưa vào runtime assembly. Build player không bao giờ chứa Editor code.

---

## 3. Cấu trúc gói và sơ đồ phụ thuộc

```
Foundation  (zero deps)
├── Inspector  (zero deps)
├── Timer      → Foundation
├── Prefs      → Foundation
├── Pool       → Foundation, Timer
├── Audio      → Foundation, Inspector, Prefs
├── Data       → Foundation, Inspector, Timer, Prefs, Newtonsoft JSON
├── UI         → Foundation, Inspector, Pool, TMP
└── Tools      (editor-only, zero deps — optional, standalone)
```

**Đọc sơ đồ này:** Mũi tên `→` nghĩa là "phụ thuộc vào". `Foundation` không có mũi tên nào chỉ ra ngoài — đây là lý do nó là lá cây.

### Tóm tắt từng package

| Package | UPM ID | Mục đích |
|---|---|---|
| Foundation | `com.rabear.revcore.foundation` | EventBus, ServiceLocator, Result\<T\>, Logger, BigNumber, helpers |
| Inspector | `com.rabear.revcore.inspector` | Attributes cho Unity Inspector (`[ReadOnly]`, `[ShowIf]`, `[AutoFill]`…) |
| Timer | `com.rabear.revcore.timer` | Callback sau N giây, condition wait, debounce event |
| Prefs | `com.rabear.revcore.prefs` | Typed wrapper cho `PlayerPrefs` và `EditorPrefs` |
| Pool | `com.rabear.revcore.pool` | Object pooling cho Component/GameObject |
| Audio | `com.rabear.revcore.audio` | AudioCollection, BaseAudioManager, SFX/BGM, Addressables |
| Data | `com.rabear.revcore.data` | JObjectDB — lưu game data dạng JSON vào PlayerPrefs |
| UI | `com.rabear.revcore.ui` | PanelStack, typed buttons, scroll views, HoledLayerMask |
| Tools | `com.rabear.revcore.tools` | Editor tools hub (Navigate, Search, Generators, UI Tools, Utility) |

---

## 4. Quy ước thư mục và Assembly

Mỗi package tại `Assets/RevCore/<Package>/` theo cấu trúc sau:

```
<Package>/
├── Runtime/             → RevCore.<Package>.Runtime.asmdef
│   └── ...              rootNamespace: "RevCore"
├── Editor/              → RevCore.<Package>.Editor.asmdef
│   └── ...              (Editor platform only)
├── Tests/               → RevCore.<Package>.Tests.asmdef
│   └── ...              (UNITY_INCLUDE_TESTS constraint)
├── Samples~/            → UPM samples (ẩn khỏi Unity cho đến khi import)
├── package.json
├── README.md
└── CHANGELOG.md
```

### Quy tắc asmdef quan trọng

| Quy tắc | Lý do |
|---|---|
| Runtime asmdef: `includePlatforms: []` (trống) | Chạy trên mọi platform |
| Editor asmdef: `includePlatforms: ["Editor"]` | Không compile vào player build |
| Test asmdef: `overrideReferences: true` + `"nunit.framework.dll"` | NUnit cần override để Unity tìm thấy |
| Sample asmdef: `autoReferenced: false` | Sample không tự động thêm vào project |
| Sample asmdef: `rootNamespace: "RevCore.Samples"` | Consistent namespace cho tất cả sample |

### Namespace

Tất cả code runtime dùng namespace `RevCore` (flat, không nested). Ví dụ:

```csharp
namespace RevCore { public class EventBus : IEventBus { } }
namespace RevCore { public class ServiceLocator : IServiceLocator { } }
namespace RevCore { public class TimerScheduler : ITimerScheduler { } }
```

Tools editor dùng `RevCore.Tools.Editor`. UI samples dùng `RevCore.UI.Samples`.

---

## 5. Các mẫu kiến trúc cốt lõi

### 5.1 EventBus — Giao tiếp decoupled

EventBus cho phép các hệ thống giao tiếp mà không cần biết nhau tồn tại. Thay vì gọi thẳng `healthSystem.TakeDamage(10)`, sender publish một event, receiver subscribe và xử lý.

```csharp
// Định nghĩa event — chỉ là một struct
public readonly struct DamageEvent : IEvent
{
    public readonly int Amount;
    public DamageEvent(int amount) { Amount = amount; }
}

// Sender — không biết ai đang lắng nghe
Events.Publish(new DamageEvent(10));

// Receiver — subscribe và xử lý
public class HealthPresenter
{
    public void Enable()  => Events.Subscribe<DamageEvent>(OnDamage);
    public void Disable() => Events.Unsubscribe<DamageEvent>(OnDamage);

    private void OnDamage(DamageEvent evt)
    {
        Log.Info($"Nhận sát thương: {evt.Amount}");
    }
}
```

**Khi nào dùng EventBus:**
- Thông báo sự kiện xuyên hệ thống (damage, level up, scene loaded).
- Khi sender không nên có reference đến receiver.
- Thay thế cho `UnityEvent` khi không cần wire-up trong Inspector.

**Khi nào KHÔNG dùng EventBus:**
- Truy vấn có giá trị trả về — dùng ServiceLocator thay thế.
- Giao tiếp trong cùng một MonoBehaviour — gọi method trực tiếp.

**Scoped vs Global:**

```csharp
// Global — dùng cho game-wide events
Events.Publish(new LevelUpEvent());

// Scoped — dùng cho một feature cụ thể (ví dụ: battle system)
var battleBus = new EventBus();
battleBus.Subscribe<DamageEvent>(OnDamage);
battleBus.Clear(); // Cleanup khi battle kết thúc
```

---

### 5.2 ServiceLocator — Dependency Injection đơn giản

ServiceLocator là registry để tra cứu service theo type. Không dùng reflection, không dùng string key.

```csharp
// Đăng ký — thường trong khởi tạo (Awake/Start/Init)
Services.Global.Register<IAudioService>(audioManager);
Services.Global.Register<IDataService>(dataManager);

// Tra cứu — bất cứ đâu trong code
if (Services.Global.TryGet<IAudioService>(out var audio))
    audio.PlaySfx(soundId);
```

**Khi nào dùng ServiceLocator:**
- Singleton service cần truy cập từ nhiều nơi (Audio, Data, Analytics).
- Thay thế cho `FindObjectOfType<T>()` đắt tiền.
- Khi muốn swap implementation dễ dàng (production vs test fake).

**Scoped ServiceLocator** hữu ích trong scene phức tạp:

```csharp
var battleServices = new ServiceLocator();
battleServices.Register<IBattleRules>(hardModeRules);
// Truyền battleServices xuống battle systems thay vì dùng global
```

---

### 5.3 Result\<T\> — Xử lý lỗi không exception

Thay vì throw exception cho lỗi có thể lường trước, trả về `Result<T>`:

```csharp
public Result<PlayerData> LoadPlayer(string id)
{
    if (string.IsNullOrEmpty(id))
        return Result<PlayerData>.Fail("ID không hợp lệ");

    var data = database.Find(id);
    if (data == null)
        return Result<PlayerData>.Fail("Không tìm thấy player");

    return Result<PlayerData>.Ok(data);
}

// Caller xử lý tường minh
var result = LoadPlayer(playerId);
if (result.IsOk)
    ShowPlayer(result.Value);
else
    Log.Warn(result.Error);
```

**Lợi ích:**
- Không crash silent từ unhandled exception.
- Caller bắt buộc xử lý cả success lẫn failure.
- Dễ unit test — không cần try/catch trong test.

---

### 5.4 Timer — Async không cần coroutine

Timer giải quyết "làm gì sau N giây" và "làm gì khi condition đúng" bằng callback thuần.

```csharp
// Đợi 2 giây rồi spawn
Timers.WaitForSeconds(2f, SpawnWave);

// Đợi cho đến khi data load xong
Timers.WaitForCondition(() => dataManager.IsReady, OnDataReady);

// Debounce — gom nhiều event trong 0.2s thành 1
Timers.Debounce(new InventoryChangedEvent(), 0.2f);

// Hủy timer
ITimerHandle handle = Timers.WaitForSeconds(5f, OnTimeout);
handle.Cancel();
```

**Quan trọng:** Timer không tự chạy. Cần một driver để gọi `Tick`:

```csharp
// Option 1: Dùng GlobalTimers (tự động, persistent)
GlobalTimers.Instance.WaitForSeconds(1f, OnDone);

// Option 2: Manual tick trong Update của bạn
Timers.Tick(Time.deltaTime, Time.unscaledDeltaTime);
```

---

### 5.5 JObjectDB — Lưu game data

JObjectDB là hệ thống lưu trữ game data theo mô hình Model-Collection. Data được serialized thành JSON và lưu trong `PlayerPrefs`.

**Luồng khởi tạo:**

```
GameDataManager (MonoBehaviour)
    └── GameCollection (ScriptableObject)
            ├── PlayerModel → PlayerData (JSON)
            ├── ShopModel   → ShopData (JSON)
            └── SessionModel (built-in offline tracking)
```

**Lifecycle của một JObjectModel:**

```
Init() → Load() → OnPostLoad() → [gameplay] → OnUpdate() → OnPause() → OnPreSave() → Save()
```

**Dependency injection trong Data:**

```csharp
public class ShopModel : JObjectModel<ShopData>
{
    [Inject] private PlayerModel m_player; // tự động resolve từ collection

    public bool CanAfford(int price) => m_player.Data.coins >= price;
}
```

---

### 5.6 Pool — Tái sử dụng GameObject

```csharp
// Khởi tạo pool
var pool = new RevPool<PoolObject>(bulletPrefab, initialCount: 10, parent: transform);

// Spawn và release
var bullet = pool.Spawn(position);
pool.Release(bullet, delay: 2f); // tự động release sau 2 giây (dùng Timer)
```

`PoolsContainer<T>` quản lý nhiều prefab, tự tạo pool cho mỗi loại:

```csharp
var pools = new PoolsContainer<PoolObject>("EnemyPools", 5);
var enemy = pools.Spawn(enemyPrefab, spawnPoint.position);
pools.Release(enemy);
```

---

## 6. Luồng hoạt động theo tầng

### Khởi động game (startup flow)

```
1. GameDataManager.Awake()
        └── JObjectDB.RegisterCollection(GameCollection)

2. GameDataManager.Start() → Init()
        └── GameCollection.Load()
                ├── CreateModel(playerModel, "PlayerData")  → Load JSON từ PlayerPrefs
                ├── CreateModel(shopModel, "ShopData")      → Load JSON từ PlayerPrefs
                └── CreateModel(sessionModel)               → Tính offline time

3. PlayerModel.OnPostLoad(utcNow, offlineSec)
        └── Xử lý offline rewards, validate data

4. Services.Global.Register<IDataService>(gameDataManager)
        └── Các hệ thống khác có thể tra cứu data service
```

### Gameplay loop (per frame)

```
TimerDriver.LateUpdate()
    └── TimerScheduler.Tick(deltaTime, unscaledDeltaTime)
            ├── Countdown timers → callback khi hết giờ
            ├── Condition timers → callback khi predicate == true
            └── Debounce queue  → flush accumulated events
```

### Xử lý sự kiện (event flow)

```
Sender (bất kỳ hệ thống nào)
    └── Events.Publish(new DamageEvent(10))
            └── EventBus.Publish<DamageEvent>()
                    ├── HealthPresenter.OnDamage(evt)
                    ├── AchievementTracker.OnDamage(evt)
                    └── AnalyticsReporter.OnDamage(evt)
```

### Lưu dữ liệu (save flow)

```
Model.Data.coins += reward;
model.Save();           // đặt dirty, delay 3 giây
                        // hoặc
model.Save(now: true);  // debounce 200ms

// Khi app pause / OnApplicationQuit:
JObjectDB.SaveAll();    // flush tất cả dirty models
PlayerPrefContainer.SaveChanges(); // flush tất cả PlayerPref wrappers
```

---

## 7. Onboarding — Học RevCore theo thứ tự

### Bước 1: Foundation (1-2 giờ)

Đọc `Assets/RevCore/Foundation/README.md`. Nắm được:
- `IEvent` + `Events.Subscribe/Publish` — cơ chế giao tiếp cốt lõi.
- `Services.Global.Register/TryGet` — tra cứu service.
- `Result<T>.Ok/Fail` — xử lý lỗi.
- `Log.Info/Warn/Error` — logging.

Viết một event bus test đơn giản (không cần MonoBehaviour, không cần scene).

### Bước 2: Inspector (30 phút)

Đọc `Assets/RevCore/Inspector/README.md`. Nắm được:
- `[ReadOnly]`, `[ShowIf]`, `[AutoFill]`, `[InspectorButton]`.
- Thêm attributes vào một MonoBehaviour có sẵn, xem kết quả trong Inspector.

### Bước 3: Timer (1 giờ)

Đọc `Assets/RevCore/Timer/README.md`. Hiểu:
- `Timers.WaitForSeconds` vs `GlobalTimers.Instance.WaitForSeconds`.
- `TimerScheduler` là plain C# — có thể test không cần Unity.
- Cần driver để Tick.

Thực hành: tạo `SceneTimers.Instance.WaitForCondition(() => loaded, OnLoaded)`.

### Bước 4: Prefs + Pool (1 giờ)

Prefs — đơn giản nhất: `PlayerPrefInt`, `PlayerPrefBool`, `PlayerPrefContainer.SaveChanges()`.

Pool — `RevPool<T>` cho một loại prefab, `PoolsContainer<T>` cho nhiều loại. Nhớ cần Timer để delayed release hoạt động.

### Bước 5: Data (2-3 giờ)

Đây là package phức tạp nhất. Đọc kỹ README, sau đó:

1. Tạo một `JObjectData` POCO cho một hệ thống cụ thể (ví dụ: player currency).
2. Tạo `JObjectModel<T>` và implement các lifecycle hooks.
3. Tạo `JObjectModelCollection` và `JObjectDBManager<T>`.
4. Kiểm tra dữ liệu lưu/load trong Editor (`RevCore/Data/Log` menu).

Hiểu `[Inject]` — cho phép model A truy cập model B mà không cần DI container phức tạp.

### Bước 6: Audio (1 giờ)

Tạo `AudioCollection` ScriptableObject, thêm clips. Implement `BaseAudioManager`. Gọi `PlaySfx` và `PlayBgm`. Xem demo trong Inspector.

### Bước 7: UI (2 giờ)

Nắm cơ chế `PanelStack` — push/pop panels như một stack. `JustButton` và `JustToggle` thay thế `Button` + boilerplate thủ công. `OptimizedScrollView` cho danh sách lớn.

### Bước 8: Tools

Mở `RevCore > Tools Hub`. Làm quen với từng tool. Khi cần viết tool mới, kế thừa `RevCoreTool` và override `DrawGUI()`.

---

## 8. Hướng dẫn đóng góp

### Quy tắc bắt buộc khi thêm code vào RevCore

**1. Không vi phạm dependency graph.**
Trước khi thêm `using` hoặc asmdef reference mới, kiểm tra sơ đồ phụ thuộc ở Mục 3. Nếu thêm dependency mà tạo ra cycle hoặc đi ngược chiều → từ chối và tìm cách khác.

**2. Mọi public API phải có interface.**
Nếu thêm một service mới, định nghĩa interface ở Runtime assembly trước. Implementation có thể thay đổi; interface phải ổn định.

**3. Runtime không được chứa UnityEditor code.**
Dùng asmdef split. Không dùng `#if UNITY_EDITOR` trong Runtime assembly.

**4. Test phải pass trước khi merge.**
Mỗi package có Tests assembly. Chạy:
```
Unity.exe -runTests -batchmode -projectPath . -testResults results.xml -testPlatform EditMode
```

**5. Cập nhật CHANGELOG.md của package.**
Mỗi thay đổi public cần một entry trong `CHANGELOG.md` theo format `[version] - YYYY-MM-DD`.

### Thêm tính năng mới vào package có sẵn

1. Tạo interface trước nếu cần (`IMyFeature`).
2. Implement trong `Runtime/`.
3. Nếu cần Editor support, thêm vào `Editor/`.
4. Viết test trong `Tests/`.
5. Cập nhật `README.md` (API Reference table, ví dụ code).
6. Cập nhật `CHANGELOG.md`.

### Tạo package mới

1. Tạo thư mục `Assets/RevCore/<NewPackage>/` với cấu trúc chuẩn.
2. Tạo `package.json` với tên `com.rabear.revcore.<name>`, khai báo đúng dependencies.
3. Tạo asmdef cho Runtime, Editor, Tests theo quy ước đặt tên.
4. Cập nhật sơ đồ phụ thuộc trong tài liệu này.
5. Thêm meta file cho từng file mới (Unity tự tạo khi import).

### Cấm tuyệt đối

- Modify `Assets/RCore/` mà không có phê duyệt rõ ràng.
- Thêm dependency ngược chiều trong sơ đồ (ví dụ: Foundation → Timer).
- Dùng UniTask trong RevCore.
- Hard-code Addressables dependency — phải dùng `versionDefines` và `#if`.
- Commit file `UserSettings/`, `ProjectSettings/`, `.meta` của Addressable groups trừ khi thay đổi có chủ ý.

---

## 9. Di chuyển từ RCore

RevCore và RCore **cùng tồn tại**. Không cần migrate toàn bộ cùng lúc.

### Chiến lược migration

```
Dự án hiện tại (RCore)
    ↓
Tính năng mới → viết bằng RevCore
Tính năng cũ → giữ nguyên RCore, migrate dần từng phần
    ↓
Theo thời gian RCore giảm dần, RevCore tăng dần
```

### Bảng ánh xạ nhanh

| RCore | RevCore |
|---|---|
| `EventDispatcher.Raise(e)` | `Events.Publish(e)` |
| `BaseEvent` | `IEvent` (struct) |
| `TimerEventsGlobal.Instance.WaitForSeconds` | `GlobalTimers.Instance.WaitForSeconds` |
| `RPlayerPrefBool` | `PlayerPrefBool` |
| `CustomPool<T>` | `RevPool<T>` |
| `JObjectDBManagerV2<T>` | `JObjectDBManager<T>` |
| `using RCore.Inspector;` | `using RevCore;` |
| `[RCore.Inspector.ReadOnly]` | `[ReadOnly]` |
| `Debug.Log(msg)` | `Log.Info(msg)` |
| Exceptions cho expected failures | `Result<T>.Fail(message)` |

### Tương thích namespace

RevCore dùng namespace `RevCore` (flat). RCore dùng `RCore.*` (nested). Không có conflict — cả hai có thể `using` trong cùng file.

---

## Phụ lục: Các lệnh hữu ích

```bash
# Chạy tất cả tests
Unity.exe -runTests -batchmode -projectPath . -testResults results.xml -testPlatform EditMode

# Chạy test cho một package cụ thể
Unity.exe -runTests -batchmode -projectPath . -testResults results.xml -testPlatform EditMode -testFilter "RevCore.Tests"

# Build
Unity.exe -batchmode -quit -projectPath . -buildTarget Android -executeMethod BuildScript.Build
```

**Editor menus hữu ích:**
- `RevCore > Tools Hub` — tất cả editor tools
- `RevCore > Audio > Generate IDs` — tạo constants cho audio clips
- `RevCore > Data > Log` — xem data đang lưu trong PlayerPrefs
- `RevCore > Prefs > Clear PlayerPrefs` — reset toàn bộ saved data
