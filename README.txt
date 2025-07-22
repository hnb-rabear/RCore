# RCore Framework - Technical Documentation

## Overview

RCore is a Unity framework designed to provide foundational support for game development. It is not intended to be a powerful, all-encompassing solution; rather, it focuses on a collection of essential systems and helpers to streamline common development tasks.

### Core Features

*   Dynamic Module Management
*   Integrated Audio System
*   Flexible Data Management
*   Optimized UI Components
*   Service Integration (Ads, Firebase, IAP)
*   Development-assistance Editor Tools

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

---

## 1. Core Systems (MAIN/RUNTIME)

This section contains the foundational systems of the framework.

### 1.1. Configuration System

*   `Configuration.cs`: A `ScriptableObject` singleton that manages the application's global configuration. It provides a system for `environments` and `directives` to handle different build settings, stores configuration data in `key-value pairs`, and automatically sets the `culture info` on game startup.

### 1.2. Audio System

A comprehensive and centralized system for managing all aspects of in-game audio.

*   **`AudioManager`**: A global singleton (`BaseAudioManager`) that serves as the primary interface for all audio operations. It provides independent volume control for master, music, and SFX channels, complete with fade-in/out effects. For performance, it features a robust SFX management system with pooling and concurrency limiting to prevent audio clutter. It also includes support for music playlists and automatically listens for UI events from the `EventDispatcher` to play corresponding sounds.

*   **`AudioCollection`**: A `ScriptableObject` that acts as a central database for all `AudioClips`. It is designed for flexible memory management, supporting both direct clip references and dynamic loading via the **`Addressable Assets`** system. A key feature is its integrated **`script generator`**, which can automatically parse audio files from specified folders and create static ID classes (e.g., `SfxIDs.cs`), enabling type-safe and error-free audio calls from code.

*   **`SfxSource`**: A versatile `MonoBehaviour` component for playing sound effects from any `GameObject`. It can operate in two modes:
    1.  **Managed**: By default, it acts as a simple trigger, requesting the central `AudioManager` to play a sound from its managed pool.
    2.  **Standalone**: If an `AudioSource` component is assigned directly, it takes full control of that source, making it ideal for positional 3D audio.
    It also includes features for looping, pitch randomization, and playing a random clip from a predefined list.

### 1.3. Event System

*   `EventDispatcher.cs`: A `static` class providing a centralized event system using `type-safe delegates`. It supports `AddListener`, `RemoveListener`, and `Raise events`, and includes `debounce` functionality to prevent event spam and ensure `thread-safe` operations.

### 1.4. Scene Management

*   `SceneLoader.cs`: A `static utility` class for `asynchronously` loading and unloading scenes. It provides `progress tracking`, `completion callbacks`, `fixed load time` simulation, and supports `additive loading`.

### 1.5. Module Factory System

*   `ModuleFactory.cs`: A `static factory` class for creating and managing `IModule` instances using `reflection` and `attributes`. It automatically discovers modules marked with `ModuleAttribute` and creates them based on a defined `load order`. Note: It cannot create modules that inherit from `MonoBehaviour`.
*   `ModuleManager.cs`: Manages the `lifecycle` of all registered modules, including `registration`, `initialization`, `tick updates`, `shutdown`, and `retrieval`.
*   `ModuleAttribute.cs`: An `attribute` used to mark a class as a module, containing `metadata` such as `Key`, `AutoCreate` flag, and `LoadOrder`.

### 1.6. Data Config Management

*   `ConfigCollection.cs`: An `abstract ScriptableObject base` class for loading configuration data from text files (primarily `JSON`). It supports loading from `Resources` folders or directly from the `AssetDatabase` in the Editor.

---

## 2. Data Systems

The framework features a flexible, JSON-based persistence system designed for clear separation of data and logic. It leverages `ScriptableObjects` for in-editor management and uses `PlayerPrefs` as its storage backend, providing a robust solution for managing game state.

### 2.1. JObjectDB System

This system is built on a layered architecture that separates the persistence engine from the data models and their logic.

