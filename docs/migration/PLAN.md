# Migration Playbook: RCore → RevCore

This document is the step-by-step playbook a consumer project follows when it has decided to migrate. The plan is staged so each step is independently reversible — never rewrite the whole codebase in one PR.

Read [`README.md`](README.md) first if you are wondering whether to migrate at all. The short answer is "probably not yet" — RCore continues to ship and is not deprecated.

## 0. Prerequisites

- A green test suite on the consumer project, against the current RCore baseline. Without this, you cannot tell whether a regression came from your migration or from existing code.
- A git branch dedicated to migration. Do not migrate in `main` / `master` / `develop`.
- The current `Assets/RCore/` folder kept in place. RCore and RevCore are designed to coexist — different namespaces, different asmdefs, no compile collisions. Removing RCore is the **last** step, not the first.
- Read the [Migration map](rcore-to-revcore-api-map.md) end to end. Skim, then bookmark.

## 1. Inventory your usage

Before changing anything, find out which RCore types your project actually touches.

```bash
# Run from the consumer project root.
# All RCore type references in your code:
grep -RIn --include='*.cs' -E '\b(RCore\.|using RCore)' Assets/ > rcore-usage.txt

# All RCore-prefixed type names (catches `RPlayerPrefInt`, `REditorPrefBool`, etc.):
grep -RIn --include='*.cs' -E '\bR(Player|Editor)Pref[A-Z]\w*\b' Assets/ >> rcore-usage.txt
```

Cross-reference each unique type name in `rcore-usage.txt` against [`rcore-to-revcore-api-map.csv`](rcore-to-revcore-api-map.csv) and classify it into one of four buckets:

| Bucket | Map status | What you do |
|---|---|---|
| **Mechanical rename** | `PORTED` (97) or `RENAMED` (15) | Step 3 — `using RCore;` → `using RevCore;` plus the few type renames. |
| **GAP — replaceable** | `GAP` rows mapped to `REPLACE` in [`gap-categories.md`](gap-categories.md) | Step 4 — replace with the documented RevCore equivalent (often a different API shape). |
| **GAP — gone** | `GAP` rows mapped to `DROP` in [`gap-categories.md`](gap-categories.md) | Step 5 — fork the source into your own project, or stay on RCore for that type only. |
| **GAP — open** | `GAP` rows mapped to `DEFER` in [`gap-categories.md`](gap-categories.md) | Step 6 — decide per-type. Usually: keep using RCore for now, file a feature request. |

After this step you should have a count per bucket. If "mechanical rename" is ≥80 % of your usage, the migration is cheap. If "GAP — gone" is non-trivial, weigh the cost before continuing.

## 2. Module-by-module order

Migrate modules in dependency order. Foundation first because everything else depends on it.

1. **Foundation** — `EventBus`, `BigNumber`, `Result<T>`, helpers (`MathHelper`, `ColorHelper`, `TransformHelper`, …). Most consumers touch these everywhere; do this first so the rest of the migration has a stable base.
2. **Prefs** — typed `PlayerPref{Int,Float,Bool,String}` wrappers, EditorPrefs equivalents.
3. **Pool** — `RevPool`, `PoolsContainer<T>`. Old `RCore.Common.Pool.CustomPool` callers will need behavior validation.
4. **Timer** — `TimerScheduler`, `CountdownTimer`. RCore's `Common/Timer/*Event*` types are conceptually replaced by the `TimerScheduler` API; this is **not** a mechanical rename.
5. **Audio** — `BaseAudioManager`, `AudioCollection`. Mostly PORTED; namespace-only change for most callers.
6. **Data** — `JObjectDB`, `JObjectModel<T>`, `JObjectData`. Save-file compatibility section below.
7. **Inspector** — `[ShowIf]`, `[AutoFill]`, `[ReadOnly]`, etc. Drawer rewires are runtime-invisible.
8. **UI** — `PanelStack`, `PanelController`, `JustButton`, `JustToggle`, `OptimizedScrollView`. Heavy module; do last.

Treat each module as its own PR. Run your full test suite between modules.

## 3. Mechanical renames (PORTED + RENAMED)

`PORTED` means "same type name, different namespace" — most of the framework. `RENAMED` means "different type name plus different namespace".

**PORTED batch** — replace top-of-file `using` directives:

```diff
-using RCore;
-using RCore.Common;
-using RCore.UI;
+using RevCore;
```

The RevCore namespace is flat at the runtime level (`RevCore`, `RevCore.Tests`); sub-namespaces in RCore (`RCore.Common.Helper`, `RCore.Audio`, …) are collapsed.

**RENAMED batch** — 13 verified renames (post-Phase 8 audit). The audit promoted 7 LIKELY rows to RENAMED and demoted 9 to GAP:

| Old RCore type | New RevCore type | Notes |
|---|---|---|
| `BigNumberD` | `BigNumber` | RCore had double-backed and float-backed variants. RevCore consolidates. |
| `BigNumberF` | `BigNumber` | Same. Verify numeric precision in your code paths. |
| `Common.RPlayerPrefBool` | `Prefs.PlayerPrefBool` | Already mapped in `rcore-to-revcore-api-map.csv`. |
| `Common.RPlayerPrefInt` | `Prefs.PlayerPrefInt` | Same. |
| `Common.RPlayerPrefFloat` | `Prefs.PlayerPrefFloat` | Same. |
| `Common.RPlayerPrefString` | `Prefs.PlayerPrefString` | Same. |
| `JObjectDBManagerV2` | `JObjectDBManager<T>` | Now generic over the collection type. Old non-generic callers add a type argument. |
| `JObjectHandler` | `IJObjectHandler` | Class → interface. Consumers that inherited `JObjectHandler` now `implements IJObjectHandler`. |
| `UI.Layout.HorizontalAlignment` | `UI.HorizontalAlignmentUI` | UI suffix added to disambiguate from `TextAnchor`. |
| `UI.Layout.TableAlignment` | `UI.TableAlignmentUI` | Same. |
| `UI.Layout.VerticalAlignment` | `UI.VerticalAlignmentUI` | Same. |
| `UI.SimpleButton` | `UI.SimpleTMPButton` | Same `JustButton` base, label is TextMeshPro. Deleted from RevCore in v0.5.0 — only the SimpleTMPButton remains. |
| `UI.CustomToggleSlider` | `UI.JustToggle` | Same `Toggle.isOn` contract, richer transition system. CustomToggleSlider deleted from both frameworks in v0.5.0. |

