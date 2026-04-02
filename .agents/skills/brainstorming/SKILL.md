---
name: brainstorming
description: "You MUST use this before any creative work - creating features, building components, adding functionality, or modifying behavior. Explores user intent, requirements and design before implementation."
---

# Brainstorming Ideas Into Designs

Help turn ideas into fully formed designs and specs through natural collaborative dialogue.

Start by understanding the current project context, then ask questions one at a time to refine the idea. Once you understand what you're building, present the design and get user approval.

<HARD-GATE>
Do NOT write any code, scaffold any project, or take any implementation action until you have presented a design and the user has approved it. This applies to EVERY project regardless of perceived simplicity.
</HARD-GATE>

## Anti-Pattern: "This Is Too Simple To Need A Design"

Every project goes through this process. A UI popup, a config change, a new API integration — all of them. "Simple" projects are where unexamined assumptions cause the most wasted work. The design can be short (a few sentences for truly simple projects), but you MUST present it and get approval.

## Checklist

You MUST complete these steps in order:

1. **Explore project context** — check relevant files, existing patterns, related systems
2. **Ask clarifying questions** — one at a time, understand purpose/constraints/success criteria
3. **Propose 2-3 approaches** — with trade-offs and your recommendation
4. **Present design** — in sections scaled to complexity, get user approval after each section
5. **Write design doc** — save to `implementation_plan.md` artifact
6. **User reviews plan** — ask user to review before proceeding
7. **Transition to implementation** — begin coding only after approval

## The Process

**Understanding the idea:**

- Check out the current project state first (files, existing patterns, related systems)
- Before asking detailed questions, assess scope: if the request describes multiple independent subsystems, flag this immediately
- If the project is too large for a single spec, help the user decompose into sub-projects
- Ask questions one at a time to refine the idea
- Prefer multiple choice questions when possible, but open-ended is fine too
- Only one question per message
- Focus on understanding: purpose, constraints, success criteria

**Exploring approaches:**

- Propose 2-3 different approaches with trade-offs
- Present options conversationally with your recommendation and reasoning
- Lead with your recommended option and explain why

**Presenting the design:**

- Once you believe you understand what you're building, present the design
- Scale each section to its complexity: a few sentences if straightforward, up to 200-300 words if nuanced
- Ask after each section whether it looks right so far
- Cover: architecture, components, data flow, error handling
- Be ready to go back and clarify if something doesn't make sense

**Design for isolation and clarity:**

- Break the system into smaller units that each have one clear purpose
- Communicate through well-defined interfaces, testable independently
- Can someone understand what a unit does without reading its internals?
- Smaller, well-bounded units are easier to work with

**Working in existing codebases (critical for this Unity project):**

- Explore the current structure before proposing changes. **Follow existing patterns.**
- Where existing code has problems that affect the work, include targeted improvements as part of the design
- Don't propose unrelated refactoring. Stay focused on what serves the current goal.

## Unity/Goods-Jam Specific Considerations

When brainstorming for this project, always consider:

- **Existing architecture patterns**: Check `_LiveOps/Scripts/` for established patterns (Data/Models, UI, Config, Server)
- **iGameService integration**: Check `.agents/workflows/` for existing API integration patterns
- **Package separation**: Features shared across games go in `Packages/com.zego.game-server`, game-specific code goes in `Assets/_LiveOps/`
- **UI patterns**: Follow existing Popup/Panel/View naming conventions
- **Data models**: Follow existing patterns in `Data/Models/`

## After the Design

**Documentation:**

- Write the validated design to `implementation_plan.md` artifact
- Use the standard implementation plan format with Proposed Changes, Verification Plan sections

**Implementation:**

- Begin implementation only after user approval
- Follow DRY and YAGNI principles

## Key Principles

- **One question at a time** — Don't overwhelm with multiple questions
- **Multiple choice preferred** — Easier to answer than open-ended when possible
- **YAGNI ruthlessly** — Remove unnecessary features from all designs
- **Explore alternatives** — Always propose 2-3 approaches before settling
- **Incremental validation** — Present design, get approval before moving on
- **Be flexible** — Go back and clarify when something doesn't make sense
