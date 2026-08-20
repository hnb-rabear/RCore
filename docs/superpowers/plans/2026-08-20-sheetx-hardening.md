# SheetX Hardening Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `Assets/RCore.SheetX` safe to publish and correct in its export paths, so it can be consumed as an unmodified UPM dependency by external projects (specifically the iKit kit at `e:\Projects\IKame\ikit.git`).

**Architecture:** No structural change. SheetX stays a single Editor-only assembly (`RCore.SheetX.Editor`) with the sheet-name-as-contract design intact. All work is confined to: credential storage, four export-path bugs, and three hygiene items.

**Tech Stack:** Unity 2022.3+, `RCore.SheetX.Editor` asmdef (Editor-only), NPOI, Google.Apis.Sheets.v4, Newtonsoft.Json 3.2.1, `EditorPrefs`.

**Branch:** Create from current `main` HEAD. Suggested name `fix/sheetx-security-and-export-flags`.

---

## Revision 2026-08-21 — read before executing

The plan was reviewed against the source before execution. Six problems were found and are now folded into the tasks below; this list exists so a reader can tell what changed and why, not as separate work.

| # | Where | Change |
|---|---|---|
| 1 | Task 1 Step 2 | **Was data-destroying.** `Encryption.Decrypt` only throws on malformed Base64; a wrong `encryptionKey` returns garbage silently, so the old `try`/`catch` never fired and the unconditional blanking wiped the credential. Rewritten to validate the decrypted string and leave the field alone on failure. |
| 2 | Task 1 Step 2 | `AssetDatabase.SaveAssets()` called from `Init()`, which runs from three `OnEnable` handlers. Now `SetDirty` + `EditorApplication.delayCall`. |
| 3 | Task 1 Step 6 | HelpBox was unreachable — `encryptJson` / `encryptionKey` are `[HideInInspector]` with no UI anywhere. Two workable options given; pick one. |
| 4 | Task 3 | Missed two ragged-row sites: `GoogleSheetHandler.cs:158` and `:322`. Added as Step 3b. |
| 5 | Task 4 Step 2 | **Already done** in commit `34e5f5f`, and the shipped fix is broader than the step described. Marked done; do not re-do. |
| 6 | Task 1 Steps 2–3 | Must land in one commit or every Google export is broken at the intermediate commit. Stated explicitly. |

### Decisions taken 2026-08-21

- **Git-history purge (Task 0 Step 3): NO.** Rotation makes the historical values inert, so a purge would be cleanup rather than containment, and it invalidates every existing clone and open PR. Record this in the Task 1 commit body.
- **Version: 1.1.0, not 2.0.0.** Task 4 Step 1 (`SheetXHelper : MonoBehaviour` → `static`) is **dropped** — see that step. Task 2's behavior change still ships with a loud CHANGELOG note as a written-down policy exception.
- **Task 1 Step 6: option 6a** — warn from `GetEncryption()`. No new UI.

### Execution gate

**Task 1 is blocked on Task 0.** The credential has not been rotated yet. Do not blank the asset, do not run the migration, do not touch `SheetXSettings.cs` credential storage until rotation is confirmed — blanking the working tree while the live credential is still valid in Google Cloud removes the reminder without removing the exposure.

Tasks 2 and 3 are wholly independent of the credential and may proceed first. The commit sequence below is reordered accordingly.

---

## Why this plan exists

An external consumer (iKit) evaluated SheetX for integration. The finding was that **SheetX needs no new features to serve that consumer** — see "Non-goals" below. What it does need is the security fix and the bug fixes, and those are worth doing regardless of any consumer, because they affect everyone using the tool today.

Doing them *before* the external project forks or depends on SheetX means divergence starts from a clean base.

---

## Non-goals — do not do these

Recorded so no one re-derives them mid-implementation.

| Not doing | Why |
|---|---|
| String-based IDs instead of `int` | Consumers that don't want `IDs.cs` opt out by **not naming a sheet `*IDs`**. `ExcelSheetHandler.ExportIDs` gates on `name.EndsWith(SheetXConstants.IDS_SHEET)`; `SheetXSettings.CreateFileIDs` early-returns on empty content. Nothing to change. |
| Removing / slimming `LocalizationTemplateV2.txt` | Same opt-out: the localization pass gates on `name.StartsWith("Localization")`. A consumer with its own localization runtime just doesn't use that prefix. |
| Emitting ScriptableObjects, or JSON shaped for a specific consumer's config system | Consumer-specific contract. Belongs in the consumer, not here. iKit will wrap per-sheet output on its side. |
| Changing `combineJson` output shape | It emits `Dictionary<string,string>` (nested JSON as escaped strings). Ugly, but existing users parse it. Changing it is a break for zero gain — no consumer needs it. |
| Extracting a parse-core / emit-backend plugin seam | The only justification would be multiple emit backends. None exist. `ExcelSheetHandler` (1777 LOC) and `GoogleSheetHandler` (1925 LOC) are near-duplicates; splitting them cleanly is a large refactor with no current consumer. Revisit when a second backend actually exists. |

---

## File Structure

