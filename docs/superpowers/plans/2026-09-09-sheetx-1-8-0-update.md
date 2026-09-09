# SheetX 1.8.0 Correctness Release

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the defects that silently corrupt exported data, each pinned by a test that fails first. No new features, no refactor.

**Architecture:** No public API change, no asset format change, no new files. Every task is a small edit to an existing method plus one EditMode test. The Excel and Google handlers keep their duplicated bodies; where a fix applies to both, it is made twice in the same commit.

**Tech Stack:** Unity 2022.3.62f2, C# (tabs + CRLF), NPOI, Google Sheets API v4, Newtonsoft.Json (already a dependency), NUnit EditMode, `dotnet build RCore.SheetX.Tests.csproj`.

## Global Constraints

- `.cs` files: **tabs**, **CRLF**. Enforced by `.gitattributes`.
- Private instance fields `m_camelCase`; test methods `snake_case_descriptive`.
- Conventional commits with package scope: `fix(sheetx):`, `test(sheetx):`, `chore(sheetx):`. One PR-sized change per commit.
- **Both `CHANGELOG.md` (root, canonical) and `Assets/RCore.SheetX/CHANGELOG.md` get an entry before every commit.**
- Compile gate before every commit: `dotnet build RCore.SheetX.Tests.csproj` **without** `--no-restore` (Unity regenerates `Temp/obj`).
- **`Unity.exe` is not on PATH.** EditMode tests run only when the maintainer opens the Editor. No task may exceed one sitting's eyeball review.
- Public API frozen: `SheetXExporter`, `ISheetXOutput`, `SheetXExportRequest`, `SheetXBatchExportRequest`, `SheetXExportResult`.
- **Do not commit or push unless explicitly requested.**
- Test baseline is **261 `[Test]` methods across 19 files**, effectively all on the Excel path. The Google path needs OAuth + network and has none. Every Google-side change ships on one manual export.

## Ordering

Only one constraint is load-bearing:

**Task 5 (S4 cache clearing) must land before any future change that removes a `CompilationPipeline.RequestScriptCompilation()` call.** That recompile destroys the handler on domain reload, and that accidental destruction is currently the only thing clearing the stale `m_allIds` / `m_allIDsSorted` caches. No task in this release removes one — the constraint is recorded for whoever adds skip-if-unchanged writes later.

Everything else is a preference. Tasks are ordered cheapest-first so the maintainer gets working detectors early; any other order also works.

---

## Task 1: JSON text escaping (C6)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:1795`, `:1848-1856`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:1768`, `:1821-1829`
- Test: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs` (extend)

**The bug.** The hand-rolled escape covers `\n` and `"` only. Backslash is never escaped, and must be escaped *first*; `\r` and `\t` are missing; `ValueType.ArrayText` escapes nothing at all.

A cell reading `C:\Icons\a.png` emits `"C:\Icons\a.png"`, which Newtonsoft reads back as `C:` + newline + `cons` + tab. It parses fine and the value is wrong. A trailing `\` escapes the closing quote and merges two fields.

**The fix is one call, not a helper.** `Newtonsoft.Json` is already a dependency and `JsonConvert.ToString(value)` returns the value quoted and fully escaped. Precedent in this package: `SheetXConfigSheet.cs:420`.

- [ ] **Step 1: Write `json_sheet_with_a_backslash_value_round_trips_exactly`**

Assert **exact string equality** on the emitted artifact, matching the idiom at `SheetXExportTests.cs:18-38` (`Is.EqualTo("[{\"id\":\"hero\"}]")`). Parsing successfully is not enough — this defect produces valid JSON with wrong content. Cover a text column, an `ArrayText` column, and a value ending in a backslash. Add `[SetCulture("tr-TR")]`.

- [ ] **Step 2: Run it, watch it fail** on the backslash row.

- [ ] **Step 3: Replace the manual quote-and-escape with `JsonConvert.ToString(...)`** at all four sites (two per handler). Delete the manual escaping.

- [ ] **Step 4: Run, watch it pass.**

- [ ] **Step 5: `dotnet build`, run EditMode, commit** — `fix(sheetx): escape generated JSON string values with JsonConvert`

**Estimate:** 1h.

## Task 2: Reject invalid generated JSON before writing (C6 guard)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:1969-1971`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:1942-1944`

`SheetXHelper.IsValidJson` exists at `:351` and is already called on an individual `ValueType.Json` cell (`ExcelSheetHandler.cs:1916`), never on the assembled `content`. One call at the end of `ConvertSheetToJson` turns every future concatenation break into a named error instead of a corrupt file on disk.

Cheap, additive, and it is the standing detector for the parts of this 450-line method no test reaches.

- [ ] **Step 1: Call `IsValidJson(content)` before the write; turn false into a named error.**
- [ ] **Step 2: `dotnet build`, run EditMode, commit** — `fix(sheetx): reject invalid generated JSON before writing it`

**Estimate:** 0.25h.

## Task 3: Attribute guard reads past the bound (C3)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:1561-1568`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:1534-1541`
- Test: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs` (extend)

