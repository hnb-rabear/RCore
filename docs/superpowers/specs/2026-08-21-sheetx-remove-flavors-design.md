# SheetX Single-Flavor Design

## Goal

Remove `SX_LOCALIZATION`, `SX_LITE`, and `SX_NO_LOCALIZATION` compile-time variants. SheetX becomes one full editor exporter: IDs, constants, JSON, localizations, plus single-file and multi-file Excel/Google Sheet workflows.

## Scope

- Delete every conditional-compilation branch controlled by those three symbols under `Assets/RCore.SheetX/Editor/`.
- Preserve default branch behavior from current source.
- Keep `ASSETS_STORE` handling unchanged.
- Do not rename serialized `SheetXSettings` fields or change generated-file formats.
- Do not change SheetX menu path, OAuth credential storage, package version, or unrelated code.

## Resulting Behavior

`SheetXConstants.APPLICATION_NAME` is always `SheetX - Sheets Exporter`.

When `ASSETS_STORE` is defined, settings remain at `Assets/SheetX/Editor/SheetXSettings.asset`; otherwise they remain at `Assets/SheetX/SheetXSettings.asset`.

Main window always uses `SheetX` menu name and exposes Excel/Google single-file and multi-file tabs. Both tabs expose Export All, IDs, Constants, JSON, and Localizations where applicable.

Settings always expose script, JSON, and localization output folders; ID, constants, localization, JSON options; persistent fields; and Google OAuth fields.

Excel and Google handlers always compile and run Constants and Localizations exports.

## Migration

No asset migration. Existing `SheetXSettings.asset` field names and paths stay unchanged. Projects with any removed scripting define continue compiling because Unity ignores unused symbols, but those symbols no longer alter SheetX behavior.

## Validation

1. Search `Assets/RCore.SheetX/Editor` for all three symbols. Expected: zero results. Changelogs may retain symbol names to document removal.
2. Unity EditMode: Run All. Fix failures caused by change.
3. Manual SheetX acceptance:
   - Open `SheetX` window.
   - Confirm Excel and Google views show Export Single File and Export Multi Files tabs.
   - Confirm Settings shows localization output and related fields.
   - Confirm single-file export controls include Localizations.
4. Update root `CHANGELOG.md` before commit, as required by project convention.
