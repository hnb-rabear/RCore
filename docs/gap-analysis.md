## Gap Analysis: RCore features missing in RevCore

Each gap needs one decision per row:

- **PORT** — implement in RevCore. Add ETA + module owner.
- **DROP** — not coming to RevCore. Consumers stay on RCore for this, or migrate to a third-party.
- **REPLACE** — RevCore covers via a different API; document the equivalence.
- **DEFER** — revisit after v1.0.

Maintainer (you) fills in the **Decision** column below.

Total gap types: **250** across 26 RCore modules.

### Common (35 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `AnimEventListener` | class |  |  |
| `AssetsList` | class |  |  |
| `BaseEvent` | interface |  |  |
| `DontDestroyedGroup` | class |  |  |
| `Encryption` | class |  |  |
| `Env` | class |  |  |
| `EventDispatcher` | class |  |  |
| `IEncryption` | interface |  |  |
| `REditorPref` | class |  |  |
| `REditorPrefBool` | class |  |  |
| `REditorPrefContainer` | class |  |  |
| `REditorPrefDateTime` | class |  |  |
| `REditorPrefDict` | class |  |  |
| `REditorPrefEnum` | class |  |  |
| `REditorPrefFloat` | class |  |  |
| `REditorPrefInt` | class |  |  |
| `REditorPrefList` | class |  |  |
| `REditorPrefObject` | class |  |  |
| `REditorPrefSerializableObject` | class |  |  |
| `REditorPrefString` | class |  |  |
| `REditorPrefVector` | class |  |  |
| `RMenu` | class |  |  |
| `RPlayerPrefDateTime` | class |  |  |
| `RPlayerPrefObject` | class |  |  |
| `RPlayerPrefSerializableObject` | class |  |  |
| `RVector2` | struct |  |  |
| `RVector2Int` | struct |  |  |
| `RVector3` | struct |  |  |
| `RVector3Int` | struct |  |  |
| `RVector4` | struct |  |  |
| `Roman` | class |  |  |
| `SceneLoader` | class |  |  |
| `TapFeedback` | enum |  |  |
| `UIPivot` | enum |  |  |
| `YesNoNone` | enum |  |  |

### Common/BigNumber (2 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `BigNumberAlphaExtension` | class |  |  |
| `BigNumberHelper` | class |  |  |

### Common/Debug (3 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `Debug` | class |  |  |
| `DebugDraw` | class |  |  |
| `FOVInfo` | class |  |  |

### Common/Helper (49 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `AddressableUtil` | class |  |  |
| `AnchorType` | enum |  |  |
| `AssetBundleRef` | class |  |  |
| `AssetBundleWith2EnumKeys` | class |  |  |
| `AssetBundleWithEnumKey` | class |  |  |
| `AssetBundleWithIntKey` | class |  |  |
| `AssetBundleWrap` | class |  |  |
| `AssetRef_FontAsset` | class |  |  |
| `AssetRef_SpriteAtlas` | class |  |  |
| `CameraHelper` | class |  |  |
| `ComponentHelper` | class |  |  |
| `ComponentRef` | class |  |  |
| `ComponentRef_SpriteRenderer` | class |  |  |
| `DateTimePickerWindow` | class |  |  |
| `DebugDrawHelper` | class |  |  |
| `EditorAssetUtil` | class |  |  |
| `EditorBuildUtil` | class |  |  |
| `EditorComponentUtil` | class |  |  |
| `EditorDrawing` | class |  |  |
| `EditorFileUtil` | class |  |  |
| `EditorGui` | class |  |  |
| `EditorHelper` | class |  |  |
| `EditorLayout` | class |  |  |
| `EditorSerializedPropertyExtensions` | class |  |  |
| `GUIStyleHelper` | class |  |  |
| `GuiButton` | class |  |  |
| `GuiColor` | class |  |  |
| `GuiDropdownListEnum` | class |  |  |
| `GuiDropdownListInt` | class |  |  |
| `GuiDropdownListString` | class |  |  |
| `GuiFoldout` | class |  |  |
| `GuiHeaderFoldout` | class |  |  |
| `GuiInt` | class |  |  |
| `GuiObject` | class |  |  |
| `GuiTabs` | class |  |  |
| `GuiText` | class |  |  |
| `GuiToggle` | class |  |  |
| `IDraw` | interface |  |  |
| `IPInfo` | struct |  |  |
| `JsonHelper` | class |  |  |
| `NameGenerator` | class |  |  |
| `ProjectileHelper` | class |  |  |
| `RUtil` | class |  |  |
| `RUtilExtension` | class |  |  |
| `RandomExtension` | class |  |  |
| `SpriteInfo` | struct |  |  |
| `StringBuilderExtension` | class |  |  |
| `TrajectoryHelper` | class |  |  |
| `WebRequestHelper` | class |  |  |

