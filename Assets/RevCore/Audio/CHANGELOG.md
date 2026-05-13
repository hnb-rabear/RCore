# Changelog

## [1.0.0] - 2026-05-13

### Added
- AudioCollection — ScriptableObject clip repository with Addressables support (#if ADDRESSABLES)
- BaseAudioManager — Volume control, SFX pooling, DOTween/coroutine fading
- AudioManager — Singleton with UISfxTriggeredEvent integration via Events bus
- SfxSource — Per-object SFX component (managed or standalone mode)
- AudioCollectionEditor — Sort, generate AudioIDs.cs, validate Addressable sounds
- BaseAudioManagerEditor — Play/Stop music and SFX inspector buttons
- SfxSourceEditor — Clip search picker from AudioCollection
- Unit tests for AudioCollection
- Sample scene code
