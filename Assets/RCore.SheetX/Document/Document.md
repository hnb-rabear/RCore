# SheetX Document

[English Document](Document.md) | [Tài liệu Tiếng Việt](Document_VN.md)

## 1. Introduction

This tool simplifies database design and management for game developers and designers, allowing easy modification of game statistics without needing developer help.

As game projects grow, so does the need for efficient data table, constant, and ID management. This tool centralizes the process, enabling easy search, modification, and updates.

Originally designed for RPGs with extensive databases, it now supports various game genres and utilizes Excel and Google Spreadsheets for data management.

### Samples
Download the example project [Here](https://github.com/hnb-rabear/hnb-rabear.github.io/blob/main/sheetx/SheetXExample.unitypackage).

## 2. Main Functions

- **Excel and Google Sheets Integration:** Manage your entire database using Excel or Google Spreadsheets.
- **ID and Constant Management:** Make batch adjustments to IDs and constants without impacting the database.
- **Localization System:** Effortlessly handle multiple languages, with seamless Unity integration.
- **JSON Export:** Convert data tables to JSON files for easy Unity integration.
- **Flexible Data Formats:** Support a variety of data formats, adaptable to your design needs.

## 3. Settings

Navigate to the main menu and select: `RCore > Tools > SheetX > Settings`

![tab_settings](https://github.com/user-attachments/assets/8d339afe-3323-4f03-99d0-34b3cc7dc56e)

- **Scripts Output Folder:** Stores exported C# scripts, including IDs, Constants, Localization Components, and Localization API.
- **Json Output Folder:** Stores exported JSON data.
- **Localization Output:** Stores Localization Data, which should be inside the Resources folder for loading via Resources, or in the Localizations folder for loading via Addressable Asset System.
- **Namespace:** Defines the namespace for the exported C# files.
- **Separate IDs: Sheets**
  - **TRUE:** Exports `[%IDs]` sheets to individual C# files named `[SheetName] + IDs.cs`.
  - **FALSE:** Merges all `[%IDs]` sheets from all Excel files into a single C# file named `IDs.cs`.
- **Separate Constants: Sheets**
  - **TRUE:** Exports `[%Constants]` sheets to individual C# files named `[SheetName] + %Constants.cs`.
  - **FALSE:** Merges all `[%Constants]` sheets from all Excel files into a single C# file named `Constants.cs`.
- **Separate Localization Sheets:**
  - **TRUE (default):** Exports `[Localization%]` sheets to separate groups, each containing Localization Data, Component, and API, with the following file name structure:
    - Localization Data: `[SheetName]_[language].txt`
    - Component: `[SheetName] + Text.cs`
    - API: `[SheetName].cs`
  - **FALSE:** Merges all `[Localization%]` sheets from all Excel files into a single group, with the following file name structure:
    - Localization Data: `Localization_[language].txt`
    - Component: `LocalizationText.cs`
    - API: `Localization.cs`
- **Only enum as IDs:** For `[%IDs]` sheets, columns with the extension `[enum]` will be exported as enums and will not include the Integer Constant form.
- **Combine Json Sheets:** Merges the Data Table from one Excel file into a single JSON file, named `[ExcelName].txt`.
- **Language Char Sets:** Used in Localization with TextMeshPro to compile the character table of a language, mainly applied for Korean, Japanese, and Chinese due to their extensive character systems.
- **Persistent columns:** Specifies the names of columns to retain during processing even if they are empty.
- **Google Client ID:** Enter your Google Client ID (retrieved from Credentials in Google Console).
- **Google Client Secret:** Enter your Google Secret (retrieved from Credentials in Google Console).

SheetX is one unified package. IDs, Constants, JSON, Localizations, and single- and multi-file exporters are always available for both Excel and Google Sheets. Old `SX_LOCALIZATION`, `SX_LITE`, and `SX_NO_LOCALIZATION` scripting defines no longer change SheetX; remove them from Player Settings.

Settings are stored in `Assets/SheetX/SheetXSettings.asset`, created on first use. Commit that
asset to keep your Excel paths and Google spreadsheet lists across clones and across your team.
Changes are written when the SheetX window loses focus or is closed.

**The two Google credentials are the exception, and are deliberately not stored in that asset.**
They go to `EditorPrefs`, which is per-machine and never enters version control, so each developer
enters their own once. Do not put them in a shared repository, a shared `.sx` file, or any other
committed file: a credential in a git repository is a published credential, and obfuscating it
does not change that. Saved `.sx` settings files contain no credentials.

## 4. Excel Sheets Exporter

### 4.1. Export Single Excel File

Navigate to the main menu and select: `RCore > Tools > SheetX > Excel Spreadsheets`

![tab_excel_1](https://github.com/user-attachments/assets/b8ef6183-21c6-43b9-b952-8b5d57fc4c0b)

This function is ideal for learning how to use the tools. It's great for small, simple Static Databases that only need one Excel file for all the data.

Key Functions:

- **Export IDs:** Converts ID sheets to C# files.
- **Export Constants:** Converts Constants sheets to C# files.
- **Export Json:** Transforms Data Table sheets into JSON data.
- **Export Localization:** Exports Localization Data, Localization Components, and Localization API.
- **Export All:** Performs all the functions with a single click.

### 4.2. Export Multiple Excel Files

![tab_excel_2](https://github.com/user-attachments/assets/54b3632d-18f9-4053-b2fe-3feef6f71f92)
![tab_excel_2_edit](https://github.com/user-attachments/assets/d958d749-5410-416b-9095-a598f9fe5a82)

This feature is essential for managing complex Static Databases divided into multiple Excel files. It helps you efficiently handle and export all your files with one click:

1. Add all the Excel files you want to process.
2. For each Excel file, you have the option to choose which sheets to include or exclude.
3. Press the Export All button to complete the process.

## 5. Google Spreadsheets

Prefer using Google Spreadsheets? No problem.

Navigate to the main menu and select: `RCore > Tools > SheetX > Google Spreadsheets`

### 5.1. Setup Google Client ID and Client Secret

#### Step 1: Enable Google Sheets API

1. Visit the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project or select an existing one.
3. Click on **Go to APIs overview**.
4. Select **Enable APIs and Services**.
5. Search for and select **Google Sheets API**, then click **Enable**.

#### Step 2: Obtain Credentials

1. On the top Google Sheets API screen, click on **Create Credentials**.
2. Choose **Google Sheets API**, **User data**, then click **Next**.
3. In the Scopes section, click on **Add or remove scopes**.
4. Find and select the **Google Sheets API** (description: "See all your Google Sheets Spreadsheets"), then **Save and Continue**.
5. In the OAuth Client ID section, select Application Type as Desktop App, enter any name, then click **Create**.
6. Click **Done**.

#### Step 3: Accessing Your Client ID and Client Secret:

1. On the Google Sheets API screen, go to the **Credentials** tab, you will find the new Client ID.
2. Click on the Edit button to find the Client ID and Client Secret.
3. Copy the **Client ID** and **Client Secret**, and paste them into the corresponding settings in the **Sheets Exporter Settings** Window

They are saved to `EditorPrefs` on this machine only, not to the settings asset. Each developer on
the team repeats this step once with their own credentials.

The OAuth token Google writes after you authorize is stored in `Library/SheetX`, outside Unity's asset pipeline and version control. Do not move token files under `Assets/`. Legacy token-cache filenames under `Assets/Editor` remain ignored by wildcard as a safeguard for old projects.

![tab_settings_2](https://github.com/user-attachments/assets/4140a3e8-05df-4bbe-a3b8-a2fb0576f1ee)

### 5.2. Export Single Google Spreadsheet

![tab_google_1](https://github.com/user-attachments/assets/02d6e2a9-3c39-4087-9a1e-0e77eec73a19)

Enter the Google Sheet ID, then click the Download button. You can find the ID in the Google Sheet's URL, formatted like this:

```url
https://docs.google.com/spreadsheets/d/[GOOGLE_SHEET_ID]/edit?......
```

### 5.3. Export Multiple Google Spreadsheets

Click on **Add Google Spreadsheets**, then enter the Google Sheet ID in the popup that appears. Press **Download**, then select the sheets you want to process.

![tab_google_2](https://github.com/user-attachments/assets/f2ba3d93-7785-42b3-b33b-13b2687f699f)
![tab_google_2_edit](https://github.com/user-attachments/assets/3386dda3-a2ba-4f88-87d0-f25e43ebfa56)

## 6. Public Export API (for External Tooling)

`RCore.SheetX.Editor.SheetXExporter` runs single-file and multi-source batch exports without the Settings window, the `.sx` asset, or any `EditorPrefs` state. Use it when another Editor tool needs to trigger a SheetX export programmatically — for example a build pipeline, a custom CLI, or a project-specific config layer that owns its own settings UI.

### 6.1. Single-Source Export

```cs
using RCore.SheetX.Editor;

var request = new SheetXExportRequest
{
    SpreadsheetPath = "Assets/Data/Game.xlsx",   // .xlsx path, or a Google spreadsheet ID for ExportGoogle
    Sheets = null,                                 // null = every sheet; empty list = none
    ConstantsOutputPath = "Assets/Generated/Scripts",
    JsonOutputPath = "Assets/Generated/Json",
    LocalizationOutputPath = "Assets/Generated/Localization",
    Namespace = "MyGame.Data",
};

SheetXExportResult result = SheetXExporter.ExportExcel(request, myOutput);
// or: SheetXExporter.ExportGoogle(request, myOutput) — also needs GoogleClientId/GoogleClientSecret

if (!result.Success)
{
    // Result holds every diagnostic; exporter never writes to Console
    foreach (var error in result.Errors)
        ReportError(error);
}
```

### 6.2. Multi-Source Batch Export

`SheetXExporter.ExportBatch` exports multiple Excel workbooks and Google spreadsheets in one run, sharing one global symbolic-ID namespace so ID references across sources resolve cleanly.

```cs
using System.Collections.Generic;
using RCore.SheetX.Editor;

var request = new SheetXBatchExportRequest
{
    Sources = new List<SheetXBatchSource>
    {
        new SheetXBatchSource
        {
            Kind = SheetXSourceKind.Excel,
            SpreadsheetPath = "Assets/Data/Core.xlsx",
            Sheets = null, // null = all sheets in workbook order
            OutputName = "Core",
        },
        new SheetXBatchSource
        {
            Kind = SheetXSourceKind.Google,
            SpreadsheetPath = "1_9BqoKwRsod5cMwML5n_pLpuWk045lD3Jd7nrizqVBo",
            Sheets = new List<string> { "HeroIDs", "Heroes" },
        },
    },
    ConstantsOutputPath = "Assets/Generated/Scripts",
    JsonOutputPath = "Assets/Generated/Json",
    LocalizationOutputPath = "Assets/Generated/Localization",
    Namespace = "MyGame.Data",
    CombineJson = true,
    SeparateIDs = false,
    SeparateConstants = false,
    SeparateLocalizations = true,
    GoogleClientId = callerGoogleClientId,
    GoogleClientSecret = callerGoogleClientSecret,
};

SheetXExportResult result = SheetXExporter.ExportBatch(request, callerOutput);
if (!result.Success)
{
    // Batch flushes zero artifacts to callerOutput if any error occurred
    foreach (var error in result.Errors)
        ReportError(error);
}
```

### 6.3. Contract and Execution Rules

- **`ISheetXOutput.Write(relativePath, content)`** receives every finished artifact. The exporter never calls `EditorUtility.DisplayDialog`, `Debug.Log`, `File.WriteAllText`, or `AssetDatabase` — writing to disk, importing, or showing UI is entirely the caller's choice. An exception thrown from `Write` is caught and reported in `SheetXExportResult.Errors`.
- **Staged output and error isolation:** Batch export materializes and validates all sources, loads global IDs, builds all sources, and generates aggregate files in staging before flushing to `ISheetXOutput`. If any error occurs during validation, materialization, ID resolution, or build, **no artifacts are flushed to the sink** and `SheetXExportResult.Files` is empty. If the sink throws during flush, earlier accepted writes are recorded in `Files`, subsequent writes halt, and the failure is appended to `Errors`.
- **Sheet selection:** `Sheets == null` selects every sheet in source/metadata order; a non-null list selects that subset in native order; an empty list selects none. A requested sheet name absent from the source is returned as an error.
- **Global ID namespace & duplicate policy:** All selected ID sheets across all batch sources load into a shared symbolic-ID lookup table before other sheets build. Duplicate symbolic IDs across sources or sheets are treated as errors (even if their integer values match), keeping the first definition for resolution. ID errors and path collisions report both source and sheet origins.
- **Isolated execution:** Batch export never reads, creates, or dirties `Assets/SheetX/SheetXSettings.asset` and never reads or writes `EditorPrefs`. Google OAuth credentials must be supplied on the request object.
- **`SheetXExportResult.Files`** lists every artifact written, tagged with a `SheetXExportFileType` (`Ids`, `Constants`, `Json`, `Localization`, `CharacterSet`, `LocalizationManager`, `LocalizationConstants`, `LocalizationComponent`).

This API is separate from, and does not replace, the Settings-window-driven exporters in sections 4 and 5 above — those remain the way to configure and run exports interactively for a single project.

## 7. Data Config Collections

Data Config Collections are optional. Enable **Data Config Collections** in SheetX Settings. Disabled remains legacy SheetX: ordinary JSON, IDs, Constants, and Localization output stay unchanged.

### 7.1. Setup

Set these project-relative folders before first export:

```text
Assets/Game/DataConfig/Code
Assets/Game/DataConfig/Json
Assets/Game/DataConfig/Collections
Assets/Game/Resources
```

- **Collection Code Folder** owns `SheetXDataCollections.cs` for generated row models and JSON path constants, plus one matching `<TypeName>.cs` for each collection `ScriptableObject` (`GlobalConfigCollection.cs`, `<Name>ConfigCollection.cs`). Every file banner marks it as automatically generated by SheetX. Do not edit them; add developer partial classes in separate files.
- **Collection JSON Folder** stores editor-time bake input in any ordinary project-relative `Assets/` folder. Do not place it under `Resources` or `StreamingAssets`, because Unity includes those folders in player builds.
- **Collection Asset Folder** stores feature collection assets.
- **Global Resources Folder** must end exactly in `Resources`. It stores generated `GlobalConfigCollection.asset` directly in that folder for runtime `Resources` lookup.
- Exact `Configuration` is automatic Collection input when Collections is enabled. Its fields become direct `GlobalConfigCollection` members and values bake into Global; it never uses a binding or standalone Configuration asset.
- In Collections mode, its sheet row shows checked/disabled with tooltip `Configuration is always exported automatically to GlobalConfigCollection.`; Output Mode, Collection, and Data Class show read-only `Automatic`, `Global`, and `GlobalConfigCollection`.
- Configuration JSON is written as plaintext `Configuration.txt` in Collection JSON Folder. Generated `SheetXCollectionPaths.Configuration` marks compiled participation; without marker, baker ignores stale file.
- Collections-disabled mode keeps standalone Configuration lifecycle unchanged. Old standalone files are dormant in Collections mode and are not deleted. Detached and batch exports keep row-array behavior.

Generated source set describes Collection bindings accepted by current export session. For each Excel file or Google spreadsheet processed in that session, include every Collection-bound sheet from that source. A missing binding from a processed source aborts collection writes to avoid replacing shared declarations with an incomplete source. Saved bindings from unrelated, unprocessed sources do not block the export. Their previous JSON remains untouched, but their declarations are not copied into newly generated source set.

Use **Manage Collections...** to open collection manager. Immutable **Global** is first row; its name cannot change or delete, but Auto and **Load Data** remain available. Add, rename, load, or delete custom collections there. Delete moves bindings to Global. Old generated code and assets remain; SheetX never deletes orphaned output.

### 7.2. Sheet output modes

Ordinary data sheets gain **Output Mode**, **Collection**, and **Data Class** controls. IDs, Constants, Settings, and Localization sheets never gain collection bindings.

- **JSON Only** preserves legacy JSON export.
- **Generated Data Class** infers field types from longest non-empty cell in each column, generates partial data and collection classes, and derives data class name from sheet name.
- **Existing Data Class** keeps legacy JSON shape. Pick a type you have marked with `[SheetXBindable]`. Missing or invalid types log an error and skip that sheet.

Existing Data Class row types are opt-in. Both attributes are required: `[Serializable]` so Unity serializes the baked array, and `[SheetXBindable]` so SheetX offers the type. Classes and structs are both accepted; the type must be concrete and non-generic. `[SheetXBindable]` lives in the auto-referenced runtime assembly, so game code needs no asmdef change.

```csharp
using System;
using RCore.SheetX;

[Serializable, SheetXBindable]
public class EnemyAttackRow
{
    public int id;
    public float damage;
}
```

The Data Class dropdown lists only marked types. A project that has marked none sees `No [SheetXBindable] type found`. The picker, export, and bake share one rule, so a type offered in the dropdown is guaranteed to export and bake.

**Migration from 1.6.0 and earlier.** Any serializable class used to qualify. Add `[SheetXBindable]` to every class or struct already bound as an Existing Data Class; an unmarked type disappears from the dropdown and is rejected at export and bake with an error naming the missing marker. Structs are new — the old rule accepted classes only.

Generated Data Class header grammar:

```text
id | price | enabled | tags[] | reward.amount
id:string | price:float | enabled:bool | tags[]:string | reward.amount:int
```

Plain headers infer `int`, `float`, `bool`, or `string` from longest non-empty cell in their column. Empty columns infer `string`. Scalar arrays use `[]`; dotted names generate nested objects. Add `:type` only to override inference, such as `id:string` for leading-zero IDs or `price:float` when current values are all integers. Supported scalar type annotations are `int`, `float`, `bool`, and `string`. Enum types cannot be declared directly in data sheet headers (e.g. `type:enum` or `type:MyEnum` is not supported); define enums in `[%IDs]` sheets and reference their symbolic keys in data cells instead.

Any header containing exact, case-sensitive `[x]` text anywhere is ignored. A path containing an exact C# keyword segment, such as `fixed` or `reward.class`, is skipped with a warning. Ignored columns retain their source positions, so later field values stay aligned. Other invalid identifiers, malformed or unsupported annotations, duplicate normalized paths, object/leaf conflicts, invalid values, and generated-name collisions log an actionable error and skip only that sheet. Later valid sheets continue. Diagnostics identify source, sheet, 1-based column, raw header when applicable, cause, and a `Fix:` action. Previous JSON for a skipped sheet remains untouched but is excluded from current automatic bake.

### 7.3. Bake and runtime access

**Export** writes accepted collection JSON and matching generated sources atomically. One bad sheet logs once, skips, and does not block later valid sheets. Global namespace/folder/collection-definition failures, a missing Collection-bound sheet from any processed source, or final staging failure abort collection flush so shared output cannot become inconsistent. Bindings from unprocessed sources do not abort current session. After compilation, SheetX creates or updates collection assets and Global feature references from accepted bindings only. Existing collection assets whose Inspector shows `Script: None (Mono Script)` are repaired in place from matching generated script without recreating asset or discarding serialized data. SheetX deserializes JSON only in Editor, writes arrays into assets, and stores no `TextAsset` reference. Runtime collection bases depend only on `UnityEngine` and read serialized data only.

**Auto Load After Export** and **Auto Load Before Play** load only collections whose per-row **Auto Load** is enabled. Disabled collection arrays remain unchanged until pressing that collection's **Load Data**. Global references still refresh. Global **Load All** loads every collection.

```cs
var global = GlobalConfigCollectionBase.Instance<GlobalConfigCollection>();
GlobalConfigCollectionBase.SetInstance(customGlobal);
```

`Instance<T>()` loads default Global asset from Resources. Call `SetInstance<T>()` before first access to override it, for tests or custom bootstraps.

Collection JSON is not secret. Serialized ScriptableObject data remains extractable from player builds; validate sensitive state server-side. Detached `SheetXExporter` and batch APIs do not support Collections.

## 8. Rules in Spreadsheet

### 8.1. IDs

| Hero   |     |         | Building      |     |         | Pet      |     |         | Gender[enum]      |     |
| ------ | --- | ------- | ------------- | --- | ------- | -------- | --- | ------- | ----------------- | --- |
| HERO_1 | 1   | comment | BUILDING_NULL | 0   | comment | PET_NULL | 0   | comment | GENDER_NONE       | 0   |
| HERO_2 | 2   | comment | BUILDING_1    | 1   |         | PET_1    | 1   |         | GENDER_MALE       | 1   |
| HERO_3 | 3   | comment | BUILDING_2    | 2   |         | PET_2    | 2   |         | GENDER_FEMALE     | 2   |
|        |     |         | BUILDING_3    | 3   |         | PET_3    | 3   |         | GENDER_HELICOPTER | 3   |
|        |     |         | BUILDING_4    | 4   |         | PET_4    | 4   |         |                   |     |
|        |     |         | BUILDING_5    | 5   |         | PET_5    | 5   |         |                   |     |
|        |     |         | BUILDING_6    | 6   |         | PET_6    | 6   |         |                   |     |
|        |     |         | BUILDING_7    | 7   |         | PET_7    | 7   |         |                   |     |
|        |     |         | BUILDING_8    | 8   |         |          |     |         |                   |     |

ID Sheets, named with the suffix `IDs` are used to compile all IDs into Integer Constants or C# `enum`s. The design rules are:

- The sheet name must end with `IDs`.
- Only the Integer data type is allowed.
- Each group is organized in 3 consecutive columns: Key Name, Key Value, Comment.
- The first row contains the group name for easy reference.
- The first column holds the Key Name, and the next column holds the Key Value.
- Key Value must be an integer.
- By default, all IDs in a column will be exported as Integer Constants (`public const int KEY = VALUE;`).
- **Enum Header Annotation:** Add the suffix `[enum]` to the group name in Row 0 (e.g. `Gender[enum]`, `ItemType[enum]`) to export that group as a C# `public enum Name { KEY = VALUE, ... }`.
  - With default settings (`Only enum as IDs` = FALSE), SheetX exports **both** `public const int` constants and `public enum`.
  - With `Only enum as IDs` = TRUE in Settings, SheetX exports **only** `public enum` and omits `const int` declarations for that group.
  - In data tables, referring to symbolic enum keys (e.g. `GENDER_MALE`) automatically resolves to integer values in exported JSON.

```
| Group | Key | Comment |
| ----- | --- | ------- |
```

### 8.2. Constants

| Name                  | Type        | Value              | Comment               |
| --------------------- | ----------- | ------------------ | --------------------- |
| EXAMPLE_INT           | int         | 83                 | Integer Example       |
| EXAMPLE_FLOAT         | float       | 1.021              | Float example         |
| EXAMPLE_STRING        | string      | 321fda             | String example        |
| EXAMPLE_INT_ARRAY_1   | int-array   | 4                  | Integer array example |
| EXAMPLE_INT_ARRAY_2   | int-array   | 0:3:4:5            | Integer array example |
| EXAMPLE_FLOAT_ARRAY_1 | float-array | 5                  | FLoat array example   |
| EXAMPLE_FLOAT_ARRAY_2 | float-array | 5:1:1:3            | FLoat array example   |
| EXAMPLE_VECTOR2_1     | vector2     | 1:2                | Vector2 example       |
| EXAMPLE_VECTOR2_2     | vector2     | 1:2:3              | Vector2 example       |
| EXAMPLE_VECTOR3       | vector3     | 3:3:4              | Vector3 example       |
| EXAMPLE_REFERENCE_1   | int         | HERO_1             | Integer example       |
| EXAMPLE_REFERENCE_2   | int-array   | HERO_1 : HERO_2    | Integer array example |
| EXAMPLE_REFERENCE_3   | int-array   | HERO_1 \| HERO_3   | Integer array example |
| EXAMPLE_REFERENCE_4   | int-array   | HERO_1 HERO_4      | Integer array example |
| EXAMPLE_FORMULA_1     | int         | =1\*10\*36         | Excel formula example |
| EXAMPLE_FORMULA_2     | float       | =1+2+3+4+5+6+7+8+9 | Excel formula example |

Constants Sheets, named with the suffix `Constants` compile project constants. The design rules are:

- The sheet name must end with `Constants`.
- There are four columns: Name, Type, Value, and Comment.
  - **Name:** The name of the constant; must be continuous, without special characters.
  - **Type:** The data type of the constant. Possible data types include: `int`, `float`, `bool`, `string`, `int-array`, `float-array`, `vector2`, and `vector3`.
  - **Value:** The value matching the data type. For array types, separate elements with `:` or `|` or `newline`.

```
| Name | Type | Value | Comment |
| ---- | ---- | ----- | ------- |
```

### 8.3. Localization

| idstring     | relativeId | english                   | spanish                        |
| ------------ | ---------- | ------------------------- | ------------------------------ |
| message_1    |            | this is english message 1 | este es el mensaje en ingles 1 |
| message_2    |            | this is english message 2 | este es el mensaje en ingles 2 |
| message_3    |            | this is english message 3 | este es el mensaje en ingles 3 |
|              |            |                           |                                |
| content      | 1          | this is english message 1 | este es el mensaje en ingles 1 |
| content      | 2          | this is english message 2 | este es el mensaje en ingles 2 |
| content      | 3          | this is english message 3 | este es el mensaje en ingles 3 |
|              |            |                           |                                |
| title_1      |            | this is english title 1   | este es el titulo 1 en ingles  |
| title_2      |            | this is english title 2   | este es el titulo 2 en ingles  |
| title_3      |            | this is english title 3   | este es el titulo 3 en ingles  |
|              |            |                           |                                |
| whatever_msg |            | this is a sample message  | este es un mensaje de muestra  |
|              |            |                           |                                |
| hero_name    | HERO_1     | hero name 1               | nombre del héroe 1             |
| hero_name    | HERO_2     | hero name 2               | nombre del héroe 2             |
| hero_name    | HERO_3     | hero name 3               | nombre del héroe 3             |

Localization Sheets are named with the prefix `Localization` and follow these rules:

- The sheet name must start with `Localization`.
- Each sheet has two key columns: the main key `idString` and an additional key `relativeId`.
- The following columns contain localized content.
- The key for each row is a combination of `idString` and `relativeId`.
- `relativeId` can reference an ID from the IDs sheets.

```
| idString | relativeId | english | spanish | japan | .... |
| -------- | ---------- | ------- | ------- | ----- | ---- |
```

### 8.4. Data Table - JSON Data

#### Basic Data Type: Boolean, Number, String

| numberExample1 | numberExample2 | numberExample3 | boolExample | stringExample |
| -------------- | -------------- | -------------- | ----------- | ------------- |
| 1              | 10             | 1.2            | TRUE        | text          |
| 2              | 20             | 3.1            | TRUE        | text          |
| 3              | BUILDING_8     | 5              | FALSE       | text          |
| 6              | HERO_3         | 10.7           | FALSE       | text          |
| 9              | PET_2          | 16.4           | FALSE       | text          |

#### Extended Data Type: Array, JSON Object

| array1[]                | array2[]    | array3[]                       | array4[]              | array5[]   | array6[]    | JSON{}                                                                   |
| ----------------------- | ----------- | ------------------------------ | --------------------- | ---------- | ----------- | ------------------------------------------------------------------------- |
| text1                   | 1           | 1                              | TRUE                  | 123<br/>66 | aaa<br/>ccc | {}                                                                       |
| text2                   | 2 \| 2 \| 3 | 1 \| 2 \| 3                    | TRUE \| FALSE \| TRUE | 123<br/>71 | aaa<br/>ccc | {"id":1, "name":"John Doe 1"}                                            |
| text1 \| text2          | 1 \| 2      | 1 \| BUILDING_2                | TRUE \| FALSE         | 123<br/>67 | aaa<br/>ccc | {"id":2, "name":"John Doe 2"}                                            |
| text1 \| text2 \| text3 | 1 \| 2 \| 3 | BUILDING_1 \| HERO_2           | TRUE \| FALSE \| TRUE | 123<br/>68 | aaa<br/>ccc | {"id":HERO_2, "name":"JohnDoe 2"}                                        |
| text3                   | 4 \| 2      | BUILDING_3 \| HERO_1 \| HERO_2 | TRUE \| FALSE         | 123<br/>76 | aaa<br/>ccc | [{"id":HERO_1, "name":"John Doe 1"},{"id":HERO_2, "name":"Mary Sue 2"}] |
| text1 \| text2 \| text7 | 5           | 1 \| 2 \| 4 \| PET_5           | TRUE                  | 123<br/>78 | aaa<br/>ccc | [{"id":HERO_1, "name":"John Doe 1"},{"id":HERO_2, "name":"Mary Sue 2"}] |

- For array types, the column name must end with `[]`.
- For JSON object types, the column name must end with `{}`.

### 8.5. Configuration ScriptableObject

Interactive Excel and Google exports special-case only a worksheet named exactly `Configuration`. Matching is ordinal and case-sensitive. `Config`, `RemoteConfig`, `BattleConfig`, and `configuration` remain ordinary row-array JSON sheets controlled by normal selection and **Combine Json Sheets** rules.

Use this exact header after trimming cell text. Only first four columns are read; later columns are ignored.

```text
| Sub Class | Field Name | Type | Value |
```

- A non-empty **Sub Class** starts a nested group. Empty **Sub Class** rows continue current group.
- A fully blank row closes current group. Later rows with empty **Sub Class** become root fields.
- Empty **Sub Class** before any group is an error.
- Add `[]` only as display suffix to **Field Name**; SheetX removes only final suffix. Actual array shape comes from **Type**.
- Arrays use `|` between values. Supported type names are case-insensitive: `int`, `float`, `boolean`, `string`, `int-array`, `float-array`, `string-array`, `vector2`, and `vector3`.
- When Data Config Collections is disabled, duplicate group names, class names, fields, root fields, and root/group collisions are preserved in source order. Duplicate JSON keys remain in output. Generated C# can fail to compile; fix duplicate worksheet data.
- When Data Config Collections is enabled, exact `Configuration` uses strict duplicate and generated-symbol validation; any collision aborts Collection artifact flush before writes.

When Data Config Collections is disabled, interactive single-file export reads physical exact `Configuration` even when its serialized sheet entry is absent or unchecked. It writes fixed, unencrypted artifacts:

- JSON folder: `Configuration.txt`
- Scripts/Constants folder: `Configuration.cs`
- After script compilation: `Configuration.asset` beside generated script, only when no matching asset exists

Interactive multi-file export reads physical exact `Configuration` from each selected Excel workbook or Google spreadsheet in source-list order, ignores serialized sheet selection, keeps first header, appends later data rows, then writes one fixed artifact set. `Configuration` stays outside ordinary combined JSON. **Encrypt Json** does not affect `Configuration.txt`.

Generated C# uses configured namespace and contains **Load** inspector context menu. `autoLoad` starts enabled: after generated script reload, SheetX assigns Configuration JSON and loads values. Disable `autoLoad` to preserve serialized values until manually running **Load**.

SheetX finds existing Configuration assets by exact generated runtime type across project. Zero matching assets creates one beside script; one matching asset is reused even after move or rename; multiple matching assets log one `SheetX:` error and update none. Delete matching asset, then next export creates replacement.

When Data Config Collections is enabled, exact `Configuration` is automatic Global input. It ignores sheet selection and binding settings, writes plaintext `Configuration.txt` to Collection JSON Folder, emits direct fields and nested classes in `GlobalConfigCollection.cs`, and bakes values into `GlobalConfigCollection.asset`. It does not update `Configuration.cs` or `Configuration.asset`; existing standalone artifacts remain dormant and are not deleted. Strict duplicate, symbol, field, and value collisions abort complete Collection output with source/row diagnostics and a `Fix:` action. Runtime access remains:

```csharp
var global = GlobalConfigCollectionBase.Instance<GlobalConfigCollection>();
```

The generated `SheetXCollectionPaths.Configuration` constant marks active Configuration schema. If marker is absent, baker ignores stale Collection `Configuration.txt`. Detached `SheetXExporter` and batch APIs do not generate typed Configuration artifacts; both `Config` and `Configuration` remain ordinary row-array JSON there.

#### Special Data Type: Attributes List

| attribute0 | value0 | unlock0 | increase0 | max0 | attribute1 | value1[] | unlock1[] | increase1[] | max1[]   | ... | attributeN |
| ---------- | ------ | ------- | --------- | ---- | ---------- | -------- | --------- | ----------- | -------- | --- | ---------- |
| ATT_HP     | 30     | 2       | 1.2       | 8    |            |          |           |             |          | ... |            |
| ATT_AGI    | 25     | 3       | 1.5       | 8    |            |          |           |             |          | ... |            |
| ATT_INT    | 30     | 2       | 1         | 5    | ATT_CRIT   | 3 \| 2   | 0 \| 11   | 0.5 \| 1    | 10 \| 20 | ... |            |
| ATT_ATK    | 30     | 2       | 1         | 8    | ATT_CRIT   | 10 \| 1  | 1 \| 12   | 1.5 \| 1    | 10 \| 20 | ... |            |
|            |        |         |           |      | ATT_CRIT   | 10 \| 1  | 1 \| 12   | 1.5 \| 1    | 10 \| 20 | ... |            |

Attribute is a specific data type, specially created for RPG genre games - where characters and equipment can possess various different and non-fixed attributes and stats. This data type makes character and equipment customization more flexible, without restrictions.

![Attribute Example](https://github.com/nbhung100914/excel-to-unity/assets/9100041/2d619d56-5fa9-4371-b212-3e857bcbbead)

To define an attribute object type, the following rules should be followed:

- The attribute columns should be placed at the end of the data table.
- Attribute id is a constant integer, so it should be defined in the IDs sheet.
- An attribute has the following structure:

  1. **`attribute`**: The column name follows the pattern _`attribute + (index)`_, where index can be any number, but should start from 0 and increase. The value of this column is the id of the attribute (Integer), this value should be set in the IDs sheet.
  2. **`value`**: The column name follows the pattern _`value + (index)`_ or _`value + (index) + []`_, the value of the column can be a number or a number array.
  3. **`increase`**: The column name follows the pattern _`increase + (index)`_ or _`increase + (index) + []`_. This is an additional value, used for level-up situations (optional).
  4. **`unlock`**: The column name follows the pattern _`unlock + (index)`_ or _`unlock + (index) + []`_. This is an additional value, used for unlock conditions like minimum level (optional).
  5. **`max`**: The column name follows the pattern _`max + (index)`_ or _`max + (index) + []`_. This is an additional value, for maximum limits (optional).

    ```
    Example 1: attribute0, value0, increase0, value0, max0.
    Example 2: attribute1, value1[], increase1[], value1[], max1[].
    ```

## 9. How to Integrate

**Download and import the [Example](https://github.com/hnb-rabear/hnb-rabear.github.io/blob/main/sheetx/SheetXExample.unitypackage)**

After importing the example package, open the excel file located at `Assets/SheetXExample/Examples/Exporting an Excel File/Example.xlsx`. This is a sample Excel file identifying various types of data such as IDs, Constants, and Data Tables.

![Excel File](https://github.com/user-attachments/assets/2b4c8fe3-3c58-42bc-a85b-dea33c8122cf)

**For the example using Google Sheets, you can view the file here.**

Example for exporting single file
[**Example**](https://docs.google.com/spreadsheets/d/1_9BqoKwRsod5cMwML5n_pLpuWk045lD3Jd7nrizqVBo/edit?usp=drive_link)

Example for exporting multiple files
[**Example 1**](https://docs.google.com/spreadsheets/d/1l9_elk7QfABbWlKanOHqkSIYlWcxWO1EIPt9Ax4XtUE/edit?usp=drive_link)
[**Example 2**](https://docs.google.com/spreadsheets/d/1d53vWQzrp-qNsoeyEmkqQx4KeQObONOk55oWeNS2YXg/edit?usp=drive_link)
[**Example 3**](https://docs.google.com/spreadsheets/d/1i2CmDGYpAYuX_8vBUbHXBAhuWPKHi_gd52uwzsegLdY/edit?usp=drive_link)
[**Example 4**](https://docs.google.com/spreadsheets/d/1kq0KaQxQ129f1OABm62x6GtfOKTg_3t4M8gODGHzSu8/edit?usp=drive_link)

### 9.1. Create Folders for Exporting Files

Create 3 directories to store the files that will be exported:

- A folder to store the C# scripts (IDs, Constants, Localization Component, Localization API).
- A folder to store the JSON data files.
- A folder to store the Localization data.

  - There are two ways to set up the folder for Localization data, depending on how you want to load Localizations:
    - **Resources:** Create a folder inside the Resources folder (e.g., `Resources/Localizations`).
    - **Addressables:** Create a "Localizations" folder outside Resources and set it as an Addressable Asset.

- Navigate to `RCore > Tools > SheetX > Settings`
- In Sheets Exporter Settings, set up the paths for the "Scripts Output Folder," "Json Output Folder," and "Localization Output Folder" using the three folders you just created.

For this example I will create 3 folders:

- `Assets\SheetXExample\Scripts\Generated`: for C# scripts
- `Assets\SheetXExample\DataConfig`: for Json data
- `Assets\SheetXExample\Resources\Localizations`: for Localization data

### 9.2. Scripting

#### Create a ScriptableObject as Storage for Static Database

- Create Serializable classes that correspond to the data fields in the data tables.

```cs
[Serializable]
public class ExampleData1
{
    public int numberExample1;
    public int numberExample2;
    public int numberExample3;
    public bool boolExample;
    public string stringExample;
}
```

```cs
[Serializable]
public class ExampleData2
{
    [Serializable]
    public class Example
    {
        public int id;
        public string name;
    }

    public string[] array1;
    public int[] array2;
    public int[] array3;
    public bool[] array4;
    public int[] array5;
    public string[] array6;
    public Example json1;
}
```

```cs
[Serializable]
public class ExampleData3
{
    public int id;
    public string name;
    public List<Attribute> Attributes;
}

[Serializable]
public class Attribute
{
    //=== MAIN
    public int id;
    public float value;
    public int unlock;
    public float increase;
    public float max;
    //=== Optional
    public float[] values;
    public float[] increases;
    public float[] unlocks;
    public float[] maxes;
}
```

- Create a ScriptableObject to encapsulate the above Serializable classes.

```cs
[CreateAssetMenu(fileName = "ExampleDataCollection", menuName = "SheetXExample/Create ExampleDataCollection")]
public class ExampleDataCollection : ScriptableObject
{
    public List<ExampleData1> exampleData1s;
    public List<ExampleData2> exampleData2s;
    public List<ExampleData3> exampleData3s;
}
```

- Load Json Data into Serializable classes

```cs
// NOTE: This function utilizes the UnityEditor library and must be placed in the Editor directory or within #if UNITY_EDITOR directives.
// If you prefer not to use Editor code, you can alternatively store the JSON data files in the Resources directory or Asset Bundles and load them accordingly.
[ContextMenu("Load")]
private void LoadData()
{
    #if UNITY_EDITOR

    var txt = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Import/Json/ExampleData1.txt");
    exampleData1s = JsonConvert.DeserializeObject<List<ExampleData1>>(txt.text);

    txt = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/SheetXExample/DataConfig/ExampleData2.txt");
    exampleData2s = JsonConvert.DeserializeObject<List<ExampleData2>>(txt.text);

    txt = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/SheetXExample/DataConfig/ExampleData3.txt");
    exampleData3s = JsonConvert.DeserializeObject<List<ExampleData3>>(txt.text);

    #endif
}
```

![Example Data Collection](https://github.com/user-attachments/assets/8a0a1dc4-3cac-4c88-bd7e-a3bc2fa7b546)

![Example Data Collection](https://github.com/user-attachments/assets/23e9aec3-cfbd-416c-8459-66cbb0e2fb58)

### 9.3. Localization Integration

- Initialization

```cs
LocalizationManager.Init();
```

- Change the language.

```cs
// Set the language japanese
LocalizationsManager.CurrentLanguage = "jp";
```

- Register an event handler for the language change event.

```cs
// Register an action when language changed
LocalizationsManager.OnLanguageChanged += OnLanguageChanged;
```

#### Switch Language in Scene View

Switch generated localization text without entering Play Mode:

1. Open **Scene View**.
2. Enable **Localization** from **View > Overlays > Localization** if hidden.
3. Select a language from the dropdown, or use `<` and `>` to move between languages.

The overlay reloads generated localization data, then refreshes matching localization text components in all loaded scenes and open Prefab Mode. It requires generated SheetX localization classes; no template change or code regeneration is needed. If none exist, it shows `No localization class found`.

- You can retrieve localized content using three different methods.

  1. **Key Retrieval:** Retrieve localized content using a Key. Note that the text will not automatically refresh when the language changes using this method.

      ```cs
      // Retrieve localized text using an integer key
      m_simpleText1.text = LocalizationExample2.Get(LocalizationExample2.GO_TO_SHOP).ToString();
      // Retrieve localized text using an integer key with an argument
      m_simpleText2.text = LocalizationExample2.Get(LocalizationExample2.REQUIRED_CITY_LEVEL_X, 10).ToString();
      // Retrieve localized text using a string key with an argument
      m_simpleText3.text = LocalizationExample2.Get("REQUIRED_CITY_LEVEL_X", 25).ToString();
      ```

  2. **Dynamic Binding:** Link a GameObject containing a Text or TextMeshProUGUI component with a key so that the text automatically updates when the language changes.

      ```cs
      // Register dynamic localized text using an integer key
      LocalizationExample2.RegisterDynamicText(m_dynamicText1.gameObject, LocalizationExample2.TAP_TO_COLLECT);
      // Register dynamic localized text using an integer key with an argument
      LocalizationExample2.RegisterDynamicText(m_dynamicText2.gameObject, LocalizationExample2.REQUIRED_LEVEL_X, "3");
      // Register dynamic localized text using a string key with an argument
      LocalizationExample2.RegisterDynamicText(m_dynamicText3.gameObject, "REQUIRED_LEVEL_X", "30");
      // Unregister the gameObject
      Localization.UnregisterDynamicText(m_textGameObject1);
      Localization.UnregisterDynamicText(m_textGameObject2);
      Localization.UnregisterDynamicText(m_dynamicText3);
      ```

  3. **Component:** Using Localization Component.

      ![Using Localization Component](https://github.com/user-attachments/assets/0f0214b9-51ed-44bf-9b27-f2a210e6f0f6)

#### Combine Localizations

If you want to combine all Localization Sheets, simply deselect the "Separate Localization Sheets" checkbox in the Settings. Next, delete all generated files and re-export everything.

Then, replace instances of **LocalizationExample1** and **LocalizationExample2** with **Localization**. Also, replace component **LocalizationExample1Text** and **LocalizationExample2Text** with **LocalizationText**.

#### Creating TextMeshPro Fonts for Different Languages

To create TextMeshPro fonts for Japanese, Korean, and Chinese, follow these steps using the respective character set files `characters_set_jp`, `characters_set_ko`, and `characters_set_cn`, which include all characters from the localization sheets:

Fonts to use in this example:

- Japanese: NotoSerif-Bold
- Korean: NotoSerifJP-Bold
- Chinese: NotoSerifTC-Bold

Creating TextMeshPro Fonts:

- For each language font, create a TextMeshPro font asset.
- Open the Font Asset Creator window in Unity.
- Under the *Character Set* section, select *Character From File*.
- Choose the appropriate character set file (e.g., characters_set_jp) in the Character File section.

![Create Japanese font](https://github.com/user-attachments/assets/7bc98c77-9994-4551-8e5a-dae51eba9f45)

![Create Korean font](https://github.com/user-attachments/assets/dc14fbbb-b38f-4f56-89b0-844d94b825cb)

![Create Chinese font](https://github.com/user-attachments/assets/08020e00-14b1-47cd-a9f2-be3d4321ca48)

#### Loading Localization Using the Addressable Assets System

To utilize this feature, follow these steps:

- Install the Addressable Assets System.
- Add `ADDRESSABLES` to the directives list in the Build Settings.
- Move the Localizations folder out of the Resources folder. Additionally, relocate the Output folder in the SheetX Settings window.
- Set the Localizations folder as an Addressable Asset.

![SheetX Settings](https://github.com/user-attachments/assets/ee17fdaa-c951-4f9c-8a6b-a5e2614db546)

![Localizations Folder](https://github.com/user-attachments/assets/1ecf2ae1-00e9-4c9f-9056-2867d04e8ee1)

![Build Settings](https://github.com/user-attachments/assets/229da607-da10-4b87-b799-5d9549e5620d)