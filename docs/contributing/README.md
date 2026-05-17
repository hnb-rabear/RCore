## Contributing to RevCore

RevCore is a Unity framework used in multiple production projects. Any change you make is a change in those projects' contracts. Treat every public API as a long-lived promise.

### Required reading before opening a PR

1. [API Design Guidelines](API_DESIGN_GUIDELINES.md) — naming, nullability, threading, error model.
2. [Semver Policy](SEMVER_POLICY.md) — when to bump MAJOR/MINOR/PATCH.
3. [Deprecation Policy](DEPRECATION_POLICY.md) — how to remove or rename a public API safely.
4. [Public API Guide](PUBLIC_API_GUIDE.md) — how `PublicAPI.Shipped.txt` works and what to update.
5. [Release Checklist](RELEASE_CHECKLIST.md) — the steps the maintainer follows on tag.

CI is intentionally minimal — see [CI Setup](CI_SETUP.md). The only workflow is `release.yml`, which fires on `v*` tag pushes to publish the GitHub Release. Every quality gate runs locally before push.

### Quick rules

- Public API changes require an entry in `PublicAPI.Unshipped.txt`. Review-only until v1.0; check the diff before pushing.
- Public types and members require XML doc comments. Run `python scripts/check-xmldoc-coverage.py` locally to verify the 100% baseline still holds.
- New behavior requires a test under the relevant module's `Tests/Runtime/` folder. Run Unity Test Runner → EditMode locally and confirm all 151 tests are green before pushing.
- Performance-sensitive paths run under `Unity.PerformanceTesting`. Run the Performance category locally and feed `Library/PerformanceTestResults.json` to `scripts/check-benchmark-regression.py` to verify the 5% tolerance.
- Breaking changes go through Deprecation Policy. No silent removals.

### Branches

- `main` — development (currently pre-1.0; minor versions may break).
- `vX.Y-lts` — created on demand for backports.

### Issue templates

Use the bug/feature/question templates when filing issues. They pre-fill the data needed for triage.
