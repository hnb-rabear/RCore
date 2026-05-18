#!/usr/bin/env python3
"""Promote PublicAPI.Unshipped.txt entries to PublicAPI.Shipped.txt across all
RevCore modules. Idempotent: re-running on an already-sealed module is a no-op.

Workflow:
  1. Add or change a public symbol in RevCore code.
  2. Run Unity / Rider — the PublicApiAnalyzers complains (RS0016) until the
     new symbol is recorded.
  3. Apply the "Add to public API" code fix in Rider — the entry lands in
     `<Module>/Runtime/PublicAPI.Unshipped.txt`.
  4. At release-cut time, run this script: every Unshipped entry moves into
     `Shipped.txt`, sorted; Unshipped.txt is reset to header-only.

This matches the Microsoft.CodeAnalysis.PublicApiAnalyzers convention:
Unshipped is the staging lane for not-yet-released additions; Shipped is the
committed contract.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

HEADER = "#nullable enable\n"


def seal_module(runtime_dir: Path, dry_run: bool) -> tuple[int, int]:
    """Return (entries_promoted, entries_already_shipped)."""
    shipped_path = runtime_dir / "PublicAPI.Shipped.txt"
    unshipped_path = runtime_dir / "PublicAPI.Unshipped.txt"

    if not shipped_path.exists() or not unshipped_path.exists():
        return 0, 0

    shipped_lines = shipped_path.read_text(encoding="utf-8").splitlines()
    unshipped_lines = unshipped_path.read_text(encoding="utf-8").splitlines()

    # Strip header and blank lines from both.
    shipped_entries = {l for l in shipped_lines if l and l != HEADER.strip()}
    unshipped_entries = {l for l in unshipped_lines if l and l != HEADER.strip()}

    new_entries = unshipped_entries - shipped_entries
    if not new_entries:
        return 0, len(unshipped_entries)

    merged = sorted(shipped_entries | unshipped_entries)
    new_shipped = HEADER + "\n".join(merged) + "\n"
    new_unshipped = HEADER

    if not dry_run:
        shipped_path.write_text(new_shipped, encoding="utf-8")
        unshipped_path.write_text(new_unshipped, encoding="utf-8")

    return len(new_entries), len(unshipped_entries - new_entries)


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument(
        "--root",
        default="Assets/RevCore",
        help="RevCore root containing one folder per module (Foundation, Audio, ...)",
    )
    p.add_argument(
        "--dry-run",
        action="store_true",
        help="Report what would change without writing files.",
    )
    args = p.parse_args()

    root = Path(args.root)
    if not root.is_dir():
        print(f"error: {root} is not a directory", file=sys.stderr)
        return 1

    total_promoted = 0
    total_modules = 0
    for module_dir in sorted(p for p in root.iterdir() if p.is_dir() and not p.name.startswith("_")):
        runtime_dir = module_dir / "Runtime"
        if not runtime_dir.is_dir():
            continue
        promoted, _ = seal_module(runtime_dir, args.dry_run)
        if promoted > 0:
            verb = "would promote" if args.dry_run else "promoted"
            print(f"{module_dir.name:<12} {verb} {promoted} entry/entries to Shipped.txt")
            total_promoted += promoted
        total_modules += 1

    if total_promoted == 0:
        print(f"All {total_modules} modules already sealed — nothing to do.")
    else:
        verb = "would promote" if args.dry_run else "promoted"
        print(f"\nTotal: {verb} {total_promoted} entries across {total_modules} modules.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
