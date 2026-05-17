# Session Handoff — Web Claude Code → Local Claude Code

**Created**: at the end of the web-based session that produced Phase 0-5 of the RevCore framework hardening.
**Reason**: switching to local Claude Code on Windows (`E:\Projects\_\RCore`) so the new session has direct filesystem access for Unity-side diagnostics.

> **First action for the new session**: read this entire document, then `docs/PHASE5_SUMMARY.md`, then `docs/contributing/README.md`. After that, address the BLOCKER section below.

---

## 0. Project context (background — once)

- **RCore** is the legacy Unity framework currently in production across 4+ projects (under `Assets/RCore/`).
- **RevCore** is its next-generation rewrite (under `Assets/RevCore/`) — still pre-1.0, not yet shipped to any project.
- **Goal of this work**: harden RevCore to production quality so it can ship to ≥5 teams. Solo maintainer (the user).
- **Unity targets**: 2022 LTS and Unity 6 (matrix CI).
- **Distribution**: UPM git URL with semver tags.
- **Constraint mentioned by user**: "không gấp, tỉ mỉ, không được phạm sai sót" — careful, no rush, but high quality bar.

The 10-phase plan lives in conversation history and partially in `docs/`. Phases 0, 1, 2, 3, 5 are done. Phases 4, 6, 7, 8, 9 are pending.

---

## 1. Branch + git state

- **Working branch**: `claude/analyze-revcore-modules-LHYH7`
- **Base**: `main`
- **Latest pushed commit**: `docs(phase-5): finalize summary at 100% coverage` (~`c48bb17`) plus this handoff commit after it.
- **All work pushed to origin**: yes.
- **Commit count on branch**: ~28 commits.

Quick state check in new session:
```bash
git status                  # should be clean after pulling
git branch --show-current   # claude/analyze-revcore-modules-LHYH7
git log --oneline -10       # see recent commits
```

---

## 2. What was done — phase by phase

### Phase 0 — Governance & CI (1 commit, ~39 files)

Foundation for everything else. Nothing changes runtime behavior.

- `docs/contributing/` × 6 files:
  - `README.md` — entry point
  - `API_DESIGN_GUIDELINES.md` — naming (m_ prefix, etc.), nullability, error model, threading
  - `SEMVER_POLICY.md` — what counts as breaking; pre-1.0 minor may break with note
  - `DEPRECATION_POLICY.md` — 3-stage: soft `Obsolete(error: false)` → hard `error: true` → remove
  - `PUBLIC_API_GUIDE.md` — `PublicAPI.{Shipped,Unshipped}.txt` workflow
  - `RELEASE_CHECKLIST.md`
- `.github/`:
  - `PULL_REQUEST_TEMPLATE.md` + 3 issue templates
  - `labels.yml`, `release-drafter.yml`
  - Workflows: `unity-test.yml` (matrix 2022 + Unity 6), `lint.yml`, `docs-coverage.yml`, `release-drafter.yml`, `release.yml`, `benchmark.yml` (Phase 2)
- `.editorconfig` augmented with C# style + analyzer severities
- `.gitattributes` — `*.meta merge=union`
- `CHANGELOG.md` at root
- `PublicAPI.{Shipped,Unshipped}.txt` skeleton × 8 Runtime asmdef (Audio, Data, Foundation, Inspector, Pool, Prefs, Timer, UI)
- Tooling: `scripts/check-xmldoc-coverage.py` + initial baseline JSON

### Phase 1 — Inventory & Characterization (1 commit, ~12 files)

Measurement layer; nothing changes runtime.

- `docs/api-inventory.csv` — 503 public symbols across RCore + RevCore
- `docs/migration/rcore-to-revcore-api-map.{md,csv}` — 99 PORTED / 6 RENAMED / 16 LIKELY / 250 GAP
- `docs/gap-analysis.md` — 250 RCore-only types grouped by module, empty Decision column for maintainer
- 16 characterization tests across 4 files pinning current behavior:
  - `Characterization_ColorHelperTests` — pins silent-fail of `HexToColor` on invalid input
  - `Characterization_EventBusTests` — pins `Publish` with 0 listeners is no-op, dedup of Subscribe, ListenerCount semantics
  - `Characterization_RevPoolTests` — pins over-capacity eviction picks `m_activeList[0]`, `LimitNumber == 0` means no cap, `ActiveItems` is live view
  - `Characterization_TimerSchedulerTests` — pins `Cancel(int)` on empty is no-op, **`Cancel(0)` cancels every default-id timer** (sharp edge), `Cancel(null)` tolerated
