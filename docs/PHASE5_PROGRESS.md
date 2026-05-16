## Phase 5 — Documentation Progress

Phase 5 is the "fill every public XML doc" pass. Tracking against `scripts/xmldoc-baseline.json`.

### Status snapshot

| Module | Documented | Total | Coverage |
|---|---:|---:|---:|
| **Foundation** | 339 | 339 | **100%** ✓ |
| **Pool** | 62 | 62 | **100%** ✓ |
| **Timer** | 24 | 24 | **100%** ✓ |
| **Inspector** | 14 | 14 | **100%** ✓ |
| **Prefs** | 22 | 22 | **100%** ✓ |
| **Audio** | 53 | 53 | **100%** ✓ |
| **Data** | 107 | 107 | **100%** ✓ |
| **UI** | ~0 | 344 | **0%** |
| **Overall** | **621** | **996** | **62.35%** |

### Commits in this phase

1. `docs(foundation): XML doc Helpers — Camera, Collection, String, Time, Color` — 63 members
2. `docs(foundation): XML doc Contracts, Events, Logging, Services, Results` — 66 members
3. `docs(foundation): XML doc Types — BigNumber, SerializableDictionary, PerfectRatio` — 31 members
4. `docs(foundation): XML doc Helpers — Component, Transform, Math` — 150 members
5. `docs: XML doc Pool, Timer, Inspector, Prefs modules` — 122 members
6. `docs(audio): XML doc AudioCollection, AudioManager, SfxSource, BaseAudioManager` — 51 members
7. `docs(data): XML doc IJObject contracts, JObjectData/Model/Collection, Session, JObjectDB(Manager)` — 82 members

### What's left

The remaining 375 undocumented members are concentrated in the **UI** module — ~20 files of `MonoBehaviour` components (panels, scroll views, buttons, toggles, joysticks, layout helpers, holes/screen-safe areas, image+text composites). Many entries are public serialized fields rather than methods, so the documentation tends to be one-liner descriptions of "what this field controls in the inspector."

The UI files, in rough priority of "API surface consumers care about":

| File | Members | Why important |
|---|---:|---|
| `PanelRoot.cs` | 15 | Panel stack root + dimmer overlay |
| `PanelStack.cs` | 14 | Panel lifecycle stack |
| `PanelController.cs` | 22 | Per-panel controller |
| `JustButton.cs` | 23 | Customized button |
| `JustToggle.cs` | 37 | Customized toggle |
| `OptimizedScrollView.cs` | 12 | Virtualized scroll base |
| `OptimizedHorizontalScrollView.cs` | 21 | Horizontal variant |
| `OptimizedVerticalScrollView.cs` | 33 | Vertical variant |
| `OptimizedScrollItem.cs` | 7 | Item interface |
| `HorizontalSnapScrollView.cs` | 18 | Snap-to-page horizontal |
| `SnapScrollItem.cs` | 5 | Snap item |
| `Joystick.cs`, `JoystickArea.cs` | 20 | Virtual joystick |
| `ProgressBar.cs` | 11 | Slider-driven progress |
| `ImageWithText.cs`, `ImageWithTextTMP.cs` | 6 | Image+label composites |
| `HoledLayerMask.cs` | 17 | Cut-out overlay |
| `ScreenSafeArea.cs`, `IgnoreScreenSafe.cs` | 12 | Notch / safe-area handling |
| `HorizontalAlignmentUI.cs`, `VerticalAlignmentUI.cs`, `TableAlignmentUI.cs` | 26 | Layout helpers |
| `UICircleArranger.cs` | 15 | Radial arrangement |
| `ScrollRectEx.cs`, `SimpleButton.cs`, `SimpleTMPButton.cs`, `CustomToggleGroup.cs`, `CustomToggleSlider.cs`, `IAligned.cs` | ~30 | Miscellaneous |

### Decision point

Three options:

- **Continue and finish (estimated 2-3 more commits).** Brings the framework to 100% public XML doc coverage, which is the original Phase 5 goal.
- **Stop at the 7 fully-documented modules.** UI components are mostly inspector-facing and consumers can read the source — the framework's *API-facing* surface (Foundation + Pool + Timer + Inspector + Prefs + Audio + Data) is fully documented.
- **Cherry-pick the high-API UI files** (`PanelRoot`, `PanelStack`, `PanelController`, `JustButton`, `JustToggle`, the scroll views) and leave inspector-only components for a follow-up pass. Likely lands ~50% of the remaining 344 members.

Whatever the call, the `scripts/xmldoc-baseline.json` gate keeps regression-blocked from this point — new public members must be documented to merge.
