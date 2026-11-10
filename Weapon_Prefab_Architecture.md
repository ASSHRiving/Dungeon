# 武器 Prefab 基礎架構與批量製作規範指南
> **說明**：本規範文件分析自 `Assets/Prefab/Weapon/` 中現有武器預製件（以 `@Sword.prefab` 與 `@Axe.prefab` 最基礎架構為基準，排除粒子與拖尾等額外 VFX 特效），作為後續提供模型並批量生成武器 Prefab 的標準工程依據。

---

## 1. 架構核心概述 (Architecture Overview)

在當前專案中，武器 Prefab 承載了兩個核心生命週期角色：
1. **世界掉落物 (World Drop / Interactable Item)**：具有剛體物理與碰撞體，可掉落在地上並供玩家靠近互動拾取（按 E / 互動鍵），或在商店陳列。
2. **角色裝備實體 (Equipped Weapon)**：被玩家裝備後掛載在角色的右手武器槽位（`weaponHolder`），剛體變為 Kinematic，碰撞體關閉，並將武器自身的攻擊力、移動速度、音效、Hitbox 判定點（`AttackPoint`）以及動畫控制器（`AnimatorOverrideController`）動態註冊給角色戰鬥系統。

現有 Prefab 分為兩種類型：
- **基礎架構 Prefab（如 `Sword.prefab`、`Axe.prefab`）**：僅包含根節點、模型子節點、`AttackPoint` 子節點，以及 5 個基礎功能組件。乾淨簡潔，為所有新武器的標準模版。
- **特效增強 Prefab（如 `Katana`、`Daybreak`、`TitansTooth`、`Crag`、`StarEater`、`RoyalSword`）**：在基礎架構之上，額外擴充了 `TrailRenderer`（刀光拖尾）、`ParticleSystem`（斬擊火花 / 碎片）與動態光源（URP Light）。

---

## 2. 武器 Prefab 層級結構 (Hierarchy)

```
[武器名稱] (Root GameObject, Layer: 12 "Weapon", Tag: "Untagged")
├── [模型節點] (FBX Model Instance / Mesh Child, Layer: 12 "Weapon")
│     - 依據手握點 (Grip/Pivot) 對齊 Root 原點 (0, 0, 0)
│     - 武器朝向：刀刃 / 斧頭前端統一沿著 -X 軸 向前延伸
└── AttackPoint (Child GameObject, Layer: 12 "Weapon", Tag: "Untagged")
      - Transform:
          - Local Position: 置於武器攻擊打擊判定球心 (通常在 -X 軸的刀身中上段)
          - Local Rotation: (0, 0, 0)
          - Local Scale: (1, 1, 1)
```

### 重要空間規範 (Transform & Pivot Guidelines)
1. **手握點 (Pivot Alignment)**：
   - 當玩家拾取武器時，代碼會將武器 Root 父物件設為手部的 `weaponHolder`，並強制設置：
     ```csharp
     newWeapon.transform.localPosition = Vector3.zero;
     newWeapon.transform.localRotation = Quaternion.identity;
     ```
   - 因此，**Root GameObject 的 `(0, 0, 0)` 必須精準對應角色握持時的手心握點**。
   - 子物件模型（Model）需進行平移與旋轉偏移校正，使握柄位置對準 Root 原點。
2. **朝向規範**：
   - 角色裝備槽位的局部座標向前方向為 **`-X` 軸**。
   - 武器刀刃鋒刃、斧刃前端需沿著 `-X` 軸向前延伸，確保在角色揮舞動畫時朝向正確。
3. **Layer 規範**：
   - **所有節點（Root、Model、AttackPoint）的 Layer 必須統一設為 `12` (`Weapon`)**。

---

## 3. Root 必備組件清單與參數配置 (Required Components)

每個基礎武器 Prefab 的 Root 物件上，嚴格掛載以下 **5 個功能組件**：

### (1) `Transform`
- Position: `(0, 0, 0)`
- Rotation: `(0, 0, 0)`
- Scale: `(1, 1, 1)`

