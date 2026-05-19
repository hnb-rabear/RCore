# Session Handoff — RevCore.Addressables v1.0.0 branch (2026-05-19)

Read this first in any new Claude Code session. This repo houses two Unity frameworks side by side: **RCore** (legacy, `Assets/RCore/`, frozen and still used by production consumer projects) and **RevCore** (`Assets/RevCore/`, next-generation modular UPM framework). Active work stays in RevCore; do not edit RCore unless the user explicitly asks.

---

## 0. Project context

RevCore is maintained as independent UPM packages. Quality bar is company-framework level: public API paper trail, 100% XML doc coverage, CHANGELOG entry per PR-sized change, no hidden cross-package dependencies.

Current focus: **RevCore.Addressables v1.0.0**, a new standalone package replacing the RCore Addressables helper/wrapper family by design, not by copy-paste.

---

## 1. Branch + PR state

- **Current working branch**: `feat/addressables-v1.0`
- **Base branch**: `main`
- **Remote branch**: `origin/feat/addressables-v1.0`
- **Push state**: pushed successfully.
- **PR state**: not created from CLI because `gh` is not installed in Bash or PowerShell PATH.
- **Manual PR URL**: `https://github.com/hnb-rabear/RCore/pull/new/feat/addressables-v1.0`

Recommended PR title:

```text
feat(addressables): RevCore.Addressables v1.0.0
```

Recommended PR body:

