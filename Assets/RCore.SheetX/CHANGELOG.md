# Changelog

## [Unreleased]

### Added

- Optional Data Config Collections: interactive Excel and Google exports can generate typed collection shells, bake editor-time JSON into serialized ScriptableObject arrays, and create a Global Resources root with feature references. Generated Data Class infers `int`, `float`, `bool`, or `string` from longest non-empty column cells; optional header annotations override inference. Runtime reads serialized data only; it never parses collection JSON. `autoLoadAfterExport` and `autoLoadBeforePlay` respect per-collection Auto Load. Collection metadata remains unsupported by detached `SheetXExporter` and batch APIs.

### Changed

- Generated collection source is now named `SheetXDataCollections.cs` instead of `SheetXDataCollections.g.cs`. It contains row models and JSON path constants; every collection `ScriptableObject` now lives in its matching `GlobalConfigCollection.cs` or `<Name>ConfigCollection.cs`, so Unity can display its Mono Script. Every generated file carries the SheetX banner; successful export removes the legacy `.g.cs` file to prevent duplicate generated types.
- Collection JSON no longer requires an `Editor` path segment. Any project-relative `Assets/` folder is allowed except paths under `Resources` or `StreamingAssets`, whose contents Unity includes in player builds.

- Interactive Excel and Google exports route exact ordinal `Configuration` worksheets to fixed plaintext `Configuration.txt` and `Configuration.cs`, then create or reuse `Configuration.asset` after script reload. Single exports ignore Configuration selection. Multi-file exports merge physical Configuration sheets from selected sources in source-list order, keeping duplicate data. Configuration stays outside combined JSON. `Config` remains ordinary row-array JSON. Detached and batch APIs retain ordinary row-array behavior for both names.

### Fixed

- Existing collection assets with a missing `m_Script` reference now bind to their matching generated `MonoScript` during bake without recreating the asset or discarding serialized data.
- Generated Data Class now ignores any header containing `[x]`, skips exact C# keyword path segments with actionable warnings, and preserves source-column alignment after ignored fields. Malformed headers, invalid values, binding errors, and generated-name collisions now log once and skip only offending sheet; later valid sheets continue. Accepted JSON and generated source still write atomically, skipped JSON stays untouched and is excluded from current automatic bake. A missing Collection binding from a processed source still aborts collection flush, while bindings from unrelated, unprocessed sources no longer block the current export. Generated source contains accepted current-session bindings only.
- Generated Data Class now resolves exact symbolic ID keys from `*IDs` sheets before type inference and JSON emission, including array items, for both Excel and Google exports. Explicit header annotations still control generated field types; embedded or unknown text stays unchanged.
- Global Collection asset now saves before feature assets after references are assigned, preventing later asset saves from clearing a same-bake Global feature reference.
- `SheetXExporter.ExportExcel` now owns one named, read-only `MemoryStream` through the complete export. NPOI can read workbook parts lazily, so its source stream no longer depends on garbage collection or closes before later selected sheets are read.
- Per-sheet localization artifacts now have distinct types: language `.txt` data remains `Localization`, `{file}.cs` is `LocalizationConstants`, and `{file}Text.cs` is `LocalizationComponent`. Excel and Google handlers route identically; regression coverage runs on Excel only because Google export requires OAuth and network access.

## [1.4.0] - 2026-08-23

### Added

- `SheetXExporter.ExportBatch(SheetXBatchExportRequest, ISheetXOutput)` exports detached Excel and Google source lists through one caller-owned sink. Batch sources share output options, request-provided Google credentials, and one global symbolic-ID namespace; `Sources` membership enables a source.
- Batch selection keeps source order and each source's native sheet order: `Sheets == null` selects all, an empty list selects none, and a requested missing sheet becomes a returned error.

### Changed

- Batch export materializes and validates every source before artifacts reach `ISheetXOutput`. It stages artifacts, flushes only with zero errors, and records sink writes in order; a sink failure records accepted earlier artifacts, stops later writes, and returns an error.
- Batch duplicate symbolic IDs are errors even when values match; first definition remains available for resolution and diagnostics name both origins. Path collisions and duplicate combined-JSON output names also return errors before sink output.
- `ExportBatch` never reads or writes the Settings asset or `EditorPrefs`; credentials are request-only and never persisted by the batch API.

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
