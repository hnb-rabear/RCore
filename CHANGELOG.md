# Changelog

All notable changes to RevCore are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions follow [SemVer 2.0.0](https://semver.org/) per [docs/contributing/SEMVER_POLICY.md](docs/contributing/SEMVER_POLICY.md).

## [Unreleased]

### Added

- Governance docs under `docs/contributing/`: API design guidelines, semver policy, deprecation policy, release checklist, public API guide.
- GitHub Actions workflows: Unity test matrix (2022 LTS + Unity 6), lint, docs coverage, release drafter.
- Issue templates (bug, feature, question) and PR template.
- Root `CHANGELOG.md`.
- Phase 1 inventory artifacts: `docs/api-inventory.csv` (503 symbols), `docs/migration/rcore-to-revcore-api-map.{md,csv}`, `docs/gap-analysis.md` (250 RCore-only types catalogued).
- Characterization tests pinning current behavior of `ColorHelper.HexToColor`, `EventBus.Publish`/`ListenerCount`, `RevPool` over-capacity eviction, `TimerScheduler.Cancel` (incl. the `id=0` matches-all-defaults sharp edge).
- Tooling scripts: `scripts/extract-api-surface.py`, `scripts/build-migration-map.py`.

### Changed

- `.editorconfig` extended with C# code style and analyzer severity rules.
- `.gitattributes` added with `*.meta merge=union` to reduce conflict on regenerated GUIDs.

[Unreleased]: https://github.com/hnb-rabear/rcore/compare/main...HEAD
