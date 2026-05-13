# RevCore.Prefs

Typed PlayerPrefs and EditorPrefs wrappers for RevCore.

## Install

Unity Package Manager, local path:

```text
Assets/RevCore/Prefs
```

Or by name when published:

```json
"com.rabear.revcore.prefs": "0.1.0"
```

Requires:

```json
"com.rabear.revcore.foundation": "0.1.0"
```

## 60-second Quick Start

```csharp
using RevCore;

public class Settings
{
    private PlayerPrefBool m_musicEnabled = new("music_enabled", true);
    private PlayerPrefInt m_highScore = new("high_score");

    public bool MusicEnabled
    {
        get => m_musicEnabled.Value;
        set => m_musicEnabled.Value = value;
    }

    public int HighScore
    {
        get => m_highScore.Value;
        set => m_highScore.Value = value;
    }

    public void Save() => PlayerPrefContainer.SaveChanges();
}
```

## Concepts

### PlayerPref wrappers

Typed wrappers around `UnityEngine.PlayerPrefs`. Changes are tracked via a dirty flag and only written on `SaveChange()` or `PlayerPrefContainer.SaveChanges()`.

```csharp
var score = new PlayerPrefInt("high_score", 0);
score.Value = 100;
PlayerPrefContainer.SaveChanges();
```

### EditorPref wrappers

Editor-only wrappers around `UnityEditor.EditorPrefs`. Auto-saved via `EditorApplication.update`.

```csharp
// Editor code only
var showGizmos = new EditorPrefBool("show_gizmos", true);
showGizmos.Value = false;
```

### Batched save

`PlayerPrefContainer.SaveChanges()` saves all registered prefs at once. Call at checkpoints (scene transitions, pause, etc.) rather than on every change.

## API Reference

| Type | Assembly | Purpose |
|---|---|---|
| `IPref` | Runtime | Shared contract for all prefs |
| `PlayerPref` | Runtime | Base class for PlayerPrefs wrappers |
| `PlayerPrefBool` | Runtime | Bool via `GetInt`/`SetInt` |
| `PlayerPrefInt` | Runtime | Int wrapper |
| `PlayerPrefFloat` | Runtime | Float wrapper |
| `PlayerPrefString` | Runtime | String wrapper |
| `PlayerPrefContainer` | Runtime | Batched save/delete for all registered prefs |
| `EditorPref` | Editor | Base class for EditorPrefs wrappers |
| `EditorPrefBool` | Editor | Bool via `GetInt`/`SetInt` |
| `EditorPrefInt` | Editor | Int wrapper |
| `EditorPrefFloat` | Editor | Float wrapper |
| `EditorPrefString` | Editor | String wrapper |
| `EditorPrefEnum<T>` | Editor | Enum via `GetInt`/`SetInt` |
| `EditorPrefVector` | Editor | Vector3 via three float keys |
| `EditorPrefContainer` | Editor | Auto-save via `EditorApplication.update` |

## Migration from RCore

| RCore | RevCore.Prefs |
|---|---|
| `RPlayerPrefBool` | `PlayerPrefBool` |
| `RPlayerPrefInt` | `PlayerPrefInt` |
| `RPlayerPrefFloat` | `PlayerPrefFloat` |
| `RPlayerPrefString` | `PlayerPrefString` |
| `RPlayerPrefContainer.SaveChanges()` | `PlayerPrefContainer.SaveChanges()` |
| `RPlayerPrefList<T>` | Deferred to RevCore.Data |
| `RPlayerPrefDict<TKey,TVal>` | Deferred to RevCore.Data |
| `RPlayerPrefObject<T>` | Deferred to RevCore.Data |
| `REditorPrefBool` | `EditorPrefBool` |
| `REditorPrefEnum<T>` | `EditorPrefEnum<T>` |
| `REditorPrefVector` | `EditorPrefVector` |

## Safety Notes

- Prefs wrappers do not use `Newtonsoft.Json` or `Encryption`. Complex JSON/encrypted prefs belong in RevCore.Data.
- Runtime has no `UnityEditor` dependency.
- EditorPrefs wrappers live in a separate Editor asmdef.
- `PlayerPrefContainer` does not auto-save. Call `SaveChanges()` explicitly.
- `EditorPrefContainer` auto-saves via `EditorApplication.update`.
