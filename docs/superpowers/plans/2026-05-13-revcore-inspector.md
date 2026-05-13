# RevCore.Inspector Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build RevCore.Inspector — attribute-based inspector decorations and custom editors, zero external deps beyond Unity + Foundation.

**Architecture:** Attributes in Runtime asmdef (so any script can use `[ReadOnly]`, `[Separator]` etc.). Drawers/Editors in Editor asmdef. Clean split unlike RCore's `#if UNITY_EDITOR` in same file. No TMP, no Addressables, no RCore dependency.

**Tech Stack:** Unity 2022.3, C#, asmdef isolation, EditMode tests.

---

## File Structure

```
Assets/RevCore/Inspector/
  package.json
  README.md
  CHANGELOG.md
  Runtime/
    RevCore.Inspector.Runtime.asmdef
    Attributes/
      ReadOnlyAttribute.cs
      SeparatorAttribute.cs
      CommentAttribute.cs
      HighlightAttribute.cs
      ShowIfAttribute.cs
      AutoFillAttribute.cs
      CreateScriptableObjectAttribute.cs
      ExposeScriptableObjectAttribute.cs
      FolderPathAttribute.cs
      SingleLayerAttribute.cs
      SpriteBoxAttribute.cs
      TagSelectorAttribute.cs
      DisplayEnumAttribute.cs
      InspectorButtonAttribute.cs
  Editor/
    RevCore.Inspector.Editor.asmdef
    Drawers/
      ReadOnlyDrawer.cs
      SeparatorDrawer.cs
      CommentDrawer.cs
      HighlightDrawer.cs
      ShowIfDrawer.cs
      AutoFillDrawer.cs
      CreateScriptableObjectDrawer.cs
      ExposeScriptableObjectDrawer.cs
      FolderPathDrawer.cs
      SingleLayerDrawer.cs
      SpriteBoxDrawer.cs
      TagSelectorDrawer.cs
      DisplayEnumDrawer.cs
      InspectorButtonEditor.cs
    Editors/
      MeshInfoEditor.cs
      MeshRendererEditor.cs
  Tests/
    Runtime/
      RevCore.Inspector.Tests.asmdef
      InspectorAttributeTests.cs
  Samples~/
    InspectorSample/
      InspectorSample.cs
```

## Scope decisions

### Included (standalone, no external deps)
| Attribute | Purpose | RCore source |
|---|---|---|
| ReadOnly | Disable GUI for field | `Runtime/Inspector/ReadOnlyAttribute.cs` |
| Separator | Horizontal line with optional title | `Runtime/Inspector/SeparatorAttribute.cs` |
| Comment | Help text above field | `Runtime/Inspector/CommentAttribute.cs` |
| Highlight | Cyan-highlighted field | `Runtime/Inspector/HighlightAttribute.cs` |
| ShowIf | Conditional show by bool | `Runtime/Inspector/ShowIfAttribute.cs` |
| AutoFill | Auto-assign Component/SO refs | `Runtime/Inspector/AutoFillAttribute.cs` |
| CreateScriptableObject | Create button for SO field | `Runtime/Inspector/CreateScriptableObjectAttribute.cs` |
| ExposeScriptableObject | Inline SO foldout | `Runtime/Inspector/ExposeScriptableObjectAttribute.cs` |
| FolderPath | Folder picker string | `Runtime/Inspector/FolderPathAttribute.cs` |
| SingleLayer | Layer dropdown for int | `Runtime/Inspector/SingleLayerAttribute.cs` |
| SpriteBox | Sprite preview box | `Runtime/Inspector/SpriteBoxAttribute.cs` |
| TagSelector | Tag dropdown for string | `Runtime/Inspector/TagSelectorAttribute.cs` |
| DisplayEnum | Int as enum popup | `Runtime/Inspector/DisplayEnumAttribute.cs` |
| InspectorButton | Method buttons in inspector | `Runtime/Inspector/InspectorButtonAttribute.cs` |

