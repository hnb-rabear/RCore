# RevCore.Tools Package Design

## Goal

Create a standalone, optional `RevCore.Tools` editor-only package containing ported RCore Unity Editor tools. No other RevCore package depends on it. Tools are discoverable via a Hub window that uses reflection.

## Constraints

- Scope: `Assets/RevCore/Tools/`.
- Zero RevCore package dependencies — `RevCore.Tools.Editor.asmdef` references no other RevCore asmdefs.
- Do not modify `Assets/RCore/`.
- All tools are editor-only.
- Existing `Assets/RevCore/Hub/` will be removed and replaced by this package.
- Tools must not reference `EditorHelper`, `REditorPrefString`, `ColorHelper`, `MathHelper`, `RUtil`, `JsonHelper`, `GuiButton`, `IDraw`, or any other RCore helper. All needed helpers are reimplemented locally inside the Tools package.

## Package Structure

```
Assets/RevCore/Tools/
  package.json                          # com.rabear.revcore.tools, version 1.0.0
  Editor/
    RevCore.Tools.Editor.asmdef         # Editor-only, no RevCore refs
    Hub/
      RevCoreToolsWindow.cs             # Main Hub EditorWindow
      RevCoreTool.cs                    # Abstract base class
    Helpers/
      EditorPrefsValue.cs               # Generic EditorPrefs wrapper (replaces REditorPrefString/Bool)
      EditorGuiHelper.cs                # Thin wrappers: Button, ButtonColor, HeaderFoldout, DragDropBox, Separator, Toggle, TextField, ObjectField
      AssetPathHelper.cs                # FormatPathToUnityPath, folder utilities
    Navigate/
      ScenesNavigatorWindow.cs          # Quick-open build/project scenes
      AssetShortcutsWindow.cs           # Category bookmarks with drag/drop (JsonUtility, new storage path)
      ProjectExplorerTool.cs            # Quick action: open common folders
    Search/
      FindByGuidTool.cs                 # Quick action: GUID → asset lookup
      FindComponentReferenceWindow.cs   # Find prefabs containing specific component
      ObjectsFinderWindow.cs            # Container for Script/ParticleSystem/PersistentEvent finders
      ScriptFinder.cs                   # Find used/unused scripts (no delete action in v1)
      ParticleSystemFinder.cs           # Find ParticleSystem prefabs
      PersistentEventFinder.cs          # Find UnityEvent listeners
    Generators/
      ScreenshotTakerWindow.cs          # Capture Game View with resolution/transparency options
      CharactersSetGeneratorTool.cs     # Merge text files → unique char set
    Utility/
      AutoPlayFirstSceneTool.cs         # Toggle auto-play first build scene
      ToggleRaycastAllTool.cs           # Bulk toggle Raycast Target on selected hierarchy
      AssetCleaner/
        AssetCleaner.cs                 # Core logic: find unused assets, build reference cache
        AssetCleanerWindow.cs           # EditorWindow with cleaner + reference finder tabs
        AssetCleanerSettings.cs         # ScriptableObject settings (ignore paths, etc.)
    Addressables/
      AddressableGroupsColorizer.cs     # #if ADDRESSABLES — color-code groups in Project window
      AddressableGroupsColorizerSettings.cs  # ScriptableObject for colorizer rules
```

## Hub Design

### RevCoreToolsWindow

- Opens via menu item `RevCore/Tools Hub`.
- Discovers all classes inheriting `RevCoreTool` via reflection on first open.
- Groups tools by `Category` property.
- Category tabs at top + search field.
- Quick actions render as inline cards with action button.
- Window tools render as list items with "Open" button that calls `EditorWindow.GetWindow<T>()`.
- Refresh button re-scans tools.

### RevCoreTool Base Class

```csharp
public abstract class RevCoreTool
{
    public abstract string Name { get; }
    public abstract string Category { get; }
    public virtual bool IsQuickAction => false;
    public virtual void OnGUI() { }
    public virtual void OnOpen() { }
}
```

