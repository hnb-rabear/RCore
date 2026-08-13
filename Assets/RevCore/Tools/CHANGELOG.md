# Changelog

## [Unreleased]

### Removed

- **Asset Cleaner**: Removed `AssetCleanerWindow` and `AssetCleanerSettings` from RevCore Tools. Use RCore.Main Asset Cleaner for maintained cleaner and reference-scanning workflows.

### Migration

- Replace `RevCore.Tools.Editor.AssetCleanerWindow` and `RevCore.Tools.Editor.AssetCleanerSettings` usage with RCore.Main Asset Cleaner tooling. No source-compatible replacement API is provided.

## [1.0.0] - 2026-05-14

### Added
- RevCoreTool base class with reflection-based hub discovery
- RevCore Tools Hub window (RevCore > Tools Hub)
- Navigate: Scenes Navigator, Asset Shortcuts, Project Explorer
- Search: Find By GUID, Component Reference Finder, Objects Finder (Script/ParticleSystem/PersistentEvent)
- Generators: Screenshot Taker, Characters Set Generator
- UI Tools: Toggle Raycast All
- Utility: Auto Play First Scene, Asset Cleaner (removed in Unreleased)
- Addressables: Addressable Groups Colorizer (conditional, requires com.unity.addressables)
- Helpers: EditorPrefsValue, EditorGuiHelper, AssetPathHelper
