# Data Config Collections

Enable **Data Config Collections** in SheetX Settings, then configure project-relative output folders:

```text
Assets/
  Game/
    DataConfig/
      Code/
      Json/
      Collections/
    Resources/
```

- **Collection Code Folder** stores `SheetXDataCollections.cs`, `GlobalConfigCollection.cs`, and matching feature collection scripts.
- **Collection JSON Folder** may be any project-relative `Assets/` folder except one under `Resources` or `StreamingAssets`.
- **Collection Asset Folder** stores feature collection assets.
- **Global Resources Folder** must end exactly in `Resources`; it stores `GlobalConfigCollection.asset`.

## Generated Data Class header

| id:int | name:string | tags[]:string | reward.amount:float | enabled:bool |
| --- | --- | --- | --- | --- |
| 1 | Potion | healing, common | 25 | true |

Plain headers infer `int`, `float`, `bool`, or `string`. Add `:type` only when source values cannot establish intended type. Use `[]` for scalar arrays and dotted names for nested objects.

## Automatic Configuration

When Collections is enabled, worksheet named exactly `Configuration` is always exported from each selected source. Match is ordinal and case-sensitive.

```text
| Sub Class | Field Name | Type | Value |
| economy   | startCoins | int  | 100   |
|           | rates[]    | float-array | 1|1.5|2 |
```

Configuration behavior:

- Sheet row is checked and disabled; it does not create Collection binding.
- Output cells show `Automatic`, `Global`, and `GlobalConfigCollection`.
- Plaintext `Configuration.txt` goes to Collection JSON Folder.
- Nested classes and direct fields go into `GlobalConfigCollection.cs`.
- Values bake into `GlobalConfigCollection.asset`.
- Generated `SheetXCollectionPaths.Configuration` marks active schema. Stale JSON is ignored when marker is absent.
- Existing standalone `Configuration.cs` and `Configuration.asset` remain untouched and dormant.

When Collections is disabled, standalone Configuration export remains unchanged. Detached `SheetXExporter` and batch APIs treat `Configuration` as ordinary row-array JSON.

Full setup, validation, bake, and runtime details: `Assets/RCore.SheetX/Document/Document.md`, sections 7 and 8.5.
