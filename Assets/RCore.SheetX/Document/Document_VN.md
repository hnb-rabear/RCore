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

### Mở tài liệu từ Unity

Thanh công cụ của cửa sổ SheetX có hai nút `Docs (EN)` và `Tài liệu (VI)`, hiển thị ở mọi tab. Mỗi nút mở tài liệu tương ứng trên GitHub bằng trình duyệt — bản đã render, vì bản nằm trong package sẽ mở bằng trình soạn thảo văn bản thuần, nơi mọi ảnh minh họa đều là link hỏng.

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

Kiểu dữ liệu dùng cho Existing Data Class phải được khai báo tường minh. Bắt buộc có cả hai attribute: `[Serializable]` để Unity serialize mảng dữ liệu sau khi bake, và `[SheetXBindable]` để SheetX đưa kiểu đó vào danh sách chọn. Hỗ trợ cả `class` lẫn `struct`; kiểu phải là kiểu cụ thể (không abstract) và không generic. Kiểu còn phải **truy cập được công khai** — khai báo `public`, và nếu là kiểu lồng nhau thì mọi kiểu bao ngoài cũng phải `public` — vì mã sinh ra khai báo field `public` của kiểu đó; kiểu `internal`, `private` hoặc lồng trong một kiểu không `public` đều bị từ chối, kể cả khi file sinh ra nằm cùng assembly. `[SheetXBindable]` nằm trong assembly runtime được auto-reference, nên mã nguồn game không cần chỉnh asmdef.

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

### 7.4. Chế độ lưu trữ Collection (Separate Asset và Inline)

Mỗi collection chọn một trong hai chế độ lưu trữ trong cửa sổ **Manage Collections...**:

- **Separate Asset (mặc định):** Collection được sinh ra dưới dạng `ScriptableObject` kế thừa `SheetXConfigCollectionBase` và bake vào một file `.asset` riêng trong Collection Asset Folder. `GlobalConfigCollection` chỉ giữ một object reference trỏ tới asset đó.
- **Inline:** Collection được sinh ra dưới dạng class `[Serializable]` thông thường, không kế thừa base class, và được serialize trực tiếp bên trong `GlobalConfigCollection.asset`. Không có file `.asset` riêng nào được tạo.

Ở cả hai chế độ, generator đều sinh file `<Name>ConfigCollection.cs`; chỉ khác nhau ở khai báo class (`: SheetXConfigCollectionBase` so với `[Serializable]` không base class). Giữ file ở cả hai chế độ để tránh lỗi biên dịch trùng kiểu do một file cũ bị bỏ lại.

**Khác biệt và hành vi khi chuyển chế độ:**