### Included custom editors
| Editor | Purpose | RCore source |
|---|---|---|
| MeshInfoEditor | Show vertex/tri counts | `Editor/Inspector/MeshInfoEditor.cs` |
| MeshRendererEditor | Sorting layer popup | `Editor/Inspector/MeshRendererEditor.cs` |

### Excluded (external deps)
| Item | Reason |
|---|---|
| TMPFontMaterials | Requires TextMeshPro package |
| AssetBundleRef/Wrap drawers | Requires Addressables + RCore types |
| TimeStampDrawer | Requires DateTimePickerWindow from RCore editor helpers |

---

## Task 1: Package scaffold

**Files:**
- Create: `Assets/RevCore/Inspector/package.json`
- Create: `Assets/RevCore/Inspector/Runtime/RevCore.Inspector.Runtime.asmdef`
- Create: `Assets/RevCore/Inspector/Editor/RevCore.Inspector.Editor.asmdef`
- Create: `Assets/RevCore/Inspector/Tests/Runtime/RevCore.Inspector.Tests.asmdef`
- Create: `Assets/RevCore/Inspector/CHANGELOG.md`

- [ ] **Step 1: Create package.json**

```json
{
  "name": "com.rabear.revcore.inspector",
  "version": "0.1.0",
  "displayName": "RevCore.Inspector",
  "description": "Attribute-based inspector decorations and custom editors for Unity.",
  "unity": "2022.3",
  "documentationUrl": "https://github.com/hnb-rabear/RCore",
  "author": {
    "name": "HNB RaBear",
    "email": "nbhung71711@gmail.com",
    "url": "https://github.com/hnb-rabear"
  },
  "keywords": ["inspector", "attributes", "editor", "drawers"],
  "dependencies": {}
}
```

- [ ] **Step 2: Create Runtime asmdef**

```json
{
  "name": "RevCore.Inspector.Runtime",
  "rootNamespace": "RevCore",
  "references": [],
  "includePlatforms": [],
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

- [ ] **Step 3: Create Editor asmdef**

```json
{
  "name": "RevCore.Inspector.Editor",
  "rootNamespace": "RevCore.Editor",
  "references": ["RevCore.Inspector.Runtime"],
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

- [ ] **Step 4: Create Tests asmdef**

```json
{
  "name": "RevCore.Inspector.Tests",
  "rootNamespace": "RevCore.Tests",
  "references": [
    "RevCore.Inspector.Runtime",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": [
    "UNITY_INCLUDE_TESTS"
  ],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 5: Create CHANGELOG.md**

```markdown
# Changelog

## [0.1.0] - 2026-05-13

### Added
- Package scaffold
- 14 inspector attributes: ReadOnly, Separator, Comment, Highlight, ShowIf, AutoFill, CreateScriptableObject, ExposeScriptableObject, FolderPath, SingleLayer, SpriteBox, TagSelector, DisplayEnum, InspectorButton
- Property drawers for all attributes
- MeshInfoEditor and MeshRendererEditor custom editors
- EditMode attribute tests
- README with API reference and usage examples
```

- [ ] **Step 6: Verify scaffold**

Check all 5 files exist. No RCore references.

---

## Task 2: Simple decoration attributes (ReadOnly, Separator, Comment, Highlight)

**Files:**
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/ReadOnlyAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/SeparatorAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/CommentAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/HighlightAttribute.cs`

- [ ] **Step 1: Create ReadOnlyAttribute**

Read RCore source `Assets/RCore/Main/Runtime/Inspector/ReadOnlyAttribute.cs`, extract attribute only (no drawer).

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class ReadOnlyAttribute : PropertyAttribute { }
}
```

- [ ] **Step 2: Create SeparatorAttribute**

Read RCore source `Assets/RCore/Main/Runtime/Inspector/SeparatorAttribute.cs`, extract attribute only.

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class SeparatorAttribute : PropertyAttribute
    {
        public string Title { get; }
        public float Height { get; }

        public SeparatorAttribute(string title = "", float height = 1f)
        {
            Title = title;
            Height = height;
        }
    }
}
```

