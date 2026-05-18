## Public API Guide

We use the pattern from the Roslyn team: each runtime assembly tracks its public surface in two files at the asmdef root:

- `PublicAPI.Shipped.txt` — symbols already released. Never edited except during a release (when `Unshipped` is merged in).
- `PublicAPI.Unshipped.txt` — symbols added since the last release. Every PR that adds/changes public surface updates this file.

The `Microsoft.CodeAnalysis.PublicApiAnalyzers` package enforces this. CI fails if the surface diverges from the files.

### File location

For each runtime assembly:

```
Assets/RevCore/<Module>/Runtime/
  RevCore.<Module>.Runtime.asmdef
  PublicAPI.Shipped.txt
  PublicAPI.Unshipped.txt
```

Editor and Tests assemblies do not require these files (their public surface is not consumer-facing).

### File format

Plain text. One symbol per line. Header line required:

```
#nullable enable
```

Symbol format follows the analyzer's convention:

```
RevCore.Audio.BaseAudioManager
RevCore.Audio.BaseAudioManager.MasterVolume.get -> float
RevCore.Audio.BaseAudioManager.MasterVolume.set -> void
RevCore.Audio.BaseAudioManager.PlaySfx(string clipName, float volume = 1, bool loop = false) -> UnityEngine.AudioSource?
```

Sort lines lexicographically. (The analyzer offers a "Sort" code fix.)

### Workflow

1. You add a new public type or member.
2. Build → analyzer reports `RS0016: Symbol 'X' is not part of the declared API`.
3. Use Roslyn Quick Fix → "Add to public API" → adds to `PublicAPI.Unshipped.txt`.
4. Commit the change to `Unshipped.txt` together with the code change.

### Removing public API

1. Mark the symbol `[Obsolete(..., error: false)]` (Stage 1 per Deprecation Policy).
2. The line stays in `PublicAPI.Shipped.txt` — it is still part of the surface.
3. After Stage 3 (actual removal), add a line in `Unshipped.txt` with the `*REMOVED*` prefix:

   ```
   *REMOVED*RevCore.Audio.BaseAudioManager.LegacyMethod() -> void
   ```

4. At release, that `*REMOVED*` line moves to `Shipped.txt` and replaces the original line (or the maintainer deletes the original — analyzer rules document the exact behavior).

### Why this matters

Without this file, public API changes are invisible in PR diffs (they hide inside large code diffs). With it, every surface change is one line in a tracked file that the reviewer (you, solo) can scan in seconds. This is the single most important governance mechanism for a solo-maintained framework.

### Analyzer wiring

As of v1.0.0, the analyzer DLLs ship in the repo but are **dormant**:

- DLL pair at `Assets/RevCore/_Analyzers/` (`Microsoft.CodeAnalysis.PublicApiAnalyzers.dll` + the matching `CodeFixes.dll`). Neither has the `RoslynAnalyzer` asset label — Unity does not load them as analyzers.
- Each Runtime asmdef has a sibling `csc.rsp` declaring its two `additionalfile`s, so when the analyzer IS activated (by setting the `RoslynAnalyzer` label) it knows which symbol file pair belongs to which assembly.

**Why dormant:** Unity's `RoslynAnalyzer` label loads an analyzer DLL project-wide. That hits the default `Assembly-CSharp(-Editor)` compilations which contain legacy RCore folders (no asmdef, no PublicAPI tracking by design). The analyzer would then fire RS0016 on every public RCore symbol — noise the maintainer cannot resolve without touching RCore, which is out of scope. Editorconfig-based scoping (path globs, nested `.editorconfig`) does not suppress these in practice. Until Unity gains asmdef-level analyzer scoping, the analyzer cannot be activated without polluting the RCore compile.

**To temporarily activate** (for an audit pass, IDE Quick Fix workflow, etc.):

1. Edit `Assets/RevCore/_Analyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.dll.meta` — add a `labels:` block with `- RoslynAnalyzer` underneath, between the `guid:` line and the `PluginImporter:` line. Do the same for `Microsoft.CodeAnalysis.PublicApiAnalyzers.CodeFixes.dll.meta`.
2. Recompile — analyzer fires RS0016 for any public symbol not in `Shipped.txt` / `Unshipped.txt`.
3. Use Rider / VS code fix to add entries (writes to `Unshipped.txt`).
4. Run `python scripts/seal-public-api.py` to promote to `Shipped.txt`.
5. **Revert the label** before committing if you want the dormant state preserved.

**Without the analyzer live**, the `Shipped.txt` files are still useful as a committed paper trail — every PR that adds public surface includes a one-line diff to the right module's file, which the reviewer (you, solo) can scan in seconds. Discipline replaces enforcement.

### Suppressed analyzer rules

Five analyzer rules from `Microsoft.CodeAnalysis.PublicApiAnalyzers` are disabled in `.editorconfig`. Two groups, two reasons.

**Group 1 — N/A for source-package distribution (RS0026, RS0027):**

- **RS0026** — *Do not add multiple public overloads with optional parameters.* Guards against silent binary-compatibility breaks when a consumer is linked against a pre-built DLL and the maintainer later changes the overload set or default values: the consumer's IL keeps the old binding, the new assembly behaves differently.
- **RS0027** — *Public API with optional parameter(s) should have most parameters before any optional ones.* Same reasoning.

Both N/A because RevCore is distributed as a Unity Package Manager source package (`?path=Assets/RevCore/<Module>#<tag>` git URL) — consumer projects compile from `.cs` source against the pinned tag, so binary-ABI concerns do not apply. Can be re-enabled if a future RevCore distribution shifts to pre-built DLLs (e.g. publishing to NuGet).

**Group 2 — nullability not adopted project-wide (RS0041, RS0050, RS0051):**

- **RS0041** — *Public members should not use oblivious types.*
- **RS0050** — *Public API contains oblivious reference types.*
- **RS0051** — *Public API has nullability mismatch.*

RevCore has not enabled `#nullable enable` across its runtime sources. Annotating the full public API surface with `?` / non-null is a multi-day effort (every existing parameter, return value, and field needs a nullability decision; many cascading CS8XXX warnings to resolve). That work is post-v1.0 and tracked as a separate hardening phase. The PublicAPI files use `#nullable enable` as a format header — it does not propagate to the code that the analyzer scans.

### Releasing — Unshipped to Shipped

At release-cut time, run:

```bash
python scripts/seal-public-api.py
```

The script promotes every `PublicAPI.Unshipped.txt` entry to `PublicAPI.Shipped.txt` (sorted), and resets each `Unshipped.txt` to its header-only state. Idempotent: re-running on an already-sealed module is a no-op. Use `--dry-run` to preview.

The v1.0.0 release used this same script after Rider bulk-applied "Add to public API" across the full surface — see `release/v1.0.0` branch history.
