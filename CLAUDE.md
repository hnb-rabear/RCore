# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

One Unity 2022.3.62f2 project holding **three separately-versioned code bases**:

| Path | What | Version | Status |
| --- | --- | --- | --- |
| `Assets/RCore/` | Legacy monolith framework (`Main`, `Services`, `Sub`) | 1.1.9 | In production across 4+ consumer projects. Frozen — do not edit unless asked. |
| `Assets/RevCore/` | Next-gen rewrite: 9 runtime + 1 editor-only UPM packages | 1.0.0–1.1.0 per package | Active work. Semver-stable since v1.0.0. |
| `Assets/RCore.SheetX/` | Editor-only spreadsheet→code/JSON exporter | 1.1.0 | Own CHANGELOG, own release cadence. |

Namespaces don't collide (`RevCore` flat vs `RCore.*` nested), so both frameworks compile side by side. Migration is opt-in with no deadline.

Solo maintainer. Quality bar is "shippable to 5+ teams" — the user's standing instruction is *"không gấp, tỉ mỉ, không được phạm sai sót"* (careful, no rush, no mistakes).

## Commands

No PR-time CI. `.github/workflows/release.yml` is the only workflow (fires on `v*` tag, validates every `Assets/RevCore/**/package.json` version equals the tag, publishes the release). **Every gate below runs locally before push.**

```powershell
# XML doc coverage — must stay at 0 regressions (currently 1015/1015)
python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json

# Refresh the public API inventory after adding public members
python scripts/extract-api-surface.py --out docs/api-inventory.csv

# Promote PublicAPI Unshipped -> Shipped at a release cut
python scripts/seal-public-api.py            # --dry-run to preview

# Benchmark regression (5% tolerance) after a Unity Performance run
python scripts/check-benchmark-regression.py --results Library/ --baseline scripts/benchmark-baseline.json
```

### Tests

Unity Test Runner is the primary path: **EditMode → Run All** (~160 tests, ~25s). Performance benchmarks live under the `Performance` NUnit category — run that category separately, then feed `Library/PerformanceTestResults.json` to the regression script above.

Headless, when `Unity.exe` is on PATH:

```powershell
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -quit
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults-PlayMode.xml -quit
```

Single test / subset: add `-testFilter "RevCore.Tests.BigNumberTests"` or `-testCategory "Performance"`. In the Editor, filter by typing the class name into the Test Runner search box.

Foundation, Timer, EventBus, Result, and the Pool scheduler are plain C# and run without a scene — those are the fast ones.

### Editor menus

`RevCore > Tools Hub`, `RevCore > Audio > Generate IDs`, `RevCore > Data > Log`, `RevCore > Prefs > Clear PlayerPrefs`. Legacy RCore hub is `Ctrl+Alt+/`.

## RevCore architecture

Packages, not a monolith. Each has its own `package.json`, `README.md`, `CHANGELOG.md`, `Samples~/`, `Tests/`, and separate Runtime/Editor asmdefs.

```text
Foundation  ──────────►  (Inspector)     (Tools — editor-only, standalone)
   ▲   ▲   ▲   ▲
 Timer  Prefs  │
   ▲     ▲     │
   │   Pool ───┘
   ├── Audio ──────────► Inspector + Prefs
   ├── Data  ──────────► Timer + Prefs + Inspector + Newtonsoft JSON
   └── UI    ──────────► Inspector + Pool + TMP

Addressables ──────────► Unity Addressables + UniTask   (zero RevCore deps)
```

`A → B` = A depends on B. Read `docs/ARCHITECTURE.md` (Vietnamese) for the full treatment — sections 4 (design philosophy), 5 (core patterns), 8 (known weaknesses).

Invariants that a change must not violate:

- **Foundation is the root.** It knows nothing about any other package. Everything else builds on it.
- **Zero undeclared cross-dependency.** A package may only reference what its `package.json` + asmdef declare. `Pool` cannot `using RevCore.Audio` even though both are in the project.
- **Editor code never enters a runtime assembly.** Each package splits Runtime/Editor asmdefs instead of using `#if UNITY_EDITOR` in runtime code.
- **Interface first.** Every public service has one (`IEventBus`, `ITimerScheduler`, `IAudioService`) so tests can fake it.
- **UniTask is a hard dependency of Timer, Audio, and Addressables only.** Foundation/Pool/Data/UI must stay UniTask-free.
- **Addressables and DOTween are optional**, gated by asmdef `versionDefines` + `#if ADDRESSABLES` / `#if DOTWEEN` with working fallbacks.
- **No encryption in RevCore.** Deliberate and closed: hardcoded-key obfuscation stops nobody with the binary. Sensitive state validates server-side.