- [ ] **Step 3: Create CommentAttribute**

Read RCore source `Assets/RCore/Main/Runtime/Inspector/CommentAttribute.cs`, extract attribute.

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class CommentAttribute : PropertyAttribute
    {
        public string Text { get; }

        public CommentAttribute(string text)
        {
            Text = text;
        }
    }
}
```

- [ ] **Step 4: Create HighlightAttribute**

Read RCore source `Assets/RCore/Main/Runtime/Inspector/HighlightAttribute.cs`, extract attribute.

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class HighlightAttribute : PropertyAttribute { }
}
```

- [ ] **Step 5: Verify**

Check 4 files exist. All `namespace RevCore`. No `using UnityEditor`.

---

## Task 3: Conditional and behavior attributes (ShowIf, AutoFill, InspectorButton)

**Files:**
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/ShowIfAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/AutoFillAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/InspectorButtonAttribute.cs`

- [ ] **Step 1: Create ShowIfAttribute**

Read RCore source, port attribute definition.

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionName { get; }
        public bool Inverse { get; }

        public ShowIfAttribute(string conditionName, bool inverse = false)
        {
            ConditionName = conditionName;
            Inverse = inverse;
        }
    }
}
```

- [ ] **Step 2: Create AutoFillAttribute**

Read RCore source, port attribute definition.

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class AutoFillAttribute : PropertyAttribute { }
}
```

- [ ] **Step 3: Create InspectorButtonAttribute**

Read RCore source, port attribute definition.

```csharp
using System;

namespace RevCore
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class InspectorButtonAttribute : Attribute
    {
        public string Label { get; }

        public InspectorButtonAttribute(string label = null)
        {
            Label = label;
        }
    }
}
```

- [ ] **Step 4: Verify**

Check 3 files. No `using UnityEditor`.

---

## Task 4: Field-type attributes (DisplayEnum, SingleLayer, TagSelector, FolderPath, SpriteBox)

**Files:**
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/DisplayEnumAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/SingleLayerAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/TagSelectorAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/FolderPathAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/SpriteBoxAttribute.cs`

- [ ] **Step 1: Create DisplayEnumAttribute**

Read RCore source, port attribute.

```csharp
using System;
using UnityEngine;

namespace RevCore
{
    public sealed class DisplayEnumAttribute : PropertyAttribute
    {
        public Type EnumType { get; }

        public DisplayEnumAttribute(Type enumType)
        {
            EnumType = enumType;
        }
    }
}
```

- [ ] **Step 2: Create SingleLayerAttribute**

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class SingleLayerAttribute : PropertyAttribute { }
}
```

- [ ] **Step 3: Create TagSelectorAttribute**

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class TagSelectorAttribute : PropertyAttribute
    {
        public bool AllowUntagged { get; }

        public TagSelectorAttribute(bool allowUntagged = true)
        {
            AllowUntagged = allowUntagged;
        }
    }
}
```

- [ ] **Step 4: Create FolderPathAttribute**

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class FolderPathAttribute : PropertyAttribute { }
}
```

- [ ] **Step 5: Create SpriteBoxAttribute**

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class SpriteBoxAttribute : PropertyAttribute
    {
        public float Height { get; }

        public SpriteBoxAttribute(float height = 64f)
        {
            Height = height;
        }
    }
}
```

- [ ] **Step 6: Verify**

Check 5 files. No `using UnityEditor`.

---

## Task 5: ScriptableObject attributes (CreateScriptableObject, ExposeScriptableObject)