- Modify `Assets/RCore.SheetX/Editor/SheetXSettings.cs` — credential storage moves to `EditorPrefs`; default-encryption-key warning (option 6a). `FILE_PATH` already fixed in `34e5f5f`.
- Modify `Assets/RCore.SheetX/Editor/SheetXSettingsWindow.cs` — call the migration from `Load()`; default-encryption-key HelpBox only if option 6b is chosen.
- Modify `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs` — `ExportIDs` flag fix; localization header NRE; null-sheet log NRE.
- Modify `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs` — `ExportIDs` flag fix + hoist merged write out of the loop; localization ragged-row `IndexOutOfRangeException`; IDs/Constants `col + 1` bounds.
- Modify `Assets/RCore.SheetX/Editor/SheetXHelper.cs` — `MonoBehaviour` → `static class`.
- Modify `Assets/RCore.SheetX/SheetXSettings.asset` — blank the two credential fields.
- Modify `Assets/RCore.SheetX/CHANGELOG.md` — one entry per task.
- Modify `Assets/RCore.SheetX/package.json` — version bump.
- Delete `Assets/RCore.SheetX/Samples~/` — contains one stray `.meta`, no sample.
- Do not modify `Assets/RevCore` or the rest of `Assets/RCore`.

### Conventions that apply

- `.cs` files: **tabs**, **CRLF** (`.gitattributes` enforces). SheetX already uses tabs — match surrounding code exactly.
- Private instance fields `m_camelCase`, private static `s_camelCase`, public `PascalCase`. SheetX follows this.
- One CHANGELOG entry per commit; conventional commit messages with scope `sheetx`.
- **The XML-doc coverage gate does not cover this code** — `scripts/check-xmldoc-coverage.py` roots at `Assets/RevCore`. Still write `/// <summary>` on anything new, matching the file's existing density (SheetX is well-documented already).
- **The public-API workflow does not apply.** `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` exist only under `Assets/RevCore/*/Runtime/` (9 pairs, verified). SheetX has none. Do not create one.
- `docs/contributing/SEMVER_POLICY.md` is written for RevCore, but its definition of "breaking" is the house rule and this plan follows it. SheetX is at 1.0.2, so the **post-1.0** column applies: MINOR must not break public API.

---

## Task 0: Rotate the exposed Google OAuth credentials

**This is a human action outside the repo. It must happen before or alongside Task 1, and it does not wait on any code.**

`Assets/RCore.SheetX/SheetXSettings.asset` lines 105-106 contain a live Google OAuth client ID and client secret:

```
googleClientId: nemLtHanO8yTvVCfcmm1olScbLHtgY0GJca44nzXxmm+JshTsX16Ez/egFrn7TyzyaG135y3q0bvEBeUXPKpLH90fSKzeOuk
googleClientSecret: 75P71h7NJajJuBH/E2OdrGSlUouv9IUberqj0EngjWqYGew=
```

These are XOR-then-Base64 encoded with a 256-byte key that is hardcoded in the same repository at `Encryption.cs:20` and duplicated at `SheetXSettings.cs:143`. That is obfuscation, not encryption — anyone who clones the repository can recover the plaintext. The repository is published publicly on GitHub.

- [ ] **Step 1: Rotate in Google Cloud Console**

Go to the Google Cloud project that owns this OAuth client. Delete the exposed client credential and create a new one (Desktop app type). Do not paste the new secret into any file in this repository.

- [ ] **Step 2: Revoke existing user grants**

The OAuth flow stores tokens via `FileDataStore` under `Assets/Editor/`. After rotation those tokens are invalid; confirm the exposed client no longer appears under the project's OAuth clients.

- [ ] **Step 3: Decide on git-history purge**

The credential values remain in git history even after Task 1 blanks them in the working tree.

Once the credential is rotated, the historical values are inert, so purging is cleanup rather than containment. **A history purge (`git filter-repo`) rewrites every commit hash and invalidates every existing clone and open PR.** Only do it if the exposed client held permissions beyond reading the maintainer's own spreadsheets.

**Decided 2026-08-21: no purge.** The exposed client only reads the maintainer's own spreadsheets, so rotation alone contains it. Put this line in the Task 1 commit body so the next reader does not re-litigate it:

```text
History purge declined: the rotated client held no permissions beyond reading the
maintainer's own spreadsheets, so the historical values are inert. A filter-repo
rewrite would invalidate every clone for no security gain.
```

---

