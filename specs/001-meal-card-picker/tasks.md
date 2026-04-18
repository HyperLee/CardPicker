# Tasks: 餐點抽卡網站

**Input**: 設計文件來自 `/specs/001-meal-card-picker/`  
**Prerequisites**: `plan.md`、`spec.md`、`research.md`、`data-model.md`、`contracts/meal-card-pages.md`、`quickstart.md`

**Tests**: 本功能已明確要求採 TDD 與可驗證的使用情境，因此各使用者故事皆先建立單元測試與整合測試、確認失敗，並在取得使用者批准後再進入實作。

**Organization**: 任務依使用者故事分組，讓每個故事都能獨立實作、測試與驗收。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可平行執行（位於不同檔案，且在其前置依賴完成後不互相阻塞）
- **[Story]**: 對應的使用者故事（`[US1]`、`[US2]`、`[US3]`）
- 每個任務描述都包含明確檔案路徑，方便直接執行

## Phase 1: Setup（共用初始化）

**Purpose**: 建立實作與測試專案骨架，讓後續基礎設施與故事任務可以接續落地。

- [X] T001 更新 `CardPicker/CardPicker.csproj`，加入 Serilog 與 Razor Pages 所需套件參考與設定
- [X] T002 建立 `CardPicker.Tests/CardPicker.Tests.csproj`，加入 xUnit、Moq、Microsoft.AspNetCore.Mvc.Testing 與測試 SDK 依賴
- [X] T003 更新 `CardPicker.sln`，將 `CardPicker.Tests/CardPicker.Tests.csproj` 納入方案管理
- [X] T004 [P] 建立 `CardPicker.Tests/GlobalUsings.cs`，整理 Web 專案與測試專案共用 using 與測試命名空間

---

## Phase 2: Foundational（阻塞性前置基礎）

**Purpose**: 完成所有使用者故事都依賴的資料模型、持久化、服務層與測試基礎設施。

**⚠️ CRITICAL**: 本階段完成前，不得開始任何使用者故事的頁面功能實作。

- [X] T005 [P] 建立 `CardPicker/Models/MealType.cs`、`CardPicker/Models/MealCard.cs`、`CardPicker/Models/CardLibraryDocument.cs`、`CardPicker/Models/CardSearchCriteria.cs`，定義 `Guid.CreateVersion7()` ID、`CreatedAtUtc` / `UpdatedAtUtc`、`SchemaVersion = "1.0"` 與核心驗證邊界
- [X] T006 [P] 建立 `CardPicker/Options/CardStorageOptions.cs` 與 `CardPicker/data/cards.json`，定義資料檔設定與三餐種子卡牌
- [X] T007 [P] 建立 `CardPicker/Services/IMealCardRepository.cs` 與 `CardPicker/Services/JsonMealCardRepository.cs`，實作版本化 JSON 載入、原子寫入、首次初始化與持久化失敗記錄邏輯
- [X] T008 [P] 建立 `CardPicker/Services/IMealCardService.cs` 與 `CardPicker/Services/MealCardService.cs`，封裝卡牌查詢、依 ID 讀取與共用驗證規則
- [X] T009 [P] 建立 `CardPicker.Tests/Integration/Infrastructure/CardPickerWebApplicationFactory.cs` 與 `CardPicker.Tests/Integration/Infrastructure/TestCardDataDirectory.cs`，提供隔離式整合測試宿主與臨時資料目錄
- [X] T010 [P] 建立 `CardPicker.Tests/Unit/Services/MealCardServiceTests.cs` 與 `CardPicker.Tests/Integration/Infrastructure/JsonMealCardRepositoryTests.cs`，先鎖定資料完整性、種子初始化與 JSON 持久化基線
- [X] T011 更新 `CardPicker/Program.cs`、`CardPicker/appsettings.json` 與 `CardPicker/appsettings.Development.json`，註冊 Serilog、HTTPS/HSTS、CSP、`CardStorageOptions`、Repository 與基礎服務

**Checkpoint**: 完成後可開始各使用者故事；US1、US2、US3 皆只依賴本階段成果即可獨立推進。

---

