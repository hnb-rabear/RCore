## Benchmark Guide

The benchmark suite tracks the cost of performance-critical RevCore APIs across releases.

### Where benchmarks live

Benchmark tests are co-located with regular tests under `Assets/RevCore/<Module>/Tests/Runtime/Benchmark_*.cs`. They:

- Use `Unity.PerformanceTesting` (`Measure.Method(...)...Run()`).
- Are tagged with `[Category("Performance")]`.
- Sit in the same NUnit asmdef as the regular tests, so they share fixtures.

### Running locally

Open Test Runner in Unity, switch to **EditMode**, filter by category `Performance`, hit Run. Results are written to `PerformanceTestResults.{xml,json}` under the project's Library folder.

### CI

`.github/workflows/benchmark.yml` runs the suite on Unity 2022 LTS on every PR. Results are uploaded as the `BenchmarkResults` artifact. The script `scripts/check-benchmark-regression.py` then compares each test's **Median** sample against `scripts/benchmark-baseline.json` and fails the job if any test exceeds the baseline by more than 5%.

### Updating the baseline

You refresh the baseline once per release, on `main`, after CI has reported the new numbers:

```bash
# Download the BenchmarkResults artifact from the latest CI run into ./_bench,
# then:
python3 scripts/check-benchmark-regression.py \
  --results _bench \
  --baseline scripts/benchmark-baseline.json \
  --write-baseline
git add scripts/benchmark-baseline.json
git commit -m "chore(benchmark): refresh baseline for vX.Y.Z"
```

Never edit the baseline by hand. Never refresh inside a perf-improvement PR — the PR's job is to **show** the improvement; the next release commits it.

### Adding a new benchmark

1. Pick a hot path with a clear "win number" you can defend (allocation count, ns per call, frame budget).
2. Add a `Benchmark_*Tests.cs` next to the unit tests for that module.
3. Tag the test class with `[Category("Performance")]`.
4. Open the file in Test Runner once locally to verify it runs; record the numbers in the PR description.
5. The first CI run after merge will populate the baseline entry — the gate is "no regression", so a brand-new test passes the first time.

### Interpreting results

- `Time.ms` (or `Time` in ns depending on package version) is the median wall-clock cost of one Run iteration.
- Allocation counts come from `.GC()` — Phase 4 work targets zero alloc on hot paths.
- A regression marker (`REGRESS` in the comparison output) is a CI fail. To fix: either land the optimization first, or update the test if the regression reflects an intentional algorithmic trade-off (document in PR).
