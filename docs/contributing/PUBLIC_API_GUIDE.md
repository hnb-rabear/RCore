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

As of v1.0.0, the analyzer is live:

- The analyzer DLL pair lives at `Assets/RevCore/_Analyzers/` (`Microsoft.CodeAnalysis.PublicApiAnalyzers.dll` + the matching `CodeFixes.dll`). Both have the **`RoslynAnalyzer`** asset label set on their `.meta` files — Unity feeds them into the C# compiler invocation.
- Each Runtime asmdef has a sibling `csc.rsp` declaring its two `additionalfile`s, so the analyzer knows which symbol file pair belongs to which assembly.

Together: edit a public symbol → recompile → analyzer fires → Quick Fix in Rider / VS / VS Code (with C# Dev Kit) writes to the right `Unshipped.txt`. No further wiring needed.

### Releasing — Unshipped to Shipped

At release-cut time, run:

```bash
python scripts/seal-public-api.py
```

The script promotes every `PublicAPI.Unshipped.txt` entry to `PublicAPI.Shipped.txt` (sorted), and resets each `Unshipped.txt` to its header-only state. Idempotent: re-running on an already-sealed module is a no-op. Use `--dry-run` to preview.

The v1.0.0 release used this same script after Rider bulk-applied "Add to public API" across the full surface — see `release/v1.0.0` branch history.
