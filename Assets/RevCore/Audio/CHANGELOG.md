# Changelog

## [Unreleased]

## [1.1.0] - 2026-05-19

### Added

- `AudioAsyncExtensions.FadeMusicAsync` — awaitable music-volume fade.
- `AudioAsyncExtensions.FadeOutMusicAsync` — awaitable fade-to-zero + stop.
- Hard dependency on `com.cysharp.unitask` declared in `package.json`.

## [0.5.0] - 2026-05-17

### Fixed

- AudioCollection lookups no longer throw when clip arrays are unassigned.
- BaseAudioManager initializes AudioSources at runtime when added dynamically.
- BaseAudioManager collection-backed playback methods no-op when AudioCollection is missing.
- AudioManager clears singleton reference on destroy.
- SfxSource handles missing AudioManager or AudioCollection gracefully.
- AudioCollectionEditor sort/generate handles null or empty clip arrays.
- Addressables load/unload methods guard null arrays.

### Added

- Tests for BaseAudioManager runtime initialization, AudioManager singleton cleanup, SfxSource missing-manager path.

## [1.0.0] - 2026-05-13

### Added

- AudioCollection — ScriptableObject clip repository with Addressables support (`#if ADDRESSABLES`)
- BaseAudioManager — Volume control, SFX pooling, DOTween/coroutine fading
- AudioManager — Singleton with UISfxTriggeredEvent integration via Events bus
- SfxSource — Per-object SFX component (managed or standalone mode)
- AudioCollectionEditor — Sort, generate AudioIDs.cs, validate Addressable sounds
- BaseAudioManagerEditor — Play/Stop music and SFX inspector buttons
- SfxSourceEditor — Clip search picker from AudioCollection
- Unit tests for AudioCollection
- Sample scene code
