# Changelog — RevCore.Data

All notable changes to this package.

## [0.1.0] — 2026-05-13

### Added
- `JObjectDB` static registry with `CreateCollection`, `Save`, `Reload`, `Delete`, `DeleteAll`, `Import`, `Backup`, `Restore`
- `JObjectData` base class — PlayerPrefs backend, `JsonUtility` default serialization, Newtonsoft.Json minimized mode
- `JObjectModel<T>` ScriptableObject base with lifecycle hooks
- `JObjectModelCollection` root aggregator with `[Inject]` dependency injection and `Get<T>()` resolver
- `JObjectDBManager<T>` MonoBehaviour lifecycle driver — delayed save, auto-save on pause/quit
- `SessionModel` + `SessionData` — session counting, daily streak, offline time, `NewDayStartedEvent`
- `InjectAttribute` for cross-model field injection
- Editor inspectors for collection and manager with backup/restore buttons
