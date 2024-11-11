**This project is a simple guide to help you understand the [Excel To Unity tool](https://github.com/nbhung100914/excel-to-unity).**

# Getting Started

This repository consists of two main parts:

- Basic part: This section will help you understand more about designing data to use Excel To Unity.
- Advanced part: In this section, we will explore how a real mid-core RPG game uses this tool.

However, in this article, I will only be discussing the basic part. The advanced part will be supplemented later.

## 1. Structure of the Excel File

First, open the excel file located at `/Assets/Basic/Data/Example.xlsx`. This is a sample Excel file. Within this file, there are sheets containing sample data that will help you understand how to design various types of data such as IDs, Constants, and Data Tables.

![excel-to-unity-basic-excel-file](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/87454ce5-e7a3-489d-8ffe-2cecb647622c)

### Constants:

```
| Name | Type | Value | Comment |
| ---- | ---- | ----- | ------- |
```

- The sheet name needs to have `Constants` as a prefix or suffix.
- There are four columns: Name, Type, Value, and Comment.
- Name: This is the name of the constant, it must be written continuously, does not contain special characters, and should be capitalized.
- Type: This is the data type of the constant. You can use the following data types: `int`, `float`, `bool`, `string`, `int-array`, `float-array`, `vector2`, and `vector3`.
- Value: The value corresponding to the data type. For array data types, elements must be separated by `:` or `|` or `newline`.

### IDs:

```
| Group | Key | Comment |
| ----- | --- | ------- |
```

- The sheet name needs to have `IDs` as a prefix or suffix.
- In this Sheet, only use the Integer data type.
- Each group is arranged in 3 consecutive columns.
- The first row contains the group name for easy lookup.
- The first column contains the Key Name, and the next column contains the Key Value.
- Key Value must be an integer.
- By default, all ids of a column will be exported as Integer Constants, but you can also export them as enum by adding the suffix [enum] to the group name.
- You can choose to only export enum and ignore Integer Constant by selecting `Only enum as IDs` in the Settings section.

### Localization:

```
| idString | relativeId | english | spanish | japan | .... |
| -------- | ---------- | ------- | ------- | ----- | ---- |
```

- The sheet name must have `Localization` as a prefix or suffix.
- This sheet has a structure that includes 2 key columns, one is the main key `idString` and the other is the additional key `relativeId`.
- The following columns will contain localized content.
- The key of a row is a combination of `idString` and `relativeId`.

```
For example, if idString is "hero_name" and relativeId is 1, then the key will be hero_name_1
```

- `relativeId` can reference the id of the IDs sheet.

### Data Table:

- The name of the data table sheet should not contain the strings `IDs`, `Constants`, and `Localization`.
- This sheet can use the following data types: `number`, `string`, `boolean`, `list/array`, `JSON object`, and `attribute object`.
- The first row is used to name the data fields, columns without a name will be skipped when exporting Json data.
- Add a `[]` suffix to the column name to define the `list/array` data type.
- Add a `{}` suffix to the column name to define the `JSON object` data type.
- Cells with empty values, 0, or FALSE will be skipped when exporting Json Data.
- Columns that only have a name but no value, empty value, 0, or FALSE will be skipped when exporting JSON Data. This helps avoid redundant data and optimizes the size of JSON Data.
- To keep columns from being skipped, add the column name to the `Unminimized Fields` cell.
- Add a `[x]` suffix to the column name to exclude that column from the Json data export.
- To define the attribute object type, follow these rules:

  - The attribute column must be placed at the end of the data table.
  - Attribute id is a constant integer, so it should be defined in the IDs sheet.
  - An attribute has the following structure:

    1. **`attribute`**: The column name follows the pattern _`attribute + (index)`_, where index can be any number, but should start from 0 and increase. The value of this column is the id of the attribute, which is an Integer type, this value should be set in the IDs sheet.
    2. **`value`**: The column name follows the pattern _`value + (index)`_ or _`value + (index) + []`_, the value of the column can be a number or a number array.
    3. **`increase`**: The column name follows the pattern _`increase + (index)`_ or _`increase + (index) + []`_. This is an additional value, which can be optional, usually used for level-up situations, specifying the additional increase when a character or item levels up.
    4. **`unlock`**: The column name follows the pattern _`unlock + (index)`_ or _`unlock + (index) + []`_. This is an additional value, which can be optional, usually used for situations where the attribute needs conditions to unlock, such as minimum level or minimum rank.
    5. **`max`**: The column name follows the pattern _`max + (index)`_ or _`max + (index) + []`_. This is an additional value, which can be optional, usually used for situations where the attribute has a maximum value.

    ```
    Example 1: attribute0, value0, increase0, value0, max0.
    Example 2: attribute1, value1[], increase1[], value1[], max1[].
    ```

## 2. Exporting

### Unity

Create 3 directories to store the files that will be exported:

- `Assets\Basic\Scripts\Generated` to store the IDs, Constants, Localization API, and LocalizationText Component scripts.
- `Assets\Basic\Data` to store the exported Json data.
- `Assets\Basic\Resources\Data` to store the Localization data.

### Excel To Unity

Enter the paths to the directories created above, and other necessary settings.

![excel-to-unity-basic-settings](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/ed6874aa-b240-40e6-94fc-8494271344d3)

- Json Data Output: `[your project path]\Assets\Basic\Data`.
- Constant Output: `[Your project path]\Assets\Basic\Scripts\Generated`, IDs, Constants, Localization API, and LocalizationText Component will be stored here.
- Localization Output: `[Your project path]\Assets\Basic\Resources\Data`, Localization data needs to be stored in the Resources folder to load/unload language files.
- Namespace: `Excel2Unity.Basic`.
- Languages maps: `korean, japanese, chinese`, we will create a separate characters map for these 3 languages

![excel-to-unity-basic-exporting](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/1adc69e7-06fc-433a-b59c-fc2049a53163)

- Enter the path to the excel file or select the file using the `Select File` button
- Finally, press `Export Json`, `Export IDs`, `Export Constants`, and `Export Localization` to export data and scripts

The exported files will be as follows

![excel-to-unity-basic-exported-scripts](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/365f5526-e0b2-410b-912b-5a7c09710edc)
![excel-to-unity-basic-exported-data](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/ea6c490d-fa24-4857-b48b-fffc4d85ddcd)
![excel-to-unity-basic-exported-localization](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/669aa26d-de9c-4218-87da-964d302df6a1)


## 3. Scripting

### Create a ScriptableObject as Static Database

- Create Serializable Objects corresponding to the data fields in the Data Tables.

```cs
[Serializable]
public class DataBasic1
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
public class DataBasic2
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
//NOTE: To use the Attributes feature, the class needs to inherit from AttributesCollection.
[Serializable]
public class DataAdvanced : AttributesCollection<AttributeParse>
{
    public int id;
    public string name;
}
```

- Create a ScriptableObject that contains the above Serializable Objects.

```cs
[CreateAssetMenu(fileName = "DataCollectionBasic", menuName = "Excel2Unity/DataCollectionBasic")]
public class DataCollectionBasic : ScriptableObject
{
    public List<DataBasic1> dataBasic1;
    public List<DataBasic2> dataBasic2;
    public List<DataAdvanced> dataAdvanced;
}
```

- Load Json Data into Serializable Objects

```cs
// NOTE: This function uses the UnityEditor library, so it must be located in the Editor directory or within #if UNITY_EDITOR
// If you don't want to use Editor code, you can choose to store the Json Data files in the Resources directory or Asset Bundles and load them using the corresponding method.
private void LoadData()
{
    var txt =  AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Basic/Data/ExampleDataBasic1.txt");
    dataBasic1 = JsonHelper.ToList<DataBasic1>(txt.text);
    txt =  AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Basic/Data/ExampleDataBasic2.txt");
    dataBasic2 = JsonHelper.ToList<DataBasic2>(txt.text);
    txt = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Basic/Data/ExampleDataAdvanced.txt");
    dataAdvanced = JsonHelper.ToList<DataAdvanced>(txt.text);
}
```

- Whenever there are changes, you just need to edit on excel and export the new data. Then, in Unity, you just need to Reload the Static Database.

![excel-to-unity-basic-scriptable-object](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/0b4bd9fb-5a24-4ec6-ba7d-05b7caba1f22)
![excel-to-unity-basic-load-data](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/3452494e-0a13-4953-af79-900c83ec3809)

### Localization

- Change the language.

```cs
// Set the language japanese
Localization.currentLanguage = "japanese";
```

- Register an event handler for the language change event.

```cs
// Register an action when language changed
Localization.onLanguageChanged += OnLanguageChanged;
```

```csharp
// Display current language
m_txtCurrentLanguage.text = Localization.currentLanguage;
```

- Get the localized content using a Key. However, with this method, the Text will not automatically update its display when the language changes.

```cs
// Get the localized text using integer key
m_txtExample1.text = Localization.Get(Localization.hero_name_1).ToString();
// Get the localized text using string key
m_txtExample2.text = Localization.Get("DAY_X", 100).ToString();
```

- You can link a gameObject which contain a Text or TextMeshProUGUI Component with a Key so that the Text automatically updates when the language changes.

```cs
// Register Dynamic localized Text
Localization.RegisterDynamicText(m_textGameObject1, Localization.hero_name_5);
// Register Dynamic localized Text
Localization.RegisterDynamicText(m_textGameObject2, "TAP_TO_COLLECT");
// Unregister the gameObject
Localization.UnregisterDynamicText(m_textGameObject1);
Localization.UnregisterDynamicText(m_textGameObject2);
```

- Using LocalizationText Component.

![excel-to-unity-basic-localization-component](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/28e9453c-2d25-44b3-ae55-ef08adee8063)

### Separate Localization

In case you choose Separate Localization in the Settings table. The output Localization data and Localization scripts will look like this.

![excel-to-unity-basic-exported-multi-localization](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/000b0c23-c1b8-4596-aa15-bbbc3e009e68)

The Localization Code will change as follows.

```cs
LocalizationsManager.currentLanguage = "spanish";
```

```cs
private IEnumerator Start()
{
    yield return LocalizationsManager.InitAsync(null);
}
```

```cs
// Register an action when language changed
LocalizationsManager.onLanguageChanged += OnLanguageChanged;
```

```cs
// Display current language
m_txtCurrentLanguage.text = LocalizationsManager.currentLanguage;
```

```cs
// Get localized string from sheet ExampleLocalization
m_txtExample1.text = ExampleLocalization.Get(ExampleLocalization.hero_name_1).ToString();
// Get localized string from sheet ExampleLocalization2
m_txtExample2.text = ExampleLocalization2.Get("DAY_X", 100).ToString();
```

```cs
// Register Dynamic localized Text in sheet ExampleLocalization
ExampleLocalization.RegisterDynamicText(m_textGameObject1, ExampleLocalization.hero_name_5);
// Register Dynamic localized Text in sheet ExampleLocalization2
ExampleLocalization2.RegisterDynamicText(m_textGameObject2, "TAP_TO_COLLECT");
// Unregister gameObject
ExampleLocalization.UnregisterDynamicText(m_textGameObject1);
ExampleLocalization2.UnregisterDynamicText(m_textGameObject2);
```

![excel-to-unity-basic-localization-component-2](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/0fcb4c69-8816-40d7-a281-6f2f8a35db44)

### TextMeshPro custom font.

We will use three files `characters_map_japan`, `characters_map_korean`, and `characters_map_chinese` to create three TextMeshPro fonts for these languages. These three characters_map files contain all the characters appearing in the Localization sheet of each language.

In this example, I use three different fonts to create three TextMeshPro fonts:

- Japanese: NotoSerif-Bold
- Korean: NotoSerifJP-Bold
- Chinese: NotoSerifTC-Bold

For each of these fonts, create a TextMeshPro font. In the `Font Asset Creator` window, under the `Character Set` section, select `Character From File`. Then, select the corresponding `characters_map` file under the `Character File` section.

![excel-to-unity-basic-font-jp](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/f56bc334-18ac-43bd-bad5-7c20eb8ffc2f)
![excel-to-unity-basic-font-kr](https://github.com/nbhung100914/excel-to-unity-example/assets/9100041/4f4cd856-f307-4155-b957-489e92a3ad35)

With the features shown, you now have all the tools you need to build a Static Database with Excel. This is enough to meet the needs of any Casual or Mid-core game.