```markdown
## Summary

- New standalone package `com.rabear.revcore.addressables` v1.0.0.
- Wraps Unity Addressables behind a UniTask-first API; throws `AddressableLoadException` on terminal failure; honours `CancellationToken` + `IProgress<float>` everywhere.
- Adds serialisable wrappers (`AssetRef<T>`, `KeyedAssetRef<TKey,T>`, `ComponentRef<TComponent>`, `PrefabRef<TComponent>`) to replace RCore's `AssetBundleRef` family.
- Zero dependency on other RevCore packages.

See `docs/superpowers/specs/2026-05-19-revcore-addressables-design.md` for the design and `docs/migration/rcore-to-revcore-api-map.csv` for the row-by-row migration map.

## Test plan

- [ ] Unity EditMode tests pass (loader, downloader, catalog, wrappers — fake `IResourceLocator`) — skipped in CLI; Unity unavailable.
- [ ] Unity PlayMode tests pass (instantiate, scene, prefab ref) — skipped in CLI; Unity unavailable.
- [x] `python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json` reports 100% (`1015/1015`).
- [x] PublicAPI surface sealed (`Unshipped.txt` empty except header).
- [ ] Manual smoke: load a known address from a sample scene, instantiate, release, verify no leak in Addressables profiler.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

---

## 2. What changed in RevCore.Addressables

New package path: `Assets/RevCore/Addressables/`

Package metadata:

- UPM ID: `com.rabear.revcore.addressables`
- Version: `1.0.0`
- Unity: `2022.3`
- Dependencies:
  - `com.unity.addressables` `1.22.0`
  - `com.cysharp.unitask` `2.5.10`
- Runtime asmdef: `RevCore.Addressables.Runtime`
- Namespace: `RevCore`
- RevCore package dependencies: none

Runtime surface:

- `AddressableLoadException`
- `AddressableLoader`
- `AddressableDownloader`
- `AddressableCatalog`
- `AddressableScene`
- `AssetRef<T>`
- `KeyedAssetRef<TKey, T>`
- `ComponentRef<TComponent>`
- `PrefabRef<TComponent>`

Docs:

- `Assets/RevCore/Addressables/README.md`
- `Assets/RevCore/Addressables/CHANGELOG.md`
- `docs/superpowers/specs/2026-05-19-revcore-addressables-design.md`
- `docs/superpowers/plans/2026-05-19-revcore-addressables-implementation.md`
- `docs/migration/rcore-to-revcore-api-map.csv`
- `docs/migration/rcore-to-revcore-api-map.md`

---

## 3. Contract decisions

- RCore remains frozen. Addressables work used RCore as anchor/reference only.
- Addressables package is standalone. It must not depend on Foundation, Timer, Audio, Pool, Data, UI, Inspector, or Tools.
- Helpers are stateless wrappers over Unity Addressables.
- Wrappers own serialized references and cached runtime state.
- All async public methods use UniTask.
- Progress parameters use `IProgress<float> progress = null`.
- Cancellation parameters use `CancellationToken ct = default`.
- Terminal failures throw `AddressableLoadException` with key, status, and inner exception.
- Cancellation does not eagerly release in-flight Addressables handles; cleanup is attached to completion.
- `LoadAssetWithHandleAsync<T>` transfers handle ownership to caller.
- `PrefabRef<TComponent>.InstantiateAsync` defaults `defaultActive = false`.
- `PrefabRef<TComponent>` exposes `IsLoading` and `IsLoaded` through its inner `ComponentRef<TComponent>`.

---

## 4. Verification state

Completed:

```powershell
python scripts/check-xmldoc-coverage.py --root Assets/RevCore --baseline scripts/xmldoc-baseline.json
# Public members: 1015; Documented: 1015; Undocumented: 0; Coverage: 100.00%
```

Also completed:

- PublicAPI sealed for Addressables.
- Existing Audio and Timer `PublicAPI.Unshipped.txt` entries were also promoted by `scripts/seal-public-api.py --root Assets/RevCore` because the script processes all modules.
- `docs/api-inventory.csv` refreshed after sealing.
- Addressables README and changelog written.
- Migration CSV extended with Addressables replacement rows.

Not completed in shell:

- Unity EditMode tests.
- Unity PlayMode tests.
- Manual Addressables smoke test.

Reason: Unity Editor/Test Runner unavailable in CLI environment.

---

## 5. Current docs update

After Addressables implementation, docs were updated to avoid stale v1.0-only references:

- `docs/ARCHITECTURE.md` now includes RevCore.Addressables as runtime package, clarifies UniTask is optional for core but required by Addressables, updates XML doc coverage to `1015/1015`, and links the Addressables README.
- `docs/migration/README.md` now documents REPLACED/DROPPED Addressables rows and marks CSV as migration tooling source of truth.
- `docs/migration/rcore-to-revcore-api-map.md` regenerated from CSV so it includes `REPLACED` and `DROPPED` sections.

---

## 6. Recent commit stack on `feat/addressables-v1.0`

```text
2db5687 release(addressables): v1.0.0
a50ed4a chore(release): seal PublicAPI and refresh api-inventory
76e5a22 fix(migration): quote CSV cells with generic type params containing commas
a8abacf docs(migration): add RCore→RevCore Addressables row map
1c6e219 docs(addressables): production README with quickstart, surface table, lifetime contract
70ede3c fix(addressables): align PrefabRef with design spec
d16b48d feat(addressables): PrefabRef<TComponent>
068f853 feat(addressables): ComponentRef<TComponent>
e6cac0c feat(addressables): KeyedAssetRef<TKey, T>
7506565 fix(addressables): AssetRef<T> IsLoading gap and concurrent-LoadAsync handle capture
b81d03e feat(addressables): AssetRef<T> serializable wrapper
62ac5f1 feat(addressables): AddressableScene (load/unload)
e29bdf1 feat(addressables): AddressableCatalog (check + update)
c5d1b15 feat(addressables): AddressableDownloader (size/download/clear-cache)
351fbfd docs(addressables): complete AddressableLoader XML docs
ad112d6 docs(addressables): expand AddressableLoader XML docs for batch and instance APIs
6f3bca8 feat(addressables): AddressableLoader batch + instantiate + locations + release
74ae954 fix(addressables): wrap sync throw in LoadAssetAsync(string) consistent with reference overloads
088a319 fix(addressables): wrap sync throws in LoadAssetAsync(AssetReference) and LoadAssetWithHandleAsync
adf1a1a feat(addressables): AddressableLoader reference overloads + handle variant
bb8640a feat(addressables): AddressableLoader.LoadAssetAsync(string) (red→green: load_asset_async_*)
931e60d test(addressables): add FakeResourceLocator + AddressableTestFixture base
2c01e7e feat(addressables): add AddressableLoadException (red→green: constructor_populates_key_status_and_inner_exception)
70789ba chore(addressables): add Unity meta files for scaffold
39c3e8b feat(addressables): scaffold RevCore.Addressables package (v1.0.0)
```

---

## 7. Important conventions

- Private instance fields: `m_camelCase`.
- Private static fields: `s_camelCase`.
- Public members: `PascalCase`.
- `.cs` files: tabs + CRLF.
- Other text files: spaces + LF.
- Test methods: `snake_case_descriptive`.
- Public API additions go into module `Runtime/PublicAPI.Unshipped.txt`, then get promoted at release cut.
- Every public member needs XML docs.
- Each PR-sized change gets a CHANGELOG entry.
- Do not stage unrelated dirty Unity/project files.

---

## 8. Known local state caveat

Repo has unrelated dirty/untracked files outside Addressables work (Unity Addressable group assets, editor layout, local `.claude` / `.understand-anything` / `.vscode`, project settings, docs drafts). Preserve them unless user explicitly asks otherwise.
