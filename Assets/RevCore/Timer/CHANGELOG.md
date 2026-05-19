# Changelog

## [Unreleased]

## [1.1.0] - 2026-05-19

### Added

- `Timers.DelayAsync(float, bool, CancellationToken)` — awaitable equivalent of `WaitForSeconds`.
- `Timers.WaitForConditionAsync(Func<bool>, CancellationToken)` — awaitable equivalent of `WaitForCondition`.
- `Timers.WaitForFramesAsync(int, CancellationToken)` — awaitable that returns after N scheduler Ticks.
- Hard dependency on `com.cysharp.unitask` declared in `package.json`.

## [1.0.0] - 2026-05-13

### Added
- Package scaffold
- Manual timer scheduler
- TimedAction and countdown timers
- Condition timers
- Debounced RevCore event publishing
- Main-thread action queue
- Optional global and scene Unity drivers
- Runtime tests
- README and sample
