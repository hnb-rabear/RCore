# SheetX Document

## 1. Introduction

This tool simplifies database design and management for game developers and designers, allowing easy modification of game statistics without needing developer help.

As game projects grow, so does the need for efficient data table, constant, and ID management. This tool centralizes the process, enabling easy search, modification, and updates.

Originally designed for RPGs with extensive databases, it now supports various game genres and utilizes Excel and Google Spreadsheets for data management.

__You can download Example [Here](https://github.com/hnb-rabear/hnb-rabear.github.io/blob/main/sheetx/SheetXExample.unitypackage)__

## 2. Main functions

- __Excel and Google Sheets Integration:__ Manage your entire database using Excel or Google Spreadsheets.
- __ID and Constant Management:__ Make batch adjustments to IDs and constants without impacting the database.
- __Localization System:__ Effortlessly handle multiple languages, with seamless Unity integration.
- __JSON Export:__ Convert data tables to JSON files for easy Unity integration.
- __Flexible Data Formats:__ Support a variety of data formats, adaptable to your design needs.

## 3. Settings

Navigate to the main menu and select: `RCore > Tools > SheetX > Settings`

![tab_settings](https://github.com/user-attachments/assets/8d339afe-3323-4f03-99d0-34b3cc7dc56e)

- __Scripts Output Folder:__ Stores exported C# scripts, including IDs, Constants, Localization Components, and Localization API.
- __Json Output Folder:__ Stores exported JSON data.
- __Localization Output:__ Stores Localization Data, which should be inside the Resources folder for loading via Resources, or in the Localizations folder for loading via Addressable Asset System.
- __Namespace:__ Defines the namespace for the exported C# files.
- __Separate IDs: Sheets__
  - TRUE: Exports _[%IDs]_ sheets to individual C# files named _[SheetName] + IDs.cs_.
  - FALSE: Merges all _[%IDs]_ sheets from all Excel files into a single C# file named _IDs.cs._
- __Separate Constants: Sheets__
  - TRUE: Exports _[%Constants]_ sheets to individual C# files named _[SheetName] + %Constants.cs_.
  - FALSE: Merges all _[%Constants]_ sheets from all Excel files into a single C# file named _Constants.cs_.
- __Separate Localization Sheets:__
  - TRUE (default): Exports _[Localization%]_ sheets to separate groups, each containing Localization Data, Component, and API, with the following file name structure:
    - Localization Data: _[SheetName]\_[language].txt_
    - Component: _[SheetName] + Text.cs_
    - API: _[SheetName].cs_
  - FALSE: Merges all _[Localization%]_ sheets from all Excel files into a single group, with the following file name structure:
    - Localization Data: _Localization\_ + [language].txt_
    - Component: _LocalizationText.cs_
    - API: _Localization.cs_
- __Only enum as IDs:__ For _[%IDs]_ sheets, columns with the extension _[enum]_ will be exported as enums and will not include the Integer Constant form.
- __Combine Json Sheets:__ Merges the Data Table from one Excel file into a single JSON file, named _[ExcelName].txt_.
- __Language Char Sets:__ Used in Localization with TextMeshPro to compile the character table of a language, mainly applied for Korean, Japanese, and Chinese due to their extensive character systems.
- __Persistent columns:__ Specifies the names of columns to retain during processing even if they are empty.
- __Google Client ID:__ Enter your Google Client ID (retrieved from Credentials in Google Console).
- __Google Client Secret:__ Enter your Google Secret (retrieved from Credentials in Google Console).

## 4. Excel Sheets Exporter

### 4.1. Export Single Excel File

Navigate to the main menu and select: `RCore > Tools > SheetX > Excel Spreadsheets`

![tab_excel_1](https://github.com/user-attachments/assets/b8ef6183-21c6-43b9-b952-8b5d57fc4c0b)

This function is ideal for learning how to use the tools. It's great for small, simple Static Databases that only need one Excel file for all the data.

Key Functions:

- __Export IDs:__ Converts ID sheets to C# files.
- __Export Constants:__ Converts Constants sheets to C# files.
- __Export Json:__ Transforms Data Table sheets into JSON data.
- __Export Localization:__ Exports Localization Data, Localization Components, and Localization API.
- __Export All:__ Performs all the functions with a single click.

### 4.2. Export multiple Excel Files (PRO only)

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
3. Click on __Go to APIs overview__.
4. Select __Enable APIs and Services__.
5. Search for and select __Google Sheets API__, then click __Enable__.

#### Step 2: Obtain Credentials

1. On the top Google Sheets API screen, click on __Create Credentials__.
2. Choose __Google Sheets API__, __User data__, then click __Next__.
3. In the Scopes section, click on __Add or remove scopes__.
4. Find and select the __Google Sheets API__ (description: "See all your Google Sheets Spreadsheets"), then __Save and Continue__.
5. In the OAuth Client ID section, select Application Type as Desktop App, enter any name, then click __Create__.
6. Click __Done__.

#### Step 3: Accessing Your Client ID and Client Secret:

1. On the Google Sheets API screen, go to the __Credentials__ tab, you will find the new Client ID.
2. Click on the Edit button to find the Client ID and Client Secret.
3. Copy the __Client ID__ and __Client Secret__, and paste them into the corresponding settings in the __Sheets Exporter Settings__ Window

![tab_settings_2](https://github.com/user-attachments/assets/4140a3e8-05df-4bbe-a3b8-a2fb0576f1ee)

### 5.2. Export single Google Spreadsheet

![tab_google_1](https://github.com/user-attachments/assets/02d6e2a9-3c39-4087-9a1e-0e77eec73a19)

Enter the Google Sheet ID, then click the Download button. You can find the ID in the Google Sheet's URL, formatted like this: 

```url
https://docs.google.com/spreadsheets/d/[GOOGLE_SHEET_ID]/edit?......
```

### 5.3. Export multiple Google Spreadsheets (PRO only)

Click on __Add Google Spreadsheets__, then enter the Google Sheet ID in the popup that appears. Press __Download__, then select the sheets you want to process.

![tab_google_2](https://github.com/user-attachments/assets/f2ba3d93-7785-42b3-b33b-13b2687f699f)
![tab_google_2_edit](https://github.com/user-attachments/assets/3386dda3-a2ba-4f88-87d0-f25e43ebfa56)

## 6. Rules in Spreadsheet

### 6.1. IDs

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

ID Sheets, named with the suffix `IDs` are used to compile all IDs into Integer Constants. The design rules are:

- The sheet name must end with `IDs`.
- Only the Integer data type is allowed.
- Each group is organized in 3 consecutive columns.
- The first row contains the group name for easy reference.
- The first column holds the Key Name, and the next column holds the Key Value.
- Key Value must be an integer.
- By default, all IDs in a column will be exported as Integer Constants. Add the suffix [enum] to the group name to export them as an enum.
- To only export enums and skip Integer Constants, select `Only enum as IDs` in the Settings.

```a
| Group | Key | Comment |
| ----- | --- | ------- |
```

### 6.2. Constants

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
  - Name: The name of the constant; must be continuous, without special characters.
  - Type: The data type of the constant. Possible data types include: `int`, `float`, `bool`, `string`, `int-array`, `float-array`, `vector2`, and `vector3`.
  - Value: The value matching the data type. For array types, separate elements with `:` or `|` or `newline`.

```a
| Name | Type | Value | Comment |
| ---- | ---- | ----- | ------- |
```

### 6.3. Localization

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

- TThe sheet name must start with `Localization`.
- Each sheet has two key columns: the main key `idString` and an additional key `relativeId`.
- The following columns contain localized content.
- The key for each row is a combination of `idString` and `relativeId`.
- `relativeId` can reference an ID from the IDs sheets.

```a
| idString | relativeId | english | spanish | japan | .... |
| -------- | ---------- | ------- | ------- | ----- | ---- |
```

### 6.4. Data table - JSON Data

#### Basic data type: Boolean, Number, String

| numberExample1 | numberExample2 | numberExample3 | boolExample | stringExample |
| -------------- | -------------- | -------------- | ----------- | ------------- |
| 1              | 10             | 1.2            | TRUE        | text          |
| 2              | 20             | 3.1            | TRUE        | text          |
| 3              | BUILDING_8     | 5              | FALSE       | text          |
| 6              | HERO_3         | 10.7           | FALSE       | text          |
| 9              | PET_2          | 16.4           | FALSE       | text          |

#### Extended data type: Array, JSON object

| array1[]                | array2[]    | array3[]                       | array4[]              | array5[]   | array6[]    | JSON\{}                                                                   |
| ----------------------- | ----------- | ------------------------------ | --------------------- | ---------- | ----------- | ------------------------------------------------------------------------- |
| text1                   | 1           | 1                              | TRUE                  | 123<br/>66 | aaa<br/>ccc | \{}                                                                       |
| text2                   | 2 \| 2 \| 3 | 1 \| 2 \| 3                    | TRUE \| FALSE \| TRUE | 123<br/>71 | aaa<br/>ccc | \{"id":1, "name":"John Doe 1"}                                            |
| text1 \| text2          | 1 \| 2      | 1 \| BUILDING_2                | TRUE \| FALSE         | 123<br/>67 | aaa<br/>ccc | \{"id":2, "name":"John Doe 2"}                                            |
| text1 \| text2 \| text3 | 1 \| 2 \| 3 | BUILDING_1 \| HERO_2           | TRUE \| FALSE \| TRUE | 123<br/>68 | aaa<br/>ccc | \{"id":HERO_2, "name":"JohnDoe 2"}                                        |
| text3                   | 4 \| 2      | BUILDING_3 \| HERO_1 \| HERO_2 | TRUE \| FALSE         | 123<br/>76 | aaa<br/>ccc | [\{"id":HERO_1, "name":"John Doe 1"},\{"id":HERO_2, "name":"Mary Sue 2"}] |
| text1 \| text2 \| text7 | 5           | 1 \| 2 \| 4 \| PET_5           | TRUE                  | 123<br/>78 | aaa<br/>ccc | [\{"id":HERO_1, "name":"John Doe 1"},\{"id":HERO_2, "name":"Mary Sue 2"}] |

- For array types, the column name must end with `[]`.
- For JSON object types, the column name must end with `{}`.

#### Special data type: Attributes list

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

  1. __`attribute`__: The column name follows the pattern _`attribute + (index)`_, where index can be any number, but should start from 0 and increase. The value of this column is the id of the attribute, which is an Integer type, this value should be set in the IDs sheet.
  2. __`value`__: The column name follows the pattern _`value + (index)`_ or _`value + (index) + []`_, the value of the column can be a number or a number array.
  3. __`increase`__: The column name follows the pattern _`increase + (index)`_ or _`increase + (index) + []`_. This is an additional value, which can be present or not, usually used for level-up situations, specifying the additional increase when a character or item levels up.
  4. __`unlock`__: The column name follows the pattern _`unlock + (index)`_ or _`unlock + (index) + []`_. This is an additional value, which can be present or not, usually used for situations where the attribute needs conditions to be unlocked, such as minimum level or minimum rank.
  5. __`max`__: The column name follows the pattern _`max + (index)`_ or _`max + (index) + []`_. This is an additional value, which can be present or not, usually used for situations where the attribute has a maximum value.

    ```a
    Example 1: attribute0, value0, increase0, value0, max0.
    Example 2: attribute1, value1[], increase1[], value1[], max1[].
    ```

## 7. How to integration

__Download and import the [Example](https://github.com/hnb-rabear/hnb-rabear.github.io/blob/main/sheetx/SheetXExample.unitypackage)__

First, open the excel file located at `/Assets/SheetX/Examples/Exporting a Single Excel/Example.xlsx`. This is a sample Excel file. Within this file, there are sheets containing sample data that will help you understand how to design various types of data such as IDs, Constants, and Data Tables.

![Excel File](https://github.com/user-attachments/assets/2b4c8fe3-3c58-42bc-a85b-dea33c8122cf)

__For the example using Google Sheets, you can view the file here.__

Example for exporting single file
[__Example__](https://docs.google.com/spreadsheets/d/1_9BqoKwRsod5cMwML5n_pLpuWk045lD3Jd7nrizqVBo/edit?usp=drive_link)

Example for exporting multiple files
[__Example 1__](https://docs.google.com/spreadsheets/d/1l9_elk7QfABbWlKanOHqkSIYlWcxWO1EIPt9Ax4XtUE/edit?usp=drive_link)
[__Example 2__](https://docs.google.com/spreadsheets/d/1d53vWQzrp-qNsoeyEmkqQx4KeQObONOk55oWeNS2YXg/edit?usp=drive_link)
[__Example 3__](https://docs.google.com/spreadsheets/d/1i2CmDGYpAYuX_8vBUbHXBAhuWPKHi_gd52uwzsegLdY/edit?usp=drive_link)
[__Example 4__](https://docs.google.com/spreadsheets/d/1kq0KaQxQ129f1OABm62x6GtfOKTg_3t4M8gODGHzSu8/edit?usp=drive_link)

### 7.1. Create folders for exporting files

Create 3 directories to store the files that will be exported:

- A folder to store the C# scripts (IDs, Constants, Localization Component, Localization API).
- A folder to store the JSON data files.
- A folder to store the Localization data.

  - There are two ways to set up the folder for Localization data, depending on how you want to load Localizations:
    - The easiest method is to load from the Resources folder. Create a folder inside the Resources folder to store Localization data. You can name this folder anything you like.
    - Alternatively, use the Addressable Asset System. In this case, create a "Localizations" folder outside the Resources folder and set it as an Addressable Asset. It's recommended to name this folder "Localizations".

- Navigate to `RCore > Tools > SheetX > Settings`
- In Sheets Exporter Settings, set up the paths for the "Scripts Output Folder," "Json Output Folder," and "Localization Output Folder" using the three folders you just created.

For this example I will create 3 folders:

- `Assets\SheetXExample\Scripts\Generated`: for C# scripts
- `Assets\SheetXExample\DataConfig`: for Json data
- `Assets\SheetXExample\Resources\Localizations`: for Localization data

### 7.2. Scripting

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

### 7.3. Localization integration

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

- You can retrieve localized content using three different methods.

  1. Retrieve localized content using a Key. Note that the text will not automatically refresh when the language changes using this method.

      ```cs
      // Retrieve localized text using an integer key
      m_simpleText1.text = LocalizationExample2.Get(LocalizationExample2.GO_TO_SHOP).ToString();
      // Retrieve localized text using an integer key with an argument
      m_simpleText2.text = LocalizationExample2.Get(LocalizationExample2.REQUIRED_CITY_LEVEL_X, 10).ToString();
      // Retrieve localized text using a string key with an argument
      m_simpleText3.text = LocalizationExample2.Get("REQUIRED_CITY_LEVEL_X", 25).ToString();
      ```

  2. Link a GameObject containing a Text or TextMeshProUGUI component with a key so that the text automatically updates when the language changes.

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

  3. Using Localization Component.

      ![Using Localization Component](https://github.com/user-attachments/assets/0f0214b9-51ed-44bf-9b27-f2a210e6f0f6)

#### Combine Localizations

If you want to combine all Localization Sheets, simply deselect the "Separate Localization Sheets" checkbox in the Settings. Next, delete all generated files and re-export everything.

Then, replace instances of __LocalizationExample1__ and __LocalizationExample2__ with __Localization__. Also, replace component __LocalizationExample1Text__ and __LocalizationExample2Text__ with __LocalizationText__.

#### Creating TextMeshPro Fonts for Different Languages

To create TextMeshPro fonts for Japanese, Korean, and Chinese, follow these steps using the respective character set files __characters_set_jp__, __characters_set_ko__, and __characters_set_cn__, which include all characters from the localization sheets:

Fonts to use in this example:

- Japanese: NotoSerif-Bold
- Korean: NotoSerifJP-Bold
- Chinese: NotoSerifTC-Bold

Creating TextMeshPro Fonts:

- For each language font, create a TextMeshPro font asset.
- Open the Font Asset Creator window in Unity.
- Under the _Character Set_ section, select _Character From File_.
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