- **Cách đọc dữ liệu không đổi:** Mã game truy xuất dữ liệu y hệt nhau ở cả hai chế độ (`global.player.Characters`).
- **Inline không có asset độc lập:** Collection ở chế độ `Inline` không có file asset, nên không thể kéo thả vào Prefab hay field trong Inspector, không có property `IsLoaded`, và không thể nạp riêng lẻ. Nó được nạp cùng lúc với Global, và **Auto Load** của nó đi theo Global chứ không dùng cờ đã lưu của chính nó.
- **Asset cũ được giữ lại khi chuyển sang Inline:** Khi chuyển một collection sang `Inline`, file `.asset` cũ vẫn nằm nguyên trên ổ đĩa chứ không bị xóa. Nó chỉ ngừng được bake, và Inspector của Unity sẽ hiển thị "associated script can not be loaded" cho asset đó vì class tương ứng không còn là `ScriptableObject` nữa. Khi chuyển ngược lại, SheetX dùng lại đúng file và GUID đó, nên các reference chưa bị đụng tới sẽ trỏ đúng trở lại.
- **Cảnh báo: field `[SerializeField]` không báo lỗi biên dịch.** Một field `[SerializeField]` mang kiểu của collection (ví dụ `[SerializeField] private PlayerConfigCollection m_player;`) **vẫn biên dịch bình thường** sau khi chuyển sang `Inline`, nhưng âm thầm trở thành một bản sao inline rỗng thay vì trỏ tới dữ liệu của Global. Đây là mất dữ liệu ngầm, không phải lỗi compile — bắt buộc phải tự rà soát và sửa tay từng field như vậy.
- **Xác nhận migration và khôi phục snapshot:** Việc đổi chế độ lưu trữ của một collection sẽ hỏi xác nhận trước khi ghi đè mã nguồn sinh ra, đồng thời lưu một snapshot bền tại `Library/SheetX/migration-snapshot.json` — snapshot này tồn tại xuyên qua domain reload. Nếu bake thất bại sau domain reload, lỗi được báo là một lần đổi chế độ lưu trữ chưa hoàn tất, và menu **RCore > SheetX: Restore Migration Snapshot** sẽ đưa mã nguồn cũ trở lại rồi refresh. Với collection mà file sinh ra là mới hoàn toàn trong lần migration đó (trước đây chưa từng có trên ổ đĩa), restore giữ nguyên file, không xóa.

  Restore chỉ hoàn nguyên **mã nguồn sinh ra**. Field trong Global đã đổi từ object reference sang giá trị inline rồi đổi ngược lại qua hai lần domain reload, nên Unity đã bỏ mất reference đó: `Global.player` sẽ rỗng cho đến lần bake kế tiếp. Không có dữ liệu nào bị mất — file `.asset` vẫn còn nguyên cùng GUID, và lần bake thành công tiếp theo sẽ trỏ lại đúng nó. Hãy chạy **Manage Collections > Load All Collections**, hoặc export lại.

  Restore cũng hỏi xác nhận và nêu rõ tên những collection sẽ bị hoàn nguyên, vì `Capture` giữ lại cả snapshot của một lần migration đã bỏ dở — nếu không hỏi, nó có thể âm thầm hoàn nguyên nhầm một lần export khác. Chọn **Discard** để xóa snapshot cũ đó mà không restore và không đụng vào mã nguồn nào; các lần migration sau sẽ tự tạo snapshot rollback riêng.
- **Export headless (batch mode):** `EditorUtility.DisplayDialog` luôn trả về false trong batch mode, nên một script export gặp thay đổi chế độ lưu trữ sẽ dừng lại với thông báo "was not confirmed" thay vì migrate. Script migration chạy headless phải gán `SheetXCollectionExportSession.ConfirmDepthChange` trước khi export.

### 7.5. Xem trước cấu trúc sheet và kiểm tra Data Class

Bảng danh sách sheet ở cả bốn cửa sổ — `Excel Spreadsheets`, `Google Spreadsheets` và hai cửa sổ `Edit Spreadsheets` — có thêm cột **Structure**, mỗi sheet dữ liệu thông thường một nút. Nút này đọc sheet và hiển thị cấu trúc C# tương ứng với dữ liệu sẽ được xuất, nhưng không ghi file, không tạo binding, không thay đổi bất kỳ setting nào; tooltip ghi rõ `Preview this sheet's C# structure without exporting files.` Nút luôn có mặt, bất kể Data Config Collections đang bật hay tắt.

Những sheet không có nút: sheet có tên **kết thúc bằng** `IDs`, `Constants` hoặc `Settings`, hoặc **bắt đầu bằng** `Localization`, đều không có cấu trúc dòng để hiển thị — nên `HeroIDs`, `ShopConstants`, `GameSettings` và `LocalizationVN` đều bị loại. Riêng sheet có tên **chính xác** là `Configuration` bị loại vì export ghi nó thành class cấu hình có kiểu chứ không phải mảng dòng; trường hợp này so khớp theo tên chính xác, nên tên dài hơn như `ConfigurationExtra` vẫn là sheet thường và vẫn có nút.

Nút mở cửa sổ **Sheet Structure**. Phần đầu cửa sổ hiển thị **Source**, **Sheet**, **Output Mode**, **Fetched** — ngày và giờ địa phương lúc đọc ảnh chụp này — rồi **Data Class**, kế đến là bảng **Top-level fields**, rồi tới đoạn mã C# tương ứng với sheet. Nút **Refresh** đọc lại sheet, nút **Copy Code** chép đoạn mã class vào clipboard — chỉ chép mã, không bao giờ chép JSON.

**Ý nghĩa của đoạn mã phụ thuộc vào Output Mode:**