```csharp
if (j + 1 >= rowContent.fieldNames.Count)
    isAttribute = false;
string nextFieldName = rowContent.fieldNames[j + 1];   // runs regardless
```

Any JSON sheet whose **last** header contains "attribute" throws `ArgumentOutOfRangeException` mid-export.

- [ ] **Step 1: Write `json_sheet_with_a_trailing_attribute_column_exports_without_throwing`, watch it fail.**
- [ ] **Step 2: Fold the guard into one `&&` in both handlers.**
- [ ] **Step 3: Run, watch it pass. `dotnet build`, commit** — `fix(sheetx): stop reading past the last field when detecting attributes`

**Estimate:** 0.25h.

## Task 4: Empty and numeric-header columns vanish from JSON (C15)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXHelper.cs` — both `GetFieldValueTypes` overloads (`:95`, `:220`)
- Test: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs` (extend)

**Four misplaced `Add` calls, one of them dead.** In each overload:

- Non-array branch: `fieldValueTypes.Add(fieldValueType)` sits inside the non-empty `else`.
- Array branch, `values.Length > 0` path: same shape, `Add` inside the non-empty `else`.
- Array branch, `values.Length == 0` path: `if (!string.IsNullOrEmpty(longestValue)) Add(...)`. `SplitValueToArray` uses `StringSplitOptions.RemoveEmptyEntries` (`SheetXHelper.cs:86`), so `values.Length == 0` implies the `foreach` never ran and `longestValue` is still `""`. **That guard is provably always false — the branch is dead code.**

So an entirely-empty column is dropped in every path, defeating `persistentFields` (default `"id, key"`), the setting that exists to prevent exactly this. Runtime reads a default value and looks fine.

Second half: `SheetXHelper.cs:234` reads `if (cell == null || !cell.IsMergedCell && cell.CellType != CellType.String) continue;` — a numeric header cell (`2024`, `1`) drops the whole column. Use `cell.ToCellString()`.

**Test through the exported JSON, not through `GetFieldValueTypes`.** Keeping the inferred type is necessary but not sufficient: the row reader also skips null cells at `ExcelSheetHandler.cs:1499-1501`. A unit test on the helper would pass while the artifact stays broken.

- [ ] **Step 1: Write `all_empty_persistent_column_still_appears_in_the_exported_json`** — full export through `SheetXExporter.ExportExcel` with a memory sink, exact-string assertion.
- [ ] **Step 2: Run, watch it fail.**
- [ ] **Step 3: Move four `Add` calls out of their `else` across the two overloads; collapse the dead branch; use `cell.ToCellString()` for headers.**
- [ ] **Step 4: Run. If the reader still drops the column, fix `ExcelSheetHandler.cs:1499-1501` to emit the persistent field's default rather than skipping it.**
- [ ] **Step 5: `dotnet build`, run EditMode, commit** — `fix(sheetx): keep empty and numeric-header columns in the exported JSON`

**Estimate:** 1.5h.

## Task 5: Stale ID caches survive across export buttons (S4)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs`

Six guards read `if (m_allIds == null || m_allIds.Count == 0)`: `ExcelSheetHandler.cs:435, 665, 1110-1112`, `GoogleSheetHandler.cs:463, 676, 1149`. The handler is one-per-window (`ExcelSheetXWindow.cs:37`) and lives as long as the window.

**The sharper half is `m_allIDsSorted`** (`ExcelSheetHandler.cs:29`, a `Dictionary<string,int>`). It is reset at **one site only** — `ExcelSheetHandler.cs:2052` / `GoogleSheetHandler.cs:1983`, inside `ExportAllFiles`. `ExportIDs` resets `m_idsBuilderDict`, `m_allIds` and `m_declaredIds` at `:97-99` and leaves `m_allIDsSorted` alone. *Export IDs* on one sheet then *Export Json* on all sheets reuses a sorted table built from a different ID set: every symbolic substitution silently resolves against stale data while `m_allIds` looks fresh.