*   **`JObjectDB`**: A static database class that functions as the core persistence engine. It manages the low-level operations of serializing data objects to JSON and writing them to `PlayerPrefs`. It provides a global interface for creating, loading, saving, backing up, and restoring data collections.

*   **`JObjectData`**: An abstract base class that defines the contract for any serializable data structure. All custom data classes (e.g., `PlayerData`, `InventoryData`) must inherit from this class to be managed by the system.

*   **`JObjectModel`**: An abstract `ScriptableObject` that represents a high-level data model. It encapsulates a corresponding `JObjectData` instance (the raw state) and contains all the business logic for that data. By implementing lifecycle callbacks like `OnUpdate`, `OnPause`, and `OnPostLoad`, it can independently manage its state, such as calculating offline progress or handling resource generation.

*   **`JObjectModelCollection`**: A central `ScriptableObject` that acts as the root aggregator for the entire data system. It holds a collection of all `JObjectModel`s used in the game and is responsible for orchestrating their lifecycle events, propagating calls like `Load`, `Save`, and `OnUpdate` to every registered model.

*   **`JObjectDBManagerV2`**: A `MonoBehaviour` orchestrator designed to be placed on a persistent object in the scene. It bridges Unity's application lifecycle with the data system by driving the `JObjectModelCollection`. It handles the initial data load and provides robust auto-save features (on pause, on quit) with a configurable delay to batch write operations and enhance performance.

---

## 3. Common Utilities

### 3.1. Helper Classes

*   `AddressableHelper.cs`: Provides `utilities` and `wrapper classes` to simplify working with the `Unity Addressables system`. Supports `async/await`, `coroutines`, download size checking, and asset lifecycle management.
*   `CameraHelper.cs`: `Extension methods` for the `Camera` class for coordinate transformations, camera size calculations, and object visibility checks.
*   `ColorHelper.cs`: `Extension methods` for the `Color` struct to manipulate `alpha`, `invert` colors, and perform other basic color operations.
*   `ComponentHelper.cs`: `Extension methods` for Unity `Components`, such as sorting `SpriteRenderers` by `sorting order` and manipulating the `alpha` of a UI `Image`.
*   `JsonHelper.cs`: `Utilities` for `serializing/deserializing` JSON arrays and lists using `Unity's JsonUtility`.
*   `MathHelper.cs`: Provides extended math functions and `extension methods` for `Vector3`, `int`, and `float`.
*   `RandomHelper.cs`: `Utilities` for randomization and `shuffling` collections.
*   `TimeHelper.cs`: `Utilities` for formatting time, converting `Unix timestamps`, and other time-related calculations.
*   `TransformHelper.cs`: `Extension methods` for `Transform` and `RectTransform` to conveniently manipulate position, `scale`, and `rotation`.

### 3.2. Pooling System

A generic and high-performance object pooling system designed to minimize garbage collection and reduce CPU overhead from frequent object instantiation and destruction.

*   **`CustomPool<T>`**: A generic, serializable class that forms the foundational block of the system. Each instance manages the lifecycle of a single type of `Component` prefab. It maintains separate lists of active and inactive objects, allowing for efficient recycling. Key features include pre-warming (pre-instantiating objects to prevent runtime hitches), callbacks for re-initializing spawned objects, and an optional limit on active instances to automatically recycle the oldest objects.

*   **`PoolsContainer<T>`**: A high-level factory that acts as a "pool of pools," providing a centralized manager for multiple `CustomPool` instances. It simplifies object management by offering a single interface for spawning any prefab; the container automatically creates and maintains a dedicated pool for each prefab on demand. Its most significant feature is a highly optimized release mechanism that tracks every spawned object's origin, allowing it to return an object to its correct pool instantly without performing slow searches.

### 3.3. Timer System

An efficient and versatile system for managing time-based logic, conditional waits, and thread-safe actions without the overhead of Unity's coroutines. The system is split into a lightweight utility and a powerful, centralized event manager.