## Task 1: Move Google credentials out of the committed asset

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettingsWindow.cs`
- Modify: `Assets/RCore.SheetX/SheetXSettings.asset`
- Modify: `Assets/RCore.SheetX/CHANGELOG.md`

The two `string` fields on the ScriptableObject are the root cause: a serialized field on a committed asset is a file on disk in a git repository. `EditorPrefs` is per-machine and never enters version control.

**The storage split, stated once so it is not re-litigated.** Commit `34e5f5f` deliberately made the settings asset *more* durable and *more* committed — it now lives at `Assets/SheetX/SheetXSettings.asset` in the consuming project, and `Document.md` tells the user to commit it, so Excel paths and Google sheet lists survive a clone. This task moves credentials the opposite way, into per-machine `EditorPrefs`. That is not a contradiction, it is the split:

| What | Where | Why |
|---|---|---|
| Excel paths, Google sheet IDs and lists, output folders, toggles | committed asset | team-shared, worthless to an attacker, painful to re-enter |
| Google OAuth client ID and secret | `EditorPrefs`, per machine | per-developer, and a committed secret is a published secret |

Everything the asset holds after this task is safe to commit. That is the point of doing both.

Keep the serialized fields in place — removing a serialized field from a `ScriptableObject` is a breaking change under `docs/contributing/SEMVER_POLICY.md`. They become a **one-time migration source only**, then get blanked.

- [ ] **Step 1: Add the EditorPrefs-backed store**

In `SheetXSettings.cs`, add a project-scoped key prefix. `EditorPrefs` is machine-global, so the key must include a project discriminator or two projects on one machine will overwrite each other. The file already does exactly this at `SheetXSettingsWindow.cs:81` (`$"{Application.identifier}.RateClicked"`) — reuse that idiom rather than inventing a second one:

```csharp
private static string PrefKey(string field) => $"{Application.identifier}.SheetX.{field}";
```

Rewrite the two properties at `:220` and `:244` to read and write `EditorPrefs` instead of decrypting the serialized fields. Keep the property names — `SheetXSettingsWindow`, `GoogleSheetXWindow`, and `GoogleSheetHandler` already call them (12 call sites, verified), so nothing downstream changes.

```csharp
public string ObfGoogleClientId
{
    get => EditorPrefs.GetString(PrefKey("GoogleClientId"), "");
    set => EditorPrefs.SetString(PrefKey("GoogleClientId"), value ?? "");
}
```

Same shape for `ObfGoogleClientSecret`. Delete the `m_obfGoogleClientId` / `m_obfGoogleClientSecret` cache fields (`:219`, `:243`) and the try/catch — with no decryption there is nothing left to fail, and the `if (value == m_obf…) return;` short-circuit in the old setters goes with them.

**Watch the setter semantics.** The old setter guarded on the cache field, so a `set` from `EditorHelper.TextField` was cheap when unchanged. `EditorPrefs.SetString` on every OnGUI repaint is also cheap, but `EditorGUI.BeginChangeCheck` at `SheetXSettingsWindow.cs:27` still wraps these assignments — and since the credential no longer lives on the asset, an edit to it will trip `EditorUtility.SetDirty(m_sheetXSettings)` at `:60` without there being anything to save. Harmless. Do not add machinery to avoid it.

- [ ] **Step 2: Add one-time migration**

A user upgrading has their credential in the asset and nothing in `EditorPrefs`. Migrate once, then blank the asset field so the secret leaves their disk too.

Add to `SheetXSettings` (`internal`, not `private` — Step 3b calls it from the window).

**Two traps this code must avoid — both verified against the source, both fatal if ignored.**

*Trap 1 — `try`/`catch` around `Decrypt` does not do what it looks like it does.* `Encryption.Decrypt` (`Encryption.cs:41-45`) is `Convert.FromBase64String` then XOR then `Encoding.UTF8.GetString`. Only the Base64 step throws. XOR with the **wrong key** produces garbage bytes, and `UTF8.GetString` replaces every invalid sequence with U+FFFD rather than throwing. A user whose `encryptionKey` differs from the one their credential was encrypted with — anyone who edited the key, loaded a `.sx` from another machine, or upgraded from a build with a different default — silently gets garbage in `EditorPrefs`. The `catch` never fires, so a blank-after-catch structure destroys the only copy of the credential.

Validate the *result*, and never blank a field whose migration did not clearly succeed. Losing a credential is worse than leaving one in the asset for one more session — the user can always re-run after fixing the key, but they cannot un-delete.

*Trap 2 — no `AssetDatabase.SaveAssets()` here.* `Init()` is called from three `OnEnable` handlers (`SheetXWindow.cs:32`, `SheetXSettingsWindow.cs:22`, plus the Excel/Google tab windows), which run during assembly reload where an `AssetDatabase` write is unsafe. Mark dirty and defer, matching the pattern already shipped in `SheetXWindow.FlushSettings()` (commit `34e5f5f`).

```csharp
/// <summary>
/// Moves credentials from the legacy encrypted serialized fields into EditorPrefs, then
/// clears them from the asset. No-op once the fields are empty. A field whose decryption
/// does not yield a plausible credential is left in place, not blanked.
/// </summary>
internal void MigrateCredentialsToEditorPrefs()
{
    if (string.IsNullOrEmpty(googleClientId) && string.IsNullOrEmpty(googleClientSecret))
        return;

    bool migrated = TryMigrate("GoogleClientId", ref googleClientId)
        | TryMigrate("GoogleClientSecret", ref googleClientSecret);
    if (!migrated)
        return;

    EditorUtility.SetDirty(this);
    var self = this;
    EditorApplication.delayCall += () =>
    {
        if (self != null)
            AssetDatabase.SaveAssetIfDirty(self);
    };
    UnityEngine.Debug.Log("SheetX: Google credentials moved to EditorPrefs and cleared from the settings asset.");
}