**Clear at operation boundaries, not at the four public wrappers.** `ExportAll()` calls the internal overloads directly (`ExcelSheetHandler.cs:2006-2011`), and `ExportIDs(workBook)` replaces `m_allIds` without touching the sorted cache. Reset both dictionaries wherever an export operation begins, including that internal path.

Invisible today only because `RequestScriptCompilation()` destroys the handler on domain reload. `Export Json` (`ExcelSheetXWindow.cs:178-179`) already omits the recompile and is exactly where it bites.

- [ ] **Step 1: Clear `m_allIds` and `m_allIDsSorted` together at every operation boundary in both handlers, `ExportAll`'s internal path included.**
- [ ] **Step 2: `dotnet build`, run EditMode, commit** — `fix(sheetx): clear both ID caches at every export operation boundary`

**Estimate:** 0.75h. **Manual check:** *Export IDs* (one sheet) → *Export Json* (all sheets), no recompile between. Symbolic references must resolve correctly.

## Task 6: Duplicate IDs resolve differently in `.cs` and in JSON (C7)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:356-358`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:398-400`
- Test: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs` (extend)

`BuildContentOfFileIDs` `continue`s on a duplicate — **first wins**. `LoadSheetIDsValues` warns then assigns `m_allIds[key] = value` — **last wins**. `ITEM_SWORD` = 12 then 45 yields a const of 12 and a JSON reference of 45. The two sites are byte-identical between handlers.

**Test the JSON-loading path directly.** A full `ExportExcel` can pass without exercising the bug: `ExportIDs` populates `m_allIds` first-wins, then the JSON stage's `if (m_allIds == null || m_allIds.Count == 0)` guard at `ExcelSheetHandler.cs:1112-1117` skips `LoadSheetIDsValues` entirely. Drive the JSON export path on a handler whose `m_allIds` is empty, so `LoadSheetIDsValues` actually runs.

**Do not touch D3 (Google's missing `m_declaredIds` gate) here.** `ExportAllFiles` prepopulates `m_allIds` at `:2065-2078` before `BuildContentOfFileIDs` at `:2129`; adding an Excel-shaped gate without care makes Google multi-file ID export skip every const.

- [ ] **Step 1: Write `duplicate_id_resolves_first_wins_when_json_loads_ids`, watch it fail.**
- [ ] **Step 2: Add `continue;` after `Blocking` in both handlers.**
- [ ] **Step 3: Run, watch it pass. `dotnet build`, commit** — `fix(sheetx): resolve duplicate IDs first-wins in JSON as well as in code`

**Estimate:** 0.75h.

## Task 7: Constants crash on short vectors, and string constants are unescaped (C4 + C5)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:592-601`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:606-612`
- Test: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs` (extend)

- **C4:** `vector2Values[0]/[1]` and `vector3Values[0..2]` are indexed unguarded. `SplitValueToArray` uses `RemoveEmptyEntries`, so a cell reading `3` or blank yields a short array and an `IndexOutOfRangeException` aborts the whole Constants export.
- **C5:** `= \"{value.Trim()}\"` is unescaped. `C:\Builds\out` or `He said "hi"` emits a non-compiling `.cs` and blocks the entire consuming project. Use `JsonConvert.ToString` here too — C# and JSON string escaping agree on backslash, quote, `\r`, `\n`, `\t`.

- [ ] **Step 1: Write `constants_string_value_with_quotes_and_backslashes_compiles` and `constants_vector2_with_one_component_reports_an_error_instead_of_throwing`. Watch both fail.**
- [ ] **Step 2: Add the length guard with a named error; escape via `JsonConvert.ToString`. Both handlers.**
- [ ] **Step 3: Run, watch them pass. `dotnet build`, commit** — `fix(sheetx): guard short vector constants and escape string constants`

**Estimate:** 0.75h.

## Task 8: `AddNamespace` corrupts content containing `NEW_LINE` (C12)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXHelper.cs:588-598`

The literal `NEW_LINE` is used as an unescaped sentinel, so a constant *named* `NEW_LINE` becomes a newline plus a tab and the generated file no longer compiles.

