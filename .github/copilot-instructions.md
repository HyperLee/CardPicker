<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read `specs/001-meal-card-picker/plan.md`
<!-- SPECKIT END -->

# CardPicker — AI 編程助手指引

## 專案簡介

CardPicker 是一個卡牌選取與管理應用程式，提供卡牌選取演算法、隨機化邏輯與組合產生功能。目前處於初始架構階段，功能尚未實作。

**所有使用者面向文件（spec、plan、research）必須使用繁體中文（zh-TW）撰寫。**

## 技術堆疊

- **後端**: ASP.NET Core 10.0 + C# 14 + Razor Pages
- **前端**: Bootstrap 5 + jQuery + `wwwroot/css/site.css`
- **測試**: xUnit + Moq + WebApplicationFactory（規劃中）
- **日誌**: Serilog 

## 建構與執行

```bash
dotnet build                          # Debug 建構
dotnet run                            # 啟動（HTTP: localhost:5280）
dotnet run --launch-profile https     # HTTPS: localhost:7271
dotnet test                           # 執行所有測試
dotnet publish -c Release             # Release 發佈
```

## 架構重點

- 入口點：[Program.cs](../CardPicker/Program.cs)
- 頁面：[Pages/](../CardPicker/Pages/)（Razor Pages MVVM 模式）
- 版面：[Pages/Shared/_Layout.cshtml](../CardPicker/Pages/Shared/_Layout.cshtml)
- 設定：[appsettings.json](../CardPicker/appsettings.json)

## 編程規範

詳細的 C# 撰寫慣例請參閱 [.github/instructions/csharp.instructions.md](instructions/csharp.instructions.md)（套用於 `**/*.cs`）。  
核心原則與品質標準請參閱 [.specify/memory/constitution.md](../.specify/memory/constitution.md)。

**關鍵規範摘要**：
- 優先使用 C# 14 功能：模式匹配、switch 表達式、檔案範圍命名空間
- Nullable Reference Types 已啟用；在進入點檢查 null，使用 `is null` / `is not null`
- 所有公開 API 必須有 XML 文件註解（含 `<example>` 和 `<code>` 區段）
- 遵循 TDD：先寫測試，取得批准後再實作
- 測試中禁止使用 `// Arrange`、`// Act`、`// Assert` 註解

## 重要約束

- **資料完整性（NON-NEGOTIABLE）**：卡牌選取邏輯必須有完整的邊界值驗證與單元測試
- **安全優先**：輸入驗證、XSS/CSRF 防護、HTTPS Only
- **效能目標**：FCP < 1.5s、LCP < 2.5s，使用 async/await 避免阻塞
- 靜態資源使用 `MapStaticAssets`（非 `UseStaticFiles`）管線