A future automated script can apply the PORTED + RENAMED batches mechanically — see §6.

## 4. GAP — replaceable (`gap-categories.md` says REPLACE)

For each `REPLACE` category, the new API has a different shape. Common examples:

- `RCore.Common.Timer.CountdownEvent` + `TimerEvents` → `RevCore.TimerScheduler.WaitForSeconds(...)`. The event-bus pattern is gone; you schedule and hold an `ITimerHandle`.
- `RCore.Common.Encryption` / `IEncryption` → no replacement. RevCore intentionally does not ship value encryption; see [`Assets/RevCore/Prefs/README.md`](../../Assets/RevCore/Prefs/README.md) safety notes. Route sensitive state through your backend.
- `RCore.JObjectHandler` (class) → `RevCore.IJObjectHandler` (interface). Inheritance becomes implementation.

Spend the most time here — a wrong `REPLACE` rewrite ships subtle behavior bugs that pass compile.

## 5. GAP — gone (`gap-categories.md` says DROP)

For each `DROP` category, RevCore does not ship the type and never will. Three options:

1. **Stay on RCore for that type only.** RCore and RevCore coexist. Keep the `RCore.*` reference; the rest of your file can use `RevCore.*`. This is the default — pick it unless you have a specific reason not to.
2. **Fork the source into your project.** Copy `Assets/RCore/.../Foo.cs` into `Assets/YourProject/Vendored/Foo.cs`, renamespace to your project namespace, remove the RCore dependency. Cleaner long-term but means you own the maintenance.
3. **Switch to a third-party library.** For `Services/Ads`, `Services/Firebase`, `Services/IAP`, etc., the canonical answer: integrate the vendor SDK directly without an RCore wrapper. RCore's wrappers added little value over the SDK.

## 6. Save-file compatibility (Data module)

RevCore.Data uses the same PlayerPrefs keys + same JSON shape as RCore for the JObject family. So a player who upgrades from a build with `RCore.JObjectDB` to a build with `RevCore.JObjectDB` keeps their progress, **if and only if** the consumer's data classes ([`JObjectData`](../../Assets/RevCore/Data/Runtime/JObjectData.cs) subclasses) keep the same field names and serialization shape.

If you renamed fields, restructured arrays, or changed types as part of the migration:

- Implement the migration in [`JObjectModel<T>.OnPostLoad`](../../Assets/RevCore/Data/Runtime/JObjectModel.cs) — this is the canonical place to fix up older data, decided in the Phase 7.2 design call (versioned envelopes were rejected as unnecessary).
- Test by loading a real shipped save before merging.

`RCore.Data.KeyValueDB` does **not** survive the migration. Its 21 types are flagged DROP in [`gap-categories.md`](gap-categories.md); consumers using KeyValueDB write a one-time `KeyValueDB → JObjectDB` exporter in their bootstrap, or stay on RCore for that subsystem.

## 7. Validation

Per module, before merging:

- Compile clean (no new warnings) on the consumer's target Unity version.
- Full test suite green.
- Smoke test the affected feature in PlayMode.
- For Data: load a real save and assert the player visible state matches the pre-migration build.
- For UI: walk every panel; assert no `Coroutine couldn't be started because the game object is inactive` regressions (the fix lives in `RevCore.PanelController.Show` — see [`CHANGELOG.md`](../../CHANGELOG.md)).

After all modules are migrated:

- Search for any remaining `RCore.` references. Anything left is either a deliberate "stay on RCore" choice (document it inline) or a missed migration (finish it).
- Only then consider removing `Assets/RCore/` from the project. Hold off — there is no upside to deleting it, and reverting becomes much harder.

## 8. Rollback

Each module-PR is independently revertible. Revert order is the reverse of §2 (UI first, Foundation last). If a deep Foundation revert is needed, freeze migration progress on the branch and ship from the previous tag — do not try to partially-revert Foundation while later modules depend on the new types.

## 9. Sketch of a future migration tool

If a consumer decides they want automation:

- **Input**: their `Assets/` folder + a checked-out copy of `docs/migration/rcore-to-revcore-api-map.csv`.
- **Pass 1 (mechanical)**: walk every `.cs` file, rewrite top-of-file `using RCore[...];` lines and any fully-qualified `RCore.TypeName` references for rows where `status` is `PORTED` or `RENAMED`. Write diffs to a preview folder; require `--apply` to commit changes.
- **Pass 2 (audit)**: for each `GAP` reference, emit a CSV row with `(file, line, type, gap_category, recommended_action)` so the operator can triage the long tail in their IDE.
- **Out of scope**: type-shape changes (e.g. `JObjectHandler` → `IJObjectHandler` is an inheritance → implementation flip; no regex catches that safely). Pass 2 reports these so the operator does them by hand.

The script lives in a future phase, not Phase 8.