Core patterns: `EventBus` (type-keyed pub/sub, zero-alloc `Publish`), `ServiceLocator` (type registry, no reflection), `Result<T>` (no-exception error model — `TryGetValue` / `ValueOr`), `ITimerScheduler` (plain C# scheduler, needs a driver to `Tick`), JObjectDB V2 (Data–Model–Collection–Manager over PlayerPrefs+JSON, `[Inject]` cross-model DI), `IRevDiagnostics` (10 opt-in hot-path hooks, 1 null check when unwired).

## Conventions

- Private instance fields `m_camelCase`, private static `s_camelCase`, public `PascalCase`. Enforced as a suggestion in `.editorconfig`.
- `.cs` files: **tabs**, **CRLF** (`.gitattributes` enforces). Everything else spaces + LF.
- Test methods `snake_case_descriptive`; test class named after the production class. Prefixes in use: `Characterization_` (locks existing behavior), `Benchmark_` (Performance category).
- Every public member needs `/// <summary>`. `CS1591` is a warning locally, escalated with `/warnaserror` in the coverage gate.
- **Adding a public member to a RevCore Runtime assembly requires a line in that module's `PublicAPI.Unshipped.txt`.** The Roslyn analyzer that would enforce this is *dormant* — the `RoslynAnalyzer` label is deliberately absent because Unity applies analyzers project-wide and `RS0016` would flood legacy RCore. So this is a **human review gate**: check the diff. See `docs/contributing/CI_SETUP.md` and `Assets/RevCore/_Analyzers/README.md`.
- Deprecate, never delete — 3 stages per `docs/contributing/DEPRECATION_POLICY.md`.
- One PR-sized change per commit, each with its own CHANGELOG entry. Root `CHANGELOG.md` is canonical; per-module CHANGELOGs are partly stale scaffold.
- Conventional commits with a package scope: `feat(addressables):`, `fix(sheetx):`, `docs(rcore.main):`. `!` marks a breaking change.

## Docs map

- `docs/SESSION_HANDOFF.md` — state carried over from the previous session. Read when picking up unfinished work.
- `docs/ARCHITECTURE.md` — RevCore design, flows, strengths, weaknesses (Vietnamese).
- `docs/contributing/` — SEMVER_POLICY, DEPRECATION_POLICY, PUBLIC_API_GUIDE, API_DESIGN_GUIDELINES, RELEASE_CHECKLIST, BENCHMARK_GUIDE, CI_SETUP.
- `docs/migration/` — RCore → RevCore API map and gap analysis. Planning only; no tooling shipped.
- `docs/superpowers/plans/` and `specs/` — dated implementation plans. Plans use `- [ ]` checkboxes and are executed with the `superpowers:executing-plans` skill.
- `Assets/RevCore/<Module>/README.md` — per-package API reference. Don't duplicate it here.

## Known state to be aware of

- SheetX had a live Google OAuth client ID and secret committed at `SheetXSettings.asset:105-106`, and a live `refresh_token` committed at `Assets/Editor/Google.Apis.Auth.OAuth2.Responses.TokenResponse-user`. Both are out of the working tree as of `9a6f695` / `f241b95` — credentials now live in `EditorPrefs`, the token store is gitignored. **The historical values are still in git history on a public remote** (a purge was considered and declined: rotation makes them inert, and a rewrite invalidates every clone). Rotation in Google Cloud Console and revoking the grant at `myaccount.google.com/permissions` are human actions tracked in `docs/superpowers/plans/2026-08-20-sheetx-hardening.md`.
- **SheetX cannot be tagged with the existing release workflow.** `.github/workflows/release.yml:31` validates a `v*` tag against `find Assets/RevCore -name package.json` only, so a `v1.1.0` tag fails against the ten RevCore packages sitting at 1.0.0/1.1.0. SheetX at 1.1.0 is committed but untagged; tagging it needs a separate scheme or a workflow change. `docs/contributing/RELEASE_CHECKLIST.md` is likewise RevCore-only — its version-bump and PublicAPI-promotion steps have no SheetX equivalent.
- SheetX has no automated tests and no `PublicAPI.*.txt` — the RevCore workflows do not apply to it. Its acceptance is the manual matrix in that plan.
- The doc-coverage gate roots at `Assets/RevCore` only. RCore and SheetX are outside it.