*   **`TimedAction.cs`**: A simple, non-MonoBehaviour class for handling basic countdowns and cooldowns. It must be manually updated from a `MonoBehaviour`'s `Update` loop and provides a lightweight solution for self-contained timed logic.

*   **`TimerEvents.cs`**: A `MonoBehaviour` that acts as a central processor for various event types. It is highly optimized, automatically disabling itself when no events are pending to conserve resources. It manages two primary event types:
    *   **`CountdownEvent`**: Fires an action after a specified duration. Supports both scaled and unscaled time, auto-restarting, and early termination via a break condition.
    *   **`ConditionEvent`**: Fires an action once a specified delegate condition returns true.

*   **`TimerEventsInScene.cs`**: A scene-specific singleton accessor for `TimerEvents`. It provides a global point of access for any timed events that should be cleared when the scene changes. It is the standard choice for most in-game timers.

*   **`TimerEventsGlobal.cs`**: A persistent, global singleton (`DontDestroyOnLoad`) version of `TimerEvents`. It serves two critical functions:
    1.  Managing long-running timers that must persist across scene loads.
    2.  Providing a **thread-safe execution queue**. Its `Enqueue(Action)` method allows actions from background threads (e.g., network callbacks) to be safely marshaled and executed on Unity's main thread.

### 3.4. Big Number System

*   `BigNumberD.cs`: Represents large numbers using `decimal` as the base type for high precision.
*   `BigNumberF.cs`: Represents large numbers using `float` as the base type for better performance. Supports conversion to `notation string` (e.g., 1.23E+45) and `KKK format` (e.g., 1.23AA).
*   `BigNumberHelper.cs`: A `static` class with `utility methods` to format and display large numbers in a user-friendly way.

---

## 4. UI System

### 4.1. Panel System

The Panel System is a hierarchical, stack-based framework designed to manage UI lifecycle, navigation, and presentation. It provides a robust architecture for creating complex, nested UI flows through three primary components.

*   **`PanelController`**: The base class for any individual UI view (e.g., a dialog, screen, or menu). Each controller is responsible for its own lifecycle, including presentation state and transition animations (`Show`/`Hide`). As a `PanelStack` itself, a `PanelController` can manage its own sub-panels, enabling deeply nested UI structures.

*   **`PanelStack`**: An abstract class that implements the core stack-based navigation model. It manages a collection of `PanelController` instances, handling the logic for pushing (adding) and popping (removing) panels. It supports various presentation strategies, such as adding a panel `OnTop` of another or as a `Replacement`.

*   **`PanelRoot`**: A singleton that serves as the top-level container and entry point for the entire UI system. It employs an event-driven architecture, listening for global requests to display panels (`PushPanelEvent`, `RequestPanelEvent`). This decouples the UI from other game systems. Key features include a global panel queue for sequential displays and automated management of a background dimmer overlay for modal views.

### 4.2. UI Components

A collection of robust, production-ready UI components that extend Unity's base UI system with advanced features, animations, and performance optimizations.

*   **`JustButton` / `SimpleTMPButton`**: An enhanced button system built to replace the standard Unity Button.
    *   **`JustButton`** is the base class, providing a scale-bounce animation on click, integrated sound effects via the `EventDispatcher`, automatic greyscaling for the disabled state, and optional sprite swapping.
    *   **`SimpleTMPButton`** inherits from `JustButton` and adds support for TextMeshPro labels. It includes features to automatically swap font colors and materials based on the button's enabled state, making it the recommended button for modern UI development. An obsolete `SimpleButton` for legacy UI Text is also included.

*   **`JustToggle` / `CustomToggleGroup`**: A powerful toggle system for creating dynamic and interactive selection controls.
    *   **`JustToggle`** is an advanced toggle with a rich, tween-based transition system. It can animate the size, position, color, and sprite of multiple target graphics when its state changes. It also includes sound effects and a locking mechanism.
    *   **`CustomToggleGroup`** extends the standard `ToggleGroup` by managing a dynamic background element that can smoothly animate its position and size to highlight the currently selected toggle.

