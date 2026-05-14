# RevCore.Tools Package Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build standalone editor-only `RevCore.Tools` package by porting approved RCore editor tools into `Assets/RevCore/Tools` with no dependency on other RevCore or RCore packages.

**Architecture:** `RevCore.Tools` owns its Hub, tool base class, package-local IMGUI helpers, and all approved tools. Full tools open in their own `EditorWindow`; quick actions render inline in Hub. Addressables colorizer compiles only behind `#if ADDRESSABLES`.

**Tech Stack:** Unity 2022.3, UnityEditor IMGUI, UPM package layout, C# editor asmdef, `EditorPrefs`, `JsonUtility`, optional Addressables guarded by `ADDRESSABLES`.

---

## Scope and Constraints

- Work only under `Assets/RevCore/Tools/` plus delete old `Assets/RevCore/Hub/` files.
- Do not modify `Assets/RCore/`.
- Do not stage unrelated dirty files.
- Review after every task by reading changed files and checking `git diff -- Assets/RevCore`.
- No other RevCore package can reference `RevCore.Tools`.
- `RevCore.Tools.Editor.asmdef` references no other RevCore asmdefs.
- Do not use `EditorHelper`, `REditorPrefString`, `REditorPrefBool`, `ColorHelper`, `MathHelper`, `RUtil`, `JsonHelper`, `GuiButton`, `GuiToggle`, `IDraw`, or RCore namespaces.
- Remove old `Assets/RevCore/Hub/` because Tools package replaces it.

## File Structure

### Create package files
- `Assets/RevCore/Tools/package.json` — UPM manifest for `com.rabear.revcore.tools`.
- `Assets/RevCore/Tools/package.json.meta`
- `Assets/RevCore/Tools/Editor/RevCore.Tools.Editor.asmdef` — editor-only asmdef.
- `Assets/RevCore/Tools/Editor/RevCore.Tools.Editor.asmdef.meta`

### Create Hub files
- `Assets/RevCore/Tools/Editor/Hub/RevCoreTool.cs` — abstract tool contract.
- `Assets/RevCore/Tools/Editor/Hub/RevCoreToolsWindow.cs` — reflection-driven launcher window.

### Create helper files
- `Assets/RevCore/Tools/Editor/Helpers/EditorPrefsValue.cs` — typed EditorPrefs wrapper.
- `Assets/RevCore/Tools/Editor/Helpers/EditorGuiHelper.cs` — thin IMGUI helpers.
- `Assets/RevCore/Tools/Editor/Helpers/AssetPathHelper.cs` — path conversion helpers.

### Create Navigate tools
- `Assets/RevCore/Tools/Editor/Navigate/ScenesNavigatorWindow.cs`
- `Assets/RevCore/Tools/Editor/Navigate/ScenesNavigatorTool.cs`
- `Assets/RevCore/Tools/Editor/Navigate/AssetShortcutsWindow.cs`
- `Assets/RevCore/Tools/Editor/Navigate/AssetShortcutsTool.cs`
- `Assets/RevCore/Tools/Editor/Navigate/ProjectExplorerTool.cs`

### Create Search tools
- `Assets/RevCore/Tools/Editor/Search/FindByGuidTool.cs`
- `Assets/RevCore/Tools/Editor/Search/FindComponentReferenceWindow.cs`
- `Assets/RevCore/Tools/Editor/Search/FindComponentReferenceTool.cs`
- `Assets/RevCore/Tools/Editor/Search/ObjectsFinderWindow.cs`
- `Assets/RevCore/Tools/Editor/Search/ObjectsFinderTool.cs`
- `Assets/RevCore/Tools/Editor/Search/ScriptFinder.cs`
- `Assets/RevCore/Tools/Editor/Search/ParticleSystemFinder.cs`
- `Assets/RevCore/Tools/Editor/Search/PersistentEventFinder.cs`

### Create Generator tools
- `Assets/RevCore/Tools/Editor/Generators/ScreenshotTakerWindow.cs`
- `Assets/RevCore/Tools/Editor/Generators/ScreenshotTakerTool.cs`
- `Assets/RevCore/Tools/Editor/Generators/CharactersSetGeneratorTool.cs`

### Create Utility tools
- `Assets/RevCore/Tools/Editor/Utility/AutoPlayFirstSceneTool.cs`
- `Assets/RevCore/Tools/Editor/Utility/ToggleRaycastAllTool.cs`
- `Assets/RevCore/Tools/Editor/Utility/AssetCleaner/AssetCleaner.cs`
- `Assets/RevCore/Tools/Editor/Utility/AssetCleaner/AssetCleanerWindow.cs`
- `Assets/RevCore/Tools/Editor/Utility/AssetCleaner/AssetCleanerSettings.cs`
- `Assets/RevCore/Tools/Editor/Utility/AssetCleaner/AssetCleanerTool.cs`

### Create Addressables tools
- `Assets/RevCore/Tools/Editor/Addressables/AddressableGroupsColorizer.cs`
- `Assets/RevCore/Tools/Editor/Addressables/AddressableGroupsColorizerSettings.cs`
- `Assets/RevCore/Tools/Editor/Addressables/AddressableGroupsColorizerTool.cs`

### Delete old Hub
- Delete `Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs`
- Delete `Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs.meta`
- Delete `Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef`
- Delete `Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef.meta`
- Delete `Assets/RevCore/Hub/Editor.meta`
- Delete `Assets/RevCore/Hub.meta`

