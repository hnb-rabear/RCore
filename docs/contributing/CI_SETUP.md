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

## PublicAPI Roslyn analyzer (one-time at v1.0)

The analyzer DLLs are committed under `Assets/RevCore/_Analyzers/` and each Runtime asmdef has a `csc.rsp` that wires the per-module `PublicAPI.*.txt` files into the compile. The `RoslynAnalyzer` asset label is intentionally absent so the analyzer is dormant — activating now would block compile with ~1,000 `RS0016` warnings against an empty Shipped surface.

Activation is the one-time v1.0 release task documented in `docs/contributing/RELEASE_CHECKLIST.md`. Until then the `PublicAPI.*.txt` files are a manually-curated record the maintainer scans in PR diffs by eye.
