# 快速開始：餐點抽卡網站

本文件提供目前實作完成版本的本機操作、smoke test 前提、人工驗收腳本，以及可用性與 FCP / LCP 量測方式。

---

## 1. 先決條件

- 已安裝 .NET 10 SDK
- 可使用 macOS、Windows 或 Linux
- 使用桌面瀏覽器（建議 Chrome；Safari、Firefox、Edge 亦可）
- 目前 repo root 為 `/Users/qiuzili/CardPicker`

---

## 2. 啟動專案

在 repo root 執行：

```bash
dotnet build
dotnet run --project CardPicker/CardPicker.csproj
```

- 實際網址以終端機輸出為準
- 首次啟動且 `CardPicker/data/cards.json` 不存在時，系統會自動建立資料檔並預載早餐 / 午餐 / 晚餐種子卡牌
- 若 `CardPicker/data/cards.json` 已存在，系統不得覆寫既有資料

---

## 3. Smoke test 與驗收資料前提

### 3.1 正式應用程式的預設資料假設

正式啟動流程預期使用 `CardPicker/data/cards.json`，且首次初始化後至少涵蓋：

- 1 張早餐卡牌
- 1 張午餐卡牌
- 1 張晚餐卡牌

### 3.2 Quickstart smoke test 固定夾具

自動化 smoke / acceptance 情境應使用：

- `CardPicker.Tests/Integration/Fixtures/cards.quickstart.json`

此夾具刻意只保留：

- 早餐：`火腿蛋吐司`
- 午餐：`Burger Bento`
- 晚餐：0 張

原因：

1. 先驗證抽卡與搜尋的正向流程
2. 再用晚餐空卡池驗證新增 → 編輯 → 刪除 → 空狀態 → 重啟持久化的完整鏈路
3. 避免既有晚餐卡牌干擾 create / draw / delete 的預期結果

### 3.3 Smoke test 操作前提

- 測試必須使用隔離資料檔，不可改寫 repo 內真正的 `CardPicker/data/cards.json`
- 所有 POST 表單都必須帶 Anti-Forgery Token
- 本文件的 CRUD 驗收步驟以晚餐卡牌為主，預設新增卡牌名稱如下：
  - 建立：`Smokehouse Burger Bowl`
  - 編輯後：`Smokehouse Burger Deluxe`

---

## 4. 最終主流程驗收腳本

### 4.1 首頁抽卡

1. 開啟 `/`
2. 確認頁面顯示 `今天吃什麼？`
3. 選擇 `早餐`
4. 點擊 `抽一張`
5. 確認頁面顯示：
   - `火腿蛋吐司`
   - `早餐`
   - `附近早餐店的招牌組合，五分鐘內可以外帶。`

### 4.2 卡牌瀏覽與搜尋

1. 開啟 `/Cards`
2. 確認列表頁標題為 `餐點卡牌庫`
3. 在 `關鍵字` 輸入 `BURGER`
4. `餐別` 選擇 `午餐`
5. 點擊 `搜尋`
6. 確認結果包含 `Burger Bento`
7. 確認結果不包含 `火腿蛋吐司`
8. 點擊 `查看詳情` 時，需可看到完整描述

### 4.3 新增卡牌

1. 進入 `/Cards/Create`
2. 輸入：
   - 餐點名稱：`Smokehouse Burger Bowl`
   - 餐別：`晚餐`
   - 描述：`炙燒煙燻風味，適合想吃飽的晚餐。`
3. 點擊 `儲存卡牌`
4. 確認返回 `/Cards`
5. 確認頁面顯示 `已成功新增餐點卡牌。`
6. 以 `keyword=burger&mealType=Dinner` 搜尋，確認可看到新卡牌
7. 回首頁抽 `晚餐`，確認可抽到 `Smokehouse Burger Bowl`

### 4.4 編輯卡牌

1. 進入 `/Cards/Edit?id={createdCardId}`
2. 更新為：
   - 餐點名稱：`Smokehouse Burger Deluxe`
   - 餐別：`晚餐`
   - 描述：`炙燒煙燻風味再升級，晚餐份量更有飽足感。`
3. 點擊 `儲存變更`
4. 確認返回 `/Cards`
5. 確認頁面顯示 `已成功更新餐點卡牌。`
6. 以 `keyword=DELUXE&mealType=Dinner` 搜尋，確認：
   - 可看到 `Smokehouse Burger Deluxe`
   - 不再出現 `Smokehouse Burger Bowl`
7. 回首頁抽 `晚餐`，確認結果與描述已同步更新

### 4.5 刪除卡牌