---

## Task 1: Baseline and Package Skeleton

**Files:**
- Create: `Assets/RevCore/Tools/package.json`
- Create: `Assets/RevCore/Tools/Editor/RevCore.Tools.Editor.asmdef`
- Create: `.meta` files for new folders/files

- [ ] **Step 1: Check baseline git state**

Run:

```powershell
git status --short
```

Expected:
- Existing unrelated dirty files may appear.
- No edits in `Assets/RCore/` from this task.

- [ ] **Step 2: Create Tools package manifest**

Create `Assets/RevCore/Tools/package.json`:

```json
{
  "name": "com.rabear.revcore.tools",
  "version": "1.0.0",
  "displayName": "RevCore Tools",
  "description": "Standalone Unity Editor tools for RevCore projects.",
  "unity": "2022.3",
  "author": {
    "name": "hnb-rabear"
  },
  "keywords": [
    "revcore",
    "unity",
    "editor",
    "tools"
  ]
}
```

- [ ] **Step 3: Create Editor asmdef**

Create `Assets/RevCore/Tools/Editor/RevCore.Tools.Editor.asmdef`:

```json
{
  "name": "RevCore.Tools.Editor",
  "rootNamespace": "RevCore.Tools.Editor",
  "references": [],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 4: Add Unity meta files**

Create `.meta` files for:
- `Assets/RevCore/Tools.meta`
- `Assets/RevCore/Tools/Editor.meta`
- `Assets/RevCore/Tools/package.json.meta`
- `Assets/RevCore/Tools/Editor/RevCore.Tools.Editor.asmdef.meta`

Use Unity YAML style:

```yaml
fileFormatVersion: 2
guid: UNIQUE_32_HEX_GUID
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
```

For asmdef meta use:

```yaml
fileFormatVersion: 2
guid: UNIQUE_32_HEX_GUID
AssemblyDefinitionImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
```

- [ ] **Step 5: Review Task 1**

Run:

```powershell
git diff -- Assets/RevCore/Tools/package.json Assets/RevCore/Tools/Editor/RevCore.Tools.Editor.asmdef
```

Expected:
- Manifest exists.
- Asmdef has `references: []`.
- `includePlatforms` contains `Editor`.

Run:

```powershell
git status --short -- Assets/RevCore/Tools Assets/RevCore/Hub Assets/RCore
```

Expected:
- New Tools files only.
- No `Assets/RCore` modifications.

---

## Task 2: Hub Foundation

**Files:**
- Create: `Assets/RevCore/Tools/Editor/Hub/RevCoreTool.cs`
- Create: `Assets/RevCore/Tools/Editor/Hub/RevCoreToolsWindow.cs`

- [ ] **Step 1: Create `RevCoreTool.cs`**

```csharp
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public abstract class RevCoreTool
    {
        public abstract string Name { get; }
        public abstract string Category { get; }
        public virtual bool IsQuickAction => false;

        public virtual void OnGUI()
        {
        }

        public virtual void OnOpen()
        {
        }
    }
}
```

- [ ] **Step 2: Create `RevCoreToolsWindow.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class RevCoreToolsWindow : EditorWindow
    {
        private readonly List<RevCoreTool> m_tools = new();
        private Vector2 m_scroll;
        private string m_search = string.Empty;
        private string m_category = "All";

        [MenuItem("RevCore/Tools Hub", priority = 0)]
        public static void Open()
        {
            GetWindow<RevCoreToolsWindow>("RevCore Tools");
        }

        private void OnEnable()
        {
            RefreshTools();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawCategories();

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            foreach (RevCoreTool tool in FilteredTools())
                DrawTool(tool);
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("RevCore Tools", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            m_search = GUILayout.TextField(m_search, GUI.skin.FindStyle("ToolbarSearchTextField"), GUILayout.Width(220));
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshTools();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategories()
        {
            string[] categories = new[] { "All" }.Concat(m_tools.Select(x => x.Category).Distinct().OrderBy(x => x)).ToArray();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            foreach (string category in categories)
            {
                bool selected = category == m_category;
                if (GUILayout.Toggle(selected, category, EditorStyles.toolbarButton))
                    m_category = category;
            }
            EditorGUILayout.EndHorizontal();
        }

        private IEnumerable<RevCoreTool> FilteredTools()
        {
            foreach (RevCoreTool tool in m_tools.OrderBy(x => x.Category).ThenBy(x => x.Name))
            {
                if (m_category != "All" && tool.Category != m_category)
                    continue;
                if (!string.IsNullOrWhiteSpace(m_search) && !tool.Name.ToLowerInvariant().Contains(m_search.ToLowerInvariant()))
                    continue;
                yield return tool;
            }
        }

        private static void DrawTool(RevCoreTool tool)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{tool.Category} / {tool.Name}", EditorStyles.boldLabel);
            if (!tool.IsQuickAction && GUILayout.Button("Open", GUILayout.Width(80)))
                tool.OnOpen();
            EditorGUILayout.EndHorizontal();

            if (tool.IsQuickAction)
                tool.OnGUI();

            EditorGUILayout.EndVertical();
        }

        private void RefreshTools()
        {
            m_tools.Clear();
            Type baseType = typeof(RevCoreTool);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(x => x != null).ToArray();
                }

                foreach (Type type in types)
                {
                    if (type == null || type.IsAbstract || !baseType.IsAssignableFrom(type))
                        continue;
                    if (Activator.CreateInstance(type) is RevCoreTool tool)
                        m_tools.Add(tool);
                }
            }
        }
    }
}
```

- [ ] **Step 3: Review Task 2**

Read both files and verify:
- Namespace is `RevCore.Tools.Editor`.
- No `using RCore`.
- Hub menu path is `RevCore/Tools Hub`.
- Reflection catches `ReflectionTypeLoadException`.

Run:

```powershell
git diff -- Assets/RevCore/Tools/Editor/Hub
```

Expected:
- Only Hub files changed.

---

## Task 3: Shared Helpers

**Files:**
- Create: `Assets/RevCore/Tools/Editor/Helpers/EditorPrefsValue.cs`
- Create: `Assets/RevCore/Tools/Editor/Helpers/EditorGuiHelper.cs`
- Create: `Assets/RevCore/Tools/Editor/Helpers/AssetPathHelper.cs`

- [ ] **Step 1: Create `EditorPrefsValue.cs`**

```csharp
using UnityEditor;

