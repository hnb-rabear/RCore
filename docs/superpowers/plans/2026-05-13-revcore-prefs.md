# RevCore.Prefs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build RevCore.Prefs as a Unity Package Manager package for typed PlayerPrefs and EditorPrefs wrappers with explicit save/delete behavior and no RCore dependency.

**Architecture:** Runtime contains typed PlayerPrefs wrappers and a small container for batched save/delete. Editor assembly contains EditorPrefs wrappers and editor-only container. Object/list/dictionary persistence from RCore is deferred to RevCore.Data because it requires JSON/encryption policy.

**Tech Stack:** Unity 2022.3, C#, NUnit EditMode tests, Unity asmdefs, UPM package layout, optional dependency on RevCore.Foundation only.

---

## Source Audit

RCore prefs source lives in:

- `Assets/RCore/Main/Runtime/Common/RPlayerPrefs.cs`
  - `RPlayerPrefContainer`: `Register`, `SaveChanges`, `DeleteAll`
  - `RPlayerPref`: `key`, `changed`, `Delete`, `SaveChange`
  - primitive wrappers: `RPlayerPrefBool`, `RPlayerPrefInt`, `RPlayerPrefFloat`, `RPlayerPrefString`
  - complex wrappers: `RPlayerPrefList<T>`, `RPlayerPrefDict<TKey,TVal>`, `RPlayerPrefObject<T>`, `RPlayerPrefSerializableObject<T>`
  - dependencies: `UnityEngine.PlayerPrefs`, `Newtonsoft.Json`, `JsonHelper`, `Encryption`
- `Assets/RCore/Main/Runtime/Common/REditorPrefs.cs`
  - editor-only wrappers under `#if UNITY_EDITOR`
  - primitive wrappers plus `REditorPrefEnum<T>`, `REditorPrefVector`
  - dependencies: `UnityEditor.EditorPrefs`, `EditorApplication`, `Newtonsoft.Json`, `JsonHelper`, `Encryption`

RevCore.Prefs keeps primitive typed wrappers now. Complex JSON/encrypted object storage moves to future RevCore.Data to avoid coupling Prefs to Newtonsoft/encryption.

## File Structure

Create these files only:

```text
Assets/RevCore/Prefs/
  package.json
  README.md
  CHANGELOG.md
  Runtime/
    RevCore.Prefs.Runtime.asmdef
    Contracts/
      IPref.cs
    PlayerPrefs/
      PlayerPrefContainer.cs
      PlayerPref.cs
      PlayerPrefBool.cs
      PlayerPrefInt.cs
      PlayerPrefFloat.cs
      PlayerPrefString.cs
  Editor/
    RevCore.Prefs.Editor.asmdef
    EditorPrefs/
      EditorPrefContainer.cs
      EditorPref.cs
      EditorPrefBool.cs
      EditorPrefInt.cs
      EditorPrefFloat.cs
      EditorPrefString.cs
      EditorPrefEnum.cs
      EditorPrefVector.cs
  Tests/
    Runtime/
      RevCore.Prefs.Tests.asmdef
      PlayerPrefTests.cs
  Samples~/
    PrefsSample/
      PrefsSample.cs
```

No files under `Assets/RCore/` are modified.

---

### Task 1: Package scaffold

**Files:**
- Create: `Assets/RevCore/Prefs/package.json`
- Create: `Assets/RevCore/Prefs/CHANGELOG.md`
- Create: `Assets/RevCore/Prefs/Runtime/RevCore.Prefs.Runtime.asmdef`
- Create: `Assets/RevCore/Prefs/Editor/RevCore.Prefs.Editor.asmdef`
- Create: `Assets/RevCore/Prefs/Tests/Runtime/RevCore.Prefs.Tests.asmdef`

- [ ] **Step 1: Create package manifest**

Write `Assets/RevCore/Prefs/package.json`:

```json
{
  "name": "com.rabear.revcore.prefs",
  "version": "0.1.0",
  "displayName": "RevCore.Prefs",
  "description": "Typed PlayerPrefs and EditorPrefs wrappers for the RevCore framework.",
  "unity": "2022.3",
  "documentationUrl": "https://github.com/hnb-rabear/RCore",
  "author": {
    "name": "HNB RaBear",
    "email": "nbhung71711@gmail.com",
    "url": "https://github.com/hnb-rabear"
  },
  "keywords": ["prefs", "playerprefs", "editorprefs", "framework"],
  "dependencies": {
    "com.rabear.revcore.foundation": "0.1.0"
  }
}
```

