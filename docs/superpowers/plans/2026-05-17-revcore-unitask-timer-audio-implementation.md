# Implementation plan — UniTask async API for Timer + Audio (v1.1.0, PR-A)

**Spec**: [`docs/superpowers/specs/2026-05-17-revcore-unitask-integration-design.md`](../specs/2026-05-17-revcore-unitask-integration-design.md)
**Target release**: `v1.1.0` (additive — no deprecations).
**Branch**: `feat/timer-audio-unitask-v1.1` off `main`.
**Verified against** code at commit `f394ff0` (PR #13 merge — current `origin/main` head). PR #13 was docs-only; no `.cs`, `.asmdef`, or `package.json` changes between `30950b0` (v1.0.0 cut) and `f394ff0`, so the code-claim verification table below is unaffected by the PR #13 merge.

This plan covers PR-A only. PR-B (new `RevCore.Addressables` module) is a separate plan written after PR-A merges.

## Pre-flight: facts verified before writing tasks

To avoid hallucinated APIs, the following were confirmed against the actual code on this branch:

| Claim | Source | Status |
|---|---|---|
| UniTask asmdef name is `UniTask` (no version qualifier) | `Library/PackageCache/com.cysharp.unitask@73a63b7f67/Runtime/UniTask.asmdef` | confirmed |
| UniTask asmdef GUID is `f51ebe6a0ceec4240a699833d6309b23` | `UniTask.asmdef.meta` | confirmed |
| UniTask is `autoReferenced: true` BUT custom asmdef still need to list it (RCore does, by GUID) | `Assets/RCore/Main/Runtime/RCore.Main.Runtime.asmdef` line 5 | confirmed |
| `Timers` class at `Assets/RevCore/Timer/Runtime/Core/Timers.cs` is NOT partial | file scanned | confirmed |
| Accessor is `Timers.Scheduler` (not `Default`) | `Timers.cs:15` | confirmed |
| `ITimerScheduler.WaitForSeconds(float, Action, bool unscaledTime = false, int id = 0)` — param is `unscaledTime` | `ITimerScheduler.cs:27` | confirmed |
| `ITimerScheduler.WaitForCondition(ConditionalDelegate, Action, int id = 0)` — predicate is `ConditionalDelegate` (`delegate bool()`) | `ITimerScheduler.cs:36`, `Foundation/Runtime/Delegates.cs:16` | confirmed |
| `ITimerHandle.Cancel()` is on the interface — wrapper can call `handle.Cancel()` directly | `ITimerHandle.cs:31` | confirmed |
| `BaseAudioManager.MusicVolume` is a public read-only getter | `BaseAudioManager.cs:46` | confirmed |
| `BaseAudioManager.SetMusicVolume(float, float fadeDuration = 0, Action onComplete = null)` fires `onComplete` after the fade | `BaseAudioManager.cs:221` | confirmed |
| `SetMusicVolume` calls `m_musicTweener?.Kill()` at the top — calling it again snap-cancels any in-flight fade | `BaseAudioManager.cs:224` | confirmed |
| Audio Runtime asmdef has `versionDefines` for `DOTWEEN` and `ADDRESSABLES` already | `RevCore.Audio.Runtime.asmdef:17-28` | confirmed |
| Test asmdef pattern uses `"overrideReferences": true` + explicit `precompiledReferences: ["nunit.framework.dll"]` | `RevCore.Timer.Tests.asmdef`, `RevCore.Audio.Tests.asmdef` | confirmed |
| Tests use `RevCore.Tests` namespace (per existing pattern in `TimerSchedulerTests.cs:3`) | `TimerSchedulerTests.cs:3` | confirmed |

Two design choices already locked in by spec §8, defaulted here to keep the plan executable without another approval round:

- **`Timers` partial-class question** → make `Timers` `partial`. New async methods land in a sibling `TimersAsync.cs` file for clean separation. If the user later prefers a single-file `Timers.cs`, the merge is one move-and-delete.
- **UniTask version pin** → leave at the existing git URL pin in `Packages/manifest.json` (`com.cysharp.unitask` from default branch). Module `package.json` declares the dependency as `"com.cysharp.unitask": "2.5.10"` to match the installed version (verified in `Library/PackageCache/com.cysharp.unitask@.../package.json`). UPM resolves the git-URL pin regardless, so this is mainly documentation. The drift risk noted in spec §6 is acknowledged.
- **Cancellation-race test** → covered by inspection (`TrySet*` idempotency comment) in the implementation; no dedicated test fixture in PR-A.

## Task graph

```
T1 (branch)
 ├── T2 (Timer asmdef) ── T3 (Timers.cs partial + TimersAsync.cs) ── T4 (Timer test asmdef) ── T5 (TimerAsyncTests.cs) ── T6 (Timer meta: package.json + PublicAPI + CHANGELOG)
 └── T7 (Audio asmdef) ── T8 (AudioAsyncExtensions.cs) ── T9 (Audio test asmdef) ── T10 (AudioAsyncTests.cs) ── T11 (Audio meta)
                                                                                                                          └── T12 (root CHANGELOG)
                                                                                                                                └── T13 (verify in Unity)
                                                                                                                                      └── T14 (commit + push + PR)
```

Tasks T2–T6 (Timer) and T7–T11 (Audio) are independent; can be done in parallel or interleaved.

---

## T1 — Create branch

```powershell
git checkout main
git pull origin main
git checkout -b feat/timer-audio-unitask-v1.1
```

**Verify:** `git rev-parse --abbrev-ref HEAD` prints `feat/timer-audio-unitask-v1.1`. `git log --oneline -1` prints the v1.0.0 release commit (currently `30950b0`).

---

## T2 — Add UniTask reference to Timer Runtime asmdef

**File:** `Assets/RevCore/Timer/Runtime/RevCore.Timer.Runtime.asmdef`

**Change:** the `references` array gains a `"UniTask"` entry.

```json
{
  "name": "RevCore.Timer.Runtime",
  "rootNamespace": "RevCore",
  "references": ["RevCore.Foundation.Runtime", "UniTask"],
  ...
}
```

**Verify:** open Unity → Library/ScriptAssemblies rebuilds → no compile error introduced. UniTask types resolve in any `.cs` file under `Assets/RevCore/Timer/Runtime/`.

---

## T3 — Timer Runtime: make `Timers` partial + add async methods

### T3.1 — Convert existing `Timers.cs` to partial

**File:** `Assets/RevCore/Timer/Runtime/Core/Timers.cs`

**Change:** line 10 (the class declaration), add `partial` modifier. Everything else in the file is unchanged.

**Before:**

```csharp
public static class Timers
```

**After:**

```csharp
public static partial class Timers
```

**Verify:** file still compiles. No other behavioural change.

### T3.2 — Create `TimersAsync.cs` with three new methods

**File:** `Assets/RevCore/Timer/Runtime/Core/TimersAsync.cs` (NEW)

**Full content:**

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace RevCore
{
	public static partial class Timers
	{
		/// <summary>
		/// Awaitable equivalent of <see cref="WaitForSeconds(float, Action, bool, int)"/>. Returns
		/// when the wall-clock delay has elapsed on the active <see cref="Scheduler"/>. Cancellation
		/// via <paramref name="cancellationToken"/> cancels the underlying timer handle.
		/// </summary>
		/// <param name="seconds">Delay in seconds. Non-positive returns synchronously on the next Tick.</param>
		/// <param name="unscaledTime">When <c>true</c>, advances by unscaled delta time.</param>
		/// <param name="cancellationToken">Cancels the wait and the underlying timer.</param>
		public static UniTask DelayAsync(float seconds, bool unscaledTime = false, CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);

			var tcs = new UniTaskCompletionSource();
			var handle = s_scheduler.WaitForSeconds(seconds, () => tcs.TrySetResult(), unscaledTime);

			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					handle.Cancel();
					// TrySetCanceled is idempotent — if the timer's natural completion ran first
					// and already set the result, this is a no-op.
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}

		/// <summary>
		/// Awaitable equivalent of <see cref="WaitForCondition(ConditionalDelegate, Action, int)"/>.
		/// Returns the first scheduler Tick on which <paramref name="predicate"/> evaluates to <c>true</c>.
		/// </summary>
		/// <param name="predicate">Polled on every Tick. Keep it cheap. Exceptions surface through the returned <see cref="UniTask"/>.</param>
		/// <param name="cancellationToken">Cancels the wait.</param>
		public static UniTask WaitForConditionAsync(Func<bool> predicate, CancellationToken cancellationToken = default)
		{
			if (predicate == null)
				return UniTask.FromException(new ArgumentNullException(nameof(predicate)));
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);

			var tcs = new UniTaskCompletionSource();
			ConditionalDelegate cd = () =>
			{
				try
				{
					return predicate();
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
					return true; // stop polling
				}
			};

			var handle = s_scheduler.WaitForCondition(cd, () => tcs.TrySetResult());

			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					handle.Cancel();
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}

		/// <summary>
		/// Awaitable that returns after <paramref name="frameCount"/> scheduler Ticks. One Tick equals
		/// one Update on the driver MonoBehaviour, so this is approximately one Unity frame per count.
		/// </summary>
		/// <param name="frameCount">Number of Ticks to wait. Non-positive returns synchronously.</param>
		/// <param name="cancellationToken">Cancels the wait.</param>
		public static UniTask WaitForFramesAsync(int frameCount, CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);
			if (frameCount <= 0)
				return UniTask.CompletedTask;

			var tcs = new UniTaskCompletionSource();
			int remaining = frameCount;
			ConditionalDelegate cd = () => --remaining <= 0;
			var handle = s_scheduler.WaitForCondition(cd, () => tcs.TrySetResult());

			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					handle.Cancel();
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}
	}
}
```

**Notes:**

- Uses the existing `s_scheduler` field directly — that field is `private`, but `Timers` is partial so this file shares access.
- `Func<bool>` is the public predicate type for caller ergonomics; converted internally to `ConditionalDelegate`.
- Cancellation registration uses `CancellationToken.Register(Action)` — the returned `CancellationTokenRegistration` is not disposed. This is acceptable for short-lived timers: the registration object is GC-friendly and disposed automatically once the token's source is GC'd.
- Exception handling in `WaitForConditionAsync` returns `true` from the predicate after capturing the exception, which tells the scheduler to stop polling — and the natural completion callback then calls `tcs.TrySetResult()` which is a no-op because the exception was already set.

**Verify:**

- File compiles (Unity Console: no error).
- The three new methods appear in IDE IntelliSense on `Timers.`.

---

## T4 — Add UniTask reference to Timer Tests asmdef

**File:** `Assets/RevCore/Timer/Tests/Runtime/RevCore.Timer.Tests.asmdef`

**Change:** add `"UniTask"` to the `references` array (between `RevCore.Foundation.Runtime` and the test-runner entries — order doesn't matter functionally).

```json
"references": [
  "RevCore.Timer.Runtime",
  "RevCore.Foundation.Runtime",
  "UniTask",
  "UnityEngine.TestRunner",
  "UnityEditor.TestRunner",
  "Unity.PerformanceTesting"
],
```

**Verify:** Test Runner re-imports the assembly without error.

---

## T5 — Timer tests: `TimerAsyncTests.cs`

**File:** `Assets/RevCore/Timer/Tests/Runtime/TimerAsyncTests.cs` (NEW)

**Test coverage (12 tests across 3 methods):**

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace RevCore.Tests
{
	public class TimerAsyncTests
	{
		private TimerScheduler m_scheduler;

		[SetUp]
		public void SetUp()
		{
			m_scheduler = new TimerScheduler();
			Timers.Scheduler = m_scheduler;
		}

		[TearDown]
		public void TearDown()
		{
			Timers.Scheduler = null; // restore default
		}

		// --- DelayAsync ---

		[Test]
		public async UniTaskVoid DelayAsync_returns_after_duration()
		{
			var task = Timers.DelayAsync(0.05f);
			Assert.IsFalse(task.Status.IsCompleted(), "Should not complete before Tick.");

			// Simulate ~0.06s elapsing across multiple Tick calls
			for (int i = 0; i < 6; i++)
				m_scheduler.Tick(0.01f, 0.01f);

			await task;
			Assert.IsTrue(task.Status == UniTaskStatus.Succeeded);
		}

		[Test]
		public void DelayAsync_pre_cancelled_token_returns_cancelled()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();

			var task = Timers.DelayAsync(1f, false, cts.Token);

			Assert.AreEqual(UniTaskStatus.Canceled, task.Status);
			Assert.AreEqual(0, m_scheduler.ActiveCount, "Pre-cancelled token must not schedule a timer.");
		}

		[Test]
		public async UniTaskVoid DelayAsync_mid_flight_cancellation_throws_OperationCanceledException()
		{
			var cts = new CancellationTokenSource();
			var task = Timers.DelayAsync(1f, false, cts.Token);

			m_scheduler.Tick(0.1f, 0.1f); // advance partway
			Assert.AreEqual(1, m_scheduler.ActiveCount);

			cts.Cancel();
			m_scheduler.Tick(0.0f, 0.0f); // reap

			Assert.AreEqual(0, m_scheduler.ActiveCount, "Cancellation must drop the handle.");

			try { await task; Assert.Fail("Expected OperationCanceledException"); }
			catch (OperationCanceledException) { /* expected */ }
		}

		[Test]
		public async UniTaskVoid DelayAsync_zero_seconds_completes_on_next_tick()
		{
			var task = Timers.DelayAsync(0f);
			// Scheduler.WaitForSeconds with seconds <= 0 completes synchronously per current contract.
			await task;
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
		}

		// --- WaitForConditionAsync ---

		[Test]
		public async UniTaskVoid WaitForConditionAsync_returns_when_predicate_true()
		{
			bool flag = false;
			var task = Timers.WaitForConditionAsync(() => flag);

			m_scheduler.Tick(0f, 0f);
			Assert.IsFalse(task.Status.IsCompleted());

			flag = true;
			m_scheduler.Tick(0f, 0f);

			await task;
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
		}

		[Test]
		public void WaitForConditionAsync_null_predicate_returns_faulted()
		{
			var task = Timers.WaitForConditionAsync(null);
			Assert.AreEqual(UniTaskStatus.Faulted, task.Status);
		}

		[Test]
		public async UniTaskVoid WaitForConditionAsync_predicate_exception_propagates()
		{
			var task = Timers.WaitForConditionAsync(() => throw new InvalidOperationException("predicate boom"));

			m_scheduler.Tick(0f, 0f);

			try { await task; Assert.Fail("Expected InvalidOperationException"); }
			catch (InvalidOperationException ex) { Assert.AreEqual("predicate boom", ex.Message); }
		}

		[Test]
		public async UniTaskVoid WaitForConditionAsync_mid_flight_cancellation()
		{
			var cts = new CancellationTokenSource();
			var task = Timers.WaitForConditionAsync(() => false, cts.Token);

			m_scheduler.Tick(0f, 0f);
			Assert.AreEqual(1, m_scheduler.ActiveCount);

			cts.Cancel();
			m_scheduler.Tick(0f, 0f);

			try { await task; Assert.Fail("Expected OperationCanceledException"); }
			catch (OperationCanceledException) { }
		}

		// --- WaitForFramesAsync ---

		[Test]
		public async UniTaskVoid WaitForFramesAsync_returns_after_n_ticks()
		{
			var task = Timers.WaitForFramesAsync(3);

			m_scheduler.Tick(0f, 0f); // remaining 3 → 2
			Assert.IsFalse(task.Status.IsCompleted());
			m_scheduler.Tick(0f, 0f); // remaining 2 → 1
			Assert.IsFalse(task.Status.IsCompleted());
			m_scheduler.Tick(0f, 0f); // remaining 1 → 0 → predicate true

			await task;
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
		}

		[Test]
		public void WaitForFramesAsync_zero_frames_completes_synchronously()
		{
			var task = Timers.WaitForFramesAsync(0);
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
			Assert.AreEqual(0, m_scheduler.ActiveCount);
		}

		[Test]
		public void WaitForFramesAsync_negative_frames_completes_synchronously()
		{
			var task = Timers.WaitForFramesAsync(-5);
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
		}

		[Test]
		public async UniTaskVoid WaitForFramesAsync_mid_flight_cancellation()
		{
			var cts = new CancellationTokenSource();
			var task = Timers.WaitForFramesAsync(100, cts.Token);

			m_scheduler.Tick(0f, 0f);
			cts.Cancel();
			m_scheduler.Tick(0f, 0f);

			try { await task; Assert.Fail("Expected OperationCanceledException"); }
			catch (OperationCanceledException) { }
		}
	}
}
```