namespace RevCore.Tools.Editor
{
    public sealed class EditorPrefsValue<T>
    {
        private readonly string m_key;
        private readonly T m_defaultValue;

        public EditorPrefsValue(string key, T defaultValue = default)
        {
            m_key = key;
            m_defaultValue = defaultValue;
        }

        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        private T GetValue()
        {
            object value;
            if (typeof(T) == typeof(string))
                value = EditorPrefs.GetString(m_key, (string)(object)m_defaultValue);
            else if (typeof(T) == typeof(bool))
                value = EditorPrefs.GetBool(m_key, (bool)(object)m_defaultValue);
            else if (typeof(T) == typeof(int))
                value = EditorPrefs.GetInt(m_key, (int)(object)m_defaultValue);
            else if (typeof(T) == typeof(float))
                value = EditorPrefs.GetFloat(m_key, (float)(object)m_defaultValue);
            else
                throw new System.NotSupportedException(typeof(T).Name);
            return (T)value;
        }

        private void SetValue(T value)
        {
            if (typeof(T) == typeof(string))
                EditorPrefs.SetString(m_key, (string)(object)value);
            else if (typeof(T) == typeof(bool))
                EditorPrefs.SetBool(m_key, (bool)(object)value);
            else if (typeof(T) == typeof(int))
                EditorPrefs.SetInt(m_key, (int)(object)value);
            else if (typeof(T) == typeof(float))
                EditorPrefs.SetFloat(m_key, (float)(object)value);
            else
                throw new System.NotSupportedException(typeof(T).Name);
        }
    }
}
```

- [ ] **Step 2: Create `AssetPathHelper.cs`**

```csharp
using System.IO;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    internal static class AssetPathHelper
    {
        public static string ToUnityPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            string normalized = absolutePath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (!normalized.StartsWith(dataPath))
                return string.Empty;

            return "Assets" + normalized.Substring(dataPath.Length);
        }

        public static string EnsureDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return path;
        }
    }
}
```

- [ ] **Step 3: Create `EditorGuiHelper.cs`**

```csharp
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore.Tools.Editor
{
    internal static class EditorGuiHelper
    {
        public static bool ButtonColor(string label, Color color, float width = 0)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            bool clicked = width > 0 ? GUILayout.Button(label, GUILayout.Width(width)) : GUILayout.Button(label);
            GUI.color = oldColor;
            return clicked;
        }

        public static bool HeaderFoldout(string label, string key = null)
        {
            string prefKey = string.IsNullOrEmpty(key) ? "RevCore.Tools.Foldout." + label : "RevCore.Tools.Foldout." + key;
            bool value = EditorPrefs.GetBool(prefKey, true);
            value = EditorGUILayout.Foldout(value, label, true, EditorStyles.foldoutHeader);
            EditorPrefs.SetBool(prefKey, value);
            return value;
        }

        public static void Separator()
        {
            EditorGUILayout.Space(4);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f));
            EditorGUILayout.Space(4);
        }

        public static void DragDropBox<T>(string label, Action<T[]> onDrop) where T : Object
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, label);

            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                var results = new System.Collections.Generic.List<T>();
                foreach (Object obj in DragAndDrop.objectReferences)
                    if (obj is T typed)
                        results.Add(typed);
                onDrop(results.ToArray());
                evt.Use();
            }
        }
    }
}
```

- [ ] **Step 4: Review Task 3**

Run:

```powershell
git diff -- Assets/RevCore/Tools/Editor/Helpers
```

Expected:
- Helpers contain no RCore references.
- Helpers only wrap UnityEditor/UnityEngine APIs.

---

## Task 4: Navigate Tools

**Files:**
- Create: `ScenesNavigatorWindow.cs`, `ScenesNavigatorTool.cs`
- Create: `AssetShortcutsWindow.cs`, `AssetShortcutsTool.cs`
- Create: `ProjectExplorerTool.cs`

- [ ] **Step 1: Create `ScenesNavigatorWindow.cs`**

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class ScenesNavigatorWindow : EditorWindow
    {
        private readonly EditorPrefsValue<string> m_search = new("RevCore.Tools.ScenesNavigator.Search", string.Empty);
        private List<string> m_scenes = new();
        private Vector2 m_scroll;

        [MenuItem("RevCore/Tools/Scenes Navigator", priority = 100)]
        public static void Open()
        {
            GetWindow<ScenesNavigatorWindow>("Scenes Navigator");
        }

        private void OnEnable()
        {
            m_scenes = GetAllScenesInProject();
        }

        private void OnGUI()
        {
            var buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Scenes in Build", EditorStyles.boldLabel);
            foreach (var scene in EditorBuildSettings.scenes)
            {
                string sceneName = Path.GetFileNameWithoutExtension(scene.path);
                if (GUILayout.Button(sceneName, buttonStyle))
                    OpenScene(scene.path);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("All Scenes in Project", EditorStyles.boldLabel);
            m_search.Value = EditorGUILayout.TextField("Search", m_search.Value);
            for (int i = 0; i < m_scenes.Count; i++)
            {
                string scene = m_scenes[i];
                if (!string.IsNullOrEmpty(m_search.Value) && !scene.ToLowerInvariant().Contains(m_search.Value.ToLowerInvariant()))
                    continue;
                if (GUILayout.Button($"{i}\t {scene}", buttonStyle))
                    OpenScene(scene);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private static void OpenScene(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(scenePath);
        }

        private static List<string> GetAllScenesInProject()
        {
            var scenes = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
                scenes.Add(AssetDatabase.GUIDToAssetPath(guid));
            return scenes;
        }
    }
}
```

