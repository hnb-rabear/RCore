# RCore Framework

## Overview

RCore is a Unity framework providing essential systems and helpers for game development. It focuses on streamlining common tasks with reusable modules, UI components, data persistence, and editor productivity tools.

## Installation

Add packages via Unity Package Manager → "Add package from git URL...":

1.  **UniTask** (Dependency)
    ```
    https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
    ```
2.  **RCore**
    ```
    https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Main
    ```
3.  **RCore Services** (Optional)
    *   **Ads**: `https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Ads`
    *   **Firebase**: `https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Firebase`
    *   **GameServices**: `https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/GameServices`
    *   **IAP**: `https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/IAP`
    *   **Notification**: `https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Notification`
4.  **RCore.SheetX** (Optional)
    ```
    https://github.com/hnb-rabear/RCore.git?path=Assets/RCore.SheetX
    ```
    [Read Documentation](Assets/RCore.SheetX/Document/Document.md)

---

## Core Systems

### Configuration System
**`Configuration.cs`** — A singleton `ScriptableObject` for managing global project settings, build environments with scripting define symbols, key-value data storage, and automatic `CultureInfo` standardization (en-US).

### Audio System
*   **`AudioManager`** — Singleton for music/SFX playback with volume control, fade effects, SFX pooling, and UI event integration via `EventDispatcher`.
*   **`AudioCollection`** — `ScriptableObject` database for `AudioClip` references, supporting both direct references and Addressable Assets loading. Includes an audio ID script generator.
*   **`SfxSource`** — Per-GameObject SFX component with two modes: Managed (via `AudioManager` pool) or Standalone (direct `AudioSource` control). Supports looping, pitch randomization, and random clip selection.

### Event System
**`EventDispatcher`** — A static, type-safe publish-subscribe event system for decoupled communication. Supports global event raising (`Raise`), listener management (`AddListener`/`RemoveListener`), and event debouncing via UniTask.

### Module Factory System
An attribute-based system for dynamic module discovery and lifecycle management.
*   **`IModule`** — Interface defining `Initialize()`, `Tick()`, `Shutdown()`.
*   **`ModuleAttribute`** — Marks a class as a discoverable module with `Key`, `AutoCreate`, and `LoadOrder`.
*   **`ModuleFactory`** — Scans assemblies and instantiates modules.
*   **`ModuleManager`** — Persistent singleton orchestrating the module lifecycle.

### Data Config Management
**`ConfigCollection`** — Abstract `ScriptableObject` base class for populating configuration data from external `JSON` files, supporting `Resources` and `AssetDatabase` loading.

---

## Data Systems

### JObjectDB System
A layered, JSON-based persistence system using `PlayerPrefs` as its backend.
*   **`JObjectDB`** — Core persistence engine for serializing/deserializing data to `PlayerPrefs`.
*   **`JObjectData`** — Abstract base class for serializable data structures.
*   **`JObjectModel`** — `ScriptableObject` encapsulating data and business logic with lifecycle callbacks (`OnUpdate`, `OnPause`, `OnPostLoad`).
*   **`JObjectModelCollection`** — Root aggregator orchestrating all `JObjectModel` lifecycles.
*   **`JObjectDBManagerV2`** — `MonoBehaviour` bridging Unity's app lifecycle with auto-save on pause/quit.

### KeyValueDB System
A simpler key-value persistence alternative for settings, flags, and counters.
*   **`KeyValueDB`** — Typed read/write access (`int`, `float`, `string`, `bool`, `DateTime`) backed by `PlayerPrefs`.
*   **`KeyValueCollection`** — `ScriptableObject` defining key-value schemas with default values.
*   **`KeyValueDBManager`** — `MonoBehaviour` managing load/save/auto-save.

### RPGBase System
Foundational attribute and modifier system for RPG-style games.
*   **`Attribute.cs`** — Game attribute with base value and modifier support (flat/percentage).
*   **`Mod.cs`** — Modifier with type, value, and source tracking.

### Binary Data Saver
**`BinaryDataSaver.cs`** — Utility for serializing/deserializing objects to binary files via `BinaryFormatter`.

---

## Common Utilities

### Helper Classes

