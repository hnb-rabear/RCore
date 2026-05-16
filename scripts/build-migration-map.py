#!/usr/bin/env python3
"""Build first-pass RCore -> RevCore migration map from api-inventory.csv.

Matches types by exact name first, then by fuzzy similarity. Marks each row:
  UNCHANGED  — same name + namespace in both frameworks
  RENAMED    — same name in different namespace/module
  LIKELY     — name similarity > 0.85
  PORTED     — RCore module collapsed/renamed into RevCore (e.g. Common/Helper -> Foundation/Helpers)
  GAP        — no obvious RevCore equivalent

Output is markdown + raw CSV so the maintainer can audit & override the LIKELY rows.
"""
from __future__ import annotations

import argparse
import csv
from collections import defaultdict
from difflib import SequenceMatcher
from pathlib import Path

# Known module collapses RCore -> RevCore. Used to infer "PORTED" status.
MODULE_MAP = {
    "Common/Helper": "Foundation",
    "Common/BigNumber": "Foundation",
    "Common/SerializableDictionary": "Foundation",
    "Common/Debug": "Foundation",
    "Common/Pool": "Pool",
    "Common/Timer": "Timer",
    "Common": "Foundation",  # catch-all for misc Common files
    "Audio": "Audio",
    "Data/JObjectDB": "Data",
    "Data/KeyValueDB": "Prefs",  # RevCore Prefs is the spiritual successor
    "Data/Common": "Data",
    "Data": "Data",
    "Inspector": "Inspector",
    "UI": "UI",
    "UI/Button": "UI",
    "UI/Toggle": "UI",
    "UI/Layout": "UI",
    "UI/Scrollview": "UI",
    "UI/Joystick": "UI",
    "UI/HomePage": "UI",
    "UI/PanelStack": "UI",
    "UI/DragDrop": "UI",  # not actually in RevCore yet -> will flag as gap
}

# Whole modules with no RevCore equivalent.
RCORE_ONLY_MODULES = {
    "Services/Ads",
    "Services/Firebase",
    "Services/GameServices",
    "Services/IAP",
    "Services/Notification",
    "Plugins",
    "Common/VFX",
    "Data/RPGBase",
    "Configuration.cs",  # detected as module by the heuristic; ignore
}

def normalize_name(n: str) -> str:
    """Strip leading R/Rev prefix for fuzzy matching."""
    s = n
    for prefix in ("Rev", "R"):
        if s.startswith(prefix) and len(s) > len(prefix) and s[len(prefix)].isupper():
            s = s[len(prefix):]
            break
    return s.lower()

