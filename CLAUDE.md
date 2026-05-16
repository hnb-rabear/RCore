# Project context for Claude Code

## What this repo is

Unity project housing **two frameworks** side by side:
- `Assets/RCore/` — legacy framework in production across 4+ consumer projects
- `Assets/RevCore/` — next-generation rewrite, pre-1.0, being hardened to ship to ≥5 teams

The active work is on **RevCore**. The user is a solo maintainer.

## Working branch

`claude/analyze-revcore-modules-LHYH7`

This branch carries Phase 0-5 of the RevCore hardening plan (governance, inventory, test infrastructure, non-breaking fixes, 100% XML doc coverage). Phases 4, 6, 7, 8, 9 are pending.

## Start here

**Always read `docs/SESSION_HANDOFF.md` before doing anything else.** It captures the full state of work from the previous (web-based) Claude Code session that produced this branch. Especially section 3 — there is an unresolved Unity Test Runner blocker that should be addressed first.

After the handoff doc, useful reading:
- `docs/PHASE5_SUMMARY.md` — most recent phase, complete with coverage stats
- `docs/contributing/README.md` — governance docs (semver, deprecation, public API, release checklist)
- `CHANGELOG.md` — keep-a-changelog format with `[Unreleased]` section open

## Critical conventions

- Private instance fields: `m_camelCase`. Private static: `s_camelCase`. Public: `PascalCase`.
- `.cs` files use **tabs** and **CRLF** (enforced via `.gitattributes`). All other files use spaces and LF.
- Test methods: `snake_case_descriptive`. Test classes named after production class.
- Adding a public member requires adding a line to that module's `PublicAPI.Unshipped.txt`.
- Every public member must have XML doc (`/// <summary>`). Coverage gate enforces this in CI.
- Deprecate, don't delete (3-stage per `docs/contributing/DEPRECATION_POLICY.md`).
- Each fix or feature commits as its own PR-sized change; CHANGELOG entry per commit.

## Tooling cheat sheet

```powershell
# Doc coverage gate (must stay at 0 regressions)
python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json

# Refresh API inventory after adding public members
python scripts/extract-api-surface.py --out docs/api-inventory.csv

# Benchmark regression check (after a Unity Performance run)
python scripts/check-benchmark-regression.py --results <dir-with-PerformanceTestResults.json> --baseline scripts/benchmark-baseline.json
```

## What is NOT this repo

- This is not a fresh project — RCore consumers depend on existing APIs.
- This is not a hobby project — quality bar is "shippable to 5+ teams".
- The user explicitly said "không gấp, tỉ mỉ, không được phạm sai sót" — careful, no rush, no mistakes.
