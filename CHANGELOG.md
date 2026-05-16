# Changelog

All notable changes to RevCore are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions follow [SemVer 2.0.0](https://semver.org/) per [docs/contributing/SEMVER_POLICY.md](docs/contributing/SEMVER_POLICY.md).

## [Unreleased]

### Deprecated

- `JObjectDB.collections` field marked `[Obsolete]` (Stage 1 per `DEPRECATION_POLICY.md`). Direct mutation of this static dictionary bypasses key persistence and is the source of "save file silently drops a collection after reload" reports. Use `GetCollection`, `CreateCollection`, `GetCollectionKeys`, or `GetAllData` instead. Field will be made private in v1.0.

### Fixed

- `PanelController.Show` now activates the GameObject before starting the show coroutine. The previous flow set `gameObject.SetActive(true)` from inside `IE_Show`, but Unity refuses `StartCoroutine` on an inactive GameObject and logs `Coroutine couldn't be started because the the game object '<name>' is inactive!` — meaning panels that began hidden (the typical case) never animated in. `SetActivePanel(true)` inside `IE_Show` stays, idempotent for active GameObjects, so subclass overrides still see the activation hook at the same lifecycle point.
- `ColorHelper.TryHexToColor` now enforces its documented `Color.clear` failure contract for invalid (non-empty, non-null) hex strings. Unity's `ColorUtility.TryParseHtmlString` leaves its `out` parameter at `Color.white` on parse failure, which was leaking through our wrapper: e.g. `TryHexToColor("not-a-hex", out var c)` returned `false` with `c == Color.white` instead of `Color.clear`. Affects only the new explicit-failure API; the legacy `HexToColor` silent-fail behavior is unchanged (it intentionally passes Unity's behavior through).
- `TimerScheduler.WaitForSeconds` / `WaitForCondition` no longer throw `ArgumentOutOfRangeException` when invoked a second time with the same non-zero `id`. The replace path cancelled the existing handle before overwriting the list slot; `Handle.Cancel()` fires the `RemoveHandle` callback, which `RemoveAt(i)`s the very slot the next line then tries to assign — the indexer throws because the list is now shorter than `i`. Fix: replace the slot first, then cancel the (no-longer-listed) handle. The "same non-zero id replaces existing timer" contract pinned by `TimerSchedulerTests.Same_non_zero_id_replaces_existing_timer` is restored. Both `m_countdowns` and `m_conditions` paths are affected; both are fixed.
- All 8 RevCore test asmdef now set `"includePlatforms": ["Editor"]` so tests appear in the Test Runner **EditMode** tab instead of PlayMode. All tests are pure `[Test]` (no `[UnityTest]`), and benchmark tests use `Measure.Method` (no `Measure.Frames`), so EditMode is safe. Previously tests defaulted to PlayMode, leaving the CI `editmode` matrix job discovering zero RevCore tests.
- `OptimizedScrollView.Initialize`, `OptimizedHorizontalScrollView.Init`, `OptimizedVerticalScrollView.Init` now early-return cleanly on `totalItems <= 0` (free items, zero out cached state, collapse container size). Previously they fell through to indexing `m_itemsScrolled[0]`, which crashed when the prefab list was empty. The empty-list state is a legitimate intermediate during async data loads and must not raise.
- `OptimizedHorizontalScrollView.ScrollBarChanged` no longer logs an error when `m_optimizedTotal == 0` — that condition is the legitimate empty-scroll state, not a programming bug. The horizontal variant now matches the existing silent guards on `OptimizedScrollView` and `OptimizedVerticalScrollView`.
- `BaseAudioManager.SetMasterVolume` / `SetMusicVolume` / `SetSFXVolume` now null-check the cached `Tweener` before calling `Kill()`. Calling these volume setters before any fade had ever started (e.g. on the very first frame, or after a domain reload that nulled the tween fields) would throw a `NullReferenceException` when DOTWEEN was enabled. Guarded with `?.Kill()`.

### Added

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

- `.editorconfig` extended with C# code style and analyzer severity rules.
- `.gitattributes` added with `*.meta merge=union` to reduce conflict on regenerated GUIDs.

[Unreleased]: https://github.com/hnb-rabear/rcore/compare/main...HEAD
