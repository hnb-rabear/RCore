# GAP Categories — bulk recommendations

The migration map flags **259 types** (250 from auto-generation + 9 demoted from LIKELY in the Phase 8 audit) as `GAP`: present in RCore, absent from RevCore. This document groups them into ~14 buckets and assigns one of four actions per bucket:

| Action | Meaning |
|---|---|
| **PORT** | RevCore should ship an equivalent before v1.0. Track in [`../gap-analysis.md`](../gap-analysis.md). |
| **REPLACE** | RevCore covers the use case with a different API. Consumer rewrites. |
| **DROP** | RevCore will never ship this. Consumer stays on RCore for that type, forks the source, or switches to a third-party. |
| **DEFER** | Decision postponed past v1.0. Revisit when a consumer actually requests it. |

The action is per **bucket**, not per type — the bucket rationale applies uniformly. A handful of intra-bucket exceptions are called out inline.

Per-type decisions can be filled into [`../gap-analysis.md`](../gap-analysis.md) lazily, as each consumer migration surfaces concrete need. **Do not** preemptively fill the entire table — the action below is the default; only deviations need to be recorded.

## Bucket 1 — Editor-only drawers and inspectors (≈35 types)

Affected groups: `Inspector` (17), `UI/Button.{JustButtonEditor, SimpleButtonEditor}` (2), `UI/PanelStack.*Editor` (4), `UI/Toggle.*Editor` (2), `UI/Scrollview.*Editor` (4 from demoted LIKELY), `Common/SerializableDictionary.SerializableDictionaryDrawer` (1), `Data.ConfigCollectionEditor` (2), `Data/JObjectDB.JObjectModelCollectionEditor` (2), `Common/VFX.CFX_ParticleComponentEditor` (1).

**Action: DROP.**

