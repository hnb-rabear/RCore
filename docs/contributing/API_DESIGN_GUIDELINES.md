## RevCore API Design Guidelines

These are non-negotiable rules for any code under `Assets/RevCore/`. Internal-only code (under `Internal/` namespaces or marked `internal`) has more freedom but should still follow the spirit.

### Naming

- Public types and members: `PascalCase`.
- Method parameters and locals: `camelCase`.
- Private instance fields: `m_camelCase` (matches existing RevCore convention).
- Private static fields: `s_camelCase`.
- Constants: `PascalCase`. (We do not use `SCREAMING_SNAKE_CASE`.)
- Interfaces: `IPascalCase`. Generic type parameters: `T`, `TKey`, `TValue`, never `T1`/`T2`.
- Async methods returning `Task`/`UniTask`/`Awaitable`: suffix `Async`.
- Boolean properties: `IsX`, `HasX`, `CanX`. Avoid `Get` prefix on properties.
- Event names: present tense for "about to happen" (`Closing`), past tense for "already happened" (`Closed`).
- Verb prefixes for methods: `Get`/`Set`/`Try`/`Create`/`Build`/`Find`. `Try*` returns `bool` and uses `out`. `Find*` may return null. `Get*` never returns null — throw if not found.

### Nullability

- All public method parameters, return values, and fields are annotated. Use `?` for nullable.
- Argument validation at boundaries: `ThrowHelper.ArgumentNull(arg, nameof(arg))` or `ArgumentNullException`. Document non-null assumptions in XML doc.
- Internal code may assume non-null without checking; trust the boundary.

### Error model

- Recoverable failure (caller is expected to handle): return `Result<T>` or `bool` + `out`.
- Programmer error (precondition violated): throw `ArgumentException` / `InvalidOperationException`. Do not catch.
- Unexpected runtime failure: log + raise diagnostic event (when `IRevDiagnostics` is wired up in Phase 7). Do not silently swallow.
- Never return sentinel values like `Color.clear` or `-1` from a parsing method. Use `TryParse` pattern.

### Threading model

- Default: **Unity main thread only**. Document this in XML doc with `<remarks>Main thread only.</remarks>`.
- Thread-safe types must say so explicitly and have a `[ThreadSafe]` attribute (to be defined in Foundation).
- Never block the main thread on `Wait()`, `Result`, or `.GetAwaiter().GetResult()` for async work. Use UniTask/Awaitable.
- Coroutines may not call thread-unsafe code post-`yield`.

### Allocation

- Hot paths must be zero-alloc. CI benchmark fails regressions.
- Provide buffer-overload variants for queries: `CopyTo(List<T> buffer)` over `GetList()` returning a new list.
- Boxing of value types in `object`/`IEquatable` paths is a regression.

### Lifecycle

- `MonoBehaviour`-derived public types must be safe to enable/disable repeatedly and to put through scene reload.
- Disposable resources (subscriptions, native arrays, tweeners) released in `OnDestroy`/`Dispose`.
- Never call into other RevCore subsystems from `OnDestroy` of one — order is undefined. Use `OnDisable` for unsubscription if cross-system.

### MonoBehaviour vs ScriptableObject vs POCO

- POCO: pure logic, no Unity dependency. Test-friendly. Prefer this.
- ScriptableObject: configuration or shared, designer-edited data. Not for runtime state that resets per scene.
- MonoBehaviour: needs Unity callbacks (Update/OnEnable/etc.) or scene/inspector binding. Last resort, not default.

### Public surface

- Anything `public` is contract. If you would not document it for a stranger, mark it `internal` or `private`.
- `[InternalsVisibleTo("RevCore.<Module>.Tests")]` to expose internals for tests.
- Do not expose mutable static state. Use a service registered through Foundation.

### XML doc requirements

- Every `public` type, method, property, event has `<summary>`.
- Every parameter has `<param>`. Every return has `<returns>`. Exceptions have `<exception>`.
- Non-trivial behavior (throttling, caching, retries, side effects) goes in `<remarks>`.
- Code examples for entry-point types: `<example><code>...</code></example>`.

### Obsolete

- See [DEPRECATION_POLICY.md](DEPRECATION_POLICY.md). TL;DR: `[Obsolete("Use X instead. Removed in v2.0.")]` for ≥1 minor, then `error: true` for ≥1 minor, then delete.

### Editor-only code

- Wrap with `#if UNITY_EDITOR` only in shared asmdefs; prefer placing in a separate `Editor/` asmdef.
- Never reference `UnityEditor.*` from a runtime asmdef.
