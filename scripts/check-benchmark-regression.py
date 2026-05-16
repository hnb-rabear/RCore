#!/usr/bin/env python3
"""Compare Unity.PerformanceTesting results against a baseline JSON.

Reads the PerformanceTestResults.json that game-ci/unity-test-runner emits next
to PerformanceTestResults.xml (the Performance package writes both). Diffs each
benchmark's "Median" sample value against the baseline. Fails the run if any
test's median exceeds (baseline * (1 + tolerance/100)).

When --write-baseline is passed, the script overwrites the baseline with the
current results — used once per release to refresh the reference.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

def find_results_json(results_dir: Path) -> Path | None:
    candidates = list(results_dir.rglob("PerformanceTestResults.json"))
    if not candidates:
        return None
    return candidates[0]

def load_results(path: Path) -> dict[str, float]:
    """Map FullName -> median sample value (ns or ms depending on package version)."""
    data = json.loads(path.read_text())
    out: dict[str, float] = {}
    # The schema varies slightly by package version. We handle the common shape:
    # { "Results": [ { "Name": "...", "Categories": [...], "SampleGroups": [ { "Name": "Time", "Median": 12345 } ] }, ... ] }
    for test in data.get("Results", []):
        name = test.get("Name") or test.get("FullName")
        if not name:
            continue
        median = None
        for sg in test.get("SampleGroups", []):
            if sg.get("Name", "").lower() in {"time", "time.ms", "main thread time"}:
                median = sg.get("Median")
                break
        if median is None and test.get("SampleGroups"):
            median = test["SampleGroups"][0].get("Median")
        if median is not None:
            out[name] = float(median)
    return out

def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--results", required=True, help="Directory containing PerformanceTestResults.json")
    p.add_argument("--baseline", type=Path, required=True)
    p.add_argument("--tolerance", type=float, default=5.0, help="Allowed regression %% over baseline")
    p.add_argument("--write-baseline", action="store_true")
    args = p.parse_args()

    results_dir = Path(args.results)
    rj = find_results_json(results_dir)
    if rj is None:
        print(f"::warning::No PerformanceTestResults.json under {results_dir}. Skipping comparison.")
        return 0

    current = load_results(rj)
    if not current:
        print("::warning::PerformanceTestResults.json parsed but no benchmarks found.")
        return 0

    print(f"Found {len(current)} benchmark results.")

    if args.write_baseline:
        args.baseline.write_text(json.dumps(current, indent=2, sort_keys=True) + "\n")
        print(f"Wrote baseline -> {args.baseline}")
        return 0

    if not args.baseline.exists():
        print(f"::warning::Baseline {args.baseline} does not exist yet. Skipping comparison; "
              "run with --write-baseline locally to seed it.")
        return 0

    baseline = json.loads(args.baseline.read_text())
    tol = 1 + args.tolerance / 100.0

    failed = False
    print("\nBenchmark comparison:")
    print(f"{'Test':<70} {'Baseline':>12} {'Current':>12} {'Delta %':>10}")
    print("-" * 110)
    for name in sorted(current):
        b = baseline.get(name)
        c = current[name]
        if b is None:
            print(f"{name:<70} {'NEW':>12} {c:>12.2f} {'—':>10}")
            continue
        delta_pct = ((c - b) / b * 100) if b else 0
        marker = "" if c <= b * tol else "  REGRESS"
        print(f"{name:<70} {b:>12.2f} {c:>12.2f} {delta_pct:>+9.2f}%{marker}")
        if c > b * tol:
            failed = True
            print(f"::error::Benchmark {name} regressed {delta_pct:.2f}% (baseline {b:.2f}, now {c:.2f}, tolerance {args.tolerance}%)")

    # Detect missing benchmarks (could indicate a test got renamed or skipped)
    for name in sorted(baseline):
        if name not in current:
            print(f"::warning::Baseline contained {name} but no current result.")

    return 1 if failed else 0

if __name__ == "__main__":
    raise SystemExit(main())