- **`Generated Data Class`** được gán nhãn **Generated class preview**. Đây là class sinh ra thật sự: phần khai báo class giống đúng từng byte với class mà lần export kế tiếp sẽ ghi, không bị chèn thêm bất kỳ chú thích riêng nào của phần xem trước. Bản thân file được export thì dài hơn — nó còn chứa collection paths và row type của mọi bảng khác.
- **`JSON Only` và `Existing Data Class`** được gán nhãn **Inferred draft from exported JSON**. Hai chế độ này không khai báo schema, nên cửa sổ tự suy luận cấu trúc từ chính JSON mà exporter sẽ tạo ra. Hãy xem đây là bản nháp để tham khảo, không phải bản khai báo chính thức. Cửa sổ cũng tự nêu ba giới hạn: ô trống hoặc cột bị bỏ qua không có mặt trong dữ liệu nên không thể xuất hiện ở đây; kiểu dữ liệu là suy luận từ các giá trị đang có chứ không phải khai báo; và phạm vi phân giải ID phụ thuộc vào tab đã mở phần xem trước, như mô tả ngay bên dưới.

**Phạm vi phân giải ID phụ thuộc vào tab đã mở phần xem trước**, vì hai tab chạy hai kiểu export khác nhau. Xem trước mở từ tab **Export Single File** dự đoán đúng lệnh export của tab đó, nên ID ký hiệu chỉ được phân giải trong phạm vi nguồn đang xem; cửa sổ ghi `IDs are resolved against this source only, as this tab's own export does`. Xem trước mở từ tab **Export Multi Files** — kể cả cửa sổ `Edit Spreadsheets` của từng nguồn mà nút **Select** mở ra — dự đoán lệnh `Export All`, nên ID được phân giải trên danh sách đó, theo đúng thứ tự danh sách, định nghĩa đầu tiên thắng; cửa sổ ghi `IDs are resolved against every source in the Export Multi Files list, as that export does`. Hai nền tảng khác nhau ở chỗ mục nào trong danh sách góp ID, và mỗi phần xem trước đều bám đúng lệnh export của nền tảng mình: Excel đọc sheet `*IDs` của mọi workbook trong danh sách bất kể checkbox, còn Google chỉ đọc các spreadsheet được chọn cùng các sheet `*IDs` được chọn của chúng. Điều này chỉ chi phối việc phân giải ID — sheet dữ liệu của một nguồn không được chọn thì vẫn không bao giờ được export. Một hệ quả riêng ở phía Google: nếu xem trước một sheet nằm trong spreadsheet có trong danh sách nhưng **không được tick**, phạm vi vẫn là một spreadsheet duy nhất, vì `Export All` bỏ qua hẳn spreadsheet đó; các sheet `*IDs` của chính nó vẫn được phân giải, nên phần xem trước không hề đọc ít ID hơn so với khi mở riêng nó. Cùng một file hoặc spreadsheet có thể nằm ở cả hai nơi, và nó giữ đúng phạm vi của từng tab — xem trước từ tab Single sẽ không mượn ID của danh sách.

Với Google, bản đồ ID đa nguồn chỉ được dựng một lần rồi giữ lại trong phiên làm việc, vì mỗi lần dựng tốn một kết nối cộng một lượt đọc dải ô cho mỗi sheet `*IDs` của mỗi spreadsheet trong danh sách. Nút **Refresh** dựng lại bản đồ này, đó là cách để nhận những thay đổi vừa sửa trên web. Bản đồ chỉ được ghi nhớ khi nó được dựng đầy đủ: nếu có spreadsheet trong danh sách không kết nối được, phần xem trước vẫn hiển thị kết quả kèm cảnh báo nêu tên spreadsheet đó, nhưng lần xem trước kế tiếp sẽ duyệt lại toàn bộ danh sách chứ không dùng lại phần còn thiếu. Những sheet vừa được thêm trên web được phát hiện từ metadata trực tiếp của spreadsheet, nên một sheet `*IDs` mới tạo sẽ phân giải được trong phần xem trước đúng như khi export, không cần mở lại `Edit Spreadsheets` — còn sheet IDs bạn đã chủ động bỏ chọn thì vẫn bị loại.

