# Changelog

## [1.0.0] - 2026-11-26

Initial release. Moved out of `rcore.main` (`Assets/RCore/Main/Editor/Tool/ReskinToolkit/`), where it was never pushed, so projects with any RCore version can use it.

### Added

- **`AtlasMigration`** (`RCore.Editor.Tool`): `Plan`, `WriteTps` and `Apply` move loose sprite textures to a packed TexturePacker atlas and keep each sprite's pivot and 9-slice border. Built for AI agents calling through UnityMCP; each call takes the loose folder, the atlas PNG and the `.tps`; `Plan` and `Apply` also take an optional scope.
  - `WriteTps` matches `.tps` entries by their path relative to the `.tps` folder, adds an entry for a sprite the `.tps` has none for, and reports stale entries TexturePacker ignores. It skips a file the `.tps` `fileList` does not pack and says so.
  - Pivots are read from the full sprite rect, so Tight-mesh sprites with transparent margins keep the right pivot.
  - The `.tps` is edited as XML text: every other setting and the file's line endings are kept, and unknown elements (`<true/>`, `<int>`, `<enum>`) are never dropped.
- **`WriteTpsForWindow`**: entry point for RCore's Replace Sprite window, which calls it by reflection.
- **AI skill** `Editor/AI/unity-texturepacker-atlas-migration/SKILL.md`, and **RCore → Tools → Install AI Skills**, which copies it into the project's `.claude/skills/`.