- [ ] **Step 1: Replace with `string.Join(Environment.NewLine + "\t", content.Split(new[]{"\r\n","\n"}, StringSplitOptions.None))`.**
- [ ] **Step 2: `dotnet build`, run EditMode, commit** — `fix(sheetx): stop using NEW_LINE as a text sentinel`

**Estimate:** 0.25h.

## Task 9: Localization emits non-compiling code (C14)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:811-1010`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:836-1035`
- Test: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs` (extend)

Three defects. **Report, do not silently dedupe** — the enum members after the first use implicit numbering while the constants use original row indices (`ExcelSheetHandler.cs:827-852`), so dropping a colliding row misaligns every value after it.

- `:957-1010` — `English` and `English (US)` both match `Contains("english")`, producing a duplicate `SystemLanguage.English =>` case label and a file that will not compile. Detect the collision and report the offending column names with a named error.
- `:827` / `:841` — `RemoveSpecialCharacters` **preserves dots** (`SheetXHelper.cs:906` allows `'.'`), so `SHOP.BUY` does not collapse into `SHOP_BUY` — it stays an invalid C# identifier. `2X_REWARD` is invalid too. A HashSet fixes neither. Validate each generated identifier before the write — `SheetXCollectionNaming.IsValidIdentifier` (`Editor/Collection/SheetXCollectionSchema.cs:63`) is `internal static` in the same assembly, so call it directly — and report the offending row.
- `lang.ToLower()` at `:957` is culture-sensitive. Sweep `.ToLowerInvariant()` across both handlers — grep-verifiable, 14 hits in `GoogleSheetHandler.cs` alone. Fold in `Path.GetExtension(dropped).ToLower()` in `ExcelSheetXWindow` for free.

- [ ] **Step 1: Write `two_english_language_columns_report_a_collision` and `localization_key_with_a_dot_reports_an_invalid_identifier`. Watch both fail.**
- [ ] **Step 2: Add collision detection and identifier validation with named errors; sweep `.ToLowerInvariant()`.**
- [ ] **Step 3: Run, watch them pass. `dotnet build`, commit** — `fix(sheetx): report localization collisions and invalid identifiers instead of emitting broken code`

**Estimate:** 2h.

## Task 10: Google exports IDs from unchecked spreadsheets

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:1997`

The first `ExportAllFiles` loop iterates `googleSheetsPaths` with **no source-level `selected` check**, then builds and writes IDs. Every later loop has one — `:2032`, `:2050`, `:2063`. `GoogleSheetXWindow.cs:185` (`googleSheetsPath.selected = isOn;`) exposes the toggle, so a user who unchecks a spreadsheet still gets its IDs exported into the project.

One line, matching the three loops below it.

- [ ] **Step 1: Add `if (!googleSheets.selected) continue;` at the top of the loop.**
- [ ] **Step 2: `dotnet build`, commit** — `fix(sheetx): skip unchecked spreadsheets when exporting Google IDs`

**Estimate:** 0.25h. **Manual check:** uncheck one spreadsheet, Export All, confirm its IDs are absent.

## Task 11: `WriteFile` emits a BOM and writes non-atomically (C11)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXHelper.cs:53-65`
- Test: `Assets/RCore.SheetX/Tests/SheetXHelperTests.cs` (extend)

- `new StreamWriter(path, false, Encoding.UTF8)` emits `EF BB BF`. Use `new UTF8Encoding(false)`.
- Truncate-then-write: a crash mid-write destroys the previous artifact, and the interactive window path has no snapshot. Write a `.tmp` beside the target, then `File.Replace`. **`File.Replace` requires the destination to exist** and throws across volumes — fall back to `File.Move` when the target is absent, or a first export throws.

**Only the BOM half gets a test.** `grep -E 'ReadAllBytes|Encoding|BOM|65279' Tests/*.cs` returns zero hits — every existing on-disk assertion goes through `File.ReadAllText`, which strips a BOM transparently. That is exactly why it survived. The atomicity half ships untested: `CollectionGenerationTests.FailingFileOutput` throws **after** `SheetXHelper.WriteFile` returns (`CollectionGenerationTests.cs:1552-1558`), so it exercises transaction rollback, not `WriteFile` atomicity, and no existing fake can interrupt a write mid-flight. `File.Replace` is atomic by construction; that is the whole argument for it.

