# Session Handoff — Post v1.0.0 cut (2026-05-17)

Written at the end of the local Claude Code session that completed Phases 6.3 → 9 of the RevCore hardening plan and tagged `v1.0.0`. Read this first in any new session.

> **First action for the new session**: skim sections 0–3 of this document, then `CHANGELOG.md` `[1.0.0]` entry, then this session's memory under `C:\Users\Adminn\.claude\projects\e--Projects---RCore\memory\MEMORY.md`.

---

## 0. Project context (one paragraph)

The repo houses two Unity frameworks side by side: **RCore** (legacy, `Assets/RCore/`, in production across 4+ consumer projects, frozen and untouched) and **RevCore** (`Assets/RevCore/`, the next-generation rewrite, now at **v1.0.0**). Solo maintainer. Distribution is UPM git URL with semver tags. The maintainer's working language is Vietnamese + English mix and the working style is "tỉ mỉ, không gấp, không được phạm sai sót" — careful, no rush, high quality bar.

The 10-phase hardening plan (Phases 0–9) is **complete**. Future work is incremental on top of v1.0.

---

## 1. What shipped in v1.0.0

Released as git tag `v1.0.0`, GitHub Release published via `release.yml`. Same-day cut as v0.5.0 — the v0.5.0 release was the pre-1.0 baseline, v1.0.0 is the cleanup.

**Stage cleanup (removed):**

- `MathHelper.Ded2Rad`, `MathHelper.Tad2Deg` (typos) → use `Deg2Rad` / `Rad2Deg`.
- `TransformHelper.CovertAnchoredPosFromChildToParent` (both overloads, typo) → use `ConvertAnchoredPosFromChildToParent`.
- `PoolsContainer<T>.GetActiveList()`, `GetAllItems()` (allocating) → use `ForEachActive` / `ForEachItem` / `CopyActiveTo` / `CopyAllTo`.
- `JObjectDB.collections` (public obsolete property) deleted; `s_collections` stays private. Use the accessor methods.
- `Result<T>.Value` moved from public `[Obsolete]` to `internal`. Public read API is `TryGetValue` / `ValueOr`. Tests access via `[InternalsVisibleTo("RevCore.Foundation.Tests")]` added to `Result.cs`.

**Public surface committed:** 1337 entries across the 8 runtime modules populated into per-module `PublicAPI.Shipped.txt` (Audio 74, Data 139, Foundation 391, Inspector 40, Pool 85, Prefs 42, Timer 70, UI 496). The `Unshipped.txt` files are header-only and become the staging lane for v1.0.1+ additions.

**Version:** all nine `package.json` files bumped from `0.5.0` to `1.0.0`.

**Tests / coverage:** 160 EditMode tests pass. XML doc coverage 956 / 956 (100%). Delta -7 versus v0.5.0 matches the deleted symbols exactly.

---

## 2. Analyzer status — important

`Microsoft.CodeAnalysis.PublicApiAnalyzers` ships in `Assets/RevCore/_Analyzers/` but is **dormant**: the `RoslynAnalyzer` asset label is NOT set on either DLL. Unity loads any labelled analyzer project-wide; that would fire `RS0016` on legacy `Assets/RCore.Archives/` and `Assets/RCore.LXLite/` and `Assets/RCore.SheetX/` — none carry asmdefs, none track PublicAPI, the maintainer explicitly does not touch them. Editorconfig-based scoping (path globs, nested `.editorconfig`) does not suppress these in practice. Until Unity gains asmdef-level analyzer scoping, the analyzer cannot be live without polluting RCore compile.

The `Shipped.txt` files still serve as a committed paper trail for PR review. Reactivation procedure (for an audit pass) is documented in `docs/contributing/PUBLIC_API_GUIDE.md` "Analyzer wiring".

`scripts/seal-public-api.py` is the idempotent helper that promotes `Unshipped` → `Shipped` at release-cut time. Used by `RELEASE_CHECKLIST` §3.

---

## 3. Branch + git state