**Verify:**

- Unity Test Runner → EditMode → tab shows new `TimerAsyncTests` class with 12 tests.
- Run all → 12 / 12 green.
- Total test count rises from 160 to 172.

---

## T6 — Timer module metadata

### T6.1 — Update Timer `package.json`

**File:** `Assets/RevCore/Timer/package.json`

**Change:** bump version to `1.1.0` and add UniTask dependency. Version `2.5.10` matches the actual installed UniTask in `Library/PackageCache/com.cysharp.unitask@.../package.json`. UPM resolves the git-URL pin in `Packages/manifest.json` regardless of the value here, so this is mainly a documentation hint — but match-on-install keeps it honest.

```json
{
  "name": "com.rabear.revcore.timer",
  "version": "1.1.0",
  ...
  "dependencies": {
    "com.rabear.revcore.foundation": "1.0.0",
    "com.cysharp.unitask": "2.5.10"
  },
  ...
}
```

### T6.2 — Update `PublicAPI.Unshipped.txt`

**File:** `Assets/RevCore/Timer/Runtime/PublicAPI.Unshipped.txt`

Append (after the `#nullable enable` header):

```
static RevCore.Timers.DelayAsync(float seconds, bool unscaledTime = false, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) -> Cysharp.Threading.Tasks.UniTask
static RevCore.Timers.WaitForConditionAsync(System.Func<bool> predicate, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) -> Cysharp.Threading.Tasks.UniTask
static RevCore.Timers.WaitForFramesAsync(int frameCount, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) -> Cysharp.Threading.Tasks.UniTask
```