- [ ] **Step 1: Write `write_file_emits_no_byte_order_mark` using `File.ReadAllBytes`; assert the first three bytes are not `EF BB BF`. Watch it fail.**
- [ ] **Step 2: Switch to `new UTF8Encoding(false)`; add temp-write plus `File.Replace` with the `File.Move` fallback.**
- [ ] **Step 3: Run, watch it pass; confirm the existing collection rollback tests are still green. `dotnet build`, commit** — `fix(sheetx): write files atomically without a BOM`

**Estimate:** 1h.

## Task 12: Empty sheets disappear from combined JSON (C16)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:1448-1451`, `:1969-1974`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs` (counterparts)

Two returns, and only the second one was found before:

- `:1448-1451` — `if (sheet == null || sheet.LastRowNum == 0) { Warn; return null; }`. **This is the common case** — a header-only sheet never reaches the tail at all.
- `:1969-1974` — returns null when the assembled content is `"[]"`.

Either way `:1148`'s `if (m_settings.combineJson && json != null)` drops the key, and the consumer gets a `KeyNotFoundException` for a sheet they can see in the picker. Emit `[]` in both places; keep the warning.

- [ ] **Step 1: Write `empty_sheet_appears_as_an_empty_array_in_combined_json`, watch it fail.**
- [ ] **Step 2: Return `"[]"` from both sites in both handlers.**
- [ ] **Step 3: Run, watch it pass. `dotnet build`, commit** — `fix(sheetx): emit an empty array for empty sheets instead of dropping the key`

**Estimate:** 0.5h.

## Task 13: Malformed encryption key silently uses the published key (C1)

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs:413-425`
- Test: `Assets/RCore.SheetX/Tests/SheetXSettingsTests.cs` (extend)

```csharp
m_encryption ??= SheetXHelper.CreateEncryption(encryptionKey);
return m_encryption ?? Encryption.Singleton;
```

`SheetXHelper.CreateEncryption:561-583` returns null if any token fails `byte.TryParse`. A key like `"12, 34, 256, 78"` produces no error — the default-key warning at `:415` compares strings and is false here — and every JSON ships encrypted with the key published in this repository.

- [ ] **Step 1: Write `encryption_key_with_a_non_byte_token_is_reported_not_silently_defaulted`** — assert the result is not `Encryption.Singleton`, `LogAssert.Expect` the message. Watch it fail.
- [ ] **Step 2: Error instead of falling back.**
- [ ] **Step 3: Run, watch it pass. `dotnet build`, commit** — `fix(sheetx): report a malformed encryption key instead of falling back`

**Estimate:** 0.5h.

## Task 14: Pin the documented symbolic-ID syntax (C9 characterization only)

**Files:**
- Test: `Assets/RCore.SheetX/Tests/SheetXExportTests.cs` (extend)
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:1911-1915` (comment only)

`ExcelSheetHandler.cs:1911-1915` substitutes IDs by containment: `if (fieldValue.Contains(id.Key)) fieldValue = fieldValue.Replace(...)`. `SortIDsByLength` fixes prefixes, not containment, so `GOLD` = 3 turns `{"reward":"GOLD","label":"Buy GOLD now"}` into `{"reward":"3","label":"Buy 3 now"}` — still valid JSON, so Task 2's guard will not catch it.

**Do not fix it in this release.** The obvious fix — parse the cell and substitute only whole-value `JValue` strings — cannot work: `Document.md:443-445` documents **bare, unquoted** symbolic IDs as supported input (`{"id":HERO_2, "name":"JohnDoe 2"}`), which no JSON parser will accept. Substitution has to run *before* parsing, which is exactly what makes it textual. A correct fix needs a real tokenizer for a documented non-JSON dialect: a design change, not a bug fix.

Note also that the current replacement preserves the surrounding quotes, so `"GOLD"` becomes `"3"` — a string. Any future fix that emits a bare `3` is itself a breaking change for consumers deserializing that field as `string`.

- [ ] **Step 1: Write `Characterization_json_column_substitutes_bare_and_quoted_symbolic_ids`** — assert the documented `{"id":HERO_2}` form works and that `"GOLD"` becomes `"3"`. This test **passes immediately**; it is a lock on existing behaviour, not a TDD cycle. The `Characterization_` prefix is the repo convention for exactly that.
- [ ] **Step 2: Add a comment at `:1911` naming the containment hazard and pointing at this test.**
- [ ] **Step 3: `dotnet build`, run EditMode, commit** — `test(sheetx): pin the documented symbolic-ID substitution behaviour`

