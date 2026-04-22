---
name: writing-plans
description: Use when planning implementation of a feature, refactoring, or multi-step change — creates bite-sized tasks with exact file paths, code, and verification steps
---

# Writing Plans

> Adapted from [superpowers:writing-plans](https://github.com/obra/superpowers)

## Overview

Write comprehensive implementation plans assuming the engineer has zero context. Document everything: which files to touch, exact code, how to verify. Plans are bite-sized tasks (2-5 minutes each).

## Principles

- **DRY** — Don't Repeat Yourself
- **YAGNI** — You Aren't Gonna Need It
- **Frequent commits** — Commit after each logical step

## Save Plans To

```
.agents/memory/plans/YYYY-MM-DD-<feature-name>.md
```

## Plan Document Header

Every plan MUST start with:

```markdown
# [Feature Name] Implementation Plan

**Goal:** [One sentence — what this builds]

**Package:** [Which package this affects — e.g. RCore Main, Services/Ads, Sub]

**Architecture:** [2-3 sentences about approach]

**Files affected:**
- Create: `Assets/RCore/<Package>/Runtime/exact/path.cs`
- Modify: `Assets/RCore/<Package>/Editor/exact/path.cs`

---
```

## Task Structure

Each task should be self-contained:

````markdown
### Task N: [Component Name]

**Files:**
- Create: `Assets/RCore/Main/Runtime/UI/NewComponent.cs`
- Modify: `Assets/RCore/Main/Runtime/Data/JObjectDB/JObjectModelCollection.cs`

**Step 1: Create the data model**

```csharp
[Serializable]
public class NewData : JObjectData
{
    public int value;
}
```

**Step 2: Create the ScriptableObject model**

```csharp
public class NewModel : JObjectModel<NewData>
{
    protected override void OnPostLoad(bool pIsNewSave) { }
}
```

**Verify:**
1. Enter Play Mode — no console errors
2. Open JObjectDB Editor — new model data appears
3. Modify value → exit Play Mode → re-enter → value persists
````

## Scope Check

If the feature spans multiple independent subsystems, break into separate plans. Each plan should produce working, testable software on its own.

## RCore Package Structure

This project contains multiple Unity packages. Always specify which package is affected:

| Package | Path | Content |
|---------|------|---------|
| **RCore Main** | `Assets/RCore/Main/` | Core systems, UI, data, helpers, editor tools |
| **Ads** | `Assets/RCore/Services/Ads/` | Ad provider integrations |
| **Firebase** | `Assets/RCore/Services/Firebase/` | Firebase services |
| **Game Services** | `Assets/RCore/Services/GameServices/` | Cloud Save, In-App Review/Update |
| **IAP** | `Assets/RCore/Services/IAP/` | In-App Purchases |
| **Notification** | `Assets/RCore/Services/Notification/` | Local notifications |
| **Sub** | `Assets/RCore/Sub/` | Supplementary utilities |

Each package has its own `Runtime/`, `Editor/`, `CHANGELOG.md`, and `package.json`.

## RCore Conventions

- **Runtime code** → `<Package>/Runtime/` — organized by system (Data, UI, Common)
- **Editor code** → `<Package>/Editor/` — mirrors Runtime structure
- **Samples** → `<Package>/Samples~/` — demonstrates patterns for users (hidden in UPM)
- **Data models**: `JObjectData` for serializable data, `JObjectModel<T>` for business logic
- **UI panels**: Extend `PanelController`, push via `PanelStack` methods
- **Inspector attributes**: Custom drawers in `Editor/`, attributes in `Runtime/`
- **Dependencies**: Use `[Inject]` for cross-model wiring, register in `ModelCollection.Load()`
