# ServiceLocator Sample

Demonstrates `Services.Register<T>` and `Services.TryGet<T>` pattern.

- `IAudioService` — service contract.
- `SimpleAudioService` — concrete implementation using `Debug.Log`.
- `SampleBootstrap` — registers services in `Awake`, clears in `OnDestroy`.
- `SampleConsumer` — retrieves service via `Services.TryGet<IAudioService>`.

## Usage

1. Add `SampleBootstrap` to a GameObject in the scene.
2. Add `SampleConsumer` to another GameObject.
3. Press Play — console shows `[AudioService] Play: click`.
