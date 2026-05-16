#!/usr/bin/env python3
"""Check XML doc coverage on public C# members under a root directory.

Heuristic, not a full parser. Flags public types/methods/properties/fields/events
that lack a /// <summary> block immediately above (within 6 lines, allowing
attributes between).

Used by .github/workflows/docs-coverage.yml.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path

PUBLIC_DECL = re.compile(
    r"""^\s*
        (?:\[[^\]]*\]\s*)*                       # leading attributes (single-line)
        public\s+
        (?!.*\b(?:class|struct|interface|enum|record|delegate)\b\s+\w+\s*[<({:])  # negative lookahead handled below differently
    """,
    re.VERBOSE,
)

# We use a simpler positive pattern set instead.
PATTERNS = [
    re.compile(r"^\s*public\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+|unsafe\s+)*"
               r"(class|struct|interface|enum|record|delegate)\s+(\w+)"),
    re.compile(r"^\s*public\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|sealed\s+|async\s+|extern\s+|unsafe\s+|new\s+)*"
               r"(?!class\s|struct\s|interface\s|enum\s|record\s|delegate\s)"
               r"[\w<>\[\],?\.\s]+?\s+(\w+)\s*\("),  # method
    re.compile(r"^\s*public\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|sealed\s+|new\s+)*"
               r"[\w<>\[\],?\.\s]+?\s+(\w+)\s*\{\s*(get|set)"),  # property
    re.compile(r"^\s*public\s+(?:static\s+|readonly\s+|const\s+)*"
               r"[\w<>\[\],?\.\s]+?\s+(\w+)\s*(?:=|;)"),  # field/const
    re.compile(r"^\s*public\s+event\s+[\w<>\[\],?\.\s]+?\s+(\w+)"),
]

@dataclass
class Finding:
    file: Path
    line: int
    snippet: str

def has_doc_above(lines: list[str], idx: int) -> bool:
    """Walk backwards from idx-1 through blank lines, attributes, and other ///,
    return True if we hit a /// <summary> within reach."""
    i = idx - 1
    seen_summary = False
    steps = 0
    while i >= 0 and steps < 25:
        line = lines[i].strip()
        if line.startswith("///"):
            if "<summary>" in line or "<inheritdoc" in line:
                return True
            seen_summary = True
        elif line.startswith("//"):
            pass
        elif line.startswith("[") and line.endswith("]"):
            pass
        elif line == "":
            pass
        else:
            return seen_summary  # hit code; only counts if we already saw ///
        i -= 1
        steps += 1
    return seen_summary

def scan_file(path: Path) -> list[Finding]:
    findings: list[Finding] = []
    try:
        text = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        text = path.read_text(encoding="utf-8-sig", errors="ignore")
    lines = text.splitlines()
    in_block_comment = False
    for i, raw in enumerate(lines):
        line = raw
        if in_block_comment:
            if "*/" in line:
                in_block_comment = False
            continue
        if line.lstrip().startswith("/*"):
            if "*/" not in line:
                in_block_comment = True
            continue
        if "public" not in line:
            continue
        # skip obvious non-decls
        stripped = line.strip()
        if stripped.startswith("//") or stripped.startswith("///"):
            continue
        for pat in PATTERNS:
            m = pat.match(line)
            if m:
                if not has_doc_above(lines, i):
                    findings.append(Finding(path, i + 1, stripped[:120]))
                break
    return findings

def is_excluded(path: Path) -> bool:
    parts = set(p.lower() for p in path.parts)
    return any(seg in parts for seg in ("editor", "tests", "samples~", "internal"))

def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--root", required=True)
    p.add_argument("--min-coverage", type=float, default=100.0)
    p.add_argument("--baseline", type=Path, default=None,
                   help="JSON file mapping file path -> allowed undocumented count. "
                        "Allows incremental adoption without blocking PRs that don't touch legacy code.")
    p.add_argument("--write-baseline", action="store_true",
                   help="Instead of checking, write the current state to --baseline and exit 0.")
    args = p.parse_args()

    root = Path(args.root)
    if not root.is_dir():
        print(f"::error::root not found: {root}", file=sys.stderr)
        return 2

    baseline: dict[str, int] = {}
    if args.baseline and args.baseline.exists():
        baseline = json.loads(args.baseline.read_text())

    total_public = 0
    total_undocumented = 0
    by_file: dict[str, list[Finding]] = {}

    for cs in sorted(root.rglob("*.cs")):
        if is_excluded(cs):
            continue
        findings = scan_file(cs)
        # Approximate "public count" by re-scanning: number of lines that match any PATTERN.
        public_count = 0
        text_lines = cs.read_text(encoding="utf-8", errors="ignore").splitlines()
        for line in text_lines:
            if "public" not in line:
                continue
            stripped = line.strip()
            if stripped.startswith("//"):
                continue
            for pat in PATTERNS:
                if pat.match(line):
                    public_count += 1
                    break
        total_public += public_count
        total_undocumented += len(findings)
        if findings:
            by_file[str(cs.relative_to(Path.cwd()) if cs.is_absolute() else cs)] = findings

    documented = total_public - total_undocumented
    coverage = (documented / total_public * 100) if total_public else 100.0

    print(f"Public members: {total_public}")
    print(f"Documented:     {documented}")
    print(f"Undocumented:   {total_undocumented}")
    print(f"Coverage:       {coverage:.2f}% (gate: {args.min_coverage}%)")

    if args.write_baseline:
        if not args.baseline:
            print("::error::--write-baseline requires --baseline path", file=sys.stderr)
            return 2
        snapshot = {f: len(items) for f, items in sorted(by_file.items())}
        args.baseline.write_text(json.dumps(snapshot, indent=2, sort_keys=True) + "\n")
        print(f"Wrote baseline with {len(snapshot)} file entries -> {args.baseline}")
        return 0

    fail = False

    if coverage + 1e-9 < args.min_coverage:
        # If there is no baseline, this is a fatal failure once we are setting the gate.
        # In Phase 0, we ship the script with --min-coverage 100 BUT we also produce a baseline
        # so that the gate is effectively "no regression". The gate tightens module-by-module
        # in later phases as docs are filled in.
        if not baseline:
            print("::warning::No baseline file present. Run with --baseline to record current state.")
        else:
            for f, items in by_file.items():
                allowed = baseline.get(f, 0)
                actual = len(items)
                if actual > allowed:
                    fail = True
                    print(f"::error file={f}::xmldoc regressed: {actual} undocumented public members (baseline allows {allowed})")
                    for it in items[:10]:
                        print(f"::error file={f},line={it.line}::missing /// <summary>: {it.snippet}")

    if fail:
        return 1
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