- [ ] **Step 2: Create `ScenesNavigatorTool.cs`**

```csharp
namespace RevCore.Tools.Editor
{
    internal sealed class ScenesNavigatorTool : RevCoreTool
    {
        public override string Name => "Scenes Navigator";
        public override string Category => "Navigate";

        public override void OnOpen()
        {
            ScenesNavigatorWindow.Open();
        }
    }
}
```

- [ ] **Step 3: Create `ProjectExplorerTool.cs`**

```csharp
using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    internal sealed class ProjectExplorerTool : RevCoreTool
    {
        public override string Name => "Project Explorer";
        public override string Category => "Navigate";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            if (GUILayout.Button("Open Assets Folder"))
                EditorUtility.RevealInFinder(Application.dataPath);
            if (GUILayout.Button("Open StreamingAssets Folder"))
                EditorUtility.RevealInFinder(Application.streamingAssetsPath);
            if (GUILayout.Button("Open PersistentData Folder"))
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            if (GUILayout.Button("Open Unity Editor Folder"))
                EditorUtility.RevealInFinder(System.IO.Path.GetDirectoryName(EditorApplication.applicationPath));
        }
    }
}
```

- [ ] **Step 4: Create `AssetShortcutsWindow.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore.Tools.Editor
{
    public class AssetShortcutsWindow : EditorWindow
    {
        [Serializable]
        private class Category
        {
            public string name;
            public List<string> guids = new();
        }

        [Serializable]
        private class ShortcutData
        {
            public List<Category> categories = new();
        }

        private const string FilePath = "ProjectSettings/RevCore/AssetShortcuts.json";
        private ShortcutData m_data = new();
        private Vector2 m_scroll;
        private string m_newCategoryName = string.Empty;
        private int m_editingCategory = -1;

        [MenuItem("RevCore/Tools/Asset Shortcuts", priority = 101)]
        public static void Open()
        {
            GetWindow<AssetShortcutsWindow>("Asset Shortcuts");
        }

        private void OnEnable()
        {
            LoadData();
        }

        private void OnDisable()
        {
            SaveData();
        }

        private void LoadData()
        {
            if (File.Exists(FilePath))
                m_data = JsonUtility.FromJson<ShortcutData>(File.ReadAllText(FilePath));
            m_data ??= new ShortcutData();
            m_data.categories ??= new List<Category>();
        }

        private void SaveData()
        {
            AssetPathHelper.EnsureDirectory(FilePath);
            File.WriteAllText(FilePath, JsonUtility.ToJson(m_data, true));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            m_newCategoryName = EditorGUILayout.TextField(m_newCategoryName);
            if (GUILayout.Button("Add Category", GUILayout.Width(110)) && !string.IsNullOrEmpty(m_newCategoryName))
            {
                m_data.categories.Add(new Category { name = m_newCategoryName });
                m_newCategoryName = string.Empty;
                SaveData();
            }
            EditorGUILayout.EndHorizontal();

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            for (int i = 0; i < m_data.categories.Count; i++)
                DrawCategory(i);
            EditorGUILayout.EndScrollView();
        }

        private void DrawCategory(int index)
        {
            Category category = m_data.categories[index];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            if (m_editingCategory == index)
                category.name = EditorGUILayout.TextField(category.name);
            else
                EditorGUILayout.LabelField(category.name, EditorStyles.boldLabel);

            if (GUILayout.Button(m_editingCategory == index ? "Done" : "Edit", GUILayout.Width(60)))
            {
                m_editingCategory = m_editingCategory == index ? -1 : index;
                SaveData();
            }
            if (EditorGuiHelper.ButtonColor("Delete", Color.red, 70))
            {
                m_data.categories.RemoveAt(index);
                SaveData();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            DrawDropArea(category);
            DrawAssets(category);
            EditorGUILayout.EndVertical();
        }

        private void DrawDropArea(Category category)
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Assets Here");
            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
                    if (!string.IsNullOrEmpty(guid) && !category.guids.Contains(guid))
                        category.guids.Add(guid);
                }
                SaveData();
                evt.Use();
            }
        }

        private void DrawAssets(Category category)
        {
            for (int i = 0; i < category.guids.Count; i++)
            {
                string guid = category.guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset == null)
                    continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(asset, typeof(Object), false);
                if (asset is SceneAsset)
                {
                    if (GUILayout.Button("Open", GUILayout.Width(50)) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(path);
                }
                else if (PrefabUtility.GetPrefabAssetType(asset) == PrefabAssetType.Regular)
                {
                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                        AssetDatabase.OpenAsset(asset);
                }

                if (EditorGuiHelper.ButtonColor("Delete", Color.red, 70))
                {
                    category.guids.RemoveAt(i);
                    SaveData();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
```