- Tooling: `scripts/extract-api-surface.py`, `scripts/build-migration-map.py`

### Phase 2 — Test Infrastructure (1 commit, ~13 files)

CI gates beyond compile.

- **`Packages/manifest.json`** modified:
  - Added `"com.unity.test-framework.performance": "3.0.3"`
  - Added `"com.unity.testtools.codecoverage": "1.2.6"`
- 8 benchmark tests across 3 files (all `[Category("Performance")]`):
  - `Benchmark_EventBusTests` — Publish 100 listeners × 10k events, ListenerCount lookup cost, Subscribe/Unsubscribe pair
  - `Benchmark_RevPoolTests` — Spawn+Release 1000, over-cap 2000 eviction cycles
  - `Benchmark_TimerSchedulerTests` — Cancel-one-of-1000 (the Phase 4 win target), Tick 1000 × 1000 frames, Create 10k timers
- `.github/workflows/benchmark.yml` — runs Performance category on Unity 2022 LTS, compares against `scripts/benchmark-baseline.json` with 5% tolerance
- `scripts/check-benchmark-regression.py` — parses PerformanceTestResults.json, supports `--write-baseline`
- `scripts/benchmark-baseline.json` — empty placeholder; **needs first Unity run to seed**
- `docs/contributing/BENCHMARK_GUIDE.md`
- Updated 3 test asmdefs to reference `"Unity.PerformanceTesting"`:
  - `Assets/RevCore/Foundation/Tests/Runtime/RevCore.Foundation.Tests.asmdef`
  - `Assets/RevCore/Pool/Tests/Runtime/RevCore.Pool.Tests.asmdef`
  - `Assets/RevCore/Timer/Tests/Runtime/RevCore.Timer.Tests.asmdef`

### Phase 3 — Non-breaking Safety Fixes (7 commits)

Each fix is additive or behavior-preserving. Zero breaking changes. Public API additions all tracked in `PublicAPI.Unshipped.txt`.

| Commit | Module | Description |
|---|---|---|
| `feat(foundation): add ColorHelper.TryHexToColor` | Foundation | New `TryHexToColor(string, out Color) -> bool`. Old `HexToColor` unchanged (silent-fail). |
| `feat(foundation): add EventBus.ListenerCountFor<T>` | Foundation | New per-type counter on concrete `EventBus`. **Not** added to `IEventBus` to avoid breaking external implementers. |
| `fix(data): JObjectDBManager.SaveForced bypasses 200ms throttle on end-of-life events` | Data | New `SaveForced()` virtual. Internal `OnApplicationPause`/`OnApplicationQuit` now use it to prevent lost end-of-life writes. `Save(now: true)` unchanged. |
| `fix(audio): null-safe tweener Kill on volume setters` | Audio | `?.Kill()` at 3 sites (`SetMasterVolume`, `SetMusicVolume`, `SetSFXVolume`). Eliminates NRE on first-frame volume calls. |
| `fix(ui): scroll views handle totalItems<=0 without crashing` | UI | Added `<= 0` early-return guard at top of `Initialize` / `Init` in `OptimizedScrollView`, `OptimizedHorizontalScrollView`, `OptimizedVerticalScrollView`. Also removed LogError from horizontal `ScrollBarChanged` empty path. |
| `fix(data): deprecate JObjectDB.collections direct field access` | Data | `[Obsolete(error: false)]` Stage 1. Field becomes private in v1.0. Internal usage wrapped with `#pragma warning disable CS0618`. |
| `docs(phase-3): summary` | meta | Phase 3 summary doc. |

Two test files added for the new APIs:
- `Foundation/Tests/Runtime/TryHexToColorTests.cs`
- `Foundation/Tests/Runtime/EventBusListenerCountForTests.cs`

### Phase 4 — DEFERRED (next major chunk)

Performance optimizations. Each will be benchmarked before/after. Needs working Test Runner first.

Planned hot paths:
- `TimerScheduler.Cancel(int)`: O(n) linear scan → O(1) dictionary lookup. Expect ~3 orders of magnitude drop on `Cancel_one_id_among_1000_timers`.
- `EventBus.ListenerCount` aggregate: cache counter, drop the cross-type walk.
- `RevPool.RelocateInactive` / `RemoveNullInactiveItems`: lazy cleanup tied to spawn-failure paths.
- Inspector drawer reflection: compiled lambda cache for `ShowIfDrawer`, `AutoFillDrawer`.
- `BaseAudioManager.GetSFXSource`: O(n) loop → `Dictionary<int, int>` clip-playing count.
- `PoolsContainer.GetActiveList` / `GetAllItems`: zero-alloc `ForEachActive(Action<T>)` / `CopyActiveTo(List<T>)` replacements.