1. 進入 `/Cards/Delete?id={createdCardId}`
2. 勾選 `我已確認要刪除此餐點卡牌`
3. 點擊 `確認刪除`
4. 確認返回 `/Cards`
5. 確認頁面顯示 `已成功刪除餐點卡牌。`
6. 以 `keyword=burger&mealType=Dinner` 搜尋，確認顯示 `查無符合條件的餐點卡牌。`

### 4.6 刪除後的空卡池驗證

1. 回首頁 `/`
2. 選擇 `晚餐`
3. 點擊 `抽一張`
4. 確認頁面顯示 `目前沒有可抽取的餐點。`
5. 確認未顯示剛刪除的卡牌名稱或描述

### 4.7 持久化驗證

1. 完成新增 → 編輯 → 刪除後關閉應用程式
2. 重新執行：

```bash
dotnet run --project CardPicker/CardPicker.csproj
```

3. 開啟 `/Cards`
4. 確認仍看得到：
   - `火腿蛋吐司`
   - `Burger Bento`
5. 確認看不到：
   - `Smokehouse Burger Deluxe`

---

## 5. 自動化 smoke test 建議指令

若只想驗證 quickstart 主流程，可執行：

```bash
dotnet test --filter QuickstartSmokeTests
```

若要連同可用性 / 安全 / 效能 smoke baseline 一起確認，可再執行：

```bash
dotnet test --filter "FullyQualifiedName~QuickstartSmokeTests|FullyQualifiedName~SecurityHeadersTests|FullyQualifiedName~PerformanceSmokeTests|FullyQualifiedName~AccessibilityResponsiveSmokeTests"
```

---

## 6. SC-001 / SC-002 可用性量測方式

### 6.1 量測前準備

- 受測者需為首次接觸此網站者
- 建議至少 10 位受測者；通過門檻為 9 / 10 人成功
- 使用無痕視窗或清空瀏覽器狀態
- 以人工碼表或螢幕錄影計時
- 測試資料：
  - SC-001 可用正式種子資料或 quickstart fixture
  - SC-002 建議使用 quickstart fixture，避免搜尋結果過多

### 6.2 SC-001：首次抽卡 30 秒內完成

1. 從受測者看到首頁 `/` 開始計時
2. 告知任務：`請抽出一個你今天可以吃的餐點`
3. 受測者完成以下條件即算成功：
   - 有選擇餐別
   - 有按下 `抽一張`
   - 看見有效推薦結果
4. 記錄完成秒數
5. 驗收標準：90% 受測者需在 30 秒內完成

### 6.3 SC-002：20 秒內找到指定餐點卡牌

1. 從受測者看到 `/Cards` 開始計時
2. 告知任務：`請找出 Burger Bento 這張午餐卡牌`
3. 受測者可使用瀏覽、關鍵字搜尋、餐別篩選或查看詳情
4. 畫面出現目標卡牌即停止計時
5. 驗收標準：90% 受測者需在 20 秒內完成

---

## 7. FCP / LCP 實務量測步驟

### 7.1 量測條件

- 建議使用 Chrome DevTools 或 Lighthouse
- 在本機 release build 執行站台
- 同一頁面至少量測 3 次，取中位數
- 量測頁面至少包含：
  - 首頁 `/`
  - 卡牌列表頁 `/Cards`

### 7.2 建議流程（Chrome DevTools）

1. 執行：

```bash
dotnet build -c Release
dotnet run --project CardPicker/CardPicker.csproj -c Release
```

2. 用 Chrome 開啟目標頁面
3. 開啟 DevTools
4. 到 `Lighthouse` 或 `Performance`
5. 重新整理頁面並錄製
6. 記錄：
   - FCP（First Contentful Paint）
   - LCP（Largest Contentful Paint）

### 7.3 驗收門檻

- FCP < 1.5 秒
- LCP < 2.5 秒
- `/Cards` 搜尋與首頁抽卡的主要互動應在 1 秒內有可理解回應

### 7.4 實務判讀補充

- 若 FCP / LCP 偶發超標，先確認是否為首次編譯、瀏覽器擴充套件或背景程序干擾
- 請避免把第一次 JIT / 冷啟動結果當作唯一結論
- 若使用 quickstart fixture，記得它只代表 smoke dataset，不代表大量資料情境

---

## 8. 驗收重點整理

- 未選餐別時必須顯示 `請先選擇餐別。`
- 空卡池時必須顯示 `目前沒有可抽取的餐點。`
- 搜尋無結果時必須顯示 `查無符合條件的餐點卡牌。`
- 新增 / 編輯 / 刪除後，首頁抽卡與 `/Cards` 搜尋都必須立即反映最新狀態
- 重新啟動後，JSON 持久化結果必須保留
- 所有使用者可見訊息與文件皆應維持 zh-TW
