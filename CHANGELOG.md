# Changelog

All notable changes to RevCore are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions follow [SemVer 2.0.0](https://semver.org/) per [docs/contributing/SEMVER_POLICY.md](docs/contributing/SEMVER_POLICY.md).

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

## [1.0.0] - 2026-05-17

The 1.0 release cut. Closes out the framework hardening plan: every Stage 1 deprecation tracked through v0.5.0 is resolved (deleted, made internal, or promoted to Stage 3), the full RevCore public surface is recorded in per-module `PublicAPI.Shipped.txt` files (1337 entries across 8 modules), and the package version is bumped.

Pre-1.0 minor versions were allowed to break the public API per `docs/contributing/SEMVER_POLICY.md`. From 1.0.0 onward, breaks require a major bump and a deprecation window.

**Analyzer status:** the `Microsoft.CodeAnalysis.PublicApiAnalyzers` Roslyn analyzer DLLs ship in `Assets/RevCore/_Analyzers/` and the per-module `PublicAPI.Shipped.txt` / `Unshipped.txt` files are committed, but the `RoslynAnalyzer` asset label is NOT set on the DLLs — meaning the analyzer is dormant and does not enforce the surface at compile time. The reason: Unity loads any DLL with the `RoslynAnalyzer` label project-wide, including the default `Assembly-CSharp(-Editor)` compilations that contain the legacy RCore folders (which carry no asmdef and deliberately track no PublicAPI surface). Editorconfig-based scoping does not prevent the analyzer from firing RS0016 on those files. The maintainer's policy is to keep RCore untouched, so live enforcement is deferred to a future release if Unity gains better asmdef-level analyzer scoping. The Shipped.txt files still serve as a committed paper trail for PR review and consumer-facing documentation of the v1.0 contract.

### Added

- `IRevDiagnostics` (Phase 7.1) — opt-in observability interface that the framework's hot paths fire on. Ten hooks across four modules: Timer (scheduled / cancelled / completed), EventBus (subscribed / unsubscribed / published), Pool (spawn / release), and Audio (PlaySFX / PlayMusic). Default state (no listener) costs one null check per hook site; the static accessor `RevDiagnostics.Listener` lets a consumer wire a single implementation at startup. Payload is metadata only (primitive types + `System.Type`) — no `ITimerHandle`, no `Component`, no `AudioClip` — so consumer code doesn't accidentally prolong GC lifetimes by capturing object refs in closures.
- Three reference listeners under `Foundation/Samples~/Diagnostics/`: `CrashBufferDiagnostics` (last N events as a ring buffer for crash report attachments), `DebugOverlayDiagnostics` (on-screen counter MonoBehaviour for active timers / events/sec / spawns/sec), and `UnityLogDiagnostics` (verbose `Debug.Log` mirror — dev-only). Surfaced via the Foundation `samples[]` Package Manager entry.
- `docs/migration/README.md`, `docs/migration/PLAN.md`, `docs/migration/gap-categories.md` (Phase 8) — the consumer-facing migration documentation set. The README is the entry point; PLAN.md is the step-by-step playbook (when to migrate, module order, mechanical-rename batch, save-file compatibility, rollback); gap-categories.md groups the 259 `GAP` types into ~14 buckets with a default `PORT` / `REPLACE` / `DROP` / `DEFER` action per bucket. The plan is documentation-only — no migration tool ships yet.

### Changed

- All nine `package.json` files bumped from `0.5.0` to `1.0.0` (Audio, Data, Foundation, Inspector, Pool, Prefs, Timer, Tools, UI). Consumers pinned to `?path=Assets/RevCore/{Module}#v0.5.0` upgrade to `#v1.0.0` to pick up this cut.
- All currently-public RevCore symbols bulk-populated into `PublicAPI.Shipped.txt` (1337 entries across the eight runtime modules — Audio 74, Data 139, Foundation 391, Inspector 40, Pool 85, Prefs 42, Timer 70, UI 496). The `PublicAPI.Unshipped.txt` files are empty post-cut (with the canonical `#nullable enable` header retained as the format requires). Generated by Rider's "Add to public API" code fix applied solution-wide while the analyzer was temporarily active, then promoted to Shipped via `scripts/seal-public-api.py`.
- `scripts/seal-public-api.py` added — idempotent helper that promotes every module's `PublicAPI.Unshipped.txt` entries to `PublicAPI.Shipped.txt` (sorted), resetting `Unshipped.txt` to header-only. Used at every release cut.
- `docs/migration/rcore-to-revcore-api-map.{md,csv}` audited: 16 `LIKELY` rows resolved (7 promoted to `RENAMED` after verifying the RevCore target exists, 9 demoted to `GAP` because the fuzzy-match target was wrong — Editor drawers, mismatched type kinds, etc.). The map now reports 0 LIKELY, 15 RENAMED, 97 PORTED, 259 GAP. The two stale `PORTED` rows for `SimpleButton` and `CustomToggleSlider` (both deleted from RevCore in v0.5.0) are recorded as `RENAMED` to their drop-in replacements (`SimpleTMPButton`, `JustToggle`).
- `EventBus.Publish<T>`, `EventBus.ListenerCountFor<T>`, and `EventBus.Clear<T>` are now zero-alloc on the hot path. The per-type storage moves from `Dictionary<Type, Delegate>` to `Dictionary<Type, Entry>` where `Entry` keeps the multicast delegate plus a maintained `Count`. The old code called `Delegate.GetInvocationList()` inside `Publish` purely to feed the diagnostics callback the listener count — that call returns a freshly-allocated `Delegate[]` every Publish, which dominated steady-state allocations for consumer projects that publish per-frame. Subscribe / Unsubscribe maintain the count alongside the delegate; behavior (dedup on Subscribe, no-op on unknown Unsubscribe, silent Publish with zero listeners, accurate `Clear<T>` decrement) is bit-for-bit identical and remains pinned by `Characterization_EventBusTests` and `RevDiagnosticsTests`.
- `Result<T>.Value` is now `internal` (was public `[Obsolete]` Stage 1). The public API for reading a `Result<T>` is `TryGetValue(out T value)` (preferred) or `ValueOr(T fallback)`. The internal getter is preserved for tests pinning the throw-on-error contract via `[InternalsVisibleTo("RevCore.Foundation.Tests")]` (added to `Result.cs`); the `#pragma warning disable CS0618` lines in `ResultTests` are gone with the obsolete attribute.

### Removed

- `MathHelper.Ded2Rad(float)` and `MathHelper.Tad2Deg(float)` deleted (had been `[Obsolete]` Stage 1 — typos). Use `Deg2Rad(float)` / `Rad2Deg(float)`.
- `TransformHelper.CovertAnchoredPosFromChildToParent(...)` (both overloads) deleted (had been `[Obsolete]` Stage 1 — typo). Use `ConvertAnchoredPosFromChildToParent(...)`.
- `PoolsContainer<T>.GetActiveList()` and `PoolsContainer<T>.GetAllItems()` deleted (had been `[Obsolete]` Stage 1 — allocated a fresh `List<T>` per call). Use `ForEachActive(Action<T>)` / `ForEachItem(Action<T>)` for zero-alloc iteration, or `CopyActiveTo(List<T>)` / `CopyAllTo(List<T>)` when a buffer is needed.
- `JObjectDB.collections` (the public `Dictionary<string, JObjectData>` get-only property, formerly `[Obsolete(..., error: true)]` Stage 2) deleted. Internal storage `s_collections` remains private. Consumers use `GetCollection(string)`, `CreateCollection<T>`, `GetCollectionKeys()`, or `GetAllData()`.
- Reference in `Assets/RevCore/Prefs/README.md` to a future "encrypted prefs" RevCore.Data feature. RevCore intentionally does not ship value encryption — a hardcoded-key obfuscation layer only stops PlayerPrefs editors, not anyone with the binary. The README now states the no-encryption policy explicitly and points consumers at server-side validation for sensitive state.

## [0.5.0] - 2026-05-17

First tagged release. Establishes the pre-1.0 baseline that consumer projects can pin via UPM `?path=` git URL. All Phase 0–6 work plus most of Phase 4 (perf wins) is in this cut. v1.0 is reserved for the final cleanup — Stage 1 deprecations going to Stage 2/3 and the PublicAPI Roslyn analyzer flipping live.

### Removed

- `.github/workflows/lint.yml`, `.github/workflows/docs-coverage.yml`, `.github/workflows/release-drafter.yml` (workflow), `.github/release-drafter.yml` (config), `.github/labels.yml`, `.markdownlint-cli2.jsonc` — the three PR-time workflows added too little value for the solo-maintained workflow. Lint catches issues an EditorConfig-aware IDE already catches on save; docs coverage and benchmark regression run as local checks via the same Python scripts; release notes can be composed by hand at v1.0 cut time. The repo keeps `release.yml` (tag-triggered publish) and the local scripts (`check-xmldoc-coverage.py`, `check-benchmark-regression.py`). Quality gates move entirely to the local-run-before-push discipline documented in `docs/contributing/CI_SETUP.md` and `docs/contributing/README.md`.
- `.github/workflows/unity-test.yml` and `.github/workflows/benchmark.yml` — the maintainer does not hold a Unity license and Personal manual activation has been deprecated upstream (`license.unity3d.com/manual` now only accepts Plus/Pro serial numbers). Tests and benchmarks run locally before push instead. The four remaining workflows (lint, docs-coverage, release-drafter, release) are pure-text and need no secrets. The `Unity.PerformanceTesting` and `com.unity.testtools.codecoverage` packages stay in `manifest.json` so the local Test Runner can still execute the benchmark suite and the regression script.
- `RevCore.UI.SimpleButton` and its `SimpleButtonEditor` deleted. The type had been `[Obsolete("Use SimpleTMPButton instead")]` for one minor. `SimpleTMPButton` is the drop-in replacement — same `JustButton` base, label is TextMeshPro instead of the legacy `UnityEngine.UI.Text`. Pre-1.0 minor break per `SEMVER_POLICY`. RCore's `SimpleButton` (legacy framework) is intentionally retained for backward compatibility with the four in-flight consumer projects.
- `RevCore.UI.CustomToggleSlider` and its `CustomToggleSliderEditor` deleted. The type had been `[Obsolete("Use JustToggle instead")]` for one minor. `JustToggle` is the drop-in replacement — same underlying `Toggle.isOn` contract, richer transition system. Pre-1.0 minor break per `SEMVER_POLICY`.
- `RCore.UI.CustomToggleSlider` (legacy framework) deleted in lockstep with RevCore. The legacy sample `Assets/RCore/Main/Samples~/Examples/Script/UI/PanelExample.cs` now references `JustToggle` instead.

### Deprecated

- `PoolsContainer<T>.GetActiveList()` and `PoolsContainer<T>.GetAllItems()` marked `[Obsolete]` (Stage 1) — both allocate a new `List<T>` on every call. Replacements: `ForEachActive(Action<T>)` / `ForEachItem(Action<T>)` (zero-alloc iteration) or `CopyActiveTo(List<T>)` / `CopyAllTo(List<T>)` (caller owns the buffer, zero-alloc when reused across frames). The old methods now delegate to the new ones, so the warning is the only behavior change. Both will be removed at v1.0.
- `JObjectDB.collections` bumped to **Stage 2** (`[Obsolete(..., error: true)]`) — direct field access is now a compile error. The deprecation window opened in Phase 3; consumers that haven't migrated have one minor's notice. Use `GetCollection`, `CreateCollection`, `GetCollectionKeys`, or `GetAllData` instead. The field stays in place during Stage 2 so reflection-based access (e.g. unit tests using `#pragma warning disable CS0619`) still works; the field will become private at v1.0 (Stage 3). Internal callers in `JObjectDB`'s own pragma block, the two Editor utilities (`JObjectDBManagerEditor`, `JObjectModelCollectionEditor`), and the test suite were migrated to the public API in the same commit.
- `MathHelper.Ded2Rad(float)` / `Tad2Deg(float)` marked `[Obsolete]` (Stage 1) — typos. Use `Deg2Rad` / `Rad2Deg` instead. Will be removed in v1.0.
- `TransformHelper.CovertAnchoredPosFromChildToParent` (both overloads) marked `[Obsolete]` (Stage 1) — typo. Use the new `ConvertAnchoredPosFromChildToParent` instead. Will be removed in v1.0.
- `Result<T>.Value` getter marked `[Obsolete]` (Stage 1). Throws on error; prefer `TryGetValue(out value)` or `ValueOr(fallback)` to avoid the throw. Will become internal in v1.0.
- `JObjectDB.collections` field marked `[Obsolete]` (Stage 1 per `DEPRECATION_POLICY.md`). Direct mutation of this static dictionary bypasses key persistence and is the source of "save file silently drops a collection after reload" reports. Use `GetCollection`, `CreateCollection`, `GetCollectionKeys`, or `GetAllData` instead. Field will be made private in v1.0.

### Fixed

- `PanelRoot` and `BaseAudioManager` no longer trigger Unity's "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate" warnings on import. `PanelRoot` declares `[RequireComponent(typeof(Canvas))]` + `[RequireComponent(typeof(GraphicRaycaster))]` instead of creating those components from `OnValidate`. `BaseAudioManager` moves the "Music" / "Sfx" child GameObject creation from `OnValidate` to `Reset()` (Editor-only, fires once on Add Component); the runtime path in `EnsureAudioSources` (called from `Start`) still creates the children on demand, so PlayMode behavior is unchanged. `OnValidate` retains the read-only field validation pass.
- `PanelController.Show` now activates the GameObject before starting the show coroutine. The previous flow set `gameObject.SetActive(true)` from inside `IE_Show`, but Unity refuses `StartCoroutine` on an inactive GameObject and logs `Coroutine couldn't be started because the the game object '<name>' is inactive!` — meaning panels that began hidden (the typical case) never animated in. `SetActivePanel(true)` inside `IE_Show` stays, idempotent for active GameObjects, so subclass overrides still see the activation hook at the same lifecycle point.
- `ColorHelper.TryHexToColor` now enforces its documented `Color.clear` failure contract for invalid (non-empty, non-null) hex strings. Unity's `ColorUtility.TryParseHtmlString` leaves its `out` parameter at `Color.white` on parse failure, which was leaking through our wrapper: e.g. `TryHexToColor("not-a-hex", out var c)` returned `false` with `c == Color.white` instead of `Color.clear`. Affects only the new explicit-failure API; the legacy `HexToColor` silent-fail behavior is unchanged (it intentionally passes Unity's behavior through).
- `TimerScheduler.WaitForSeconds` / `WaitForCondition` no longer throw `ArgumentOutOfRangeException` when invoked a second time with the same non-zero `id`. The replace path cancelled the existing handle before overwriting the list slot; `Handle.Cancel()` fires the `RemoveHandle` callback, which `RemoveAt(i)`s the very slot the next line then tries to assign — the indexer throws because the list is now shorter than `i`. Fix: replace the slot first, then cancel the (no-longer-listed) handle. The "same non-zero id replaces existing timer" contract pinned by `TimerSchedulerTests.Same_non_zero_id_replaces_existing_timer` is restored. Both `m_countdowns` and `m_conditions` paths are affected; both are fixed.
- All 8 RevCore test asmdef now set `"includePlatforms": ["Editor"]` so tests appear in the Test Runner **EditMode** tab instead of PlayMode. All tests are pure `[Test]` (no `[UnityTest]`), and benchmark tests use `Measure.Method` (no `Measure.Frames`), so EditMode is safe. Previously tests defaulted to PlayMode, leaving the CI `editmode` matrix job discovering zero RevCore tests.
- `OptimizedScrollView.Initialize`, `OptimizedHorizontalScrollView.Init`, `OptimizedVerticalScrollView.Init` now early-return cleanly on `totalItems <= 0` (free items, zero out cached state, collapse container size). Previously they fell through to indexing `m_itemsScrolled[0]`, which crashed when the prefab list was empty. The empty-list state is a legitimate intermediate during async data loads and must not raise.
- `OptimizedHorizontalScrollView.ScrollBarChanged` no longer logs an error when `m_optimizedTotal == 0` — that condition is the legitimate empty-scroll state, not a programming bug. The horizontal variant now matches the existing silent guards on `OptimizedScrollView` and `OptimizedVerticalScrollView`.
- `BaseAudioManager.SetMasterVolume` / `SetMusicVolume` / `SetSFXVolume` now null-check the cached `Tweener` before calling `Kill()`. Calling these volume setters before any fade had ever started (e.g. on the very first frame, or after a domain reload that nulled the tween fields) would throw a `NullReferenceException` when DOTWEEN was enabled. Guarded with `?.Kill()`.

