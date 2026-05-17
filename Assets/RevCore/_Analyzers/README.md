# RevCore Analyzers

Roslyn analyzers consumed at compile time by every RevCore Runtime asmdef.

## What goes here

`Microsoft.CodeAnalysis.PublicApiAnalyzers.dll` — enforces the `PublicAPI.{Shipped,Unshipped}.txt` files described in [`docs/contributing/PUBLIC_API_GUIDE.md`](../../../docs/contributing/PUBLIC_API_GUIDE.md). Without this DLL the files are tracked as plain text but never validated against the actual compile.

## How to add it

1. Download `Microsoft.CodeAnalysis.PublicApiAnalyzers` from NuGet: <https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers/>.
2. The `.nupkg` is a zip — open it, navigate to `analyzers/dotnet/cs/`, take `Microsoft.CodeAnalysis.PublicApiAnalyzers.dll`.
3. Drop the DLL into this folder (`Assets/RevCore/_Analyzers/`).
4. In Unity Project view, select the DLL. In Inspector:
   - **Include Platforms**: untick every platform.
   - **Asset Labels**: add the label `RoslynAnalyzer` (lowercase r capital A — Unity recognizes it).
5. Save. Unity reimports; the analyzer activates on the next compile.

The 8 Runtime asmdefs each have a `csc.rsp` next to them that wires the per-module `PublicAPI.*.txt` files into the compile as `/additionalfile` references — no further per-module setup needed.

## Why this folder exists

We could place the DLL directly under each module's `Runtime/` folder, but every module would need its own copy. A single `_Analyzers/` folder at the framework root is the conventional Unity layout (the underscore prefix sorts it above feature folders).
