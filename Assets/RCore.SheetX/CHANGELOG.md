# Changelog

## [1.1.0] - 2026-08-18
- Added Localization Scene View overlay for switching language directly from Scene View via dropdown and `<`/`>` arrows, without entering Play Mode. Discovers generated localization classes by reflection — no template change or code regeneration needed. Refreshes all matching text components in loaded scenes and Prefab Stage.
- Improved missing localization ID diagnostics: `OnValidate` error now reports component type, missing ID, GameObject name, full hierarchy path, and source asset/scene. Double-clicking the Console entry pings the offending object.

## [1.0.2] - 2026-01-02
- Improved documentation
- Added support section in Settings Window with "Rate on Asset Store" and "Star on GitHub" buttons.

## [1.0.1] - 2026-01-01
- Maintenance update.

## [1.0.0]
- Initial release.
