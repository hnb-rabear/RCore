# IRevDiagnostics — Phase 7.1 design

**Date**: 2026-05-17
**Phase**: 7.1 (advanced features — observability)
**Target release**: v0.6.0
**Effort estimate**: 2–3 hours

## Problem

Consumer projects currently cannot observe what RevCore is doing under the hood. When production crashes, lag spikes, or save corruption happens, the consumer has no way to trace the sequence of framework operations that led to the issue:

- **Crash investigation**: was Timer firing too many callbacks? Did a Pool overflow? Did an Event publish recursively?
- **Frame profiling**: which subsystem is allocating per frame?
- **Gameplay analytics**: PM wants "bomb usage per session" — needs hook into `PlaySFX("bomb_*")`.
- **Live debug HUD**: dev wants on-screen counter of active timers + events/sec.
- **Test verification**: whitebox tests that count framework operations.

The available workarounds are all bad:

- Patching `Debug.Log` into RevCore source — overwritten on every framework update.
- Subclassing `TimerScheduler` / `EventBus` / `RevPool` — RevCore is not designed for that (sealed or non-virtual hot paths).
- Wrapping every `PlaySFX` / `Spawn` call in consumer code — invasive, miss internal triggers (e.g. `OnApplicationQuit` calls `SaveForced`, consumer can't intercept).

## Solution

Add a single `IRevDiagnostics` interface and a static accessor `RevDiagnostics.Listener`. The framework's hot paths call the listener after they perform their work. Default `Listener == null` means zero cost (one null check per hook). Consumer assigns an implementation when they want to observe.

## Architecture

### Interface

```csharp
namespace RevCore
{
    public static class RevDiagnostics
    {
        public static IRevDiagnostics Listener;
    }

    public interface IRevDiagnostics
    {
        // Timer (3 hooks)
        void OnTimerScheduled(int id, float duration, bool unscaled);
        void OnTimerCancelled(int id);
        void OnTimerCompleted(int id, float overtime);

        // EventBus (3 hooks)
        void OnEventPublished(System.Type eventType, int listenerCount);
        void OnEventSubscribed(System.Type eventType, int newCount);
        void OnEventUnsubscribed(System.Type eventType, int newCount);

        // Pool (2 hooks)
        void OnPoolSpawn(string poolName, bool reused);
        void OnPoolRelease(string poolName);

        // Audio (2 hooks)
        void OnAudioPlaySFX(string clipName);
        void OnAudioPlayMusic(string clipName, bool looping);
    }
}
```

**Single listener, not multi-cast**. Multi-listener is the consumer's responsibility — they wrap a `CompositeDiagnostics` if needed. This keeps the framework interface flat and the call site idiomatic.

**Static accessor, not DI**. Matches the existing `Events.Global` pattern. Consumer wires the listener once at startup; no constructor changes anywhere.

### Payload — metadata only

Every hook passes primitive types or `System.Type`. No `ITimerHandle`, no `Component`, no full `AudioClip`. Reasons:

1. **No closure capture**. Consumer can write `(id, dur) => total += dur` without capturing handle objects that would prolong GC lifetime.
2. **No generic complications**. `OnPoolSpawn<T>` would require generic-method hooks; `string poolName` works for the 100% of analytics/logging use cases.
3. **Minimal coupling**. Consumer never sees `ITimerHandle` / `RevPool<T>` / `JObjectData` — diagnostics are an observability contract, not a control contract.

### Threading

Main thread only. Matches the existing framework convention (every public path except `EventBus.Enqueue` is documented as main-thread). Listener implementations that want to forward to a background thread (logger queue, analytics SDK) are responsible for their own marshalling.

### Cost model

Per hook when `Listener == null`: one null check, predicted branch. ~1 ns.

Per hook when `Listener` is set: one null check + one interface dispatch + arg push. ~5 ns.

Compared to the hot paths these hooks live on:

| Hook site | Operation cost | Hook overhead | % overhead |
| --- | --- | --- | --- |
| `Timer.Cancel` (cached) | ~1.3 µs | 5 ns | 0.4% |
| `EventBus.Publish` (1 listener) | ~700 ns | 5 ns | 0.7% |
| `Pool.Spawn` (reuse) | ~14 µs (Spawn_and_release / 1000) | 5 ns | 0.04% |
| `Audio.PlaySFX` | varies, Unity-bound | 5 ns | < 0.01% |

Acceptable across the board.

## Firing semantics

Each hook fires **after the underlying operation completes successfully**. Operations that early-return (no-op, idempotent guards, validation failures) skip the hook.

| Hook | Fires | Skipped |
| --- | --- | --- |
| `OnTimerScheduled` | After timer is added to the internal list | `seconds <= 0` (the immediate-complete path) |
| `OnTimerCancelled` | After `Handle.Cancel()` flips `IsCancelled` | Handle was already cancelled / completed |
| `OnTimerCompleted` | After the user callback runs, before list removal | Timer was cancelled (counted as cancellation, not completion) |
| `OnEventPublished` | Once per `Publish<T>(evt)` call, **even when no subscribers are registered** (`listenerCount == 0`) | — |
| `OnEventSubscribed` | After a new listener is appended | Dedup hit — the listener was already in the invocation list |
| `OnEventUnsubscribed` | After a listener is removed | The listener was not in the invocation list |
| `OnPoolSpawn` | After `MoveToActive` on the spawned item | — |
| `OnPoolRelease` | After `MoveToInactive` | Item null, or not currently in the active bucket |
| `OnAudioPlaySFX` | After `source.PlayOneShot` / `source.Play` | Clip null, `audioCollection` null, or `EnabledSFX == false` |
| `OnAudioPlayMusic` | After `m_musicSource.Play` | Clip null |

## Integration points

| Module | File | Hook sites |
| --- | --- | --- |
| Timer | `Assets/RevCore/Timer/Runtime/Core/TimerScheduler.cs` | After `AddCountdown` in `WaitForSeconds(...)`; in `Cancel(int)` per snapshot entry; in `Tick` when `timer.Tick(delta)` returns true and the handle was not already cancelled |
| EventBus | `Assets/RevCore/Foundation/Runtime/Events/EventBus.cs` | After `Publish` invocation; after `Subscribe` increment; after `Unsubscribe` decrement |
| Pool | `Assets/RevCore/Pool/Runtime/Core/RevPool.cs` | End of `Spawn(...)` overload with the `reused` flag in scope; in `Release(T)` after `MoveToInactive` |
| Audio | `Assets/RevCore/Audio/Runtime/BaseAudioManager.cs` | In `PlaySFX(AudioClip,...)` after `source.PlayOneShot` / `source.Play`; in `PlayMusic(AudioClip,...)` after `m_musicSource.Play` |

Each call site is `RevDiagnostics.Listener?.OnXxx(...)` — single statement, easy to grep for.

## Files modified

```text
NEW:
  Assets/RevCore/Foundation/Runtime/Diagnostics/IRevDiagnostics.cs
  Assets/RevCore/Foundation/Runtime/Diagnostics/RevDiagnostics.cs
  Assets/RevCore/Foundation/Tests/Runtime/RevDiagnosticsTests.cs
  Assets/RevCore/Foundation/Samples~/Diagnostics/CrashBufferDiagnostics.cs
  Assets/RevCore/Foundation/Samples~/Diagnostics/DebugOverlayDiagnostics.cs
  Assets/RevCore/Foundation/Samples~/Diagnostics/UnityLogDiagnostics.cs

MODIFIED:
  Assets/RevCore/Foundation/Runtime/PublicAPI.Unshipped.txt   (+12)
  Assets/RevCore/Foundation/package.json                      (samples[] entry)
  Assets/RevCore/Foundation/Runtime/Events/EventBus.cs        (+3 hook calls)
  Assets/RevCore/Timer/Runtime/Core/TimerScheduler.cs         (+3 hook calls)
  Assets/RevCore/Pool/Runtime/Core/RevPool.cs                 (+2 hook calls)
  Assets/RevCore/Audio/Runtime/BaseAudioManager.cs            (+2 hook calls)
  CHANGELOG.md                                                (Added entry)
```

## Sample scripts

Ship 3 example listener implementations under `Assets/RevCore/Foundation/Samples~/Diagnostics/`. The `~` suffix marks them as opt-in samples — Unity does not auto-import them; consumers explicitly enable via Package Manager → "Samples". Each sample is a single self-contained `.cs` file demonstrating one real-world scenario from the design rationale.

### 1. `CrashBufferDiagnostics.cs`

Ring buffer that records the last 100 framework events. Drop into a singleton `MonoBehaviour`; in the crash handler call `.Dump()` to attach the trace to a Crashlytics / Sentry custom key.

```csharp
public sealed class CrashBufferDiagnostics : IRevDiagnostics
{
    private readonly Queue<string> m_buffer = new();
    private const int Capacity = 100;

    public string Dump() => string.Join("\n", m_buffer);

    public void OnTimerScheduled(int id, float duration, bool unscaled) => Push($"Timer[{id}] sched dur={duration:F2}");
    public void OnTimerCancelled(int id) => Push($"Timer[{id}] cancel");
    public void OnTimerCompleted(int id, float overtime) => Push($"Timer[{id}] done over={overtime:F2}");
    public void OnEventPublished(Type t, int n) => Push($"Pub {t.Name} (->{n})");
    public void OnEventSubscribed(Type t, int n) => Push($"Sub {t.Name} (now {n})");
    public void OnEventUnsubscribed(Type t, int n) => Push($"Unsub {t.Name} (now {n})");
    public void OnPoolSpawn(string pool, bool reused) => Push($"Pool {pool} {(reused ? "reuse" : "NEW")}");
    public void OnPoolRelease(string pool) => Push($"Pool {pool} release");
    public void OnAudioPlaySFX(string clip) => Push($"SFX {clip}");
    public void OnAudioPlayMusic(string clip, bool loop) => Push($"Music {clip} loop={loop}");

    private void Push(string msg)
    {
        m_buffer.Enqueue($"{Time.frameCount}: {msg}");
        while (m_buffer.Count > Capacity) m_buffer.Dequeue();
    }
}
```

### 2. `DebugOverlayDiagnostics.cs`

`MonoBehaviour` that draws on-screen counters using OnGUI. Wires itself on `Awake`, unwires on `OnDestroy`. Shows: active timer count, events/sec, spawns/sec.

```csharp
public sealed class DebugOverlayDiagnostics : MonoBehaviour, IRevDiagnostics
{
    private int m_activeTimers, m_eventsThisSec, m_spawnsThisSec;
    private float m_resetAt;

    private void Awake() => RevDiagnostics.Listener = this;
    private void OnDestroy() { if (ReferenceEquals(RevDiagnostics.Listener, this)) RevDiagnostics.Listener = null; }

    public void OnTimerScheduled(int id, float dur, bool u) => m_activeTimers++;
    public void OnTimerCancelled(int id) => m_activeTimers--;
    public void OnTimerCompleted(int id, float o) => m_activeTimers--;
    public void OnEventPublished(Type t, int n) => m_eventsThisSec++;
    public void OnEventSubscribed(Type t, int n) { }
    public void OnEventUnsubscribed(Type t, int n) { }
    public void OnPoolSpawn(string pool, bool reused) => m_spawnsThisSec++;
    public void OnPoolRelease(string pool) { }
    public void OnAudioPlaySFX(string clip) { }
    public void OnAudioPlayMusic(string clip, bool loop) { }

    private void OnGUI()
    {
        if (Time.unscaledTime >= m_resetAt) { m_eventsThisSec = 0; m_spawnsThisSec = 0; m_resetAt = Time.unscaledTime + 1f; }
        GUI.Label(new Rect(10, 10, 300, 20), $"Active timers: {m_activeTimers}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Events/sec: {m_eventsThisSec}");
        GUI.Label(new Rect(10, 50, 300, 20), $"Spawns/sec: {m_spawnsThisSec}");
    }
}
```

### 3. `UnityLogDiagnostics.cs`

Simplest possible listener — `Debug.Log` every event. Useful for "I want to see everything RevCore does in Console" during a debugging session. Verbose; the sample file calls this out so users don't ship it.

```csharp
public sealed class UnityLogDiagnostics : IRevDiagnostics
{
    public void OnTimerScheduled(int id, float d, bool u) => Debug.Log($"[Rev] Timer[{id}] sched dur={d:F2}");
    public void OnTimerCancelled(int id) => Debug.Log($"[Rev] Timer[{id}] cancel");
    public void OnTimerCompleted(int id, float o) => Debug.Log($"[Rev] Timer[{id}] done over={o:F2}");
    public void OnEventPublished(Type t, int n) => Debug.Log($"[Rev] Pub {t.Name} (->{n})");
    public void OnEventSubscribed(Type t, int n) => Debug.Log($"[Rev] Sub {t.Name} (now {n})");
    public void OnEventUnsubscribed(Type t, int n) => Debug.Log($"[Rev] Unsub {t.Name} (now {n})");
    public void OnPoolSpawn(string pool, bool reused) => Debug.Log($"[Rev] Pool {pool} {(reused ? "reuse" : "NEW")}");
    public void OnPoolRelease(string pool) => Debug.Log($"[Rev] Pool {pool} release");
    public void OnAudioPlaySFX(string clip) => Debug.Log($"[Rev] SFX {clip}");
    public void OnAudioPlayMusic(string clip, bool loop) => Debug.Log($"[Rev] Music {clip} loop={loop}");
}
```

### Package manifest entry

`Foundation/package.json` gains a `samples` array entry so Unity Package Manager's UI surfaces the sample under "Samples":

```json
"samples": [
  {
    "displayName": "Diagnostics Listeners",
    "description": "Reference IRevDiagnostics implementations: crash buffer, debug HUD, verbose Unity log.",
    "path": "Samples~/Diagnostics"
  }
]
```

Consumer clicks "Import" in Package Manager → Unity copies the three `.cs` files into the consumer project's `Assets/Samples/RevCore.Foundation/<version>/Diagnostics/`. Samples do not become part of the framework's compile or its tests.

## Testing

`Foundation/Tests/Runtime/RevDiagnosticsTests.cs` adds a `FakeDiagnostics` recording-test-double:

```csharp
private sealed class FakeDiagnostics : IRevDiagnostics
{
    public readonly List<string> Calls = new();
    public void OnTimerScheduled(int id, float dur, bool u) => Calls.Add($"TimerScheduled({id},{dur:F1},{u})");
    public void OnTimerCancelled(int id) => Calls.Add($"TimerCancelled({id})");
    // ... etc
}
```

Tests cover:

- Default `Listener == null` produces no calls and no exceptions.
- Setting a fake listener captures every hook from every module.
- Switching listener mid-run takes effect immediately (no caching of the old reference).
- Hooks fire in the expected order during compound operations (e.g. `WaitForSeconds(...).Cancel()` → `OnTimerScheduled` then `OnTimerCancelled`).

## Public API additions

`Assets/RevCore/Foundation/Runtime/PublicAPI.Unshipped.txt`:

```text
RevCore.IRevDiagnostics
RevCore.IRevDiagnostics.OnAudioPlayMusic(string clipName, bool looping) -> void
RevCore.IRevDiagnostics.OnAudioPlaySFX(string clipName) -> void
RevCore.IRevDiagnostics.OnEventPublished(System.Type eventType, int listenerCount) -> void
RevCore.IRevDiagnostics.OnEventSubscribed(System.Type eventType, int newCount) -> void
RevCore.IRevDiagnostics.OnEventUnsubscribed(System.Type eventType, int newCount) -> void
RevCore.IRevDiagnostics.OnPoolRelease(string poolName) -> void
RevCore.IRevDiagnostics.OnPoolSpawn(string poolName, bool reused) -> void
RevCore.IRevDiagnostics.OnTimerCancelled(int id) -> void
RevCore.IRevDiagnostics.OnTimerCompleted(int id, float overtime) -> void
RevCore.IRevDiagnostics.OnTimerScheduled(int id, float duration, bool unscaled) -> void
RevCore.RevDiagnostics
static RevCore.RevDiagnostics.Listener -> RevCore.IRevDiagnostics
```

## Out of scope (deferred to future phases)

- Multi-listener composition (consumer wraps if needed).
- Per-event subscribe/unsubscribe (single `Listener` covers everything).
- Async hook invocation (hooks run synchronously on the main thread; listener marshals if needed).
- Additional hooks for Inspector / UI / Data modules — defer to v1.1+ based on real demand.
- Performance counter framework (Phase 7.4 separate scope).

## Release plan

Lands in a single PR titled `feat(foundation): IRevDiagnostics observability hooks for Timer / EventBus / Pool / Audio`. Targets v0.6.0 (next minor). PublicAPI promotion happens at release cut time.
