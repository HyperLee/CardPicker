# 研究文件：餐點卡牌網站設計決策

**研究日期**: 2026-04-19  
**範疇**: ASP.NET Core 10 Razor Pages + 本機 JSON 持久化 + 抽卡公平性 + 測試與安全設計  
**適用約束**: 單機單人、無 DBMS、桌面瀏覽器、p95 < 200ms、頁面載入 < 3 秒、單一請求記憶體 < 100MB

---

## 1. JSON 檔案位置與首次種子資料

### Decision

將卡牌庫固定存放在執行期 `{ContentRootPath}/data/cards.json`，對應 repo 內規劃位置 `CardPicker/data/cards.json`。應用程式啟動時若檔案不存在，就建立 `data/` 目錄並寫入涵蓋早餐、午餐、晚餐的預設種子資料；若檔案已存在則直接載入，絕不覆寫使用者既有資料。

### Rationale

- `ContentRootPath` 對 ASP.NET Core Razor Pages 是最自然的應用程式資料根目錄，不會把使用者資料暴露到 `wwwroot/`。
- 單一 JSON 檔案符合規格要求，也讓備份、搬移與人工檢查都很直覺。
- 首次啟動才補種子資料，能同時滿足「預載可立即抽取的卡牌」與「重新啟動後保留最新狀態」兩個需求。

### Alternatives considered

- **`wwwroot/`**：會把資料暴露成靜態資源，違反安全與資料隔離需求。
- **作業系統 AppData 路徑**：雖可行，但會增加定位與備份成本，對目前單機本地專案沒有必要。
- **SQLite 或其他資料庫**：超出需求並增加部署與測試複雜度。

---

## 2. 序列化格式、版本欄位與原子寫入

### Decision

使用 `System.Text.Json` 讀寫版本化根文件，格式為：

```json
{
  "schemaVersion": "1.0",
  "cards": [
    {
      "id": "0195f2f4e7d47c6496ef0bbca4e6df6d",
      "name": "牛肉麵",
      "mealType": "Lunch",
      "description": "湯頭濃郁，晚下班時很適合。",
      "createdAtUtc": "2026-04-19T00:00:00Z",
      "updatedAtUtc": "2026-04-19T00:00:00Z"
    }
  ]
}
```

所有寫入都採用「同目錄暫存檔 → `File.Move(..., overwrite: true)` 覆寫正式檔」模式，以保留最後一份完整可讀資料。

### Rationale

- `System.Text.Json` 為 .NET 10 內建，無額外依賴，速度與維護性都足夠。
- `schemaVersion` 讓未來欄位演進時能明確處理相容性，而不是把格式假設寫死。
- 同目錄暫存檔覆寫能在單機單人場景下提供足夠的當機安全性；就算寫入中斷，也通常只會留下舊檔 + `.tmp`，不會把正式檔寫壞。

### Alternatives considered

- **Newtonsoft.Json**：功能完整，但此案例不需要額外序列化特性。
- **直接覆寫正式檔**：實作較簡單，但一旦進程中斷就可能留下半寫入內容。
- **Append-only 日誌格式**：讀寫模型較複雜，不適合目前以簡單 CRUD 為主的資料量級。

---

## 3. 不可變 ID 與重複卡牌判定

### Decision

卡牌 ID 使用 .NET 10 內建的 `Guid.CreateVersion7()` 產生，建立後以字串形式持久化並視為不可變欄位。重複卡牌判定不另存雜湊欄位，而是在新增與編輯前，以正規化後的 `(Name, MealType, Description)` 組合做比對：

- `Name`、`Description`：先 `Trim()`，再統一換行格式。
- 文字比對：採不分大小寫比較，避免 `Burger` / `burger` 類型的意外重複。
- `MealType`：必須是 `Breakfast`、`Lunch`、`Dinner` 其中之一。

### Rationale

- `Guid.CreateVersion7()` 具排序友善與內建支援，不需額外套件，適合作為本機持久化實體的唯一鍵。
- 目前資料量級小，以正規化後的欄位組合直接比對即可達到清晰且可維護的去重邏輯。
- 在編輯時重跑相同比對，可防止使用者把一張既有卡牌改成與另一張完全相同。

