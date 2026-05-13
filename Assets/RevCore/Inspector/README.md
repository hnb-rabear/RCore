# RevCore.Inspector

Attribute-based inspector decorations and custom editors for Unity. No external dependencies.

## Install

Unity Package Manager, local path:

```text
Assets/RevCore/Inspector
```

Or by name when published:

```json
"com.rabear.revcore.inspector": "0.1.0"
```

## 60-second Quick Start

```csharp
using UnityEngine;
using RevCore;

public class Player : MonoBehaviour
{
    [ReadOnly] public int health = 100;
    [Separator("Movement")]
    [Comment("Units per second")]
    public float speed = 5f;
    [Highlight] public string playerName;
    [ShowIf("showDebug")] public bool debugMode;
    public bool showDebug;
    [SingleLayer] public int groundLayer;
    [TagSelector] public string enemyTag;
    [SpriteBox] public Sprite icon;

    [InspectorButton("Reset Health")]
    public void ResetHealth()
    {
        health = 100;
    }
}
```

## Concepts

**Attributes** live in the Runtime assembly so any script can use them. **Drawers** live in the Editor assembly and are never included in player builds.

### Decoration
- `[ReadOnly]` — disable GUI for a field
- `[Separator("title")]` — horizontal line with optional title
- `[Comment("text")]` — help text above field
- `[Highlight]` — cyan-highlighted field

### Conditional
- `[ShowIf("boolFieldOrMethod")]` — show field only when condition is true
- `[AutoFill]` / `[AutoFill("path")]` — auto-assign Component or ScriptableObject refs
- `[InspectorButton]` / `[InspectorButton("label")]` — draw button for a method

### Field Type
- `[DisplayEnum(typeof(MyEnum))]` — draw int as enum popup
- `[SingleLayer]` — draw int as layer dropdown
- `[TagSelector]` — draw string as tag dropdown
- `[FolderPath]` — draw string as folder picker
- `[SpriteBox]` / `[SpriteBox(w, h)]` — sprite preview box

### ScriptableObject
- `[CreateScriptableObject]` — add Create button for SO field
- `[ExposeScriptableObject]` — inline foldout editor for SO

### Custom Editors
- `MeshInfoEditor` — vertex/triangle/submesh count for SkinnedMeshRenderer and MeshFilter
- `MeshRendererEditor` — sorting layer popup and order in layer

## API Reference

| Attribute | Target | Field Type |
|---|---|---|
| `ReadOnly` | Field | Any |
| `Separator` | Field | Any |
| `Comment` | Field | Any |
| `Highlight` | Field | Any |
| `ShowIf` | Field | Any |
| `AutoFill` | Field | Component, ScriptableObject, arrays |
| `InspectorButton` | Method | N/A |
| `DisplayEnum` | Field | int |
| `SingleLayer` | Field | int |
| `TagSelector` | Field | string |
| `FolderPath` | Field | string |
| `SpriteBox` | Field | Sprite |
| `CreateScriptableObject` | Field | ScriptableObject |
| `ExposeScriptableObject` | Field | ScriptableObject |

## Common Use Cases

### Read-only runtime state

```csharp
[ReadOnly] public float elapsedTime;
```

### Conditional debug fields

```csharp
public bool enableDebug;
[ShowIf("enableDebug")] public int debugSeed;
```

### Auto-wire references

```csharp
[AutoFill] public Rigidbody body;
[AutoFill("Canvas/Panel")] public RectTransform panel;
```

### Method buttons

```csharp
[InspectorButton("Spawn Wave")]
public void SpawnWave()
{
    // ...
}
```

## Extension Points

All drawers inherit from `PropertyDrawer` or `DecoratorDrawer`. To customize behavior, subclass and override.

`InspectorButtonEditor` uses `[CustomEditor(typeof(MonoBehaviour), true)]`. If a custom editor for a specific MonoBehaviour subclass exists, Unity will prefer the more specific editor.

## Migration from RCore

| RCore | RevCore.Inspector |
|---|---|
| `using RCore.Inspector;` | `using RevCore;` |
| `[RCore.Inspector.ReadOnly]` | `[RevCore.ReadOnly]` |
| Attribute + drawer in same file | Attribute in Runtime, drawer in Editor |
| `#if UNITY_EDITOR` guards | Clean asmdef split |

All attribute names and constructor signatures match RCore. Migration is a namespace change.

## Troubleshooting

| Problem | Fix |
|---|---|
| Attribute not drawing | Ensure RevCore.Inspector.Editor asmdef is in project |
| ShowIf field always visible | Condition member name must be exact, case-sensitive |
| AutoFill not finding component | Check path string matches child transform name |
| InspectorButton not appearing | Method must be on a MonoBehaviour, not ScriptableObject |
| Compile error on build | Drawers are Editor-only; verify no Runtime code references Editor namespace |