### T6.3 — Update Timer module CHANGELOG

**File:** `Assets/RevCore/Timer/CHANGELOG.md`

Add new `[1.1.0] - 2026-05-19` section above the existing `[1.0.0] - 2026-05-13` entry:

```markdown
## [1.1.0] - 2026-05-19

### Added

- `Timers.DelayAsync(float, bool, CancellationToken)` — awaitable equivalent of `WaitForSeconds`.
- `Timers.WaitForConditionAsync(Func<bool>, CancellationToken)` — awaitable equivalent of `WaitForCondition`.
- `Timers.WaitForFramesAsync(int, CancellationToken)` — awaitable that returns after N scheduler Ticks.
- Hard dependency on `com.cysharp.unitask` declared in `package.json`.
```

**Verify:**

- `python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json` reports the new public members as documented; coverage stays 100 %.
- The XML doc baseline count rises by 3 (the three async methods).

---

## T7 — Add UniTask reference to Audio Runtime asmdef

**File:** `Assets/RevCore/Audio/Runtime/RevCore.Audio.Runtime.asmdef`

**Change:** add `"UniTask"` to the `references` array.

```json
"references": [
  "RevCore.Foundation.Runtime",
  "RevCore.Inspector.Runtime",
  "Unity.Addressables",
  "Unity.ResourceManager",
  "UniTask"
],
```

