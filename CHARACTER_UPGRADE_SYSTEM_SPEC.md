# 角色局外養成升級系統（Character Upgrade System）規格與 UI 接班手冊

> **本文件旨在提供給後續負責 UI 製作與場景搭建的開發人員／AI 助理。**
> 後端邏輯、數據持久化（PlayerPrefs）、貨幣扣除管線、戰鬥內屬性注入（CharacterStats）以及 UI 控制器代碼均已全面編寫並通過編譯。接班者只需依據本指南進行 UI 視覺排版與元件綁定即可。

---

## 目錄
1. [系統總覽與運作架構](#1-系統總覽與運作架構)
2. [核心代碼檔案清單](#2-核心代碼檔案清單)
3. [數據模型與核心 API 規格](#3-數據模型與核心-api-規格)
4. [現成 UI 控制器：CharacterUpgradeUIController](#4-現成-ui-控制器characterupgradeuicontroller)
5. [大廳整合機制（LobbyManager 連動）](#5-大廳整合機制lobbymanager-連動)
6. [推薦的 UI 階層結構與排版設計建議](#6-推薦的-ui-階層結構與排版設計建議)
7. [測試與驗證流程](#7-測試與驗證流程)

---

## 1. 系統總覽與運作架構

本系統允許玩家在**選角大廳（LobbyScene）**消耗通關獲得的局外貨幣**「靈魂（Soul）」**，對角色的三大核心基礎屬性進行永久強化：

```mermaid
flowchart TD
    subgraph 貨幣與存檔層
        MPM[MetaProgressionManager<br>持有總靈魂、扣除與儲存]
        PP[PlayerPrefs 本地持久化<br>MetaUpgrade_{charID}_{type}]
    end

    subgraph 養成邏輯層
        CUM[CharacterUpgradeManager<br>等級查詢、消耗計算、升級執行]
        SO[CharacterUpgradeConfigSO<br>數值加成與消耗曲線設定]
    end

    subgraph 大廳表現層
        UI[CharacterUpgradeUIController<br>按鈕狀態、等級文字、靈魂顯示]
        LM[LobbyManager<br>左側角色總屬性條即時動態跳動]
    end

    subgraph 戰鬥注入層
        SR[StartRoom.Init<br>開局實體化玩家時]
        CS[CharacterStats<br>以 StatModifier 形式注入<br>不破壞原有基礎裝備數值]
    end

    MPM -->|扣除靈魂| CUM
    SO -->|提供曲線| CUM
    CUM -->|讀寫存檔| PP
    CUM -->|廣播更新事件| UI
    CUM -->|廣播更新事件| LM
    CUM -->|進入地牢時注入| CS
    SR -->|觸發注入| CUM
```

* **升級屬性類型**：
  1. `Health`（生命上限）：每級 $+15$ HP，滿級 Lv.10 提供 $+150$ HP。
  2. `Defense`（防禦護甲）：每級 $+3$ 護甲，滿級 Lv.10 提供 $+30$ 護甲。
  3. `Attack`（基礎攻擊）：每級 $+4$ 攻擊力，滿級 Lv.10 提供 $+40$ 攻擊力。
* **無痛注入機制**：
  進入戰鬥房間（`StartRoom`）時，系統會自動在玩家身上掛載來源標籤為 `"MetaUpgrade"` 的屬性修正器（`StatModifier`），與裝備加成完美疊加，絕不會產生數值漂移或污染基礎預製體。

---

## 2. 核心代碼檔案清單

| 檔案路徑 | 類型 | 說明 |
| :--- | :--- | :--- |
| `Assets/Script/MetaProgression/UpgradeType.cs` | 核心結構 | 定義 `UpgradeType` 列舉與 `UpgradeInfo` 資料包 |
| `Assets/Script/MetaProgression/CharacterUpgradeConfigSO.cs` | 配置資產 | 數值加成、最大等級、消耗曲線之 ScriptableObject 定義 |
| `Assets/Script/MetaProgression/CharacterUpgradeManager.cs` | 單例管理器 | 掌管升級、扣款、存檔、發送事件、戰鬥屬性注入之中央服務 |
| `Assets/Script/UI/Lobby/CharacterUpgradeUIController.cs` | UI 控制器 | **【UI 製作重點】** 負責按鈕點擊、文字更新、貨幣足夠與否的視覺反饋 |
| `Assets/Script/Lobby/LobbyManager.cs` | 大廳整合 | 已接入養成加成，升級時大廳左側進度條會即時動態增加 |
| `Assets/Script/Scene/Room/StartRoom.cs` | 戰鬥注入 | 生成玩家時自動呼叫 `ApplyUpgradesToPlayer` |
| `Assets/Resources/CharacterUpgradeConfig.asset` | 數值實體 | 預設的升級配置檔，可在 Inspector 自由調整消耗與數值 |

---

## 3. 數據模型與核心 API 規格

### 3.1 枚舉與資訊結構
```csharp
public enum UpgradeType
{
    Health,   // 生命上限
    Defense,  // 護甲/防禦
    Attack    // 基礎攻擊力
}

public struct UpgradeInfo
{
    public UpgradeType type;        // 類型
    public string displayName;      // 顯示名稱 (如: "生命上限")
    public int currentLevel;        // 目前等級 (0 ~ maxLevel)
    public int maxLevel;            // 等級上限 (預設 10)
    public bool isMaxLevel;         // 是否已滿級
    public int nextCost;            // 升到下一級所需的靈魂 (若滿級為 0)
    public float currentBonus;      // 當前累積獲得的加成 (如: 45)
    public float nextBonus;         // 升級後將達到的總加成 (如: 60)
    public bool canAfford;          // 玩家是否買得起且未滿級
}
```

### 3.2 `CharacterUpgradeManager` 常用 API
後續 UI 代碼如需手動呼叫，可直接使用以下方法（均已支援防呆）：

```csharp
// 1. 取得指定角色某項屬性的即時資訊包 (最推薦 UI 綁定使用)
UpgradeInfo info = CharacterUpgradeManager.Instance.GetUpgradeInfo(characterID, UpgradeType.Health);

// 2. 檢查是否能升級 (等級未滿且靈魂足夠)
bool canUpgrade = CharacterUpgradeManager.Instance.CanUpgrade(characterID, UpgradeType.Health);

// 3. 執行升級 (會自動扣除靈魂、存檔、廣播事件；成功傳回 true)
bool success = CharacterUpgradeManager.Instance.TryUpgrade(characterID, UpgradeType.Health);

// 4. 取得當前累積加成 (如查詢戰士當前加了多少攻擊)
float atkBonus = CharacterUpgradeManager.Instance.GetStatBonus(characterID, UpgradeType.Attack);

// 5. 事件監聽 (當升級成功時會觸發)
CharacterUpgradeManager.OnAnyUpgradeChanged += YourRefreshMethod;
CharacterUpgradeManager.OnCharacterUpgradeChanged += (charId, type, newLvl) => { ... };
```

---

## 4. 現成 UI 控制器：`CharacterUpgradeUIController`

我們已經為 UI 製作人員寫好了專屬的控制腳本 [`CharacterUpgradeUIController.cs`](file:///c:/Unity/Dungeon/Assets/Script/UI/Lobby/CharacterUpgradeUIController.cs)，您**不需要重寫按鈕監聽邏輯**，只需將它掛在大廳的升級面板上並拖入對應的 UI 元件。

### Inspector 欄位對應表：

* **持有貨幣**：
  * `currentSoulsText`：綁定顯示「目前持有的總靈魂」文字（例如 `1,420 靈魂`）。
* **三大插槽（Health / Defense / Attack）**，每個插槽具備：
  * `statTitleText`：顯示屬性名稱（如「生命強化」）。
  * `levelText`：顯示等級進度（例如 `Lv. 3 / 10`，滿級顯示 `Lv.10 (MAX)`）。
  * `bonusText`：顯示當前與預覽加成（例如 `+45 HP → +60`，滿級顯示 `+150 HP (最大)`）。
  * `costText`：顯示下一級消耗（靈魂不足時自動變紅字 `<color=#FF6666>`，滿級顯示 `已滿級`）。
  * `upgradeButton`：升級按鈕（買不起或滿級時自動設定為不可點擊 `interactable = false`）。
  * `buttonCanvasGroup`：按鈕透明度（滿級時為 `0.4`，可用時為 `1.0`）。

### 按鈕點擊方法（可直接於 Inspector 的 Button OnClick() 綁定）：
* `OnClickUpgradeHealth()`
* `OnClickUpgradeDefense()`
* `OnClickUpgradeAttack()`

---

## 5. 大廳整合機制（LobbyManager 連動）

大廳核心腳本 [`LobbyManager.cs`](file:///c:/Unity/Dungeon/Assets/Script/Lobby/LobbyManager.cs) 已經完成以下對接：

1. **切換角色時自動通知升級介面**：
   在 `LobbyManager` 的 Inspector 中，有一個欄位：
   * `[SerializeField] private CharacterUpgradeUIController upgradeUIController;`
   只要把場景中的升級面板拖入這個欄位，點選不同角色時，升級面板就會自動切換顯示該角色的強化等級！
2. **大廳屬性條實時聯動**：
   玩家在升級面板點擊升級生命、防禦或攻擊時，大廳左側原有的進度條會**立刻播放 DOTween 動畫**，即時加上升級後的數值，體驗極其流暢！

---

## 6. 推薦的 UI 階層結構與排版設計建議

建議在 `LobbyScene` 的 Canvas 內建立升級面板，可採取以下兩種推薦佈局之一：

### 推薦方案 A：整合至左側面板下方（最直覺、同屏一覽）
放在目前左側的屬性面板（`LeftStatsPanel`）下方或做成第二分頁，玩家一邊看角色屬性一邊升級。

### 推薦方案 B：右上角／右側滑入式彈出視窗（獨立、美觀）
點擊大廳右上角或左側按鈕「角色強化」，彈出一個獨立面板。

### 推薦 Hierarchy 結構範例：
```text
Canvas
└── CharacterUpgradePanel (掛載 CharacterUpgradeUIController)
    ├── Panel_Background (半透明黑底 + 金屬邊框)
    ├── Header_Title (Text: "角 色 養 成")
    ├── CurrencyDisplay (Text: "持 有 靈 魂: 1,250")
    ├── Slot_Health
    │   ├── StatName (Text: "生命上限")
    │   ├── Level (Text: "Lv. 3 / 10")
    │   ├── Bonus (Text: "+45 HP → +60")
    │   └── Btn_Upgrade (Button: 綁定 OnClickUpgradeHealth)
    │       └── CostText (Text: "140 靈魂")
    ├── Slot_Defense
    │   ├── StatName (Text: "防禦護甲")
    │   ├── Level (Text: "Lv. 2 / 10")
    │   ├── Bonus (Text: "+6 護甲 → +9")
    │   └── Btn_Upgrade (Button: 綁定 OnClickUpgradeDefense)
    │       └── CostText (Text: "90 靈魂")
    └── Slot_Attack
        ├── StatName (Text: "基礎攻擊")
        ├── Level (Text: "Lv. 4 / 10")
        ├── Bonus (Text: "+16 攻擊 → +20")
        └── Btn_Upgrade (Button: 綁定 OnClickUpgradeAttack)
            └── CostText (Text: "220 靈魂")
```

---

## 7. 測試與驗證流程

接班人員在搭完 UI 後，可透過以下步驟進行快速功能驗證：

1. **快速取得靈魂進行測試**：
   * 在 Hierarchy 找到掛有 `MetaProgressionManager` 的物件（或執行時選取它）。
   * 在 Inspector 點擊右上角三點選單（ContextMenu），點擊 **「測試：增加 1000 靈魂」**，即可立即充值靈魂。
2. **升級測試**：
   * 點擊升級生命、防禦或攻擊按鈕。
   * 觀察：
     * 消耗靈魂是否扣除。
     * 等級文字是否變為 `Lv. 1 / 10`。
     * 加成文字是否即時變更。
     * 左側角色的大廳屬性條是否即時增長。
3. **戰鬥注入測試**：
   * 升級幾次生命後，點擊「開始遊戲」進入地牢關卡。
   * 觀察角色血條：除了原本騎士套裝的 177 HP 外，額外增加了升級的生命值（例如升 2 級生命，進遊戲血量正確顯示為滿血 `207 / 207`）。
   * 攻擊敵人時傷害也有顯著提升。
4. **重置存檔**：
   * 在 `CharacterUpgradeManager` 的 Inspector 右鍵選單，或執行代碼 `CharacterUpgradeManager.Instance.ResetCharacterUpgrades(0)`，可一鍵清空升級等級。

---
*本手冊由後端架構工程師交付，所有代碼均已就緒，請 UI 接班人員放心搭建界面！*