private bool TryMigrate(string field, ref string legacy)
{
    if (string.IsNullOrEmpty(legacy))
        return false;

    string plain;
    try { plain = GetEncryption().Decrypt(legacy); }
    catch { plain = null; }

    // Decrypt only throws on malformed Base64. A wrong encryptionKey yields garbage bytes that
    // UTF8.GetString turns into U+FFFD instead of throwing, so the result must be checked, not
    // just the exception. A real OAuth credential is printable ASCII.
    if (string.IsNullOrEmpty(plain) || plain.Any(c => c < 0x20 || c > 0x7E))
    {
        UnityEngine.Debug.LogWarning(
            $"SheetX: could not decrypt {field} — the encryptionKey does not match the one it was "
            + "saved with. The field has been left untouched; re-enter the credential in Settings, "
            + "which will overwrite it.");
        return false;
    }

    EditorPrefs.SetString(PrefKey(field), plain);
    legacy = "";
    return true;
}
```

Note the `|` (not `||`) in `MigrateCredentialsToEditorPrefs` — both fields must be attempted even when the first one fails.

`Init()` (`:85-103`) has three return paths. Call the migration on the two that return an **existing** asset — the `LoadAssetAtPath` hit at `:87` and the `FindAssets` fallback hit at `:97`. The third path (`:100`, `CreateScriptableAsset` + `ResetToDefault`) produces empty fields, so it needs no call.

> Line numbers in this task shifted by ~3 after commit `34e5f5f`. The anchors above are post-`34e5f5f`; anchors elsewhere in this plan may be stale by the same amount — match on code, not on line number.

`ResetToDefault():139-140` sets both legacy fields to `""`. Leave those two lines — they are correct and keep "Reset to default settings" from resurrecting a stale credential. But note that Reset no longer clears the *live* credential, since that now lives in `EditorPrefs`. That is the right behavior (a settings reset should not log you out of Google), so make no change.

- [ ] **Step 3: Convert the four raw-field checks — this step is what keeps Google export working**

`GoogleSheetHandler.cs` checks the raw serialized fields in **four** places: `:68` (`ExportIDs`), `:384` (`ExportConstants`), `:608` (`ExportLocalizations`), `:1037` (`ExportJson`). All four are identical:

```csharp
if (string.IsNullOrEmpty(m_settings.googleClientId) || string.IsNullOrEmpty(m_settings.googleClientSecret))
```

After migration those fields are always empty, so all four exports would refuse to run. Convert every one to the properties:

```csharp
if (string.IsNullOrEmpty(m_settings.ObfGoogleClientId) || string.IsNullOrEmpty(m_settings.ObfGoogleClientSecret))
```

`GoogleSheetXWindow.cs:88` and `:172` already use the properties — leave them. `GoogleSheetHandler.cs:1887` and `GoogleSheetXWindow.cs:81,155,242` pass the properties into `SheetXHelper.AuthenticateGoogleUser` / `DownloadGoogleSheet` / `EditGoogleSheetsWindow.ShowWindow`, which all take plaintext `string` parameters — no change there either.

After this step, grep `m_settings.googleClient` across `Editor/`: it must return zero hits. The only reads of `googleClientId` / `googleClientSecret` left anywhere are inside `MigrateCredentialsToEditorPrefs` (and its `TryMigrate` helper).

**Steps 2 and 3 must land in the same commit.** Step 2 blanks the legacy fields; Step 3 is what teaches the four export gates to look somewhere else. Commit Step 2 alone and every Google export at that commit refuses with "Please setup the Client Id and Client Secret!" — a broken bisect point and a broken checkout for anyone who lands on it. Task 1 is one commit, not four.

- [ ] **Step 3b: Plug the `.sx` save/load leak**

`SheetXSettingsWindow.Save():113` writes `JsonUtility.ToJson(m_sheetXSettings)` to a user-chosen `.sx` file — which today includes both credential fields. Users share `.sx` files to sync settings across a team, so this is a second exfiltration path, not just a storage problem.

Once Step 2 blanks the fields, `Save()` stops leaking on its own. But `Load():130` uses `JsonUtility.FromJsonOverwrite`, so loading an **old** `.sx` repopulates the legacy fields. Call `MigrateCredentialsToEditorPrefs()` at the end of `Load()` (make it `internal` rather than `private` so the window can reach it) so an old file's credentials land in `EditorPrefs` and are immediately cleared from the asset rather than silently re-committed.

- [ ] **Step 4: Mark the legacy fields**

```csharp
[HideInInspector, System.Obsolete("Legacy storage. Credentials live in EditorPrefs; this field is migration-only and is cleared on load.")]
public string googleClientId;
```

Same for `googleClientSecret`. Unity still serializes `[Obsolete]` public fields, so nothing breaks; the attribute stops new call sites. Suppress the obsolete warning inside the migration method with `#pragma warning disable 618` / `restore 618`.

- [ ] **Step 5: Blank the committed asset**

Edit `Assets/RCore.SheetX/SheetXSettings.asset` so lines 105-106 read:

```yaml
  googleClientId:
  googleClientSecret:
```

- [ ] **Step 6: Warn about the shipped default encryption key**

`encryptJson` is a legitimate feature and stays. But `ResetToDefault` sets `encryptionKey` to a key that is published in this repository, so JSON "encrypted" with the default key is decryptable by anyone.

**Decided 2026-08-21: option 6a.** Skip to it; 6b is recorded only so the reasoning is not re-derived.

**A HelpBox alone is unreachable — read this before writing it.** Both fields are `[HideInInspector]` (`SheetXSettings.cs:81-82`) and neither has a control anywhere in `SheetXSettingsWindow.OnGUI` (verified: the only reads outside `SheetXSettings.cs` are the four export call sites, `ExcelSheetHandler.cs:999`, `:1019`, `:1653`, `:1673` and `GoogleSheetHandler.cs:1074`, `:1094`, `:1757`, `:1776`). So `encryptJson` is always `false` for anyone who has not hand-edited the asset YAML, and a warning drawn under `if (encryptJson && key == default)` would render for nobody.

Two ways to close that, pick one:

**6a (recommended, lazier): warn at the point of use, not in the settings UI.** In `SheetXSettings.GetEncryption()`, log a one-time warning when `encryptionKey` equals the shipped default. That reaches the user who hand-edited the YAML — the only user who can currently be affected — and needs no new UI:

```csharp
public Encryption GetEncryption()
{
    if (encryptJson && encryptionKey == s_defaultEncryptionKey && !s_warnedDefaultKey)
    {
        s_warnedDefaultKey = true;
        UnityEngine.Debug.LogWarning(
            "SheetX: encryptJson is on but encryptionKey is still the key shipped with this "
            + "package, which is published in a public repository. Anyone can decrypt the output. "
            + "Set your own key before shipping encrypted data.");
    }
    m_encryption ??= SheetXHelper.CreateEncryption(encryptionKey);
    return m_encryption ?? Encryption.Singleton;
}
```

