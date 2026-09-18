# SheetX Structure Preview and Data Class Validation

Status: written spec approved by the user on 2026-09-16; implementation planning authorized.
Date: 2026-09-16.
Audience: SheetX maintainer and implementation/review agents.

## Goal

Let developers inspect a sheet's C# data structure without exporting files, and detect exported JSON keys that an Existing Data Class would silently discard.

Example: click `Structure` beside `TournamentBotPools`, inspect its proposed class, and see whether the selected `JetpackQuestMilestoneConfig` accepts the JSON members.

## Approved scope

- One button per ordinary JSON sheet, across all three Output Modes.
- Excel and Google Sheets use the same preview window.
- Preview displays selectable C# code and a `Copy Code` action, not a separate field-tree view or sample-JSON tab.
- Existing Data Class validation runs in preview and during export.
- Interactive export asks `Export Anyway` / `Cancel` when JSON contains unmatched members.
- Successful Newtonsoft deserialization defines type compatibility; do not invent a stricter numeric/type conversion policy.
- Member comparison is top-level only for this release.
- Preview retains its current window-local snapshot; `Refresh` rereads the source. No shared static cache.
- Export validation always uses that export's fresh JSON, never preview data.
- No implementation, version bump, commit, or push is authorized by this spec-writing request.

## Existing behavior and constraints

### Two export grammars

`JsonOnly` and `ExistingDataClass` use the legacy converters in `ExcelSheetHandler` and `GoogleSheetHandler`. `GeneratedDataClass` uses `SheetXCollectionSchemaParser` and `SheetXCollectionGenerator`.

They are not interchangeable:

| Header | Legacy JSON | Generated schema |
| --- | --- | --- |
| `bot_ids[]` | `bot_ids` | `botIds` |
| `winRate:float` | literal `winRate:float` | `winRate`, explicitly float |
| `name[3]` | literal `name[3]` | invalid header |

Legacy export also combines Attribute System columns and resolves symbolic IDs. Header inspection cannot reproduce its output contract.

Relevant code:

- `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:1245-1268`: mode-specific collection routing; existing rows use legacy conversion with encryption and file writes disabled.
- `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:1120-1126`: ID loading before conversion.
- `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionSchema.cs:158`: generated schema parsing.
- `Assets/RCore.SheetX/Editor/Collection/SheetXCollectionGenerator.cs:374-416`: existing row/nested-class emitter.

### Existing compatibility check

`SheetXCollectionExportSession.TryAddExistingTable` already deserializes JSON into the selected row-array type before admitting a candidate (`:190-216`). Conversion failures skip that sheet and preserve its previous JSON.

Default Newtonsoft behavior ignores unknown JSON members. Successful deserialization alone therefore does not prove that every exported member has a destination.

The new field check supplements, rather than replaces or weakens, the existing deserialization check.

### Missing values are not missing declarations

Both export paths omit some empty cells:

- `ExcelSheetHandler.cs:1697-1701`.
- `SheetXCollectionSchema.cs:286-291`.

An absent JSON key cannot prove that the sheet lacks the column or that a class member is obsolete. A member absent from all exported rows is informational, not an export mismatch.

### Preview must not export

Calling a converter with file writing disabled is necessary but not sufficient: preview must also avoid settings mutations, source generation, batch commits, collection baking, refresh/reload, and unrelated exporter dialogs.

IDs must be resolved using the source's normal export preparation. A newly constructed handler with an empty ID map is not a faithful preview.

## User experience

### Sheet-row action

Add a final `Structure` action column to the shared sheet table built by `SheetXHelper.CreateSpreadsheetTable`.

- Available for ordinary JSON sheets in all three Output Modes, including an unchecked sheet.
- Available for JSON-only use with Collections disabled.
- Shared table integration covers Excel and Google host windows; do not build a separate Export Multi Files browser.
- No action for IDs, Constants, Settings, Localization, or automatic `Configuration`. Those have separate export contracts, not ordinary row classes.
- Tooltip: `Preview this sheet's C# structure without exporting files.`

The host supplies the source kind and source identity through a callback. Avoid guessing whether an arbitrary sourceId string is an Excel path or Google ID, and avoid temporarily assigning another source to shared settings.

### Preview window

Title: `Sheet Structure`.

Header shows source, sheet, Output Mode, and selected Data Class when applicable. Toolbar contains `Refresh` and `Copy Code`.

The main area displays selectable, scrollable C# text. Inspection and copying do not write a `.cs` file or change a binding.

Mode-specific labels:

| Mode | Preview source | Label |
| --- | --- | --- |
| JSON Only | Actual legacy JSON, inferred across all rows | `Inferred draft from exported JSON` |
| Generated Data Class | Current schema and existing row-class emitter | `Generated class preview` |
| Existing Data Class | Actual legacy JSON, inferred across all rows | `Inferred draft from exported JSON` |

Legacy previews explain that blank or omitted fields may be absent and inferred types are suggestions, not declarations recovered from the sheet.

For Existing Data Class, show validation results below the code:

