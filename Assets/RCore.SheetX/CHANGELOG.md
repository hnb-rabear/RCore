# Changelog

## [Unreleased]

### Security

- Google OAuth client ID and secret now live in `EditorPrefs`, per machine, instead of as
  serialized fields on the settings asset. A serialized field on a committed asset is a file
  in a git repository, and the XOR obfuscation around it used a key published in this same
  repository. **On first launch after upgrading, an existing credential is migrated out of the
  asset automatically and the asset fields are blanked**; a field whose decryption does not
  yield a plausible credential is left untouched and warned about rather than destroyed.
  Everything the settings asset still holds — Excel paths, Google sheet lists, output folders,
  toggles — is safe to commit, which is the point of doing both.
- `.sx` settings files no longer carry credentials. Loading an *old* `.sx` drains its
  credentials into `EditorPrefs` and clears them, so they are not silently re-committed.
- `GetEncryption()` now warns once when `encryptJson` is on and `encryptionKey` is still the
  key shipped with this package. That key is published here, so output encrypted with it is
  decryptable by anyone.
- The Google OAuth token store (`Assets/Editor/Google.Apis.Auth.OAuth2.Responses.TokenResponse-user`)
  is no longer tracked by git and is now covered by `.gitignore`. It holds a `refresh_token` —
  an already-granted authorization, not a challenge — so it was the more sensitive of the two
  exposures.

### Fixed

- `Export IDs` honoured the "Separate Constants Sheets" toggle instead of "Separate IDs
  Sheets" in both the Excel and Google exporters. **If you had those two toggles set
  differently, the IDs output layout changes with this release** — it now matches what
  `Export All` already produced.
- Google `Export IDs` wrote the merged `IDs.cs` inside the per-sheet loop, so the file was
  rewritten once per sheet and the final content depended on sheet order.
- Localization export threw on a blank header cell (Excel) or a short row (Google Sheets).
- Google IDs and Constants export threw on a row whose trailing value cell was blank.
- Exporting an empty sheet threw instead of logging a warning.
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
