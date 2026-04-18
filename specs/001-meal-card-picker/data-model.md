# 資料模型：餐點抽卡網站

## 1. MealCard（餐點卡牌）

### 說明

代表一張可被瀏覽、搜尋、抽取、編輯與刪除的餐點卡牌。

### 欄位

| 欄位 | 型別 | 說明 | 驗證規則 |
|------|------|------|----------|
| `Id` | `string` | 系統自動產生的不可變唯一識別碼 | 建立時以 `Guid.CreateVersion7()` 產生；新增後不得修改；不得為空 |
| `Name` | `string` | 餐點名稱 | 必填；去除前後空白後不得為空 |
| `MealType` | `MealType` | 餐別 | 必填；僅允許 `Breakfast`、`Lunch`、`Dinner` |
| `Description` | `string` | 餐點描述、店家或備註 | 必填；去除前後空白後不得為空 |
| `CreatedAtUtc` | `DateTimeOffset` | 建立時間（UTC） | 建立時指派；不得回寫為未來時間 |
| `UpdatedAtUtc` | `DateTimeOffset` | 最後更新時間（UTC） | 新增與每次編輯時更新；不得早於 `CreatedAtUtc` |

### 商業規則

- 正規化後的 `(Name, MealType, Description)` 組合在卡牌庫中必須唯一。
- 被刪除後的卡牌不得再出現在搜尋結果與抽卡結果中。
- 編輯卡牌時只允許變更 `Name`、`MealType`、`Description`、`UpdatedAtUtc`；`Id` 與 `CreatedAtUtc` 保持不變。

---

## 2. MealType（餐別列舉）

### 說明

限制卡牌與抽卡流程僅能使用規格允許的三種餐別。

### 允許值

| 值 | 顯示文字 |
|----|----------|
| `Breakfast` | 早餐 |
| `Lunch` | 午餐 |
| `Dinner` | 晚餐 |

### 規則

- 使用者未選擇餐別時不得執行抽卡。
- 搜尋可不指定餐別；若指定則僅回傳該餐別卡牌。

---

## 3. CardSearchCriteria（搜尋條件）

### 說明

代表卡牌列表頁用來縮小結果集的查詢條件。

### 欄位

| 欄位 | 型別 | 說明 | 驗證規則 |
|------|------|------|----------|
| `Keyword` | `string?` | 名稱關鍵字 | 可為空；若有值則先 `Trim()`；以大小寫不敏感部分比對 |
| `MealType` | `MealType?` | 餐別篩選 | 可為空；若有值則必須是合法列舉值 |

### 規則

- `Keyword` 與 `MealType` 可單獨使用，也可同時使用。
- 同時使用時採 AND 條件。
- 無任何條件時回傳所有卡牌的摘要列表。

---

## 4. DrawRequest（抽卡請求）

### 說明

代表一次使用者在首頁發起的抽卡操作。

### 欄位

| 欄位 | 型別 | 說明 | 驗證規則 |
|------|------|------|----------|
| `SelectedMealType` | `MealType?` | 使用者欲抽取的餐別 | 必須有值才能進行抽卡 |

### 規則

- 先依 `SelectedMealType` 過濾出卡池後，再以均勻隨機方式抽出一張卡。
- 當卡池為空時，必須回傳明確空狀態與提示訊息，不得產生假結果。

---

## 5. DrawResult（抽卡結果）

### 說明

代表首頁抽卡後回傳給畫面的結果模型；屬於暫態資料，不需要單獨持久化。

### 欄位

| 欄位 | 型別 | 說明 |
|------|------|------|
| `CardId` | `string?` | 抽中的卡牌 ID；空值表示未抽中 |
| `State` | `DrawResultState` | 抽卡狀態 |
| `Message` | `string?` | 顯示給使用者的 zh-TW 訊息 |

### 狀態

| 狀態 | 說明 |
|------|------|
| `NotRequested` | 尚未抽卡 |
| `ValidationFailed` | 未選擇餐別或提交資料無效 |
| `EmptyPool` | 指定餐別目前沒有卡牌 |
| `Drawn` | 成功抽出一張卡牌 |

---

## 6. CardLibraryDocument（JSON 根文件）

### 說明

代表 `cards.json` 的根結構，作為單一持久化文件的邊界。

### 欄位

| 欄位 | 型別 | 說明 | 驗證規則 |
|------|------|------|----------|
| `SchemaVersion` | `string` | JSON 結構版本 | 初版固定為 `"1.0"`；載入時必須驗證 |
| `Cards` | `IReadOnlyList<MealCard>` | 卡牌集合 | 不可為 `null`；每張卡牌 `Id` 必須唯一 |

### 規則

- 讀取後需驗證所有卡牌資料完整性，包含必填欄位、合法餐別與唯一 ID。
- 寫入前應先在記憶體中完成所有業務驗證，再以原子方式覆寫正式檔。

---

## 關係摘要

```text
CardLibraryDocument 1 ── * MealCard
CardSearchCriteria   ── 篩選 ──> MealCard
DrawRequest          ── 指定餐別 ──> MealCard pool ──> DrawResult
```

---

## 狀態轉換

### MealCard 生命週期

```text
Seeded / Created
    └──> Updated (可重複)
            └──> Deleted
```

- `Deleted` 為終止狀態。
- 任何狀態都不得改寫既有 `Id`。

### DrawResult 生命週期

```text
NotRequested
    ├──> ValidationFailed
    ├──> EmptyPool
    └──> Drawn
```
