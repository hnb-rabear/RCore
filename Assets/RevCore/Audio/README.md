# RevCore.Audio

Audio management for Unity — clip repository, SFX pooling, music playback with volume fading.

## Features

- **AudioCollection** — ScriptableObject clip repository with optional Addressables support
- **BaseAudioManager** — Volume control (master/sfx/music), SFX source pooling, DOTween or coroutine fading
- **AudioManager** — Singleton with event-driven UI SFX via `UISfxTriggeredEvent`
- **SfxSource** — Per-object SFX component, managed or standalone mode
- **Editor tools** — Audio ID generator, Addressable validation, play/stop inspector buttons, clip search picker

## Optional Dependencies

- **Addressables** (`com.unity.addressables`) — Enables `abSfxClips`/`abMusicClips` fields and async load/unload. Auto-detected via `ADDRESSABLES` define.
- **DOTween** (`com.demigiant.dotween`) — Enables smooth volume fading via tweeners. Falls back to coroutine lerp. Auto-detected via `DOTWEEN` define.

## Quick Start

1. Create AudioCollection: `Create > RevCore > Audio Collection`
2. Add AudioManager to scene (auto-creates Music and Sfx child AudioSources)
3. Assign AudioCollection to AudioManager
4. Play: `AudioManager.Instance.PlaySFX("clip_name")` or `AudioManager.Instance.PlayMusicById(0)`
5. UI SFX via events: `Events.Publish(new UISfxTriggeredEvent("button_click"))`

## Runtime Safety

AudioManager initializes required AudioSources at runtime when added dynamically instead of prepared by editor validation. Missing AudioCollection or missing clips are treated as no-op playback requests.

AudioCollection lookup methods return null or empty arrays when clip arrays are unassigned.

## Dependencies

- RevCore.Foundation (Events, Log)
- RevCore.Inspector (AutoFill attribute)
- RevCore.Prefs (EditorPrefString — editor only)
