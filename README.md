# CardPicker

CardPicker 是一個以 **ASP.NET Core 10 Razor Pages** 建置的餐點抽卡網站，適合在本機用卡牌方式整理早餐、午餐、晚餐選項。**目前已實作可用的 Web 應用程式**，包含首頁抽卡、卡牌庫搜尋與詳情、卡牌新增/編輯/刪除、JSON 檔案持久化、種子資料初始化、結構化日誌，以及完整的單元與整合測試。

## 目前已實作的內容

- **首頁餐點抽卡**：在 `/` 依早餐、午餐、晚餐三種餐別抽出一張目前卡池中的餐點。
- **卡牌庫瀏覽 / 搜尋 / 詳情**：在 `/Cards` 依名稱關鍵字（不分大小寫）與餐別篩選，並可查看單張卡牌詳情。
- **卡牌管理**：提供 `/Cards/Create`、`/Cards/Edit?id=...`、`/Cards/Delete?id=...`，可建立、修改、刪除餐點卡牌。
- **本機 JSON 持久化**：資料存於 `CardPicker/data/cards.json`，文件格式目前為 **schema version 1.0**。
- **種子資料行為**：若 `CardPicker/data/cards.json` 不存在，啟動時會自動建立檔案並產生 3 張種子卡（早餐 / 午餐 / 晚餐各 1 張）；若檔案已存在，系統不會覆寫既有資料。
- **日誌**：使用 **Serilog** 輸出到主控台與 `CardPicker/logs/cardpicker-.log`（rolling file）。
- **測試**：`CardPicker.Tests` 使用 **xUnit、Moq、Microsoft.AspNetCore.Mvc.Testing / WebApplicationFactory**，涵蓋單元測試、整合測試、quickstart smoke、安全標頭、效能與可及性 smoke。
- **安全基線**：啟用 HTTPS 重新導向；非 Development 環境啟用 HSTS 與 CSP / Referrer-Policy / X-Frame-Options / X-Content-Type-Options / Permissions-Policy；POST 表單受 Anti-Forgery 保護；Razor 預設輸出編碼；卡片資料路徑限制在應用程式內容根目錄下。

> 補充：`/Privacy` 與 `/Error` 頁面目前仍以 ASP.NET Core 範本預設頁面為主，只做基本保留。

## 技術堆疊

- **後端 / Web**：ASP.NET Core 10、C# 14、Razor Pages
- **前端**：Bootstrap 5、jQuery、jQuery Validation
- **日誌**：Serilog
- **測試**：xUnit、Moq、Microsoft.AspNetCore.Mvc.Testing / WebApplicationFactory

## 先決條件

- **.NET 10 SDK**
- 一個桌面瀏覽器（Chrome、Safari、Firefox、Edge 皆可）

## 開發執行方式

以下指令皆以 **repo root** 為目前目錄：

```bash
dotnet build
dotnet run --project CardPicker/CardPicker.csproj
dotnet run --project CardPicker/CardPicker.csproj --launch-profile https
dotnet test
dotnet test --filter QuickstartSmokeTests
dotnet publish CardPicker/CardPicker.csproj -c Release
```

### 預設開發網址

來自 `CardPicker/Properties/launchSettings.json`：

- HTTP：`http://localhost:5280`
- HTTPS：`https://localhost:7271`

## 使用說明

1. 啟動站台。
2. 開啟首頁 `/`，選擇餐別後按 **抽一張**。
3. 前往 `/Cards` 瀏覽或搜尋卡牌。
4. 透過 `/Cards/Create` 新增卡牌，或在卡牌庫中進入編輯 / 刪除流程。
5. 所有新增、編輯、刪除都會直接寫回 `CardPicker/data/cards.json`。

## 資料與持久化

- 資料檔位置：`CardPicker/data/cards.json`
- JSON 文件欄位包含：`schemaVersion` 與 `cards`
- 目前 schema version：`1.0`
- 寫入策略：先寫入同目錄暫存檔，再以原子方式覆蓋正式檔案
- 預設種子資料目前包含：
  - 早餐：`火腿蛋吐司`
  - 午餐：`紅燒牛肉麵`
  - 晚餐：`鮭魚便當`

## 測試

目前 solution 包含 Web 專案 `CardPicker` 與測試專案 `CardPicker.Tests`。

- `dotnet test`：執行完整測試套件
- `dotnet test --filter QuickstartSmokeTests`：只跑主流程 smoke test

測試內容目前涵蓋：

- 首頁抽卡頁面與抽卡流程
- 卡牌庫列表 / 搜尋 / 詳情
- 建立、編輯、刪除卡牌流程
- JSON repository 與種子初始化行為
- 安全標頭與 Anti-Forgery
- quickstart、效能 smoke、可及性 / 響應式 smoke

## Repository / Project Structure

```text
.
├── CardPicker.sln
├── CardPicker/
│   ├── CardPicker.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Models/           # 餐點卡牌、抽卡結果、搜尋條件、JSON 文件模型
│   ├── Options/          # CardStorage 設定
│   ├── Services/         # 抽卡、卡牌查詢/CRUD、JSON repository、亂數提供者
│   ├── Pages/            # Razor Pages（首頁、Cards、Shared、Error、Privacy）
│   ├── data/             # cards.json 本機資料檔
│   ├── logs/             # Serilog rolling log 輸出目錄
│   └── wwwroot/          # Bootstrap、jQuery、CSS、前端 JS
├── CardPicker.Tests/
│   ├── Integration/      # WebApplicationFactory 與頁面整合測試
│   └── Unit/             # 服務與模型單元測試
└── specs/001-meal-card-picker/
    └── quickstart.md     # 本功能的快速開始與 smoke test 文件
```

## 安全姿態（目前實作）

- 目標情境是**單機、本機使用**，目前**沒有登入、帳號或角色權限系統**。
- 所有表單提交流程已有 Anti-Forgery 驗證。
- 生產環境會加上 HSTS 與多個安全標頭。
- Razor 頁面預設 HTML 編碼可降低 XSS 風險。
- 卡片存放路徑經過限制，避免寫出應用程式內容根目錄之外。

如果你要直接修改功能，建議先看 `CardPicker/Program.cs`、`CardPicker/Pages/`、`CardPicker/Services/` 與 `CardPicker.Tests/`。
