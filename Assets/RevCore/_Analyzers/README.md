# RevCore Analyzers

Roslyn analyzers consumed at compile time by every RevCore Runtime asmdef.

## What goes here

`Microsoft.CodeAnalysis.PublicApiAnalyzers.dll` — enforces the `PublicAPI.{Shipped,Unshipped}.txt` files described in [`docs/contributing/PUBLIC_API_GUIDE.md`](../../../docs/contributing/PUBLIC_API_GUIDE.md). Without this DLL the files are tracked as plain text but never validated against the actual compile.

## How to add it

1. Download `Microsoft.CodeAnalysis.PublicApiAnalyzers` from NuGet: <https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers/>.
2. The `.nupkg` is a zip with an unusual extension — rename a copy to `.zip`, or use `Expand-Archive` in PowerShell, then navigate to `analyzers/dotnet/`. Take both:
   - `Microsoft.CodeAnalysis.PublicApiAnalyzers.dll` — the analyzer itself (RS0016, RS0017, …).
   - `Microsoft.CodeAnalysis.PublicApiAnalyzers.CodeFixes.dll` — the "Add to public API" quick fix that updates `PublicAPI.Unshipped.txt` on a single keypress.
3. Drop both DLLs into this folder (`Assets/RevCore/_Analyzers/`).
4. In Unity Project view, select each DLL one at a time. In Inspector:
   - **Include Platforms**: untick every platform (this is critical — analyzers must never ship into a runtime build).
   - **Asset Labels**: add the label `RoslynAnalyzer` (capital A — Unity recognizes that exact label).
5. Save. Unity reimports; the analyzer activates on the next compile.

> The 3.3.4 release places the main analyzer DLL directly under `analyzers/dotnet/`, not `analyzers/dotnet/cs/`. The `cs/` subfolder only holds localized resource DLLs which Unity does not need.

The 8 Runtime asmdefs each have a `csc.rsp` next to them that wires the per-module `PublicAPI.*.txt` files into the compile as `/additionalfile` references — no further per-module setup needed.

## Current state: dormant — post-v1.0

Both DLLs are committed but **the `RoslynAnalyzer` asset label is intentionally absent**.

At the v1.0 cut (2026-05-17), the label was temporarily set so Rider could populate every module's `PublicAPI.Unshipped.txt` via its "Add to public API" code fix at solution scope. `scripts/seal-public-api.py` then promoted those 1337 entries into the matching `PublicAPI.Shipped.txt` files. After seeding, the label was **reverted**.

The reason the label cannot stay on: Unity loads any DLL bearing `RoslynAnalyzer` project-wide, which means the analyzer fires for the default `Assembly-CSharp(-Editor)` compilations. Those compilations contain legacy `Assets/RCore.Archives/`, `Assets/RCore.LXLite/`, `Assets/RCore.SheetX/` — none of which carry asmdefs or track a PublicAPI surface. The result is ~130 `RS0016` warnings on legacy code the maintainer has explicitly decided to leave untouched. Editorconfig-based scoping (path globs in root config, nested `.editorconfig` under `Assets/RevCore/`) does not suppress these in practice — Roslyn's editorconfig matcher and Unity's analyzer loader do not interact the way the spec implies.

Live enforcement and the "RCore untouched" mandate are therefore mutually exclusive in current Unity. Without asmdef-level analyzer scoping in a future Unity version, the analyzer stays dormant.

**The `Shipped.txt` files still serve as a committed paper trail.** Every PR adding public surface includes a one-line diff to the right module's file; the solo maintainer scans it by eye. Discipline replaces enforcement.

### How to temporarily activate

For an audit pass (e.g. verifying a refactor didn't expand the surface) or to re-seed after a major reshuffling:

1. Edit `Microsoft.CodeAnalysis.PublicApiAnalyzers.dll.meta` — add a `labels:` block with `- RoslynAnalyzer` underneath, between the `guid:` line and the `PluginImporter:` line. Same edit for `Microsoft.CodeAnalysis.PublicApiAnalyzers.CodeFixes.dll.meta`.
2. Recompile in Unity — RS0016 fires for any public symbol not in `Shipped.txt` / `Unshipped.txt`.
3. Apply Rider quick fixes solution-wide if needed.
4. Run `python scripts/seal-public-api.py` to promote.
5. **Revert** the label change before committing.

## Why this folder exists

We could place the DLL directly under each module's `Runtime/` folder, but every module would need its own copy. A single `_Analyzers/` folder at the framework root is the conventional Unity layout (the underscore prefix sorts it above feature folders).