Hoist the default key literal out of `ResetToDefault` into a `private static readonly string s_defaultEncryptionKey` so the comparison has something to compare against — right now the string exists only inline at `:159-160`.

**6b: expose the fields, then add the HelpBox.** Drop `[HideInInspector]`, draw a `Toggle` for `encryptJson` and a `TextField` for `encryptionKey` in `OnGUI`, and put the `EditorGUILayout.HelpBox` under them. More useful long-term — an undiscoverable feature is not much of a feature — but it is a new UI surface, needs its own acceptance row, and is a MINOR-worthy addition rather than a security fix.

Do not do both. Do not block the export either way — just say it.

- [ ] **Step 7: CHANGELOG**

Add under a new `[Unreleased]` heading:

```markdown
### Security
- Google OAuth client ID and secret now live in `EditorPrefs` instead of the committed
  `SheetXSettings.asset`. Existing values are migrated automatically on first load and
  cleared from the asset. **Credentials previously committed to a public repository must be
  rotated in Google Cloud Console — migration does not un-publish them.**
- Settings window now warns when `encryptJson` is used with the shipped default key.
```

---

## Task 2: Fix `ExportIDs` reading the wrong flag

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs`
- Modify: `Assets/RCore.SheetX/CHANGELOG.md`

Both `ExportIDs` methods branch on `separateConstants` where they mean `separateIDs`. `ExportAllFiles` gets it right — `separateIDs` at `ExcelSheetHandler.cs:1641`, `:1710` and `GoogleSheetHandler.cs:1717`, `:1808` for the IDs pass; `separateConstants` at `ExcelSheetHandler.cs:1687`, `:1726` and `GoogleSheetHandler.cs:1787`, `:1824` for the Constants pass. Same author, same file, correct pairing. So `ExportIDs` is a bug, not a design choice.

Effect today: the "Export IDs" button honours the Constants toggle. A user with `separateIDs = false, separateConstants = true` gets one file per sheet instead of the merged `IDs.cs`, and "Export All" then produces a different layout than "Export IDs" from the same settings.

- [ ] **Step 1: Excel**

`ExcelSheetHandler.cs:68` and `:76` — change `m_settings.separateConstants` to `m_settings.separateIDs`. No other change; the structure is already correct (per-sheet writes inside the loop, merged write after it).

- [ ] **Step 2: Google — flag *and* placement**

`GoogleSheetHandler.cs:79-119` has the same wrong flag plus a second defect: the merged-write block (`:108-118`) sits **inside** the `foreach`. So `IDs.cs` is rewritten once per ID sheet, each time with more accumulated content, and the surviving file depends on sheet iteration order.

Restructure to match Excel:

```csharp
foreach (var sheet in m_settings.googleSheetsPath.sheets)
{
    if (!sheet.selected || !sheet.name.EndsWith(SheetXConstants.IDS_SHEET))
        continue;

    var sheetInfo = sheetMetadata.Sheets.FirstOrDefault(s => s.Properties.Title == sheet.name);
    if (sheetInfo == null)
        continue;

    var columnCount = sheetInfo.Properties.GridProperties.ColumnCount;
    var range = $"{sheet.name}!A1:{GetColumnLetter(columnCount.Value)}";
    var request = service.Spreadsheets.Values.Get(m_settings.googleSheetsPath.id, range);
    var response = request.Execute();
    var values = response.Values;

    //Load All IDs
    BuildContentOfFileIDs(sheet.name, values);

    //Create IDs Files
    if (m_settings.separateIDs)
        m_settings.CreateFileIDs(sheet.name, m_idsBuilderDict[sheet.name].ToString());
}

if (!m_settings.separateIDs)
{
    var iDsBuilder = new StringBuilder();
    foreach (var builder in m_idsBuilderDict)
    {
        iDsBuilder.Append(builder.Value);
        iDsBuilder.AppendLine();
    }
    m_settings.CreateFileIDs("IDs", iDsBuilder.ToString());
}
```

- [ ] **Step 3: Leave `ExportConstants` alone**

`ExcelSheetHandler.cs:344`, `:349` (in `ExportConstants` at `:313`) and `GoogleSheetHandler.cs:420`, `:424` (in `ExportConstants` at `:376`) read `separateConstants` and are **correct** — that method is about Constants. Do not touch them.

Full inventory of the flag, so nothing is missed and nothing extra is changed:

| File | Line | Flag read | Verdict |
|---|---|---|---|
| `ExcelSheetHandler.cs` | 68, 76 | `separateConstants` | **fix → `separateIDs`** |
| `ExcelSheetHandler.cs` | 344, 349 | `separateConstants` | correct |
| `ExcelSheetHandler.cs` | 1641, 1710 | `separateIDs` | correct |
| `ExcelSheetHandler.cs` | 1687, 1726 | `separateConstants` | correct |
| `GoogleSheetHandler.cs` | 102, 108 | `separateConstants` | **fix → `separateIDs`** |
| `GoogleSheetHandler.cs` | 420, 424 | `separateConstants` | correct |
| `GoogleSheetHandler.cs` | 1717, 1808 | `separateIDs` | correct |
| `GoogleSheetHandler.cs` | 1787, 1824 | `separateConstants` | correct |

Four lines change in total. Everything else on this list stays.

- [ ] **Step 4: CHANGELOG — call the behavior change out loudly**

This changes output for users who had the two toggles set differently. It is a fix, but it is observable.

```markdown
### Fixed
- `Export IDs` honoured the "Separate Constants Sheets" toggle instead of "Separate IDs
  Sheets" in both the Excel and Google exporters. **If you had those two toggles set
  differently, the IDs output layout changes with this release** — it now matches what
  `Export All` already produced.