**Estimate:** 0.75h.

## Task 15: Cut the release

**Files:**
- Modify: `Assets/RCore.SheetX/package.json:3`
- Modify: `CHANGELOG.md`, `Assets/RCore.SheetX/CHANGELOG.md`
- Add: `Assets/RCore.SheetX/Tests/ExcelDropAreaTests.cs.meta`

`package.json` is still `1.7.1`. **A CHANGELOG entry is not enough:** `SheetXUpdateChecker.cs:84-103` reads package versions, so without the bump existing users never see the release through the package's own updater. The tag-workflow limitation (`release.yml:31` validates `v*` against `Assets/RevCore/**/package.json` only) blocks tagging, not package metadata.

`ExcelDropAreaTests.cs` has no committed `.meta` — `git ls-files Assets/RCore.SheetX/Tests/` shows the `.cs` alone, and `.gitignore` carries `!/[Aa]ssets/**/*.meta`, so every clone generates a fresh GUID and a spurious untracked file.

- [ ] **Step 1: Bump `package.json` to `1.8.0`.**
- [ ] **Step 2: Commit the missing `.meta`.**
- [ ] **Step 3: Write the CHANGELOG entry in both files.**
- [ ] **Step 4: `dotnet build`, run EditMode, commit** — `chore(sheetx): cut 1.8.0`

**Estimate:** 0.25h.

**Total: ~10.75h.**

## CHANGELOG shape

```
### Fixed
- Generated JSON string values were not escaped for backslashes, carriage returns or tabs; array text values were not escaped at all.
- Generated JSON was written without being validated.
- A trailing column whose header contained "attribute" crashed the JSON export.
- Empty and numeric-header columns were dropped from exported JSON, defeating persistentFields.
- ID caches persisted across export buttons within one window session.
- Duplicate IDs resolved to different values in generated code and in JSON.
- Short vector constants crashed the Constants export; string constants were not escaped.
- Constants named NEW_LINE corrupted the generated file.
- Localization emitted duplicate switch arms and invalid C# identifiers without reporting them.
- Google "Export All" exported IDs from unchecked spreadsheets.
- Generated files were written non-atomically and carried a UTF-8 BOM.
- Empty sheets disappeared from combined JSON instead of appearing as an empty array.
- A malformed encryption key silently fell back to SheetX's published default key.
```

---

# Not doing, and why

The three analyses and their cross-critiques proposed a 1.9.0 refactor release and four features. All of it is cut. Recorded so nobody re-derives it.

## Features

| Proposal | Ruling |
|---|---|
| **Validate dry-run button** | Cut. `SheetXExport.cs:404` `Validate` is a **private request-validation helper** (null sink, null request, empty path, `CreateTransient`), not a dry-run API. Routing a window button through detached `SheetXExporter` runs a *different pipeline* — detached mode disables typed Configuration and collections (`ExcelSheetHandler.cs:47`, `:1181`). A green result for a pipeline the user is not running is worse than no button. |
| **Google per-source Reload** | Cut. The gap was a grep artifact: `GoogleSheetXWindow.cs:75` already has a **Download** button that re-fetches metadata and re-syncs sheet names (`SheetXHelper.cs:777-818`), and Multi Files reaches the same action through Select (`EditGoogleSheetsWindow.cs:58`). Searching for the word `Reload` missed it. `m_cachedSpreadsheet` (`GoogleSheetHandler.cs:42`, never cleared anywhere) is still worth invalidating — fold that into the existing Download when someone next touches that file. |
| **Collection skip report** | Cut. `SheetXCollectionsWindow.cs:137-194` does not own spreadsheet export; those four error sites handle rename, delete and baking existing data. Export skip reasons originate in the handlers. One HelpBox here is cross-window result plumbing. |
| **Encrypt Json settings UI** | Cut. No demonstrated user need, and it is not the six lines it looked like: `SheetXSettings.cs:423` caches the first key indefinitely (`m_encryption ??=`), so editing the key in a new UI would keep using the old cipher. Task 13 fixes the failure mode that actually matters. |
| **Exception boundary around export buttons** | Cut. Unity already surfaces uncaught editor exceptions with a full stack trace in the Console. A `try`/`catch` that logs a friendlier message repairs no revision mixing, names no affected files, and risks replacing the stack trace with something less useful. |

## Refactor