## Phase 3: User Story 1 - 依餐別抽卡快速決定餐點（Priority: P1） 🎯 MVP

**Goal**: 讓使用者在首頁選擇早餐、午餐或晚餐後，取得完全等機率且符合餐別的餐點推薦，並正確處理未選餐別與空卡池情境。

**Independent Test**: 預載卡牌資料後，進入 `/` 選擇任一餐別執行抽卡，確認結果僅來自該餐別、可顯示完整描述，且未選餐別或空卡池時顯示正確 zh-TW 訊息。

### Tests for User Story 1

**Gate**: `T012`、`T013` 完成並確認失敗後，必須先取得使用者批准，才能開始 `T014`-`T017`。

- [X] T012 [P] [US1] 建立 `CardPicker.Tests/Unit/Services/MealDrawServiceTests.cs`，先驗證未選餐別、空卡池、僅從指定餐別抽卡、固定亂數索引可覆蓋所有候選卡牌以證明等機率邏輯，與抽卡結果狀態轉換
- [X] T013 [P] [US1] 建立 `CardPicker.Tests/Integration/Pages/HomeDrawPageTests.cs`，先驗證首頁 `GET /` 與 `POST /?handler=Draw` 的抽卡流程、Anti-Forgery、CSP 與訊息輸出

### Implementation for User Story 1

- [X] T014 [P] [US1] 建立 `CardPicker/Models/DrawRequest.cs`、`CardPicker/Models/DrawResult.cs`、`CardPicker/Models/DrawResultState.cs`、`CardPicker/Services/IRandomIndexProvider.cs` 與 `CardPicker/Services/CryptoRandomIndexProvider.cs`，定義抽卡輸入輸出與可測試、可重現的亂數索引抽象
- [X] T015 [US1] 建立 `CardPicker/Services/IMealDrawService.cs` 與 `CardPicker/Services/MealDrawService.cs`，實作餐別驗證、以索引均勻映射的等機率抽卡、抽卡操作記錄與空池處理
- [X] T016 [US1] 更新 `CardPicker/Program.cs` 與 `CardPicker/Pages/Index.cshtml.cs`，接上抽卡服務、PageModel 狀態與表單提交流程
- [X] T017 [US1] 更新 `CardPicker/Pages/Index.cshtml` 與 `CardPicker/wwwroot/css/site.css`，實作餐別選擇、抽卡結果卡片、錯誤/空狀態訊息與可讀性樣式

**Checkpoint**: US1 完成後，首頁抽卡體驗即可單獨上線作為 MVP，且不依賴瀏覽或 CRUD 頁面也能產生價值。

---

## Phase 4: User Story 2 - 瀏覽與搜尋餐點靈感（Priority: P2）

**Goal**: 讓使用者能在 `/Cards` 瀏覽卡牌摘要、以名稱關鍵字與餐別過濾，並查看單一卡牌的完整描述內容。

**Independent Test**: 進入 `/Cards` 後，不輸入條件可看到名稱與餐別摘要；輸入名稱、餐別或兩者組合時，只顯示符合條件的卡牌；查無結果時顯示明確提示；選定卡牌時可看到完整描述。

### Tests for User Story 2

**Gate**: `T018`、`T019` 完成並確認失敗後，必須先取得使用者批准，才能開始 `T020`-`T022`。

- [ ] T018 [P] [US2] 建立 `CardPicker.Tests/Unit/Services/MealCardSearchTests.cs`，先驗證大小寫不敏感部分比對、餐別篩選與 AND 條件組合
- [ ] T019 [P] [US2] 建立 `CardPicker.Tests/Integration/Pages/CardLibraryPageTests.cs`，先驗證 `/Cards` 列表、搜尋、清除條件、空結果與詳細內容呈現

### Implementation for User Story 2

- [ ] T020 [P] [US2] 更新 `CardPicker/Pages/Cards/Index.cshtml.cs`，加入查詢條件綁定、清除條件後的 query reset、摘要列表載入與卡牌詳細內容選取狀態
- [ ] T021 [US2] 更新 `CardPicker/Pages/Cards/Index.cshtml`，實作搜尋表單、清除條件按鈕、卡牌摘要列表、詳細內容區與查無結果訊息
- [ ] T022 [US2] 更新 `CardPicker/Pages/Shared/_Layout.cshtml`，加入首頁抽卡與卡牌列表導覽，並為新增卡牌入口預留導覽位置且在 US3 完成前先隱藏或停用連結

