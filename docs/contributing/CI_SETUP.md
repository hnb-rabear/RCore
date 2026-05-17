# CI setup

The repo runs four CI workflows out of the box (no setup needed):

- `lint.yml` — EditorConfig + YAML + Markdown lint on every PR.
- `docs-coverage.yml` — runs `scripts/check-xmldoc-coverage.py` to enforce 100% public XML doc coverage.
- `release-drafter.yml` — auto-updates the draft release notes when PR labels change.
- `release.yml` — publishes the GitHub Release when a `v*` tag is pushed.

These need no secrets and run on Ubuntu — pure-text checks, no Unity Editor involved. Unity-side test runs and performance benchmarks are intentionally **not** wired to CI; the framework is solo-maintained and the maintainer runs them locally before pushing.

## PublicAPI Roslyn analyzer (one-time at v1.0)

The `PublicAPI.{Shipped,Unshipped}.txt` files under each Runtime asmdef are currently review-only: the analyzer DLLs are committed under `Assets/RevCore/_Analyzers/` but the `RoslynAnalyzer` asset label is intentionally absent so the analyzer stays dormant.

Activating now would block compile with ~1,000 `RS0016` warnings ("Symbol 'X' is not part of the declared API") because the Shipped surface is empty. Seeding is scheduled as a one-time v1.0 release task; the full flip-live procedure lives in `docs/contributing/RELEASE_CHECKLIST.md` under "One-time at v1.0: seed the baseline".

Until then, the `.txt` files are a manually-curated record — the maintainer scans them in PR diffs by eye.
