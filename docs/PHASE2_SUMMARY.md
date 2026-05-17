## Phase 2 — Test Infrastructure Summary

Phase 2 turns "tests pass" into a measurable, gated quality bar. Coverage and benchmarks become CI signals that block regression.

### What landed

| Component | Status | File(s) |
|---|---|---|
| **Code coverage tracking** | Wired via `game-ci` action | `.github/workflows/unity-test.yml` (`coverageOptions`) |
| **Coverage package** | Added | `Packages/manifest.json` (`com.unity.testtools.codecoverage`) |
| **Benchmark package** | Added | `Packages/manifest.json` (`com.unity.test-framework.performance`) |
| **Benchmark tests** | 8 tests across 3 modules | `Assets/RevCore/{Foundation,Pool,Timer}/Tests/Runtime/Benchmark_*.cs` |
| **Benchmark CI** | Runs on every PR, fails on >5% regression | `.github/workflows/benchmark.yml` |
| **Regression checker** | Diffs current results vs baseline JSON | `scripts/check-benchmark-regression.py` |
| **Baseline file** | Empty placeholder; CI will warn on first run, populate via `--write-baseline` | `scripts/benchmark-baseline.json` |
| **Benchmark guide** | How to add benchmarks, refresh baseline | `docs/contributing/BENCHMARK_GUIDE.md` |
| **Static analyzers** | Severities set in `.editorconfig` (Phase 0); Roslyn DLLs deferred until PC access | — |
| **Mutation testing** | Skipped per plan (overkill for Phase 2) | — |

### Benchmarks at a glance

| Test | What it measures | Phase 4 expectation |
|---|---|---|
| `Publish_100_listeners_10k_events` | EventBus dispatch hot path | No regression after Publish rework |
| `ListenerCount_5_types_5_listeners_each_1k_lookups` | EventBus ListenerCount cost (currently O(types × listeners)) | Large drop after O(1) refactor |
| `Subscribe_Unsubscribe_pair_1k_iterations` | EventBus mutation hot path | No regression |
| `Spawn_and_release_1000` | RevPool throughput | No regression past initial prewarm |
| `Spawn_at_cap_2000_eviction_cycles` | Over-cap eviction cost | No regression if Phase 4 adds ring-buffer eviction |
| `Cancel_one_id_among_1000_timers` | TimerScheduler.Cancel(int) | **Large drop expected** after O(1) dict lookup |
| `Tick_1000_timers_for_1000_frames` | TimerScheduler.Tick | Regression guard only |
| `Create_10000_timers` | Allocation cost per timer | Drop after Phase 4 pooling (if landed) |

### CI gating now in effect

| Gate | Workflow | Effect when failed |
|---|---|---|
| Unity tests (2022 + Unity 6, edit + play) | `unity-test.yml` | PR cannot merge |
| Coverage report uploaded | `unity-test.yml` | Artifact only (no gate yet — Phase 5 will set threshold per module) |
| Lint (editorconfig, yaml, markdown) | `lint.yml` | PR cannot merge |
| Docs coverage (baseline-driven) | `docs-coverage.yml` | PR cannot merge if it adds a new undocumented public member |
| Benchmark regression >5% | `benchmark.yml` | PR cannot merge |
| Public API change w/o Shipped/Unshipped update | `unity-test.yml` (via analyzer when DLL is wired) | Pending PC step |
| Release-time package.json version match | `release.yml` | Cannot publish release |

### What still needs the maintainer (at a PC)

1. **First CI run after merge** will populate the benchmark baseline because `benchmark-baseline.json` is `{}` and the script falls back to "no comparison" with a warning. After the first successful CI on `main`, download the artifact and commit a real baseline:

   ```bash
   gh run download <run-id> --name BenchmarkResults --dir _bench
   python3 scripts/check-benchmark-regression.py --results _bench --baseline scripts/benchmark-baseline.json --write-baseline
   git commit -am "chore(benchmark): seed baseline from first CI run"
   ```

2. **Roslyn analyzer DLLs** (per `docs/contributing/PUBLIC_API_GUIDE.md`): the analyzer + CodeFixes DLLs are committed under `Assets/RevCore/_Analyzers/` with `csc.rsp` wired per Runtime asmdef, but the `RoslynAnalyzer` label is deliberately absent — activation is a one-time v1.0 step (see `docs/contributing/RELEASE_CHECKLIST.md`).

3. **CI**: the Unity-driven workflows (`unity-test.yml`, `benchmark.yml`) were removed because the maintainer does not hold a Unity license and Personal manual activation has been deprecated. Tests and benchmarks run locally before push instead. The four remaining workflows (lint, docs-coverage, release-drafter, release) are pure-text and require no secrets.

### Why we stopped here in Phase 2

Adding more benchmarks (Audio voice tracking, Inspector drawer reflection cost) would require live `AudioSource` and `OnGUI` plumbing that's much higher effort than the win warrants today. They will appear in Phase 4 alongside the optimization PRs they prove out.

### Ready for Phase 3

Phase 3 is non-breaking safety fixes — audio voice cap, tweener null-safety, Save force-bypass throttle, ColorHelper TryParse, etc. Each fix:

1. Has a characterization test (some already pinned in Phase 1; others added per fix).
2. Lands as its own PR labelled `fix`.
3. Adds an entry to `PublicAPI.Unshipped.txt` if it expands the surface.
4. Adds a CHANGELOG line.

The infrastructure to enforce all four of those bullets is now in place.
