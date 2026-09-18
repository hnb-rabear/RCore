# Changelog

## [Unreleased]

## [1.10.0] - 2026-09-18

### Added

- `Structure` column in the sheet table of the `Excel Spreadsheets`, `Google Spreadsheets`, and both `Edit Spreadsheets` windows. One button per ordinary data sheet opens a `Sheet Structure` window showing the C# structure that sheet corresponds to, with `Refresh` and `Copy Code`. It reads the spreadsheet without writing a file, creating a binding, or changing a setting, and is available whether or not Collections is enabled. Sheets whose name ends in `IDs`, `Constants`, or `Settings`, or begins with `Localization`, have no button, nor does a sheet named exactly `Configuration` — export writes that one as a typed configuration class rather than a row array.
- `Generated Data Class` sheets show the real generated class, labelled `Generated class preview`. `JSON Only` and `Existing Data Class` sheets show an `Inferred draft from exported JSON` built from the JSON the exporter would produce: blank cells are omitted from exported JSON, so a column empty in every row does not appear at all, and inferred types are suggestions read from the values present rather than declarations. The preview reads one spreadsheet, so symbolic IDs resolve against that file's own `*IDs` sheets; a multi-file export shares IDs across sources and can resolve them differently. The window states all three limits.
- Member check for `Existing Data Class` sheets: exported JSON keys with no destination in the selected class are reported as `Unmatched JSON member '<name>'`, because Newtonsoft discards an unknown key silently. `Not present in exported data` is informational — a class member absent from every row proves nothing when blank cells are omitted. A class that cannot be modelled (custom converter, non-object contract, extension data, no row type selected) reports `Member coverage cannot be verified` with the reason instead of guessing. Top-level only this release; nested members are not compared.
- The snapshot is per-window and taken on open, never cached or shared. A failed `Refresh` keeps the previous code on screen under `Stale — last refresh failed` rather than presenting it as current. The header carries a `Fetched` row with the date and local time of the snapshot on screen, so a window left open is never mistaken for a fresh read; a failed refresh keeps the kept snapshot's original timestamp.
- Inferred drafts explain themselves: a three-line header stating the code is inferred rather than a recovered sheet schema, and per-field comments for the two cases a reader cannot guess — a JSON key that had to be renamed to be a valid C# identifier (the comment names the exact exported key, never a spreadsheet header, which the legacy Attribute System makes unrecoverable from JSON), and a type that fell back to `object` or `object[]`, carrying the reason inference recorded. Ordinary declarations get no comment. `Generated Data Class` emission is unchanged byte for byte — schema-backed output takes no draft commentary.
- `Top-level fields` table above the code: `JSON key`, `C# field`, `Type`, with `*` marking an inferred fallback and the reason in its tooltip. Top-level only; nested types remain visible in the code as supporting classes. Populated from metadata the snapshot already carries, so a repaint neither parses JSON nor re-runs inference. `Generated Data Class` shows a one-line note pointing at the code instead.
- `Class Code` and `JSON` tabs for snapshots that carry exported JSON. `Generated Data Class` has no legacy JSON and shows no JSON tab. The JSON is pretty-printed once per snapshot, not per repaint or Data Class change, and date-like strings stay strings. The view renders at most 50,000 characters: raw input already past the cap is excerpted unformatted as `Large JSON: showing a raw excerpt` rather than being formatted only to be discarded, and input that only exceeds the cap once indented is cut with its own note; both say the excerpt is incomplete and will not parse as JSON alone. The stored `Json` used by the member check is never shortened, and `Copy Code` still copies code only.
- Validator messages now say what to do. An unmatched member states the values are discarded on load and names both fixes, and where the draft inferred a declaration it is offered as a suggestion rather than a guaranteed fix — with its `JsonProperty` mapping when the key was renamed, and a caution that an `object`/`object[]` fallback is not Unity-serializable as-is. An unobserved member stays informational and no longer implies what it holds after loading. An unverifiable contract keeps its specific reason and says coverage can be neither confirmed nor denied. A conversion failure keeps its precedence over the clean result and points at the value and the declared type. No classification or precedence changed.
- Preview ID scope now follows the tab that opened it, so it predicts the export sitting next to it. A preview opened from an `Export Single File` tab resolves symbolic IDs against that one source, as that tab's `Export All` does; a preview opened from an `Export Multi Files` tab — including the per-source `Edit Spreadsheets` window its `Select` button opens — resolves them across every selected source in that list, in list order, first definition winning, as `Export All` does there. The same file or spreadsheet listed in both places keeps each tab's own scope, so previewing it from the Single tab no longer borrows the list's IDs. The window's last help line names whichever scope was actually used. For Google, the multi-source ID map is built once per session and rebuilt by `Refresh`, since it costs one connection plus one range read per `*IDs` sheet per listed spreadsheet.