Bản nháp mở đầu bằng ba dòng chú thích nhắc lại rằng đây là cấu trúc suy luận chứ không phải schema khôi phục được, để ý nghĩa đó đi theo đoạn mã khi bạn dán nó sang nơi khác. Bên trong class, chỉ hai loại field được chú thích: field có khóa JSON không dùng được làm định danh C# nên bị đổi tên, và field có kiểu rơi về `object` hoặc `object[]`. Chú thích của field đổi tên nêu đúng **khóa JSON**, không phải tên cột trong spreadsheet — vì Attribute System kiểu cũ có thể gộp nhiều cột thành một khóa, và tên cột gốc không thể khôi phục từ JSON. Chú thích rơi kiểu nêu đúng lý do mà bước suy luận đã ghi nhận, ví dụ `fallback to object: mixes null with a number` hoặc `fallback to object[]: only empty arrays were observed`. Các khai báo bình thường không kèm chú thích nào.

**Bảng Top-level fields.** Phía trên đoạn mã là bảng ba cột `JSON key`, `C# field` và `Type`, chỉ liệt kê các field ở cấp cao nhất. Dấu `*` sau tên kiểu đánh dấu trường hợp rơi kiểu, lý do nằm trong tooltip. Object lồng nhau trở thành class phụ và xuất hiện trong đoạn mã bên dưới chứ không nằm trong bảng. Ở chế độ `Generated Data Class`, bảng được thay bằng một dòng ghi chú trỏ xuống đoạn mã, vì field ở chế độ đó lấy từ schema khai báo sẵn của sheet.

Quy tắc ô trống là điểm dễ gây nhầm nhất. SheetX loại hẳn ô trống ra khỏi JSON xuất ra, nên một cột trống ở **mọi** dòng sẽ không xuất hiện trong bản nháp; còn cột chỉ trống ở một vài dòng thì vẫn sinh ra field. Nếu cần giữ cột đó trong mọi trường hợp, hãy khai báo tên cột trong mục **Persistent columns** ở Settings. Phạm vi ID thì ngược lại, nhưng chỉ khi so giữa hai tab: một khóa ký hiệu mà phần xem trước từ tab **Single** để nguyên dạng chữ vẫn có thể được thay bằng số khi chạy `Export All` từ danh sách **Multi Files**, vì danh sách đó dùng chung một không gian ID. Phần xem trước mở từ tab **Multi Files** vốn đã phân giải trên chính danh sách đó, nên không lệch so với lệnh export mà nó dự đoán.

**Hai tab `Class Code` và `JSON`.** Với sheet ở chế độ `JSON Only` và `Existing Data Class`, cửa sổ có hai tab: bản nháp suy luận, và chính phần JSON được xuất ra. Chuyển tab không đọc lại gì cả — JSON được định dạng đúng một lần lúc chụp ảnh dữ liệu, và chuỗi trông giống ngày tháng vẫn được giữ nguyên dạng chuỗi chứ không bị định dạng lại thành thứ mà export chưa từng ghi. `Generated Data Class` không có JSON kiểu cũ nên hoàn toàn không hiện tab JSON. Khung hiển thị tối đa 50.000 ký tự: dữ liệu vốn đã dài hơn mức đó được cắt thô kèm nhãn `Large JSON: showing a raw excerpt`, còn dữ liệu chỉ vượt mức sau khi thụt lề thì bị cắt kèm ghi chú riêng. Cả hai trường hợp đều ghi rõ phần hiển thị là không đầy đủ và tự nó không parse được thành JSON hợp lệ. Việc cắt chỉ áp dụng cho phần hiển thị — JSON được lưu để đối chiếu thành viên luôn là tài liệu đầy đủ.

**Độ mới của dữ liệu.** Ảnh chụp của chính sheet đang xem được lấy đúng lúc mở cửa sổ và chỉ thuộc về cửa sổ đó; nó không bao giờ được cache, và bấm **Structure** lần nữa là đọc lại sheet đó từ đầu. Bản đồ ID dùng chung của Google nói ở trên là ngoại lệ duy nhất: nó được dùng lại trong suốt phiên làm việc thay vì đọc lại cho từng lần xem trước, cho tới khi **Refresh**, một lần domain reload, một settings asset khác, hoặc một thay đổi trong danh sách `Export Multi Files` hay phần chọn của nó khiến nó được dựng lại. Dòng **Fetched** ở phần đầu cửa sổ hiển thị ngày và giờ địa phương của ảnh chụp đang xem, nên một cửa sổ mở lâu không bao giờ bị hiểu nhầm là vừa đọc mới; nếu refresh thất bại, ảnh chụp cũ cùng mốc thời gian gốc của nó vẫn được giữ nguyên. Sau khi sửa spreadsheet, hãy bấm **Refresh**. Nếu lần refresh thất bại, đoạn mã cũ vẫn được giữ trên màn hình kèm dòng `Stale — last refresh failed:` và lý do, để nó không bao giờ bị hiểu nhầm là dữ liệu mới. Nếu Output Mode của sheet bị đổi trong lúc cửa sổ đang mở, cửa sổ báo `Output Mode changed since this preview was taken. Press Refresh to reload it.`