### Added

- `PoolsContainer<T>.ForEachActive(Action<T>)` / `ForEachItem(Action<T>)` — zero-alloc iteration over the pool's active or full membership; preferred over the allocating `GetActiveList()` / `GetAllItems()` on per-frame hot paths.
- `PoolsContainer<T>.CopyActiveTo(List<T>)` / `CopyAllTo(List<T>)` — caller-owned-buffer variants. Reuse the same list across frames to reach steady-state zero allocations.
- `TransformHelper.ConvertAnchoredPosFromChildToParent` (extension on `RectTransform` + static overload with raw values) — correctly-spelled replacement for the legacy `Covert…` typo. Same behavior; new API is the path forward, old name remains for one minor as `[Obsolete]` Stage 1.
- `ColorHelper.TryHexToColor(string, out Color)` — explicit-failure variant of `HexToColor`. Returns `false` and `Color.clear` on invalid/null/empty input. The existing `HexToColor` is unchanged.
- `JObjectDBManager<T>.SaveForced()` — bypasses the 200 ms throttle that `Save(now: true)` applies. Internal `OnApplicationPause`/`OnApplicationQuit` now use this so end-of-life saves can never silently fail under the throttle (the original cause of intermittent save loss in shipping projects). The legacy `Save(now: true)` behaviour is unchanged.
- `EventBus.ListenerCountFor<T>()` — per-type listener count on the concrete `EventBus` class. O(1) dictionary lookup plus one invocation-list walk only when listeners exist; prefer this over the aggregate `IEventBus.ListenerCount` on hot paths. Not added to `IEventBus` to avoid breaking external implementations.
- Governance docs under `docs/contributing/`: API design guidelines, semver policy, deprecation policy, release checklist, public API guide.
- GitHub Actions workflows: Unity test matrix (2022 LTS + Unity 6), lint, docs coverage, release drafter.
- Issue templates (bug, feature, question) and PR template.
- Root `CHANGELOG.md`.
- Phase 1 inventory artifacts: `docs/api-inventory.csv` (503 symbols), `docs/migration/rcore-to-revcore-api-map.{md,csv}`, `docs/gap-analysis.md` (250 RCore-only types catalogued).
- Characterization tests pinning current behavior of `ColorHelper.HexToColor`, `EventBus.Publish`/`ListenerCount`, `RevPool` over-capacity eviction, `TimerScheduler.Cancel` (incl. the `id=0` matches-all-defaults sharp edge).
- Tooling scripts: `scripts/extract-api-surface.py`, `scripts/build-migration-map.py`.
- Phase 2: benchmark suite (`Benchmark_EventBusTests`, `Benchmark_RevPoolTests`, `Benchmark_TimerSchedulerTests`) + CI workflow (`.github/workflows/benchmark.yml`) + regression checker (`scripts/check-benchmark-regression.py`) + `docs/contributing/BENCHMARK_GUIDE.md`.
- `com.unity.test-framework.performance` (3.0.3) and `com.unity.testtools.codecoverage` (1.2.6) added to `Packages/manifest.json`.

