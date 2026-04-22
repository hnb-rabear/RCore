---
name: brainstorming
description: "MANDATORY — STOP and activate BEFORE writing ANY code for new features, significant changes, or multi-file modifications. You MUST ask clarifying questions, explore alternatives, and get explicit design approval first. Do NOT skip this step. Do NOT start coding without approval."
---

# Brainstorming Ideas Into Designs

> Adapted from [superpowers:brainstorming](https://github.com/obra/superpowers)

## The Iron Rule

```
NEVER start coding before the design is approved.
```

Even "simple" changes benefit from 2 minutes of design thinking.

## Anti-Pattern: "This Is Too Simple To Need A Design"

If you're thinking this, you're wrong. Every feature that touches multiple files, modifies data flow, or affects UX deserves a brief design.

## The Process

### 1. Understand the Request
- What problem does this solve?
- Who needs it? (game developers using RCore framework)
- What's the current workaround?

### 2. Ask Clarifying Questions
- Don't assume. Ask about edge cases, scope, and priorities
- Example: "Should this work in both Editor and Runtime?"
- Example: "Does this need to serialize with PlayerPrefs or binary?"

### 3. Explore Alternatives
- Present 2-3 approaches with trade-offs
- Consider: complexity, performance, future extensibility
- Reference existing patterns in RCore

### 4. Present Design in Digestible Chunks
Break the design into sections short enough to actually read:
- **Data model changes** (if any) — new JObjectData fields, ScriptableObject structure
- **Runtime changes** — new MonoBehaviour, modified system lifecycle
- **Editor changes** — new inspector drawers, editor windows, custom attributes
- **Integration points** — how it connects to existing RCore systems

### 5. Validate
- Get explicit approval before proceeding
- If the user has concerns, iterate on the design

## After the Design

1. Save design document to `.agents/memory/plans/YYYY-MM-DD-<feature>-design.md`
2. Use the writing-plans skill to create an implementation plan
3. Execute the plan

## RCore Design Checklist

- [ ] Does this affect JObjectDB serialization? → Plan data migration strategy
- [ ] Does this add a new ScriptableObject? → Consider [CreateScriptableObject, AutoFill] attributes
- [ ] Does this add a new UI component? → Follow PanelStack patterns, check nested Canvas
- [ ] Does this need Inspector support? → Plan custom drawer or attribute
- [ ] Does it affect existing systems? → Review README.md for dependencies
- [ ] Does it allocate per-frame? → Profile GC impact, avoid boxing/LINQ in hot paths
- [ ] Does it need to work with IL2CPP? → Avoid reflection in hot paths, check AOT compatibility
- [ ] Is it Editor-only or Runtime? → Proper #if UNITY_EDITOR guards

## Red Flags

| Thought | Reality |
|---------|---------|
| "This is just a simple change" | Simple changes break complex systems. Design first. |
| "I already know what to do" | Knowing ≠ designing. Write it down. |
| "The user just wants it done fast" | Fast without design = rework later. |
| "Let me just start coding" | STOP. Design first, always. |
