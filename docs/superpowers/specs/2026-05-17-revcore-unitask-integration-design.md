# RevCore UniTask Integration — Design

**Status**: Draft for approval.
**Target releases**: PR-A → `v1.1.0` (Timer + Audio async). PR-B → `v1.2.0` (new RevCore.Addressables module).
**Author**: Solo maintainer.
**Date**: 2026-05-17.

## 1. Problem statement

RevCore v1.0 ships with synchronous and callback-based async surfaces. Consumer game code increasingly uses `async`/`await` via [UniTask](https://github.com/Cysharp/UniTask) (Cysharp's struct-based zero-alloc `Task` replacement for Unity). Today, consumers wanting an awaitable from RevCore have to wrap RevCore callbacks themselves with `UniTaskCompletionSource`. That is boilerplate the framework should provide.

Concretely:

- `TimerScheduler.WaitForSeconds(duration, Action onComplete)` — caller wraps the callback into a `UniTaskCompletionSource`.
- `BaseAudioManager.SetMusicVolume(target, duration, onComplete)` — same pattern.
- Address­ables loading is not in RevCore at all; consumers either pull in RCore's `Assets/RCore/Main/Runtime/Common/Helper/Addressable/` (which already uses UniTask) or hand-roll their own.

UniTask is already on the project's `Packages/manifest.json` (line 4, `com.cysharp.unitask` git URL). The consumer install cost is zero — UniTask is in their UPM dependency graph by virtue of using anything in this repo.

## 2. Goals

1. Add UniTask-returning async API to **RevCore.Timer** and **RevCore.Audio** without breaking existing callback API. Ship as `v1.1.0` — minor version, no deprecations, additive only.
2. Port RCore's Addressables helpers into a **new RevCore.Addressables** module with the same UniTask-based surface. Ship as `v1.2.0` of the new module (independent of the framework version since it's a new package).
3. Both PRs use UniTask as a **hard dependency** declared in each module's `package.json`. No `#if REVCORE_UNITASK` guards.
4. Async tests follow the standard Unity Test Runner `async UniTaskVoid` pattern, covering happy path + `CancellationToken` honouring + timeout + exception propagation per public method.

## 3. Non-goals

- **Not** converting RevCore.UI's coroutine-based `PanelController.IE_Show`/`IE_Hide` to UniTask. The coroutine pattern works, the API is established, the consumer impact of changing it is large. Defer to a future v2.0 if-and-when needed.
- **Not** converting `TimerScheduler` internals to UniTask. The handle-based / `Tick`-driven scheduler is the established pattern and the new async API sits on top of it via a `UniTaskCompletionSource` bridge.
- **Not** introducing `RevCore.Data.SaveAsync`. JObjectDB saves synchronously to PlayerPrefs; an `Async` variant has no current use case.
- **Not** deprecating any v1.0 surface. Async is purely additive.

## 4. PR-A — `v1.1.0` Timer + Audio async additions

### 4.1 Module dependency change

`Assets/RevCore/Timer/package.json` and `Assets/RevCore/Audio/package.json` gain `"com.cysharp.unitask"` in their `dependencies` block.

### 4.2 Timer module — new API

In a new file `Assets/RevCore/Timer/Runtime/TimerAsyncExtensions.cs` (extension methods on `ITimerScheduler` + static helpers on the global `Timers` facade):

```csharp
namespace RevCore
{
    public static class TimerAsyncExtensions
    {
        /// <summary>Awaits <paramref name="seconds"/> on the scheduler. Cancellation cancels the underlying timer.</summary>
        public static UniTask DelayAsync(this ITimerScheduler scheduler, float seconds, bool unscaled = false, CancellationToken cancellationToken = default);

        /// <summary>Awaits until <paramref name="predicate"/> returns true. Polled on every scheduler Tick.</summary>
        public static UniTask WaitForConditionAsync(this ITimerScheduler scheduler, Func<bool> predicate, CancellationToken cancellationToken = default);

        /// <summary>Awaits <paramref name="frameCount"/> scheduler Ticks. One Tick = one Update on the driver MonoBehaviour.</summary>
        public static UniTask WaitForFramesAsync(this ITimerScheduler scheduler, int frameCount, CancellationToken cancellationToken = default);
    }

    public static partial class Timers // existing class; partial to add async
    {
        public static UniTask DelayAsync(float seconds, bool unscaled = false, CancellationToken cancellationToken = default)
            => Default.DelayAsync(seconds, unscaled, cancellationToken);

        public static UniTask WaitForConditionAsync(Func<bool> predicate, CancellationToken cancellationToken = default)
            => Default.WaitForConditionAsync(predicate, cancellationToken);

        public static UniTask WaitForFramesAsync(int frameCount, CancellationToken cancellationToken = default)
            => Default.WaitForFramesAsync(frameCount, cancellationToken);
    }
}
```

Implementation sketch:

```csharp
public static UniTask DelayAsync(this ITimerScheduler scheduler, float seconds, bool unscaled = false, CancellationToken ct = default)
{
    if (ct.IsCancellationRequested) return UniTask.FromCanceled(ct);
    var tcs = new UniTaskCompletionSource();
    var handle = scheduler.WaitForSeconds(seconds, () => tcs.TrySetResult(), unscaled);
    if (ct.CanBeCanceled)
        ct.Register(() => { handle.Cancel(); tcs.TrySetCanceled(ct); });
    return tcs.Task;
}
```

Behaviour:

- Cancellation cancels the underlying `TimerHandle`. The scheduler's own cancellation contract (drop from index immediately, reap on next Tick) is unchanged.
- Exceptions in the predicate (`WaitForConditionAsync`) propagate through the returned `UniTask`. The timer handle is cancelled before the exception is raised.
- All three methods are zero-alloc per call **except** the unavoidable `UniTaskCompletionSource` allocation. The completion source is short-lived; under steady-state usage it lands in Gen 0.

### 4.3 Audio module — new API

In a new file `Assets/RevCore/Audio/Runtime/AudioAsyncExtensions.cs`:

```csharp
namespace RevCore
{
    public static class AudioAsyncExtensions
    {
        /// <summary>Awaitable equivalent of <see cref="BaseAudioManager.SetMusicVolume"/>. Returns when the fade completes or is cancelled.</summary>
        public static UniTask FadeMusicAsync(this BaseAudioManager manager, float targetVolume, float duration, CancellationToken cancellationToken = default);

        /// <summary>Awaitable fade to 0 followed by <see cref="BaseAudioManager.StopMusic"/>.</summary>
        public static UniTask FadeOutMusicAsync(this BaseAudioManager manager, float duration, CancellationToken cancellationToken = default);
    }
}
```

The implementation hands the completion callback to the existing `SetMusicVolume(targetVolume, duration, onComplete)` overload and wires `cancellationToken.Register(...)` to kill the DOTween tweener that `BaseAudioManager` already caches.

### 4.4 Test plan — PR-A

Per public method, a test class under the matching module's `Tests/Runtime/` folder. Each class covers:

1. **Happy path** — `await Timers.DelayAsync(0.05f);` returns after roughly 0.05 s real time (use `Time.realtimeSinceStartup` delta with 50 % tolerance because Unity test scheduler is jittery).
2. **Pre-cancelled token** — call with a `new CancellationTokenSource().Token` after `Cancel()` already invoked → returns a cancelled `UniTask` synchronously.
3. **Mid-flight cancellation** — start the await, fire `cts.Cancel()` after one Tick → `OperationCanceledException` propagates and the underlying timer handle is gone from `ActiveCount`.
4. **Predicate exception** — `WaitForConditionAsync(() => throw new InvalidOperationException("x"))` propagates the exception through the `UniTask`.

Test signature template:

```csharp
[Test]
public async UniTaskVoid DelayAsync_returns_after_duration() { /* ... */ }
```

`UniTaskVoid` keeps the test harness from caring about the return value while still surfacing exceptions.

### 4.5 Files touched / created — PR-A

| File | Change |
|---|---|
| `Assets/RevCore/Timer/package.json` | + `com.cysharp.unitask` dep |
| `Assets/RevCore/Audio/package.json` | + `com.cysharp.unitask` dep |
| `Assets/RevCore/Timer/Runtime/TimerAsyncExtensions.cs` | NEW |
| `Assets/RevCore/Audio/Runtime/AudioAsyncExtensions.cs` | NEW |
| `Assets/RevCore/Timer/Tests/Runtime/TimerAsyncTests.cs` | NEW |
| `Assets/RevCore/Audio/Tests/Runtime/AudioAsyncTests.cs` | NEW |
| `Assets/RevCore/Timer/Runtime/PublicAPI.Unshipped.txt` | + 6 entries (3 extension methods × 2 — one on scheduler, one on Timers facade) |
| `Assets/RevCore/Audio/Runtime/PublicAPI.Unshipped.txt` | + 2 entries |
| `CHANGELOG.md` | + `[Unreleased]` ⇒ `[1.1.0]` entry under Added |
| `Assets/RevCore/Timer/CHANGELOG.md` | + 1.1.0 entry |
| `Assets/RevCore/Audio/CHANGELOG.md` | + 1.1.0 entry |
| `scripts/seal-public-api.py` run at release-cut promotes Unshipped → Shipped | (no script change) |

Approximate diff: ~250 lines of code + ~150 lines of tests + CHANGELOG.

## 5. PR-B — `v1.2.0` new `RevCore.Addressables` module

### 5.1 Module structure

