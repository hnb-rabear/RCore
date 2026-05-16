## Deprecation Policy

Public APIs in RevCore are removed in three stages, never in one step.

### Stages

| Stage | Attribute | Minimum duration | Effect on consumer |
|---|---|---|---|
| 1. Soft deprecate | `[Obsolete("Use X instead. Will be removed in v<future>.", error: false)]` | 1 MINOR version | Compiler warning |
| 2. Hard deprecate | `[Obsolete("Use X instead. Will be removed in v<future>.", error: true)]` | 1 MINOR version | Compiler error |
| 3. Remove | (deleted) | — | Symbol no longer exists |

### Rules

- Each stage MUST be announced in CHANGELOG under that release's "Deprecated" / "Removed" section.
- The `[Obsolete]` message MUST name the replacement and the target removal version.
- During Stage 1, the old API continues to behave identically. No behavior change.
- During Stage 2, source builds fail but the symbol still exists (so binary-only consumers and reflection paths keep working).
- During Stage 3, the symbol is gone.
- Renames are deprecations: ship the new name, mark the old name `[Obsolete]`, point to the new name.

### Codemod requirement

Any rename or signature change that affects ≥5 call sites in the RevCore-using projects should ship with a Roslyn analyzer + code fix so consumers can apply Quick Fix from their IDE. Track in the PR's "Migration" section.

### Exceptions

- **Security fix that requires breaking change**: may skip Stage 1; goes directly to Stage 2 with a release note flagged "SECURITY".
- **Pre-1.0 only**: Stage 1 may be shortened to "released and announced in the same MINOR as the breaking change, removed in the next." Post-1.0, the full schedule applies.
- All exceptions documented explicitly in CHANGELOG.

### Example

```csharp
// v0.5: introduce new
public static bool TryHexToColor(string hex, out Color color) { ... }

// v0.5: deprecate old (Stage 1)
[Obsolete("Use TryHexToColor. Will be removed in v1.0.", error: false)]
public static Color HexToColor(string hex) { ... }

// v0.7: harden (Stage 2)
[Obsolete("Use TryHexToColor. Will be removed in v1.0.", error: true)]
public static Color HexToColor(string hex) { ... }

// v1.0: remove (Stage 3) — method deleted
```
