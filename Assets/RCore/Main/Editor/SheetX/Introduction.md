__Download via [Releases page](https://github.com/nbhung100914/excel-to-unity/releases)__

__You can find more detailed instructions and the demo project [Here](https://github.com/nbhung100914/excel-to-unity-example)__

---
# 1. Introduction

This tool simplifies database design and management for game developers and designers, allowing easy modification of game statistics without needing developer help.

As game projects grow, so does the need for efficient data table, constant, and ID management. This tool centralizes the process, enabling easy search, modification, and updates.

Originally designed for RPGs with extensive databases, it now supports various game genres and utilizes Excel and Google Spreadsheets for data management.

---
# 2. Main functions

- **Excel and Google Sheets Integration:** Manage your entire database using Excel or Google Spreadsheets.
- **ID and Constant Management:** Make batch adjustments to IDs and constants without impacting the database.
- **Localization System:** Effortlessly handle multiple languages, with seamless Unity integration.
- **JSON Export:** Convert data tables to JSON files for easy Unity integration.
- **Flexible Data Formats:** Support a variety of data formats, adaptable to your design needs.

---
# 3. Excel Sheets Exporter

## 3.1. Export Single Excel File

Navigate to the main menu and select: RCore > Tools > SheetX > Excel Sheets Exporter

![SheetXExcel1](https://github.com/user-attachments/assets/b2349e9f-2599-40bb-9c3b-452b9944d080)

This essential function is designed to help you familiarize yourself with the tools. It's perfect for smaller, less complex Static Databases that only require a single Excel file to contain all the data.

Key Functions:

- **Export IDs:** Converts ID sheets to C# files.
- **Export Constants:** Converts Constants sheets to C# files.
- **Export Json:** Transforms Data Table sheets into JSON data.
- **Export Localization:** Exports Localization Data, Localization Components, and Localization API.
- **Export All:** Performs all the functions with a single click.

## 3.2. Export multiple Excel Files

![SheetXExcel2](https://github.com/user-attachments/assets/faf954f1-86fa-4a43-9e52-04ac019faa98)

For managing complex Static Databases that need to be divided into multiple Excel files, this feature is essential. It allows you to efficiently handle and export all your Excel files with a single button press:

1. Add all the Excel files you wish to process.
2. For each Excel file, you have the option to choose which sheets to include or exclude.
3. Press the Export All button to complete the process.

---
# 4. Google Spreadsheets

Prefer using Google Spreadsheets? No problem.

## 4.1. Setup Instructions

### Step 1: Enable Google Sheets API
1. Visit the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project or select an existing one.
3. Click on **Go to APIs overview**.
4. Select **Enable APIs and Services**.
5. Search for and select **Google Sheets API**, then click **Enable**.

### Step 2: Obtain Credentials
1. On the top Google Sheets API screen, click on **Create Credentials**.
2. Choose **Google Sheets API**, **User data**, then click **Next**.
3. In the Scopes section, click on **Add or remove scopes**.
4. Find and select the **Google Sheets API** (description: "See all your Google Sheets Spreadsheets"), then **Save and Continue**.
5. In the OAuth Client ID section, select Application Type as Desktop App, enter any name, then click **Create**.
6. Click **Done**.

### Step 3: Accessing Your Client ID and Client Secret:
1. On the Google Sheets API screen, go to the **Credentials** tab, you will find the new Client ID.
2. Click on the Edit button to find the Client ID and Client Secret.
3. Copy the **Client ID** and **Client Secret**, and paste them into the corresponding settings in the SheetX Settings Window

## 4.2. Export single Google Spreadsheet

![SheetXGoogle1](https://github.com/user-attachments/assets/cda3989b-a4af-491d-80e3-3f41c13a6ff6)

Enter the Google Sheet ID, then click the Download button. You can find the ID in the Google Sheet's URL, formatted like this: 
```
https://docs.google.com/spreadsheets/d/[google-sheet-id]/edit?......
```

## 4.3. Export multiple Google Spreadsheets

![SheetXGoogle2](https://github.com/user-attachments/assets/66a750f1-1997-4c6c-ac1b-b3194fb11167)

![SheetXEditGoogleSheet](https://github.com/user-attachments/assets/ac74f8a0-d59a-4d96-886f-9212395509dc)

## 5. Settings

![SheetXSettings](https://github.com/user-attachments/assets/206194b4-cd6c-4397-bd35-c49ecabf7aa2)

- **Constants Output Folder:** Stores exported C# scripts, including IDs, Constants, Localization Components, and Localization API.
- **Json Output Folder:** Stores exported JSON data.
- **Localization Output:** Stores Localization Data, which should be inside the Resources folder for loading via Resources, or in the Localizations folder for loading via Addressable Asset System.
- **Namespace:** Defines the namespace for the exported C# files.
- **Separate IDs: Sheets**
  - TRUE: Exports _[%IDs]_ sheets to individual C# files named _[SheetName] + IDs.cs_.
  - FALSE: Merges all _[%IDs]_ sheets from all Excel files into a single C# file named _IDs.cs._
- **Separate Constants: Sheets**
  - TRUE: Exports _[%Constants]_ sheets to individual C# files named _[SheetName] + %Constants.cs_.
  - FALSE: Merges all _[%Constants]_ sheets from all Excel files into a single C# file named _Constants.cs_.
- **Separate Localization Sheets:**
  - TRUE (default): Exports _[Localization%]_ sheets to separate groups, each containing Localization Data, Component, and API, with the following file name structure:
    - Localization Data: _[SheetName]\_[language].txt_
    - Component: _[SheetName] + Text.cs_
    - API: _[SheetName].cs_
  - FALSE: Merges all _[Localization%]_ sheets from all Excel files into a single group, with the following file name structure:
    - Localization Data: _Localization\_ + [language].txt_
    - Component: _LocalizationText.cs_
    - API: _Localization.cs_
- **Only enum as IDs:** For _[%IDs]_ sheets, columns with the extension _[enum]_ will be exported as enums and will not include the Integer Constant form.
- **Combine Json Sheets:** Merges the Data Table from one Excel file into a single JSON file, named _[ExcelName].txt_.
- **Language Char Sets:** Used in Localization with TextMeshPro to compile the character table of a language, mainly applied for Korean, Japanese, and Chinese due to their extensive character systems.
- **Persistent columns:** Specifies the names of columns to retain during processing even if they are empty.
- **Google Client ID:** Enter your Google Client ID (retrieved from Credentials in Google Console).
- **Google Client Secret:** Enter your Google Secret (retrieved from Credentials in Google Console).

## 3.5. Encrypt & Decrypt Text

![excel-2-unity-tab-4](https://github.com/user-attachments/assets/f7e50d41-6cac-4f32-9251-91589652c1ca)

This function allows you to encrypt or decrypt a string of characters based on the Key provided in the Settings Tab. You can use this function to secure the content of a text, or to open and read the encrypted JSON Data files after they have been exported.

# 4. Data Design Rules in Spreadsheet

## 4.1. IDs

[Download detail example from GitHub](https://github.com/nbhung100914/excel-to-unity/blob/main/Example.xlsx)

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

Sheets named according to the syntax _[%IDs]_ are called IDs sheets. They are used to compile all ids into Integer Constants. The design rules are as follows:

- The sheet name needs to have `IDs` as a prefix or suffix.
- In this Sheet, only use the Integer data type.
- Each group is arranged in 3 consecutive columns.
- The first row contains the group name for easy lookup.
- The first column contains the Key Name, and the next column contains the Key Value.
- Key Value must be an integer.
- By default, all ids of a column will be exported as Integer Constants, but you can also export them as enum by adding the suffix [enum] to the group name.
- You can choose to only export enum and ignore Integer Constant by selecting `Only enum as IDs` in the Settings section.

```
| Group | Key | Comment |
| ----- | --- | ------- |
```

## 4.2. Constants

| Name                  | Type        | Value              | Comment               |
| --------------------- | ----------- | ------------------ | --------------------- |
| EXAMPLE_INT           | int         | 83                 | Integer Example       |
| EXAMPLE_FLOAT         | float       | 1.021              | Float example         |
| EXAMPLE_STRING        | string      | 321fda             | String example        |
| EXAMPLE_INTARRAY_1    | int-array   | 4                  | Integer array example |
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

Sheets named according to the syntax _[%Constants]_ are called Constants Sheets. They are used to compile the Constants in the project. The table below will help you refer to all the data types that can be used in this sheet. The design rules are as follows:

- The sheet name needs to have `Constants` as a prefix or suffix.
- There are four columns: Name, Type, Value, and Comment.
- Name: This is the name of the constant, it must be written continuously, does not contain special characters, and should be capitalized.
- Type: This is the data type of the constant. You can use the following data types: `int`, `float`, `bool`, `string`, `int-array`, `float-array`, `vector2`, and `vector3`.
- Value: The value corresponding to the data type. For array data types, elements must be separated by `:` or `|` or `newline`.

```
| Name | Type | Value | Comment |
| ---- | ---- | ----- | ------- |
```

## 4.3. Localization

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

Sheets named according to the syntax _[%Localization%]_ are called Localization Sheets. The design rules are as follows:

- The sheet name needs to have `Localization` as a prefix or suffix.
- This sheet has a structure of 2 key columns, one is the main key `idString` and one is the additional key `relativeId`.
- The next columns will contain localized content.
- The key of a row is the combination of `idString` and `relativeId`.

```
| idString | relativeId | english | spanish | japan | .... |
| -------- | ---------- | ------- | ------- | ----- | ---- |
```

# 4.4 Data table - JSON Data

### Basic data type: Boolean, Number, String

| numberExample1 | numberExample2 | numberExample3 | boolExample | stringExample |
| -------------- | -------------- | -------------- | ----------- | ------------- |
| 1              | 10             | 1.2            | TRUE        | text          |
| 2              | 20             | 3.1            | TRUE        | text          |
| 3              | BUILDING_8     | 5              | FALSE       | text          |
| 6              | HERO_3         | 10.7           | FALSE       | text          |
| 9              | PET_2          | 16.4           | FALSE       | text          |

### Extended data type: Array, JSON object

| array1[]                | array2[]    | array3[]                       | array4[]              | array5[]   | array6[]    | JSON\{}                                                                   |
| ----------------------- | ----------- | ------------------------------ | --------------------- | ---------- | ----------- | ------------------------------------------------------------------------- |
| text1                   | 1           | 1                              | TRUE                  | 123<br/>66 | aaa<br/>ccc | \{}                                                                       |
| text2                   | 2 \| 2 \| 3 | 1 \| 2 \| 3                    | TRUE \| FALSE \| TRUE | 123<br/>71 | aaa<br/>ccc | \{"id":1, "name":"John Doe 1"}                                            |
| text1 \| text2          | 1 \| 2      | 1 \| BUILDING_2                | TRUE \| FALSE         | 123<br/>67 | aaa<br/>ccc | \{"id":2, "name":"John Doe 2"}                                            |
| text1 \| text2 \| text3 | 1 \| 2 \| 3 | BUILDING_1 \| HERO_2           | TRUE \| FALSE \| TRUE | 123<br/>68 | aaa<br/>ccc | \{"id":HERO_2, "name":"JohnDoe 2"}                                        |
| text3                   | 4 \| 2      | BUILDING_3 \| HERO_1 \| HERO_2 | TRUE \| FALSE         | 123<br/>76 | aaa<br/>ccc | [\{"id":HERO_1, "name":"John Doe 1"},\{"id":HERO_2, "name":"Mary Sue 2"}] |
| text1 \| text2 \| text7 | 5           | 1 \| 2 \| 4 \| PET_5           | TRUE                  | 123<br/>78 | aaa<br/>ccc | [\{"id":HERO_1, "name":"John Doe 1"},\{"id":HERO_2, "name":"Mary Sue 2"}] |

- For array type, the column name must have a suffix [].
- For JSON object type, the column name must have a suffix \{}.

### Special data type: Attributes list

| attribute0 | value0 | unlock0 | increase0 | max0 | attribute1 | value1[] | unlock1[] | increase1[] | max1[]   | ... | attributeN |
| ---------- | ------ | ------- | --------- | ---- | ---------- | -------- | --------- | ----------- | -------- | --- | ---------- |
| ATT_HP     | 30     | 2       | 1.2       | 8    |            |          |           |             |          | ... |            |
| ATT_AGI    | 25     | 3       | 1.5       | 8    |            |          |           |             |          | ... |            |
| ATT_INT    | 30     | 2       | 1         | 5    | ATT_CRIT   | 3 \| 2   | 0 \| 11   | 0.5 \| 1    | 10 \| 20 | ... |            |
| ATT_ATK    | 30     | 2       | 1         | 8    | ATT_CRIT   | 10 \| 1  | 1 \| 12   | 1.5 \| 1    | 10 \| 20 | ... |            |
|            |        |         |           |      | ATT_CRIT   | 10 \| 1  | 1 \| 12   | 1.5 \| 1    | 10 \| 20 | ... |            |

Attribute is a specific data type, specially created for RPG genre games - where characters and equipment can possess various different and non-fixed attributes and stats. This data type makes character and equipment customization more flexible, without restrictions.

![attribute example](https://github.com/nbhung100914/excel-to-unity/assets/9100041/2d619d56-5fa9-4371-b212-3e857bcbbead)

To define an attribute object type, the following rules should be followed:

- The attribute column should be placed at the end of the data table.
- Attribute id is a constant integer, so it should be defined in the IDs sheet.
- An attribute has the following structure:

  1. **`attribute`**: The column name follows the pattern _`attribute + (index)`_, where index can be any number, but should start from 0 and increase. The value of this column is the id of the attribute, which is an Integer type, this value should be set in the IDs sheet.
  2. **`value`**: The column name follows the pattern _`value + (index)`_ or _`value + (index) + []`_, the value of the column can be a number or a number array.
  3. **`increase`**: The column name follows the pattern _`increase + (index)`_ or _`increase + (index) + []`_. This is an additional value, which can be present or not, usually used for level-up situations, specifying the additional increase when a character or item levels up.
  4. **`unlock`**: The column name follows the pattern _`unlock + (index)`_ or _`unlock + (index) + []`_. This is an additional value, which can be present or not, usually used for situations where the attribute needs conditions to be unlocked, such as minimum level or minimum rank.
  5. **`max`**: The column name follows the pattern _`max + (index)`_ or _`max + (index) + []`_. This is an additional value, which can be present or not, usually used for situations where the attribute has a maximum value.

    ```
    Example 1: attribute0, value0, increase0, value0, max0.
    Example 2: attribute1, value1[], increase1[], value1[], max1[].
    ```

# 5. Setup for a Unity Project

You can view the demo project [Here](https://github.com/nbhung100914/excel-to-unity-example)