- Google `Export IDs` wrote the merged `IDs.cs` inside the per-sheet loop, so the file was
  rewritten once per sheet and the final content depended on sheet order.
```

---

## Task 3: Fix crashes on ragged sheets

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs`
- Modify: `Assets/RCore.SheetX/CHANGELOG.md`

Five unguarded accesses. All are reachable from ordinary spreadsheets — a blank header cell or a short row is not malformed input, it is Tuesday.

- [ ] **Step 1: Excel localization header — `NullReferenceException`**

`ExcelSheetHandler.cs:642`:

```csharp
var fieldName = sheet.GetRow(0).GetCell(col).ToString();
```

`GetCell` returns `null` for an empty cell inside `LastCellNum`. Also re-fetches row 0 every iteration when `firstRow` is already captured at `:630`. Use the null-safe extension that the very next line already uses:

```csharp
var fieldName = firstRow.GetCell(col).ToCellString();
```

`SheetXExtension.ToCellString` (`SheetXHelper.cs:716`) returns the default for a null cell — verified. The existing `if (!string.IsNullOrEmpty(fieldName))` guard at `:648` then handles the empty case correctly.

- [ ] **Step 2: Google localization row — `IndexOutOfRangeException`**

`GoogleSheetHandler.cs:702-703`:

```csharp
var fieldName = rowsData[0][col].ToString();
string fieldValue = rowData[col].ToString();
```

`maxCellNum` comes from `firstRow.Count` at `:692`, and the Sheets API truncates each row at its last non-empty cell — so `rowData.Count < maxCellNum` is the normal case for any sheet whose trailing columns are blank on some rows. Both lines also `ToString()` a possibly-null element.

```csharp
string fieldValue = col < rowData.Count ? rowData[col]?.ToString() ?? "" : "";
```

`fieldName` reads `rowsData[0][col]`, and `col < maxCellNum == firstRow.Count`, so that index is in range by construction — but the element can still be null:

```csharp
var fieldName = firstRow[col]?.ToString() ?? "";
```

Use `firstRow` (already captured at `:691`) rather than re-indexing `rowsData[0]`, matching the Excel fix in Step 1.

- [ ] **Step 3: Null-sheet log — `NullReferenceException`**

`ExcelSheetHandler.cs:1043-1046`:

```csharp
if (sheet == null || sheet.LastRowNum == 0)
{
    UnityEngine.Debug.LogWarning($"Sheet {sheet.SheetName} is empty!");
```

Dereferences `sheet` inside the branch that admits `sheet == null`. Use the parameter:

```csharp
UnityEngine.Debug.LogWarning($"Sheet {pSheetName} is empty!");
```

**This is the only instance.** Verified: every other "is empty!" warning already uses `pSheetName` — `ExcelSheetHandler.cs:95`, `:275`, `:368`, `:624`, `:1567` and `GoogleSheetHandler.cs:126`, `:305`, `:442`, `:685`, `:1120`, `:1644`. `:1045` is the lone outlier, so Google needs no equivalent fix.

`ExcelSheetHandler.cs:130` also reads `sheet.SheetName`, but it sits after the null guard returned, so it is safe. Leave it.

- [ ] **Step 3b: Google IDs/Constants key-value pair — `ArgumentOutOfRangeException`**

Two more sites the original survey missed. `GoogleSheetHandler.cs:158` (in `BuildContentOfFileIDs`) and `:322` (in `LoadIDsValues`) both do:

```csharp
var cellValue = rowData[col + 1];
```

inside `for (int col = 0; col < rowData.Count; col += 3)` (`:139`, `:314`). The loop bound admits `col == rowData.Count - 1`, so `col + 1` is out of range whenever a row's cell count is not a multiple of 3 — which the Sheets API produces for any IDs block whose last row leaves the value column blank. `rowData` is `IList<object>`, so this throws `ArgumentOutOfRangeException`, not `IndexOutOfRangeException`.

Both sites already null-check the result on the next line, so the guard is a bounds check folded into the existing fetch:

```csharp
var cellValue = col + 1 < rowData.Count ? rowData[col + 1] : null;
```

Nothing else changes — `:159` and `:323` handle `null` correctly already (`:159` warns the user the key has no value, `:323` skips).

The Excel side does not need this: `ExcelSheetHandler` reads through `row.GetCell(col + 1)`, and NPOI's `GetCell` returns `null` for an out-of-range index rather than throwing. Verified — no Excel equivalent.

- [ ] **Step 4: CHANGELOG**

```markdown
### Fixed
- Localization export threw on a blank header cell (Excel) or a short row (Google Sheets).
- Google IDs and Constants export threw on a row whose trailing value cell was blank.
- Exporting an empty sheet threw instead of logging a warning.
```

---

## Task 4: Hygiene

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXHelper.cs`
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs`
- Delete: `Assets/RCore.SheetX/Samples~/`
- Modify: `Assets/RCore.SheetX/CHANGELOG.md`

- [x] ~~**Step 1: `SheetXHelper` is not a `MonoBehaviour`**~~ — **DROPPED 2026-08-21.**