| Proposal | Ruling |
|---|---|
| **R1 — extract the `ConvertSheetToJson` tail** | Deferred. The measured diff between the two tails is only **22 lines** across ~468, which is the case *for* extracting — but it is not the "pure move" the earlier draft claimed. The tail calls private `GetReferenceId` (13 sites) and `CheckExistedId` (2 sites), and branches on `m_batchState` to pick between `m_writer.Error` and `m_writer.Blocking` (`ExcelSheetHandler.cs:1920`). "Reader-free" was verified; "dependency-free" was not, and the proposed signature accounted for neither. Combined with pre-sorting `m_allIDsSorted` (a cache-lifecycle change) and adopting Excel's `break` (a Google behaviour change), the "pure move" commit would carry three behaviour changes on the path with zero automated coverage. Revisit when a real change has to be made in both copies at once. |
| **The "build from the Excel copy" construction rule** | Withdrawn — the proof was false. `SheetXExportTests.cs:64-83` uses a flat single-column `Payload{}` fixture, so `nestedField == false` and the post-switch nested append does nothing. `break` and `continue` both log exactly once and the test cannot distinguish them; it does not pin Excel's `break`. Excel is still the sensible base — it has the tests — but not for the stated reason. |
| **D2 as a separate commit after R1** | Withdrawn. Choosing either handler's copy for a shared body *is* the behaviour change. Keeping Google's `continue` through the move only to delete it next commit is ceremony. |
| **R0 — normalise Google's writer-call shape** | Cut. Seven cosmetic edits with no independent payoff; it only existed to shrink a refactor diff that is no longer happening. |
| **R2 — collapse the five byte-identical members** | Cut. ~500 stable, bug-free duplicated lines with no pending change. Tasks 7 and 9 already fix both copies in one commit, which is what "maintained twice" actually costs today. |
| **R3 — move the batch emitters off `ExcelSheetHandler`** | Cut. `SheetXBatchExport.cs:682-714` calling `m_excel.BatchEmit*` for Google-only batches is ugly but works, and the move is not self-contained: two of the five call private localization generators still inside `ExcelSheetHandler` (`:2429`, `:2437`), so it means moving more code or inventing accessors. Hypothetical future breakage does not justify that today. |
| **C9 — whole-value ID substitution** | Downgraded to Task 14, characterization only. The proposed fix breaks documented input (`Document.md:443`). |

## Deferred fixes

| Item | Ruling |
|---|---|
| **Skip-if-unchanged writes + conditional recompile** | Real value — `git status` currently shows 30 changed files after altering one number — but the risk is a false *negative*: `ExcelSheetHandler.cs:1096` gates `AssetDatabase.Refresh()` on `session?.WroteArtifacts == true`, and `SheetXCollectionExportSession.CaptureSnapshots:406-425` assumes every path was written. A skipped write must still count as handled by the rollback path. Needs Task 5 first, and tests for both false-negative cases. Reuse `SourcesChanged:481-490`. **Task 11's BOM change is not a prerequisite** — a byte comparison against a BOM'd file rewrites once and matches thereafter; it does not mark files changed forever. |
| **One error summary instead of a modal per bad row** | `SheetXWriter.Blocking:71-77` shows an `EditorUtility.DisplayDialog`, and the eight call sites (Excel `172, 198, 357, 1923`; Google `219, 241, 399, 1894`) sit inside per-row loops, so 300 duplicated IDs means 300 dialogs. The correct behaviour already exists in the same method's detached branch. Worth doing; classify as **Changed**, not Fixed. |
| **`ExportAllFiles` re-opens every workbook 4×** | `ExcelSheetHandler.cs:2067, 2088, 2103, 2120`; `SheetXData.cs:107-115` re-reads from disk each call, so a file saved during a 20s export yields IDs from revision A and JSON from revision B. Same bug 1.3.0 fixed for the single-file path (`SheetXExport.cs:337-341`). |
| **D3 — Google `m_declaredIds`** | Manual verification only; see the caution in Task 6. |
| **Google A1 ranges unquoted** | A sheet named `Item Data` or `Player's Items` needs `'`-quoting with doubled internal quotes. Untestable, ships on judgement. The related unguarded `ColumnCount.Value` at `GoogleSheetHandler.cs:1331` has a correct guard to copy at `TryGetGridRange:2405-2414` — take that one whenever that file is next open. |
| **Collection rollback gaps** | `RestoreSnapshot:457-470` never calls `AssetDatabase.ImportAsset`, so after a rollback the asset database holds the failed export's scripts; and a previously generated per-source script no longer in `sources` is in neither the snapshot list nor any delete path, so it stays on disk declaring a class the export no longer produces. That second half is what breaks a compile. |
| **`SearchLoadedAssemblies` short-name match** | Dead code — `SheetXCollectionSheetGUI.cs:149` always writes `AssemblyQualifiedName` and `TryResolveRowType:579-604` resolves it via `Type.GetType` first. Residual note only: `SheetXCollectionExportSession:618-619` accepts `FullName` or short `Name` while `SheetXCollectionBaker.FindType:674-688` accepts `FullName` or `AssemblyQualifiedName`, so the two resolvers disagree on accepted forms. |
| **Collection assets default into `Resources`** | Not a defect. `GlobalConfigCollectionBase.cs:17` is `Resources.Load<T>(typeof(T).Name)` and `SheetXCollectionSettings.ValidateFolders:406-411` *requires* the global folder to end with `Resources`. The residual true statement — baked ScriptableObjects are never encrypted even when `encryptJson` is on — belongs in `Document.md`, not in code. |
| **Watch mode, CLI wrapper, diff preview, undo, per-sheet overrides, incremental hashing, schema validation, progress bar, collections in the detached API, general `ISheetSource`, unifying the two windows, splitting `SheetXHelper.cs`, UIToolkit** | Rejected. Each duplicates something that already ships, or needs a source abstraction whose only proof would be a Google test that cannot exist. |

