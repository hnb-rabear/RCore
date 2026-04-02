---
name: writing-plans
description: "Write comprehensive implementation plans with bite-sized tasks. Use after brainstorming to create detailed step-by-step plans before coding."
---

# Writing Plans

## Overview

Write comprehensive implementation plans assuming the engineer has zero context for the codebase. Document everything they need to know: which files to touch for each task, complete code, how to verify it. Give them the whole plan as bite-sized tasks. DRY. YAGNI. Frequent commits.

**Announce at start:** "I'm using the writing-plans skill to create the implementation plan."

**Save plans to:** `implementation_plan.md` artifact (following Antigravity's standard artifact format)

## Scope Check

If the spec covers multiple independent subsystems, suggest breaking this into separate plans — one per subsystem. Each plan should produce working, testable software on its own.

## File Structure

Before defining tasks, map out which files will be created or modified and what each one is responsible for.

- Design units with clear boundaries and well-defined interfaces
- Prefer smaller, focused files over large ones that do too much
- Files that change together should live together
- **In this Unity project, follow established patterns:**
  - API client methods → `Packages/com.zego.game-server/Scripts/Core/GameClientApi.*.cs`
  - Server wrappers → `Packages/com.zego.game-server/Scripts/Simple/GameServer.*.cs`
  - Data models → `Assets/_LiveOps/Scripts/Data/Models/`
  - UI components → `Assets/_LiveOps/Scripts/UI/`
  - Config → `Assets/_LiveOps/Scripts/Config/`

## Bite-Sized Task Granularity

**Each step is one action (2-5 minutes):**
- "Add the data model class" — step
- "Add the API client method" — step
- "Add the server wrapper" — step
- "Add the UI component" — step
- "Verify compilation" — step
- "Commit" — step

## Plan Document Format

Every plan MUST use the standard `implementation_plan.md` format:

```markdown
# [Goal Description]

Brief description of the problem and what the change accomplishes.

## User Review Required

Document anything that requires user review (breaking changes, design decisions).

## Proposed Changes

### [Component Name]

#### [MODIFY/NEW/DELETE] [file basename](file:///absolute/path/to/file)

Description of changes to this file.

## Verification Plan

### How to verify
- Exact steps to verify the changes work
```

## Task Structure

````markdown
### Task N: [Component Name]

**Files:**
- Create: `exact/path/to/file.cs`
- Modify: `exact/path/to/existing.cs`

- [ ] **Step 1: Add the data model**

```csharp
public class ExampleData
{
    public string Id { get; set; }
    public string Name { get; set; }
}
```

- [ ] **Step 2: Add API client method**

```csharp
public async Task<ExampleData> GetExample(string id)
{
    var response = await SendRequest("GET", $"/api/example/{id}");
    return JsonUtility.FromJson<ExampleData>(response);
}
```

- [ ] **Step 3: Verify compilation**

Build the project in Unity and check for errors.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat: add example API integration"
```
````

## Unity/Goods-Jam Specific Guidelines

When writing plans for this project:

1. **Follow package separation**: Shared server code → `com.zego.game-server`, game-specific → `Assets/_LiveOps/`
2. **Check existing workflows**: Look at `.agents/workflows/` for API integration patterns already documented
3. **Data model conventions**: Follow existing patterns in `Data/Models/` — serializable classes, proper field attributes
4. **UI conventions**: Follow existing Popup/Panel/View patterns, use TweenFX for animations
5. **Editor tooling**: Add test entries to `GameServerTesterWindow` when adding new API endpoints
6. **No unit tests** (unless project adds a test framework): Instead, plan manual verification steps and Editor test window entries

## Remember

- Exact file paths always
- Complete code in plan (not "add validation")
- Exact verification steps
- DRY, YAGNI, frequent commits
- Follow existing project patterns — don't invent new conventions

## Execution Handoff

After saving the plan:

**"Plan complete and saved to `implementation_plan.md`. Ready to execute?"**

Wait for user approval before beginning implementation.