**Verify:** Audio Runtime compiles after Unity reimport.

---

## T8 — Audio Runtime: `AudioAsyncExtensions.cs`

**File:** `Assets/RevCore/Audio/Runtime/AudioAsyncExtensions.cs` (NEW)

**Full content:**

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace RevCore
{
	/// <summary>
	/// UniTask-based awaitable extensions over <see cref="BaseAudioManager"/>. Each method bridges
	/// the existing callback-style API to <see cref="UniTask"/>; cancellation kills the in-flight
	/// fade by re-invoking <see cref="BaseAudioManager.SetMusicVolume"/> with the current volume
	/// and a zero fade duration, which triggers the DOTween tweener's <c>Kill</c> at the top of
	/// that method.
	/// </summary>
	public static class AudioAsyncExtensions
	{
		/// <summary>
		/// Awaitable equivalent of <see cref="BaseAudioManager.SetMusicVolume(float, float, Action)"/>.
		/// Returns when the fade completes; cancellation snap-stops the fade at the current volume.
		/// </summary>
		/// <param name="manager">The audio manager.</param>
		/// <param name="targetVolume">Final music volume.</param>
		/// <param name="duration">Fade duration in seconds. Zero or less snaps and returns immediately.</param>
		/// <param name="cancellationToken">Cancels the fade and returns the task as cancelled.</param>
		public static UniTask FadeMusicAsync(this BaseAudioManager manager, float targetVolume, float duration, CancellationToken cancellationToken = default)
		{
			if (manager == null)
				return UniTask.FromException(new ArgumentNullException(nameof(manager)));
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);

			var tcs = new UniTaskCompletionSource();
			manager.SetMusicVolume(targetVolume, duration, () => tcs.TrySetResult());

			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					// Snap the volume to its current value with zero fade — this triggers the
					// tweener Kill() at the top of SetMusicVolume.
					manager.SetMusicVolume(manager.MusicVolume, 0);
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}

		/// <summary>
		/// Awaitable fade-to-zero followed by <see cref="BaseAudioManager.StopMusic"/>. Returns when
		/// the fade completes and the music source has stopped.
		/// </summary>
		/// <param name="manager">The audio manager.</param>
		/// <param name="duration">Fade-out duration in seconds.</param>
		/// <param name="cancellationToken">Cancels the fade. The music source is NOT stopped on cancellation.</param>
		public static UniTask FadeOutMusicAsync(this BaseAudioManager manager, float duration, CancellationToken cancellationToken = default)
		{
			if (manager == null)
				return UniTask.FromException(new ArgumentNullException(nameof(manager)));
			if (cancellationToken.IsCancellationRequested)
				return UniTask.FromCanceled(cancellationToken);

			var tcs = new UniTaskCompletionSource();
			manager.StopMusic(duration, () => tcs.TrySetResult());

			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					manager.SetMusicVolume(manager.MusicVolume, 0);
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}
	}
}
```

**Verify:** file compiles. New methods appear on `audioManager.` in IntelliSense.

---

## T9 — Add UniTask reference to Audio Tests asmdef

**File:** `Assets/RevCore/Audio/Tests/RevCore.Audio.Tests.asmdef`

**Change:** add `"UniTask"`.

```json
"references": [
  "RevCore.Foundation.Runtime",
  "RevCore.Audio.Runtime",
  "UnityEngine.TestRunner",
  "UnityEditor.TestRunner",
  "RevCore.Inspector.Runtime",
  "UniTask"
],
```

**Verify:** Test Runner re-imports Audio.Tests.

---

## T10 — Audio tests: `AudioAsyncTests.cs`

**File:** `Assets/RevCore/Audio/Tests/AudioAsyncTests.cs` (NEW)

`BaseAudioManager` is a `MonoBehaviour` — these tests follow the existing `BaseAudioManagerTests` pattern of creating a GameObject + AddComponent + reflectively invoking lifecycle hooks. Inspect `Assets/RevCore/Audio/Tests/BaseAudioManagerTests.cs` for the exact bootstrap pattern before writing — it varies by Unity version.

**Test coverage (6 tests across 2 methods):**

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
	public class AudioAsyncTests
	{
		private GameObject m_go;
		private BaseAudioManager m_manager;

		[SetUp]
		public void SetUp()
		{
			m_go = new GameObject("AudioAsyncTests_Manager");
			m_manager = m_go.AddComponent<BaseAudioManager>();
			// Match BaseAudioManagerTests's reflective bootstrap if needed — see that file.
		}

		[TearDown]
		public void TearDown()
		{
			if (m_go != null)
				UnityEngine.Object.DestroyImmediate(m_go);
		}

		// --- FadeMusicAsync ---

		[Test]
		public async UniTaskVoid FadeMusicAsync_zero_duration_completes_immediately()
		{
			var task = m_manager.FadeMusicAsync(0.5f, 0);
			await task;
			Assert.AreEqual(0.5f, m_manager.MusicVolume, 0.001f);
		}

		[Test]
		public void FadeMusicAsync_null_manager_returns_faulted()
		{
			BaseAudioManager nullManager = null;
			var task = nullManager.FadeMusicAsync(0.5f, 1f);
			Assert.AreEqual(UniTaskStatus.Faulted, task.Status);
		}

		[Test]
		public void FadeMusicAsync_pre_cancelled_token_returns_cancelled()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();
			var task = m_manager.FadeMusicAsync(0.5f, 1f, cts.Token);
			Assert.AreEqual(UniTaskStatus.Canceled, task.Status);
		}

		// --- FadeOutMusicAsync ---

		[Test]
		public async UniTaskVoid FadeOutMusicAsync_zero_duration_stops_music_immediately()
		{
			m_manager.SetMusicVolume(0.8f, 0);
			var task = m_manager.FadeOutMusicAsync(0);
			await task;
			Assert.AreEqual(0f, m_manager.MusicVolume, 0.001f);
		}

		[Test]
		public void FadeOutMusicAsync_null_manager_returns_faulted()
		{
			BaseAudioManager nullManager = null;
			var task = nullManager.FadeOutMusicAsync(1f);
			Assert.AreEqual(UniTaskStatus.Faulted, task.Status);
		}

		[Test]
		public void FadeOutMusicAsync_pre_cancelled_token_returns_cancelled()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();
			var task = m_manager.FadeOutMusicAsync(1f, cts.Token);
			Assert.AreEqual(UniTaskStatus.Canceled, task.Status);
		}
	}
}
```

