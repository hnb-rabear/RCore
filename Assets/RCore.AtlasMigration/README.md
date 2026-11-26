# RCore Atlas Migration

Editor-only tool that moves a Unity project from loose sprite textures to the sprites of a packed TexturePacker atlas, keeping each sprite's pivot and 9-slice border. It is built for an AI agent (Claude Code with UnityMCP), and it works in any project: it does not need RCore.

## Install

Add to `Packages/manifest.json`:

```json
"com.rabear.rcore.atlasmigration": "https://github.com/hnb-rabear/RCore.git?path=Assets/RCore.AtlasMigration"
```

The project also needs the TexturePacker Importer (atlas sprites load from the PNG) and, for the agent, the TexturePacker command-line tool.

## Use with an AI agent

Run **RCore → Tools → Install AI Skills**. It copies `unity-texturepacker-atlas-migration/SKILL.md` into the project's `.claude/skills/`; commit it so the team gets it. Then ask the agent, for example: "Migrate the textures in `Assets/Art/Loose` to the atlas `Assets/Art/Atlas/ui.png` with `ui.tps`." The agent stops for your approval before it changes any prefab.

## API (`RCore.Editor.Tool.AtlasMigration`)

| Method | What it does |
| --- | --- |
| `Report Plan(string oldFolder, string atlasTexturePath, string tpsPath, string[] scope = null)` | Pairs loose and atlas sprites by name, compares pivots and borders, lists the assets that reference the loose sprites. Changes nothing. |
| `Report WriteTps(string oldFolder, string atlasTexturePath, string tpsPath)` | Writes each loose sprite's pivot and 9-slice border into the `.tps`. Publish with TexturePacker afterwards. |
| `Report Apply(string oldFolder, string atlasTexturePath, string tpsPath, string[] scope = null)` | Replaces references to the loose sprites with the atlas sprites. Skips assets open in a scene or Prefab Mode. |
| `string[] WriteTpsForWindow(string tpsPath, Sprite[] oldSprites, string outputPath)` | Used by RCore's Replace Sprite window. Never adds `.tps` entries. Changing this signature is a breaking change. |

Print a report with `UnityEngine.JsonUtility.ToJson(report, true)`.

## Limits

- Only sprite references are swapped. Materials, `.spriteatlas` packing lists, `RawImage`, `Resources.Load` strings, Addressables addresses and config data that use the loose textures are not changed.
- RCore's `Assets/Editor/FindAndReplaceAssetToolkit.asset` (the Replace Sprite window's saved pairs) is never rewritten.
- The old textures are never deleted.
