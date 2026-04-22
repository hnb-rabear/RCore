# Developer Profile

**Cập nhật:** 2026-04-22

## Preferences
- Viết memory bằng tiếng Việt, code comments bằng tiếng Anh
- Git log / CHANGELOG viết bằng tiếng Anh
- Thích git log ngắn gọn, tập trung vào tính năng, không liệt kê class/function
- Không dùng backticks trong git log (IDE convert thành links)
- Ưu tiên thiết kế đơn giản, tránh over-engineering

## Coding Style
- Private fields: `m_` prefix
- Prefer `[SerializeField]` cho Inspector exposure
- Dùng `[Inject]` cho DI thay vì constructor injection
- Ưu tiên coroutine cho UI transitions (chưa migrate sang UniTask)

## Workflow
- Làm việc trên nhiều máy — cần memory sync qua git
- Dùng Antigravity (Gemini) làm AI coding assistant
- Skill brainstorming trước khi code feature mới
- Changelog trước khi push