- [ ] **Step 5: Create `AssetShortcutsTool.cs`**

```csharp
namespace RevCore.Tools.Editor
{
    internal sealed class AssetShortcutsTool : RevCoreTool
    {
        public override string Name => "Asset Shortcuts";
        public override string Category => "Navigate";

        public override void OnOpen()
        {
            AssetShortcutsWindow.Open();
        }
    }
}
```

- [ ] **Step 6: Review Task 4**

Run:

```powershell
git diff -- Assets/RevCore/Tools/Editor/Navigate Assets/RevCore/Tools/Editor/Helpers
```

Expected:
- AssetShortcuts storage path is `ProjectSettings/RevCore/AssetShortcuts.json`.
- No Newtonsoft usage.
- No RCore namespace usage.

Run:

```powershell
Select-String -Path "Assets/RevCore/Tools/Editor/Navigate/*.cs" -Pattern "Newtonsoft|RCore\.|EditorHelper|REditorPref"
```

Expected:
- No matches.

---

## Task 5: Search Tools

**Files:**
- Create Search files listed in file structure.

- [ ] **Step 1: Create `FindByGuidTool.cs`**

```csharp
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore.Tools.Editor
{
    internal sealed class FindByGuidTool : RevCoreTool
    {
        private string m_guid = string.Empty;
        private Object m_asset;

        public override string Name => "Find By GUID";
        public override string Category => "Search";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            m_guid = EditorGUILayout.TextField("GUID", m_guid);
            EditorGUILayout.ObjectField("Asset", m_asset, typeof(Object), false);
            if (GUILayout.Button("Find"))
            {
                string path = AssetDatabase.GUIDToAssetPath(m_guid);
                m_asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Object>(path);
                if (m_asset != null)
                {
                    Selection.activeObject = m_asset;
                    EditorGUIUtility.PingObject(m_asset);
                }
            }
        }
    }
}
```

- [ ] **Step 2: Create `FindComponentReferenceWindow.cs` and tool wrapper**

Use the RCore `FindComponentReferenceWindow` logic with namespace changed to `RevCore.Tools.Editor`, add try/catch around `asm.GetTypes()`, keep `ProjectBrowser` reflection null-safe.

Create wrapper:

```csharp
namespace RevCore.Tools.Editor
{
    internal sealed class FindComponentReferenceTool : RevCoreTool
    {
        public override string Name => "Find Component Reference";
        public override string Category => "Search";

        public override void OnOpen()
        {
            FindComponentReferenceWindow.Open();
        }
    }
}
```

- [ ] **Step 3: Create `ParticleSystemFinder.cs`**

Use RCore logic with `EditorHelper.FormatPathToUnityPath` replaced by `AssetPathHelper.ToUnityPath`, and replace `EditorHelper.HeaderFoldout`/`Button` with `EditorGuiHelper.HeaderFoldout`/`GUILayout.Button`.

- [ ] **Step 4: Create `PersistentEventFinder.cs`**

Use RCore logic with:
- `EditorHelper.HeaderFoldout` → `EditorGuiHelper.HeaderFoldout`
- `EditorHelper.Foldout` → `EditorGUILayout.Foldout` with local dictionary state if needed
- `JsonHelper.ToJson(m_AllPersistentMethods)` → `string.Join("\n", m_AllPersistentMethods)`
- `OpenFilePanel` bug fixed to `OpenFolderPanel`

- [ ] **Step 5: Create `ScriptFinder.cs`**

Use RCore logic with:
- `EditorHelper.FormatPathToUnityPath` → `AssetPathHelper.ToUnityPath`
- `EditorHelper.HeaderFoldout` → `EditorGuiHelper.HeaderFoldout`
- Remove `Delete Unused Scripts` button and remove `DeleteUnusedScripts` method.
- Preserve scene-open behavior but prompt `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()` before opening scenes.

- [ ] **Step 6: Create `ObjectsFinderWindow.cs` and wrapper**

Window contains three boxed sections:
- `ScriptFinder.DrawOnGUI()`
- `ParticleSystemFinder.DrawOnGUI()`
- `PersistentEventFinder.DrawOnGUI()`

Wrapper:

```csharp
namespace RevCore.Tools.Editor
{
    internal sealed class ObjectsFinderTool : RevCoreTool
    {
        public override string Name => "Objects Finder";
        public override string Category => "Search";

        public override void OnOpen()
        {
            ObjectsFinderWindow.Open();
        }
    }
}
```

- [ ] **Step 7: Review Task 5**

Run:

```powershell
Select-String -Path "Assets/RevCore/Tools/Editor/Search/*.cs" -Pattern "DeleteUnusedScripts|AssetDatabase.DeleteAsset|JsonHelper|EditorHelper|RCore\."
```