- **Working branch at session end**: `feat/timer-audio-unitask-v1.1`.
- **Base**: `origin/main` at commit `f394ff0` (PR #13 docs/post-v1.0 updates merged).
- **Latest commit**: `cc800e3 feat(timer,audio): UniTask async API (v1.1.0)`.
- **Pushed branch**: `origin/feat/timer-audio-unitask-v1.1`.
- **PR status**: branch pushed, but PR creation failed because `gh` is not authenticated (`gh auth login` needed). Create PR manually from `feat/timer-audio-unitask-v1.1` into `main`.
- **Local caveat**: worktree still shows unrelated line-ending drift in `Assets/SheetXExample/Scripts/Generated/*`; those files were deliberately left unstaged and are not in commit `cc800e3`.

PR history this session:

| # | Branch | Subject |
|---|---|---|
| 8 | `spec/revcore-irevdiagnostics` | Phase 7.1 — IRevDiagnostics observability hooks |
| 9 | `docs/prefs-no-encryption-policy` | docs(prefs): document no-encryption policy |
| 10 | `perf/eventbus-zero-alloc-publish` | perf(eventbus): zero-alloc Publish via cached listener count |
| 11 | `docs/migration-plan-phase-8` | Phase 8 — consumer migration plan + GAP categorisation |
| 12 | `release/v1.0.0` | Phase 9 release cut |
| 13 | `docs/post-v1.0-updates` | Post-v1.0 doc updates |
| 14 (pending merge) | `feat/timer-audio-unitask-v1.1` | UniTask async API for Timer + Audio (v1.1.0) |

---

## 4. Current v1.1.0 PR-A state

Implemented UniTask async API for Timer + Audio per `docs/superpowers/plans/2026-05-17-revcore-unitask-timer-audio-implementation.md`.

Shipped in commit `cc800e3`:

- `RevCore.Timer`: `Timers.DelayAsync`, `Timers.WaitForConditionAsync`, `Timers.WaitForFramesAsync` in `Core/TimersAsync.cs`; `Timers` made `partial`.
- `RevCore.Audio`: `AudioAsyncExtensions.FadeMusicAsync` and `FadeOutMusicAsync`.
- Runtime/test asmdefs reference `UniTask` for Timer and Audio.
- Timer and Audio `package.json` bumped to `1.1.0` and declare `com.cysharp.unitask` `2.5.10`.
- PublicAPI.Unshipped entries added for all new public members.
- Root/module CHANGELOGs updated; Unity `.meta` files added for all new scripts.

Verification run:

```powershell
python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json
# Public members: 963; Documented: 963; Undocumented: 0; Coverage: 100.00%
```

Not run in shell: Unity compile and Unity Test Runner EditMode. PR body should keep these as manual checklist items. Expected test delta: +18 tests (12 TimerAsyncTests + 6 AudioAsyncTests).

## 5. Future work (none gated, all opportunistic)

Original plan declined / deferred entries that may resurface as consumer demand:

- **Migration tooling.** The migration map (`docs/migration/rcore-to-revcore-api-map.csv`) is audited and ready as input. A Python or Roslyn-based rewrite tool can be built when a consumer initiates migration. See `docs/migration/PLAN.md` §9 sketch.
- **Schema versioning for `JObjectData`.** Declined in Phase 7.2 — the `JObjectModel<T>.OnPostLoad` hook covers the use case. Reopen if consumer reports cross-version save corruption.
- **Encrypted PlayerPrefs.** Declined permanently per Phase 7.3. The "no encryption" policy is documented in `Assets/RevCore/Prefs/README.md` and `memory/project_no_encryption.md`. Do not reopen.
- **PublicAPI analyzer live enforcement.** Awaits Unity asmdef-level analyzer scoping (see §2). Until then, paper-trail-only.
- **GAP type ports.** 259 GAP types in `docs/migration/gap-categories.md` default to DROP / DEFER / REPLACE; zero default to PORT. Specific PORTs happen on consumer-driven feedback.
- **Module-level CHANGELOG sync.** Per-module `Assets/RevCore/<Module>/CHANGELOG.md` files are inconsistent (mix of "1.0.0 - 2026-05-13" scaffold dates and unreleased sections). Could be brought into sync with framework versioning, but low priority — root `CHANGELOG.md` is the canonical source.
- **`Assets/RCore.Archives/` cosmetic diffs.** Two files (`BuildSettings.cs`, `BuilderWindow.cs`) had a Rider Code Cleanup pass run on them during the v1.0 cut. Excluded from PR #12 to respect the "do not touch RCore" mandate. User can `git checkout Assets/RCore.Archives/` to revert if they want.

No blockers. No outstanding bugs. The framework is at a clean v1.0 state.

---

## 6. Memory & conventions

Auto-memory is stored at `C:\Users\Adminn\.claude\projects\e--Projects---RCore\memory\MEMORY.md`. Key entries:

- `user_revcore_context.md` — solo maintainer hardening RevCore for ship to 5+ teams.
- `feedback_commit_granularity.md` — split commits by scope, name red→green tests in body.
- `project_phase_status.md` — UPDATED this session: now reflects v1.0.0 complete.
- `project_no_encryption.md` — RevCore ships no value encryption. Do not propose re-adding.

Critical conventions reminder (also in `CLAUDE.md`):

- Private instance fields: `m_camelCase`. Static: `s_camelCase`.
- `.cs` files use tabs + CRLF. Other files spaces + LF.
- Test methods: `snake_case_descriptive`.
- Adding public surface: line in module `PublicAPI.Unshipped.txt`, promoted via `seal-public-api.py` at release.
- Every public member: XML `/// <summary>`. Coverage gate at 100%.