```
Assets/RevCore/Addressables/
├── Runtime/
│   ├── AddressablesHelper.cs        (port of RCore.AddressableHelper, ~12 async statics)
│   ├── AssetRef.cs                  (port + generic refinement)
│   ├── AssetBundleRef.cs            (port)
│   ├── AssetBundleWrap.cs           (port)
│   ├── ComponentRef.cs              (port)
│   ├── PublicAPI.Shipped.txt        (header only at first commit)
│   ├── PublicAPI.Unshipped.txt
│   ├── RevCore.Addressables.Runtime.asmdef
│   └── csc.rsp
├── Tests/Runtime/
│   ├── AddressablesHelperTests.cs
│   ├── AssetRefTests.cs
│   └── RevCore.Addressables.Tests.asmdef
├── Samples~/
│   └── BasicLoad/
├── CHANGELOG.md
├── README.md
└── package.json
```

### 5.2 Package metadata

```json
{
  "name": "com.rabear.revcore.addressables",
  "version": "1.0.0",
  "displayName": "RevCore.Addressables",
  "description": "UniTask-based wrappers over Unity Addressables. Load assets, scenes, bundles, and component refs with cancellation and progress reporting.",
  "unity": "2022.3",
  "dependencies": {
    "com.cysharp.unitask": "2.5.0",
    "com.unity.addressables": "1.21.21"
  }
}
```

### 5.3 Port differences from RCore source

- Namespace: `RevCore.Addressables` (not `RCore`).
- Field naming: `m_camelCase` instance, `s_camelCase` static, per RevCore convention.
- 100 % XML doc coverage gate must pass; RCore source has gaps — fill them as part of the port.
- `[InternalsVisibleTo("RevCore.Addressables.Tests")]` for the small number of internal helpers that tests need.
- Per-module `PublicAPI.{Shipped,Unshipped}.txt` populated. The analyzer stays dormant project-wide (same constraint as v1.0).
- Refinement: `IProgress<float>` parameters become a default `null`, not omitted — RCore had inconsistent ordering across overloads.

### 5.4 Public surface (preview)

Roughly 20 entries, mostly mirroring RCore's existing API. Captured in `PublicAPI.Shipped.txt` at v1.0.0 of the new module via the `seal-public-api.py` workflow.

### 5.5 Test plan — PR-B

Trickier than PR-A because Addressables tests need a baked content catalog. Two options:

1. **EditMode-only smoke tests** using `Resources.FakeAsset` fixtures — verifies API shape compiles and cancellation propagates, but does not exercise real Addressables loading.
2. **PlayMode integration test** with a minimal Addressables group baked into the test project. Heavier setup, more meaningful coverage.

PR-B starts with option 1; option 2 is a follow-up if a consumer reports a bug only the integration path catches.

## 6. Risks

- **UniTask version drift.** UniTask is on a git URL pin, not a SemVer-pinned package. If Cysharp pushes a breaking change to UniTask's `UniTaskCompletionSource` or `IUniTaskAsyncEnumerable`, RevCore's tests break. Mitigation: pin to a specific UniTask tag in `manifest.json` rather than `master` once the v1.1 work is shipped.
- **Test flakiness on `DelayAsync(0.05f)`.** Unity Test Runner does not guarantee sub-frame timing. Use 50 % tolerance and a minimum-time assert rather than equality.
- **Cancellation race with scheduler Tick.** If `cts.Cancel()` fires concurrently with the timer's natural completion in `Tick`, both completion paths attempt to set the `UniTaskCompletionSource`. The `TrySet*` methods are idempotent so this is benign — but worth a regression test.
- **DOTween dependency for Audio fade.** `BaseAudioManager` uses DOTween for the existing fade. The async wrapper inherits that dependency. Document that `FadeMusicAsync` requires DOTween (already noted in `Audio/README.md` for the sync API).

## 7. Rollback

Each PR is independently revertible:

- PR-A revert: drop the two new `.cs` files, the two new test files, the `package.json` dependency lines, and the CHANGELOG entries. No consumer impact because v1.1.0 is purely additive — the prior v1.0.0 surface is unaffected.
- PR-B revert: delete `Assets/RevCore/Addressables/` entirely. The module is brand-new in v1.2.0; no prior surface exists. Consumer reverts the dependency entry in their `manifest.json` if they had pinned it.

## 8. Open questions for approval

1. **UniTask version pin.** Should `package.json` declare `"com.cysharp.unitask": "2.5.0"` (a recent stable tag) or follow the existing pattern in `Packages/manifest.json` (git URL on default branch)? Default branch is currently fine but creates the drift risk in §6.
2. **`Timers` facade as `partial class`.** Adding the async methods via `partial class Timers` keeps them in their own file but assumes `Timers` is already `partial` — currently it isn't. Either make it `partial` (no observable effect, just enables file split) or add the new methods to the existing `Timers.cs`. Preference?
3. **Test fixture for cancellation race.** Worth a deterministic test using a manual scheduler, or accept the inherent jitter and call the "TrySet* is idempotent" path covered by inspection?

## 9. Execution order

1. User approves this design.
2. New branch `feat/timer-audio-unitask-v1.1` off `main`.
3. Land PR-A. Tag `v1.1.0` once merged.
4. Branch `feat/addressables-module-v1.2` off `main`.
5. Land PR-B. Tag `v1.2.0`.
6. Update `docs/SESSION_HANDOFF.md` after each tag.
