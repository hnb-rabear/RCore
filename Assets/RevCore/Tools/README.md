# RevCore.Tools

Standalone Unity Editor tools for RevCore projects. Zero dependency on other RevCore packages.

## Install

Unity Package Manager, local path:

```text
Assets/RevCore/Tools
```

Or by name when published:

```json
"com.rabear.revcore.tools": "1.0.0"
```

No dependencies required.

## Usage

Open the hub: **RevCore > Tools Hub**

All tools are discovered via reflection from the `RevCoreTool` base class. Third-party tools can extend `RevCoreTool` to appear in the hub automatically.

## Tools by Category

### Navigate

| Tool | Type | Description |
|---|---|---|
| Scenes Navigator | Window | Build scenes and project scenes with search |
| Asset Shortcuts | Window | Drag-and-drop bookmarks organized by category |
| Project Explorer | Quick Action | Open Assets, StreamingAssets, PersistentData, Editor folders |

### Search

| Tool | Type | Description |
|---|---|---|
| Find By GUID | Quick Action | GUID to asset lookup with ping |
| Component Reference Finder | Window | Find prefabs/assets containing a specific component |
| Objects Finder | Window | Script finder, ParticleSystem finder, PersistentEvent finder |

### Generators

| Tool | Type | Description |
|---|---|---|
| Screenshot Taker | Window | Capture screenshots at custom resolution |
| Characters Set Generator | Quick Action | Extract unique character set from TextAssets |

### UI Tools

| Tool | Type | Description |
|---|---|---|
| Toggle Raycast All | Quick Action | Toggle raycastTarget on selected Graphic components |

### Utility

| Tool | Type | Description |
|---|---|---|
| Auto Play First Scene | Quick Action | Toggle auto-play from first build scene |
| Asset Cleaner | Window | Find unused assets, find references, bulk delete with confirmation |

### Addressables

| Tool | Type | Description |
|---|---|---|
| Addressable Groups Colorizer | Quick Action | Color project window items by Addressable group prefix |

Addressables tools require `com.unity.addressables` package. Guarded by `#if ADDRESSABLES`.

## Extending

```csharp
using RevCore.Tools.Editor;

internal sealed class MyCustomTool : RevCoreTool
{
    public override string Name => "My Tool";
    public override string Category => "Custom";
    public override bool IsQuickAction => true;

    public override void OnGUI()
    {
        // IMGUI code here
    }
}
```

## Helpers

| Type | Purpose |
|---|---|
| `EditorPrefsValue<T>` | Generic EditorPrefs wrapper (string, bool, int, float) |
| `EditorGuiHelper` | IMGUI utilities (ButtonColor, HeaderFoldout, Separator, DragDropBox) |
| `AssetPathHelper` | Path conversion (ToUnityPath, EnsureDirectory) |

## Migration from RCore

| RCore | RevCore.Tools |
|---|---|
| `RCore Hub` menu | `RevCore > Tools Hub` |
| `EditorHelper.FormatPathToUnityPath` | `AssetPathHelper.ToUnityPath` |
| `EditorHelper.HeaderFoldout` | `EditorGuiHelper.HeaderFoldout` |
| `EditorHelper.Button` / `ButtonColor` | `EditorGuiHelper.ButtonColor` |
| `REditorPrefString` / `REditorPrefBool` | `EditorPrefsValue<string>` / `EditorPrefsValue<bool>` |
| `JsonHelper.ToJson` | Direct string operations |
| `Assets/Editor/AssetShortcuts.json` storage | `ProjectSettings/RevCore/AssetShortcuts.json` |