- `Unmatched JSON member`: key exists in exported data but has no accepted destination in the selected class.
- `Not present in exported data`: accepted class member was not observed anywhere in the JSON; informational only.
- `No unmatched top-level JSON members`: a scoped result, not a claim of complete schema or Unity serialization validity.
- Deserialization errors: show the existing conversion diagnostic, not a success indicator.

Always display `Top-level member check only; nested members are not compared.`

### Snapshot lifecycle

Keep only the current source snapshot in the preview window. Repainting does not reread Excel or call Google APIs. Closing the window or a domain reload discards it.

`Refresh` reloads the source and relevant ID inputs, then recomputes code and validation. Changing source or Output Mode must not reuse a snapshot from the previous context. Changing only the selected Data Class may revalidate the same JSON against the new type.

Show when the snapshot was fetched. If refresh fails, do not present old results as fresh: retain them only with a visible stale/error label. Disable copying when no valid code exists.

Google fetch may require authentication, metadata, and supporting IDs sheets as well as the target sheet. Do not promise one API request. Show loading/error state; do not perform network work from repeated GUI repaint callbacks. Dispose workbook/service resources when no longer needed.

## Structure generation

### Generated Data Class

Use the existing schema path, including the same reference-ID resolution used by export. Expose a small internal wrapper around the existing row/nested-type emitter.

Do not serialize a known schema into JSON and infer it back. Do not invoke full collection generation, require unrelated bindings to be valid, or read a possibly stale generated `.cs` file merely to preview one row type.

Preserve the generator's naming, declared types, nested classes, and formatting. A schema can remain previewable even when there are no data rows.

### Legacy modes

Obtain unencrypted JSON through the existing converter with output writes disabled. Analyze all object rows, not just the first populated row.

- Union property names across rows in deterministic first-seen order.
- Preserve exact JSON member names, including case and underscores.
- Infer scalars from observed JSON token kinds; choose `int` when integral values fit, otherwise `long`, and `double` for observed fractional numbers.
- Combine integral and fractional observations as `double` in this draft; do not use draft type equality for validation.
- Infer arrays from all observed elements; empty arrays alone do not establish an element type.
- Generate supporting classes for nested objects and arrays of objects. Nested code generation does not extend the top-level validator's scope.
- Null-only, unknown array-element, or incompatible mixed shapes use an explicit `object` / `object[]` fallback and a draft diagnostic. Do not invent certainty.
- Escape C# keywords; sanitize otherwise invalid identifiers and preserve their JSON names with `[JsonProperty("originalName")]`. Resolve identifier/type-name collisions deterministically and escape string literals correctly.
- Include imports needed by the displayed code. Use the normal SheetX row naming convention for the proposed root name; this draft does not replace the selected Existing Data Class.
- Empty data or rows with no observable members show `Cannot infer structure from empty exported data`, not an empty class advertised as complete.
- Reject malformed JSON and unsupported ordinary-sheet root shapes with an actionable error.

The draft should be syntactically usable C# for supported inputs. It does not promise that every inferred CLR type is Unity-serializable. In particular, fallback `object` members require developer review before collection baking.

## Validator semantics

### Effective JSON contract

Use Newtonsoft's effective object contract rather than a second handwritten field/property reflection policy. Honor serialized names, inherited members, ignored members, opt-in rules, and private members explicitly exposed by JSON attributes. Follow the deserializer's exact-name-first, then case-insensitive matching behavior.

Do not report an unknown key as discarded if an active extension-data receiver accepts it. For custom converters or non-object contracts whose accepted keys cannot be established from the ordinary contract, report `Member coverage cannot be verified` rather than asserting a mismatch or full success. Existing deserialization checks still apply.

This checks JSON-to-CLR acceptance only. It does not certify that Unity will persist every accepted property or attributed private member.

### Results

- **Unmatched JSON member:** observed top-level JSON key has no accepted destination under the effective contract. This triggers interactive export confirmation.
- **Not present in exported data:** an accepted class member never appears across the observed rows. Preview information only; does not trigger confirmation.
- **Unknown structure:** no usable JSON member evidence. Do not mark every class member as missing.
- **Conversion failure:** existing Newtonsoft deserialization fails. Preserve existing sheet rejection; `Export Anyway` cannot override this failure.

Do not recurse into nested objects for member coverage in this release. Nested conversion errors may still be caught by deserialization, but unknown nested keys remain outside this check.

## Export confirmation and cancellation

Collect unmatched-member diagnostics for admitted Existing Data Class candidates. Present one aggregated confirmation per collection export session, before that session captures migration state or writes artifacts.

Dialog identifies source, sheet, selected class, and unmatched member names. Explain that those JSON members will not populate the selected class. Send the complete diagnostic list to the normal output channel if dialog space is limited.

- `Export Anyway`: continue unchanged through existing generation, write, and bake checks. Do not remove keys or modify class source.
- `Cancel`: abort this collection session before it writes JSON or C# and before it starts baking. Preserve its existing artifacts and pending migration state.
- Confirmation does not persist an ignore flag. A later export with the same mismatch asks again.

