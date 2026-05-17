## Phase 3 — Non-breaking Safety Fixes Summary

Phase 3 commits land as individual PR-sized changes, each with its own commit message containing the rationale. Every fix here is either purely additive (new API) or behavior-preserving (handles a previously-crashing edge case without changing the happy path). No characterization test had to be updated — the contracts they pin still hold.

### Commits in this phase

| Commit | Module | Type | What |
|---|---|---|---|
| `feat(foundation): add ColorHelper.TryHexToColor` | Foundation | additive | Explicit-failure parse variant; old silent-fallback `HexToColor` unchanged. |
| `feat(foundation): add EventBus.ListenerCountFor<T>` | Foundation | additive | Per-type listener count on concrete `EventBus`; not in `IEventBus` to avoid breaking external implementers. |
| `fix(data): JObjectDBManager.SaveForced bypasses 200ms throttle` | Data | additive + behavior | New `SaveForced()`. Internal `OnApplicationPause`/`OnApplicationQuit` now use it so end-of-life writes can no longer be eaten by the throttle. Public `Save(now: true)` is unchanged. |
| `fix(audio): null-safe tweener Kill on volume setters` | Audio | bug fix | `?.Kill()` on `m_masterTweener`/`m_musicTweener`/`m_sfxTweener` — eliminates NRE on first-frame volume calls. No surface change. |
| `fix(ui): scroll views handle totalItems<=0 without crashing` | UI | bug fix | `Initialize`/`Init` early-return on empty data, no more crash on `m_itemsScrolled[0]` access. Horizontal `ScrollBarChanged` no longer log-errors on the empty state. |
| `fix(data): deprecate JObjectDB.collections direct field access` | Data | deprecation Stage 1 | `[Obsolete(error: false)]` on the public static dict. Direct mutation skipped key persistence and was a known source of silent save-file corruption. Becomes private in v1.0. |

### Public API surface changes

`Assets/RevCore/Foundation/Runtime/PublicAPI.Unshipped.txt`:

```
RevCore.ColorHelper.TryHexToColor(string hex, out UnityEngine.Color color) -> bool
RevCore.EventBus.ListenerCountFor<T>() -> int
```

`Assets/RevCore/Data/Runtime/PublicAPI.Unshipped.txt`:

```
virtual RevCore.JObjectDBManager<T>.SaveForced() -> bool
```

Three new public symbols. Zero removals.

### Tests added

| Test file | Asserts |
|---|---|
| `Foundation/Tests/Runtime/TryHexToColorTests.cs` | Valid/invalid/null/empty/RGBA inputs all produce the documented result. |
| `Foundation/Tests/Runtime/EventBusListenerCountForTests.cs` | Zero before subscribe, counts only requested type, returns zero after `Clear<T>()`, aggregate `ListenerCount` contract unchanged. |

### Deferred to later phases

These items were on the original Phase 3 list but were judged either too large for a non-breaking fix, or better landed alongside a related Phase 4/6 change:

- **`BaseAudioManager` voice cap with steal-voice strategy.** This is an additive `[SerializeField] m_maxSfxSources` plus a new policy method; needs a small design pass (which oldest source to steal — non-loop only? lowest volume? least recently used?). Lands in Phase 4 alongside the `m_clipPlayingCount` dictionary optimization.
- **`PlayMusicsCoroutine` infinite-loop guard.** Low priority — only triggered when a music playlist is misconfigured. Will fix when we audit the music playback path.
- **`ShowIfAttribute` `fallbackVisible` option.** Adds a new constructor; while additive, the attribute serialization needs care to not break existing decorated members. Lands in Phase 4.
- **`PoolsContainer.GetActiveList` / `GetAllItems` zero-alloc replacements.** Phase 4 will both deprecate the old methods and add `ForEachActive` / `CopyActiveTo`.

### Gate verification

After every commit on this branch:

- [ ] Unity Tests workflow runs (cannot verify locally — user opens Unity to confirm). Characterization tests must still pass.
- [ ] Benchmark workflow runs (no comparison until baseline is seeded; no regressions expected from these fixes).
- [ ] Docs coverage workflow runs against the baseline — `TryHexToColor`, `ListenerCountFor`, and `SaveForced` all carry XML docs so the gate stays green.
- [ ] PublicAPI analyzer (once DLL is wired) will see the three new entries in `Unshipped.txt`.

### Ready for Phase 4

Phase 4 (performance) can begin once these changes are reviewed. The benchmark suite from Phase 2 is in place to prove every optimization; the Cancel-one-of-1000 baseline in particular will show a ~3-order-of-magnitude drop when the O(1) dict refactor lands.