### (2) `Rigidbody`
作為地面掉落物時的重力與下落物理驅動。
- `Mass`: `1`
- `Linear Damping (Drag)`: `0`
- `Angular Damping`: `0.05`
- `Use Gravity`: `true` (`1`)
- `Is Kinematic`: `false` (`0`)
- `Interpolate`: `None` (`0`)
- `Collision Detection`: `Discrete` (`0`)
> *註：角色撿起裝備時，`CharacterCombatBase` 會自動將 `isKinematic` 切換為 `true`。*

### (3) `BoxCollider`
作為地面掉落物著地時的外圍實體碰撞盒。
- `Is Trigger`: `false` (`0`)
- `Material`: `None`
- `Center` & `Size`: 依據武器模型包裹盒自適應設置（包裹刀身長寬高，避免穿透地面）。
> *註：角色撿起裝備時，`CharacterCombatBase` 會自動將此 Collider 的 `enabled` 切換為 `false`，實際戰鬥判定由 `AttackPoint` 負責。*

### (4) `DropsInteract` (腳本)
GUID: `a60f232d8b049af42b69647bcd3ca4df`
玩家靠近按互動鍵（E）拾取的交互組件。
- `interactableName` (string): 顯示於畫面上的拾取名稱（繁體中文，如 `"劍"`、`"斧頭"`）。
- 裝備後該組件會自動被設為 `enabled = false`。

### (5) `ShopWeapon` (腳本)
GUID: `ca1089fa64b577645a9717f3952584cd`
地牢商店陳列與定價組件。
- `itemName` (string): 商店道具名稱（可留空，初始化時自動同步 `Weapon.weaponName`）。
- `price` (int): 購買價格（基礎武器預設為 `0`，可依數值平衡調整）。
- `weight` (int): 商店隨機出現權重（預設為 `1`）。

### (6) `Sword` (腳本 - 實為通用近戰武器驅動 Melee / Weapon)
GUID: `969d645455862074c8087ba7d22a8c8b`
專案核心戰鬥武器組件。雖然類別名稱為 `Sword`，但它繼承自 `Melee` 與 `Weapon`，當前專案內的所有近戰武器（劍、斧、太刀、巨錘等）均直接掛載此腳本以驅動攻擊連段。

#### 核心序列化屬性配置清單：
| 欄位分類 | 變數名稱 | 類型 | 說明 / 預設值 |
| :--- | :--- | :--- | :--- |
| **基礎設定** | `weaponName` | `string` | 武器名稱（如 `"劍"`、`"斧頭"`） |
| | `itemIcon` | `Sprite` | UI 武器插槽圖標 (Sprite) |
| | `damage` | `float` | 武器附加基礎傷害（平移修正至 CharacterStats） |
| | `weaponType` | `int` | 武器類型：`0` = 單手劍, `1` = 太刀, `2` = 重型/斧頭 |
| | `combo` | `int` | 初始為 `0` |
| **移動速度** | `runSpeed` | `float` | 裝備時玩家奔跑速度（劍: 7, 斧: 6, 太刀: 10, 重型: 4~5） |
| | `walkSpeed` | `float` | 裝備時玩家行走速度（一般為 3, 太刀: 4, 巨型: 2） |
| **攻擊檢測** | `attackPoint` | `Transform` | **必填**，拖入子物件 `AttackPoint` |
| | `attackRadius` | `float` | 攻擊判定球半徑（單手: 0.55, 斧: 0.5, 太刀: 0.6, 重型: 0.7~0.75） |
| | `weaponSoundType` | `enum` | 揮擊音效類型（`1` = Sword, `2` = GSword） |
| | `overrideController`| `AnimatorOverrideController` | 角色攻擊動畫覆寫控制器（依武器類型指定） |
| **打擊反饋與特效**| `weaponTrails` | `TrailRenderer[]` | 基礎架構填空陣列 `[]` |
| | `weaponEffect` | `ParticleSystem` | 基礎架構填 `null` |
| | `hitImpactPrefab`| `GameObject` | 基礎打擊反饋特效（可留空 `null`） |
| | `bladeStartRef` | `Transform` | 劍顎/起點參考點（基礎架構可為 `null`） |
| | `bladeTipRef` | `Transform` | 劍尖/終點參考點（基礎架構可為 `null`） |
| | `gemPointLight` | `Light` | 寶石點光源（基礎架構為 `null`） |
| | `hitStopDuration`| `float` | 命中頓幀時長（預設 `0.045` 秒） |
| **連段設定** | `totalComboSteps` | `int` | 最大連續攻擊段數（單手劍: 5, 太刀: 4, 斧頭/重武器: 3） |
| | `comboResetTime` | `float` | 連段逾時重置秒數（通常為 `2.0` 秒） |