These are `[CustomEditor]` / `[CustomPropertyDrawer]` classes. They are not consumer code — they live inside the framework asmdef alongside the runtime type they decorate. The consumer never references them directly. When RevCore ships a runtime type, its drawer ships with it (or doesn't; both frameworks coexist in the same project so the RCore drawer would attach to the RCore type and the RevCore drawer to the RevCore type).

Migration impact: zero. Consumer code does not need to be touched for any entry in this bucket.

## Bucket 2 — RCore Editor utility libraries (≈30 types from Common/Helper)

Affected: every `Editor*` class under `Common/Helper`: `EditorAssetUtil`, `EditorBuildUtil`, `EditorComponentUtil`, `EditorDrawing`, `EditorFileUtil`, `EditorGui`, `EditorHelper`, `EditorLayout`, `EditorSerializedPropertyExtensions`, plus the `Gui*` family (`GuiButton`, `GuiColor`, `GuiDropdown*`, `GuiFoldout`, `GuiHeaderFoldout`, `GuiInt`, `GuiObject`, `GuiTabs`, `GuiText`, `GuiToggle`), `GUIStyleHelper`, `DateTimePickerWindow`.

**Action: DROP.**

These are Editor extension toolkits that the consumer's own Editor scripts use. If a consumer's Editor scripts depend on `RCore.EditorGui.IntField(...)`, they continue to depend on RCore — there is no benefit to porting these to RevCore (the Editor surface is not part of RevCore's "shippable framework" mandate). Consumers can keep `Assets/RCore/Main/Editor/` in their project for these.

Future option: a small subset (e.g. `EditorAssetUtil` if it is genuinely useful at runtime tooling) may be revisited as a separate `RevCore.Tools` Editor module post-v1.0. Defer until requested.

## Bucket 3 — Vendored JSON parser (`Plugins` 18 types)

Affected: `JSON`, `JSONArray`, `JSONBool`, `JSONLazyCreator`, `JSONNode`, `JSONNodeExt`, `JSONNodeType`, `JSONNull`, `JSONNumber`, `JSONObject`, `JSONString`, `JSONTextMode`, `Enumerator`, `KeyEnumerator`, `LinqEnumerator`, `ValueEnumerator`, `RNative`, `UniClipboard`.

**Action: DROP.**

RCore vendored `SimpleJSON` ([github.com/Bunny83/SimpleJSON](https://github.com/Bunny83/SimpleJSON)) as `Plugins/SimpleJSON.cs`. RevCore depends on `Newtonsoft.Json` (`com.unity.nuget.newtonsoft-json`) as the standard Unity JSON dependency. Consumers calling `JSON.Parse(...)` migrate to `JsonConvert.DeserializeObject<T>(...)` — different API but well-documented elsewhere.

`RNative` and `UniClipboard` are small platform-bridge utilities (`Application.OpenURL`, iOS/Android clipboard); if a consumer uses them, copy the file into their own `Assets/YourProject/Vendored/` namespace.

## Bucket 4 — Service integrations (≈43 types)

Affected: `Services/Ads` (7), `Services/Firebase` (20), `Services/GameServices` (8), `Services/IAP` (1), `Services/Notification` (7).

**Action: DROP.**

The service wrappers in RCore were thin shims over vendor SDKs (Firebase Unity SDK, Google Play Games, IronSource, AdMob, Mobile Notifications). They added little value over calling the vendor SDK directly, and they coupled RCore releases to vendor SDK release cadences — a maintenance burden that does not belong in a framework that wants to be stable for ≥5 teams.

Consumer migration path: call the vendor SDK directly. Each vendor has good documentation; the RCore shim was rarely doing anything non-trivial.

The maintainer's policy is: RevCore stays scoped to engine-level utilities (events, timers, pools, save data, UI primitives). Service integrations belong in consumer code or in separate dedicated packages.

## Bucket 5 — Encryption (3 types)

Affected: `Common.Encryption`, `Common.IEncryption`, plus any `Common.Configuration.Directive` flag related to encryption.

**Action: DROP.**

Documented decision: RevCore intentionally ships no value encryption. A hardcoded-key obfuscation layer raises the bar only against PlayerPrefs editors, not against any attacker who can read the binary; real anti-cheat requires server-side validation. Shipping a half-measure invites consumers to mistake it for security.

See [`Assets/RevCore/Prefs/README.md`](../../Assets/RevCore/Prefs/README.md) — the no-encryption policy is documented there at the policy level.

## Bucket 6 — Event-based timer (`Common/Timer` 8 types)

Affected: `ConditionEvent`, `ConditionEventsGroup`, `CountdownEvent`, `CountdownEventsGroup`, `DelayableEvent`, `TimerEvents`, `TimerEventsGlobal`, `TimerEventsInScene`.

**Action: REPLACE.**

RCore's timer was an event/observer model: register a `CountdownEvent`, wire it into a `TimerEvents` MonoBehaviour, it fires `onTrigger` when done. RevCore's `TimerScheduler` is handle-based: call `Timers.Schedule(duration, callback)`, you get back an `ITimerHandle` that you cancel if needed. The two are not source-compatible — a consumer rewrites this code by hand.

Migration mapping:

| RCore pattern | RevCore equivalent |
|---|---|
| `new CountdownEvent { time = 5f, onTimeOut = OnDone }` + `TimerEvents.Register(e)` | `Timers.Schedule(5f, OnDone)` returning `ITimerHandle` |
| `new ConditionEvent { condition = () => ready, onTrigger = OnReady }` | `Timers.WaitFor(() => ready, OnReady)` |
| `DelayableEvent` (event with a delay before firing) | `Timers.Schedule(delay, () => Events.Publish(myEvent))` |

The new shape is ~2 lines per usage. Mechanical search-replace **does not work** for this bucket; treat each call site individually.

## Bucket 7 — Legacy key-value DB (`Data/KeyValueDB` 21 types)

Affected: `BigNumberData`, `BoolData`, `DataGroup`, `DateTimeData`, `FloatData`, `FunData`, `IntegerData`, `InvItemData`, `InvRPGItemData`, `InventoryData`, `InventoryRPGData`, `KeyValueCollection`, `KeyValueDB`, `KeyValueDBManager`, `KeyValueDBManagerEditor`, `KeyValueSS`, `ListData`, `LongData`, `StringData`, `TimedTaskData`, `ObjectData`.

**Action: DROP.**

`KeyValueDB` was an earlier save-data design that `JObjectDB` superseded — the two coexisted in RCore. RevCore.Data ships only the JObject lineage. A consumer still on KeyValueDB writes a one-time bootstrap-time exporter that reads the old KeyValueDB entries (still in PlayerPrefs) and writes them out as a JObjectModel collection, then drops the RCore dependency.

Sketch of the exporter (consumer-side, runs once on app start, gated behind a `HasKey("revcore_migrated")` PlayerPrefs flag):

```csharp
if (!PlayerPrefs.HasKey("revcore_migrated"))
{
    // RCore.KeyValueDB still active here — read it
    int coins = RCore.IntegerData.Get("Coins");
    bool tutorialDone = RCore.BoolData.Get("TutorialDone");

    // Write into RevCore.JObjectDB
    var playerData = JObjectDB.CreateCollection<MyPlayerData>("Player");
    playerData.coins = coins;
    playerData.tutorialDone = tutorialDone;
    playerData.Save();

    PlayerPrefs.SetInt("revcore_migrated", 1);
}
```

After one production cycle (≈2 weeks of player launches), the exporter and the RCore.KeyValueDB dependency can be removed.

## Bucket 8 — RPG-specific data (`Data/RPGBase` 7 types)

Affected: `Attribute`, `AttributeParseExtenstion` (sic), `AttributesCollection`, `LinkedMod`, `Mod`, `ModsContainer`, `TimedMod`.

**Action: DROP.**

These are game-specific abstractions (stat modifiers, attribute systems) — they belong in the consumer's gameplay code, not in a framework. The maintainer chose not to port them because the API shape of "attribute" varies wildly between games (additive vs. multiplicative, capped vs. uncapped, derived vs. base). A consumer who used them copies the source into their own project namespace.

## Bucket 9 — Custom JSON for RCore (`Plugins.JSON*`)

Covered under Bucket 3.

## Bucket 10 — Misc runtime utilities (≈20 types)

Affected: `Common.RVector{2,2Int,3,3Int,4}` (5), `Common.Roman` (1), `Common.SceneLoader` (1), `Common.TapFeedback`, `UIPivot`, `YesNoNone` (enums, 3), `Common.AnimEventListener`, `AssetsList`, `BaseEvent`, `DontDestroyedGroup`, `EventDispatcher`, `RMenu` (6), `Common/BigNumber.BigNumberHelper`, `BigNumberAlphaExtension` (2), `Common/Helper.RandomExtension`, `RUtil`, `RUtilExtension`, `StringBuilderExtension`, `IPInfo`, `JsonHelper`, `NameGenerator`, `ProjectileHelper`, `TrajectoryHelper`, `WebRequestHelper`, `AddressableUtil`.

**Action: split.**

- `RVector*` — **DROP.** Unity's `Vector2`/`Vector3`/`Vector2Int`/`Vector3Int` cover the use case. RCore's R* prefixed copies were from a pre-Unity-2017 era.
- `Roman` (Arabic ↔ Roman numerals) — **DROP.** ~30 lines; copy if needed.
- `SceneLoader` — **DEFER.** Could be useful at framework scope, but Unity 2023+ has `SceneManager.LoadSceneAsync` with `Addressables` — design conflict pending.
- `TapFeedback`, `UIPivot`, `YesNoNone` — **DROP.** Project-specific enums; copy if used.
- `AnimEventListener` — **DROP.** Trivial MonoBehaviour wrapping `OnAnimationEvent`; copy.
- `AssetsList` — **DROP.** Editor-time asset enumeration; not framework-level.
- `BaseEvent` (interface) — **REPLACE** with `RevCore.IEvent`.
- `DontDestroyedGroup` — **DROP.** ~10 lines; copy.
- `EventDispatcher` — **REPLACE** with `RevCore.EventBus` / `RevCore.Events.Publish`.
- `RMenu` — **DROP.** RCore's editor menu organizer; not framework-level.
- `BigNumberHelper`, `BigNumberAlphaExtension` — **DEFER.** The core `BigNumber` is ported; these are extension methods. Port lazily when a consumer asks.
- `RandomExtension`, `StringBuilderExtension`, `RUtilExtension` — **DEFER.** Useful extension methods; revisit per-method post v1.0.
- `RUtil` — **DEFER.** Grab-bag static. Pick individual methods worth porting; drop the rest.
- `IPInfo` — **DROP.** External IP lookup; project-specific.
- `JsonHelper` — **DROP.** Use Newtonsoft directly (see Bucket 3).
- `NameGenerator`, `ProjectileHelper`, `TrajectoryHelper` — **DROP.** Game-specific.
- `WebRequestHelper` — **DROP.** Use `UnityWebRequest` directly.
- `AddressableUtil` — **DEFER.** Addressables integration is its own design discussion.

## Bucket 11 — Asset reference wrappers (`Common/Helper.AssetBundle*`, `AssetRef_*`, `ComponentRef*`, `SerializableDictionary` 4)

Affected: `AssetBundleRef`, `AssetBundleWith2EnumKeys`, `AssetBundleWithEnumKey`, `AssetBundleWithIntKey`, `AssetBundleWrap`, `AssetRef_FontAsset`, `AssetRef_SpriteAtlas`, `ComponentRef`, `ComponentRef_SpriteRenderer`, `AssetReferenceWrapper`, `SerializableKeyValue`, `SerializableKeyValueDrawer`, `UnityObjectWrapper`.

**Action: DEFER.**

These wrap Unity's `AssetReference` and `SerializeReference` to compensate for inspector limitations in Unity 2020/2021. Unity 2022.3+ (RevCore's minimum) has solved most of these natively — `[SerializeReference]` plus `SerializeReferencePolymorphic`, Addressables groups, etc. A consumer using these probably wants to migrate to the native Unity API directly, but the cost/benefit per type is unclear. Revisit after v1.0 when at least one consumer reports actually using these.

## Bucket 12 — UI bits (16 types)

Affected: `Bubble`, `CanvasResolutionFixerEditor`, `ContentRectSizeFilter`, `HoledLayerMaskEditor`, `LeftSnapScrollView`, `Message`, `MessageWithPointer`, `MessagesHUD`, `Notification`, `PointerAlignment`, `ProgressBarEditor` (2), `ResolutionFixer`, `ScaleType`, `UIGridScaler`, `VerticalSnapScrollView`.

**Action: split.**

- `CanvasResolutionFixerEditor`, `HoledLayerMaskEditor`, `ProgressBarEditor` — Editor drawers. **DROP**, same as Bucket 1.
- `ResolutionFixer` — Multi-aspect ratio canvas resolution helper. **DEFER.** Most consumers need this; port post-v1.0 if requested.
- `LeftSnapScrollView`, `VerticalSnapScrollView` — Snap-to-item scroll variants. **DEFER.** Adjacent to `OptimizedScrollView`; port if a consumer requests.
- `Message`, `MessageWithPointer`, `MessagesHUD`, `Notification` — In-game messaging UI. **DROP.** This is gameplay UI, not framework UI; varies wildly between games.
- `Bubble` — Speech-bubble or tooltip helper. **DROP.** Same reasoning.
- `ContentRectSizeFilter`, `UIGridScaler` — Layout filters. **DEFER.**
- `PointerAlignment`, `ScaleType` — Enums backing the above. Migrate with their owners.

## Bucket 13 — Drag-and-drop UI (`UI/DragDrop` 4 types)

Affected: `UIDragController`, `UIDragHandler`, `UIDraggableItem`, `UIDraggableWorldItem`.

**Action: DEFER.**

Useful for inventory UIs but heavy: ≈600 LOC across the four files. RevCore.UI does not have this yet. Port post-v1.0 when a consumer asks; meanwhile, the RCore versions work fine alongside RevCore.

## Bucket 14 — Configuration & bootstrap (`Configuration.cs`, `Common.Env`)

Affected: `Configuration`, `Directive`, `Env` (twice — once in `Configuration.cs`, once in `Common/`).

**Action: DEFER.**

These are project-level constants and feature flags (build environment, Debug.Log gating, etc.). RevCore does not opine on bootstrap configuration — every consumer wires this differently. A canonical `RevCore.Env` could be designed post-v1.0, but only after surveying how consumers actually use these.

## Bucket 15 — Particle helpers (`Common/VFX.CFX_*`, 3 types)

Affected: `CFX_Component`, `CFX_ParticleComponent` (+ its editor, already in Bucket 1).

**Action: DROP.**

These were originally from Cartoon FX (asset store package); RCore had a mild customization. Not framework material.

## Bucket 16 — Pool legacy (`Common/Pool.CustomPool`, 1 type)

Affected: `CustomPool`.

**Action: REPLACE.**

`RCore.Common.Pool.CustomPool<T>` is the predecessor of `RevCore.RevPool<T>`. The two have different signatures but the same role. Consumer code:

```diff
-private CustomPool<Bullet> m_bullets = new CustomPool<Bullet>(bulletPrefab, 32);
-var b = m_bullets.Spawn(position);
-m_bullets.Release(b);
+private RevPool<Bullet> m_bullets = new RevPool<Bullet>(bulletPrefab, 32);
+var b = m_bullets.Spawn(position);
+m_bullets.Release(b);
```

The two APIs are 95% compatible at the method level. Behavior differences:

- `RevPool.Spawn` is amortized O(1) for the null-eviction step; `CustomPool` walked the inactive list every spawn.
- `PoolsContainer<T>` (containing multiple pools) ships zero-alloc `ForEachActive` / `CopyActiveTo`; `RCore.PoolsContainer` allocates `GetActiveList()` per call.
- `RevPool.Spawn` fires `IRevDiagnostics.OnPoolSpawn` for observability if a listener is wired; `CustomPool` does not.

## Bucket 17 — Misc Data (4 types)

Affected: `Common.RPlayerPrefList`, `Common.RPlayerPrefDict` (demoted from LIKELY), `Data/Common.BinaryDataSaver`, `Data/JObjectDB.SessionDataHandler`.

**Action: split.**

- `RPlayerPrefList<T>`, `RPlayerPrefDict<K,V>` — **DEFER.** RCore offered list/dict-flavored PlayerPrefs; RevCore.Prefs ships only scalars. Need a design call on whether `PlayerPrefList<T>` belongs in Prefs or in Data. Probably Data (it's a glorified JObjectModel under the hood). Post-v1.0.
- `BinaryDataSaver` — **DROP.** Binary-formatter-based save (deprecated in .NET 5+ for security reasons). RevCore intentionally does not ship binary formatter wrappers.
- `SessionDataHandler` — **REPLACE.** RevCore's `RevCore.SessionModel` + `SessionData` covers the use case; same lifecycle hooks.

---

## Summary table

| Bucket | Types | Default action |
|---|---:|---|
| 1 — Editor drawers | ≈35 | DROP |
| 2 — RCore Editor utilities | ≈30 | DROP |
| 3 — SimpleJSON vendored | 18 | DROP |
| 4 — Service integrations | ≈43 | DROP |
| 5 — Encryption | 3 | DROP |
| 6 — Event-based timer | 8 | REPLACE |
| 7 — KeyValueDB | 21 | DROP (+ one-shot exporter) |
| 8 — RPG abstractions | 7 | DROP |
| 9 — (covered by 3) | — | — |
| 10 — Misc runtime | ≈20 | split (mostly DROP / DEFER) |
| 11 — Asset wrappers | 13 | DEFER |
| 12 — UI bits | 16 | split |
| 13 — Drag-and-drop | 4 | DEFER |
| 14 — Bootstrap config | 3 | DEFER |
| 15 — VFX | 3 | DROP |
| 16 — Pool legacy | 1 | REPLACE |
| 17 — Misc data | 4 | split |
| **Total** | **≈252** | mostly DROP/DEFER; ≈35 REPLACE / PORT |

About **140 types are DROP** (consumer doesn't need RevCore equivalent — fork, stay on RCore, or use vendor SDK), **40 are REPLACE** (different API shape, ~hours of consumer rewrite each), and **70 are DEFER** (revisit post-v1.0 when a consumer requests). **Zero are tagged PORT** in the default — RevCore's pre-1.0 surface is considered feature-complete for the maintainer's intended scope, and PORTs only happen on consumer-driven feedback.
