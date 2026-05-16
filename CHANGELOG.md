# Changelog

All notable changes to RevCore are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions follow [SemVer 2.0.0](https://semver.org/) per [docs/contributing/SEMVER_POLICY.md](docs/contributing/SEMVER_POLICY.md).

## [Unreleased]

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