*   **`CameraHelper`** — Camera manipulation, screen/world conversions, viewport checks, world-to-canvas point.
*   **`ColorHelper`** — Color extensions (alpha, invert, brightness, hex/int conversion) + 25+ predefined color constants.
*   **`ComponentHelper`** — Recursive child/parent finders, `GetOrAddComponent`, sprite sizing, and a simple **list-based pool system** (`Obtain`/`Free`/`Prepare`).
*   **`DebugDrawHelper`** — Editor-only Handles API tools for drawing wireframes, grids, text, and move paths.
*   **`JsonHelper`** — Wrapper for Unity's `JsonUtility` to handle arrays and lists.
*   **`MathHelper`** — Angle calculations, geometric checks, grid calculations (standard/isometric), curve generation, number conversions.
*   **`NameGenerator`** — Procedural display name generation with 10 cultural patterns (Cyrillic, Chinese, Japanese, Korean, Arabic, Vietnamese, Thai, Latin, English, comedic). Supports country-code-based auto-selection, localized structural rules (e.g., forced two-part names for Asian regions), and strict region-based restrictions for comedic names to preserve immersion.
*   **`RUtil`** — Randomization (weighted, min-distance), string manipulation, GC/memory logging, screen safe area utilities.
*   **`TimeHelper`** — Time formatting, server time sync, UTC/local conversion, cheat/offset handling, calendar calculations.
*   **`TrajectoryHelper`** — Physics trajectory calculations for projectiles.
*   **`TransformHelper`** — Extensions for resetting, scaling, and children management.
*   **`WebRequestHelper`** — Server UTC time fetch, IP info retrieval, online status management.
*   **`ENV`** — Environment variable management from `.env` files with typed accessors.
*   **`Encryption`** — XOR-based string encryption with `IEncryption` interface.
*   **`RPlayerPrefs`** / **`REditorPrefs`** — Enhanced `PlayerPrefs`/`EditorPrefs` wrappers with support for bool, List, Dictionary, and optional encryption.

### Other Utilities
*   **`Debug`** — Togglable logging wrapper with colored output, array/list/dictionary printing, JSON logging, and file logging.
*   **`DebugDraw`** — Runtime togglable Gizmos/Debug.DrawLine wrapper for shapes, grids, ellipses, FOV cones, bounds, circles, spheres, cylinders, and capsules.
*   **`SceneLoader`** — Async scene loading with progress callbacks, fixed load time, and additive/single mode support.
*   **`SerializableDictionary`** — Generic dictionary that serializes in Unity's Inspector with custom property drawer.
*   **`AssetsList<T>`** — Generic wrapper for a list of assets with index/name lookup and default fallback.
*   **`AnimEventListener`** — Simple bridge component for receiving Animation Events and forwarding them via C# `Action`.
*   **`DontDestroyedGroup`** — Singleton container for grouping persistent `DontDestroyOnLoad` objects.
*   **`RomanNumber`** — Roman numeral conversion utilities.
*   **`CFX_Component` / `CFX_ParticleComponent`** — VFX lifecycle management with auto-deactivation, sorting order control, and pooling support.
*   **`RVector2` / `RVector3`** — Serializable vector structs with implicit conversion to/from Unity vectors.
*   **`UniClipboard`** — Cross-platform clipboard access (Android/iOS/Editor).
*   **`SimpleJson`** — Lightweight JSON parser/serializer (third-party plugin).
*   **`RNative`** — Native platform bridge (Android JAR / iOS Obj-C) for haptic feedback and platform-specific calls.

### Addressable Wrappers
Wrappers simplifying Unity's Addressable Assets system with loading state tracking and cancellation support.
*   **`AddressableHelper`**, **`AssetBundleRef`**, **`AssetBundleWrap`**, **`AssetRef`**, **`ComponentRef`**

### Pool System
*   **`CustomPool<T>`** — Generic pool managing a single prefab type with pre-warming, auto-relocate, instance limits, and highly optimized, null-safe bulk release routines.
*   **`PoolsContainer<T>`** — A "pool of pools" factory with optimized per-object origin tracking for instant release.

### Timer System
*   **`TimedAction`** — Lightweight non-MonoBehaviour countdown with manual `Update()` calls.
*   **`TimerEventsGroup`** — Core data models: `CountdownEvent`, `ConditionEvent`, `DelayableEvent`.
*   **`TimerEvents`** — MonoBehaviour processing all timer events, auto-disabling when idle.
*   **`TimerEventsInScene`** — Scene-scoped singleton for in-game timers.
*   **`TimerEventsGlobal`** — Persistent singleton with thread-safe main-thread action queue.

### Big Number System
*   **`BigNumberD`** — Large number representation using `decimal` for precision. Full arithmetic, comparison, and display formats (raw, notation, KKK).
*   **`BigNumberF`** — Same as `BigNumberD` but using `float` for performance. Full operator overloading.
*   **`BigNumberHelper`** — Static formatting utilities (`ToNotation`, `ToKKKNumber`).

---

## UI System

### Panel System
Stack-based UI navigation framework.
*   **`PanelController`** — Base class for individual UI views with `Show`/`Hide` lifecycle and transition animations.
*   **`PanelStack`** — Core stack-based navigation with push/pop and presentation strategies (OnTop, Replacement).
*   **`PanelRoot`** — Singleton entry point with event-driven panel requests, global queue, and background dimmer.

