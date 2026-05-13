# RevCore Master Roadmap

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement each package plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build RevCore as the safe, modular successor to RCore: independent Unity packages, low learning curve, clear docs/diagrams/flows, less MonoBehaviour dependence, and safe reuse of stable RCore ideas.

**Architecture:** RevCore is built as additive packages under `Assets/RevCore/`. Existing `Assets/RCore/` code remains untouched unless a plan explicitly adds a bridge adapter. Each scope owns one package, one namespace, one README, and one set of tests/samples.

**Tech Stack:** Unity 2022.3, UPM packages, C# asmdefs, EditMode/PlayMode tests, Markdown docs, Mermaid diagrams.

---

## Package order

| Phase | Package | Risk | Complexity | Reason |
|---|---|---:|---:|---|
| P0 | `com.rabear.revcore.foundation` | Low | Medium | Zero deps. Contracts, event bus, logger, results, core helpers. All other scopes depend on this. |
| P0 | `com.rabear.revcore.inspector` | Low | Low | Zero deps. Low learning curve. Immediate value. Safe to port from RCore. |
| P1 | `com.rabear.revcore.timer` | Low | Medium | Foundation-only dep. Replaces hidden MonoBehaviour timer coupling with service API. |
| P1 | `com.rabear.revcore.pool` | Low | Medium | Depends on Foundation, Timer optional. Reuses RCore pool idea safely. |
| P1 | `com.rabear.revcore.prefs` | Medium | Medium | Typed prefs + obfuscation/integrity. Must avoid security confusion. |
| P2 | `com.rabear.revcore.data` | Medium | High | Rebuild JObjectDB V2 concept with storage abstraction and migration. |
| P2 | `com.rabear.revcore.ui` | Medium | High | Typed panel navigation, back flow, safe area. Hardest behavior risk. |
| P3 | `com.rabear.revcore.audio` | Low | Medium | Audio service interface + RCore-inspired manager. |
| P3 | `com.rabear.revcore.services.*` | Medium | High | Ads/IAP/Firebase/Notification contracts and mock providers. |
| P3 | `com.rabear.revcore.editor-tools` | Low | High | Wizard, package health dashboard, validators, docs launcher. |

## Safety rules

1. Do not move, rename, or modify existing serialized RCore MonoBehaviours/ScriptableObjects.
2. New RevCore code uses namespace `RevCore` only.
3. Each package has independent `package.json`, runtime asmdef, editor asmdef when needed, README, CHANGELOG.
4. Package dependencies only point downward in the order above.
5. RCore reuse happens by copying stable ideas or adding optional bridge adapters. RevCore core packages do not require RCore.
6. Every package must include: Quick Start, API contracts, Mermaid flow diagram, risk notes, samples.
7. Every implementation task includes verification before claiming done.

## Documentation standard per package

Each package README must contain:

```markdown
# RevCore.<Scope>

## Install
## 60-second Quick Start
## Concepts
## API Reference
## Flow Diagram
## Common Use Cases
## Extension Points
## Migration from RCore
## Troubleshooting
```

## Next plans

- `docs/superpowers/plans/2026-05-13-revcore-foundation.md`
- `docs/superpowers/plans/2026-05-13-revcore-inspector.md`
- `docs/superpowers/plans/2026-05-13-revcore-timer.md`
- `docs/superpowers/plans/2026-05-13-revcore-pool.md`
- `docs/superpowers/plans/2026-05-13-revcore-prefs.md`
- `docs/superpowers/plans/2026-05-13-revcore-data.md`
- `docs/superpowers/plans/2026-05-13-revcore-ui.md`
- `docs/superpowers/plans/2026-05-13-revcore-audio.md`
