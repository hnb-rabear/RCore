# RevCore Menu Tools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add zero-dependency RevCore editor menu tools while keeping every package responsible for its own menus.

**Architecture:** Package-owned menu code lives inside each package's own `Editor` assembly. Optional Hub lives in its own `Assets/RevCore/Hub` editor assembly and calls menu items by string through `EditorApplication.ExecuteMenuItem`, with no compile references to RevCore packages.

**Tech Stack:** Unity 2022.3, UnityEditor `MenuItem`, `EditorWindow`, `PackageManager.Client.List`, C# editor asmdefs, NUnit editor tests where helper logic is pure.

---

## File Structure

- Create `Assets/RevCore/Data/Editor/JObjectDBMenu.cs` — owns `RevCore/Data/...` menu entries and calls `JObjectDB` APIs.
- Create `Assets/RevCore/Prefs/Editor/PlayerPrefsMenu.cs` — owns `RevCore/Prefs/Clear PlayerPrefs` menu entry.
- Modify `Assets/RevCore/Audio/Editor/AudioCollectionEditor.cs` — delegate sort/generate/validate logic to package-local helper.
- Create `Assets/RevCore/Audio/Editor/AudioCollectionMenuTools.cs` — owns `RevCore/Audio/...` menu entries and package-local helper methods.
- Create `Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs` — optional hub window, Unity-only references, package discovery, `ExecuteMenuItem` buttons.
- Create `Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef` — references no RevCore asmdefs.
- Create Unity `.meta` files for new folders/files.
- Do not modify `Assets/RCore`.

## Task 1: Data Menu Tools

**Files:**
- Create: `Assets/RevCore/Data/Editor/JObjectDBMenu.cs`
- Create: `Assets/RevCore/Data/Editor/JObjectDBMenu.cs.meta`

- [ ] **Step 1: Add Data menu implementation**

Create `Assets/RevCore/Data/Editor/JObjectDBMenu.cs`:

```csharp
using System;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    internal static class JObjectDBMenu
    {
        private const int Priority = 1500;

        [MenuItem("RevCore/Data/Backup All", priority = Priority)]
        private static void BackupAll()
        {
            var t = DateTime.Now;
            string fileName = $"GameData_{t.Year % 100}{t.Month:00}{t.Day:00}_{t.Hour:00}h{t.Minute:00}";
            string path = EditorUtility.SaveFilePanel("Backup Data", null, fileName, "json");
            if (!string.IsNullOrEmpty(path))
                JObjectDB.Backup(path);
        }

        [MenuItem("RevCore/Data/Restore...", priority = Priority + 1)]
        private static void Restore()
        {
            string path = EditorUtility.OpenFilePanel("Select Data File", Application.dataPath, "json,txt");
            if (!string.IsNullOrEmpty(path))
                JObjectDB.Restore(path);
        }

        [MenuItem("RevCore/Data/Clear All Data", priority = Priority + 2)]
        private static void ClearAllData()
        {
            if (EditorUtility.DisplayDialog("Confirm", "Delete all JObjectDB data?", "Delete", "Cancel"))
                JObjectDB.DeleteAll();
        }

        [MenuItem("RevCore/Data/Log All Data", priority = Priority + 3)]
        private static void LogAllData()
        {
            JObjectDB.CopyAllData();
        }
    }
}
```

- [ ] **Step 2: Add Data menu meta**

Create `Assets/RevCore/Data/Editor/JObjectDBMenu.cs.meta` with a unique 32-hex GUID:

