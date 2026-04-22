---
name: systematic-debugging
description: Use when debugging any issue, fixing a bug, investigating unexpected behavior, or when something isn't working as expected — enforces 4-phase root cause process instead of guessing
---

# Systematic Debugging

> Adapted from [superpowers:systematic-debugging](https://github.com/obra/superpowers)

## The Iron Law

```
NEVER guess at fixes. Find the root cause FIRST.
```

Surface symptoms ≠ root cause. Resist the urge to change code before understanding WHY it's broken.

## Phase 1: Root Cause Investigation

1. **Reproduce the bug** — Get exact steps, inputs, and environment (Editor version, platform, device)
2. **State clearly**: What SHOULD happen vs what ACTUALLY happens
3. **Gather evidence**:
   - Unity Console logs (check for warnings too, not just errors)
   - Unity Profiler for performance/GC issues
   - JObjectDB Editor window for data state inspection
   - Inspector for serialized field values at runtime
   - Device logs for mobile (`adb logcat` for Android, Xcode console for iOS)
4. **Isolate**: Is it Editor-only, runtime-only, platform-specific, or data-dependent?

## Phase 2: Pattern Analysis

Ask yourself:
- Has this happened before? Check `CHANGELOG.md` for similar fixes
- Is it timing-related? (Awake vs Start ordering, coroutine lifecycle, async race conditions)
- Is it state-related? (stale PlayerPrefs, missing ScriptableObject reference, uninitialized [Inject] field)
- Is it platform-related? (IL2CPP stripping, Android back button, iOS safe area, Device Simulator)
- Does it only happen with certain data? (empty collections, null JObjectData fields, edge case enum values)

## Phase 3: Hypothesis & Testing

1. Form ONE hypothesis at a time
2. Design a test that would DISPROVE it
3. Run the test
4. If disproved → next hypothesis. If confirmed → proceed to fix

**NEVER** test multiple hypotheses simultaneously — you won't know which one was right.

## Phase 4: Implementation

1. Write the minimal fix
2. Verify the original bug is resolved
3. Check for regressions — does the fix break anything else?
4. Check related code paths — could the same bug exist elsewhere?

## Red Flags — STOP

| Thought | Reality |
|---------|---------|
| "Let me just try changing this..." | Find root cause first |
| "It's probably X..." | Prove it, don't guess |
| "I fixed it, moving on" | Did you verify? Run the reproduce steps again |
| "Let me change multiple things" | One change per test |
| "I've seen this before" | Every bug is unique until proven otherwise |