*   **Optimized Scroll Views**: A set of high-performance scroll view components that use UI virtualization (recycling) to display thousands of items with minimal performance impact. Instead of instantiating a GameObject for every item, a small pool of visible items is created and their content is updated as the user scrolls.
    *   **`OptimizedScrollItem`**: The abstract base class that all items used within an optimized scroll view must inherit from. It requires developers to implement the `OnUpdateContent()` method to define how the item's visuals are populated with data.
    *   **`OptimizedVerticalScrollView`**: A vertically-scrolling implementation that supports both simple lists and grid layouts.
    *   **`OptimizedHorizontalScrollView`**: A horizontally-scrolling implementation that includes support for fixed border elements at the start and end of the list.

*   **Specialized Scroll Views**:
    *   **`HorizontalSnapScrollView`**: A scroll view designed for carousels or selection screens where items automatically "snap" to a central focal point. It intelligently animates to the nearest item based on user drag velocity and position.
    *   **`ScrollRectEx`**: An extension of the standard `ScrollRect` designed to resolve conflicts with nested scroll views (e.g., a vertical list inside a horizontal pager). It detects the primary drag direction and intelligently passes the event to parent scroll rects when necessary.

---

## 5. Services Integration

*   **Ads System**: `AdsProvider.cs` (base), `AdmobProvider.cs`, `ApplovinProvider.cs`, `IronSourceProvider.cs`.
*   **Firebase Integration**: `RFirebase.cs` (core), `RFirebaseAnalytics.cs`, `RFirebaseAuth.cs`, `RFirebaseDatabase.cs`, `RFirebaseFirestore.cs`, `RFirebaseRemote.cs`, `RFirebaseStorage.cs`.
*   **Game Services**: `GameServices.cs` (core), `GameServices.CloudSave.cs`, `GameServices.InAppReview.cs`, `GameServices.InAppUpdate.cs`.
*   **IAP System**: `IAPManager.cs` for managing `In-App Purchases`.
*   **Notification System**: `NotificationsManager.cs`, `GameNotification.cs`, `PendingNotification.cs`.

---

## 6. Editor Tools

### 6.1. Inspector Enhancements

*   `AutoFillAttribute.cs`: Automatically populates component references in the Inspector.
*   `ReadOnlyAttribute.cs`: Makes a field read-only in the Inspector.
*   `CommentAttribute.cs`: Adds descriptive comments to the Inspector.
*   `HighlightAttribute.cs`: Highlights important fields in the Inspector.
*   `CreateScriptableObjectAttribute.cs`:
*   `DisplayEnumAttribute.cs`:
*   `ExposeScriptableObjectAttribute.cs`:
*   `FolderPathAttribute.cs`:
*   `SeparatorAttribute.cs`:
*   `ShowIfAttribute.cs`:
*   `SpriteBoxAttribute.cs`:
*   `TagSelectorAttribute.cs`:
*   `TMPFontMaterialsAttribute.cs`:
*   ....

### 6.2. Development Tools

*   `AssetShortcutsWindow.cs`: A window for quick access to frequently used assets.
*   `FindComponentReferenceWindow.cs`: A tool to find all references to a specific component.
*   `ObjectsFinderWindow.cs`: A tool for finding objects in the current scene.
*   `ScreenshotTaker.cs`: A utility for taking in-game screenshots.
*   `ScenesNavigatorWindow.cs`: A window for navigating quickly between scenes.
*   `PlayAssetDeliveryFilter.cs`:
*   `ToolsCollectionWindow.cs`:
*   ...

### 6.3. Reskin Toolkit

*   `FindAndReplaceAssetToolkit.cs`: A tool for finding and replacing assets in bulk.
*   `SpriteReplacer.cs`: A specialized tool for replacing `Sprites`.
*   `FontReplacer.cs`: A specialized tool for replacing `Fonts`.
*   `TMPFontReplacer.cs`: A specialized tool for replacing TextMeshPro fonts.
*   ...