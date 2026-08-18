# Changelog

## [Unreleased]

### Added

- **Localization Scene View overlay** — switch localization language directly from the Scene View via a dropdown plus `<`/`>` arrows, without entering Play Mode. Discovers generated localization classes and their `{Class}Text` components by reflection, so no template change or code regeneration is needed. On switch it sets `CurrentLanguage`, forces `InitInEditor()` to reload, and refreshes every matching text component in all loaded scenes and the open Prefab Stage.

### Fixed

- `LocalizationTextTemplate`: the `OnValidate` missing-ID error now reports the generated component type, the missing ID, the GameObject name, the full slash-separated hierarchy path, and the source prefab asset or scene, and passes the component as the log context so double-clicking the Console entry pings the offending object. Generated localization components pick this up on the next SheetX regeneration.

## [1.0.2] - 2026-01-02
- Improved documentation
- Added support section in Settings Window with "Rate on Asset Store" and "Star on GitHub" buttons.

## [1.0.1] - 2026-01-01
- Maintenance update.

## [1.0.0]
- Initial release.
