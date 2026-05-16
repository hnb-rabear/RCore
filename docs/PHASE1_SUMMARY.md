## Phase 1 — Inventory & Characterization Summary

This phase produced the *measurement layer* under which all future phases operate. Nothing in this commit changes runtime behavior; it documents the current state and pins it with tests.

### Numbers at a glance

| Metric | Count | Source |
|---|---|---|
| Total public symbols (both frameworks) | 503 | `docs/api-inventory.csv` |
| RCore public types (class/struct/iface/enum/delegate) | 371 | inventory |
| RevCore public types | 132 | inventory |
| Migration: UNCHANGED | 0 | `docs/migration/rcore-to-revcore-api-map.md` |
| Migration: PORTED (same name, new namespace) | 99 | map |
| Migration: RENAMED (high confidence) | 6 | map |
| Migration: LIKELY (fuzzy ≥ 0.85, needs audit) | 16 | map |
| Migration: GAP (no RevCore equivalent) | 250 | `docs/gap-analysis.md` |
| Characterization tests added | 16 (across 4 files) | see below |
| Public XML doc coverage | 0% (917 undocumented) | `scripts/xmldoc-baseline.json` |

### Migration status interpretation

- **PORTED (99)** is the largest non-gap bucket. These are types like `BigNumber`, `Joystick`, `PoolObject` etc. — same name, new namespace. Consumer migration is a `using` swap. A future Phase 8 codemod will automate this.
- **RENAMED (6)** are mostly the `RPlayerPref*` → `PlayerPref*` pairs. Mechanical rename + codemod.
- **LIKELY (16)** include `BigNumberD`/`BigNumberF` → `BigNumber` (RevCore consolidated the variants), `HorizontalAlignment` → `HorizontalAlignmentUI`, etc. These need maintainer eyes before any codemod is shipped.
- **GAP (250)** is the heart of the decision work for Phase 8. The largest gap clusters:
  - `Services/Firebase` (20), `Services/GameServices` (8), `Services/Notification` (7), `Services/Ads` (7), `Services/IAP` (1) — RevCore explicitly excludes Services scope.
  - `Plugins` (18 native-iOS / native-Android types) — likely DROP or stays in RCore.
  - `Data/RPGBase` (7), `Data/KeyValueDB` (22) — RPGBase has no RevCore home; KeyValueDB partially overlaps with RevCore.Prefs.
  - `Common/Helper` (63 helpers) — many ported but many are RCore-specific.
  - Each row in `docs/gap-analysis.md` has an empty Decision column for the maintainer to fill in (PORT / DROP / REPLACE / DEFER).

### Characterization tests added

Each test PINS the current behavior. Phase 3+ may intentionally change these contracts — when that happens, the test update is the signal in the diff that an observable contract changed.

| File | What it pins |
|---|---|
| `Assets/RevCore/Foundation/Tests/Runtime/Characterization_ColorHelperTests.cs` | `HexToColor("invalid")` returns `default(Color)` silently; null tolerated. |
| `Assets/RevCore/Foundation/Tests/Runtime/Characterization_EventBusTests.cs` | `Publish` with zero listeners is silent no-op; duplicate `Subscribe` is deduped; `ListenerCount` sums invocation lists across types. |
| `Assets/RevCore/Pool/Tests/Runtime/Characterization_RevPoolTests.cs` | Over-capacity `Spawn()` evicts the OLDEST active item (index 0). `LimitNumber == 0` means no cap. `ActiveItems` is a live view. |
| `Assets/RevCore/Timer/Tests/Runtime/Characterization_TimerSchedulerTests.cs` | `Cancel(int)` on empty is no-op. **`Cancel(0)` cancels ALL timers with default id** — sharp edge of the current API. `Cancel(ITimerHandle null)` tolerated. |

### Known issues caught during Phase 1 (deferred to Phase 3)

1. **`JObjectDBManager.Save(now: true)` still throttles 0.2s.** A caller asking for an immediate save can silently fail. Phase 3 will add a `force: true` overload that bypasses the throttle, called from `OnApplicationQuit` / `OnApplicationPause`. No characterization test added — the bug is well-documented enough; pinning it would just block the fix.
2. **`TimerScheduler.Cancel(0)` matches every default-id timer.** This is now characterized so we cannot accidentally "fix" it without a deliberate version bump.
3. **`EventBus.ListenerCount` is O(types × listeners).** Pinned in characterization; Phase 4 will make it O(1) without changing the observable value.

### What still needs the maintainer

Three asynchronous tasks the maintainer (you) can do at any time before Phase 3 starts:

- [ ] Walk through `docs/migration/rcore-to-revcore-api-map.md` LIKELY section (16 rows). Confirm or correct each.
- [ ] Walk through `docs/gap-analysis.md` (250 rows, but grouped by module — 20 module sections to skim). Fill in the Decision column for each row.
- [ ] When at a PC: drop the `Microsoft.CodeAnalysis.PublicApiAnalyzers` DLL under `Assets/RevCore/_Analyzers/` per the wiring note in `docs/contributing/PUBLIC_API_GUIDE.md`. Until then, the `PublicAPI.{Shipped,Unshipped}.txt` files are review-only.

Phase 2 (test infrastructure: coverage + benchmark suite) can start in parallel with these.
