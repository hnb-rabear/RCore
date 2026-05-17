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

### Wiring up the analyzer in Unity (remaining work)

The skeleton files (`PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`) are in place per Runtime asmdef. To make the analyzer actually enforce them in the Unity compile, two pieces still need to be added in a follow-up commit:

1. **Place the analyzer DLL** at `Assets/RevCore/_Analyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.dll`. Download from NuGet, take the file under `analyzers/dotnet/cs/`. In Unity Inspector, set its **Asset Labels** to include `RoslynAnalyzer`, and untick all platforms so it never ships to runtime builds.
2. **Add a `csc.rsp`** next to each Runtime asmdef containing:

   ```
   /additionalfile:Assets/RevCore/<Module>/Runtime/PublicAPI.Shipped.txt
   /additionalfile:Assets/RevCore/<Module>/Runtime/PublicAPI.Unshipped.txt
   ```

Until that wiring lands, the files are still useful as a manually-curated record — the maintainer reviews them in PR diff.
