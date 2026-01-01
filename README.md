# RCore Framework

## Overview

RCore is a Unity framework designed to provide foundational support for game development. It is not intended to be a powerful, all-encompassing solution; rather, it focuses on a collection of essential systems and helpers to streamline common development tasks.

### Core Features

*   Dynamic Module Management
*   Integrated Audio System
*   Flexible Data Management
*   Optimized UI Components
*   Service Integration (Ads, Firebase, IAP)
*   Development-assistance Editor Tools

### Table of Contents

*   [Installation](#installation)
*   [Core Systems](#core-systems)
    *   [Configuration System](#configuration-system)
    *   [Audio System](#audio-system)
    *   [Event System](#event-system)
    *   [Module Factory System](#module-factory-system)
    *   [Data Config Management](#data-config-management)
*   [Data Systems](#data-systems)
    *   [JObjectDB System](#jobjectdb-system)
*   [Common Utilities](#common-utilities)
    *   [Helper Classes](#helper-classes)
    *   [Pool System](#pool-system)
    *   [Timer System](#timer-system)
    *   [Big Number System](#big-number-system)
*   [UI System](#ui-system)
    *   [Panel System](#panel-system)
    *   [UI Components](#ui-components)
*   [Services Integration](#services-integration)
*   [Inspector Attributes](#inspector-attributes)
*   [Editor Tools](#editor-tools)

---

## Installation

To install, add the following Git URLs to the Unity Package Manager (UPM) by selecting "Add package from git URL...":

1.  **UniTask** (Dependency)
    ```
    https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
    ```
2.  **RCore**
    ```
    https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Main
    ```
3.  **RCore Services** (Optional)
    *   **Ads**:
        ```
        https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Ads
        ```
    *   **Firebase**:
        ```
        https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Firebase
        ```
    *   **GameServices**:
        ```
        https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/GameServices
        ```
    *   **IAP**:
        ```
        https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/IAP
        ```
    *   **Notification**:
        ```
        https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Notification
        ```
4.  **RCore.SheetX** (Optional)
    *   **SheetX**:
        ```
        https://github.com/hnb-rabear/RCore.git?path=Assets/RCore.SheetX
        ```
        [Read Documentation](Assets/RCore.SheetX/Document/Document.md)

---

## Core Systems

This section contains the foundational systems of the framework.

### Configuration System

**`Configuration.cs`** — A singleton `ScriptableObject` that serves as the central authority for global project settings. It provides a powerful and user-friendly interface for managing build configurations and other persistent data.

*   **Environment and Directive Management**: The core feature of this system is its ability to manage build environments (e.g., "Development", "Production"). Each environment is a collection of scripting define symbols (directives) that can be enabled or disabled. This allows developers to easily control which features are included in a build (e.g., `UNITY_IAP`, `FIREBASE_ANALYTICS`, `DEVELOPMENT`) by simply switching the active environment in the editor, which automatically updates the project's conditional compilation symbols.
*   **Key-Value Data Store**: Includes a serializable dictionary for storing general-purpose configuration data as simple key-value string pairs, accessible globally.
*   **Automatic Culture Standardization**: On application startup, the system automatically sets the project's `CultureInfo` to "en-US". This is a critical feature that prevents localization-related bugs by ensuring consistent date, time, and number formatting (e.g., using a period `.` as the decimal separator) across all devices, regardless of the user's regional settings.

### Audio System

A comprehensive and centralized system for managing all aspects of in-game audio.

*   **`AudioManager.cs`** — A global singleton (`BaseAudioManager`) that serves as the primary interface for all audio operations. It provides independent volume control for master, music, and SFX channels, complete with fade-in/out effects. For performance, it features a robust SFX management system with pooling and concurrency limiting to prevent audio clutter. It also includes support for music playlists and automatically listens for UI events from the `EventDispatcher` to play corresponding sounds.
*   **`AudioCollection.cs`** — A `ScriptableObject` that acts as a central database for all `AudioClips`. It is designed for flexible memory management, supporting both direct clip references and dynamic loading via the **`Addressable Assets`** system. A key feature is its integrated **`script generator`**, which can automatically parse audio files from specified folders and create static ID classes (e.g., `SfxIDs.cs`), enabling type-safe and error-free audio calls from code.
*   **`SfxSource.cs`** — A versatile `MonoBehaviour` component for playing sound effects from any `GameObject`. It can operate in two modes:
    1.  **Managed**: By default, it acts as a simple trigger, requesting the central `AudioManager` to play a sound from its managed pool.
    2.  **Standalone**: If an `AudioSource` component is assigned directly, it takes full control of that source, making it ideal for positional 3D audio.
        It also includes features for looping, pitch randomization, and playing a random clip from a predefined list.

### Event System

**`EventDispatcher.cs`** — A static class that provides a centralized, type-safe event system using the publish-subscribe pattern. It is designed to create a decoupled architecture, allowing different parts of the application to communicate without holding direct references to one another.

*   **Core Functionality**: Systems can subscribe to specific event types using `AddListener<T>()` and unsubscribe with `RemoveListener<T>()`. Events are broadcast globally by creating an instance of an event struct (which must implement the `BaseEvent` interface) and passing it to the `EventDispatcher.Raise()` method.
*   **Debouncing**: Features a `RaiseDeBounce()` method that prevents event spam by ensuring an event is only raised after a specified delay has passed without any new calls for the same event type. This is particularly useful for handling rapid user input, such as button clicks. This functionality is dependent on the **UniTask** library.
*   **Design**: The system is designed for high performance and type safety within Unity's single-threaded environment. It uses a dictionary-based lookup for efficient event routing and ensures that listener subscriptions are managed correctly to prevent memory leaks.

### Module Factory System

A powerful, attribute-based system designed to create a decoupled architecture. It allows for the dynamic discovery, creation, and lifecycle management of independent features or systems, referred to as "modules."

*   **`IModule`** — The core contract that all modules must implement. It defines a standard lifecycle with three essential methods: `Initialize()`, `Tick()`, and `Shutdown()`.
*   **`ModuleAttribute`** — A C# attribute used to mark a class as a discoverable module, defining its unique `Key`, `AutoCreate` behavior, and `LoadOrder`.
*   **`ModuleFactory`** — A static class that scans project assemblies for `[Module]` attributes and handles the instantiation of non-MonoBehaviour modules.
*   **`ModuleManager`** — A persistent singleton that orchestrates the entire module lifecycle. It auto-registers modules based on their attributes, provides methods for manual registration (required for `MonoBehaviour` modules), and calls `Tick()` and `Shutdown()` on all active modules.

### Data Config Management

**`ConfigCollection.cs`** — An abstract `ScriptableObject` base class for populating configuration data from external text files (e.g., `JSON`). It promotes a clean, data-driven workflow by separating configuration from code. The system supports loading from a `Resources` folder at runtime or directly from the `AssetDatabase` in the Editor, with a custom Inspector button to refresh the data on demand.

---

## Data Systems

The framework features a flexible, JSON-based persistence system designed for clear separation of data and logic. It leverages `ScriptableObjects` for in-editor management and uses `PlayerPrefs` as its storage backend, providing a robust solution for managing game state.

### JObjectDB System

This system is built on a layered architecture that separates the persistence engine from the data models and their logic.

*   **`JObjectDB`** — A static database class that functions as the core persistence engine. It manages the low-level operations of serializing data objects to JSON and writing them to `PlayerPrefs`. It provides a global interface for creating, loading, saving, backing up, and restoring data collections.
*   **`JObjectData`** — An abstract base class that defines the contract for any serializable data structure. All custom data classes (e.g., `PlayerData`, `InventoryData`) must inherit from this class to be managed by the system.
*   **`JObjectModel`** — An abstract `ScriptableObject` that represents a high-level data model. It encapsulates a corresponding `JObjectData` instance (the raw state) and contains all the business logic for that data. By implementing lifecycle callbacks like `OnUpdate`, `OnPause`, and `OnPostLoad`, it can independently manage its state, such as calculating offline progress or handling resource generation.
*   **`JObjectModelCollection`** — A central `ScriptableObject` that acts as the root aggregator for the entire data system. It holds a collection of all `JObjectModel`s used in the game and is responsible for orchestrating their lifecycle events, propagating calls like `Load`, `Save`, and `OnUpdate` to every registered model.
*   **`JObjectDBManagerV2`** — A `MonoBehaviour` orchestrator designed to be placed on a persistent object in the scene. It bridges Unity's application lifecycle with the data system by driving the `JObjectModelCollection`. It handles the initial data load and provides robust auto-save features (on pause, on quit) with a configurable delay to batch write operations and enhance performance.

---

## Common Utilities

### Helper Classes

*   **`CameraHelper`**: Utilities for camera manipulation and screen-to-world conversions.
*   **`ColorHelper`**: Extensions for color manipulation, hex conversion, and random color generation.
*   **`ComponentHelper`**: Methods for easily finding, adding, or copying components.
*   **`DebugDrawHelper`**: Tools for visualizing debug information in the scene view.
*   **`JsonHelper`**: Wrapper for Unity's JSON utility to handle arrays and lists easily.
*   **`MathHelper`**: A collection of mathematical functions, probability tools, and vector extensions.
*   **`NameGenerator`**: Procedural name generation for game entities.
*   **`RUtil`**: General-purpose utility methods for common tasks.
*   **`SpineAnimationHelper`**: Helpers for working with Spine animations.
*   **`TimeHelper`**: Utilities for time tracking, formatting, and conversion.
*   **`TrajectoryHelper`**: Physics trajectory calculations for projectiles.
*   **`TransformHelper`**: Extensions for easier Transform manipulation (resetting, scaling, children management).
*   **`WebRequestHelper`**: Simplified wrapper for `UnityWebRequest` to handle HTTP requests.
*   **`Encryption`**: Simple string encryption/decryption utilities.
*   **`RPlayerPrefs`**: Enhanced `PlayerPrefs` wrapper with support for additional types (bool, Vector3, DateTime, etc.) and simple encryption.
*   **`REditorPrefs`**: Similar to `RPlayerPrefs` but for Editor preferences.

### Pool System

A generic and high-performance object pooling system designed to minimize garbage collection and reduce CPU overhead from frequent object instantiation and destruction.

*   **`CustomPool<T>`** — A generic, serializable class that forms the foundational block of the system. Each instance manages the lifecycle of a single type of `Component` prefab. It maintains separate lists of active and inactive objects, allowing for efficient recycling. Key features include pre-warming (pre-instantiating objects to prevent runtime hitches), callbacks for re-initializing spawned objects, and an optional limit on active instances to automatically recycle the oldest objects.
*   **`PoolsContainer<T>`** — A high-level factory that acts as a "pool of pools," providing a centralized manager for multiple `CustomPool` instances. It simplifies object management by offering a single interface for spawning any prefab; the container automatically creates and maintains a dedicated pool for each prefab on demand. Its most significant feature is a highly optimized release mechanism that tracks every spawned object's origin, allowing it to return an object to its correct pool instantly without performing slow searches.

### Timer System

An efficient and versatile system for managing time-based logic, conditional waits, and thread-safe actions without the overhead of Unity's coroutines. The system is split into a lightweight utility and a powerful, centralized event manager.

*   **`TimedAction.cs`** — A simple, non-MonoBehaviour class for handling basic countdowns and cooldowns. It must be manually updated from a `MonoBehaviour`'s `Update` loop and provides a lightweight solution for self-contained timed logic.
*   **`TimerEvents.cs`** — A `MonoBehaviour` that acts as a central processor for various event types. It is highly optimized, automatically disabling itself when no events are pending to conserve resources. It manages two primary event types: `CountdownEvent` and `ConditionEvent`.
*   **`TimerEventsInScene.cs`** — A scene-specific singleton accessor for `TimerEvents`. It provides a global point of access for any timed events that should be cleared when the scene changes. It is the standard choice for most in-game timers.
*   **`TimerEventsGlobal.cs`** — A persistent, global singleton (`DontDestroyOnLoad`) version of `TimerEvents`. It serves two critical functions:
    1.  Managing long-running timers that must persist across scene loads.
    2.  Providing a **thread-safe execution queue**. Its `Enqueue(Action)` method allows actions from background threads (e.g., network callbacks) to be safely marshaled and executed on Unity's main thread.

### Big Number System

*   **`BigNumberD.cs`** — Represents large numbers using `decimal` as the base type for high precision.
*   **`BigNumberF.cs`** — Represents large numbers using `float` as the base type for better performance. Supports conversion to `notation string` (e.g., 1.23E+45) and `KKK format` (e.g., 1.23AA).
*   **`BigNumberHelper.cs`** — A `static` class with `utility methods` to format and display large numbers in a user-friendly way.

---

## UI System

### Panel System

The Panel System is a hierarchical, stack-based framework designed to manage UI lifecycle, navigation, and presentation. It provides a robust architecture for creating complex, nested UI flows through three primary components.

*   **`PanelController`** — The base class for any individual UI view (e.g., a dialog, screen, or menu). Each controller is responsible for its own lifecycle, including presentation state and transition animations (`Show`/`Hide`). As a `PanelStack` itself, a `PanelController` can manage its own sub-panels, enabling deeply nested UI structures.
*   **`PanelStack`** — An abstract class that implements the core stack-based navigation model. It manages a collection of `PanelController` instances, handling the logic for pushing (adding) and popping (removing) panels. It supports various presentation strategies, such as adding a panel `OnTop` of another or as a `Replacement`.
*   **`PanelRoot`** — A singleton that serves as the top-level container and entry point for the entire UI system. It employs an event-driven architecture, listening for global requests to display panels (`PushPanelEvent`, `RequestPanelEvent`). This decouples the UI from other game systems. Key features include a global panel queue for sequential displays and automated management of a background dimmer overlay for modal views.

### UI Components

A collection of robust, production-ready UI components that extend Unity's base UI system with advanced features, animations, and performance optimizations.

*   **`JustButton` / `SimpleTMPButton`** — An enhanced button system built to replace the standard Unity Button. `JustButton` provides scale-bounce effects and greyscaling, while `SimpleTMPButton` adds rich support for TextMeshPro labels, including color and material swapping.
*   **`JustToggle` / `CustomToggleGroup`** — A powerful toggle system for creating dynamic and interactive selection controls. `JustToggle` offers a rich, tween-based transition system for size, position, and color. `CustomToggleGroup` extends this by managing a dynamic background element that animates to highlight the selected toggle.
*   **Optimized Scroll Views** — A set of high-performance scroll view components that use UI virtualization (recycling) to display thousands of items with minimal performance impact. Includes `OptimizedScrollItem`, `OptimizedVerticalScrollView` (with grid support), and `OptimizedHorizontalScrollView`.
*   **Specialized Scroll Views** — Includes `HorizontalSnapScrollView` for carousel-like menus where items snap to a center point, and `ScrollRectEx` for properly handling nested scroll rects (e.g., vertical inside horizontal).

---

## Services Integration

*   **Ads System**: `IAdProvider.cs` (base interface), `AdmobProvider.cs`, `ApplovinProvider.cs`, `IronSourceProvider.cs`.
*   **Firebase Integration**: `RFirebase.cs` (core), `RFirebaseAnalytics.cs`, `RFirebaseAuth.cs`, `RFirebaseDatabase.cs`, `RFirebaseFirestore.cs`, `RFirebaseRemote.cs`, `RFirebaseStorage.cs`.
*   **Game Services**: `GameServices.cs` (core), `GameServices.CloudSave.cs`, `GameServices.InAppReview.cs`, `GameServices.InAppUpdate.cs`.
*   **IAP System**: `IAPManager.cs` for managing `In-App Purchases`.
*   **Notification System**: `NotificationsManager.cs`, `GameNotification.cs`, `PendingNotification.cs`.

---

## Inspector Attributes

A collection of powerful C# attributes designed to enhance the Unity Inspector, making it more organized, intuitive, and efficient. These attributes help reduce boilerplate code and streamline development workflows.

*   **`[AutoFill]`**: Automatically populates `null` fields with component or `ScriptableObject` references, reducing manual drag-and-drop setup. It supports searching by path and automatically filling arrays or lists.
*   **`[Comment]`**: Displays a descriptive note or instruction above a field, providing helpful context directly in the Inspector.
*   **`[CreateScriptableObject]`**: Adds a "Create" button next to an empty `ScriptableObject` field, allowing for the quick creation and assignment of new assets without leaving the Inspector.
*   **`[DisplayEnum]`**: Renders an integer field as a user-friendly enum dropdown menu. The enum type can be specified statically or determined dynamically from a method, offering serialization flexibility.
*   **`[ExposeScriptableObject]`**: Nests a `ScriptableObject`'s properties directly within the parent object's Inspector, allowing for convenient inline editing.
*   **`[FolderPath]`**: Turns a string field into a button that opens a folder selection dialog, storing the chosen directory as a project-relative path.
*   **`[Highlight]`**: Draws attention to an important field by coloring its background, making it stand out visually.
*   **`[InspectorButton]`**: Renders a method as a clickable button in the Inspector, allowing for the direct execution of code. It fully supports methods with parameters.
*   **`[ReadOnly]`**: Makes a serialized field visible but non-editable in the Inspector.
*   **`[Separator]`**: Draws a horizontal line, with or without a title, to visually group and organize fields for a cleaner layout.
*   **`[ShowIf]`**: Conditionally shows or hides a field based on the boolean state of another field, property, or method, creating dynamic and context-aware Inspectors.
*   **`[SingleLayer]`**: Displays an integer field as a built-in Unity Layer dropdown, storing the selected layer's index.
*   **`[SpriteBox]`**: Renders a `Sprite` field with a configurable preview image thumbnail alongside the standard object picker.
*   **`[TagSelector]`**: Displays a string field as a dropdown menu of all available Unity Tags.
*   **`[TMPFontMaterials]`**: On a component with a `TextMeshPro` object, this attribute creates a dropdown for a `Material` field, automatically populating it with all materials associated with the current font asset.

---

## Editor Tools

A suite of productivity tools designed to speed up the development workflow in the Unity Editor.

### Essential Windows
*   **Tools Collection** (`Ctrl+Alt+/`): A centralized hub that aggregates all RCore tools and common actions into a single window.
*   **Scenes Navigator** (`Ctrl+Alt+K`): A floating window for quickly switching between scenes in your project build settings or specific folders.
*   **Asset Shortcuts** (`Ctrl+Alt+L`): Allows you to bookmark frequently used assets (prefabs, scriptable objects) for quick access.
*   **Editor Icon Dictionary**: A viewer for browsing built-in Unity editor icons.

### Utility Tools
*   **Screenshot Taker**: A tool to capture high-resolution screenshots of the game view, with options for transparency and scaling.
*   **Find Component Reference**: Search for all usages of a specific component type in prefabs or scenes.
*   **Find Objects**: An advanced search tool to find objects by various criteria.
*   **Find and Replace Assets**: Batch replace references of one asset with another across the project.
*   **Date Time Picker**: A custom editor window for visually selecting date and time values.

### Helper Shortcuts (RMenu)
*   **Configuration** (`Ctrl+Alt+J`): Selects the main `Configuration` asset.
*   **Group/Ungroup GameObjects**: Quickly parent selected objects under a new container or unparent them.
*   **UI Tools**:
    *   **Perfect Image Size**: Adjusts UI Image dimensions to match their sprite's native ratio.
    *   **Anchor Tools**: Quickly snap RectTransform anchors to corners or pivot points.
    *   **Text Replacer**: Batch convert legacy standard `Text` components to `TextMeshProUGUI`.