def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--inventory", default="docs/api-inventory.csv")
    p.add_argument("--out-md", default="docs/migration/rcore-to-revcore-api-map.md")
    p.add_argument("--out-csv", default="docs/migration/rcore-to-revcore-api-map.csv")
    p.add_argument("--gap-md", default="docs/gap-analysis.md")
    args = p.parse_args()

    rcore_types: list[dict] = []
    revcore_types: list[dict] = []
    with open(args.inventory) as f:
        for row in csv.DictReader(f):
            if row["kind"] not in {"class", "struct", "interface", "enum", "record", "delegate"}:
                continue
            if row["framework"] == "RCore":
                rcore_types.append(row)
            else:
                revcore_types.append(row)

    rev_by_norm: dict[str, list[dict]] = defaultdict(list)
    for r in revcore_types:
        rev_by_norm[normalize_name(r["type"])].append(r)

    rows: list[tuple] = []  # (rcore_module, rcore_type, rcore_kind, status, revcore_module, revcore_type, note)

    for rc in rcore_types:
        rc_name = rc["type"]
        rc_module = rc["module"]
        rc_kind = rc["kind"]

        if rc_module in RCORE_ONLY_MODULES:
            rows.append((rc_module, rc_name, rc_kind, "GAP", "", "", "RCore-only module — not in RevCore scope yet"))
            continue

        norm = normalize_name(rc_name)
        candidates = rev_by_norm.get(norm, [])

        if not candidates:
            # try fuzzy
            best = None
            best_score = 0.0
            for r in revcore_types:
                score = SequenceMatcher(None, normalize_name(rc_name), normalize_name(r["type"])).ratio()
                if score > best_score:
                    best_score = score
                    best = r
            if best and best_score >= 0.85:
                rows.append((rc_module, rc_name, rc_kind, "LIKELY",
                             best["module"], best["type"],
                             f"fuzzy match score {best_score:.2f}"))
            else:
                expected = MODULE_MAP.get(rc_module, "?")
                rows.append((rc_module, rc_name, rc_kind, "GAP",
                             expected, "",
                             "no RevCore equivalent found — port or drop?"))
            continue

        # Found name match(es)
        # Prefer one in the expected RevCore module
        expected_mod = MODULE_MAP.get(rc_module)
        chosen = None
        for c in candidates:
            if c["module"] == expected_mod:
                chosen = c
                break
        if not chosen:
            chosen = candidates[0]

        same_ns = rc["namespace"] == chosen["namespace"]
        if same_ns:
            rows.append((rc_module, rc_name, rc_kind, "UNCHANGED",
                         chosen["module"], chosen["type"], chosen["namespace"]))
        else:
            rows.append((rc_module, rc_name, rc_kind,
                         "RENAMED" if rc_name != chosen["type"] else "PORTED",
                         chosen["module"], chosen["type"],
                         f"{rc['namespace']} -> {chosen['namespace']}"))

    # Write raw CSV
    csv_path = Path(args.out_csv)
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(["rcore_module", "rcore_type", "kind", "status", "revcore_module", "revcore_type", "note"])
        for r in rows:
            w.writerow(r)

    # Write markdown grouped by status
    md_path = Path(args.out_md)
    md_path.parent.mkdir(parents=True, exist_ok=True)

    by_status: dict[str, list[tuple]] = defaultdict(list)
    for r in rows:
        by_status[r[3]].append(r)

    lines: list[str] = []
    lines.append("## RCore -> RevCore Migration Map")
    lines.append("")
    lines.append("Auto-generated by `scripts/build-migration-map.py`. Treat as first-pass; LIKELY rows need maintainer audit.")
    lines.append("")
    lines.append("| Status | Count | Meaning |")
    lines.append("|---|---|---|")
    lines.append(f"| UNCHANGED | {len(by_status.get('UNCHANGED', []))} | Same type name + namespace in both. No migration needed. |")
    lines.append(f"| PORTED | {len(by_status.get('PORTED', []))} | Same type name, different namespace (module reshuffled). Update `using` only. |")
    lines.append(f"| RENAMED | {len(by_status.get('RENAMED', []))} | Different type name with high confidence. Mechanical rename. |")
    lines.append(f"| LIKELY | {len(by_status.get('LIKELY', []))} | Fuzzy match; maintainer must verify. |")
    lines.append(f"| GAP | {len(by_status.get('GAP', []))} | No RevCore equivalent. Decide: port / drop / replace. |")
    lines.append("")
    lines.append("---")
    lines.append("")

    for status in ["GAP", "LIKELY", "RENAMED", "PORTED", "UNCHANGED"]:
        items = by_status.get(status, [])
        if not items:
            continue
        lines.append(f"### {status} ({len(items)})")
        lines.append("")
        lines.append("| RCore module | RCore type | Kind | RevCore module | RevCore type | Note |")
        lines.append("|---|---|---|---|---|---|")
        for r in sorted(items):
            lines.append(f"| {r[0]} | `{r[1]}` | {r[2]} | {r[4]} | {('`' + r[5] + '`') if r[5] else '—'} | {r[6]} |")
        lines.append("")

    md_path.write_text("\n".join(lines) + "\n", encoding="utf-8")

    # Write gap analysis
    gap_path = Path(args.gap_md)
    gap_lines: list[str] = []
    gap_lines.append("## Gap Analysis: RCore features missing in RevCore")
    gap_lines.append("")
    gap_lines.append("Each gap needs one decision per row:")
    gap_lines.append("")
    gap_lines.append("- **PORT** — implement in RevCore. Add ETA + module owner.")
    gap_lines.append("- **DROP** — not coming to RevCore. Consumers stay on RCore for this, or migrate to a third-party.")
    gap_lines.append("- **REPLACE** — RevCore covers via a different API; document the equivalence.")
    gap_lines.append("- **DEFER** — revisit after v1.0.")
    gap_lines.append("")
    gap_lines.append("Maintainer (you) fills in the **Decision** column below.")
    gap_lines.append("")

    gaps = by_status.get("GAP", [])
    by_module: dict[str, list[tuple]] = defaultdict(list)
    for g in gaps:
        by_module[g[0]].append(g)

    gap_lines.append(f"Total gap types: **{len(gaps)}** across {len(by_module)} RCore modules.")
    gap_lines.append("")
    for mod in sorted(by_module):
        items = by_module[mod]
        gap_lines.append(f"### {mod} ({len(items)} types)")
        gap_lines.append("")
        gap_lines.append("| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |")
        gap_lines.append("|---|---|---|---|")
        for r in sorted(items, key=lambda x: x[1]):
            gap_lines.append(f"| `{r[1]}` | {r[2]} |  |  |")
        gap_lines.append("")

    gap_path.write_text("\n".join(gap_lines) + "\n", encoding="utf-8")

    print(f"Wrote map -> {md_path}")
    print(f"Wrote map csv -> {csv_path}")
    print(f"Wrote gap analysis -> {gap_path}")
    print()
    print("Summary by status:")
    for status in ["UNCHANGED", "PORTED", "RENAMED", "LIKELY", "GAP"]:
        print(f"  {status:10} {len(by_status.get(status, []))}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
