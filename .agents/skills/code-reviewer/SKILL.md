---
name: code-reviewer
description: "Code review expert for Unity C# projects — focuses on performance, serialization, lifecycle correctness, and maintainability."
---

# Code Reviewer

## Use this skill when
- Reviewing code changes before commit/push
- Checking for Unity-specific pitfalls
- Evaluating architecture and design decisions

## Instructions

1. Analyze code context and identify review scope
2. Check for Unity-specific issues (see checklist below)
3. Evaluate performance and GC implications
4. Review architecture consistency with existing RCore patterns
5. Provide structured feedback organized by severity

## Unity/RCore Review Checklist

### Serialization & Data
- [ ] `[SerializeField]` on private fields that need Inspector exposure
- [ ] No `[Inject]` fields left unresolved — check registration order in ModelCollection
- [ ] JObjectData changes are backward-compatible with existing PlayerPrefs saves
- [ ] ScriptableObject references properly handled (not null in builds)

### Performance & GC
- [ ] No LINQ/lambda allocations in Update/LateUpdate/FixedUpdate
- [ ] No `string.Format` or string concatenation in hot paths
- [ ] `GetComponent` calls cached, not called per-frame
- [ ] Object pooling used for frequently instantiated objects
- [ ] No boxing of value types in collections

### MonoBehaviour Lifecycle
- [ ] Coroutines properly stopped on disable/destroy
- [ ] No race conditions between Awake/OnEnable/Start across scripts
- [ ] `OnDestroy` cleans up event subscriptions
- [ ] `OnApplicationPause`/`OnApplicationQuit` handled for data persistence

### Editor vs Runtime
- [ ] `#if UNITY_EDITOR` guards on editor-only code
- [ ] No `AssetDatabase` calls in runtime code
- [ ] Custom editors use `EditorGUI.BeginChangeCheck` / `EndChangeCheck`
- [ ] `OnValidate` doesn't run expensive operations

### IL2CPP & Build
- [ ] No reflection in hot paths (IL2CPP strips unused types)
- [ ] Generic virtual methods preserved if needed
- [ ] Platform-specific code properly guarded (`#if UNITY_ANDROID`, etc.)

### Code Quality
- [ ] Clean Code / SOLID principles followed
- [ ] Consistent naming conventions (m_ prefix for private fields)
- [ ] XML doc comments on public APIs
- [ ] No magic numbers — use constants or SerializeField