---

## 4. 武器類型 (WeaponType) 與動畫對照表

專案內的角色動作依賴 `weaponType` 與其對應的 `overrideController` 資源：

| 武器種類 (類別) | `weaponType` | `overrideController` 資源路徑與 GUID | `totalComboSteps` | `weaponSoundType` | 典型參考 Prefab |
| :--- | :---: | :--- | :---: | :---: | :--- |
| **單手劍 (OneHand)** | `0` | `Assets/Animator/Player/OneHandOverride.overrideController`<br>`guid: b3cf2998262d882478fe843ccd26d35f` | 5 段 | `1` (Sword) | `Sword.prefab`<br>`Daybreak.prefab`<br>`RoyalSword.prefab` |
| **太刀 (Katana)** | `1` | `Assets/Animator/Player/OneHandOverride.overrideController`<br>`guid: b3cf2998262d882478fe843ccd26d35f` | 4 段 | `1` (Sword) | `Katana.prefab` |
| **重武器 / 斧頭 (TwoHand)** | `2` | `Assets/Animator/Player/TwoHandOverride.overrideController`<br>`guid: 28f21ae37ff98ec45bd27857a3025c47` | 3 段 | `2` (GSword) | `Axe.prefab`<br>`Crag.prefab`<br>`TitansTooth.prefab` |

---

## 5. 基準預製件詳細對比 (`Sword.prefab` vs `Axe.prefab`)

| 配置項目 | `Sword.prefab` (單手劍基準) | `Axe.prefab` (斧頭/重型基準) |
| :--- | :--- | :--- |
| **Root 名稱** | `Sword` | `Axe` |
| **Layer** | 12 (`Weapon`) | 12 (`Weapon`) |
| **BoxCollider Center** | `(-0.407, -0.002, -0.002)` | `(-0.057, -0.082, 0.028)` |
| **BoxCollider Size** | `(1.126, 0.249, 0.128)` | `(1.372, 0.515, 0.383)` |
| **DropsInteract 名稱** | `"劍"` | `"斧頭"` |
| **武器傷害 (damage)** | `10` | `20` |
| **武器類型 (weaponType)**| `0` | `2` |
| **走跑速度 (run / walk)**| `7` / `3` | `6` / `3` |
| **AttackPoint 位置** | `(-0.415, 0, 0)` | `(-0.558, -0.061, 0)` |
| **攻擊半徑 (attackRadius)** | `0.55` | `0.5` |
| **揮擊音效 (weaponSoundType)**| `1` (Sword) | `2` (GSword) |
| **連段段數 (totalComboSteps)**| `5` | `3` |
| **連段重置時間 (comboResetTime)**| `2` | `2` |
| **子模型 (Model)** | `Rebellion_Sword` (FBX) | `Axe_2` (Prefab) |

---

## 6. 運行時戰鬥判定機制 (Combat & Hitbox Mechanics)

了解戰鬥機制的運作原理，能幫助精準放置 `AttackPoint`：
1. **動作觸發**：當玩家點擊普攻時，`PlayerCombatSystem` 呼叫 `currentWeapon.Attack(animator)`，依據當前 `combo` 數值設置 Animator 參數 (`anim.SetInteger("Combo", combo); anim.SetTrigger("LAtk");`)。
2. **動畫事件 (Animation Event)**：動作動畫播到揮擊幀時，動畫事件觸發 `CharacterCombatBase.OnAttackHitboxStart(AttackData attackData)`。
3. **球體射線檢測**：在 `CheckAttackHitbox()` 函式中，系統以 `hitboxes[0].center`（即指向武器的 `attackPoint.position`）為球心、`hitboxes[0].radius`（即 `attackRadius`）為半徑，調用：
   ```csharp
   Physics.OverlapSphereNonAlloc(hitboxes[index].center.position, hitboxes[index].radius, attackHits, whatIsEnemy);
   ```
4. **判定原則**：
   - 武器上的 `BoxCollider` **不參與**傷害碰撞，純屬世界物理掉落著陸盒。
   - 真正決定打擊涵蓋範圍的是 **`AttackPoint` 的位置 + `attackRadius` 的半徑**。