### Fixed

- Row-type findings in the `Sheet Structure` window no longer go stale when the Data Class changes. Every row-type-dependent message — missing row type name, type not found, type rejected by validation, and JSON that does not map onto the type — now travels on its own snapshot member and is re-derived live against the binding selected right now, so a warning naming a class the user just replaced can no longer sit above a clean result for the new one. Generic source and inference warnings are unaffected, including any whose text happens to resemble a row-type message.
- The `Sheet Structure` member check now validates row-type eligibility before reporting coverage. A type the export would refuse — missing `[SheetXBindable]` or `[Serializable]`, not public, abstract, or generic — reported a clean match whenever its member names happened to line up; the block is now replaced by a single error carrying the rejection's own reason and `Export skips this sheet until this is fixed.`, because export skips the sheet outright. A bound type that no longer resolves is reported the same way, naming it. This is deliberately not the `Member coverage cannot be verified` arm, whose reassuring wording would be untrue for a sheet that will not export.
- The member check now follows the real deserializer instead of a fixed default contract. It resolves through `JsonSerializer.CreateDefault()` on every comparison, so a project that sets `JsonConvert.DefaultSettings` — a snake_case naming strategy, for instance — is honoured, including a change made after an earlier check ran; a reading `JsonConverter` supplied through those settings now yields `Member coverage cannot be verified` rather than a guess. A get-only collection or nested object, which Newtonsoft may populate in place depending on the instance the getter returns, is reported as unverifiable instead of being falsely accused of discarding data — previously this raised an `Export Anyway` prompt for data the export was keeping. A get-only scalar can never receive a value and is still reported as unmatched; creator parameters and ignored members are unchanged. Export's own deserialization behavior is unchanged.
- An invalid `Collection namespace` is now a preview error in `Generated Data Class` mode, as it already was in the legacy modes. The generator appends the namespace unchecked, so this branch previously showed uncompilable code labelled `Generated class preview` as a clean success — for settings under which the export itself writes nothing. A whitespace-only namespace is rejected rather than treated as absent; an empty namespace still means "emit no namespace".

### Changed

- **A collection export now stops to ask when an `Existing Data Class` sheet would drop data.** If any accepted sheet's exported JSON carries members the selected class cannot receive, SheetX shows one aggregated `Existing Data Class members` dialog — `Export Anyway` / `Cancel` — listing every offending source, sheet, class, and member, before the collection export writes anything. `Cancel` aborts the collection step with `Collection export cancelled: unmatched Existing Data Class members were not confirmed.`, writing no collection JSON and no generated collection source, so there is no half-written collection output; the collection sheets that were fine are cancelled with it. **`Cancel` does not undo earlier stages of the same run** — the collection step runs after ordinary JSON, and in `Export All` after IDs and Constants, and those artifacts are already on disk and stay there. `Export Anyway` writes byte-identical JSON to previous versions. Every finding is logged as a warning whether or not the dialog appears. Batch and headless runs show no dialog for this check: the same warnings are logged and the collection export continues — this is specific to the member mismatch and does not relax the headless storage-mode abort or a row-type conversion failure, and the detached `SheetXExporter` and batch APIs carry no collection bindings so the check never arises there. **A column that has been silently dropping values for months will now interrupt an export that previously ran clean** — add the missing members to the class, or rename the sheet columns to match it.

## [1.9.0] - 2026-09-15

### Fixed

- Inline collections now show Global's effective `Auto Load` value, including when Global Auto Load is disabled, so their locked checkbox matches bake behavior.
- Restore Migration Snapshot now offers Discard, which removes an abandoned snapshot without restoring sources so later migrations can capture their own rollback data.

### Added

- Per-collection storage mode. Each collection now chooses between `Separate Asset` (its own `.asset`, referenced from Global — the previous and still default behaviour) and `Inline` (serialized inside `GlobalConfigCollection.asset`, no separate file). Game code reads the same path in both modes: `global.player.Characters`.
- A collection changing storage mode now asks for confirmation before its generated source is replaced, and the sources it replaces are saved outside `Assets/` so a bake that fails after the domain reload can be undone.

### Changed

