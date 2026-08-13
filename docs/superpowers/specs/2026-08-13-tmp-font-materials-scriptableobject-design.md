# TMPFontMaterials ScriptableObject Support Design

**Date:** 2026-08-13

## Scope

Extend legacy `RCore.Inspector.TMPFontMaterialsAttribute` only. Do not modify RevCore in this change.

## Behavior

`[TMPFontMaterials]` remains valid only on `Material` fields.

- When inspected target is a `Component`, retain existing behavior: find a child `TMP_Text`, obtain its `TMP_FontAsset`, then list project materials sharing its atlas texture.
- When inspected target is a `ScriptableObject`, list every project `Material` whose shader name starts with `TextMeshPro/`.
- When no matching material exists, draw an empty popup without changing serialized value.
- For unsupported target types or a component without a `TMP_Text`, show contextual help text.

## Implementation

Keep `TMPFontMaterialsAttribute` public API unchanged. Add one editor-only material lookup for project-wide TMP materials, cached separately from per-font atlas results and cleared under existing reload/play-mode cache lifecycle.

Detect TMP material without new dependency: `material.shader != null && material.shader.name.StartsWith("TextMeshPro/")`.

## Tests

Add an EditMode test covering project-wide TMP material filtering using material assets with TMP and non-TMP shaders. Existing component behavior must remain unchanged.

## RevCore Follow-up

`RevCore.Inspector` has no TextMeshPro dependency. Migration map must preserve this as a future port item:

- Port `TMPFontMaterialsAttribute` and drawer to RevCore only after package ownership is chosen.
- Preserve `Component` font-atlas filtering and `ScriptableObject` project-wide TMP material dropdown.
- Prefer `RevCore.UI` ownership if TMP dependency should remain outside `RevCore.Inspector`; otherwise declare explicit `com.unity.textmeshpro` dependency.

## Out of Scope

- No RevCore package, asmdef, dependency, API inventory, or module changelog changes.
- No new attribute, configuration, or font-field reference mechanism.
