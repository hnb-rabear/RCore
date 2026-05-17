# RevCore.UI

Runtime UI components and helpers for RevCore Unity projects.

## Dependencies

- RevCore.Foundation
- RevCore.Inspector
- TextMeshPro

## Optional integrations

- DOTween via `DOTWEEN` define for tweened button, scroll, layout, and mask transitions.

## Components

### Panel navigation

- `PanelStack` — stack-based push/pop panel controller.
- `PanelController` — show/hide lifecycle, back handling, nested panel support.
- `PanelRoot` — root-level panel routing, queueing, dimmer overlay, and Foundation event integration.

`PanelRoot` uses type-based routing internally. Override `OnReceivedPanelRequest(Type panelType, object value)` for explicit panel request handling, and `OnResolvePanelByType(Type panelType)` for internal panel lookup.

### Buttons and toggles

- `JustButton`, `SimpleButton`, `SimpleTMPButton`
- `JustToggle`, `CustomToggleGroup`

Button SFX emits `UISfxTriggeredEvent` through RevCore.Foundation events. Greyscale state uses an explicit serialized material; RevCore.UI does not require a magic `Resources/Greyscale` asset.

### Scroll views

- `OptimizedScrollItem`
- `OptimizedScrollView`
- `OptimizedHorizontalScrollView`
- `OptimizedVerticalScrollView`
- `HorizontalSnapScrollView`
- `SnapScrollItem`
- `ScrollRectEx`

### Layout and safe area

- `HorizontalAlignmentUI`
- `VerticalAlignmentUI`
- `TableAlignmentUI`
- `UICircleArranger`
- `ScreenSafeArea`
- `IgnoreScreenSafe`

World-space `HorizontalAlignment`, `VerticalAlignment`, and `TableAlignment` remain in RCore and are not part of RevCore.UI.

### Tutorial focus mask

- `HoledLayerMask` — blocks interaction outside a focused UI or sprite target, with optional DOTween focus animation.

## Samples

Import `Samples~/UISample` for basic `PanelRoot` and optimized scroll item examples.