Expected:
- No matches.

Run:

```powershell
git diff -- Assets/RevCore/Tools/Editor/Search
```

Expected:
- Search tools are editor-only and no RCore references.

---

## Task 6: Generator Tools

**Files:**
- Create `ScreenshotTakerWindow.cs`, `ScreenshotTakerTool.cs`, `CharactersSetGeneratorTool.cs`

- [ ] **Step 1: Create `ScreenshotTakerWindow.cs`**

Port `ScreenshotTakerDrawer` into a standalone EditorWindow:
- Namespace `RevCore.Tools.Editor`.
- Menu item `RevCore/Tools/Screenshot Taker`.
- Class name `ScreenshotTakerWindow`.
- On capture, create `RenderTexture`, set camera target, read pixels, write PNG, clear target texture and active RT.
- If no camera exists, show `EditorGUILayout.HelpBox("No camera selected or found.", MessageType.Warning)`.

- [ ] **Step 2: Create `ScreenshotTakerTool.cs`**

```csharp
namespace RevCore.Tools.Editor
{
    internal sealed class ScreenshotTakerTool : RevCoreTool
    {
        public override string Name => "Screenshot Taker";
        public override string Category => "Generators";

        public override void OnOpen()
        {
            ScreenshotTakerWindow.Open();
        }
    }
}
```

- [ ] **Step 3: Create `CharactersSetGeneratorTool.cs`**

Create quick action that accepts dragged `TextAsset`s, merges characters, sorts unique chars, displays result in text area, and saves text file with SaveFilePanel.

Implementation shape:

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    internal sealed class CharactersSetGeneratorTool : RevCoreTool
    {
        private readonly List<TextAsset> m_assets = new();
        private string m_result = string.Empty;
        private Vector2 m_scroll;

        public override string Name => "Characters Set Generator";
        public override string Category => "Generators";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            EditorGuiHelper.DragDropBox<TextAsset>("Drag TextAssets Here", assets =>
            {
                foreach (TextAsset asset in assets)
                    if (asset != null && !m_assets.Contains(asset))
                        m_assets.Add(asset);
            });

            for (int i = 0; i < m_assets.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                m_assets[i] = (TextAsset)EditorGUILayout.ObjectField(m_assets[i], typeof(TextAsset), false);
                if (EditorGuiHelper.ButtonColor("x", Color.red, 24))
                {
                    m_assets.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Generate"))
            {
                var chars = new HashSet<char>();
                foreach (TextAsset asset in m_assets)
                    if (asset != null)
                        foreach (char c in asset.text)
                            chars.Add(c);
                m_result = new string(chars.OrderBy(c => c).ToArray());
            }

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll, GUILayout.Height(120));
            m_result = EditorGUILayout.TextArea(m_result, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Save") && !string.IsNullOrEmpty(m_result))
            {
                string path = EditorUtility.SaveFilePanel("Save Characters Set", Application.dataPath, "characters", "txt");
                if (!string.IsNullOrEmpty(path))
                    System.IO.File.WriteAllText(path, m_result);
            }
        }
    }
}
```

- [ ] **Step 4: Review Task 6**

Run:

```powershell
Select-String -Path "Assets/RevCore/Tools/Editor/Generators/*.cs" -Pattern "EditorHelper|RCore\."
```

Expected:
- No matches.

---

## Task 7: Utility Tools

**Files:**
- Create `AutoPlayFirstSceneTool.cs`
- Create `ToggleRaycastAllTool.cs`

- [ ] **Step 1: Create `AutoPlayFirstSceneTool.cs`**

Port RCore AutoPlay logic with:
- `EditorPrefsValue<bool>` key `RevCore.Tools.AutoPlayFirstScene.Active`.
- Menu path `RevCore/Tools/Toggle Auto Play First Scene`.
- `Menu.SetChecked` validation.
- Hub quick action that toggles same value.

- [ ] **Step 2: Create `ToggleRaycastAllTool.cs`**

Create quick action:
- Uses `Selection.gameObjects`.
- Finds all `UnityEngine.UI.Graphic` components in children.
- Buttons: `Enable Raycast Target`, `Disable Raycast Target`.
- Calls `Undo.RecordObject(graphic, "Toggle Raycast Target")`, sets `graphic.raycastTarget`, calls `EditorUtility.SetDirty(graphic)`.

- [ ] **Step 3: Review Task 7**

Run:

```powershell
Select-String -Path "Assets/RevCore/Tools/Editor/Utility/*.cs" -Pattern "EditorHelper|RCore\.|ColorHelper|HierarchyDeep"
```

Expected:
- No matches.

---

## Task 8: Asset Cleaner

**Files:**
- Create `AssetCleaner.cs`
- Create `AssetCleanerWindow.cs`
- Create `AssetCleanerSettings.cs`
- Create `AssetCleanerTool.cs`

- [ ] **Step 1: Create `AssetCleaner.cs`**

Port core logic from `RAssetCleaner.cs`:
- Namespace `RevCore.Tools.Editor`.
- Static caches: `UnusedAssetsCache`, `FolderStatsCache`, `ReferenceCache`, `SizeCache`.
- Methods: `FindUnusedAssets(List<string> ignorePaths)`, `BuildCache()`, `CalculateFolderStats(List<string>)`, `IsIgnored(string, List<string>)`, `IsRootAsset(string)`, `GetFileSize(string)`.
- No `RCore.Editor.AssetCleaner` namespace.

- [ ] **Step 2: Create `AssetCleanerSettings.cs`**

ScriptableObject with:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class AssetCleanerSettings : ScriptableObject
    {
        public List<string> ignorePaths = new() { "Assets/RevCore" };
    }
}
```

