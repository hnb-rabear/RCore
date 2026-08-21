# Changelog

## [Unreleased]

### Fixed

- `SheetXExporter.ExportExcel` now owns one named, read-only `MemoryStream` through the complete export. NPOI can read workbook parts lazily, so its source stream no longer depends on garbage collection or closes before later selected sheets are read.
- Per-sheet localization artifacts now have distinct types: language `.txt` data remains `Localization`, `{file}.cs` is `LocalizationConstants`, and `{file}Text.cs` is `LocalizationComponent`. Excel and Google handlers route identically; regression coverage runs on Excel only because Google export requires OAuth and network access.

## [1.3.0] - 2026-08-21

### Added

- `SheetXExporter` / `ISheetXOutput` / `SheetXExportRequest` / `SheetXExportResult` — public editor API for running an Excel or Google export without the Settings window, the `.sx` asset, or any `EditorPrefs` state. See `Document/Document.md` section 6. Every artifact travels through the caller's `ISheetXOutput.Write(relativePath, content)` exactly once; the exporter never calls `EditorUtility.DisplayDialog`, `Debug.Log`, `File.WriteAllText`, or `AssetDatabase`. `Sheets == null` exports every sheet, an empty list exports none (Google additionally skips OAuth and any network call).

### Fixed

- Culture-invariant numeric parsing and generated numeric text (previously used the current culture, so `"1,5"` could parse as a number under `de-DE`).
- Deterministic longest-key-first, ordinal ID substitution in generated content.
- Empty duplicate-name JSON columns now emit a valid `[]` instead of malformed JSON.
- One workbook opened per Excel export instead of one per sheet.
- A duplicate ID with a conflicting value now reports an error and keeps the first definition; it no longer appends a second `public const int` for the same key, which produced C# that failed to compile. Applies to both the Excel and Google handlers.
- An `IDs` sheet containing only a header row no longer produces an artifact, in both the Excel and Google handlers.
- Duplicate sheet names in a Google export request no longer produce duplicate artifacts for the same sheet.
- The `encryptJson` default-key warning is preserved when a caller omits `EncryptionKey` on the public request.

### Security

- The Google OAuth token cache moved from `Assets/Editor/` to `Library/SheetX`, outside Unity's asset pipeline and outside version control. `SheetXHelper.GetSaveDirectory()` is deprecated in favor of `GetTokenStoreDirectory()`.
- Google OAuth credentials are now keyed in `EditorPrefs` by project path instead of `Application.identifier`, which changes per build flavor and could silently split one team's credentials across keys. Existing values under the old key migrate on first read.

## [1.2.0] - 2026-08-21

### Removed

- Removed conditional compilation symbols `SX_LOCALIZATION`, `SX_LITE`, and `SX_NO_LOCALIZATION`. SheetX is now a unified single-flavor exporter (IDs, Constants, JSON, Localizations, Single & Multi-file, for both Excel and Google Sheets). `ASSETS_STORE` is unaffected — it still gates only the settings asset file path.

  **If a project still defines any of the three symbols, that define is now inert** — no compile error, but the features it used to strip are present again. Every exporter, tab, and menu ships unconditionally. Remove the stale defines from `Player Settings > Scripting Define Symbols`; nothing replaces them.

### Added

- `SheetXSettingsTests.no_legacy_flavor_defines_exist_in_editor_scripts` — fails if any of the three symbols is reintroduced into `Assets/RCore.SheetX/Editor`.

## [1.1.0] - 2026-08-21

### Breaking

- **`Export IDs` output layout changes if `Separate IDs Sheets` and `Separate Constants Sheets`
  were set differently.** Both exporters read the wrong toggle, so `Export IDs` was laying out
  its output by the Constants setting. It now honours `Separate IDs Sheets`, matching what
  `Export All` already produced from the same settings.

  | `Separate IDs Sheets` | `Separate Constants Sheets` | Before | After |
  | --- | --- | --- | --- |
  | false | true | one file per ID sheet | one merged `IDs.cs` |
  | true | false | one merged `IDs.cs` | one file per ID sheet |

  The other two combinations are unaffected. **Migration:** re-run `Export IDs` and commit the
  regenerated files. If you preferred the old layout, flip `Separate IDs Sheets` to what
  `Separate Constants Sheets` was.

  *Semver note.* `docs/contributing/SEMVER_POLICY.md` classes an observable-behavior change that
  callers reasonably depend on as breaking, which would call for 2.0.0. This ships as MINOR by
  deliberate exception: no public symbol changes shape, and the alternative is a MAJOR bump for
  every consumer over a bug fix that makes two buttons agree with each other.

### Added

- Localization Scene View overlay for switching language directly from Scene View via dropdown and
  `<`/`>` arrows, without entering Play Mode. Discovers generated localization classes by reflection
  — no template change or code regeneration needed. Refreshes all matching text components in loaded
  scenes and Prefab Stage.

### Security

**Rotate your Google OAuth client secret when convenient.** If your `SheetXSettings.asset` was
ever committed with credentials in it, those credentials are in your git history, and the
obfuscation key is public. Blanking the asset does not un-publish them; only rotating in Google
Cloud Console does.

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
  is no longer tracked by git and is now covered by `.gitignore`. It holds an OAuth token cache,
  an already-granted authorization rather than a challenge, so it was more sensitive of two
  exposures.

### Fixed

- Missing localization ID diagnostics: `OnValidate` error now reports component type, missing ID,
  GameObject name, full hierarchy path, and source asset/scene. Double-clicking the Console entry
  pings the offending object.
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

### Changed

- Removed the empty `Samples~` folder — it contained one stray `.meta` file and no `samples`
  entry in `package.json`, so Package Manager never offered it. `Document/Document.md` is the
  onboarding path.

## [1.0.2] - 2026-01-02
- Improved documentation
- Added support section in Settings Window with "Rate on Asset Store" and "Star on GitHub" buttons.

## [1.0.1] - 2026-01-01
- Maintenance update.

## [1.0.0]
- Initial release.