**Files:**
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/CreateScriptableObjectAttribute.cs`
- Create: `Assets/RevCore/Inspector/Runtime/Attributes/ExposeScriptableObjectAttribute.cs`

- [ ] **Step 1: Create CreateScriptableObjectAttribute**

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class CreateScriptableObjectAttribute : PropertyAttribute { }
}
```

- [ ] **Step 2: Create ExposeScriptableObjectAttribute**

```csharp
using UnityEngine;

namespace RevCore
{
    public sealed class ExposeScriptableObjectAttribute : PropertyAttribute { }
}
```

- [ ] **Step 3: Verify**

Check 2 files.

---

## Task 6: Decoration drawers (ReadOnly, Separator, Comment, Highlight)

**Files:**
- Create: `Assets/RevCore/Inspector/Editor/Drawers/ReadOnlyDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/SeparatorDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/CommentDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/HighlightDrawer.cs`

- [ ] **Step 1: Read RCore sources**

Read each RCore drawer implementation to port logic faithfully.

- [ ] **Step 2: Create ReadOnlyDrawer**

Port from RCore. Drawer disables GUI.

- [ ] **Step 3: Create SeparatorDrawer**

Port from RCore. DecoratorDrawer draws line + optional title.

- [ ] **Step 4: Create CommentDrawer**

Port from RCore. DecoratorDrawer draws help box text.

- [ ] **Step 5: Create HighlightDrawer**

Port from RCore. Drawer sets GUI color to cyan.

- [ ] **Step 6: Verify**

Check 4 files in Editor. All `namespace RevCore.Editor`. All use `CustomPropertyDrawer` or `CustomDecoratorDrawer`.

---

## Task 7: Conditional drawers (ShowIf, AutoFill, InspectorButton)

**Files:**
- Create: `Assets/RevCore/Inspector/Editor/Drawers/ShowIfDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/AutoFillDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/InspectorButtonEditor.cs`

- [ ] **Step 1: Read RCore sources**

Read ShowIf, AutoFill, InspectorButton drawer/editor implementations.

- [ ] **Step 2: Create ShowIfDrawer**

Port from RCore. Uses reflection to find bool field/property/method.

- [ ] **Step 3: Create AutoFillDrawer**

Port from RCore. Auto-assigns Component/SO references.

- [ ] **Step 4: Create InspectorButtonEditor**

Port from RCore. Custom Editor for MonoBehaviour that finds `[InspectorButton]` methods and draws buttons.

- [ ] **Step 5: Verify**

Check 3 files. No RCore references.

---

## Task 8: Field-type drawers (DisplayEnum, SingleLayer, TagSelector, FolderPath, SpriteBox)

**Files:**
- Create: `Assets/RevCore/Inspector/Editor/Drawers/DisplayEnumDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/SingleLayerDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/TagSelectorDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/FolderPathDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/SpriteBoxDrawer.cs`

- [ ] **Step 1: Read RCore sources**

Read each drawer implementation.

- [ ] **Step 2: Create all 5 drawers**

Port from RCore sources. Each drawer maps to its attribute.

- [ ] **Step 3: Verify**

Check 5 files. No RCore references.

---

## Task 9: ScriptableObject drawers (CreateScriptableObject, ExposeScriptableObject)

**Files:**
- Create: `Assets/RevCore/Inspector/Editor/Drawers/CreateScriptableObjectDrawer.cs`
- Create: `Assets/RevCore/Inspector/Editor/Drawers/ExposeScriptableObjectDrawer.cs`

- [ ] **Step 1: Read RCore sources**

Read CreateScriptableObject and ExposeScriptableObject drawer implementations.

- [ ] **Step 2: Create both drawers**

Port from RCore. CreateScriptableObjectDrawer creates SO asset via dialog. ExposeScriptableObjectDrawer draws inline foldout editor.

- [ ] **Step 3: Verify**

Check 2 files. No RCore references.

---

## Task 10: Custom editors (MeshInfo, MeshRenderer)