- An `Inline` collection has no asset, so it has no `IsLoaded` and cannot be assigned to a `ScriptableObject` field. It is loaded exactly when Global is, and its `Auto Load` follows Global's.
- `RCore > SheetX: Restore Migration Snapshot` now names the collections it would revert and asks for confirmation, so a snapshot left over from an abandoned migration cannot silently revert the wrong export. Restore puts back generated sources only: Global's reference to the restored collection stays empty until the next bake (`Manage Collections > Load All Collections`, or export again).
- Restoring a migration snapshot writes each source through the same write-beside-and-swap path as every other generated file, so an editor crash mid-restore cannot leave a truncated or empty `.cs` behind, and the restored file carries no BOM.
- The migration snapshot is retired only by a post-reload bake sweep that succeeds for the settings asset the migration was captured from. A later unrelated export of a different settings asset no longer deletes a still-needed snapshot.
- Switching a collection to `Inline` leaves its previous `.asset` in place rather than deleting it. The asset stops being baked, but switching back reuses the same file and GUID so existing references resolve again. **A `[SerializeField]` of that collection's type still compiles after the switch and silently becomes an empty inline copy — those fields have to be found and fixed by hand.**

## [1.8.0] - 2026-09-09

Correctness release. No new features; every entry is a defect that produced a wrong or unusable artifact.

### Fixed

- Generated JSON string values were not escaped for backslashes, carriage returns or tabs; array text values were not escaped at all.
- Generated JSON was written without being validated.
- A trailing column whose header contained "attribute" crashed the JSON export.
- Empty and numeric-header columns were dropped from exported JSON, defeating `persistentFields`.
- ID caches persisted across export buttons within one window session, so a second export could substitute IDs from the first spreadsheet.
- Duplicate IDs resolved to different values in generated code and in JSON. Both now keep the first row.
- Short vector constants crashed the Constants export; string constants were not escaped.
- Constants named `NEW_LINE` corrupted the generated file.
- Localization emitted duplicate `switch` arms and invalid C# identifiers without reporting them. Two columns mapping to the same `SystemLanguage` and a key that is not an identifier are now errors.
- Google "Export All" exported IDs from unchecked spreadsheets.
- Generated files were written non-atomically and carried a UTF-8 BOM. Writes now go through a temp file and swap, without a BOM.
- Empty sheets disappeared from combined JSON instead of appearing as an empty array.
- A malformed `encryptionKey` (a token outside 0-255, or non-numeric) silently fell back to SheetX's published default key, so "encrypted" output was readable by anyone. It is now reported and the export fails.

## [1.7.1] - 2026-09-07

### Added

- `Export Multi Files` tab now accepts drag & drop onto a drop area above the Excel path table, adding several spreadsheets in one gesture. Dropping a folder adds its top-level `.xlsx` files; non-Excel and missing paths are skipped. Unity's file dialog is single-select, so the `Add Excel SpreadSheets` button still adds one file at a time.
- `Add Folder` button in the `Export Multi Files` tab adds every top-level `.xlsx` file in a chosen folder, for adding a whole spreadsheet directory without dragging.

## [1.7.0] - 2026-09-03

### Added

- `[SheetXBindable]` (`RCore.SheetX.SheetXBindableAttribute`) — the marker a class or struct carries to be selectable as an `Existing Data Class` row type. It ships in the auto-referenced runtime assembly, so game code needs no asmdef change. `SheetXRowType.Validate` is the single rule the Data Class picker, export, and bake all apply, so a type offered in the dropdown is guaranteed to export and bake.
- Added full Vietnamese documentation (`Document_VN.md`) with bilingual navigation links between English and Vietnamese guides.
- Added comprehensive documentation for Enum definitions in `IDs` sheets (`[enum]` group suffix), `onlyEnumAsIDs` setting, and automatic symbolic ID resolution in data table JSON export.

### Changed

- `Existing Data Class` row types must now carry `[SheetXBindable]` in addition to `[Serializable]`. The Data Class dropdown previously listed every serializable type in the project and now lists only marked types, showing `No [SheetXBindable] type found` when a project has marked none. Export and bake reject an unmarked type with a message naming the fix. Structs are now valid row types alongside classes.
- A row type must also be publicly visible: `public`, and, when nested, nested only inside `public` types. The generated collection declares a `public` field of the row type, so an `internal` or privately nested type would break the consuming project's compile (CS0122 / CS0052) — `internal` fails even when the generated file lands in the same assembly. The picker, export, and bake all reject such a type with a message naming the requirement.

  **Migration:** add `[RCore.SheetX.SheetXBindable]` to every class or struct already bound as an `Existing Data Class`, and make it — plus every type it is nested in — `public`. An unmarked or non-public type disappears from the picker and is rejected at export and bake.

### Fixed