### Phase 5 — Documentation Pass (9 commits, 100% coverage)

969 / 969 public members documented. All 8 modules (Audio, Data, Foundation, Inspector, Pool, Prefs, Timer, UI) at 100%.

Doc style used:
- `<summary>` for everything; multi-sentence with `<remarks>` for non-obvious behavior.
- `<param>` for non-trivial params; `<returns>` for non-self-evident returns; `<exception>` for documented throws.
- `<inheritdoc/>` on interface implementations.
- `<see cref="..."/>` for cross-references.
- Sharp edges called out in `<remarks>` (the 200ms `Save` throttle, the `Cancel(0)` matches-all, DOTWEEN fallbacks).

Heuristic upgrades during this phase to `scripts/check-xmldoc-coverage.py`:
1. Method regex now matches generics (`Foo<T>(...)`) — caught members the original regex missed. Bumped true public count from 917 → 996.
2. Final fix: skip members inside `internal`/`private`/`protected` types (so `PushPanelEvent`, `RequestPanelEvent`, `TimerHandle`, `CountdownTimer`, `ConditionTimer`, `AnimRequest` etc. don't inflate the count). True consumer-facing API surface: **969 members**.

### Phases NOT done in this session

- Phase 4 (performance) — pending benchmark baseline + Unity verification.
- Phase 6 (breaking API cleanup → v1.0) — pending. Includes typo renames (`Ded2Rad`, `Tad2Deg`, `CovertAnchored*`, `CreatDimmer*`), `Result<T>.Value` deprecation, `JObjectDB.collections` privatization, Roslyn codemods.
- Phase 7 (advanced features) — pending. Encrypted PlayerPrefs (opt-in), `JObjectData` schema versioning, `IRevDiagnostics` observability, zero-alloc query APIs.
- Phase 8 (migration tooling) — pending. RCore→RevCore migration assistant, pilot migration of 1 of 4 consumer projects.
- Phase 9 (v1.0 release) — pending.

---

## 3. ⚠️ CURRENT BLOCKER

**User opened Unity Editor; Test Runner (EditMode) shows ZERO RevCore tests.** Only a stub from `Unity.Addressables.Editor.Tests.DocExampleCode.dll` is visible. Not even the 21 pre-existing test classes from before this session appear.

Screenshot the user shared: only `RequiredTest` from Addressables visible. Everything else missing.

### Most likely cause (90% probability)

Compile error somewhere is blocking the entire test assembly graph from loading. Top suspects:

1. **`com.unity.test-framework.performance` version `3.0.3` doesn't resolve in user's Unity.** The version was my guess. If Unity can't fetch it, the asmdefs referencing `Unity.PerformanceTesting` (Foundation.Tests, Pool.Tests, Timer.Tests) fail to compile, which cascades. → Check `Library/PackageCache/` for the package, and check Unity Console for `Could not resolve` errors.
2. **`com.unity.testtools.codecoverage` 1.2.6 version mismatch** (same reasoning, less likely since it doesn't affect compile).
3. **Branch wasn't actually fully pulled** — verify with `git log --oneline -5`.
4. **A `.cs` edit I made has a syntax/typo error** — would show as compile error in Console.

### Diagnostic plan (do this in new session, in order)

```bash
# In E:\Projects\_\RCore
git status
git branch --show-current
git log --oneline -10
```

Verify branch is `claude/analyze-revcore-modules-LHYH7` and latest commit is the handoff one.

Then in Unity:

1. **Window → General → Console** → click red error filter. Read every red error. The first one is usually the root cause.
2. **Window → Package Manager → In Project** dropdown. Look for "Performance testing API" and "Code Coverage". If missing, the package version in `manifest.json` failed to resolve.
3. Check `Library/PackageCache/` on disk for `com.unity.test-framework.performance@*` and `com.unity.testtools.codecoverage@*` directories.
4. If package is wrong version: edit `Packages/manifest.json` to use a version Unity accepts (likely `2.8.x` for older Unity, `3.0.x` for newer; check <https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.0/manual/index.html> for compatibility). Reload Unity.

### Quick fallback if performance package is unavailable

Edit the 3 test asmdef files and REMOVE the `"Unity.PerformanceTesting"` reference:
- `Assets/RevCore/Foundation/Tests/Runtime/RevCore.Foundation.Tests.asmdef`
- `Assets/RevCore/Pool/Tests/Runtime/RevCore.Pool.Tests.asmdef`
- `Assets/RevCore/Timer/Tests/Runtime/RevCore.Timer.Tests.asmdef`

Then delete (or move out of `Assets/`) the 3 `Benchmark_*.cs` files. Phase 2's benchmark suite goes on hold until package version is resolved, but Phase 1's characterization tests and Phase 3's new fix tests will run.

### After Test Runner shows tests

1. Run all EditMode tests. Confirm green.
   - Especially watch Characterization_TimerSchedulerTests — if it fails, my understanding of `Cancel(0)` semantics was wrong and we need to update the pin. That's VALUABLE not bad.
2. Run `[Category("Performance")]` tests. Wait for completion.
3. Take `Library/PerformanceTestResults.json` and run:
   ```bash
   python scripts/check-benchmark-regression.py --results <dir-with-the-json> --baseline scripts/benchmark-baseline.json --write-baseline
   ```
4. Commit `scripts/benchmark-baseline.json`.
5. From this point, Phase 4 (performance) is unblocked.

---

## 4. Pending external setup (not blockers, but enables more CI)

> Updated mid-Phase: the maintainer does not hold a Unity license and won't be acquiring one, so the Unity-side CI workflows (`unity-test.yml`, `benchmark.yml`) have been removed entirely. Tests and benchmarks run locally before push. The remaining four workflows (lint, docs-coverage, release-drafter, release) are pure-text and need no secrets.

### 4.1 PublicAPI Roslyn analyzer activation

The analyzer DLLs are committed under `Assets/RevCore/_Analyzers/` and the `csc.rsp` files are wired per Runtime asmdef. The `RoslynAnalyzer` label is intentionally absent because the Shipped surface is empty; activating now would block compile with ~1,000 RS0016 warnings. Activation is scheduled as the one-time v1.0 release task in `docs/contributing/RELEASE_CHECKLIST.md`.

Details in `docs/contributing/PUBLIC_API_GUIDE.md`.

---

## 5. Conventions in this codebase (read before editing)

### Code style
- Private instance fields: `m_camelCase`
- Private static fields: `s_camelCase`
- Public anything: `PascalCase`
- Test method names: `snake_case_descriptive`
- `.cs` files use tabs for indentation, CRLF line endings
- Other files use spaces, LF
- `.gitattributes` enforces this; expect CRLF/LF warnings from git when editing `.cs` on Linux (harmless)

### Test classes
- One test class per production class: `FooTests` (existing pattern)
- Pinning tests prefixed: `Characterization_FooTests`
- Benchmarks prefixed + categorized: `Benchmark_FooTests` with `[Category("Performance")]`
- Each test asmdef has `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`
- New tests for new APIs go in a separate test file named after the API: `TryHexToColorTests.cs`, `EventBusListenerCountForTests.cs`

### Public API tracking
- Adding a public member: must add a line to that module's `PublicAPI.Unshipped.txt`.
- Removing/renaming a public member: add `*REMOVED*` line in Unshipped (post-Stage-3 only).
- Pre-existing public surface is in `PublicAPI.Shipped.txt` (currently empty — will be populated at v0.5 or first release).

### Deprecation (3-stage)
1. `[Obsolete("Use X instead. Will be removed in v<future>.", error: false)]` — ≥ 1 MINOR
2. `[Obsolete("Use X instead. Will be removed in v<future>.", error: true)]` — ≥ 1 MINOR
3. Actually delete

`JObjectDB.collections` is at Stage 1. `Result<T>.Value` is a candidate for Stage 1 in Phase 6.

### Semver (pre-1.0)
- MINOR may break with CHANGELOG note
- PATCH never breaks

### Key design decisions logged
- `IEventBus.ListenerCountFor<T>` was NOT added to interface — only the concrete `EventBus` — to avoid breaking external implementers. Consumers cast to `EventBus` for the optimized path.
- `JObjectDB.collections` deprecated (Stage 1), not removed — direct mutation bypasses key persistence, but removing it would break v0.x consumers.
- `HexToColor` keeps its silent-fail behavior for back-compat. `TryHexToColor` is the new explicit-failure variant.
- `Ded2Rad` / `Tad2Deg` typos kept, documented as "will be removed in v1.0". Not yet [Obsolete]-marked because they're targeted for Phase 6 alongside other breaking renames.
- "Covert" spelling in `CovertAnchoredPosFromChildToParent` documented as legacy typo. Phase 6 will alias to correct `Convert*`.

---

## 6. Tooling cheat sheet for new session

All scripts in `scripts/`. Run from repo root.

```bash
# XML doc coverage gate (CI uses this)
python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json

# Regenerate API inventory after adding public members
python scripts/extract-api-surface.py --out docs/api-inventory.csv

# Regenerate migration map (RCore → RevCore)
python scripts/build-migration-map.py

# Compare benchmark results against baseline (CI uses this)
python scripts/check-benchmark-regression.py --results <dir-with-PerformanceTestResults.json> --baseline scripts/benchmark-baseline.json

# Seed benchmark baseline (release time, or first local run)
python scripts/check-benchmark-regression.py --results <dir> --baseline scripts/benchmark-baseline.json --write-baseline

# Regenerate xml-doc baseline (only if heuristic is updated)
python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json --write-baseline
```

On Windows the user may need `python` instead of `python3`.

---

## 7. Files the new session should know about

### Documentation
- `docs/PHASE1_SUMMARY.md` through `docs/PHASE5_SUMMARY.md` — each phase's deliverables
- `docs/contributing/*.md` — governance docs
- `docs/api-inventory.csv` — 503 public symbols
- `docs/migration/rcore-to-revcore-api-map.{md,csv}` — RCore → RevCore mapping
- `docs/gap-analysis.md` — 250 RCore-only types (Decision column empty, awaits maintainer)
- `CHANGELOG.md` — root, Keep-a-Changelog format with `[Unreleased]` section
- `docs/ARCHITECTURE.md` — pre-existing, project architecture overview

### Code surface
- Per module: `Assets/RevCore/<Module>/Runtime/PublicAPI.{Shipped,Unshipped}.txt`
- Per module: `Assets/RevCore/<Module>/Tests/Runtime/Characterization_*.cs`, `Benchmark_*.cs`
- Per module: regular `*Tests.cs` for unit tests

### Tooling
- `scripts/check-xmldoc-coverage.py` — XML doc gate
- `scripts/extract-api-surface.py` — inventory
- `scripts/build-migration-map.py` — migration map
- `scripts/check-benchmark-regression.py` — benchmark gate
- `scripts/xmldoc-baseline.json` — currently empty (`{}`) = no regression tolerance
- `scripts/benchmark-baseline.json` — currently empty, needs Unity seed

### Config
- `Packages/manifest.json` — has the two added test packages
- `.editorconfig` — C# style + analyzer severities
- `.gitattributes` — `*.meta merge=union`
- `.github/workflows/*.yml` — 6 workflows

---

## 8. Suggested first action sequence in new session

1. Read this doc. Then `docs/PHASE5_SUMMARY.md`. Then `docs/contributing/README.md`. (3 files, ~10 min reading.)
2. `git status`, `git log --oneline -10` — verify branch state.
3. Open Unity Console. Read errors. Paste back to user OR fix directly.
4. If `com.unity.test-framework.performance` is the issue:
   - Try alternate versions (`2.8.1-preview`, `2.8.0-preview`, `3.0.2`, `3.1.0`)
   - Or remove from manifest + remove `Unity.PerformanceTesting` reference from 3 test asmdef + delete `Benchmark_*.cs` files; pin Phase 2 work for later
5. Re-open Test Runner. Confirm RevCore tests appear.
6. Run all EditMode tests. If green → fantastic. If any red, that's diagnostic gold — share with user.
7. Run Performance category. Seed `scripts/benchmark-baseline.json`. Commit.
8. Ask user: "Phase 4 (performance) next? Or different priority?"

---

## 9. Things the user explicitly asked for / didn't want

- Wants high quality bar, no rush.
- Wants framework usable for 5+ teams.
- Did NOT want SimpleButton (deprecated) — successor is SimpleTMPButton.
- Did NOT want CustomToggleSlider — successor is JustToggle.
- Communicates in Vietnamese sometimes. Reply in whatever language they use.
- Confirmed: "tỉ mỉ, không được phạm sai sót" = careful, no mistakes allowed.

---

## 10. Open questions the new session might encounter

- What Unity version does the user have installed? Tests will tell us once Console shows the version.
- Do the 4+ RCore consumer projects have CI? If yes, Phase 8 migration tooling should integrate.
- Should the `_Analyzers` folder be under `Assets/RevCore/` (shared) or under each module? Currently planned as shared.
- Is there a preferred Unity test license tier for CI (Personal seat vs Pro)?
- After Phase 6 ships v1.0, what's the RCore deprecation timeline?

These don't block; they're for next conversation.

---

**End of handoff. Good luck.**
