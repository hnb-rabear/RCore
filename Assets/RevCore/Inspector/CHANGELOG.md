# Changelog

## [Unreleased]

### Fixed

- `[AutoFill]` now fills null references and empty arrays/lists through explicit, undoable **RevCore Auto Fill** context-menu action. Removed unsafe inspector-draw mutation that could corrupt multi-object edits, overwrite manual arrays, repeatedly query AssetDatabase, or choose nondeterministic assets.

## [1.0.0] - 2026-05-13

### Added

- Package scaffold
- 14 inspector attributes: ReadOnly, Separator, Comment, Highlight, ShowIf, AutoFill, CreateScriptableObject, ExposeScriptableObject, FolderPath, SingleLayer, SpriteBox, TagSelector, DisplayEnum, InspectorButton
- Property drawers for all attributes
- MeshInfoEditor and MeshRendererEditor custom editors
- EditMode attribute tests
- README with API reference and usage examples