**Checkpoint**: US2 完成後，使用者即使不抽卡，也能只靠瀏覽與搜尋完成餐點決策流程。

---

## Phase 5: User Story 3 - 維護本機餐點卡牌庫（Priority: P3）

**Goal**: 讓使用者能新增、編輯、刪除卡牌，並讓所有變更立即反映到瀏覽、搜尋、抽卡與 JSON 持久化結果。

**Independent Test**: 透過 `/Cards/Create`、`/Cards/Edit?id={cardId}`、`/Cards/Delete?id={cardId}` 依序驗證新增、修改與刪除，並在回到 `/Cards` 與首頁後確認資料已更新或移除。

### Tests for User Story 3

**Gate**: `T023`、`T024` 完成並確認失敗後，必須先取得使用者批准，才能開始 `T025`-`T030`。

- [ ] T023 [P] [US3] 建立 `CardPicker.Tests/Unit/Services/MealCardMutationTests.cs`，先驗證必填欄位、不可變 ID、`Trim()` + 換行正規化 + 大小寫不敏感去重、編輯撞成重複與刪除後不可再查得
- [ ] T024 [P] [US3] 建立 `CardPicker.Tests/Integration/Pages/CardManagementPageTests.cs`，先驗證新增、編輯、刪除、Anti-Forgery、確認刪除、不存在 ID、Create/Delete 寫入失敗提示與重啟後資料仍保留

### Implementation for User Story 3

- [ ] T025 [P] [US3] 建立 `CardPicker/Pages/Cards/CardFormInputModel.cs`，集中定義 Create/Edit 共用輸入模型與 zh-TW 驗證訊息
- [ ] T026 [US3] 更新 `CardPicker/Services/IMealCardService.cs` 與 `CardPicker/Services/MealCardService.cs`，補齊 `Guid.CreateVersion7()` 建立、不可變 ID、`CreatedAtUtc` / `UpdatedAtUtc` 維護、create/edit 共用的 `Trim()` + 換行正規化 + 大小寫不敏感重複比對、持久化協調與操作/驗證失敗日誌
- [ ] T027 [P] [US3] 更新 `CardPicker/Pages/Cards/Create.cshtml.cs` 與 `CardPicker/Pages/Cards/Create.cshtml`，實作新增頁面、Anti-Forgery、表單驗證、寫入失敗時保留輸入並顯示通用錯誤，以及成功導回流程
- [ ] T028 [P] [US3] 更新 `CardPicker/Pages/Cards/Edit.cshtml.cs` 與 `CardPicker/Pages/Cards/Edit.cshtml`，實作編輯頁面、既有資料預載、資料不存在 / 重複處理與儲存後刷新流程
- [ ] T029 [P] [US3] 更新 `CardPicker/Pages/Cards/Delete.cshtml.cs` 與 `CardPicker/Pages/Cards/Delete.cshtml`，實作刪除確認頁面、`confirmDelete` 明確確認、Anti-Forgery、刪除失敗時的通用錯誤提示與確認後永久移除流程
- [ ] T030 [US3] 更新 `CardPicker/Pages/Cards/Index.cshtml`、`CardPicker/Pages/Cards/Index.cshtml.cs` 與 `CardPicker/Pages/Shared/_Layout.cshtml`，串接新增/編輯/刪除操作入口、操作後狀態訊息，並在 US3 完成後啟用新增卡牌導覽連結

**Checkpoint**: US3 完成後，卡牌庫可由使用者自行維護，且所有後續瀏覽、搜尋與抽卡都會反映最新資料。

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: 補齊跨故事的一致性、操作引導、安全標頭、公開 API 文件與驗收覆蓋。

