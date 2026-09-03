# Changelog

## [1.0.1] - 2026-09-03

### Changed

- Visibility toggle now applies to entire selection when clicked object is part of it, toggling active state together in single undo step.

## [1.0.0] - 2026-08-21

Initial release. Extracted unchanged from `Assets/RCore/Main/Editor/RHierarchy/`.

### Added

- `RHierarchy` — Hierarchy row decorations: separators/row shading, activity toggle, component
  icons, child count, vertex count, tag, layer, static flags.
- `RHierarchySettings` and `RHierarchySettingsWindow` (`RCore > RHierarchy Settings`).

### Changed

- Namespace and assembly are now `RCore.RHierarchy.Editor`. All `RHierarchy_*` EditorPrefs keys are
  unchanged, so existing settings carry over with no migration.