- `OnGUI()`: draws inline UI for quick actions inside Hub.
- `OnOpen()`: called when "Open" clicked for window tools. Default opens associated EditorWindow.

## Tool Categories

### Navigate
| Tool | Type | Source |
|------|------|--------|
| Scenes Navigator | Window | ScenesNavigatorWindow.cs |
| Asset Shortcuts | Window | AssetShortcutsWindow.cs |
| Project Explorer | Quick Action | ProjectExplorerTool.cs |

### Search
| Tool | Type | Source |
|------|------|--------|
| Find By GUID | Quick Action | FindByGuidTool.cs |
| Find Component Reference | Window | FindComponentReferenceWindow.cs |
| Objects Finder | Window | ObjectsFinderWindow.cs (3 sub-finders) |

### Generators
| Tool | Type | Source |
|------|------|--------|
| Screenshot Taker | Window | ScreenshotTakerWindow.cs |
| Characters Set Generator | Quick/Window | CharactersSetGeneratorTool.cs |

### Utility
| Tool | Type | Source |
|------|------|--------|
| Auto Play First Scene | Quick Action | AutoPlayFirstSceneTool.cs |
| Toggle Raycast All | Quick/Window | ToggleRaycastAllTool.cs |
| Asset Cleaner | Window | AssetCleanerWindow.cs |

### Addressables
| Tool | Type | Source |
|------|------|--------|
| Addressable Groups Colorizer | Background | AddressableGroupsColorizer.cs |

## Shared Helpers (inside Tools package)

### EditorPrefsValue<T>

Replaces `REditorPrefString`, `REditorPrefBool`. Generic wrapper around `EditorPrefs`.

```csharp
public class EditorPrefsValue<T>
{
    public EditorPrefsValue(string key, T defaultValue = default) { ... }
    public T Value { get; set; }
}
```

Supports `string`, `bool`, `int`, `float`.

### EditorGuiHelper

Static class with thin IMGUI wrappers. Only methods actually used by ported tools:
- `ButtonColor(string label, Color color, float width)`
- `HeaderFoldout(string label, string key = null)`
- `Foldout(string label)`
- `DragDropBox<T>(string label, Action<T[]> onDrop)`
- `Separator()`
- `FormatPathToUnityPath(string absolutePath)` — converts OS path to `Assets/...` relative path.

No `GuiButton`, `GuiToggle`, `IDraw`, `GridDraws`, `PagesForList` — those are too complex and unused by approved tools.

## Safety Rules

- **ScriptFinder**: no `Delete Unused Scripts` button in v1.
- **AssetCleaner**: `DeleteAsset` requires explicit confirmation dialog showing exact file paths.
- **Asset Shortcuts**: storage path changes to `ProjectSettings/RevCore/AssetShortcuts.json` (not `Assets/Editor/`). Uses `JsonUtility` instead of `Newtonsoft.Json`.
- **Addressable Colorizer**: entire file wrapped in `#if ADDRESSABLES`.
- No `DestroyImmediate` without Undo registration or user confirmation.

## Removing Old Hub

- Delete `Assets/RevCore/Hub/` (3 files + 3 metas + folder metas).
- Remove Hub entry from any package.json if referenced.
- `RevCore/Hub` menu item replaced by `RevCore/Tools Hub`.

## External Dependencies

- None required.
- `Newtonsoft.Json` removed (AssetShortcuts switches to `JsonUtility`).
- `TMPro` not needed (skipped tools).
- `Addressables` optional via `#if ADDRESSABLES`.

## Menu Items

Tools Hub opens from: `RevCore/Tools Hub` (priority 0).

Individual tools also register own menu items:
- `RevCore/Tools/Scenes Navigator`
- `RevCore/Tools/Asset Shortcuts`
- `RevCore/Tools/Screenshot Taker`
- `RevCore/Tools/Objects Finder`
- `RevCore/Tools/Asset Cleaner`
- `RevCore/Tools/Find Component Reference`
- `RevCore/Tools/Toggle Auto Play First Scene` (checkmark toggle)
- `RevCore/Tools/Addressable Groups Colorizer Settings` (only with `#if ADDRESSABLES`)