### Changed

- `ShowIfDrawer` (Inspector) now caches a compiled-lambda accessor per `(targetType, memberName)`. The original implementation did three `Type.GetField` / `GetProperty` / `GetMethod` reflection lookups every `OnGUI` — at ~10 µs each, that's ~30 µs per `[ShowIf]` field per Inspector frame. With many `[ShowIf]` properties on a single Inspector this added up to visible jitter while scrolling. The cache pays the reflection + `Expression.Compile` cost on first hit (~tens of µs) and reduces every subsequent call to a tight delegate invoke (~tens of ns) — roughly three orders of magnitude per call. Misses (member not found) are cached too, so the Console no longer drowns when a referenced member is genuinely missing; the error is logged once per unique (type, name) pair.
- `RevPool.Spawn` no longer walks the inactive bucket for null entries on every call. Items only become null when their GameObjects were destroyed externally — rare in steady-state usage. Spawn now checks the tail (the LIFO pop site) and only triggers `RemoveNullInactiveItems` when that one slot is null. Common-case Spawn drops from O(inactive_count) to O(1) for the cleanup step. `RelocateInactive` is unchanged and still runs when the bucket is empty so externally-deactivated items get reabsorbed.
- `EventBus.ListenerCount` is now an O(1) read of a maintained counter instead of an O(types × listeners) walk that called `GetInvocationList()` once per subscribed event type. Subscribe / Unsubscribe / Clear / `Clear<T>` keep the counter in sync. Behavior preserved: deduplicated subscribes don't double-count, unsubscribing a listener that wasn't registered is still a silent no-op, and `Clear<T>` properly subtracts that type's listeners. Pinned by `Characterization_EventBusTests.ListenerCount_sums_invocation_lists_across_types`.
- `TimerScheduler.Cancel(int)`, `WaitForSeconds`, `WaitForCondition`, and the internal handle-removal callback now run in amortized O(1) instead of O(n). Two parallel indices on top of the main list — a per-id map and a per-handle index map — turn id lookups, dedup checks, and handle removals from full-list scans into dictionary hits. The observable contract (Cancel(0) matches all default-id timers, same non-zero id replaces, Tick callback order in reverse-registration) is preserved and pinned by `Characterization_TimerSchedulerTests`. Cancelled timers stay in the main list until the next `Tick` reaps them (lazy cleanup), which keeps re-entrant Cancel/Replace paths safe.
- `.editorconfig` extended with C# code style and analyzer severity rules.
- `.gitattributes` added with `*.meta merge=union` to reduce conflict on regenerated GUIDs.

[Unreleased]: https://github.com/hnb-rabear/RCore/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/hnb-rabear/RCore/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/hnb-rabear/RCore/compare/v0.5.0...v1.0.0
[0.5.0]: https://github.com/hnb-rabear/RCore/releases/tag/v0.5.0
