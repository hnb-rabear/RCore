#!/usr/bin/env python3
"""Extract public API surface from C# files under a root.

Produces a CSV with columns:
  framework, module, file, kind, namespace, type, member, signature

This is heuristic (regex-based, not a full Roslyn parser), but sufficient for
producing a stable inventory we can diff across releases and use as the input
to RCore -> RevCore migration mapping.
"""
from __future__ import annotations

import argparse
import csv
import re
import sys
from dataclasses import dataclass
from pathlib import Path

NAMESPACE_RE = re.compile(r"^\s*namespace\s+([\w\.]+)\s*[{;]?")

TYPE_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"public\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+|unsafe\s+|ref\s+)*"
    r"(class|struct|interface|enum|record|delegate)\s+"
    r"(\w+)(?:<[^>]+>)?"
)

METHOD_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"public\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|sealed\s+|async\s+|extern\s+|unsafe\s+|new\s+|partial\s+)*"
    r"(?!class\s|struct\s|interface\s|enum\s|record\s|delegate\s|event\s|const\s)"
    r"([\w<>\[\],?\.\s]+?)\s+(\w+)\s*(<[^>]+>)?\s*\("
)

PROPERTY_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"public\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|sealed\s+|new\s+)*"
    r"(?!event\s|class\s|struct\s|interface\s|enum\s|record\s|delegate\s|const\s)"
    r"([\w<>\[\],?\.\s]+?)\s+(\w+)\s*\{\s*(get|set)"
)

FIELD_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"public\s+(?:static\s+|readonly\s+|const\s+|volatile\s+)*"
    r"(?!class\s|struct\s|interface\s|enum\s|record\s|delegate\s|event\s)"
    r"([\w<>\[\],?\.\s]+?)\s+(\w+)\s*(?:=|;)"
)

EVENT_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"public\s+(?:static\s+)?event\s+([\w<>\[\],?\.\s]+?)\s+(\w+)"
)

@dataclass
class Symbol:
    framework: str
    module: str
    file: str
    kind: str
    namespace: str
    type: str
    member: str
    signature: str

def detect_module(framework: str, path: Path) -> str:
    """Map a file path to a coarse module name."""
    parts = path.parts
    if framework == "RevCore":
        # Assets/RevCore/<Module>/...
        for i, p in enumerate(parts):
            if p == "RevCore" and i + 1 < len(parts):
                return parts[i + 1]
        return "Unknown"
    elif framework == "RCore":
        # Assets/RCore/Main/Runtime/<Area>/... or Assets/RCore/Sub/Runtime/<Area>/... or Assets/RCore/Services/<X>/...
        if "Services" in parts:
            idx = parts.index("Services")
            if idx + 1 < len(parts):
                return f"Services/{parts[idx + 1]}"
        # find "Runtime" then take next
        for i, p in enumerate(parts):
            if p == "Runtime" and i + 1 < len(parts):
                area = parts[i + 1]
                if i + 2 < len(parts) and not parts[i + 2].endswith(".cs"):
                    return f"{area}/{parts[i + 2]}"
                return area
        return "Unknown"
    return "Unknown"

