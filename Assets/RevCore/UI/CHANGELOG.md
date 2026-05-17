# Changelog

## [Unreleased]

### Removed

- `CustomToggleSlider` and `CustomToggleSliderEditor` deleted (had been `[Obsolete]` Stage 1). Use `JustToggle` instead — same `Toggle.isOn` API; richer transitions.
- `SimpleButton` and `SimpleButtonEditor` deleted (had been `[Obsolete]` Stage 1). Use `SimpleTMPButton` instead — same `JustButton` lineage; label uses TextMeshPro rather than the legacy `UnityEngine.UI.Text`.

## 1.0.0

### Added

- Panel navigation: `PanelStack`, `PanelController`, `PanelRoot` with stack-based push/pop, back handling, dimmer overlay, and Foundation event routing.
- Buttons: `JustButton`, `SimpleButton`, `SimpleTMPButton` with scale bounce, DOTween optional transitions, and SFX event integration. (`SimpleButton` `[Obsolete]` from this release; removed in Unreleased.)
- Toggles: `JustToggle`, `CustomToggleGroup`, `CustomToggleSlider` (the latter `[Obsolete]` from this release; removed in Unreleased).
- Scroll views: `OptimizedScrollView`, `OptimizedHorizontalScrollView`, `OptimizedVerticalScrollView`, `HorizontalSnapScrollView`, `SnapScrollItem`, `ScrollRectEx`.
- Layout: `HorizontalAlignmentUI`, `VerticalAlignmentUI`, `TableAlignmentUI`, `UICircleArranger`.
- Safe area: `ScreenSafeArea`, `IgnoreScreenSafe`.
- Tutorial focus mask: `HoledLayerMask` with hole-overlay focus, DOTween animated zoom, and sprite mask support.
- Image helpers: `ImageWithText`, `ImageWithTextTMP`, `ProgressBar`.
- Joystick: `Joystick`, `JoystickArea`.
- Custom editors: `PanelStackEditor`, `PanelControllerEditor`, `PanelRootEditor`, `ProgressBarEditor`, `JustButtonEditor`, `SimpleTMPButtonEditor`, `JustToggleEditor`, `HoledLayerMaskEditor`.
- Tests: `PanelControllerTests`, `PanelRootTests`, `OptimizedScrollItemTests`, `JustButtonTests`, `HoledLayerMaskTests`.
- Sample: `UISample` with `SamplePanelRoot` and `SampleScrollItem`.

### Changed

- `PanelRoot` routing uses `Type` internally instead of string `FullName` and reflection. Override `OnReceivedPanelRequest(Type, object)` and optional `OnResolvePanelByType(Type)`.
- `JustButton` greyscale uses explicit serialized material instead of hidden `Resources.Load<Material>("Greyscale")`.
- Removed unused Timer and Pool package dependencies.
- Removed `OptimizedScrollItemTest` from runtime assembly (duplicate of sample).
