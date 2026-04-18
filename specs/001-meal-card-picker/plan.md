# 實作計畫：餐點抽卡網站

**Branch**: `001-build-meal-card-site` | **Date**: 2026-04-19 | **Spec**: `specs/001-meal-card-picker/spec.md`  
**Input**: 功能規格來自 `specs/001-meal-card-picker/spec.md`

## Summary

以現有 ASP.NET Core 10 Razor Pages 專案為基礎，新增首頁抽卡與 `Cards` 管理頁面，讓使用者可在本機單一 JSON 檔案中維護餐點卡牌，並依早餐、午餐、晚餐三個餐別進行公平抽卡、瀏覽與搜尋。設計將採 `System.Text.Json` 的版本化文件格式、`Guid.CreateVersion7()` 產生不可變卡牌 ID、同目錄暫存檔原子覆寫、服務層封裝資料完整性規則，以及 xUnit + WebApplicationFactory 的測試優先實作路徑。

## Technical Context

**Language/Version**: C# 14 / .NET 10.0（ASP.NET Core Razor Pages）  
**Primary Dependencies**: ASP.NET Core Razor Pages、Bootstrap 5、jQuery、jQuery Validation、System.Text.Json、Serilog（Console Sink + File Sink）  
**Storage**: 單一本機 JSON 文字檔，路徑為執行期 `{ContentRootPath}/data/cards.json`，對應 repo 內規劃位置 `CardPicker/data/cards.json`  
**Testing**: xUnit + Moq（單元測試）與 WebApplicationFactory（整合測試）；必要時以 TestServer 或 mock-based 測試補足  
**Target Platform**: 單機桌面瀏覽器（Chrome、Firefox、Safari、Edge）  
**Project Type**: Web Application（Razor Pages）  
**Performance Goals**: API / Page handler p95 < 200ms、首次頁面載入 < 3 秒、換頁 < 1 秒  
**Constraints**: 單一請求記憶體 < 100MB、單機單人本機使用、不使用專案資料庫軟體、所有使用者面向文件與訊息採繁體中文、正式環境 HTTPS Only、靜態資源沿用 `MapStaticAssets` / `WithStaticAssets`  
**Scale/Scope**: 預設提供數張種子卡牌，涵蓋早餐 / 午餐 / 晚餐；資料量以數十到數百張卡牌為主要使用情境，支援 CRUD、搜尋、抽卡與明確空狀態提示

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### 初始閘門（Phase 0 前）

- ✅ **文件語言**：`plan.md`、`research.md`、`data-model.md`、`quickstart.md` 與 UI 契約皆以繁體中文撰寫。
- ✅ **技術一致性**：規劃維持 ASP.NET Core 10 + Razor Pages + Bootstrap 5 + jQuery，未引入資料庫軟體或偏離既有靜態資源管線。
- ✅ **測試優先**：實作階段將先建立單元與整合測試，再落地業務邏輯；計畫中的分層設計可支援 TDD。
- ✅ **安全優先**：表單契約預留 Anti-Forgery、Razor 自動編碼、伺服器端驗證與正式環境 HTTPS/HSTS。
- ✅ **資料完整性**：抽卡前檢查餐別與卡池、寫入前驗證必填與重複、ID 不可變、刪除後不得出現在搜尋與抽卡結果。

### Phase 1 設計後複查

- ✅ **程式碼品質**：以 `Models` / `Services` / `Pages` 分層，讓 PageModel 保持 UI 協調職責，業務規則集中於可測服務。
- ✅ **可觀察性**：規劃以 Serilog 寫入 console + rolling file，涵蓋啟動、寫檔錯誤、驗證失敗與抽卡操作。
- ✅ **效能與延展性**：單檔 JSON + 原子寫入足以支撐目前規模，搜尋與抽卡均為記憶體內操作，可滿足既定效能目標。
- ✅ **無憲章違例**：目前設計不需要額外複雜度豁免，`Complexity Tracking` 無需填寫例外理由。

## Project Structure

### Documentation（this feature）

```text
specs/001-meal-card-picker/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── meal-card-pages.md
└── tasks.md
```

### Source Code（repository root）

```text
CardPicker/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Models/
│   ├── MealCard.cs
│   ├── MealType.cs
│   ├── CardSearchCriteria.cs
│   └── CardLibraryDocument.cs
├── Options/
│   └── CardStorageOptions.cs
├── Services/
│   ├── IMealCardRepository.cs
│   ├── JsonMealCardRepository.cs
│   ├── IMealCardService.cs
│   ├── MealCardService.cs
│   ├── IMealDrawService.cs
│   └── MealDrawService.cs
├── Pages/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   ├── Cards/
│   │   ├── Index.cshtml
│   │   ├── Index.cshtml.cs
│   │   ├── Create.cshtml
│   │   ├── Create.cshtml.cs
│   │   ├── Edit.cshtml
│   │   ├── Edit.cshtml.cs
│   │   ├── Delete.cshtml
│   │   └── Delete.cshtml.cs
│   └── Shared/
├── data/
│   └── cards.json
├── logs/
│   └── cardpicker-.log
└── wwwroot/
    ├── css/
    ├── js/
    └── lib/

CardPicker.Tests/
├── Unit/
│   ├── Services/
│   └── Models/
└── Integration/
    ├── Pages/
    └── Infrastructure/
```

**Structure Decision**: 採單一 Razor Pages Web 專案搭配獨立測試專案。Web 專案內以 `Pages` 負責 UI 與頁面流程，`Services` 負責抽卡、搜尋、驗證與資料存取協調，`Models` 與 `Options` 承載資料模型與設定；測試專案則分為單元與整合測試，避免將業務規則綁死在 PageModel 中。

## Complexity Tracking

目前無需憲章豁免；未引入額外服務、資料庫或跨程序協調機制。