def scan_file(framework: str, root: Path, path: Path) -> list[Symbol]:
    try:
        text = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        text = path.read_text(encoding="utf-8-sig", errors="ignore")
    lines = text.splitlines()

    namespace = ""
    type_stack: list[str] = []
    brace_depth_for_type: list[int] = []
    brace_depth = 0
    in_block_comment = False
    module = detect_module(framework, path)
    rel = str(path.relative_to(root.parent) if path.is_absolute() else path)
    symbols: list[Symbol] = []

    for raw in lines:
        line = raw
        # strip block comments
        if in_block_comment:
            if "*/" in line:
                line = line[line.index("*/") + 2 :]
                in_block_comment = False
            else:
                continue
        if "/*" in line and "*/" not in line:
            line = line[: line.index("/*")]
            in_block_comment = True
        # strip line comments (rough)
        if "//" in line and not line.strip().startswith("///"):
            # don't strip URLs ("://"); naive but fine for code lines
            i = line.find("//")
            if i > 0 and line[i - 1] == ":":
                pass
            else:
                line = line[:i]

        m_ns = NAMESPACE_RE.match(line)
        if m_ns:
            namespace = m_ns.group(1)

        m_type = TYPE_RE.match(line)
        if m_type:
            kind, tname = m_type.group(1), m_type.group(2)
            full = ".".join(type_stack + [tname]) if type_stack else tname
            symbols.append(Symbol(framework, module, rel, kind, namespace, full, "", line.strip()[:160]))
            type_stack.append(tname)
            brace_depth_for_type.append(brace_depth)
        else:
            # only check members when we are inside a type
            if type_stack:
                cur_type = ".".join(type_stack)
                if EVENT_RE.match(line):
                    m = EVENT_RE.match(line)
                    symbols.append(Symbol(framework, module, rel, "event", namespace, cur_type, m.group(2), line.strip()[:160]))
                elif PROPERTY_RE.match(line):
                    m = PROPERTY_RE.match(line)
                    symbols.append(Symbol(framework, module, rel, "property", namespace, cur_type, m.group(2), line.strip()[:160]))
                elif METHOD_RE.match(line):
                    m = METHOD_RE.match(line)
                    name = m.group(2)
                    # filter out constructors which match METHOD_RE accidentally
                    if name == type_stack[-1]:
                        symbols.append(Symbol(framework, module, rel, "ctor", namespace, cur_type, name, line.strip()[:160]))
                    elif name in {"if", "for", "while", "switch", "using", "return", "throw"}:
                        pass
                    else:
                        symbols.append(Symbol(framework, module, rel, "method", namespace, cur_type, name, line.strip()[:160]))
                elif FIELD_RE.match(line):
                    m = FIELD_RE.match(line)
                    symbols.append(Symbol(framework, module, rel, "field", namespace, cur_type, m.group(2), line.strip()[:160]))

        # update brace depth
        brace_depth += line.count("{") - line.count("}")
        # pop type stack when we exit
        while brace_depth_for_type and brace_depth <= brace_depth_for_type[-1]:
            brace_depth_for_type.pop()
            type_stack.pop()

    return symbols

def is_excluded(path: Path) -> bool:
    parts = [p.lower() for p in path.parts]
    return any(seg in parts for seg in ("editor", "tests", "samples~"))

def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("--rcore", default="Assets/RCore")
    p.add_argument("--revcore", default="Assets/RevCore")
    p.add_argument("--out", default="docs/api-inventory.csv")
    args = p.parse_args()

    all_symbols: list[Symbol] = []
    for framework, root_str in [("RCore", args.rcore), ("RevCore", args.revcore)]:
        root = Path(root_str)
        if not root.is_dir():
            print(f"::warning::{framework} root not found: {root}", file=sys.stderr)
            continue
        for cs in sorted(root.rglob("*.cs")):
            if is_excluded(cs):
                continue
            all_symbols.extend(scan_file(framework, root, cs))

    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    with out.open("w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(["framework", "module", "file", "kind", "namespace", "type", "member", "signature"])
        for s in all_symbols:
            w.writerow([s.framework, s.module, s.file, s.kind, s.namespace, s.type, s.member, s.signature])

    # Print summary
    from collections import Counter
    by_fw_kind = Counter((s.framework, s.kind) for s in all_symbols)
    by_fw_module_type = Counter((s.framework, s.module) for s in all_symbols if s.kind in {"class", "struct", "interface", "enum", "record", "delegate"})
    print(f"Wrote {len(all_symbols)} symbols to {out}")
    print()
    print("Symbols by framework x kind:")
    for (fw, kind), n in sorted(by_fw_kind.items()):
        print(f"  {fw:8} {kind:10} {n}")
    print()
    print("Public types by framework x module:")
    for (fw, mod), n in sorted(by_fw_module_type.items()):
        print(f"  {fw:8} {mod:35} {n}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
