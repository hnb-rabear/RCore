# TMPFontMaterials ScriptableObject Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let legacy `[TMPFontMaterials]` select all project TMP materials on `ScriptableObject` fields while preserving component font-atlas filtering.

**Architecture:** Extend existing legacy drawer with one cached project-wide TMP-material query. Existing `Component` path remains unchanged. Record deferred RevCore port requirements in migration inventory; do not change RevCore packages.

**Tech Stack:** Unity Editor `AssetDatabase`, TextMeshPro, C#, NUnit/EditMode where legacy test assembly exists.

## Global Constraints

- Modify `Assets/RCore/` only; do not modify `Assets/RevCore/`.
- Keep `TMPFontMaterialsAttribute` public API unchanged.
- C# files use tabs and CRLF.
- Update root `CHANGELOG.md`, root `README.md`, migration CSV, and generated migration Markdown.
- Do not commit without explicit user request.

---

### Task 1: Add ScriptableObject TMP-material lookup

**Files:**
- Modify: `Assets/RCore/Main/Runtime/Inspector/TMPFontMaterialsAttribute.cs`

**Interfaces:**
- Consumes: `SerializedProperty.serializedObject.targetObject`, `AssetDatabase.FindAssets("t:Material")`.
- Produces: `TMPFontMaterialsDrawer.OnGUI` showing all material assets whose shader name starts with `TextMeshPro/` for `ScriptableObject` targets.

- [ ] **Step 1: Add project material cache**

```csharp
private static Material[] s_projectMaterialCache;
```

- [ ] **Step 2: Add project-wide lookup**

```csharp
private static Material[] FindProjectMaterials()
{
	if (s_projectMaterialCache != null)
		return s_projectMaterialCache;

	s_projectMaterialCache = AssetDatabase.FindAssets("t:Material")
		.Select(AssetDatabase.GUIDToAssetPath)
		.Select(AssetDatabase.LoadAssetAtPath<Material>)
		.Where(material => material != null && material.shader != null && material.shader.name.StartsWith("TextMeshPro/"))
		.ToArray();
	return s_projectMaterialCache;
}
```

- [ ] **Step 3: Select lookup by target type**

```csharp
var materials = fontAsset != null
	? FindMaterialReferences(fontAsset)
	: property.serializedObject.targetObject is ScriptableObject
		? FindProjectMaterials()
		: null;
```

Keep component behavior on `FindMaterialReferences(fontAsset)`. Show `No TextMeshPro materials found in project.` when project-wide lookup is empty. Keep existing missing-font message for component target.

- [ ] **Step 4: Clear both caches**

```csharp
private static void ClearCaches()
{
	s_materialCache.Clear();
	s_projectMaterialCache = null;
}
```

Call `ClearCaches()` from reload and existing edit/play-mode cache invalidation.

- [ ] **Step 5: Verify source format**

Run:

```powershell
python -c "d=open('Assets/RCore/Main/Runtime/Inspector/TMPFontMaterialsAttribute.cs','rb').read(); assert b'\r\n' in d and b'\n' not in d.replace(b'\r\n', b'')"
```

Expected: exit code `0`.

### Task 2: Document behavior and deferred RevCore port

**Files:**
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/migration/rcore-to-revcore-api-map.csv`
- Modify: `docs/migration/rcore-to-revcore-api-map.md`

**Interfaces:**
- Produces: user-facing behavior description and RevCore port constraints.

- [ ] **Step 1: Update root README attribute row**

```markdown
| `[TMPFontMaterials]` | TMP font material dropdown (component: same font atlas; ScriptableObject: all project TMP materials) |
```

- [ ] **Step 2: Add Unreleased changelog entry**

```markdown
- `RCore.Inspector.TMPFontMaterialsAttribute` now lists every `TextMeshPro/*` material asset when used on a `ScriptableObject`; component behavior remains filtered to materials sharing its TMP font atlas.
```

- [ ] **Step 3: Record RevCore migration constraint**

In both migration files, replace generic `port or drop?` notes for `TMPFontMaterialsAttribute` and `TMPFontMaterialsDrawer` with port-pending notes covering:

- `RevCore.Inspector` has no TMP dependency.
- Port must retain component font-atlas filtering and ScriptableObject project-wide TMP list.
- Package ownership decision: `RevCore.UI` already depends on TMP, or Inspector declares TMP dependency.

- [ ] **Step 4: Verify CSV shape**

Run:

```powershell
python -c "import csv; rows=list(csv.reader(open('docs/migration/rcore-to-revcore-api-map.csv', encoding='utf-8'))); assert any(r[1] == 'TMPFontMaterialsAttribute' and 'port pending' in r[-1] for r in rows)"
```

Expected: exit code `0`.

### Task 3: Build check

**Files:**
- Verify: `RCore.sln`

**Interfaces:**
- Verifies: changes compile against current Unity-generated solution when SDK supports it.

- [ ] **Step 1: Run solution build without restore**

```powershell
dotnet build RCore.sln --no-restore
```

Expected: exit code `0`. If solution has unrelated Unity-generated build failures, report exact first failure and verify edited source structurally with Task 1 and Task 2 commands.
