## Summary

<!-- 1–3 sentences. What changes, and why. -->

## Type of change

<!-- Pick ONE label below and apply the matching GitHub label. Release Drafter uses it for the changelog. -->

- [ ] `breaking` — public API removed, renamed, or behavior change consumers may depend on
- [ ] `feature` — additive new functionality
- [ ] `fix` — bug fix, no API change
- [ ] `perf` — performance improvement, no API change
- [ ] `docs` — documentation only
- [ ] `chore` — build, CI, internal refactor
- [ ] `test` — tests only

## Checklist

- [ ] Tests added or updated (or N/A — explain why).
- [ ] Public API changes reflected in `PublicAPI.Unshipped.txt`.
- [ ] XML doc on all new/changed public members.
- [ ] CHANGELOG entry added under `## [Unreleased]`.
- [ ] If `breaking`: migration guidance included below.
- [ ] If `perf`: benchmark numbers included below.
- [ ] Manually verified on Unity 2022 LTS (or N/A — explain).
- [ ] Manually verified on Unity 6 (or N/A — explain).

## Migration guidance (required if `breaking`)

<!--
Before:
    var c = ColorHelper.HexToColor("#ff0000");
After:
    ColorHelper.TryHexToColor("#ff0000", out var c);
-->

## Benchmark numbers (required if `perf`)

<!-- Paste `Unity.PerformanceTesting` output. Show before/after delta. -->

## Risk assessment

<!-- What could break? Which projects (of the 4+ consumers) are most exposed? -->