### Common/Pool (1 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `CustomPool` | class |  |  |

### Common/SerializableDictionary (4 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `AssetReferenceWrapper` | class |  |  |
| `SerializableKeyValue` | class |  |  |
| `SerializableKeyValueDrawer` | class |  |  |
| `UnityObjectWrapper` | class |  |  |

### Common/Timer (8 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `ConditionEvent` | class |  |  |
| `ConditionEventsGroup` | class |  |  |
| `CountdownEvent` | class |  |  |
| `CountdownEventsGroup` | class |  |  |
| `DelayableEvent` | class |  |  |
| `TimerEvents` | class |  |  |
| `TimerEventsGlobal` | class |  |  |
| `TimerEventsInScene` | class |  |  |

### Common/VFX (3 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `CFX_Component` | class |  |  |
| `CFX_ParticleComponent` | class |  |  |
| `CFX_ParticleComponentEditor` | class |  |  |

### Configuration.cs (3 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `Configuration` | class |  |  |
| `Directive` | class |  |  |
| `Env` | class |  |  |

### Data (3 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `ConfigCollection` | class |  |  |
| `ConfigCollectionEditor` | class |  |  |
| `ConfigCollectionEditor` | class |  |  |

### Data/Common (1 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `BinaryDataSaver` | class |  |  |

### Data/JObjectDB (4 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `JObjectDataCollection` | class |  |  |
| `JObjectDataCollectionEditor` | class |  |  |
| `JObjectDataCollectionEditor` | class |  |  |
| `SessionDataHandler` | class |  |  |

### Data/KeyValueDB (21 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `BigNumberData` | class |  |  |
| `BoolData` | class |  |  |
| `DataGroup` | class |  |  |
| `DateTimeData` | class |  |  |
| `FloatData` | class |  |  |
| `FunData` | class |  |  |
| `IntegerData` | class |  |  |
| `InvItemData` | class |  |  |
| `InvRPGItemData` | class |  |  |
| `InventoryData` | class |  |  |
| `InventoryRPGData` | class |  |  |
| `KeyValueCollection` | class |  |  |
| `KeyValueDB` | class |  |  |
| `KeyValueDBManager` | class |  |  |
| `KeyValueDBManagerEditor` | class |  |  |
| `KeyValueDBManagerEditor` | class |  |  |
| `KeyValueSS` | class |  |  |
| `ListData` | class |  |  |
| `LongData` | class |  |  |
| `StringData` | class |  |  |
| `TimedTaskData` | class |  |  |

### Data/RPGBase (7 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `Attribute` | class |  |  |
| `AttributeParseExtenstion` | class |  |  |
| `AttributesCollection` | class |  |  |
| `LinkedMod` | class |  |  |
| `Mod` | class |  |  |
| `ModsContainer` | class |  |  |
| `TimedMod` | class |  |  |

### Inspector (17 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `AutoFillDrawer` | class |  |  |
| `CommentDecoratorDrawer` | class |  |  |
| `CreateScriptableObjectDrawer` | class |  |  |
| `DisplayEnumDrawer` | class |  |  |
| `ExposeScriptableObjectDrawer` | class |  |  |
| `FolderPathPropertyDrawer` | class |  |  |
| `HighlightPropertyDrawer` | class |  |  |
| `InspectorButton` | class |  |  |
| `ReadOnlyPropertyDrawer` | class |  |  |
| `SeparatorDecoratorDrawer` | class |  |  |
| `ShowIfDrawer` | class |  |  |
| `SingleLayerPropertyDrawer` | class |  |  |
| `SpriteBoxDrawer` | class |  |  |
| `TMPFontMaterialsAttribute` | class |  |  |
| `TMPFontMaterialsDrawer` | class |  |  |
| `TagSelectorPropertyDrawer` | class |  |  |
| `TimeStampAttribute` | class |  |  |

