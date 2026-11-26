---
name: unity-texturepacker-atlas-migration
description: "Use when moving Unity prefabs or scenes from loose sprite textures to a TexturePacker atlas (.tps), or atlas sprites lose pivots after a re-publish, in a project with the RCore Atlas Migration package."
---

# Migrate loose textures to a TexturePacker atlas

`RCore.Editor.Tool.AtlasMigration` (package `com.rabear.rcore.atlasmigration`) pairs each loose sprite with its atlas sprite by name, writes the old pivot and 9-slice border into the `.tps`, and swaps references. The TexturePacker Importer re-reads pivots from the `.tpsheet` on every import, so the values must go into the `.tps` and be published; editing the atlas `.meta` is lost on the next publish.

## Before you start

Stop and tell the user if any of these fail:

- UnityMCP is connected (`execute_code` works). The API needs the open Editor.
- The user names three paths: the folder of loose textures, the atlas PNG, and the `.tps`. Ask; do not guess. The user may also give a scope: a list of folders or assets to change (default: all of `Assets/`).
- `git status` shows the `.tps` and the scope committed. Git is the only backup.
- The project has the TexturePacker Importer (atlas sprites load from the PNG).
- The `.tps` packs the loose folder itself (its `fileList` contains that folder or a parent of it), not another copy of the PNGs.

Find the CLI: `TexturePacker` on `PATH`, else `C:\Program Files\CodeAndWeb\TexturePacker\bin\TexturePacker.exe`, else `/Applications/TexturePacker.app/Contents/MacOS/TexturePacker`. Not found: the user publishes in the TexturePacker app at step 3.

## Steps

Every call takes the same three paths. Print each report:

```csharp
var r = RCore.Editor.Tool.AtlasMigration.Plan("Assets/Art/Loose", "Assets/Art/Atlas/ui.png", "Assets/Art/Atlas/ui.tps");
return UnityEngine.JsonUtility.ToJson(r, true);
```

If the user gave a scope, pass the same `scope` array as the last argument to every `Plan` and `Apply` call; `WriteTps` takes no scope. Otherwise `assets` lists the whole project.

```csharp
var scope = new[] { "Assets/Prefabs/UI", "Assets/Scenes/Main.unity" };
var r = RCore.Editor.Tool.AtlasMigration.Plan("Assets/Art/Loose", "Assets/Art/Atlas/ui.png", "Assets/Art/Atlas/ui.tps", scope);
return UnityEngine.JsonUtility.ToJson(r, true);
```

1. **`Plan`.** Show the user `unmatchedOld`, `unmatchedNew`, `notes`, and the count of `assets`. Status `pivot mismatch` / `border mismatch` is expected now. Point out every `size differs` pair to the user before step 2; borders of those sprites may be written at the wrong scale.
2. **`WriteTps`** (same arguments). Show its `notes`. `added .tps entry …` notes are normal for a `.tps` that never had per-sprite settings. `stale key(s) ignored by TexturePacker: …` in such a note means the `.tps` has old entries that point to no file; the user may delete them in TexturePacker. `is on another drive than the .tps` means the user must add that file in TexturePacker. `is not in the .tps fileList` means TexturePacker packs another copy; stop and ask the user which folder the `.tps` packs.
3. **Publish.** `& "<cli>" "<absolute .tps path>"`. Non-zero exit or an error line: show it and stop. Nothing is replaced yet; `git checkout -- <tps>` undoes step 2. A good run exits 0 and prints `Processing <tps>...`; `Input files have the same content, nothing to do` also exits 0. If step 4 still shows mismatches after that message, ask the user to open the `.tps` in the TexturePacker app and publish there.
4. **`Plan` again.** Every pair should be `ok`. List every other status:
   - `pivot mismatch` / `border mismatch` after a publish: the `.tpsheet` did not take it. Check `notes` for `.tpsheet has pivot points disabled` / `.tpsheet has borders disabled`.
   - `size differs` (or `size differs, pivot mismatch`): scale or a wrong pair. Borders were not compared.
   - `; .tps ambiguous: ...` after the status: two keys match one file. The user must remove one in TexturePacker.
   - A `pixels per unit` note: `SpriteRenderer`s will change size.
5. **Ask the user.** Show the step 4 report and the `assets` list. **Do not run step 6 without an explicit yes.**
6. **`Apply`** (same arguments, with the same `scope` if the user gave one). Show `assets` (changed) and `notes` (skipped open assets). Read the summary note; if it shows `skipped` or `failed` above 0, tell the user which assets were not changed (step 7 lists them).
7. **`Plan` again.** `assets` must be empty. Then tell the user what this tool does not change:
   - materials and `.spriteatlas` packables that still use the old textures (a second copy in the build);
   - `RawImage`, materials and other references to the loose `Texture2D` itself (only sprite references are swapped, so an empty `assets` list does not cover them);
   - `Resources.Load` strings, Addressables addresses, and config or sheet data with texture names.

Never delete the old textures. Suggest an unused-asset check and a separate commit.
