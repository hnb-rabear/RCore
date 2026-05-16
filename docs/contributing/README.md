## Contributing to RevCore

RevCore is a Unity framework used in multiple production projects. Any change you make is a change in those projects' contracts. Treat every public API as a long-lived promise.

### Required reading before opening a PR

1. [API Design Guidelines](API_DESIGN_GUIDELINES.md) — naming, nullability, threading, error model.
2. [Semver Policy](SEMVER_POLICY.md) — when to bump MAJOR/MINOR/PATCH.
3. [Deprecation Policy](DEPRECATION_POLICY.md) — how to remove or rename a public API safely.
4. [Public API Guide](PUBLIC_API_GUIDE.md) — how `PublicAPI.Shipped.txt` works and what to update.
5. [Release Checklist](RELEASE_CHECKLIST.md) — the steps the maintainer follows on tag.

### Quick rules

- Public API changes require an entry in `PublicAPI.Unshipped.txt`. CI blocks otherwise.
- Public types and members require XML doc comments. CI blocks otherwise.
- New behavior requires a test. CI tracks coverage; drops are blocked.
- Performance-sensitive paths run under `Unity.PerformanceTesting`. Regressions over 5% are blocked.
- Breaking changes go through Deprecation Policy. No silent removals.
- Every PR is labelled `breaking`, `feature`, `fix`, `perf`, `docs`, `chore`, or `test`. Release Drafter uses these to generate the changelog.

### Branches

- `main` — development (currently pre-1.0; minor versions may break).
- `vX.Y-lts` — created on demand for backports.

### Issue templates

Use the bug/feature/question templates when filing issues. They pre-fill the data needed for triage.
