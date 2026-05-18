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

- Public API changes require an entry in `PublicAPI.Unshipped.txt`. The analyzer is dormant (see [CI_SETUP](CI_SETUP.md) and [PUBLIC_API_GUIDE](PUBLIC_API_GUIDE.md)), so this is enforced by review, not by compile. At release-cut, `scripts/seal-public-api.py` promotes `Unshipped` → `Shipped`.
- Public types and members require XML doc comments. Run `python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json` locally to verify the 100% baseline (956 members as of v1.0.0) still holds.
- New behavior requires a test under the relevant module's `Tests/Runtime/` folder. Run Unity Test Runner → EditMode locally and confirm all 160 tests are green before pushing.
- Performance-sensitive paths run under `Unity.PerformanceTesting`. Run the Performance category locally and feed `Library/PerformanceTestResults.json` to `scripts/check-benchmark-regression.py` to verify the 5% tolerance.
- Breaking changes go through Deprecation Policy. No silent removals. From v1.0.0 onward, public API breaks require a major version bump.

### Branches

- `main` — development. Post-v1.0.0: breaking changes require a major bump per [SEMVER_POLICY](SEMVER_POLICY.md).
- `vX.Y-lts` — created on demand for backports.

### Issue templates

Use the bug/feature/question templates when filing issues. They pre-fill the data needed for triage.