### Plugins (18 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `Enumerator` | struct |  |  |
| `JSON` | class |  |  |
| `JSONArray` | class |  |  |
| `JSONBool` | class |  |  |
| `JSONLazyCreator` | class |  |  |
| `JSONNode` | class |  |  |
| `JSONNodeExt` | class |  |  |
| `JSONNodeType` | enum |  |  |
| `JSONNull` | class |  |  |
| `JSONNumber` | class |  |  |
| `JSONObject` | class |  |  |
| `JSONString` | class |  |  |
| `JSONTextMode` | enum |  |  |
| `KeyEnumerator` | struct |  |  |
| `LinqEnumerator` | class |  |  |
| `RNative` | class |  |  |
| `UniClipboard` | class |  |  |
| `ValueEnumerator` | struct |  |  |

### Services/Ads (7 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `AdMobProvider` | class |  |  |
| `ApplovinProvider` | class |  |  |
| `IAdProvider` | interface |  |  |
| `IBannerAdListener` | interface |  |  |
| `IInterstitialAdListener` | interface |  |  |
| `IRewardedAdListener` | interface |  |  |
| `IronSourceProvider` | class |  |  |

### Services/Firebase (20 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `ABConfig` | class |  |  |
| `ABConfigPropertyDrawer` | class |  |  |
| `AnalyticEvent` | class |  |  |
| `IRemoteConfig` | interface |  |  |
| `PlayerDataDoc` | class |  |  |
| `PlayerDataDoc` | class |  |  |
| `PlayerIdentityDoc` | class |  |  |
| `PlayerIdentityDoc` | class |  |  |
| `RDatabaseReference` | class |  |  |
| `RFirebase` | class |  |  |
| `RFirebaseAnalytics` | class |  |  |
| `RFirebaseAuth` | class |  |  |
| `RFirebaseCrashlytics` | class |  |  |
| `RFirebaseDatabase` | class |  |  |
| `RFirebaseFirestore` | class |  |  |
| `RFirebaseFirestore` | class |  |  |
| `RFirebaseRemote` | class |  |  |
| `RFirebaseStorage` | class |  |  |
| `SavedFileDefinition` | class |  |  |
| `WaitForTaskStorage` | class |  |  |

### Services/GameServices (8 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `AskFriendResolutionStatus` | enum |  |  |
| `ConflictResolutionStrategy` | delegate |  |  |
| `GameServices` | class |  |  |
| `GameServices` | class |  |  |
| `GameServices` | class |  |  |
| `GameServices` | class |  |  |
| `ISavedGameMetadata` | interface |  |  |
| `SelectUIStatus` | enum |  |  |

### Services/IAP (1 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `IAPManager` | class |  |  |

### Services/Notification (7 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `DefaultSerializer` | class |  |  |
| `GameNotification` | class |  |  |
| `IPendingNotificationsSerializer` | interface |  |  |
| `NotificationsManager` | class |  |  |
| `NotificationsPlatform` | class |  |  |
| `OperatingMode` | enum |  |  |
| `PendingNotification` | class |  |  |

### UI (16 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `Bubble` | class |  |  |
| `CanvasResolutionFixerEditor` | class |  |  |
| `ContentRectSizeFilter` | class |  |  |
| `HoledLayerMaskEditor` | class |  |  |
| `LeftSnapScrollView` | class |  |  |
| `Message` | class |  |  |
| `MessageWithPointer` | class |  |  |
| `MessagesHUD` | class |  |  |
| `Notification` | class |  |  |
| `PointerAlignment` | enum |  |  |
| `ProgressBarEditor` | class |  |  |
| `ProgressBarEditor` | class |  |  |
| `ResolutionFixer` | class |  |  |
| `ScaleType` | enum |  |  |
| `UIGridScaler` | class |  |  |
| `VerticalSnapScrollView` | class |  |  |

### UI/Button (2 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `JustButtonEditor` | class |  |  |
| `SimpleButtonEditor` | class |  |  |

### UI/DragDrop (4 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `UIDragController` | class |  |  |
| `UIDragHandler` | class |  |  |
| `UIDraggableItem` | class |  |  |
| `UIDraggableWorldItem` | class |  |  |

### UI/PanelStack (4 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `PanelControllerEditor` | class |  |  |
| `PanelRootEditor` | class |  |  |
| `PanelStackEditor` | class |  |  |
| `PanelStackEditor` | class |  |  |

### UI/Toggle (2 types)

| RCore type | Kind | Decision (PORT / DROP / REPLACE / DEFER) | Note |
|---|---|---|---|
| `CustomToggleEditor` | class |  |  |
| `CustomToggleTabEditor` | class |  |  |

