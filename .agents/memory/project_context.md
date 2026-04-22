# Project Context

**Cập nhật:** 2026-04-22

## Tổng quan
- **Tên dự án:** RCore — Unity C# game framework (UPM packages)
- **Repo:** `hnb-rabear/RCore`
- **Unity version:** 2022+ (LTS)
- **Ngôn ngữ:** C#

## Kiến trúc Multi-Package

| Package | Path | Version |
|---------|------|---------|
| **RCore Main** | `Assets/RCore/Main/` | 1.1.7 |
| **Ads** | `Assets/RCore/Services/Ads/` | 1.0.1 |
| **Firebase** | `Assets/RCore/Services/Firebase/` | 1.0.1 |
| **Game Services** | `Assets/RCore/Services/GameServices/` | 1.0.1 |
| **IAP** | `Assets/RCore/Services/IAP/` | 1.0.1 |
| **Notification** | `Assets/RCore/Services/Notification/` | 1.0.1 |
| **Sub** | `Assets/RCore/Sub/` | 1.0.1 |

## Core Systems (RCore Main)
- **JObjectDB** — JSON-based data persistence (PlayerPrefs), với DI system (`[Inject]` attribute)
- **PanelStack / PanelController / PanelRoot** — UI navigation framework (stack-based, coroutine transitions)
- **EventDispatcher** — Type-based event system (dùng `System.Type` key, không string hash)
- **ScreenSafeArea** — Device-safe UI layout
- **UnifiedFontReplacer** — Editor tool thay font hàng loạt
- **NameGenerator** — Random name generation (đã disable Arabic/Thai)

## Conventions
- Private fields: `m_` prefix
- DI: `[Inject]` attribute, resolve trong `JObjectModelCollection.PostLoad()`
- Panel lifecycle: `BeforeShowing/AfterShowing/BeforeHiding/AfterHiding` hooks
- Docs per package: mỗi package có `CHANGELOG.md`, `package.json`, `README.md` riêng