---

## 7. 批量製作武器 Prefab 執行 SOP (Batch Production Plan)

當使用者提供一組模型（FBX / Prefab）時，依下列標準化步驟進行批次生成：

### 階段一：模型前置處理與資訊收集
1. 確認模型檔案路徑（如 `Assets/Res/Model/...`）。
2. 定義武器基本資料：
   - 武器名稱（如「雙刃斧」、「晨星」等）
   - 武器類別（`0: 單手劍`, `1: 太刀`, `2: 雙手/重型斧`）
   - 基礎傷害值（`damage`）與移動速度數值（`runSpeed`, `walkSpeed`）
   - 對應的 UI 圖標 Sprite（若無則先置空）

### 階段二：Prefab 生成標準作業
1. **建立根物件**：
   - 命名為該武器英文名（如 `BattleAxe`）。
   - 設定 `Layer = 12` (`Weapon`)。
   - 掛載 `Rigidbody`（Mass=1, Gravity=True, Kinematic=False）。
   - 掛載 `BoxCollider`（IsTrigger=False）。
   - 掛載 `DropsInteract`（填入繁中名稱）。
   - 掛載 `ShopWeapon`（預設值）。
   - 掛載 `Sword` 腳本（配置對應數值與 Controller）。
2. **掛載與校正模型 (Model Alignment)**：
   - 將模型 FBX 實例化為 Root 的子物件，Layer 設為 `12`。
   - 調整模型的 Local Position 與 Rotation，**將握柄中心完全對齊 Root 的 (0, 0, 0)**，並讓刀刃朝向 **`-X`** 軸。
3. **建立判定點 (AttackPoint)**：
   - 建立子物件命名為 `AttackPoint`，Layer 設為 `12`。
   - 依據刀身長度，將其位置設置在刀刃或斧頭重心處（通常在 `x = -0.4 ~ -0.9` 區間）。
   - 將該 Transform 賦值給 `Sword` 組件的 `attackPoint` 欄位。
4. **自適應 BoxCollider**：
   - 計算模型 Mesh 的 Bounds，調整 Root 上的 `BoxCollider` 的 Center 與 Size，使其精準包覆整個武器外型。
5. **儲存 Prefab**：
   - 保存至 `Assets/Prefab/Weapon/[武器名稱].prefab`。

---

## 8. 自動化 Editor 批次生成工具規劃 (Batch Weapon Tool)

為了後續能高效、無誤差地批次製作數十把武器 Prefab，可設計一個 Unity Editor 工具（例如 `BatchWeaponPrefabGenerator.cs`）：
- **功能特點**：
  1. 拖入 FBX 模型列表或資料夾。
  2. 選擇武器模板類型（單手劍 / 太刀 / 雙手巨武器）。
  3. 自動建立 Root GameObject 並設置 Layer 12。
  4. 自動掛載 `Rigidbody`, `BoxCollider`, `DropsInteract`, `ShopWeapon`, `Sword`。
  5. 自動從模型 MeshRenderer / MeshFilter 計算 Bounds，一鍵產生貼合的 `BoxCollider` 與 `AttackPoint`。
  6. 自動綁定 `OneHandOverride` 或 `TwoHandOverride` Controller。
  7. 一鍵生成並儲存至 `Assets/Prefab/Weapon/`。

---

## 9. 檢查清單 (Validation Checklist)

批量生成後，每把武器需通過以下檢驗：
- [ ] Layer 是否全部為 `12` (`Weapon`)（包含 Root、Model 與 AttackPoint）？
- [ ] Root 原點 `(0, 0, 0)` 是否剛好落在握柄處？
- [ ] 刀刃或打擊頭部是否朝向 `-X` 軸？
- [ ] `AttackPoint` 是否正確被指派給 `Sword.attackPoint`？
- [ ] `BoxCollider` 是否完全包裹武器模型且 `isTrigger == false`？
- [ ] `Rigidbody` 是否 `isKinematic == false` 且 `useGravity == true`？
- [ ] `overrideController` 是否依武器類型（單手 vs 雙手）正確賦值？
- [ ] `DropsInteract.interactableName` 與 `Sword.weaponName` 是否正確填寫？
