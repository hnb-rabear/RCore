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

*   `BaseAudioManager.cs`: An `abstract base` class for the audio system. Features include `volume control` (master, music, SFX), `fade in/out` effects, `music playlist` support, and `SFX pooling/limiting` for performance optimization.
*   `AudioManager.cs`: A `singleton` implementation of `BaseAudioManager` with `DontDestroyOnLoad` behavior, providing a global access point. It automatically listens for UI SFX events from the `EventDispatcher` to play corresponding sounds.
*   `AudioCollection.cs`: A `ScriptableObject` that holds collections of `AudioClips` for music and SFX. It supports dynamic asset loading via `Addressable Assets` and includes a `script generator` to create audio IDs from folder structures automatically.
*   `SfxSource.cs`: A `MonoBehaviour` component for playing SFX with settings for `loop`, `pitch randomization`, and `volume control`. It can be used standalone or integrated with the `AudioManager`.

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

### 2.1. JObjectDB System

A `JSON`-based system designed for handling game data with flexible structures.

*   `JObjectDB.cs`: The main `static` class for the storage system. Provides methods to create, save, load, delete, `backup`, and `restore` data collections. It uses `PlayerPrefs` as its storage backend and supports `import/export` of JSON data.
*   `JObjectData.cs`: An `abstract base` class for all data models. It provides methods for `serializing/deserializing` JSON (supporting both `JsonUtility` and `Newtonsoft.Json`), saving/loading, and deleting data.
*   `JObjectDBManagerV2.cs`: An `abstract MonoBehaviour` that manages a `JObjectModelCollection` with advanced `auto-save` features (on delay, on pause/quit) and data lifecycle management.
*   `JObjectModel.cs`: An `abstract ScriptableObject` representing a specific data model. It handles `lifecycle events` such as `Init`, `OnPause`, `OnPostLoad`, `OnUpdate`, and `OnPreSave`.
*   `JObjectModelCollection.cs`: A `ScriptableObject` that manages a collection of `JObjectModel` instances. It provides methods for `Load`, `Save`, and includes `editor tools` for data management.

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

### 3.2. Pool System

*   `CustomPool.cs`: A `generic object pooling` system designed to reuse objects and improve performance by reducing frequent `spawn/despawn` operations.
*   `PoolsContainer.cs`: A container that centrally manages multiple `object pools`, acting as a `factory` combined with object pooling.

### 3.3. Timer System

*   `TimerEvents.cs`: A `MonoBehaviour` class that manages various `timer events`, including `countdown`, `condition`, and `delayable` events. Supports both `scaled` and `unscaled` time.
*   `TimerEventsGlobal.cs`: A `singleton` version of `TimerEvents` that persists across scene loads (`DontDestroyOnLoad`). It provides an `execution queue` to run actions `thread-safe` from background threads.
*   `TimedAction.cs`: A simple class for executing an action after a specified duration, with an `onFinished` callback.

### 3.4. Big Number System

*   `BigNumberD.cs`: Represents large numbers using `decimal` as the base type for high precision.
*   `BigNumberF.cs`: Represents large numbers using `float` as the base type for better performance. Supports conversion to `notation string` (e.g., 1.23E+45) and `KKK format` (e.g., 1.23AA).
*   `BigNumberHelper.cs`: A `static` class with `utility methods` to format and display large numbers in a user-friendly way.

---

## 4. UI System

### 4.1. Panel System

*   `PanelController.cs`: An `abstract base` class for all UI `panels`. It manages the panel `lifecycle` (`Show`, `Hide`, `Back`) and `animation effects` via `coroutines`.
*   `PanelStack.cs`: Manages a `stack` of `PanelControllers`. It supports various `push modes` (`OnTop`, `Replacement`, `Queued`), a `caching system`, and panel navigation.
*   `PanelRoot.cs`: The `root container` for the entire UI system. It manages the `panel queue`, `dimmer overlay` for modal panels, and `event-driven panel pushing`.

### 4.2. UI Components

*   `JustButton.cs`: An extended `Button` class with a `scale bounce effect`, a `greyscale effect` when disabled, and a `click sound effect`.
*   `SimpleTMPButton.cs`: Inherits from `JustButton` and adds support for a `TextMeshPro` label with `font color/material swap` features.
*   `ProgressBar.cs`: A versatile `UI component` for displaying progress bars, supporting different fill modes, countdown displays, and percentage text.
*   `ScreenSafeArea.cs`: A component that automatically adjusts a `RectTransform` to fit within the screen's safe area, accounting for device notches and curved edges.
*   `HorizontalAlignment.cs`, `VerticalAlignment.cs`, `TableAlignment.cs`: Alignment components that automatically arrange child objects horizontally, vertically, or in a `grid` layout, with animation support.
*   `OptimizedScrollView.cs`, `OptimizedVerticalScrollView.cs`, `OptimizedHorizontalScrollView.cs`: Performance-optimized `ScrollView` implementations that use `object pooling` to render only the visible items in the viewport.

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