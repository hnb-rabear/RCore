## Semver Policy

RevCore follows [Semantic Versioning 2.0.0](https://semver.org/) with the conventions below.

### Versioning rules

| Pre-1.0.0 (current state) | Post-1.0.0 |
|---|---|
| MINOR may break API; document in CHANGELOG. | MINOR must NOT break public API. |
| PATCH never breaks API. | PATCH never breaks API. |
| No LTS guarantees. | `vX.Y-lts` branches may exist for 12 months. |

### What counts as "breaking"

Any of the following requires a MAJOR (post-1.0) or a MINOR with a breaking note (pre-1.0):

- Removing a public type, method, field, event, or property.
- Renaming any public symbol.
- Changing a method signature: parameter type, parameter order, return type, generic constraint.
- Changing a method's observable behavior in a way that existing callers would reasonably depend on (e.g., `HexToColor` going from "silent fallback" to "throw on invalid").
- Removing or renaming a serialized field on a `MonoBehaviour` or `ScriptableObject` that consumers may have authored data for.
- Changing the JSON shape produced by `JObjectData.ToJson()` in a way that breaks `FromJson` for older payloads (use schema versioning instead).
- Tightening a parameter nullability annotation (allowed → disallowed).
- Changing an enum's underlying type or removing an enum member.

### What does NOT count as breaking

- Adding a new public type, method, or overload (additive).
- Adding optional parameters at the end of a non-virtual method (additive — does not break source, does break binary, but Unity recompiles from source so we accept).
- Loosening a parameter nullability annotation (disallowed → allowed).
- Adding new enum members at the end (assuming no exhaustive switches in consumer code — document if risky).
- Renaming a parameter name (Roslyn warns on named-arg consumers; not a hard break).
- Internal refactors that preserve behavior.

### How to declare

- PR labelled `breaking` triggers Release Drafter to put it in the "BREAKING CHANGES" section.
- `PublicAPI.Unshipped.txt` is the source of truth for surface changes. CI fails if surface changes without matching diff in that file.
- Pre-1.0 minor releases with breaking changes must include a migration note in CHANGELOG with before/after code.

### Tag format

`vMAJOR.MINOR.PATCH[-prerelease]`. Examples: `v0.4.0`, `v1.0.0`, `v1.0.0-rc1`, `v1.1.0-beta.2`.