### UI Components
*   **`JustButton` / `SimpleTMPButton`** — Enhanced buttons with scale-bounce, greyscaling, and TMP label support.
*   **`JustToggle` / `CustomToggleGroup`** — Toggle system with tween-based transitions and animated group highlighting.
*   **`OptimizedScrollView`** — Virtualized scroll views (vertical with grid, horizontal) for large datasets with cached geometry allocations and smooth, DOTween-powered scroll animations.
*   **`HorizontalSnapScrollView`** — Carousel-like scroll view where items snap to center position.
*   **`ScrollRectEx`** — Nested scroll rect handler.
*   **`ProgressBar`** — Multipurpose bar with fill, countdown, and percentage modes. Configurable fill direction and min/max ratios.
*   **`UICircleArranger`** — Circular layout with multi-circle support and tween animations.
*   **`HoledLayerMask`** — Tutorial spotlight system creating a "hole" in an overlay mask to highlight target UI elements.
*   **`ImageWithText` / `ImageWithTextTMP`** — Compound Image+Text components with auto-resize.
*   **`ScreenSafeArea` / `IgnoreScreenSafe`** — Notch/safe area handling utilities.
*   **`Joystick` / `JoystickArea`** — Virtual joystick input components with configurable radius and drag callbacks.
*   **`Alignment Layouts`** — Custom layout components: `HorizontalAlignment`, `VerticalAlignment`, `TableAlignment` with UI and non-UI variants for flexible child arrangement.

---

## Services Integration

*   **Ads**: `IAdProvider`, `AdmobProvider`, `ApplovinProvider`, `IronSourceProvider`.
*   **Firebase**: Analytics, Auth, Database, Firestore, Remote Config, Storage.
*   **Game Services**: Cloud Save, In-App Review, In-App Update.
*   **IAP**: `IAPManager` for In-App Purchases.
*   **Notification**: `NotificationsManager`, `GameNotification`, `PendingNotification`.

---

## Inspector Attributes

Custom attributes for enhancing the Unity Inspector:

| Attribute | Purpose |
|---|---|
| `[AutoFill]` | Auto-populate null fields with component/SO references |
| `[Comment]` | Display descriptive notes above fields |
| `[CreateScriptableObject]` | Quick-create button for empty SO fields |
| `[DisplayEnum]` | Render int fields as enum dropdowns |
| `[ExposeScriptableObject]` | Inline SO editing in parent Inspector |
| `[FolderPath]` | Folder picker dialog for string fields |
| `[Highlight]` | Color-highlight important fields |
| `[InspectorButton]` | Render methods as clickable buttons |
| `[ReadOnly]` | Visible but non-editable fields |
| `[Separator]` | Horizontal divider with optional title |
| `[ShowIf]` | Conditionally show/hide fields |
| `[SingleLayer]` | Layer dropdown for int fields |
| `[SpriteBox]` | Sprite field with preview thumbnail |
| `[TagSelector]` | Tag dropdown for string fields |
| `[TimeStamp]` | Unix timestamp with date/time picker |
| `[TMPFontMaterials]` | TMP font material dropdown |

---

## Editor Tools

### Essential Windows
*   **RCore Hub** (`Ctrl+Alt+/`) — Modern, icon-driven centralized modular hub for accessing all RCore utilities and development tools categorized by purpose.
*   **Scenes Navigator** (`Ctrl+Alt+K`) — Quick scene switching (Available via RCore Hub).
*   **Asset Shortcuts** (`Ctrl+Alt+L`) — Bookmark frequently used assets (Available via RCore Hub).
*   **Asana Push Tool** — Parse markdown tasklists and seamlessly sync them to Asana as hierarchical subtasks.
*   **Editor Icons Viewer** — Browse built-in Unity editor icons.

### Asset Cleaner
Identifies and removes unused assets by analyzing project dependency graphs. Features deep GUID scanning, persistent cache, folder statistics, and a Project window overlay for at-a-glance identification.

### RHierarchy
Enhances the Hierarchy window with configurable overlays: visibility toggle, component icons, vertex/triangle counts, children count, tag/layer display, static flag, and alternating row colors.

### Other Tools
*   **Reskin Toolkit** — Batch find-and-replace for assets, fonts, sprites, and objects.
*   **Asset GUID Regenerator** — Regenerate GUIDs to resolve conflicts.
*   **Find and Replace Assets** — Batch replace asset references across the project.
*   **Find Component Reference** — Search for component usage in prefabs/scenes.
*   **Find Objects** — Advanced search by particles, persistent events, or scripts.
*   **Addressable Groups Colorizer** — Color-code Addressable asset groups.
*   **Screenshot Taker** — High-resolution game view captures.
*   **RCore Packages Manager** — Install/update RCore packages from git.
*   **WebRequest Tester** — Test `WebRequestHelper` APIs in-editor.
*   **Auto Play First Scene** — Auto-switch to first build scene on Play.
*   **Pre-Build Processor** — Validate project settings before building.
*   **Text Editor Window** — Simple multi-line text editor.
*   **Sprite Sheet Cutter** — Slice sprite sheets into individual sprites.
*   **TexturePacker XML Import** (`TpsXml`) — Import TexturePacker XML atlas data into Unity sprite sheets.