- [ ] T031 [P] 更新 `CardPicker/Pages/Shared/_ValidationScriptsPartial.cshtml` 與 `CardPicker/wwwroot/js/site.js`，補強共用 jQuery Unobtrusive Validation partial、CSP 相容前端行為與表單可用性細節
- [ ] T032 [P] 建立 `CardPicker.Tests/Integration/Pages/QuickstartSmokeTests.cs`，覆蓋 `specs/001-meal-card-picker/quickstart.md` 中的抽卡、搜尋、CRUD 與持久化主流程，作為 SC-003 / SC-004 的自動化驗收基線
- [ ] T033 更新 `specs/001-meal-card-picker/quickstart.md` 與 `CardPicker.Tests/Integration/Fixtures/cards.quickstart.json`，同步最終驗收步驟、SC-001 / SC-002 可用性量測腳本、FCP / LCP 驗收步驟、測試用種子資料假設與 smoke test 使用前提
- [ ] T034 [P] 建立 `CardPicker.Tests/Integration/Pages/SecurityHeadersTests.cs`，驗證首頁與 `/Cards` 相關頁面在正式環境組態下輸出 CSP 與必要安全標頭
- [ ] T035 [P] 更新 `CardPicker/Models/MealType.cs`、`CardPicker/Models/MealCard.cs`、`CardPicker/Models/CardLibraryDocument.cs`、`CardPicker/Models/CardSearchCriteria.cs`、`CardPicker/Models/DrawRequest.cs`、`CardPicker/Models/DrawResult.cs`、`CardPicker/Models/DrawResultState.cs`、`CardPicker/Options/CardStorageOptions.cs`、`CardPicker/Services/IMealCardRepository.cs`、`CardPicker/Services/JsonMealCardRepository.cs`、`CardPicker/Services/IMealCardService.cs`、`CardPicker/Services/MealCardService.cs`、`CardPicker/Services/IMealDrawService.cs`、`CardPicker/Services/MealDrawService.cs`、`CardPicker/Services/IRandomIndexProvider.cs` 與 `CardPicker/Services/CryptoRandomIndexProvider.cs`，補齊 XML 文件註解（含 `<example>` 與 `<code>`）並對齊憲章品質閘門
- [ ] T036 [P] 建立 `CardPicker.Tests/Integration/Pages/PerformanceSmokeTests.cs`，量測首頁抽卡 handler 與 `/Cards` 搜尋流程在測試資料集下的回應時間與單一請求記憶體基線，對齊 p95 < 200ms、主要互動 < 1 秒與單一請求記憶體 < 100MB 目標
- [ ] T037 [P] 建立 `CardPicker.Tests/Integration/Pages/AccessibilityResponsiveSmokeTests.cs`，驗證首頁與 `/Cards` 系列頁面的語意結構、鍵盤可達性、驗證訊息關聯與不同 viewport 下主要操作可用性
- [ ] T038 更新 `CardPicker/Pages/Index.cshtml`、`CardPicker/Pages/Cards/Index.cshtml`、`CardPicker/Pages/Cards/Create.cshtml`、`CardPicker/Pages/Cards/Edit.cshtml`、`CardPicker/Pages/Cards/Delete.cshtml` 與 `CardPicker/wwwroot/css/site.css`，補齊 responsive 版面、語意標記、focus 樣式、欄位說明與驗證訊息的 WCAG 2.1 對齊

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup（Phase 1）**: 無前置依賴，可立即開始
- **Foundational（Phase 2）**: 依賴 Setup 完成，且會阻塞所有使用者故事
- **User Stories（Phase 3+）**: 全部都依賴 Foundational 完成
- **Polish（Phase 6）**: 依賴欲交付的使用者故事完成後再執行

### User Story Dependencies

- **US1（P1）**: 僅依賴 Foundational，可在基礎層完成後立即獨立實作與驗收
- **US2（P2）**: 僅依賴 Foundational，可與 US1 並行，不需等待抽卡頁完成
- **US3（P3）**: 僅依賴 Foundational，可與 US1、US2 並行，但會延伸共用 `MealCardService`

### Within Each User Story

- 測試任務必須先完成、先看到失敗並取得使用者批准，再開始實作
- 先建立故事所需模型/輸入輸出，再實作服務與 PageModel
- 最後才補上 Razor 頁面、導覽與操作細節
- 每個故事完成後，都要能以該故事的獨立測試條件單獨驗收