```yaml
fileFormatVersion: 2
guid: 7b0f2a37d1be4fb2b0c9a81e5f613a44
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

- [ ] **Step 3: Review Data menu**

Run:

```powershell
Select-String -Path "Assets/RevCore/Data/Editor/JObjectDBMenu.cs" -Pattern "MenuItem|RCore|RevCore\.Audio|RevCore\.UI|RevCore\.Prefs"
```

Expected:
- Four `MenuItem` matches.
- No `RCore` match.
- No cross-package namespace matches.

## Task 2: Prefs Menu Tool

**Files:**
- Create: `Assets/RevCore/Prefs/Editor/PlayerPrefsMenu.cs`
- Create: `Assets/RevCore/Prefs/Editor/PlayerPrefsMenu.cs.meta`

- [ ] **Step 1: Add Prefs menu implementation**

Create `Assets/RevCore/Prefs/Editor/PlayerPrefsMenu.cs`:

```csharp
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    internal static class PlayerPrefsMenu
    {
        [MenuItem("RevCore/Prefs/Clear PlayerPrefs", priority = 1400)]
        private static void ClearPlayerPrefs()
        {
            if (!EditorUtility.DisplayDialog("Confirm", "Delete all PlayerPrefs?", "Delete", "Cancel"))
                return;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("PlayerPrefs cleared.");
        }
    }
}
```

- [ ] **Step 2: Add Prefs menu meta**

Create `Assets/RevCore/Prefs/Editor/PlayerPrefsMenu.cs.meta`:

```yaml
fileFormatVersion: 2
guid: 1c7b84bc6f5148f99771a912d13c4f02
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

- [ ] **Step 3: Review Prefs menu**

Run:

```powershell
Select-String -Path "Assets/RevCore/Prefs/Editor/PlayerPrefsMenu.cs" -Pattern "MenuItem|RCore|RevCore\.Data|RevCore\.Audio|RevCore\.UI"
```

Expected:
- One `MenuItem` match.
- No `RCore` match.
- No cross-package namespace matches.

## Task 3: Audio Menu Helper and Menu Items

**Files:**
- Create: `Assets/RevCore/Audio/Editor/AudioCollectionMenuTools.cs`
- Create: `Assets/RevCore/Audio/Editor/AudioCollectionMenuTools.cs.meta`
- Modify: `Assets/RevCore/Audio/Editor/AudioCollectionEditor.cs`

- [ ] **Step 1: Add Audio menu helper and menu implementation**