**Kiểm tra thành viên cho `Existing Data Class`.** Với sheet gắn vào một class do bạn tự viết, cửa sổ hiển thị thêm khối **Existing Data Class** đối chiếu các khóa JSON xuất ra với class đó:

- **`Unmatched JSON member '<tên>'`** (mỗi khóa một cảnh báo) nghĩa là dữ liệu xuất ra mang một giá trị mà class không có thành viên nào nhận được, nên giá trị của cột đó bị loại bỏ khi nạp JSON. Thông báo nói rõ điều này và nêu hai cách sửa: thêm thành viên tương ứng vào class, hoặc đổi tên cột trong sheet cho khớp một thành viên sẵn có. Nếu bản nháp có suy luận được khai báo cho khóa đó, cảnh báo sẽ kèm khai báo này dưới dạng **gợi ý, không phải cách sửa chắc chắn đúng** — hãy kiểm tra trước khi dán. Gợi ý cho khóa bị đổi tên luôn kèm theo mapping `JsonProperty`, vì khai báo trơn sẽ không nhận được khóa JSON; riêng trường hợp rơi về `object` hay `object[]` thì Unity không serialize được nguyên trạng.
- **`No unmatched top-level JSON members`** nghĩa là mọi khóa xuất ra đều có chỗ nhận.
- **`Not present in exported data: <tên>`** chỉ mang tính thông tin. Ô trống vốn đã bị loại khỏi dữ liệu xuất ra, nên có thể những thành viên này đơn giản là chưa có giá trị nào trong sheet hiện tại. Thông báo cố ý không khẳng định sau khi nạp chúng sẽ mang giá trị gì — điều đó phụ thuộc vào class, vốn có thể có giá trị mặc định từ constructor.
- **`Member coverage cannot be verified`** luôn giữ nguyên lý do cụ thể: chưa chọn row type, class dùng `JsonConverter` riêng (khai báo trên class hoặc nằm trong serializer settings), contract không phải kiểu object, class có `[JsonExtensionData]`, class có thành viên chỉ-đọc mà deserializer có thể ghi thẳng vào, hoặc có lỗi khi phân giải contract. Nó nghĩa là phép kiểm tra không thể khẳng định cũng không thể phủ định độ phủ — chứ không phải dữ liệu đang có vấn đề. Hãy tự mở class ra đối chiếu để xác nhận.
- **Lỗi binding được ưu tiên hơn cả khối kiểm tra** và thay thế toàn bộ khối đó bằng một thông báo lỗi duy nhất. Kiểu đã gắn mà không còn phân giải được sẽ được nêu đích danh — `Row type '<tên>' was not found in any loaded assembly.` — còn kiểu bị export từ chối thì giữ nguyên lý do từ chối của chính nó, ví dụ `row type '<tên đầy đủ>' is missing [SheetXBindable].` Cả hai đều kết thúc bằng `Export skips this sheet until this is fixed.`, vì export cũng phân giải và kiểm tra đúng kiểu đó rồi bỏ qua sheet. Đây là lỗi chứ không phải dòng `cannot be verified` mang tính trấn an, vì cách diễn đạt đó sẽ sai trong trường hợp này. Riêng binding chưa gắn class nào thì không phải lỗi — trường hợp đó báo `Member coverage cannot be verified: no row type is selected.`
- Khi tên thành viên khớp hết nhưng kiểu giá trị sai (ví dụ chữ nằm trong field `int`), thông báo lỗi chuyển đổi sẽ thay cho dòng kết quả sạch. Export cũng thực hiện đúng phép chuyển đổi đó và sẽ từ chối sheet, nên thông báo chỉ thẳng vào giá trị được nêu tên và kiểu đã khai báo của thành viên tương ứng. Không có gì được sửa tự động.
- Phiên bản này **chỉ kiểm tra ở cấp cao nhất**: `Top-level member check only; nested members are not compared.` Thành viên thiếu bên trong một object lồng nhau không được báo.