- [ ] **Step 2: Create runtime asmdef**

Write `Assets/RevCore/Prefs/Runtime/RevCore.Prefs.Runtime.asmdef`:

```json
{
  "name": "RevCore.Prefs.Runtime",
  "rootNamespace": "RevCore",
  "references": ["RevCore.Foundation.Runtime"],
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

- [ ] **Step 3: Create editor asmdef**

Write `Assets/RevCore/Prefs/Editor/RevCore.Prefs.Editor.asmdef`:

```json
{
  "name": "RevCore.Prefs.Editor",
  "rootNamespace": "RevCore.Editor",
  "references": ["RevCore.Prefs.Runtime"],
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

- [ ] **Step 4: Create test asmdef**

Write `Assets/RevCore/Prefs/Tests/Runtime/RevCore.Prefs.Tests.asmdef`:

```json
{
  "name": "RevCore.Prefs.Tests",
  "rootNamespace": "RevCore.Tests",
  "references": [
    "RevCore.Prefs.Runtime",
    "RevCore.Foundation.Runtime",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 5: Create changelog**

Write `Assets/RevCore/Prefs/CHANGELOG.md`:

```markdown
# Changelog

## [0.1.0] - 2026-05-13

### Added
- Package scaffold
- Typed PlayerPrefs wrappers for bool, int, float, string
- EditorPrefs wrappers for bool, int, float, string, enum, Vector3
- Batched save/delete containers
- Runtime tests
- README and sample
```

- [ ] **Step 6: Review scaffold**

Read scaffold files and verify package name, asmdef references, editor-only asmdef, test references, and no `Assets/RCore/` changes.

---

### Task 2: Runtime PlayerPrefs core

**Files:**
- Create: `Assets/RevCore/Prefs/Runtime/Contracts/IPref.cs`
- Create: `Assets/RevCore/Prefs/Runtime/PlayerPrefs/PlayerPrefContainer.cs`
- Create: `Assets/RevCore/Prefs/Runtime/PlayerPrefs/PlayerPref.cs`
- Create: `Assets/RevCore/Prefs/Runtime/PlayerPrefs/PlayerPrefBool.cs`
- Create: `Assets/RevCore/Prefs/Runtime/PlayerPrefs/PlayerPrefInt.cs`
- Create: `Assets/RevCore/Prefs/Runtime/PlayerPrefs/PlayerPrefFloat.cs`
- Create: `Assets/RevCore/Prefs/Runtime/PlayerPrefs/PlayerPrefString.cs`
- Test: `Assets/RevCore/Prefs/Tests/Runtime/PlayerPrefTests.cs`

- [ ] **Step 1: Write runtime tests**

Write `Assets/RevCore/Prefs/Tests/Runtime/PlayerPrefTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
    public class PlayerPrefTests
    {
        private const string Prefix = "revcore_prefs_tests_";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(Prefix + "bool");
            PlayerPrefs.DeleteKey(Prefix + "int");
            PlayerPrefs.DeleteKey(Prefix + "float");
            PlayerPrefs.DeleteKey(Prefix + "string");
        }

        [TearDown]
        public void TearDown()
        {
            SetUp();
            PlayerPrefContainer.ClearRegistered();
        }

        [Test]
        public void Bool_pref_saves_changed_value()
        {
            var pref = new PlayerPrefBool(Prefix + "bool");
            pref.Value = true;

            PlayerPrefContainer.SaveChanges();

            Assert.AreEqual(1, PlayerPrefs.GetInt(Prefix + "bool"));
        }

        [Test]
        public void Int_pref_loads_default_and_saves_changed_value()
        {
            var pref = new PlayerPrefInt(Prefix + "int", 3);
            Assert.AreEqual(3, pref.Value);

            pref.Value = 7;
            PlayerPrefContainer.SaveChanges();

            Assert.AreEqual(7, PlayerPrefs.GetInt(Prefix + "int"));
        }

        [Test]
        public void Float_pref_saves_changed_value()
        {
            var pref = new PlayerPrefFloat(Prefix + "float");
            pref.Value = 1.5f;

            PlayerPrefContainer.SaveChanges();

            Assert.AreEqual(1.5f, PlayerPrefs.GetFloat(Prefix + "float"));
        }

        [Test]
        public void String_pref_delete_removes_key()
        {
            var pref = new PlayerPrefString(Prefix + "string");
            pref.Value = "saved";
            pref.SaveChange();

            pref.Delete();

            Assert.IsFalse(PlayerPrefs.HasKey(Prefix + "string"));
        }
    }
}
```

- [ ] **Step 2: Create `IPref`**

```csharp
namespace RevCore
{
    public interface IPref
    {
        string Key { get; }
        bool IsChanged { get; }
        void SaveChange();
        void Delete();
    }
}
```

- [ ] **Step 3: Create runtime container**

```csharp
using System.Collections.Generic;