Create `Assets/RevCore/Audio/Editor/AudioCollectionMenuTools.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
#if ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;
#endif

namespace RevCore.Editor
{
    internal static class AudioCollectionMenuTools
    {
        private const int Priority = 1600;

        [MenuItem("RevCore/Audio/Create Audio Collection", priority = Priority)]
        private static void CreateAudioCollection()
        {
            string folder = GetSelectedFolder();
            string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "AudioCollection.asset").Replace("\\", "/"));
            var collection = ScriptableObject.CreateInstance<AudioCollection>();
            AssetDatabase.CreateAsset(collection, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = collection;
        }

        [MenuItem("RevCore/Audio/Generate Audio IDs", priority = Priority + 1)]
        private static void GenerateSelectedAudioIDs()
        {
            if (!TryGetSelectedCollection(out var collection))
                return;

            Generate(collection);
        }

        [MenuItem("RevCore/Audio/Sort Active Collection", priority = Priority + 2)]
        private static void SortSelectedCollection()
        {
            if (!TryGetSelectedCollection(out var collection))
                return;

            Sort(collection);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }

#if ADDRESSABLES
        [MenuItem("RevCore/Audio/Validate Addressable Collections", priority = Priority + 3)]
        private static void ValidateSelectedAddressables()
        {
            if (!TryGetSelectedCollection(out var collection))
                return;

            ValidateAddressables(collection);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }
#endif

        public static void Sort(AudioCollection collection)
        {
            collection.musicClips = (collection.musicClips ?? Array.Empty<AudioClip>())
                .Where(x => x != null)
                .OrderBy(x => x.name)
                .ToArray();
            collection.sfxClips = (collection.sfxClips ?? Array.Empty<AudioClip>())
                .Where(x => x != null)
                .OrderBy(x => x.name)
                .ToArray();
        }

        public static void Generate(AudioCollection collection)
        {
            collection.musicClips ??= Array.Empty<AudioClip>();
            collection.sfxClips ??= Array.Empty<AudioClip>();

            if (!string.IsNullOrEmpty(collection.generator.inputMusicsFolder))
                AddClips(collection.generator.inputMusicsFolder, ref collection.musicClips);

            if (!string.IsNullOrEmpty(collection.generator.inputSfxsFolder))
                AddClips(collection.generator.inputSfxsFolder, ref collection.sfxClips);

            Sort(collection);

            string content = GenerateAudioIDsContent(collection);
            if (!string.IsNullOrEmpty(collection.generator.outputIDsFolder))
            {
                string exportPath = collection.generator.outputIDsFolder;
                if (!exportPath.StartsWith("Assets"))
                    exportPath = "Assets/" + exportPath;
                string fullPath = Application.dataPath + "/" + exportPath.Replace("Assets/", "") + "/AudioIDs.cs";
                File.WriteAllText(fullPath, content);
                AssetDatabase.Refresh();
                Debug.Log($"AudioIDs.cs generated at {fullPath}");
            }

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }

#if ADDRESSABLES
        public static void ValidateAddressables(AudioCollection collection)
        {
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            collection.abSfxClips = new AssetReferenceT<AudioClip>[collection.sfxClips.Length];
            for (int i = 0; i < collection.sfxClips.Length; i++)
            {
                var clip = collection.sfxClips[i];
                if (clip == null) continue;
                string path = AssetDatabase.GetAssetPath(clip);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (addressableSettings.FindAssetEntry(guid) != null)
                {
                    collection.abSfxClips[i] = new AssetReferenceT<AudioClip>(guid);
                    collection.sfxClips[i] = null;
                }
            }

            collection.abMusicClips = new AssetReferenceT<AudioClip>[collection.musicClips.Length];
            for (int i = 0; i < collection.musicClips.Length; i++)
            {
                var clip = collection.musicClips[i];
                if (clip == null) continue;
                string path = AssetDatabase.GetAssetPath(clip);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (addressableSettings.FindAssetEntry(guid) != null)
                {
                    collection.abMusicClips[i] = new AssetReferenceT<AudioClip>(guid);
                    collection.musicClips[i] = null;
                }
            }
        }
#endif

        public static string GenerateAudioIDsContent(AudioCollection collection)
        {
            string musicStringKeys = GenerateStringKeys(collection.musicClips);
            string musicEnumKeys = GenerateIntKeys(collection.musicClips);
            string sfxStringKeys = GenerateStringKeys(collection.sfxClips);
            string sfxEnumKeys = GenerateIntKeys(collection.sfxClips);

            string body = $@"public static class MusicIDs
{{
    public static readonly string[] Names = new string[]
    {{
        {musicStringKeys}
    }};

    public enum Music
    {{
        {musicEnumKeys}
    }}
}}

public static class SfxIDs
{{
    public static readonly string[] Names = new string[]
    {{
        {sfxStringKeys}
    }};

    public enum Sfx
    {{
        {sfxEnumKeys}
    }}
}}";

            if (!string.IsNullOrEmpty(collection.generator.@namespace))
            {
                body = AddTabToEachLine(body);
                body = $"namespace {collection.generator.@namespace}\n{{\n{body}\n}}";
            }

            return body;
        }

        private static bool TryGetSelectedCollection(out AudioCollection collection)
        {
            collection = Selection.activeObject as AudioCollection;
            if (collection != null)
                return true;

            Debug.LogWarning("Select an AudioCollection asset first.");
            return false;
        }

        private static string GetSelectedFolder()
        {
            string path = "Assets";
            if (Selection.activeObject == null)
                return path;

            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (AssetDatabase.IsValidFolder(selectedPath))
                return selectedPath;

            string directory = Path.GetDirectoryName(selectedPath);
            return string.IsNullOrEmpty(directory) ? path : directory.Replace("\\", "/");
        }

        private static void AddClips(string folder, ref AudioClip[] clips)
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
            foreach (string guid in guids)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null && !clips.Contains(clip))
                    ArrayUtility.Add(ref clips, clip);
            }
        }

        private static string SanitizeName(string name)
        {
            return Regex.Replace(name.Replace(" ", "_").Replace("-", "_"), "[^a-zA-Z0-9_]", "");
        }

        private static string GenerateIntKeys(AudioClip[] clips)
        {
            var validClips = (clips ?? Array.Empty<AudioClip>()).Where(c => c != null).ToArray();
            var parts = new string[validClips.Length];
            for (int i = 0; i < validClips.Length; i++)
                parts[i] = $"{SanitizeName(validClips[i].name)} = {i}";
            return string.Join(",\n        ", parts);
        }

        private static string GenerateStringKeys(AudioClip[] clips)
        {
            var validClips = (clips ?? Array.Empty<AudioClip>()).Where(c => c != null).ToArray();
            var parts = new string[validClips.Length];
            for (int i = 0; i < validClips.Length; i++)
                parts[i] = $"\"{SanitizeName(validClips[i].name)}\"";
            return string.Join(",\n        ", parts);
        }

        private static string AddTabToEachLine(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
                lines[i] = "\t" + lines[i];
            return string.Join(Environment.NewLine, lines);
        }
    }
}
```

