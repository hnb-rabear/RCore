---
description: Tự động phân tích code đã staged (git diff --staged) và viết git log ngắn gọn, tập trung vào tính năng (không liệt kê class/function).
---

# Workflow: Write Git Log (/write-git-log)

Quy trình này hướng dẫn Agent cách đọc các thay đổi đã được staged (`git diff --staged`) và tạo ra một git log chuyên nghiệp.
**CRITICAL REQUIREMENTS:**
1. **ABSOLUTELY NO VIETNAMESE** in the generated git log. The entire output must be 100% in pure English.
2. **NO CODE NAMES OR LINKS.** Do not include any function names, class names, or file names (like `score_image_ctr`). Do not use backticks (` ` `) for anything, as the IDE will automatically convert them to links. The log must be absolute plain text.

## Bước 1: Lấy danh sách file và nội dung thay đổi
Sử dụng công cụ `run_command` để chạy lệnh Git. Vì PowerShell đôi khi lỗi với `&&` hoặc character encoding, hãy chạy qua `cmd` hoặc tách lệnh.
// turbo-all
1. Chạy `git status` để xem các file bị ảnh hưởng.
2. Chạy `cmd /c "git diff --staged > git_diff_staged.txt"` để xuất log diff ra file (để tránh giới hạn ký tự của terminal output).

## Bước 2: Đọc file diff
1. Dùng công cụ `view_file` hoặc `read_file` để đọc nội dung của `git_diff_staged.txt`.
2. Sau khi đọc xong, lập tức dùng `run_command` chạy lệnh `del git_diff_staged.txt` để dọn dẹp.

## Bước 3: Phân tích và viết Git Log
Đọc hiểu git diff và tổng hợp lại thành một thông điệp commit theo nguyên tắc sau:
- **Chuẩn bị tiêu đề (Header):** Dùng chuẩn Conventional Commits (VD: `feat(LiveOps): Integrate new event scoring`, `fix(UI): Fix rank display in popup`, v.v.). **Toàn bộ log phải được viết bằng tiếng Anh (English). Tuyệt đối không dùng tiếng Việt.**
- **Nguyên tắc "NGẮN GỌN - TẬP TRUNG":**
  - **CRITICAL:** Do NOT include ANY code names, function names (like score_image_ctr), class names, or files.
  - **CRITICAL:** Do NOT use markdown backticks (`) anywhere in the text. Write as plain text so the UI does not turn them into clickable links.
  - Mỗi gạch đầu dòng phải trình bày theo góc nhìn **tính năng** hoặc **mục đích của thay đổi** (VD: "Fix bug where event score is not granted at round end").
  - Go straight to the point. Group changes logically by feature.

## Bước 4: Hiển thị kết quả
Trả về nội dung git log cho người dùng trong một block markdown `text` để người dùng dễ dàng bấm copy.
Ví dụ cấu trúc đầu ra mong muốn:
```text
```text
feat(GoodsCup): Update scoring mechanism and event result display

- Automatically grant event points to players based on level difficulty upon victory.
- Fix event not automatically ending when real time expires.
- Update event result UI to display Total Score instead of Rank.
- Improve random bot names: block inappropriate keywords and ensure display format matches regional culture.
```
