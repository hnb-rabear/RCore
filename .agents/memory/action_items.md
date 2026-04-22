# Action Items

---

## Session: 2026-04-22 — Skills Cleanup + Panel System Analysis

### Đã hoàn thành
- [x] Phân tích PanelController, PanelStack, PanelRoot — đề xuất bugs, features, improvements
- [x] Dọn dẹp `.agents/skills/`: xóa 10 skills không liên quan (OAC/Python/web), rewrite 7 skills cho Unity/RCore context
- [x] Thêm multi-package awareness vào changelog, memory, writing-plans skills
- [x] Khởi tạo `.agents/memory/` system

### TODO
- [ ] Implement Panel System improvements (từ phân tích trước đó):
  - [ ] Fix coroutine chạy trên TimerEventsInScene thay vì panel — lifecycle risk
  - [ ] Cache Animator.parameters thay vì check mỗi transition
  - [ ] Thêm Android Back Button handling trong PanelRoot
  - [ ] Thêm transition timeout safety
  - [ ] Thêm typed data passing: Show<TData>(data)
  - [ ] Sửa typo CreatDimmerOverlay → CreateDimmerOverlay
- [ ] Migrate remaining legacy models sang [Inject] pattern
- [ ] Xem xét đổi Stack<T> thành List<T> trong PanelStack (cần Contains/iteration)