- [ ] **Step 3: Create `AssetCleanerWindow.cs`**

Create EditorWindow with tabs:
- Cleaner: scan unused assets, list paths, select asset, delete selected/all.
- Reference Finder: object field target asset, list referrers from `AssetCleaner.ReferenceCache`.
- Settings: edit ignore paths list.

Delete safety:
- Selected delete confirmation text includes exact path.
- Delete all confirmation text includes count and first 20 paths.
- Use `AssetDatabase.DeleteAsset(path)` only after confirmation.

- [ ] **Step 4: Create `AssetCleanerTool.cs`**

```csharp
namespace RevCore.Tools.Editor
{
    internal sealed class AssetCleanerTool : RevCoreTool
    {
        public override string Name => "Asset Cleaner";
        public override string Category => "Utility";

        public override void OnOpen()
        {
            AssetCleanerWindow.Open();
        }
    }
}
```

- [ ] **Step 5: Review Task 8**

Run:

```powershell
Select-String -Path "Assets/RevCore/Tools/Editor/Utility/AssetCleaner/*.cs" -Pattern "RAssetCleaner|RCore\.|EditorHelper"
```

Expected:
- No matches.

Read delete code and verify confirmation includes paths before every `AssetDatabase.DeleteAsset` call.

---

## Task 9: Addressables Colorizer

**Files:**
- Create `AddressableGroupsColorizer.cs`
- Create `AddressableGroupsColorizerSettings.cs`
- Create `AddressableGroupsColorizerTool.cs`

- [ ] **Step 1: Create settings file**

`AddressableGroupsColorizerSettings.cs` content:

```csharp
#if ADDRESSABLES
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class AddressableGroupsColorizerSettings : ScriptableObject
    {
        public bool enabled = true;
        public List<Rule> rules = new()
        {
            new Rule { prefix = "In", color = Color.green },
            new Rule { prefix = "Fa", color = Color.blue },
            new Rule { prefix = "On", color = Color.cyan },
            new Rule { prefix = "Ex", color = Color.red }
        };

        [Serializable]
        public class Rule
        {
            public string prefix;
            public Color color;
        }
    }
}
#endif
```

- [ ] **Step 2: Create colorizer file**

Port RCore colorizer with:
- Namespace `RevCore.Tools.Editor`.
- Entire file wrapped in `#if ADDRESSABLES`.
- Menu path `RevCore/Tools/Addressable Groups Colorizer Settings`.
- No `RMenu` reference.
- Keep `AddressableAssetSettingsDefaultObject.Settings` usage.

- [ ] **Step 3: Create tool wrapper**

```csharp
#if ADDRESSABLES
using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    internal sealed class AddressableGroupsColorizerTool : RevCoreTool
    {
        public override string Name => "Addressable Groups Colorizer";
        public override string Category => "Addressables";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            EditorGUILayout.HelpBox("Colorizes Addressable group assets in Project window when ADDRESSABLES is defined.", MessageType.Info);
            if (GUILayout.Button("Open Settings"))
                EditorApplication.ExecuteMenuItem("RevCore/Tools/Addressable Groups Colorizer Settings");
        }
    }
}
#endif
```

- [ ] **Step 4: Review Task 9**

Run:

```powershell
Select-String -Path "Assets/RevCore/Tools/Editor/Addressables/*.cs" -Pattern "RMenu|RCore\."
```

Expected:
- No matches.

Verify every Addressables file begins with `#if ADDRESSABLES`.

---

## Task 10: Remove Old Hub

**Files:**
- Delete: `Assets/RevCore/Hub/`

- [ ] **Step 1: Delete old Hub files**

Delete:
- `Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs`
- `Assets/RevCore/Hub/Editor/RevCoreHubWindow.cs.meta`
- `Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef`
- `Assets/RevCore/Hub/Editor/RevCore.Hub.Editor.asmdef.meta`
- `Assets/RevCore/Hub/Editor.meta`
- `Assets/RevCore/Hub.meta`

- [ ] **Step 2: Review Task 10**

Run:

```powershell
git status --short -- Assets/RevCore/Hub Assets/RevCore/Tools
```

Expected:
- `D Assets/RevCore/Hub/...`
- `?? Assets/RevCore/Tools/...` or modified Tools files.

Run:

```powershell
Select-String -Path "Assets/RevCore/**/*.cs","Assets/RevCore/**/*.asmdef" -Pattern "RevCore/Hub|RevCoreHubWindow|RevCore.Hub"
```

Expected:
- No matches.

---

## Task 11: Final Static Review

**Files:**
- All `Assets/RevCore/Tools/**`
- Deleted `Assets/RevCore/Hub/**`

- [ ] **Step 1: Check package independence**

Run:

```powershell
Get-ChildItem "Assets/RevCore/Tools" -Recurse -Filter *.cs | Select-String -Pattern "RCore\.|using RCore|EditorHelper|REditorPref|ColorHelper|MathHelper|RUtil|JsonHelper|GuiButton|GuiToggle|IDraw"
```