**Files:**
- Create: `Assets/RevCore/Inspector/Editor/Editors/MeshInfoEditor.cs`
- Create: `Assets/RevCore/Inspector/Editor/Editors/MeshRendererEditor.cs`

- [ ] **Step 1: Read RCore sources**

Read `Assets/RCore/Main/Editor/Inspector/MeshInfoEditor.cs` and `MeshRendererEditor.cs`.

- [ ] **Step 2: Create MeshInfoEditor**

Port SkinnedMeshRendererEditor and MeshFilterEditor. Show vertex/tri/submesh count.

- [ ] **Step 3: Create MeshRendererEditor**

Port sorting layer popup + order in layer.

- [ ] **Step 4: Verify**

Check 2 files. No RCore references.

---

## Task 11: Tests

**Files:**
- Create: `Assets/RevCore/Inspector/Tests/Runtime/InspectorAttributeTests.cs`

- [ ] **Step 1: Write attribute instantiation tests**

```csharp
using NUnit.Framework;

namespace RevCore.Tests
{
    public class InspectorAttributeTests
    {
        [Test]
        public void ReadOnly_attribute_creates()
        {
            var attr = new ReadOnlyAttribute();
            Assert.IsNotNull(attr);
        }

        [Test]
        public void Separator_stores_title_and_height()
        {
            var attr = new SeparatorAttribute("Section", 2f);
            Assert.AreEqual("Section", attr.Title);
            Assert.AreEqual(2f, attr.Height);
        }

        [Test]
        public void Comment_stores_text()
        {
            var attr = new CommentAttribute("Help text");
            Assert.AreEqual("Help text", attr.Text);
        }

        [Test]
        public void ShowIf_stores_condition_and_inverse()
        {
            var attr = new ShowIfAttribute("isEnabled", true);
            Assert.AreEqual("isEnabled", attr.ConditionName);
            Assert.IsTrue(attr.Inverse);
        }

        [Test]
        public void DisplayEnum_stores_type()
        {
            var attr = new DisplayEnumAttribute(typeof(System.DayOfWeek));
            Assert.AreEqual(typeof(System.DayOfWeek), attr.EnumType);
        }

        [Test]
        public void TagSelector_default_allows_untagged()
        {
            var attr = new TagSelectorAttribute();
            Assert.IsTrue(attr.AllowUntagged);
        }

        [Test]
        public void SpriteBox_default_height_64()
        {
            var attr = new SpriteBoxAttribute();
            Assert.AreEqual(64f, attr.Height);
        }

        [Test]
        public void InspectorButton_stores_label()
        {
            var attr = new InspectorButtonAttribute("Do Thing");
            Assert.AreEqual("Do Thing", attr.Label);
        }
    }
}
```

- [ ] **Step 2: Verify**

Check test file exists. References only `RevCore` namespace.

---

## Task 12: README docs and sample

**Files:**
- Create: `Assets/RevCore/Inspector/README.md`
- Create: `Assets/RevCore/Inspector/Samples~/InspectorSample/InspectorSample.cs`

- [ ] **Step 1: Write README**

Must include: Install, Quick Start, Concepts, API Reference table, common use cases, migration from RCore, troubleshooting.

- [ ] **Step 2: Create sample**

```csharp
using UnityEngine;

namespace RevCore.Samples
{
    public class InspectorSample : MonoBehaviour
    {
        [ReadOnly] public int health = 100;
        [Separator("Settings")]
        [Comment("Movement speed in units per second")]
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
}
```

- [ ] **Step 3: Verify**

Check README sections. Check sample compiles.

---

## Verification

After all tasks complete:

1. Unity compiles with zero errors
2. All EditMode tests pass
3. `Assets/RCore/` untouched
4. Package Manager shows "RevCore.Inspector 0.1.0"
5. README readable with clear Quick Start
6. Sample importable via Package Manager samples tab
7. No dependency on Foundation (inspector is standalone)
