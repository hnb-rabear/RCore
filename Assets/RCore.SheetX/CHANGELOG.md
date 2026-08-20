# Changelog

## [Unreleased]

### Fixed

- `Export IDs` honoured the "Separate Constants Sheets" toggle instead of "Separate IDs
  Sheets" in both the Excel and Google exporters. **If you had those two toggles set
  differently, the IDs output layout changes with this release** — it now matches what
  `Export All` already produced.
- Google `Export IDs` wrote the merged `IDs.cs` inside the per-sheet loop, so the file was
  rewritten once per sheet and the final content depended on sheet order.
- Settings no longer disappear after a fresh clone or a UPM package re-resolve. Two causes:
  - `SheetXSettings.Init()` searched `Packages/` as well as `Assets/`, so on a project that
    installs SheetX via git URL it resolved the copy shipped inside the package — which lives in
    the gitignored `Library/PackageCache/` and is rebuilt from git on every re-resolve. The search
    is now scoped to `Assets/`, and the default path is `Assets/SheetX/SheetXSettings.asset`.
  - Excel paths, Google sheet lists and sheet selections were mutated in memory but never written
    back to the asset. `SheetXWindow` now flushes on focus loss and on close.

## [1.0.2] - 2026-01-02
- Improved documentation
- Added support section in Settings Window with "Rate on Asset Store" and "Star on GitHub" buttons.

## [1.0.1] - 2026-01-01
- Maintenance update.

## [1.0.0]
- Initial release.
