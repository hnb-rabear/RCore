# Decisions Log

---

## 2026-04-22 — Skills directory cleanup

**Quyết định:** Xóa 10 skills không phù hợp (OAC Studio/Python/web), giữ 9 skills phù hợp cho Unity C# project.

**Lý do:** Skills được kế thừa từ project OAC Studio (Python/FastAPI + Vanilla JS), hoàn toàn không liên quan đến Unity C# framework.

**Skills đã xóa:** antigravity-design-expert, api-design-principles, architecture-patterns, async-python-patterns, python-fastapi-development, frontend-design, lint-and-validate, thumbnail-prompt-philosophy, doc-sync-on-change, debugging-strategies

**Skills giữ lại (đã rewrite):** agent-memory, brainstorming, changelog-before-push, code-reviewer, systematic-debugging, verification-before-completion, writing-plans

**Skills giữ nguyên:** doc-coauthoring, prompt-engineer

---

## 2026-04-22 — Loại bỏ doc-sync-on-change

**Quyết định:** Xóa skill doc-sync-on-change thay vì rewrite.

**Lý do:** Skill này hardcoded hoàn toàn cho TECHNICAL.md của OAC Studio với 12 sections cụ thể. RCore không có TECHNICAL.md — README.md là đủ.

---

## 2026-04-22 — Loại bỏ debugging-strategies (giữ systematic-debugging)

**Quyết định:** Xóa debugging-strategies, chỉ giữ systematic-debugging.

**Lý do:** Hai skills trùng chức năng. systematic-debugging có methodology 4-phase tốt hơn và đã được rewrite với Unity context.

---

## 2026-04-21 — DI System: [Inject] attribute

**Quyết định:** Implement reflection-based [Inject] cho JObjectModelCollection thay vì dùng external DI framework.

**Lý do:** Lightweight, không thêm dependency. Dùng Type.IsInstanceOfType() để support cả interface và concrete type injection. O(1) performance qua m_resolveCache.

**Constraint:** Registration order quan trọng — register leaf dependencies trước complex models.

---

## 2026-04-21 — EventDispatcher dùng System.Type key

**Quyết định:** Đổi từ string hash sang System.Type làm key cho EventDispatcher.

**Lý do:** Loại bỏ hoàn toàn string-hash collision risk.