**Caveat:** mid-flight cancellation tests for Audio depend on the DOTween tweener being in a known state, which requires PlayMode (DOTween's update hook). PR-A's Audio tests therefore cover only the synchronous paths (zero duration, null guard, pre-cancelled token); mid-flight tests are deferred to a follow-up if the user adds PlayMode test infrastructure later.

**Verify:**

- Test Runner → EditMode → `AudioAsyncTests` shows 6 tests.
- Run all → 6 / 6 green.
- Total test count rises from 172 (after T5) to 178.

---

## T11 — Audio module metadata

### T11.1 — Update Audio `package.json`

**File:** `Assets/RevCore/Audio/package.json`

```json
{
  "name": "com.rabear.revcore.audio",
  "version": "1.1.0",
  ...
  "dependencies": {
    "com.rabear.revcore.foundation": "1.0.0",
    "com.rabear.revcore.inspector": "1.0.0",
    "com.rabear.revcore.prefs": "1.0.0",
    "com.cysharp.unitask": "2.5.10"
  },
  ...
}
```

### T11.2 — Update `PublicAPI.Unshipped.txt`

**File:** `Assets/RevCore/Audio/Runtime/PublicAPI.Unshipped.txt`

Append:

```
RevCore.AudioAsyncExtensions
static RevCore.AudioAsyncExtensions.FadeMusicAsync(this RevCore.BaseAudioManager manager, float targetVolume, float duration, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) -> Cysharp.Threading.Tasks.UniTask
static RevCore.AudioAsyncExtensions.FadeOutMusicAsync(this RevCore.BaseAudioManager manager, float duration, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) -> Cysharp.Threading.Tasks.UniTask
```

### T11.3 — Update Audio module CHANGELOG

Unlike `Timer/CHANGELOG.md` (which only has a `[1.0.0]` scaffold entry), `Audio/CHANGELOG.md` currently carries a non-empty `[Unreleased]` section. Inspect the file before editing — verified at this writing it contains seven `Fixed` entries (AudioCollection lookups, BaseAudioManager init, etc.) and one `Added` entry (tests for those fixes) that all shipped at v0.5.0 but were never moved out of `[Unreleased]` during the v0.5.0 cut (pre-existing module-CHANGELOG drift acknowledged in `docs/SESSION_HANDOFF.md` §4).

Two sub-steps:

#### T11.3a — Retroactively assign the existing `[Unreleased]` content to `[0.5.0]`

**File:** `Assets/RevCore/Audio/CHANGELOG.md`

Change the existing `## [Unreleased]` header to `## [0.5.0] - 2026-05-17` (matching the v0.5.0 ship date in root `CHANGELOG.md`). The `Fixed` and `Added` lists below it are unchanged. Insert a fresh empty `## [Unreleased]` above for future cycles, then the new `## [1.1.0]` section described in T11.3b.

#### T11.3b — Add the `[1.1.0]` section

```markdown
## [Unreleased]

## [1.1.0] - 2026-05-19

### Added

- `AudioAsyncExtensions.FadeMusicAsync` — awaitable music-volume fade.
- `AudioAsyncExtensions.FadeOutMusicAsync` — awaitable fade-to-zero + stop.
- Hard dependency on `com.cysharp.unitask` declared in `package.json`.

## [0.5.0] - 2026-05-17

(existing Fixed + Added lists, formerly under Unreleased)

## [1.0.0] - 2026-05-13

(unchanged scaffold)
```

**Verify:**

- XML doc coverage script reports the new public members as documented.
- Audio CHANGELOG no longer has any pre-v1.1 content under `[Unreleased]` (the section is now empty / staging for the next cycle).

---

## T12 — Root CHANGELOG entry

**File:** `CHANGELOG.md`

Promote the empty `[Unreleased]` section to `[1.1.0] - 2026-05-19` and add a new empty `[Unreleased]` above it. Update the link table at the bottom (`[1.1.0]: …compare/v1.0.0...v1.1.0` and `[Unreleased]: …compare/v1.1.0...HEAD`).

```markdown
## [Unreleased]

## [1.1.0] - 2026-05-19

UniTask integration (PR-A of the spec at `docs/superpowers/specs/2026-05-17-revcore-unitask-integration-design.md`). Purely additive — no deprecations, no behaviour changes to the v1.0 surface.

### Added

- `RevCore.Timer`:
  - `Timers.DelayAsync(float, bool, CancellationToken)` — awaitable wall-clock delay.
  - `Timers.WaitForConditionAsync(Func<bool>, CancellationToken)` — awaitable predicate poll.
  - `Timers.WaitForFramesAsync(int, CancellationToken)` — awaitable N-Tick wait.
- `RevCore.Audio`:
  - `AudioAsyncExtensions.FadeMusicAsync(this BaseAudioManager, float, float, CancellationToken)` — awaitable music fade.
  - `AudioAsyncExtensions.FadeOutMusicAsync(this BaseAudioManager, float, CancellationToken)` — awaitable fade-out + stop.
- Hard dependency on `com.cysharp.unitask` (2.5.10) declared in both modules' `package.json`. UniTask is already in `Packages/manifest.json` at the repo level, so consumer install cost is zero.

### Changed

- `Timers` static class is now `partial` (file split: `Core/Timers.cs` keeps the v1.0 callback API; `Core/TimersAsync.cs` adds the async API). No observable change.
```

Update version links section:

```markdown
[Unreleased]: https://github.com/hnb-rabear/RCore/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/hnb-rabear/RCore/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/hnb-rabear/RCore/compare/v0.5.0...v1.0.0
[0.5.0]: https://github.com/hnb-rabear/RCore/releases/tag/v0.5.0
```

---

## T13 — User verification in Unity

User runs these gates locally and reports status before commit:

### T13.0 — Baseline check (BEFORE any of T2–T12 are applied)

Optional but recommended. From a clean `main` checkout (before branching to `feat/timer-audio-unitask-v1.1`):

1. Unity Test Runner → EditMode → Run All. Record the test count. **Expected baseline: 160** (per v1.0.0 SESSION_HANDOFF). If the count differs, the delta arithmetic in T13.2 below needs adjustment — recompute the expected post-PR-A count as `(baseline + 12 + 6)` and use that instead of the literal `178`.
2. `python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json` → record `Public members: <N>`. **Expected baseline: 956**. If different, the expected delta in T13.3 still holds (+6), but the absolute total shifts accordingly.

This step prevents discovering mid-PR that the baseline drifted between v1.0.0 and now.

### T13.1 — Compile

Unity Editor recompiles cleanly with no error (warnings RS0016 expected on legacy `Assets/RCore.*` per the dormant-analyzer documentation — those are pre-existing).

### T13.2 — Tests

Unity Test Runner → EditMode → Run All. Expected count: `<baseline from T13.0> + 18` (= **178** if baseline is 160). Breakdown: +12 from `TimerAsyncTests`, +6 from `AudioAsyncTests`. All green.

### T13.3 — XML doc coverage

```powershell
python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json
```

Expected: 100 % coverage; documented count rises by 6 versus T13.0 baseline (3 Timer async methods + `AudioAsyncExtensions` type + 2 Audio async methods). The baseline `scripts/xmldoc-baseline.json` may need its threshold updated — pass `--update-baseline` if the script complains about the new count.

### T13.4 — No untracked file noise

`git status -s` should list only the planned files (see T14 for the exact whitelist). `Assets/RCore.Archives/` and other `Assets/RCore.*` folders must be untouched.

If any step fails, capture the failure detail (test name, line number, log message) and pass back to the next session before continuing.

---

## T14 — Commit + push + PR

Once T13 is green:

```powershell
git add Assets/RevCore/Timer/ Assets/RevCore/Audio/ CHANGELOG.md
git status --short
```

Verify the staged set matches exactly the files listed in T2–T11. No stray Unity-meta noise.

```powershell
git commit -m "feat(timer,audio): UniTask async API (v1.1.0)"
```

Commit message body should expand on the change per the established pattern in earlier release commits (`a14e598`, `97e7afc`).

```powershell
git push -u origin feat/timer-audio-unitask-v1.1
```

Open PR with title `feat(timer,audio): UniTask async API — v1.1.0`. Body references the spec doc and the task list above. Test plan checkboxes mirror T13.

After merge: tag `v1.1.0` from `main`, push the tag, `release.yml` publishes the GitHub Release. Then begin PR-B (Addressables module) on a new branch.

---

## Risks

- **DOTween dependency for Audio cancellation snap.** The `manager.SetMusicVolume(manager.MusicVolume, 0)` cancellation path relies on the `m_musicTweener?.Kill()` at the top of `SetMusicVolume`. Without DOTween (define `DOTWEEN` not set), the existing `BaseAudioManager` uses `StartCoroutine(LerpCoroutine(...))` for the fade — there is no exposed handle to stop that coroutine from an extension method. **Mitigation:** document in the `AudioAsyncExtensions` XML doc that cancellation reliably snap-cancels only when DOTween is enabled. Without DOTween, cancellation still TrySetCanceled the task, but the lerp coroutine continues to mutate `m_musicVolume` until it finishes. This is acceptable for v1.1.0; a clean coroutine-side cancellation is a v1.2 follow-up.
- **Sub-frame timing in tests.** `DelayAsync_returns_after_duration` advances the scheduler manually with `Tick(0.01f, 0.01f)` × 6 to simulate elapsed time deterministically. This sidesteps Unity's real frame timing entirely — tests stay deterministic. Real-world UniTask awaits driven by `TimerDriver`'s `Update` will jitter, but that is consumer-observable, not test-observable.
- **`CancellationTokenRegistration` not disposed.** Each cancellation registration in `DelayAsync` / etc. creates a `CancellationTokenRegistration` that we never call `.Dispose()` on. This is fine — the registration disposes itself when the token source is GC'd, and our timers are short-lived. Worst case: a long-running CTS with thousands of cancelled-but-not-disposed registrations would accumulate. Mitigation if needed in a follow-up: capture the registration and `Dispose()` it inside the `TrySetResult` path. Out of scope for v1.1.0.

## Rollback

The PR is purely additive. Revert is one commit and removes:

- 2 new `.cs` files in Runtime (`TimersAsync.cs`, `AudioAsyncExtensions.cs`).
- 2 new test files (`TimerAsyncTests.cs`, `AudioAsyncTests.cs`).
- 5 lines across `package.json` files (UniTask dependency on 2 modules + version bump on 2 modules).
- ~6 lines added to 2 asmdef files (`"UniTask"` reference).
- 1 line edit to existing `Timers.cs` (the `partial` keyword).
- CHANGELOG entries.

No v1.0.0 surface touched. Reverting does not affect the v1.0 consumer contract.