# Verification

Per commit:
1. `dotnet build RCore.SheetX.Tests.csproj` — zero errors.
2. `git diff --check`; confirm tabs + CRLF on `.cs`.
3. CHANGELOG entry in both files.

Per release, in the Editor (`Unity.exe` is not on PATH):
4. EditMode → Run All. 261 existing plus the new tests, zero failures.
5. Manual Excel matrix: Export IDs → Export Constants → Export Json → Export Localizations, single file and multi-file, with and without `combineJson`.
6. Manual Google export covering Tasks 5, 9, 10, 11 and 12 — the Google half of every shared fix has no automated coverage.
7. The S4 sequence: *Export IDs* with one sheet selected, then *Export Json* with all sheets, no recompile between.

# Corrections to the analyses

Measured, not asserted. Kept so a later reader does not re-derive them.

- The `ConvertSheetToJson` tail diff is **22 lines**, not 35.
- The test baseline is **261 `[Test]` across 19 files** — not 234/23, not 230/23, not 261/23.
- `m_allIDsSorted` is a **`Dictionary<string,int>`** (`ExcelSheetHandler.cs:29`), not a `List<>`, and it is **assigned inside** the proposed extraction region at `:1909`.
- The `Blocking` sites are Excel `172, 198, 357, 1923` and Google `219, 241, 399, 1894`. Google `:1894` **is** a `Blocking` call.
- `SheetXExportTests.cs:64-83` does **not** pin Excel's `break` — its fixture is flat, so `break` and `continue` are indistinguishable there.
- `GoogleSheetXWindow.cs:75` **does** have a metadata refresh (Download). There was never a missing-Reload gap.
- `CollectionGenerationTests.FailingFileOutput` throws **after** the write returns (`:1552-1558`); it tests rollback, not `WriteFile` atomicity.
- `RemoveSpecialCharacters` **preserves dots** (`SheetXHelper.cs:906`), so `SHOP.BUY` survives as an invalid identifier and a HashSet does not fix it.
- `Document.md:443-445` documents **bare unquoted symbolic IDs** inside JSON columns, which is why C9 cannot be fixed by parsing.
- C15's array-empty branch is **provably dead**: `SplitValueToArray` uses `RemoveEmptyEntries` (`SheetXHelper.cs:86`), so `values.Length == 0` implies `longestValue == ""`.
- `ConvertSheetToJson` returns early on `LastRowNum == 0` (`:1448-1451`), so the empty-sheet case never reaches the tail.
- `GoogleSheetHandler.cs:1997`'s ID loop has no source-level `selected` check while `:2032`, `:2050` and `:2063` do.
- `package.json` is at **1.7.1** and `SheetXUpdateChecker.cs:84-103` reads package versions, so a CHANGELOG-only bump hides the release from the updater.
- The three source analyses and their cross-critiques lived in `.superpowers/sheetx-plan/` as session scratch and are not committed; every claim of theirs that survived measurement is restated above.