### Parallel Opportunities

- **Setup**: `T004` 可與 `T002`、`T003` 並行
- **Foundational**: `T005`、`T006`、`T007`、`T008`、`T009`、`T010` 在前置骨架完成後可分工並行
- **US1**: `T012` 與 `T013` 可並行；`T014` 可在測試草稿完成後與頁面設計分工
- **US2**: `T018` 與 `T019` 可並行；`T020` 與 `T022` 可由不同成員分工
- **US3**: `T023` 與 `T024` 可並行；`T027`、`T028`、`T029` 可在 `T025`、`T026` 完成後同步展開
- **Polish**: `T031`、`T032`、`T034`、`T035`、`T036`、`T037` 可並行；`T033` 與 `T038` 在前述驗收資訊確認後收尾

---

## Parallel Example: User Story 1

```bash
# 先平行準備 US1 的測試：
Task: "建立 CardPicker.Tests/Unit/Services/MealDrawServiceTests.cs"
Task: "建立 CardPicker.Tests/Integration/Pages/HomeDrawPageTests.cs"

# 再平行準備 US1 的模型與亂數抽象：
Task: "建立 CardPicker/Models/DrawRequest.cs、CardPicker/Models/DrawResult.cs、CardPicker/Models/DrawResultState.cs"
Task: "建立 CardPicker/Services/IRandomIndexProvider.cs 與 CardPicker/Services/CryptoRandomIndexProvider.cs"
```

## Parallel Example: User Story 2

```bash
# 先平行準備 US2 的驗證：
Task: "建立 CardPicker.Tests/Unit/Services/MealCardSearchTests.cs"
Task: "建立 CardPicker.Tests/Integration/Pages/CardLibraryPageTests.cs"

# 再分工處理頁面與導覽：
Task: "更新 CardPicker/Pages/Cards/Index.cshtml.cs"
Task: "更新 CardPicker/Pages/Shared/_Layout.cshtml"
```

## Parallel Example: User Story 3

```bash
# 先平行準備 US3 的測試：
Task: "建立 CardPicker.Tests/Unit/Services/MealCardMutationTests.cs"
Task: "建立 CardPicker.Tests/Integration/Pages/CardManagementPageTests.cs"

# 完成共用輸入模型與服務後，再平行切頁面：
Task: "更新 CardPicker/Pages/Cards/Create.cshtml.cs 與 CardPicker/Pages/Cards/Create.cshtml"
Task: "更新 CardPicker/Pages/Cards/Edit.cshtml.cs 與 CardPicker/Pages/Cards/Edit.cshtml"
Task: "更新 CardPicker/Pages/Cards/Delete.cshtml.cs 與 CardPicker/Pages/Cards/Delete.cshtml"
```

---

## Implementation Strategy

### MVP First（只先交付 User Story 1）

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational
3. 完成 Phase 3: US1 抽卡首頁
4. 以首頁抽卡的獨立驗收條件驗證 MVP
5. 通過後再開始下一個故事

### Incremental Delivery

1. 先完成 Setup + Foundational，建立穩定基線
2. 交付 US1，讓使用者先能抽卡決定餐點
3. 交付 US2，補上瀏覽與搜尋決策路徑
4. 交付 US3，讓卡牌庫可由使用者長期維護
5. 最後執行 Polish，補齊跨流程品質與驗收

### Parallel Team Strategy

1. 全隊先共同完成 Phase 1 與 Phase 2
2. 基礎層完成後：
   - 開發者 A：US1 抽卡流程
   - 開發者 B：US2 瀏覽與搜尋
   - 開發者 C：US3 CRUD 與持久化驗證
3. 各故事以獨立測試條件驗收後再做整體整合

---

## Notes

- `[P]` 任務代表可在前置依賴完成後，由不同人同時處理
- `[US1]`、`[US2]`、`[US3]` 標籤讓每個任務都能回溯到對應故事
- 每個故事都保留獨立測試標準，避免只能整包驗收
- 任務描述已指定精確檔案路徑，可直接交由 LLM 或工程師逐項執行
- `US1` 是建議的 MVP 範圍；`US2`、`US3` 屬於增量交付
