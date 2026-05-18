# RCore → RevCore Migration

This folder is the consumer-facing reference for migrating a Unity project from the legacy `RCore` framework (`Assets/RCore/`) to the next-generation `RevCore` framework (`Assets/RevCore/`).

**Status**: planning only. No code rewrites have been performed on consumer projects yet. RCore remains in production across all in-flight consumer projects and is supported indefinitely — there is no deadline forcing a migration.

## When to migrate

Migrate **only** when one of these is true:

- You are starting a new project and want the RevCore feature set from day one.
- You need a hardened API contract (semver-stable surface, XML doc coverage, deprecation policy, performance benchmarks). RevCore has these; RCore does not.
- An RCore feature you depend on has been deprecated and the deprecation window is closing.

Do **not** migrate an existing shipping project on a whim. RCore and RevCore can both live in the same project (different namespaces, no compile collisions). A staged migration is always safer than a flag-day rewrite.

## Documents

| Doc | Purpose |
|---|---|
| [`PLAN.md`](PLAN.md) | The full step-by-step migration playbook. Read this before touching any code. |
| [`gap-categories.md`](gap-categories.md) | The 250 RCore types with no RevCore equivalent, grouped into ~14 buckets with a recommended action per bucket. |
| [`rcore-to-revcore-api-map.md`](rcore-to-revcore-api-map.md) | Auto-generated type-by-type rename map. PORTED (97) and RENAMED (15) are mechanical; LIKELY (0 after audit) are gone; GAP (259 after audit) point at `gap-categories.md`. |
| [`rcore-to-revcore-api-map.csv`](rcore-to-revcore-api-map.csv) | Same data as the .md, machine-readable. The source any future migration tool reads. |
| [`../gap-analysis.md`](../gap-analysis.md) | Per-type decision template (PORT / DROP / REPLACE / DEFER). Fills in over time; see `gap-categories.md` for the bulk rationale. |

## Tooling status

No automated migration tool ships today. The deliberate choice: every consumer codebase is different, and a half-correct auto-rewrite is worse than a careful manual pass guided by the docs above. When the maintainer or a consumer project decides to invest in a script, the inputs are already ready:

- `rcore-to-revcore-api-map.csv` is the rewrite table (status PORTED + RENAMED ⇒ mechanical find-replace).
- `scripts/build-migration-map.py` re-generates the map from `docs/api-inventory.csv`.
- `gap-categories.md` rules tell the script which GAP cases to skip vs. flag vs. fail.

The shape of a future tool is sketched in [`PLAN.md`](PLAN.md) §6.

## Phase status

This document set was produced in **Phase 8** of the RevCore hardening plan — documentation only, no migration tooling.

**Plan complete.** Phase 9 (v1.0 cut) shipped as `v1.0.0` on 2026-05-17: Stage 1 deprecations cleared, package versions bumped to 1.0.0, full RevCore public surface committed to per-module `PublicAPI.Shipped.txt`. RevCore is now a stable framework. Migration tooling remains intentionally absent — consumer projects continue to run on RCore and there is no concrete demand for auto-rewrites yet. When a consumer initiates migration, the inputs documented here (`rcore-to-revcore-api-map.csv`, `gap-categories.md`) become the source-of-truth for any tooling built then.