Cancellation is not a new whole-project rollback guarantee: ordinary JSON or other export stages that already completed are not undone. Message must say `Collection export cancelled`, not imply that every output from the overall operation was rolled back.

No UI in detached/batch exports or `Application.isBatchMode`: log unmatched members as warnings and continue under existing validation rules. Check both conditions; a detached writer is not synonymous with Unity batch mode.

Fresh export JSON is authoritative. Neither confirmation nor validation may read the preview snapshot.

## Minimal component boundaries

Names are internal implementation targets, not new public API:

1. `SheetXSheetStructure`: JSON-derived draft model and C# text emission for legacy modes; no Unity or I/O dependencies.
2. `SheetXRowTypeMatch`: top-level JSON-member coverage against the effective Newtonsoft contract; no Unity or I/O dependencies.
3. `SheetXSheetJsonSource`: source-specific preparation and in-memory preview data acquisition, reusing small internal handler wrappers; no shared cache.
4. `SheetXSheetStructureWindow`: window-local snapshot, code display/copy, refresh, and validation presentation.

Existing integration points:

- `SheetXHelper.CreateSpreadsheetTable` and its host callbacks: append the action without requiring Collections to be enabled.
- `ExcelSheetHandler` / `GoogleSheetHandler`: prepare IDs and convert without exporting, using the selected mode's existing producer.
- `SheetXCollectionGenerator`: expose single-row schema emission without full collection generation.
- `SheetXCollectionExportSession`: reuse fresh candidate JSON and insert aggregate confirmation before mutation.

No exporter rewrite, new third-party dependency, general schema registry, persistent cache, or runtime assembly dependency is needed.

## Error handling

- Missing workbook, deleted/renamed sheet, Google authorization/network failure: show source-specific error and allow retry through Refresh.
- Conversion or reference preparation failure: show error; do not substitute fabricated empty JSON and claim success.
- Missing selected row type: show diagnostic and preserve existing export rejection rules.
- Unsupported inference: identify uncertain fields and emit explicit fallback drafts only where syntactically valid.
- UI cancellation: no preview-created JSON, scripts, assets, settings changes, or migration snapshots.
- Preview failures must not alter normal exporter behavior for later calls.

## Verification requirements

Follow repository TDD practice: observe meaningful failing tests before production changes. Reuse the existing NUnit/EditMode suite; no new test framework.

Core checks:

- Fields present only in later rows appear in the draft and member comparison.
- Optional fields omitted from all data produce information, not confirmation.
- Empty datasets report unknown structure, not false mismatches.
- Integer widening and other conversions accepted by existing Newtonsoft behavior do not add type-mismatch warnings.
- Unknown JSON members trigger confirmation; known/renamed/inherited/ignored/opt-in members follow the effective contract.
- Extension-data and custom-converter cases do not falsely claim discarded data.
- Arrays, nested draft types, nulls, mixed values, invalid identifiers, and naming collisions produce deterministic code/diagnostics.
- Generated mode preview reuses exact schema emission, including declared types and empty data.
- Legacy preview matches the corresponding export's JSON with ID references and Attribute System data.
- Preview writes no output, mutates no bindings/settings, and starts no bake or migration.
- Cancel preserves all artifacts owned by the pending collection session; Export Anyway retains JSON unchanged.
- Existing conversion failures remain non-overridable.
- Batch/detached mode logs warnings without showing dialogs.
- Preview snapshots never affect fresh export validation; refresh failure cannot show stale success.

Manual UI checks cover Excel and Google: all ordinary modes, unchecked sheets, Collections disabled, loading/error/refresh behavior, copying code, informative missing-data results, and both confirmation choices.

Run focused tests and the full SheetX EditMode suite before implementation completion. Update English and Vietnamese manuals and root/package changelogs with the feature during implementation. Document top-level-only coverage, snapshot freshness, inferred-draft limits, and cancellation scope explicitly.

## Non-goals

- Editing sheets or C# classes from preview.
- Automatically binding or creating class files.
- Nested member validation or exact schema equality.
- Unity serialization compatibility certification.
- Separate JSON sample/tree viewers.
- Automatic Configuration/IDs/Constants/Localization preview.
- Background polling, cross-window caching, or durable snapshots for preview.
- New whole-export transaction/rollback semantics.
- Changes to existing SheetX JSON grammar, inference, or ID assignment.

## Review record

Fable's practical critique was accepted before this document was requested:

1. Reuse schema-backed generation for Generated Data Class instead of lossy JSON round-tripping.
2. Treat absent JSON keys as missing observations, not proof of a structural defect; union across all rows and handle empty data explicitly.
3. Replace shared static cache with the current preview window's snapshot. Always validate exports against fresh export data.

Written-spec self-review: mode-specific sources, omitted-cell semantics, top-level validation limit, source preparation, snapshot freshness, effective JSON contracts, non-overridable conversion errors, and collection-session cancellation boundaries are stated explicitly.
