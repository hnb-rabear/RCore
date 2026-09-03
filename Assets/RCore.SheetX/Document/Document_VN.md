# Tài Liệu Hướng Dẫn SheetX

[English Document](Document.md) | [Tài liệu Tiếng Việt](Document_VN.md)

## 1. Giới thiệu

Công cụ đơn giản hóa thiết kế và quản lý dữ liệu game cho lập trình viên và game designer, cho phép chỉnh sửa thông số game trực tiếp mà không cần can thiệp code.

Khi dự án mở rộng, việc quản lý bảng dữ liệu, hằng số và ID trở nên phức tạp. SheetX tập trung quy trình này, hỗ trợ tìm kiếm, chỉnh sửa và cập nhật dễ dàng.

Ban đầu phát triển cho thể loại RPG với lượng dữ liệu lớn, hiện tại SheetX hỗ trợ đa dạng thể loại game, sử dụng Excel và Google Spreadsheets để quản lý dữ liệu.

### Mẫu thử nghiệm (Samples)
Tải project ví dụ [Tại đây](https://github.com/hnb-rabear/hnb-rabear.github.io/blob/main/sheetx/SheetXExample.unitypackage).

## 2. Các chức năng chính

- **Tích hợp Excel và Google Sheets:** Quản lý toàn bộ database qua file Excel hoặc Google Spreadsheets.
- **Quản lý ID và Constant:** Xuất và quản lý hằng số, ID độc lập hoặc đồng bộ với bảng dữ liệu.
- **Hệ thống Đa ngôn ngữ (Localization):** Xử lý nhiều ngôn ngữ, tích hợp sẵn UI component và API trong Unity.
- **Xuất dữ liệu JSON:** Chuyển đổi bảng dữ liệu sang file JSON để nạp vào Unity.
- **Định dạng dữ liệu linh hoạt:** Hỗ trợ kiểu cơ bản, mảng, đối tượng lồng nhau, JSON thô và hệ thống Attribute đặc thù cho RPG.
- **Data Config Collections:** Tự động tạo mã nguồn C# typed collections và bake dữ liệu trực tiếp vào ScriptableObject trong Unity Editor.

## 3. Cấu hình (Settings)

Mở từ menu chính: `RCore > Tools > SheetX > Settings`

![tab_settings](https://github.com/user-attachments/assets/8d339afe-3323-4f03-99d0-34b3cc7dc56e)

- **Scripts Output Folder:** Thư mục lưu mã nguồn C# được xuất ra (IDs, Constants, Localization Components, Localization API).
- **Json Output Folder:** Thư mục lưu file dữ liệu JSON.
- **Localization Output:** Thư mục lưu dữ liệu Localization (nằm trong thư mục Resources để tải qua `Resources.Load`, hoặc thư mục riêng khi dùng Addressables).
- **Namespace:** Namespace cho các file C# được sinh ra.
- **Separate IDs: Sheets**
  - **TRUE:** Xuất mỗi sheet `[%IDs]` thành một file C# riêng biệt `[SheetName] + IDs.cs`.
  - **FALSE:** Gộp tất cả các sheet `[%IDs]` từ mọi file Excel vào chung file `IDs.cs`.
- **Separate Constants: Sheets**
  - **TRUE:** Xuất mỗi sheet `[%Constants]` thành một file C# riêng `[SheetName] + %Constants.cs`.
  - **FALSE:** Gộp tất cả các sheet `[%Constants]` từ mọi file Excel vào chung file `Constants.cs`.
- **Separate Localization Sheets:**
  - **TRUE (mặc định):** Xuất mỗi sheet `[Localization%]` thành một nhóm riêng gồm file dữ liệu, Component và API:
    - File dữ liệu: `[SheetName]_[language].txt`
    - Component: `[SheetName] + Text.cs`
    - API: `[SheetName].cs`
  - **FALSE:** Gộp toàn bộ các sheet `[Localization%]` thành một bộ chung:
    - File dữ liệu: `Localization_[language].txt`
    - Component: `LocalizationText.cs`
    - API: `Localization.cs`
- **Only enum as IDs:** Đối với các sheet `[%IDs]`, các nhóm có hậu tố `[enum]` sẽ chỉ xuất ra dạng C# `enum` và lược bỏ khai báo `public const int`.
- **Combine Json Sheets:** Gộp toàn bộ các bảng dữ liệu trong một file Excel thành một file JSON duy nhất dạng `[ExcelName].txt`.
- **Language Char Sets:** Dùng để trích xuất bảng ký tự cho TextMeshPro, hỗ trợ font chữ tiếng Nhật, Hàn, Trung.
- **Persistent columns:** Tên các cột luôn được giữ lại trong JSON ngay cả khi toàn bộ cột không có dữ liệu.
- **Google Client ID & Client Secret:** Thông tin xác thực OAuth 2.0 để tải dữ liệu từ Google Sheets.

Cấu hình được lưu trong `Assets/SheetX/SheetXSettings.asset` (tạo tự động trong lần đầu sử dụng). Hãy commit file asset này để đồng bộ cấu hình trong team. Thông tin OAuth Google được lưu riêng trong `EditorPrefs` của từng máy để bảo mật.

## 4. Xuất dữ liệu từ file Excel

### 4.1. Xuất file đơn lẻ (Single Excel File)

Menu: `RCore > Tools > SheetX > Excel Spreadsheets`

![tab_excel_1](https://github.com/user-attachments/assets/b8ef6183-21c6-43b9-b952-8b5d57fc4c0b)

Thích hợp cho project nhỏ hoặc giai đoạn thử nghiệm chỉ cần một file Excel duy nhất.

Các nút chức năng:
- **Export IDs:** Chuyển đổi các sheet ID sang mã C#.
- **Export Constants:** Chuyển đổi các sheet Constants sang mã C#.
- **Export Json:** Chuyển đổi các sheet dữ liệu sang file JSON.
- **Export Localization:** Xuất dữ liệu ngôn ngữ, Component và API điều khiển.
- **Export All:** Thực hiện toàn bộ quy trình trên chỉ với một click.

### 4.2. Xuất nhiều file Excel (Multiple Excel Files)

![tab_excel_2](https://github.com/user-attachments/assets/54b3632d-18f9-4053-b2fe-3feef6f71f92)
![tab_excel_2_edit](https://github.com/user-attachments/assets/d958d749-5410-416b-9095-a598f9fe5a82)

Dành cho hệ thống cơ sở dữ liệu lớn phân tán ở nhiều file Excel khác nhau:
1. Thêm danh sách các file Excel cần xử lý.
2. Tùy chọn bật/tắt từng sheet cụ thể trong từng file.
3. Bấm **Export All** để xử lý hàng loạt.

## 5. Xuất dữ liệu từ Google Spreadsheets

Menu: `RCore > Tools > SheetX > Google Spreadsheets`

### 5.1. Cấu hình Google Client ID và Client Secret

#### Bước 1: Kích hoạt Google Sheets API
1. Truy cập [Google Cloud Console](https://console.cloud.google.com/).
2. Tạo project mới hoặc chọn project có sẵn.
3. Vào **APIs & Services > Library**, tìm kiếm **Google Sheets API** và bấm **Enable**.

#### Bước 2: Tạo thông tin xác thực (Credentials)
1. Trong Google Sheets API, chọn **Create Credentials**.
2. Chọn **User data**, cấp quyền scope `Google Sheets API` ("See all your Google Sheets Spreadsheets").
3. Tại mục OAuth Client ID, chọn Application Type là **Desktop App**, đặt tên bất kỳ và bấm **Create**.

#### Bước 3: Nhập thông tin vào SheetX
1. Copy **Client ID** và **Client Secret**.
2. Dán vào mục tương ứng trong cửa sổ **Sheets Exporter Settings**.
3. Khi tải dữ liệu lần đầu, trình duyệt sẽ mở để đăng nhập tài khoản Google. Token được lưu tại `Library/SheetX` (không đưa vào version control).

![tab_settings_2](https://github.com/user-attachments/assets/4140a3e8-05df-4bbe-a3b8-a2fb0576f1ee)

### 5.2. Xuất dữ liệu một bảng Google Sheet
Nhập Sheet ID từ URL (`https://docs.google.com/spreadsheets/d/[GOOGLE_SHEET_ID]/edit`), bấm **Download** và xuất dữ liệu.

### 5.3. Xuất dữ liệu nhiều bảng Google Sheet
Bấm **Add Google Spreadsheets**, thêm Sheet ID, tải dữ liệu và chọn các sheet cần xuất tương tự như file Excel.

## 6. Lập trình xuất dữ liệu tự động (Public API)

Class `RCore.SheetX.Editor.SheetXExporter` cho phép gọi xuất dữ liệu qua code C# độc lập với giao diện Editor (thích hợp cho Build Pipeline, CI/CLI, hoặc tool riêng):

```cs
using RCore.SheetX.Editor;

var request = new SheetXExportRequest
{
    SpreadsheetPath = "Assets/Data/Game.xlsx",
    Sheets = null, // null = xuất toàn bộ sheet
    ConstantsOutputPath = "Assets/Generated/Scripts",
    JsonOutputPath = "Assets/Generated/Json",
    LocalizationOutputPath = "Assets/Generated/Localization",
    Namespace = "MyGame.Data",
};

SheetXExportResult result = SheetXExporter.ExportExcel(request, myOutput);
if (!result.Success)
{
    foreach (var error in result.Errors)
        Debug.LogError(error);
}
```

Hỗ trợ batch xuất nhiều nguồn qua `SheetXExporter.ExportBatch`.

## 7. Data Config Collections

Tính năng tùy chọn giúp tự động sinh mã nguồn data class, quản lý collections tập trung và nạp dữ liệu vào ScriptableObject assets.

### 7.1. Cấu hình thư mục
- **Collection Code Folder:** Lưu `SheetXDataCollections.cs` (row models, đường dẫn JSON) và các file ScriptableObject `<TypeName>.cs` (`GlobalConfigCollection.cs`, `<Name>ConfigCollection.cs`).
- **Collection JSON Folder:** Lưu file JSON trung gian phục vụ bake dữ liệu. Không đặt dưới `Resources` hoặc `StreamingAssets`.
- **Collection Asset Folder:** Lưu asset ScriptableObject cho các feature collection.
- **Global Resources Folder:** Thư mục phải có đuôi `Resources` (ví dụ `Assets/Resources` hoặc `Assets/Game/Resources`) để lưu `GlobalConfigCollection.asset`.

### 7.2. Chế độ xuất dữ liệu của sheet (Output Modes)
- **JSON Only:** Xuất JSON thông thường theo cấu trúc truyền thống.
- **Generated Data Class:** Tự động suy luận kiểu dữ liệu (`int`, `float`, `bool`, `string`) từ ô dài nhất, sinh partial class tương ứng.
- **Existing Data Class:** Sử dụng một kiểu dữ liệu có sẵn trong project đã được đánh dấu `[SheetXBindable]`. Kiểu bị thiếu hoặc không hợp lệ sẽ ghi log lỗi và bỏ qua sheet đó.

Kiểu dữ liệu dùng cho Existing Data Class phải được khai báo tường minh. Bắt buộc có cả hai attribute: `[Serializable]` để Unity serialize mảng dữ liệu sau khi bake, và `[SheetXBindable]` để SheetX đưa kiểu đó vào danh sách chọn. Hỗ trợ cả `class` lẫn `struct`; kiểu phải là kiểu cụ thể (không abstract) và không generic. `[SheetXBindable]` nằm trong assembly runtime được auto-reference, nên mã nguồn game không cần chỉnh asmdef.

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

Dropdown Data Class chỉ liệt kê các kiểu đã đánh dấu. Nếu project chưa đánh dấu kiểu nào, dropdown hiển thị `No [SheetXBindable] type found`. Dropdown, quá trình export và quá trình bake dùng chung một quy tắc kiểm tra, nên kiểu đã xuất hiện trong dropdown chắc chắn export và bake được.

**Nâng cấp từ phiên bản 1.6.0 trở về trước.** Trước đây mọi class `[Serializable]` đều hợp lệ. Hãy thêm `[SheetXBindable]` vào từng class hoặc struct đang được gán làm Existing Data Class; kiểu chưa đánh dấu sẽ biến mất khỏi dropdown và bị từ chối khi export cũng như khi bake, kèm thông báo lỗi nêu rõ attribute còn thiếu. `struct` là điểm mới — quy tắc cũ chỉ chấp nhận `class`.

Cú pháp header cho Generated Data Class:
```text
id | price | enabled | tags[] | reward.amount
id:string | price:float | enabled:bool | tags[]:string | reward.amount:int
```

- Mặc định kiểu suy luận: `int`, `float`, `bool`, `string`.
- Thêm hậu tố `[]` để khai báo mảng.
- Dùng dấu chấm `.` để tạo cấu trúc đối tượng lồng nhau (`reward.amount`).
- Thêm `:type` để ép kiểu cụ thể (hỗ trợ `int`, `float`, `bool`, `string`).
- **Lưu ý về Enum:** Không hỗ trợ khai báo enum trực tiếp trên header bảng dữ liệu (`type:enum` không hợp lệ). Hãy định nghĩa enum trong sheet `[%IDs]` và dùng tên hằng số trong ô dữ liệu.
- Thêm `[x]` vào bất kỳ vị trí nào trên header để bỏ qua (ignore) cột đó.

### 7.3. Bake và Truy xuất Runtime

Sau khi xuất dữ liệu và Unity biên dịch mã nguồn, SheetX sẽ cập nhật các ScriptableObject asset tương ứng.

Truy xuất runtime:
```cs
var global = GlobalConfigCollectionBase.Instance<GlobalConfigCollection>();
// Hoặc gán instance giả lập khi chạy test:
GlobalConfigCollectionBase.SetInstance(customGlobal);
```

## 8. Quy tắc thiết kế trong Spreadsheet

### 8.1. Sheet IDs (`[%IDs]`)

| Hero   |     |         | Building      |     |         | Pet      |     |         | Gender[enum]      |     |
| ------ | --- | ------- | ------------- | --- | ------- | -------- | --- | ------- | ----------------- | --- |
| HERO_1 | 1   | comment | BUILDING_NULL | 0   | comment | PET_NULL | 0   | comment | GENDER_NONE       | 0   |
| HERO_2 | 2   | comment | BUILDING_1    | 1   |         | PET_1    | 1   |         | GENDER_MALE       | 1   |
| HERO_3 | 3   | comment | BUILDING_2    | 2   |         | PET_2    | 2   |         | GENDER_FEMALE     | 2   |
|        |     |         | BUILDING_3    | 3   |         | PET_3    | 3   |         | GENDER_HELICOPTER | 3   |

Quy tắc:
- Tên sheet phải kết thúc bằng `IDs`.
- Mỗi nhóm chiếm 3 cột liên tiếp: `Key Name`, `Key Value` (phải là số nguyên), `Comment`.
- Hàng đầu tiên (Row 0) chứa tên nhóm.
- Mặc định xuất thành Integer Constants (`public const int KEY = VALUE;`).
- **Khai báo Enum:** Thêm hậu tố `[enum]` vào tên nhóm ở Row 0 (ví dụ `Gender[enum]`, `ItemType[enum]`) để xuất thành C# `public enum Name { KEY = VALUE, ... }`.
  - Khi `Only enum as IDs` = FALSE: Xuất cả `const int` và `enum`.
  - Khi `Only enum as IDs` = TRUE: Chỉ xuất `enum`.
  - Trong các bảng dữ liệu, tên symbolic key (ví dụ `GENDER_MALE`) sẽ tự động được thay bằng giá trị số nguyên trong JSON xuất ra.

### 8.2. Sheet Constants (`[%Constants]`)

| Name                  | Type        | Value              | Comment               |
| --------------------- | ----------- | ------------------ | --------------------- |
| EXAMPLE_INT           | int         | 83                 | Integer Example       |
| EXAMPLE_FLOAT         | float       | 1.021              | Float example         |
| EXAMPLE_STRING        | string      | 321fda             | String example        |
| EXAMPLE_INT_ARRAY_1   | int-array   | 4                  | Integer array example |
| EXAMPLE_INT_ARRAY_2   | int-array   | 0:3:4:5            | Integer array example |
| EXAMPLE_FLOAT_ARRAY_1 | float-array | 5                  | Float array example   |
| EXAMPLE_VECTOR2_1     | vector2     | 1:2                | Vector2 example       |
| EXAMPLE_VECTOR3       | vector3     | 3:3:4              | Vector3 example       |
| EXAMPLE_REFERENCE_1   | int         | HERO_1             | Tham chiếu từ IDs     |

Quy tắc:
- Tên sheet phải kết thúc bằng `Constants`.
- Gồm 4 cột chuẩn: `Name`, `Type`, `Value`, `Comment`.
- Kiểu dữ liệu hỗ trợ: `int`, `float`, `bool`, `string`, `int-array`, `float-array`, `vector2`, `vector3`.
- Mảng phân cách giá trị bằng `:`, `|` hoặc xuống dòng.

### 8.3. Sheet Localization (`[Localization%]`)

| idstring     | relativeId | english                   | spanish                        |
| ------------ | ---------- | ------------------------- | ------------------------------ |
| message_1    |            | this is english message 1 | este es el mensaje en ingles 1 |
| content      | 1          | this is english message 1 | este es el mensaje en ingles 1 |
| hero_name    | HERO_1     | hero name 1               | nombre del héroe 1             |

Quy tắc:
- Tên sheet bắt đầu bằng `Localization`.
- Cột chính gồm `idString` và `relativeId` (có thể tham chiếu từ IDs). Khóa dịch là kết hợp của cả hai.
- Các cột tiếp theo chứa nội dung từng ngôn ngữ.

### 8.4. Bảng dữ liệu JSON (Data Tables)

#### Kiểu cơ bản
| numberExample1 | numberExample2 | numberExample3 | boolExample | stringExample |
| -------------- | -------------- | -------------- | ----------- | ------------- |
| 1              | 10             | 1.2            | TRUE        | text          |
| 3              | BUILDING_8     | 5              | FALSE       | text          |

#### Kiểu mở rộng: Mảng và JSON Object
| array1[] | array2[]    | JSON{}                         |
| -------- | ----------- | ------------------------------ |
| text1    | 1           | {}                             |
| text2    | 2 \| 2 \| 3 | {"id":1, "name":"John Doe"}    |

- Cột mảng phải có hậu tố `[]`.
- Cột đối tượng JSON thô phải có hậu tố `{}`.

### 8.5. Sheet Configuration (ScriptableObject Cấu Hình)

Sheet có tên chính xác là `Configuration` (phân biệt hoa thường) được xử lý riêng thành cấu trúc nạp tĩnh:
- Khi bật Collections: Tự động đưa vào `GlobalConfigCollection`.
- Khi tắt Collections: Xuất thành `Configuration.cs`, `Configuration.txt` và `Configuration.asset`.

Header chuẩn 4 cột:
```text
| Sub Class | Field Name | Type | Value |
```

- **Sub Class:** Tên nhóm/lớp con. Để trống để tiếp tục nhóm hiện tại; dòng trống đóng nhóm; để trống từ đầu tạo thuộc tính ở cấp root.
- **Type:** `int`, `float`, `boolean`, `string`, `int-array`, `float-array`, `string-array`, `vector2`, `vector3`.

#### Kiểu dữ liệu đặc thù: Hệ thống Attribute (Attributes List cho RPG)
| attribute0 | value0 | unlock0 | increase0 | max0 | attribute1 | value1[] | unlock1[] | increase1[] | max1[]   |
| ---------- | ------ | ------- | --------- | ---- | ---------- | -------- | --------- | ----------- | -------- |
| ATT_HP     | 30     | 2       | 1.2       | 8    | ATT_CRIT   | 3 \| 2   | 0 \| 11   | 0.5 \| 1    | 10 \| 20 |

Cấu trúc: `attribute{N}`, `value{N}` (hoặc `value{N}[]`), `increase{N}`, `unlock{N}`, `max{N}`.

## 9. Hướng dẫn tích hợp Code

### 9.1. Đọc dữ liệu JSON vào ScriptableObject

```cs
[Serializable]
public class ExampleData1
{
    public int numberExample1;
    public int numberExample2;
    public bool boolExample;
    public string stringExample;
}

[CreateAssetMenu(fileName = "ExampleDataCollection", menuName = "SheetXExample/Create ExampleDataCollection")]
public class ExampleDataCollection : ScriptableObject
{
    public List<ExampleData1> exampleData1s;

    [ContextMenu("Load")]
    private void LoadData()
    {
        #if UNITY_EDITOR
        var txt = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/SheetXExample/DataConfig/ExampleData1.txt");
        exampleData1s = JsonConvert.DeserializeObject<List<ExampleData1>>(txt.text);
        #endif
    }
}
```

### 9.2. Tích hợp Localization

Khởi tạo và đổi ngôn ngữ:
```cs
LocalizationManager.Init();
LocalizationsManager.CurrentLanguage = "jp";
LocalizationsManager.OnLanguageChanged += OnLanguageChanged;
```

Lấy chuỗi dịch:
```cs
// 1. Lấy trực tiếp qua key
m_text.text = LocalizationExample2.Get(LocalizationExample2.GO_TO_SHOP).ToString();

// 2. Đăng ký text tự động cập nhật khi đổi ngôn ngữ
LocalizationExample2.RegisterDynamicText(m_dynamicText.gameObject, LocalizationExample2.TAP_TO_COLLECT);
```

#### Đổi ngôn ngữ trực tiếp trong Scene View (Scene View Overlay)
1. Mở cửa sổ **Scene View**.
2. Bật overlay từ **View > Overlays > Localization**.
3. Chọn ngôn ngữ từ dropdown hoặc dùng `<` và `>` để xem trước toàn bộ UI trong Scene/Prefab mà không cần chạy Play Mode.
