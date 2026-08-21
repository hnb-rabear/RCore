# SheetX Remove Flavors Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove conditional compilation flags `SX_LOCALIZATION`, `SX_LITE`, and `SX_NO_LOCALIZATION` from `Assets/RCore.SheetX/Editor/`, standardizing on the full single-flavor SheetX exporter.

**Architecture:** Strip all `#if`/`#elif`/`#else`/`#endif` directives related to the 3 symbols across all 7 editor files in SheetX, keeping default runtime and editor behavior intact. Retain `ASSETS_STORE` define switch for settings file path.

**Tech Stack:** C# (Unity 2022.3.62f2, .NET Standard 2.1), Unity Editor API, NUnit.

## Global Constraints

- Do not change serialized field names in `SheetXSettings`.
- Do not alter `SheetXSettings.asset` schema or locations.
- Keep `ASSETS_STORE` symbol support intact.
- Tab characters and CRLF line endings on `.cs` files.
- Update root `CHANGELOG.md` and `Assets/RCore.SheetX/CHANGELOG.md` per project requirements.
- Zero occurrences of `SX_LOCALIZATION`, `SX_LITE`, `SX_NO_LOCALIZATION` in `Assets/RCore.SheetX/Editor`; changelogs may retain names to document removal.

---

### Task 1: Clean Defines in SheetX Settings and Windows

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettings.cs:20-63`
- Modify: `Assets/RCore.SheetX/Editor/SheetXWindow.cs:10-22`
- Modify: `Assets/RCore.SheetX/Editor/SheetXSettingsWindow.cs:28-58`

**Interfaces:**
- Consumes: Existing `SheetXSettings`, `SheetXWindow`, `SheetXSettingsWindow`
- Produces: Simplified classes without `SX_` symbols

- [ ] **Step 1: Clean SheetXSettings.cs**

In `Assets/RCore.SheetX/Editor/SheetXSettings.cs`:
Replace lines around `SheetXConstants.APPLICATION_NAME` and `FILE_PATH`:

```csharp
namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Defines constants used throughout the SheetX application.
	/// </summary>
	public static class SheetXConstants
	{
		public const string APPLICATION_NAME = "SheetX - Sheets Exporter";
		public const string CONSTANTS_CS_TEMPLATE = "ConstantsTemplate";
		public const string IDS_CS_TEMPLATE = "IDsTemplate";
		public const string LOCALIZATION_MANAGER_TEMPLATE = "LocalizationsManagerTemplate";
		public const string LOCALIZATION_TEMPLATE = "LocalizationTemplateV2";
		public const string LOCALIZATION_TEXT_TEMPLATE = "LocalizationTextTemplate";
		public const string IDS_SHEET = "IDs";
		public const string CONSTANTS_SHEET = "Constants";
		public const string SETTINGS_SHEET = "Settings";
		public const string LOCALIZATION_SHEET = "Localization";
	}
