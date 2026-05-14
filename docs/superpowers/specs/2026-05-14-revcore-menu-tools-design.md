# RevCore Menu Tools Design

## Goal

Build RevCore editor menu tools while preserving package independence: every package must work fully without knowing any other package exists.

## Constraints

- Scope stays under `Assets/RevCore`.
- Do not modify `Assets/RCore`.
- Do not stage unrelated dirty files.
- Each package owns its own menu entries.
- No package references another package for menu/tool discovery.
- No central compile-time menu registry.
- Optional Hub must not reference RevCore package asmdefs.

## Architecture

RevCore uses package-owned menus plus an optional standalone Hub.

Package-owned menus live in each package's own `Editor` assembly:

- `Assets/RevCore/Data/Editor` owns `RevCore/Data/...`.
- `Assets/RevCore/Audio/Editor` owns `RevCore/Audio/...`.
- `Assets/RevCore/Prefs/Editor` owns `RevCore/Prefs/...`.
- `Assets/RevCore/UI/Editor` owns `GameObject/RevCore/UI/...`.
- Future package menus follow the same pattern.

No package imports menu types from another package. No shared menu constants are required because shared constants would create package-awareness coupling. Each package may define private constants inside its own editor files.

## Menu Layout

Top-level tools:

```text
RevCore/
├── Hub
├── Data/
│   ├── Backup All
│   ├── Restore...
│   ├── Clear All Data
│   └── Log All Data
├── Audio/
│   ├── Create Audio Collection
│   ├── Generate Audio IDs
│   ├── Sort Active Collection
│   └── Validate Addressable Collections
└── Prefs/
    └── Clear PlayerPrefs
```

Context tools:

```text
GameObject/RevCore/
└── UI/
    ├── Replace Button By JustButton
    ├── Replace Button By SimpleButton
    └── Replace Button By SimpleTMPButton
```

`GameObject/RevCore/UI/...` entries already exist and remain UI-owned.

## Optional Hub

Hub is standalone editor code under `Assets/RevCore/Hub`.

Hub has no compile dependency on any RevCore package. It uses Unity editor APIs only:

- `UnityEditor`
- `UnityEditor.PackageManager`
- `UnityEngine`

Hub discovers installed RevCore packages through `PackageManager.Client.List()` and displays package cards. Tool buttons call menu items by string via `EditorApplication.ExecuteMenuItem`.

Hub never calls package classes directly. Missing packages or missing menu entries show disabled or unavailable tool state and do not break compile.

## Package Tool Contracts

Each package tool follows this contract:

1. Menu items are declared in the package's own `Editor` folder.
2. Menu paths use `RevCore/<Package>/...`.
3. Tool code references only that package's runtime/editor types and Unity APIs.
4. Tool code does not import other RevCore package editor namespaces unless the package already has a real package dependency for its own functionality.
5. Optional integrations use Unity symbols such as `#if ADDRESSABLES`.
6. Menu methods are static and internal where possible.

## Data Tools

Data menu tools wrap existing `JObjectDB` operations:

- `RevCore/Data/Backup All` uses `JObjectDB.Backup(path)`.
- `RevCore/Data/Restore...` uses `JObjectDB.Restore(path)`.
- `RevCore/Data/Clear All Data` confirms before `JObjectDB.DeleteAll()`.
- `RevCore/Data/Log All Data` uses `JObjectDB.CopyAllData()` because current RevCore Data exposes copy/export behavior in editor code and no verified `LogData` API exists.

No Data window is added in first implementation because current RevCore has only custom inspectors, not a verified standalone DB editor window.

## Audio Tools

Audio menu tools operate on selected `AudioCollection` assets:

- `RevCore/Audio/Create Audio Collection` creates `AudioCollection.asset` in selected project folder.
- `RevCore/Audio/Generate Audio IDs` reuses generation logic extracted from `AudioCollectionEditor`.
- `RevCore/Audio/Sort Active Collection` sorts selected collection clips by name.
- `RevCore/Audio/Validate Addressable Collections` is compiled only when `ADDRESSABLES` is defined.

Generation logic is moved into package-local static helper `AudioCollectionMenuTools` so inspector and menu use same code without cross-package coupling.

## Prefs Tools

Prefs menu tools stay in `Assets/RevCore/Prefs/Editor`:

- `RevCore/Prefs/Clear PlayerPrefs` confirms, calls `PlayerPrefs.DeleteAll()`, then `PlayerPrefs.Save()`.

This tool depends only on Unity APIs.

## Error Handling

Package tools:

- Show `EditorUtility.DisplayDialog` for destructive actions.
- Show `Debug.LogWarning` when selection does not contain required asset type.
- Avoid silent destructive behavior.

Hub:

- Uses `EditorApplication.ExecuteMenuItem(path)`.
- If false, logs `RevCore Hub: menu item not available: <path>`.
- Missing packages and tools do not cause compile errors.

## Testing

Editor tests verify static helper logic where possible:

- Audio sorting and ID content generation through package-local helper.
- Hub command invocation path list via pure data constants if exposed internally.

Manual Unity verification:

- Open `RevCore/Hub`.
- Run Data backup/restore/clear only on disposable test data.
- Create AudioCollection asset through menu.
- Select AudioCollection, sort clips, generate IDs.
- Clear PlayerPrefs after confirmation.
- Verify existing `GameObject/RevCore/UI/...` menus still appear.

## Non-Goals

- Do not port full RCore `RMenu`.
- Do not add shared menu registry.
- Do not add central dependencies between packages.
- Do not build Data DB editor window until RevCore has verified standalone window requirements.
- Do not modify `Assets/RCore`.