`SheetXHelper.cs:31` declares `public class SheetXHelper : MonoBehaviour` in an Editor-only assembly. Every member is `static`, so the base class buys nothing and misleadingly implies the type is attachable. Changing it to `public static class SheetXHelper` is a breaking change under the post-1.0 rules (a public type's base type and instantiability both change) — it was the only thing forcing 2.0.0.

**Decision: keep `: MonoBehaviour`, ship 1.1.0.** The cosmetic gain does not justify a MAJOR bump for every consumer. Revisit at the next real MAJOR. Do not compromise by making the class non-static-but-not-`MonoBehaviour` — that is the same break with less benefit.

- [x] ~~**Step 2: `FILE_PATH` points at the wrong folder**~~ — **DONE, superseded by commit `34e5f5f`.**

Do not re-do this step. It was executed ahead of the rest of the plan because it was one of two root causes behind a separate report ("settings gone after every clone"), and the fix that shipped is **broader than what this step described**.

What the step originally proposed — repointing `FILE_PATH` at `Assets/RCore.SheetX/SheetXSettings.asset` — would have fixed neither cause. Package root is still *inside the package*, which for a consumer installing SheetX via UPM git URL resolves under `Library/PackageCache/`: gitignored, and rebuilt from git on every re-resolve.

What actually shipped:

- `FILE_PATH` (`#else` branch only) → `Assets/SheetX/SheetXSettings.asset`, i.e. the **consuming project's** `Assets/`, never the package.
- `AssetDatabase.FindAssets("t:SheetXSettings")` scoped to `new[] { "Assets" }` — unscoped it also searched `Packages/` and resolved the copy shipped inside the package.
- `SheetXSettings.SaveToDisk()` added, plus `SheetXWindow.OnLostFocus`/`OnDisable` → `FlushSettings()`. The second root cause: `EditorUtility.SetDirty` appeared exactly **once** in the whole package (`SheetXSettingsWindow.cs:60`, Settings tab only), so Excel paths, Google sheet lists and sheet selections were mutated in memory and lost at the next domain reload — not merely at clone time.
- Three tests under `Assets/RCore.SheetX/Tests/` (new asmdef), and a CHANGELOG `### Fixed` entry.

The two `ASSETS_STORE` branches (`:54`, `:56`) were left alone, as this step instructed — those are Asset Store SKUs whose folder layout is not present in this repository, so there is nothing to verify against and they may well be correct as shipped. Changing a path you cannot test breaks a shipped product silently.

**Consequence for Task 1:** the settings asset is now a file in the *consumer's* repository that the consumer is told to commit. That makes moving credentials to `EditorPrefs` more urgent, not less — see the storage-split note under Task 1.

- [ ] **Step 3: Delete the empty samples folder**

`Samples~/BasicUsage/SheetXExample/` contains exactly one file: `Resources.meta`. There is no `samples` array in `package.json`, so Package Manager shows no Import button — the folder is invisible and empty at the same time.

Delete `Assets/RCore.SheetX/Samples~/` entirely. `Document/Document.md` (531 lines) is the actual onboarding path. Adding a real sample is a separate piece of work with its own value; a directory that promises one and delivers a stray `.meta` is worse than nothing.

- [ ] **Step 4: CHANGELOG**

```markdown
### Changed
- Removed the empty `Samples~` folder. See `Document/Document.md`.
```

Steps 1 and 2 are both off the table (dropped and already shipped), so this task is now just the `Samples~` deletion. Fold it into the release commit.

---

## Task 5: Version, docs, release

**Files:**
- Modify: `Assets/RCore.SheetX/package.json`
- Modify: `Assets/RCore.SheetX/CHANGELOG.md`
- Modify: `Assets/RCore.SheetX/Document/Document.md`

- [ ] **Step 1: Bump to 1.1.0**

SheetX is at 1.0.2, so `docs/contributing/SEMVER_POLICY.md`'s **post-1.0** column governs: *MINOR must NOT break public API.* Two items originally broke it:

- Task 2 changes observable behavior that existing callers reasonably depend on (the IDs output layout for a given pair of toggles) — listed explicitly under "What counts as breaking".
- Task 4 Step 1 changed a public type's base type. **Dropped 2026-08-21**, so it no longer applies.

`package.json`: `"version": "1.0.2"` → `"1.1.0"`.

**This is a deliberate policy exception and must be written down as one.** No public symbol changes shape, but Task 2's output-layout change is breaking under the document's own definition, and it is shipping as a MINOR anyway — the alternative is a MAJOR bump for every consumer over a bug fix that makes two buttons agree with each other. State the exception in the CHANGELOG, not just here.

Task 1 is not a factor either way: no public symbol changes shape, and `[Obsolete]` on a field is additive.

- [ ] **Step 2: Close out the CHANGELOG**

Replace `[Unreleased]` with `## [1.1.0] - <ISO date>` (existing file format is `## [1.0.2] - 2026-01-02`). Order sections Security, Fixed, Changed. Add a `### Breaking` section for the Task 2 output-layout change with before/after, and name the semver exception explicitly — `SEMVER_POLICY.md` requires a migration note for breaking releases, and this one is shipping as MINOR by choice. Confirm the credential-rotation warning survived to the top — it is the one line a reader must not miss. The `34e5f5f` clone-persistence entry already sits under `[Unreleased]`; keep it under `### Fixed`.

- [ ] **Step 3: Document credential setup**

In `Document/Document.md`, update the Google Sheets setup section: client ID and secret are entered in the Settings window and stored in `EditorPrefs`, per-machine, never committed. Each developer enters their own. State plainly that credentials must not be committed to a shared repository.

- [ ] **Step 4: Follow the release checklist**

Work through `docs/contributing/RELEASE_CHECKLIST.md`. Confirm whether it applies to `Assets/RCore.SheetX` or only to RevCore packages; if only RevCore, note that and skip the RevCore-specific gates.

---

## Acceptance

No automated tests exist for SheetX and the export-flag logic is not extractable from the two handlers without the refactor this plan explicitly declines. So acceptance is a manual matrix. Run it in a scratch project, not the repo project — Task 1 writes to `EditorPrefs` and Task 2 changes output layout.

**Task 1 — credentials**

- [ ] Open a project whose `SheetXSettings.asset` still has the old encrypted values: on first load the console logs the migration, the asset fields go empty, and the Settings window still shows the credential.
- [ ] `git diff` on the asset shows the two fields blanked and nothing else.
- [ ] Google export authenticates successfully after migration (Step 3's property conversion is what makes this pass; if it fails with "Please setup the Client Id and Client Secret", a raw-field read was missed).
- [ ] Clear both `EditorPrefs` keys: all four Google exports (IDs, Constants, Localizations, Json) refuse with "Please setup the Client Id and Client Secret!" rather than throwing. Test all four — they are four separate call sites (Step 3).
- [ ] Save a `.sx` file via the Settings window, then open it in a text editor: no credential appears in it.
- [ ] Load an **old** `.sx` file that still contains encrypted credentials: they land in `EditorPrefs`, the asset fields stay empty, and `git diff` on the asset is clean.
- [ ] Two Unity projects on the same machine with different `Application.identifier`: each keeps its own credential (confirms the key prefix works).
- [ ] **Wrong-key path — the one that can destroy data.** Hand-edit `encryptionKey` in the asset YAML to a different byte list, keep the old encrypted `googleClientId`, then open the window: the console warns it could not decrypt, the asset field is **still populated** (not blanked), and `EditorPrefs` holds no garbage. Then restore the correct key and reopen: migration succeeds normally.
- [ ] Enable `encryptJson` without changing `encryptionKey`: the default-key warning appears (6a: in the console on first export; 6b: as a HelpBox in the Settings tab).

**Task 2 — export flags.** Run "Export IDs" for all four toggle combinations, Excel and Google:

| `separateIDs` | `separateConstants` | Expected |
|---|---|---|
| false | false | one merged `IDs.cs` |
| false | true | one merged `IDs.cs` (this is the case that was wrong) |
| true | false | one file per ID sheet (this is the other case that was wrong) |
| true | true | one file per ID sheet |

- [ ] All four match the table, both exporters.
- [ ] "Export IDs" and "Export All" produce the same IDs layout from the same settings.
- [ ] Google with two or more ID sheets and `separateIDs = false`: `IDs.cs` contains every sheet's content, and reordering the sheets does not change the file.

**Task 3 — ragged input**

- [ ] Excel localization sheet with a blank header cell mid-row: exports, no `NullReferenceException`.
- [ ] Google localization sheet with rows shorter than the header: exports, no `IndexOutOfRangeException`.
- [ ] Google IDs sheet whose last row leaves the value column blank (so the API returns a row with a cell count not divisible by 3): exports, no `ArgumentOutOfRangeException`, and the existing "Key X doesn't have value!" dialog appears instead.
- [ ] Export a sheet with a header row and no data rows: warning logged, no throw.

**Task 4 — hygiene**

- [ ] Project compiles with no warnings introduced.
- [ ] No `Samples~` folder remains.
- [ ] (Step 2 already accepted under `34e5f5f` — settings asset path and persistence are covered by `Assets/RCore.SheetX/Tests/SheetXSettingsTests.cs`, not by this matrix.)

---

## Commit sequence

One commit per task. Each carries its own CHANGELOG entry.

**Reordered 2026-08-21: Task 1 last, because it is gated on the rotation and the other two are not.**

```
fix(sheetx)!: ExportIDs honours separateIDs, not separateConstants
fix(sheetx): guard ragged header rows, short IDs rows, and null sheets
fix(sheetx): move Google OAuth credentials to EditorPrefs     <- blocked on Task 0
chore(sheetx): release 1.1.0, drop empty samples
```

The `!` marks the one commit carrying a breaking change (Task 2's output layout), per Step 1 of Task 5. Task 4 now contributes only the `Samples~` deletion — folded into the release commit rather than getting one of its own.

Task 0 has no commit of its own — record the rotation and the declined history purge in the body of the credentials commit.

Commit `34e5f5f` (`fix(sheetx): keep settings across clones and package re-resolves`) already landed on `main` ahead of this sequence and carries its own CHANGELOG entry. The `[Unreleased] / ### Fixed` block it added is the section the entries below append to.

---

## Consumer note (informational — no work here)

Recorded so the next person does not go looking for the integration code in this repository.

The iKit side needs no change from SheetX. Its plan is:

1. Name a sheet after the target config asset, dotted for the field — `ShopConfig.Entries`.
2. Export per-sheet JSON with `combineJson` off and `encryptJson` off.
3. On the iKit side, an editor utility reads those files, wraps each by its dotted filename into `{"ShopConfig": {"Entries": [ ... ]}}`, and feeds the result to iKit's existing `ConfigService.Patch`, then marks the ScriptableObjects dirty and saves.

The wrapping convention is iKit's, so it lives in iKit. That work is tracked in the iKit repository, not here.