Expected:
- No matches.

- [ ] **Step 2: Check asmdef references**

Read `Assets/RevCore/Tools/Editor/RevCore.Tools.Editor.asmdef`.

Expected:
- `"references": []`
- `"includePlatforms": ["Editor"]`

- [ ] **Step 3: Check skipped tools are absent**

Run:

```powershell
Get-ChildItem "Assets/RevCore/Tools" -Recurse -Filter *.cs | Select-String -Pattern "EditorIconsWindow|EditorIcon|MeshInfo|FormatTexts|ChangeTMP|SpriteRenderer|CombineMeshes|AlignCenter|Asana|AnimationsPack|PreBuildProcessor|SpriteReplacer|ReskinToolkit"
```

Expected:
- No matches except harmless text in comments if any. Remove comments if found.

- [ ] **Step 4: Check diff scope**

Run:

```powershell
git diff --name-only -- Assets/RevCore Assets/RCore docs/superpowers/specs docs/superpowers/plans
```

Expected:
- Files under `Assets/RevCore/Tools/`.
- Deleted files under `Assets/RevCore/Hub/`.
- Spec file `docs/superpowers/specs/2026-05-14-revcore-tools-package-design.md`.
- Plan file `docs/superpowers/plans/2026-05-14-revcore-tools-package.md`.
- No `Assets/RCore/` files.

- [ ] **Step 5: Check whitespace**

Run:

```powershell
git diff --check -- Assets/RevCore/Tools Assets/RevCore/Hub docs/superpowers/specs/2026-05-14-revcore-tools-package-design.md docs/superpowers/plans/2026-05-14-revcore-tools-package.md
```

Expected:
- No trailing whitespace errors.

---

## Task 12: Unity Verification

**Files:**
- No edits unless compile issues found in `Assets/RevCore/Tools`.

- [ ] **Step 1: Compile in Unity**

Open Unity Editor and wait for script compilation.

Expected:
- No compile errors from `RevCore.Tools.Editor`.

If command-line Unity is configured, run project test/compile command used by this repo. If Unity CLI is unavailable, report that verification requires Unity Editor compile.

- [ ] **Step 2: Manual smoke test Hub**

In Unity Editor:
1. Open `RevCore/Tools Hub`.
2. Confirm categories: Navigate, Search, Generators, Utility, Addressables only if `ADDRESSABLES` exists.
3. Confirm skipped tools do not appear.
4. Click Open on Scenes Navigator, Asset Shortcuts, Screenshot Taker, Objects Finder, Asset Cleaner.
5. Confirm each window opens.
6. Use Find By GUID with known asset GUID and confirm object selection.
7. Toggle Auto Play First Scene and confirm menu checkmark changes.
8. Use Project Explorer buttons and confirm folders reveal.

- [ ] **Step 3: Manual smoke test safety**

In Asset Cleaner:
1. Run scan.
2. Click delete for one item.
3. Confirm dialog shows exact path before deletion.
4. Cancel deletion.

Expected:
- No deletion occurs on cancel.

---

## Task 13: Commit Scoped Changes

**Files:**
- Stage only:
  - `Assets/RevCore/Tools/**`
  - `Assets/RevCore/Hub/**` deletions
  - `docs/superpowers/specs/2026-05-14-revcore-tools-package-design.md`
  - `docs/superpowers/plans/2026-05-14-revcore-tools-package.md`

- [ ] **Step 1: Final status**

Run:

```powershell
git status --short
```

Expected:
- Unrelated dirty files may remain unstaged.
- Only scoped files will be staged.

- [ ] **Step 2: Stage scoped files only**

Run:

```powershell
git add Assets/RevCore/Tools docs/superpowers/specs/2026-05-14-revcore-tools-package-design.md docs/superpowers/plans/2026-05-14-revcore-tools-package.md
```

Run separately for deletions:

```powershell
git add -u Assets/RevCore/Hub
```

- [ ] **Step 3: Check staged diff**

Run:

```powershell
git diff --cached --name-only
```

Expected staged files only in scope listed above.

- [ ] **Step 4: Commit**

Run:

```powershell
git commit -m @'
feat(revcore): add standalone Tools package

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
'@
```

- [ ] **Step 5: Post-commit status**

Run:

```powershell
git status --short
```

Expected:
- Scoped Tools/Hub/spec/plan changes committed.
- Pre-existing unrelated dirty files may remain.

---

## Self-Review

### Spec coverage
- Standalone `RevCore.Tools` package: Task 1.
- Hub and `RevCoreTool` base class: Task 2.
- Local helpers replacing RCore helpers: Task 3.
- Navigate tools: Task 4.
- Search tools: Task 5.
- Generator tools: Task 6.
- Utility tools: Tasks 7-8.
- Addressables optional colorizer: Task 9.
- Remove old Hub: Task 10.
- Zero-dependency verification: Task 11.
- Unity verification: Task 12.
- Scoped commit: Task 13.

### Placeholder scan
No TBD/TODO/fill-later placeholders. Tasks that port longer RCore logic specify exact source transformations and required exclusions.

### Type consistency
- Namespace is consistently `RevCore.Tools.Editor`.
- Base class is `RevCoreTool`.
- Hub window is `RevCoreToolsWindow`.
- Helper class names match file names.
- Tool wrapper names match `OnOpen()` target windows.
