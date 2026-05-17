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

## Current state: dormant by design

Both DLLs are committed but **the `RoslynAnalyzer` asset label is intentionally absent**. With the label, the analyzer immediately reports ~1,000 `RS0016` warnings ("Symbol 'X' is not part of the declared API") because none of RevCore's existing public surface has been seeded into the `PublicAPI.Shipped.txt` files yet.

Seeding ~1,000 entries by hand is intractable, and Rider's "Add to public API" quick fix does not work in the current Unity setup because Unity's generated `.csproj` files don't propagate the `csc.rsp` `/additionalfile` references to Rider/MSBuild — Rider's analyzer instance can't see the `PublicAPI.*.txt` files.

The plan: seed `PublicAPI.Shipped.txt` for every module as a deliberate step in the v1.0 release checklist (see `docs/contributing/RELEASE_CHECKLIST.md`). At that point we add the `RoslynAnalyzer` label back to both DLLs, the analyzer goes live, and from v1.0 onward every public API change must update `Unshipped.txt` to compile.

Until then the `.txt` files are review-only: the maintainer scans them in PR diffs by eye.

## Why this folder exists

We could place the DLL directly under each module's `Runtime/` folder, but every module would need its own copy. A single `_Analyzers/` folder at the framework root is the conventional Unity layout (the underscore prefix sorts it above feature folders).
