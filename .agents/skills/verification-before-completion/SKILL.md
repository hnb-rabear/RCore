---
name: verification-before-completion
description: Use when about to claim work is complete, fixed, or passing — requires running actual verification commands and confirming output before making any success claims
---

# Verification Before Completion

> Adapted from [superpowers:verification-before-completion](https://github.com/obra/superpowers)

## The Iron Law

```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
```

If you haven't run the verification command **in this message**, you cannot claim it passes.

## The Gate Function

Before claiming ANY status:

1. **IDENTIFY** — What proves this claim?
2. **RUN** — Execute the verification (fresh, complete)
3. **READ** — Full output, check for errors/warnings
4. **VERIFY** — Does output confirm the claim?
   - NO → State actual status with evidence
   - YES → State claim WITH evidence
5. **ONLY THEN** — Make the claim

## Unity/RCore Verification Checklist

### C# Code Changes
1. Check for compilation errors — no red errors in Unity Console
2. Verify no new warnings introduced
3. If ScriptableObject changed — check Inspector shows correct fields
4. If [Inject] dependency added — verify registration order in ModelCollection

### Runtime Behavior Changes
1. Enter Play Mode — no errors on startup
2. Test the specific feature — functionality works as expected
3. Check Unity Console — no runtime exceptions or warnings
4. If data persistence changed — verify save/load cycle works

### Editor Tool Changes
1. Open the editor window — renders without errors
2. Test all interactive elements — buttons, fields, dropdowns work
3. Verify Undo support if applicable

### UI Changes
1. Enter Play Mode — panel shows/hides correctly
2. Check PanelStack state — correct push/pop behavior
3. Test on different resolutions if layout-sensitive
4. Verify safe area behavior if applicable

## Red Flags — STOP

| Thought | Reality |
|---------|---------|
| "Should work now" | RUN the verification |
| "I'm confident" | Confidence ≠ evidence |
| "No compiler errors" | Compilation ≠ runtime correctness |
| "Just this once" | No exceptions |
| "The code looks correct" | Reading ≠ running |
| "Great, that's done!" | Did you actually verify? |