...
	public class SheetXSettings : ScriptableObject
	{
#if ASSETS_STORE
		private const string FILE_PATH = "Assets/SheetX/Editor/SheetXSettings.asset";
#else
		// Must live in the consuming project's Assets/, never inside the package. When SheetX is
		// installed via UPM git URL the package resolves into Library/PackageCache, which is
		// gitignored and rebuilt from git on every re-resolve — any settings stored there are lost.
		private const string FILE_PATH = "Assets/SheetX/SheetXSettings.asset";
#endif
```

- [ ] **Step 2: Clean SheetXWindow.cs**

In `Assets/RCore.SheetX/Editor/SheetXWindow.cs`:
Replace header constants:

```csharp
	public class SheetXWindow : EditorWindow
	{
		private const string NAME = "SheetX: Sheets Exporter";
		private const string MENU = "SheetX";

		private Vector2 m_scrollPosition;
```

- [ ] **Step 3: Clean SheetXSettingsWindow.cs**

In `Assets/RCore.SheetX/Editor/SheetXSettingsWindow.cs`:
Simplify `OnGUI` without preprocessor conditionals:

```csharp
		public void OnGUI()
		{
			EditorGUI.BeginChangeCheck();
			GUILayout.BeginVertical("box");
			m_sheetXSettings.constantsOutputFolder = EditorHelper.FolderField(m_sheetXSettings.constantsOutputFolder, "Scripts output folder", 200);
			m_sheetXSettings.jsonOutputFolder = EditorHelper.FolderField(m_sheetXSettings.jsonOutputFolder, "Json output folder", 200);
			m_sheetXSettings.localizationOutputFolder = EditorHelper.FolderField(m_sheetXSettings.localizationOutputFolder, "Localization output folder", 200);
			m_sheetXSettings.@namespace = EditorHelper.TextField(m_sheetXSettings.@namespace, "Namespace", 200);
			m_sheetXSettings.separateIDs = EditorHelper.Toggle(m_sheetXSettings.separateIDs, "Separate IDs Sheets", 200);
			m_sheetXSettings.separateConstants = EditorHelper.Toggle(m_sheetXSettings.separateConstants, "Separate Constants Sheets", 200);
			m_sheetXSettings.separateLocalizations = EditorHelper.Toggle(m_sheetXSettings.separateLocalizations, "Separate Localizations Sheets", 200);
			m_sheetXSettings.onlyEnumAsIDs = EditorHelper.Toggle(m_sheetXSettings.onlyEnumAsIDs, "Only enum as IDs", 200);
			m_sheetXSettings.combineJson = EditorHelper.Toggle(m_sheetXSettings.combineJson, "Combine Json Sheets", 200);
			m_sheetXSettings.langCharSets = EditorHelper.TextField(m_sheetXSettings.langCharSets, "Lang char sets", 200);
			m_sheetXSettings.persistentFields = EditorHelper.TextField(m_sheetXSettings.persistentFields, "Persistent fields", 200);
			m_sheetXSettings.ObfGoogleClientId = EditorHelper.TextField(m_sheetXSettings.ObfGoogleClientId, "Google client id", 200);
			m_sheetXSettings.ObfGoogleClientSecret = EditorHelper.TextField(m_sheetXSettings.ObfGoogleClientSecret, "Google client secret", 200);
			GUILayout.EndVertical();
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(m_sheetXSettings);
			if (GUILayout.Button("Reset to default settings"))
				m_sheetXSettings.ResetToDefault();

			SupportDev();
		}
```

- [ ] **Step 4: Verify syntax & symbols in modified files**

Run search:
`grep -E "SX_LOCALIZATION|SX_LITE|SX_NO_LOCALIZATION" Assets/RCore.SheetX/Editor/SheetXSettings.cs Assets/RCore.SheetX/Editor/SheetXWindow.cs Assets/RCore.SheetX/Editor/SheetXSettingsWindow.cs`
Expected: zero matches.

- [ ] **Step 5: Commit**

```bash
git add Assets/RCore.SheetX/Editor/SheetXSettings.cs Assets/RCore.SheetX/Editor/SheetXWindow.cs Assets/RCore.SheetX/Editor/SheetXSettingsWindow.cs
git commit -m "fix(sheetx): remove flavor defines from settings and root window"
```

---

### Task 2: Clean Defines in Excel and Google Windows

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetXWindow.cs:38-191`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetXWindow.cs:34-150`

**Interfaces:**
- Consumes: `ExcelSheetXWindow`, `GoogleSheetXWindow`
- Produces: Single-file + Multi-file tabs unconditionally exposed in both windows

- [ ] **Step 1: Clean ExcelSheetXWindow.cs**

In `Assets/RCore.SheetX/Editor/ExcelSheetXWindow.cs`:
- In `OnGUI()`: Remove `#if SX_LITE ... #endif` wrappers, keep only the tab selection switch block.
- In `PageSingleFile()`: Remove `#if !SX_LOCALIZATION` and `#if !SX_NO_LOCALIZATION` wrappers around `Export Localizations`.
- In `PageMultiFiles()`: Remove `#if !SX_LITE` wrapper.

- [ ] **Step 2: Clean GoogleSheetXWindow.cs**

In `Assets/RCore.SheetX/Editor/GoogleSheetXWindow.cs`:
- In `OnGUI()`: Remove `#if SX_LITE ... #endif` wrappers, keep only the tab selection switch block.
- In `PageSingleFile()`: Remove `#if !SX_LOCALIZATION` and `#if !SX_NO_LOCALIZATION` wrappers around `Export Localizations`.
- In `PageMultiFiles()`: Remove `#if !SX_LITE` wrapper.

- [ ] **Step 3: Verify syntax & symbols in window files**

Run search:
`grep -E "SX_LOCALIZATION|SX_LITE|SX_NO_LOCALIZATION" Assets/RCore.SheetX/Editor/ExcelSheetXWindow.cs Assets/RCore.SheetX/Editor/GoogleSheetXWindow.cs`
Expected: zero matches.

- [ ] **Step 4: Commit**

```bash
git add Assets/RCore.SheetX/Editor/ExcelSheetXWindow.cs Assets/RCore.SheetX/Editor/GoogleSheetXWindow.cs
git commit -m "fix(sheetx): remove flavor defines from excel and google sheet windows"
```

---

### Task 3: Clean Defines in Excel and Google Handlers

**Files:**
- Modify: `Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs:310-1750`
- Modify: `Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs:380-1860`

**Interfaces:**
- Consumes: `ExcelSheetHandler`, `GoogleSheetHandler`
- Produces: Unconditional ExportConstants, ExportJson, ExportLocalizations, and ExportAllFiles

- [ ] **Step 1: Clean ExcelSheetHandler.cs**

Remove all `#if !SX_LOCALIZATION`, `#if !SX_NO_LOCALIZATION`, `#if !SX_LITE`, and their matching `#endif` inside `ExcelSheetHandler.cs`. Keep the actual method bodies and loops.

- [ ] **Step 2: Clean GoogleSheetHandler.cs**

Remove all `#if !SX_LOCALIZATION`, `#if !SX_NO_LOCALIZATION`, `#if !SX_LITE`, and their matching `#endif` inside `GoogleSheetHandler.cs`. Keep the actual method bodies and loops.

- [ ] **Step 3: Verify all SheetX files for target symbols**

Run search:
`grep -rnE "SX_LOCALIZATION|SX_LITE|SX_NO_LOCALIZATION" Assets/RCore.SheetX`
Expected: 0 matches.

- [ ] **Step 4: Commit**

```bash
git add Assets/RCore.SheetX/Editor/ExcelSheetHandler.cs Assets/RCore.SheetX/Editor/GoogleSheetHandler.cs
git commit -m "fix(sheetx): remove flavor defines from excel and google sheet handlers"
```

---

### Task 4: Add Verification Unit Test & Update Changelogs

**Files:**
- Modify: `Assets/RCore.SheetX/Tests/SheetXSettingsTests.cs`
- Modify: `Assets/RCore.SheetX/CHANGELOG.md`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: Test suite and documentation
- Produces: Automated test ensuring defines are not reintroduced, updated changelogs

- [ ] **Step 1: Add automated check in SheetXSettingsTests.cs**

Add a test method to ensure no flavor define directives exist in any `.cs` file under `Assets/RCore.SheetX/Editor/`:

```csharp
		[Test]
		public void no_legacy_flavor_defines_exist_in_editor_scripts()
		{
			var editorScripts = Directory.GetFiles("Assets/RCore.SheetX/Editor", "*.cs", SearchOption.AllDirectories);
			var legacyDefines = new[] { "SX_LOCALIZATION", "SX_LITE", "SX_NO_LOCALIZATION" };

			foreach (var file in editorScripts)
			{
				string content = File.ReadAllText(file);
				foreach (var define in legacyDefines)
				{
					Assert.IsFalse(content.Contains(define),
						$"File '{file}' still contains legacy define '{define}'. SheetX should be single-flavor.");
				}
			}
		}
```

- [ ] **Step 2: Update Assets/RCore.SheetX/CHANGELOG.md**

Add entry under `## [Unreleased]`:

```markdown
### Removed

- Removed conditional compilation symbols `SX_LOCALIZATION`, `SX_LITE`, and `SX_NO_LOCALIZATION`. SheetX is now a unified single-flavor exporter.
```

- [ ] **Step 3: Update root CHANGELOG.md**

Add entry under `## [Unreleased]` -> `### Changed`:

```markdown
- `RCore.SheetX`: Removed conditional compilation symbols `SX_LOCALIZATION`, `SX_LITE`, and `SX_NO_LOCALIZATION` from editor scripts to unify into a single-flavor exporter.
```

- [ ] **Step 4: Run EditMode tests**

Run tests via Unity or verify test suite integrity:

- `SheetXSettingsTests.no_legacy_flavor_defines_exist_in_editor_scripts`
- `SheetXSettingsTests.init_resolves_asset_under_assets_not_packages`
- `SheetXSettingsTests.init_returns_same_asset_on_repeated_calls`
- `SheetXSettingsTests.save_persists_sheet_paths_across_reload`

- [ ] **Step 5: Manual SheetX acceptance checklist**

Verify in Editor:

- Open `SheetX` window (`RCore > SheetX` or `Window > SheetX`).
- Excel and Google views show `Export Single File` and `Export Multi Files` tabs.
- Settings shows scripts, json, and localization output folders and toggles.
- Single-file export controls include `Export Localizations`.

- [ ] **Step 6: Commit**

```bash
git add Assets/RCore.SheetX/Tests/SheetXSettingsTests.cs Assets/RCore.SheetX/CHANGELOG.md CHANGELOG.md
git commit -m "chore(sheetx): add test guarding against flavor defines and update changelogs"
```
