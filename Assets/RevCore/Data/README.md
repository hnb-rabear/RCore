# RevCore.Data

JObjectDB V2 data persistence system for RevCore.

## Install

Unity Package Manager, local path:

```text
Assets/RevCore/Data
```

Or by name when published:

```json
"com.rabear.revcore.data": "0.1.0"
```

Requires:

```json
"com.rabear.revcore.foundation": "0.1.0",
"com.rabear.revcore.timer": "0.1.0",
"com.rabear.revcore.prefs": "0.1.0",
"com.rabear.revcore.inspector": "0.1.0",
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

## 60-second Quick Start

**1. Define your data POCO:**

```csharp
[Serializable]
public class PlayerData : JObjectData
{
    public int coins;
    public int level = 1;
}
```

**2. Create a model ScriptableObject:**

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

**3. Aggregate in a collection:**

```csharp
[CreateAssetMenu(menuName = "Game/GameCollection")]
public class GameCollection : JObjectModelCollection
{
    [CreateScriptableObject, AutoFill] public PlayerModel player;

    public override void Load()
    {
        base.Load(); // registers SessionModel
        CreateModel(player, "PlayerData");
    }
}
```

**4. Wire up a manager MonoBehaviour:**

```csharp
public class GameDataManager : JObjectDBManager<GameCollection>
{
    private void Start() => Init();
}
```

## Concepts

### JObjectData

Serializable POCO stored in `PlayerPrefs` as JSON. Uses `JsonUtility` by default; pass `minimizeSize: true` to `Save()` / `ToJson()` for Newtonsoft.Json with null/default stripping.

### JObjectModel\<T\>

A `ScriptableObject` that owns one `JObjectData` instance. Implements lifecycle hooks (`OnPostLoad`, `OnUpdate`, `OnPause`, `OnPreSave`, `OnRemoteConfigFetched`).

### JObjectModelCollection

Root `ScriptableObject` aggregating all models. Call `CreateModel(ref, key)` in `Load()` to register each model. Always includes built-in `SessionModel` for offline-time tracking.

### JObjectDBManager\<T\>

`MonoBehaviour` that drives the lifecycle. Place on a persistent GameObject. Call `Init()` on `Start`.

### Dependency Injection

Fields marked `[Inject]` on a model are auto-resolved from the collection after `CreateModel` calls:

```csharp
public class ShopModel : JObjectModel<ShopData>
{
    [Inject] private PlayerModel m_player;
    // m_player is resolved automatically during Init()
}
```

### Batched Save

`Save(now: true)` — immediate (debounced 200ms).  
`Save()` — delayed by `m_saveDelay` seconds (default 3).  
Auto-saves on pause and quit if configured.

### Backup / Restore

```csharp
JObjectDB.Backup();   // saves to Saves/ folder
JObjectDB.Restore(filePath);
JObjectDB.Import(jsonString);
```

## API Reference

| Type | Assembly | Purpose |
|---|---|---|
| `JObjectData` | Runtime | Base class for serializable data POCOs |
| `IJObjectData` | Runtime | Save/Load/Delete/ToJson contract |
| `JObjectModel<T>` | Runtime | ScriptableObject model base |
| `IJObjectModel` | Runtime | Data + lifecycle contract |
| `IJObjectHandler` | Runtime | Lifecycle hooks contract |
| `JObjectModelCollection` | Runtime | Root aggregator with DI |
| `JObjectDBManager<T>` | Runtime | MonoBehaviour lifecycle driver |
| `JObjectDB` | Runtime | Static DB — collections registry, backup/restore |
| `SessionModel` | Runtime | Built-in session tracking model |
| `SessionData` | Runtime | Session data POCO |
| `NewDayStartedEvent` | Runtime | Event fired when calendar day changes |
| `InjectAttribute` | Runtime | Field injection marker |
| `JObjectModelCollectionEditor` | Editor | Inspector GUI for collections |
| `JObjectDBManagerEditor` | Editor | Inspector GUI for managers |

## Migration from RCore

| RCore | RevCore.Data |
|---|---|
| `RCore.Data.JObject.JObjectData` | `RevCore.JObjectData` |
| `JObjectModel<T>` | `JObjectModel<T>` (same) |
| `JObjectModelCollection` | `JObjectModelCollection` (same) |
| `JObjectDBManagerV2<T>` | `JObjectDBManager<T>` |
| `JObjectDB` | `JObjectDB` (same) |
| `EventDispatcher.Raise(e)` | `Events.Publish(e)` |
| `BaseEvent` | `IEvent` |
| `TimerEventsGlobal.WaitForSeconds` debounce | `Timers.Debounce(e, seconds)` |
| `RPlayerPrefContainer.SaveChanges()` | `PlayerPrefContainer.SaveChanges()` |
| `[Inject]` | `[Inject]` (same) |
| V1 (`JObjectDataCollection`, `JObjectHandler`) | Dropped |
| `KeyValueDB` | Dropped |
| `BinaryDataSaver` | Dropped |
| Session module files | Built into Data package |