namespace RevCore
{
    public static class PlayerPrefContainer
    {
        private static readonly List<PlayerPref> s_prefs = new();

        public static void Register(PlayerPref pref)
        {
            for (int i = 0; i < s_prefs.Count; i++)
            {
                if (s_prefs[i].Key == pref.Key)
                {
                    s_prefs[i] = pref;
                    return;
                }
            }

            s_prefs.Add(pref);
        }

        public static void SaveChanges()
        {
            for (int i = 0; i < s_prefs.Count; i++)
                s_prefs[i].SaveChange();
        }

        public static void DeleteAll()
        {
            for (int i = 0; i < s_prefs.Count; i++)
                s_prefs[i].Delete();
        }

        public static void ClearRegistered()
        {
            s_prefs.Clear();
        }
    }
}
```

- [ ] **Step 4: Create base `PlayerPref`**

```csharp
using UnityEngine;

namespace RevCore
{
    public abstract class PlayerPref : IPref
    {
        protected bool changed;

        public string Key { get; }
        public bool IsChanged => changed;

        protected PlayerPref(string key)
        {
            Key = key;
            PlayerPrefContainer.Register(this);
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            changed = false;
        }

        public abstract void SaveChange();
    }
}
```

- [ ] **Step 5: Create primitive wrappers**

Create `PlayerPrefBool`, `PlayerPrefInt`, `PlayerPrefFloat`, `PlayerPrefString` matching RCore behavior with namespace `RevCore`, class names without `R` prefix, `Value` property, `SaveChange()` only writes when changed.

- [ ] **Step 6: Review runtime prefs**

Read runtime files. Verify no `RCore`, no `UnityEditor`, no `Newtonsoft`, no `Encryption`, namespace `RevCore`, tests use unique keys and cleanup.

---

### Task 3: EditorPrefs wrappers

**Files:**
- Create files under `Assets/RevCore/Prefs/Editor/EditorPrefs/`

Implement `EditorPrefContainer`, `EditorPref`, `EditorPrefBool`, `EditorPrefInt`, `EditorPrefFloat`, `EditorPrefString`, `EditorPrefEnum<T>`, `EditorPrefVector` using `UnityEditor.EditorPrefs`. Namespace: `RevCore.Editor`. Container uses `[InitializeOnLoad]` and saves on `EditorApplication.update` and play mode state changed.

- [ ] **Review editor prefs**

Verify all `UnityEditor` usage stays under `Assets/RevCore/Prefs/Editor/`; runtime has no `UnityEditor` refs.

---

### Task 4: README and sample

**Files:**
- Create: `Assets/RevCore/Prefs/README.md`
- Create: `Assets/RevCore/Prefs/Samples~/PrefsSample/PrefsSample.cs`

README must include install, quick start, concepts, API table, migration table, and safety notes. Sample must use `PlayerPrefInt`, `PlayerPrefBool`, and `PlayerPrefContainer.SaveChanges()`.

---

### Task 5: Unity meta files and final review

Generate `.meta` files for every new folder/file under `Assets/RevCore/Prefs/`.

Review:
- all `.cs.meta` contain `MonoImporter:`
- no `.cs.meta` contain `DefaultImporter:`
- runtime has no `UnityEditor`, `RCore`, `Newtonsoft`, `Encryption`
- editor `UnityEditor` refs only under Editor folder
- no changes under `Assets/RCore/` from this work
- tests written but not claimed passing unless Unity actually ran

---

## Verification

Run after implementation:

```bash
git status --short
```

Expected:
- new `Assets/RevCore/Prefs/` files
- new `docs/superpowers/plans/2026-05-13-revcore-prefs.md`
- unrelated existing dirty files remain unstaged

If Unity command line is available, run EditMode tests for:

```text
RevCore.Prefs.Tests
```

If Unity is not run, report that tests are written but not executed in Unity.