- [ ] **Step 2: Add Audio helper meta**

Create `Assets/RevCore/Audio/Editor/AudioCollectionMenuTools.cs.meta`:

```yaml
fileFormatVersion: 2
guid: a8fda8347b8b4f5bb51414a95ed9db23
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

- [ ] **Step 3: Update AudioCollectionEditor to use helper**

In `Assets/RevCore/Audio/Editor/AudioCollectionEditor.cs`, replace the `Sort clips` button body with:

```csharp
if (GUILayout.Button("Sort clips"))
{
    AudioCollectionMenuTools.Sort(m_collection);
    Debug.Log("Audio clips sorted alphabetically.");
}
```

Replace the entire `Generate` button body with:

```csharp
if (GUILayout.Button("Generate"))
{
    AudioCollectionMenuTools.Generate(m_collection);
}
```

Replace the `Validate Addressable Sounds` button body with:

```csharp
if (GUILayout.Button("Validate Addressable Sounds"))
{
    AudioCollectionMenuTools.ValidateAddressables(m_collection);
    Debug.Log("Validated Addressable sounds. Direct references were converted.");
}
```

Remove unused `using System.Linq;` and `using System.Text.RegularExpressions;` from `AudioCollectionEditor.cs` if compiler reports them unused.

- [ ] **Step 4: Review Audio menu**

Run:

```powershell
Select-String -Path "Assets/RevCore/Audio/Editor/AudioCollectionMenuTools.cs","Assets/RevCore/Audio/Editor/AudioCollectionEditor.cs" -Pattern "MenuItem|RCore|RevCore\.Data|RevCore\.UI|RevCore\.Prefs"
```

Expected:
- Four `MenuItem` matches in `AudioCollectionMenuTools.cs` when Addressables code included in file text.
- No `RCore` match.
- No cross-package namespace matches except existing `RevCore.Editor` namespace.

## Task 4: Optional Zero-Dependency Hub

**Files:**
- Create: `Assets/RevCore/Hub.meta`
- Create: `Assets/RevCore/Hub/Editor.meta`
- Create: `Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef`
- Create: `Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef.meta`
- Create: `Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs`
- Create: `Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs.meta`

- [ ] **Step 1: Add Hub folder metas**

Create `Assets/RevCore/Hub.meta`:

```yaml
fileFormatVersion: 2
guid: 991783589f1142a7bf4e9796dcb30e8a
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

Create `Assets/RevCore/Hub/Editor.meta`:

```yaml
fileFormatVersion: 2
guid: 2a1a951a8a62422b90f198d2d3f237db
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

- [ ] **Step 2: Add Hub asmdef**

Create `Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef`:

```json
{
  "name": "RevCore.Hub.Editor",
  "rootNamespace": "RevCore.Hub.Editor",
  "references": [],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

Create `Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef.meta`:

```yaml
fileFormatVersion: 2
guid: 48668a4c38e34c728bbf912c714cbd10
AssemblyDefinitionImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

- [ ] **Step 3: Add Hub window implementation**

Create `Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs`:

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace RevCore.Hub.Editor
{
    public class RevCoreHubWindow : EditorWindow
    {
        private static readonly ToolEntry[] Tools =
        {
            new("Data", "Backup All", "RevCore/Data/Backup All"),
            new("Data", "Restore", "RevCore/Data/Restore..."),
            new("Data", "Clear All Data", "RevCore/Data/Clear All Data"),
            new("Audio", "Create Audio Collection", "RevCore/Audio/Create Audio Collection"),
            new("Audio", "Generate Audio IDs", "RevCore/Audio/Generate Audio IDs"),
            new("Audio", "Sort Active Collection", "RevCore/Audio/Sort Active Collection"),
            new("Prefs", "Clear PlayerPrefs", "RevCore/Prefs/Clear PlayerPrefs"),
        };

        private readonly HashSet<string> m_installedPackages = new();
        private ListRequest m_listRequest;
        private Vector2 m_scroll;

        [MenuItem("RevCore/Hub", priority = 0)]
        private static void Open()
        {
            GetWindow<RevCoreHubWindow>("RevCore Hub");
        }

        private void OnEnable()
        {
            RefreshPackages();
        }

        private void Update()
        {
            if (m_listRequest == null || !m_listRequest.IsCompleted)
                return;

            m_installedPackages.Clear();
            if (m_listRequest.Status == StatusCode.Success)
            {
                foreach (var package in m_listRequest.Result)
                    if (package.name.StartsWith("com.rabear.revcore."))
                        m_installedPackages.Add(package.name);
            }

            m_listRequest = null;
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("RevCore Hub", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
                RefreshPackages();
            EditorGUILayout.EndHorizontal();

            if (m_listRequest != null)
                EditorGUILayout.HelpBox("Scanning RevCore packages...", MessageType.Info);

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            DrawPackageStatus();
            EditorGUILayout.Space(8);
            DrawTools();
            EditorGUILayout.EndScrollView();
        }

        private void DrawPackageStatus()
        {
            EditorGUILayout.LabelField("Installed Packages", EditorStyles.boldLabel);
            DrawPackage("Foundation", "com.rabear.revcore.foundation");
            DrawPackage("Inspector", "com.rabear.revcore.inspector");
            DrawPackage("Timer", "com.rabear.revcore.timer");
            DrawPackage("Pool", "com.rabear.revcore.pool");
            DrawPackage("Prefs", "com.rabear.revcore.prefs");
            DrawPackage("Data", "com.rabear.revcore.data");
            DrawPackage("Audio", "com.rabear.revcore.audio");
            DrawPackage("UI", "com.rabear.revcore.ui");
        }

        private void DrawPackage(string label, string packageName)
        {
            bool installed = m_installedPackages.Contains(packageName);
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(label);
            GUILayout.Label(installed ? "Installed" : "Not Installed", GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTools()
        {
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            foreach (var tool in Tools)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField($"{tool.Package}/{tool.Label}");
                if (GUILayout.Button("Open", GUILayout.Width(80)))
                    Execute(tool.MenuPath);
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void Execute(string menuPath)
        {
            if (!EditorApplication.ExecuteMenuItem(menuPath))
                Debug.LogWarning($"RevCore Hub: menu item not available: {menuPath}");
        }

        private void RefreshPackages()
        {
            m_listRequest = Client.List(true, true);
        }

        private readonly struct ToolEntry
        {
            public readonly string Package;
            public readonly string Label;
            public readonly string MenuPath;

            public ToolEntry(string package, string label, string menuPath)
            {
                Package = package;
                Label = label;
                MenuPath = menuPath;
            }
        }
    }
}
```

Create `Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs.meta`:

```yaml
fileFormatVersion: 2
guid: 24b9b948f9c04408b6ea9a72be2b4566
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

- [ ] **Step 4: Review Hub dependencies**

Run:

```powershell
Select-String -Path "Assets/RevCore/Hub/Editor/*" -Pattern "RevCore\.Data|RevCore\.Audio|RevCore\.Prefs|RevCore\.UI|RevCore\.Foundation|RevCore\.Inspector|RevCore\.Pool|RevCore\.Timer|references"
```

Expected:
- `RevCore.Hub.Editor.asmdef` has `"references": []`.
- `RevCoreHubWindow.cs` has only string menu paths, no package namespace imports.

## Task 5: Final Review and Verification

**Files:**
- All files changed in Tasks 1-4.

- [ ] **Step 1: Check git status**

Run:

```powershell
git status --short
```

Expected:
- New/modified files only under `docs/superpowers`, `Assets/RevCore/Data`, `Assets/RevCore/Prefs`, `Assets/RevCore/Audio`, `Assets/RevCore/Hub`.
- Existing unrelated dirty files may still appear; do not stage them.

- [ ] **Step 2: Check no RCore edits**

Run:

```powershell
git diff --name-only -- Assets/RCore
```

Expected: no output.

- [ ] **Step 3: Check no hard cross-package menu dependencies**

Run:

```powershell
Select-String -Path "Assets/RevCore/*/Editor/**/*.cs" -Pattern "using RevCore\.Data|using RevCore\.Audio|using RevCore\.Prefs|using RevCore\.UI|using RevCore\.Pool|using RevCore\.Timer|using RevCore\.Inspector|using RevCore\.Foundation"
```

Expected: no new menu tool cross-package `using` lines. Existing `namespace RevCore.Editor` lines are okay.

- [ ] **Step 4: Check Hub asmdef zero references**

Run:

```powershell
Get-Content "Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef"
```

Expected includes:

```json
"references": []
```

- [ ] **Step 5: Check diff whitespace**

Run:

```powershell
git diff --check -- Assets/RevCore docs/superpowers
```

Expected: no output.

- [ ] **Step 6: Unity verification**

If Unity CLI is available, run editmode tests for changed packages. If unavailable, report not run.

Manual verification in Unity Editor:

1. Confirm `RevCore/Hub` opens Hub window.
2. Confirm `RevCore/Data/Backup All`, `Restore...`, `Clear All Data`, `Log All Data` appear.
3. Confirm `RevCore/Prefs/Clear PlayerPrefs` appears and asks confirmation.
4. Confirm `RevCore/Audio/Create Audio Collection`, `Generate Audio IDs`, `Sort Active Collection` appear.
5. Select an `AudioCollection` asset and run sort/generate.
6. Confirm existing `GameObject/RevCore/UI/Replace Button By ...` menus still appear.

- [ ] **Step 7: Commit scoped files only if user requested commit**

Do not run broad `git add -A`. If committing this scope, stage only:

```powershell
git add "docs/superpowers/specs/2026-05-14-revcore-menu-tools-design.md" "docs/superpowers/plans/2026-05-14-revcore-menu-tools.md" "Assets/RevCore/Data/Editor/JObjectDBMenu.cs" "Assets/RevCore/Data/Editor/JObjectDBMenu.cs.meta" "Assets/RevCore/Prefs/Editor/PlayerPrefsMenu.cs" "Assets/RevCore/Prefs/Editor/PlayerPrefsMenu.cs.meta" "Assets/RevCore/Audio/Editor/AudioCollectionMenuTools.cs" "Assets/RevCore/Audio/Editor/AudioCollectionMenuTools.cs.meta" "Assets/RevCore/Audio/Editor/AudioCollectionEditor.cs" "Assets/RevCore/Hub.meta" "Assets/RevCore/Hub/Editor.meta" "Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef" "Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef.meta" "Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs" "Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs.meta"
```

Commit message:

```text
feat(revcore): add zero-dependency editor menu tools

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
```

## Self-Review

- Spec coverage: Package-owned Data, Audio, Prefs tools covered. Existing UI context menu preserved. Optional Hub covered with zero asmdef references.
- Placeholder scan: No TBD/TODO/fill-in steps. Exact file paths, code, commands, and expected results included.
- Type consistency: `AudioCollectionMenuTools` methods used by `AudioCollectionEditor` are public static and names match plan. Hub namespace matches asmdef root namespace. Menu paths match design spec.