**Phép kiểm tra bám theo deserializer thật.** Contract được phân giải qua `JsonSerializer.CreateDefault()` ở mỗi lần so khớp, nên nếu dự án có đặt `JsonConvert.DefaultSettings` — ví dụ một naming strategy snake_case — thì thiết lập đó được tôn trọng, kể cả khi bị thay đổi sau một lần kiểm tra trước đó. Ở những chỗ kết quả phụ thuộc vào dữ liệu chứ không phải vào kiểu, phép kiểm tra nói thẳng là không xác định được thay vì đoán: một collection hay object lồng nhau chỉ-đọc có thể được ghi thẳng vào hoặc không, tùy getter có trả về instance khác null hay không, nên kiểu như vậy sẽ báo `Member coverage cannot be verified` thay vì kết tội export làm mất dữ liệu mà thực tế nó vẫn giữ. Riêng **giá trị vô hướng** chỉ-đọc thì không bao giờ nhận được giá trị, nên vẫn bị báo là unmatched.

**Xác nhận khi export (thay đổi hành vi).** Trong quá trình export collection, nếu bất kỳ sheet `Existing Data Class` nào được chấp nhận có thành viên xuất ra mà class không nhận được, SheetX sẽ hỏi một lần: hộp thoại **Existing Data Class members** với hai lựa chọn **Export Anyway** và **Cancel**. Đây là một câu hỏi gộp cho toàn bộ phần export collection, liệt kê đủ nguồn, tên sheet, tên class và tên thành viên có vấn đề, và được hỏi **trước khi phần export collection ghi bất cứ thứ gì**.

- **Export Anyway** ghi ra đúng những gì các phiên bản trước vẫn ghi; nội dung JSON không thay đổi.
- **Cancel** hủy phần **collection** của lần export, kèm thông báo `Collection export cancelled: unmatched Existing Data Class members were not confirmed.` Không có JSON collection nào và không có mã nguồn collection nào được ghi ra, nên không để lại output collection dở dang. Hủy vì một sheet đồng nghĩa với hủy luôn các sheet collection còn lại.
- **Cancel không hoàn tác các giai đoạn đã chạy trước đó trong cùng lần export.** Bước collection chạy sau phần JSON thông thường, và với **Export All** thì còn sau cả IDs và Constants. Những file đó đã nằm trên ổ đĩa tại thời điểm hộp thoại hiện ra và vẫn ở nguyên đó — `Cancel` chỉ chặn phần ghi của collection chứ không rollback cả lần export. Hãy sửa class hoặc tên cột rồi chạy lại export.
- Mọi phát hiện đều được ghi log dạng warning bất kể hộp thoại có hiện hay không, nên Console luôn giữ đủ danh sách kể cả khi hộp thoại cắt bớt nội dung.
- **Chạy batch hoặc headless không bao giờ hiện hộp thoại này.** Cùng những phát hiện đó được ghi ra warning và phần export collection vẫn tiếp tục. Điều này chỉ áp dụng cho trường hợp lệch thành viên, không làm nới lỏng các trường hợp dừng khác: đổi chế độ lưu trữ vẫn khiến lần chạy headless dừng lại với thông báo "was not confirmed" (mục 7.4), và sheet có JSON không deserialize được vào row type vẫn bị bỏ qua kèm lỗi. `SheetXExporter` detached và các API batch vốn không mang binding collection nào, nên phép kiểm tra này không phát sinh ở đó.

Một cột đã âm thầm làm mất dữ liệu suốt nhiều tháng giờ sẽ chặn một lần export vốn trước đây vẫn chạy trơn tru. Đó đúng là mục đích: hộp thoại này là lần đầu tiên sự sai lệch đó được báo ra, thay vì tiếp tục lặng lẽ vứt bỏ giá trị. Hãy bổ sung thành viên còn thiếu vào class hoặc đổi tên cột cho khớp, hộp thoại sẽ không hiện nữa.

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