### Alternatives considered

- **ULID**：同樣可行，但需要額外依賴或自建實作；目前 Guid v7 已能滿足需求。
- **整數流水號**：需要另外維護遞增狀態，不如 Guid v7 穩定。
- **SHA256 持久化雜湊欄位**：能加速比對，但對目前規模屬於過早優化。

---

## 4. 抽卡公平性、搜尋規則與驗證邊界

### Decision

抽卡由獨立服務負責，流程固定為：

1. 驗證使用者已選擇餐別。
2. 先依餐別過濾卡牌。
3. 若卡池為空，回傳明確空狀態而非虛假結果。
4. 使用 `RandomNumberGenerator.GetInt32(pool.Count)` 從卡池中均勻選出索引。

搜尋則以 `CardSearchCriteria` 表示條件，支援：

- 名稱關鍵字：大小寫不敏感的部分比對。
- 餐別：可選。
- 同時指定時採 AND 條件。

驗證責任切分如下：

- **PageModel / Input Model**：處理欄位綁定、必填檢查、Anti-Forgery 與錯誤訊息顯示。
- **Service**：處理重複卡牌、抽卡空池、ID 不可變、刪除後不可再被搜尋或抽出等業務規則。
- **Repository**：只負責讀寫 JSON 與保護持久化原子性。

### Rationale

- 先過濾再抽索引，最直接地滿足「僅從指定餐別、且完全等機率抽出」。
- 讓 PageModel 保持協調角色，能讓測試聚焦在真正的業務規則與頁面契約。
- 用明確空狀態取代例外訊息濫用，可讓 UI 與驗收情境保持一致。

### Alternatives considered

- **`OrderBy(Guid.NewGuid())` 再取第一筆**：效率差，且不需要完整打亂卡池。
- **所有驗證都寫在 PageModel**：會讓邏輯難測，也讓不同頁面難以重用。
- **只靠 client-side validation**：無法防止繞過前端的非法請求。

---

## 5. 測試、安全、日誌與專案結構

### Decision

規劃採用以下實作方向：

- **測試**
  - 單元測試：`MealCardService`、`MealDrawService`、搜尋與去重規則。
  - 整合測試：Razor Pages 路由、表單提交、Anti-Forgery、JSON repository 與實際檔案 I/O。
  - 測試環境使用臨時資料夾與獨立測試 JSON 檔，避免碰觸正式 `data/cards.json`。
- **安全**
  - 所有寫入表單都啟用 Anti-Forgery。
  - 依賴 Razor 預設 HTML encoding 防 XSS，避免輸出未編碼使用者文字。
  - 伺服器端以 Data Annotations + 服務層規則雙重驗證。
- **日誌**
  - Serilog 寫入 console 與 rolling file。
  - 關鍵記錄點包含：檔案初始化、寫檔失敗、驗證失敗、抽卡成功 / 空池、刪除確認與完成。
- **結構**
  - `Pages/`：首頁抽卡與 `Cards` CRUD / 搜尋頁面。
  - `Models/`：卡牌、搜尋條件、文件根模型、餐別列舉。
  - `Services/`：抽卡、卡牌存取、驗證與資料存取協調。
  - `Options/`：資料檔位置與初始化選項。

### Rationale

- 這種分層最能對應憲章中的 TDD、安全優先與資料完整性要求。
- 臨時檔測試策略可以同時驗證實際 JSON 格式與避免污染正式資料。
- Serilog 的雙輸出足以支撐本機應用程式的可觀察性，不需要引入更重的遙測基礎設施。

### Alternatives considered

- **只做整合測試**：回饋太慢，且難以清楚驗證抽卡與去重規則。
- **FluentValidation**：可用，但目前 Data Annotations 已足夠，能避免額外依賴。
- **把所有功能塞回首頁**：UI 複雜度過高，不利於 CRUD 與搜尋流程維護。

---

## 結論

本次研究已解決規劃階段的所有 `NEEDS CLARIFICATION`：

- JSON 持久化位置與格式已明確。
- 卡牌 ID、抽卡公平性與重複判定策略已明確。
- 測試、安全、日誌與頁面分層方向已明確。
- 目前設計無需憲章豁免，可直接進入任務拆分與實作。