- `Existing Data Class` export rejected every row type. `SheetXCollectionExportSession.TryResolveRowType` tested for `[Serializable]` with `Type.IsDefined`, which never matches because the compiler emits that attribute as the `TypeAttributes.Serializable` metadata flag rather than a custom-attribute entry. Validation now runs through `SheetXRowType.Validate`, which reads `Type.IsSerializable`.
- Data Class dropdown in the Edit Spreadsheets windows was always empty, for the same `[Serializable]` reason: `TypeCache.GetTypesWithAttribute<SerializableAttribute>()` never matches. The picker now lists `[SheetXBindable]` types through `TypeCache`, filtered by `SheetXRowType.Validate`.
- Data Class dropdown no longer closes immediately on click. `EditExcelSheetsWindow` and `EditGoogleSheetsWindow` called `Focus()` from `OnLostFocus`, which stole focus back from the `AdvancedDropdown` popup and dismissed it; both overrides are removed.

## [1.6.0] - 2026-08-27

### Added

- Settings tab now checks SheetX's remote GitHub `package.json`, caches latest version in `EditorPrefs`, displays installed/latest versions and a `NEW` badge, and updates UPM Git or registry installs with `Client.Add()`. Source, embedded, and local installs stay read-only with matching badges.
- Optional Data Config Collections: interactive Excel and Google exports can generate typed collection shells, bake editor-time JSON into serialized ScriptableObject arrays, and create a Global Resources root with feature references. Generated Data Class infers `int`, `float`, `bool`, or `string` from longest non-empty column cells; optional header annotations override inference. Runtime reads serialized data only; it never parses collection JSON. `autoLoadAfterExport` and `autoLoadBeforePlay` respect per-collection Auto Load. Collection metadata remains unsupported by detached `SheetXExporter` and batch APIs.

### Changed

- Generated collection source is now named `SheetXDataCollections.cs` instead of `SheetXDataCollections.g.cs`. It contains row models and JSON path constants; every collection `ScriptableObject` now lives in its matching `GlobalConfigCollection.cs` or `<Name>ConfigCollection.cs`, so Unity can display its Mono Script. Every generated file carries the SheetX banner; successful export removes the legacy `.g.cs` file to prevent duplicate generated types.
- Collection JSON no longer requires an `Editor` path segment. Any project-relative `Assets/` folder is allowed except paths under `Resources` or `StreamingAssets`, whose contents Unity includes in player builds.

- When Data Config Collections is disabled, interactive Excel and Google exports route exact ordinal `Configuration` worksheets to fixed plaintext `Configuration.txt` and `Configuration.cs`, then create or reuse `Configuration.asset` after script reload. Single exports ignore Configuration selection. Multi-file exports merge physical Configuration sheets from selected sources in source-list order, keeping duplicate data. Configuration stays outside combined JSON. `Config` remains ordinary row-array JSON. Detached and batch APIs retain ordinary row-array behavior for both names.
- When Data Config Collections is enabled, exact `Configuration` becomes automatic direct Global schema/value input. The row is checked-disabled with read-only `Automatic / Global / GlobalConfigCollection` controls; plaintext JSON goes to Collection JSON Folder, `SheetXCollectionPaths.Configuration` drives bake, and strict collisions abort atomic Collection output with rollback. Standalone Configuration artifacts remain dormant and untouched. Detached and batch APIs retain row-array behavior.

### Fixed

- Compacted Scene View Localization overlay UI by removing redundant current-language label and setting fixed 64px dropdown width.
- Existing collection assets with a missing `m_Script` reference now bind to their matching generated `MonoScript` during bake without recreating the asset or discarding serialized data.
- Generated Data Class now ignores any header containing `[x]`, skips exact C# keyword path segments with actionable warnings, and preserves source-column alignment after ignored fields. Malformed headers, invalid values, binding errors, and generated-name collisions now log once and skip only offending sheet; later valid sheets continue. Accepted JSON and generated source still write atomically, skipped JSON stays untouched and is excluded from current automatic bake. A missing Collection binding from a processed source still aborts collection flush, while bindings from unrelated, unprocessed sources no longer block the current export. Generated source contains accepted current-session bindings only.
- Generated Data Class now resolves exact symbolic ID keys from `*IDs` sheets before type inference and JSON emission, including array items, for both Excel and Google exports. Explicit header annotations still control generated field types; embedded or unknown text stays unchanged.
- Global Collection asset now saves before feature assets after references are assigned, preventing later asset saves from clearing a same-bake Global feature reference.
- `SheetXExporter.ExportExcel` now owns one named, read-only `MemoryStream` through the complete export. NPOI can read workbook parts lazily, so its source stream no longer depends on garbage collection or closes before later selected sheets are read.
- Per-sheet localization artifacts now have distinct types: language `.txt` data remains `Localization`, `{file}.cs` is `LocalizationConstants`, and `{file}Text.cs` is `LocalizationComponent`. Excel and Google handlers route identically; regression coverage runs on Excel only because Google export requires OAuth and network access.

## [1.5.0] - 2026-08-25

Initial release version for Data Config Collections and standalone exact `Configuration` export. Follow-up hardening and final release notes ship in 1.6.0.

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
