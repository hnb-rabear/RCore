# CI setup

The repo has exactly one CI workflow:

- `.github/workflows/release.yml` — fires on `v*` tag push, publishes the GitHub Release with the package artifacts.

PR-time and push-to-main automation was removed deliberately. The framework is solo-maintained and the maintainer runs every gate locally before pushing:

| Check | How to run locally |
| --- | --- |
| Tests (151 EditMode tests, ~25s) | Unity Editor → Test Runner → EditMode → Run All |
| Benchmark regressions | Unity Editor → Test Runner → Performance category → Run, then `python scripts/check-benchmark-regression.py --results Library/ --baseline scripts/benchmark-baseline.json` |
| XML doc coverage (Phase 5 gate at 100%) | `python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json` |
| EditorConfig compliance | Any editor with EditorConfig plugin handles this on save |
| Public API drift | Open the diff before committing; new public members need a line in `PublicAPI.Unshipped.txt` |

## PublicAPI Roslyn analyzer (dormant post-v1.0)

The analyzer DLLs are committed under `Assets/RevCore/_Analyzers/` and each Runtime asmdef has a `csc.rsp` that wires the per-module `PublicAPI.*.txt` files into the compile. The `RoslynAnalyzer` asset label is intentionally absent — the analyzer is dormant.

The v1.0 cut (2026-05-17) seeded the full public surface into per-module `PublicAPI.Shipped.txt` (1337 entries) by temporarily setting the label, running Rider's "Add to public API" code fix solution-wide, then promoting `Unshipped` → `Shipped` via `scripts/seal-public-api.py`. The label was reverted after seeding. Live enforcement was not retained because Unity loads any labelled analyzer project-wide, which fires `RS0016` on legacy `Assets/RCore.*` folders that deliberately track no PublicAPI surface — noise the maintainer chooses not to resolve since RCore is frozen.

The `.txt` files now serve as a committed paper trail the maintainer scans in PR diffs by eye. Reactivation workflow (temporary or permanent if Unity gains asmdef-level analyzer scoping) is documented in [PUBLIC_API_GUIDE](PUBLIC_API_GUIDE.md) "Analyzer wiring".
