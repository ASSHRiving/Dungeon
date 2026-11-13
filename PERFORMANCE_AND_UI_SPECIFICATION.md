# 遊戲效能診斷與 UI/相機優化架構規格書
**專案名稱**：Dungeon (ARPG 地牢冒險)  
**引擎版本**：Unity 6 (6000.3.9f1) URP  
**文件版本**：1.0.0  
**編寫目的**：記錄「轉視角掉幀」的深度排查結論、uGUI Canvas 動靜分離重構方案與落地技術規格，供後續開發與維護團隊交接執行。

---

## 目錄
1. [問題概述與診斷摘要](#1-問題概述與診斷摘要)
2. [第一部分：轉視角掉幀（Camera Stutter & Frame Drop）根本原因與修復規格](#2-第一部分轉視角掉幀camera-stutter--frame-drop根本原因與修復規格)
   - [1.1 相機雙避障組件衝突 (Decollider vs Deoccluder)](#11-相機雙避障組件衝突-decollider-vs-deoccluder)
   - [1.2 動態地牢無遮擋剔除 + 超遠裁剪面 (1000m)](#12-動態地牢無遮擋剔除--超遠裁剪面-1000m)
   - [1.3 相機射線碰撞未優化的非凸 MeshCollider](#13-相機射線碰撞未優化的非凸-meshcollider)
   - [1.4 相機輸入阻尼過重與手感粘滯](#14-相機輸入阻尼過重與手感粘滯)
3. [第二部分：uGUI Canvas 動靜分離與架構規格](#3-第二部分ugui-canvas-動靜分離與架構規格)
   - [2.1 動態 UI 與靜態 UI 判定準則](#21-動態-ui-與靜態-ui-判定準則)
   - [2.2 GameScene 現有 Canvas 問題清單](#22-gamescene-現有-canvas-問題清單)
   - [2.3 目標 UI 分層架構規格 (Sub-Canvas 設計)](#23-目標-ui-分層架構規格-sub-canvas-設計)
4. [第三部分：URP 渲染管線設定調優](#4-第三部分urp-渲染管線設定調優)
5. [第四部分：實施檢核清單與驗收標準](#5-第四部分實施檢核清單與驗收標準)

---

## 1. 問題概述與診斷摘要

### 1.1 核心痛點
在遊戲執行過程中，平時幀率相對平穩，但在**旋轉視角（Rotate Camera）時會發生極為嚴重的突發性掉幀（Frame Drop）與視角頓挫（Micro-stutter）**。

### 1.2 診斷結論
該現象**並非單一模組負載過重**，而是多個模組在相機旋轉時觸發連鎖反應所致：
1. **相機避障衝突**：`PlayerTPS.prefab` 同時掛載了互相排斥的 `CinemachineDecollider` 與 `CinemachineDeoccluder`，造成每幀相機座標震盪。
2. **渲染幾何暴增**：程序化地牢（`Generator.cs`）無法享受 Unity 靜態遮擋剔除，加之相機遠裁剪面達 1000 米，轉向地牢中心時前方所有房間一次性送入渲染管線。
3. **物理射線開銷**：相機避障射線撞擊場景中 71+ 個未勾選 Convex 的高面數 `MeshCollider`。
4. **UI 全域重繪**：小地圖（`MinimapPanel`）在轉視角時每幀旋轉父容器，且整個場景僅有 1 個 Root Canvas，導致全遊戲所有 UI 每一幀被強制全量重建（Canvas Rebuild）。

---

## 2. 第一部分：轉視角掉幀根本原因與修復規格

### 1.1 相機雙避障組件衝突 (Decollider vs Deoccluder)
* **相關資產**：`Assets/Prefab/Camera/PlayerTPS.prefab`
* **組件識別**：
  * `CinemachineDecollider` (ID: 4179057956181714890)
  * `CinemachineDeoccluder` (ID: 4718252813466589969, `MaximumEffort: 4`)
* **根因分析**：
  Cinemachine 3.x 官方規範中，`Decollider`（防相機本體穿牆推擠）與 `Deoccluder`（防目標遮擋拉近）屬於同質避障方案。同時掛載時，轉動視角一旦擦碰幾何體邊緣，兩者在同一幀對相機進行相反方向的推拉修正，造成座標在極短時間內（0.2s damping）激烈拉扯，產生嚴重的抖動與卡頓感。
* **修復規格**：
  * **操作**：移除或禁用 `CinemachineDecollider` 組件，**僅保留 `CinemachineDeoccluder`**。
  * **推薦參數設定**：
    * `CameraRadius`: `0.15` ~ `0.2`
    * `MaximumEffort`: `2`（原為 4，可大幅減少分支射線數量）
    * `Damping`: `0.1` ~ `0.15`（原為 0.2，降低拉扯延遲）
    * `DampingWhenOccluded`: `0`（被遮擋時瞬間拉近，消除穿模）

### 1.2 動態地牢無遮擋剔除 + 超遠裁剪面 (1000m)
* **相關資產/代碼**：`PlayerTPS.prefab` (Lens.FarClipPlane)、`Assets/Script/GameAssets/Generator.cs`
* **根因分析**：
  1. Unity 內建 Occlusion Culling 僅能烘焙靜態場景（Baked Static），對運行時動態生成的多個房間無效。
  2. 相機遠裁剪面預設高達 **1000 米**。
  3. 玩家轉向地牢內部時，面前牆壁無法阻擋背後幾十米外的多個房間，所有房間內的柱子、地面、裝飾物與未交戰怪物全部落入視錐體，Draw Call 與頂點數瞬間成倍暴增。
* **修復規格**：
  * **操作 1**：將 `PlayerTPS.prefab` 的 `FarClipPlane` 從 `1000` 降至 **`50` ~ `60`**（視房間最遠對角線微調），並在 URP Volume 中配合輕微距離霧（Distance Fog）。
  * **操作 2**：在 `Assets/Settings/PC_RPAsset.asset` 中，將 `GPUResidentDrawerMode` 設置為 `Instanced Drawing`（Unity 6 新增特性，大幅降低動態物件 Draw Call）。

### 1.3 相機射線碰撞未優化的非凸 MeshCollider
* **相關資產**：`Assets/Prefab/Rooms/*.prefab`（`NormalRoom01`, `NormalRoom03`, `ShopRoom` 等）
* **根因分析**：
  房間 Prefab 內有超過 71 個 Props、Columns 等幾何體掛載了非凸（`m_Convex: 0`）的 `MeshCollider`。相機避障的 ObstacleLayers 包含了 `Layer 0 (Default)`，轉視角時避障射線密集掃過這些高面數幾何網格，造成 CPU PhysX 主執行緒出現計算尖峰。
* **修復規格**：
  * **操作 1**：在專案中新增專用圖層 `CameraCollision`。
  * **操作 2**：地牢房間中僅外圍主牆壁與大型立柱使用簡易 `BoxCollider` 並設為 `CameraCollision` 圖層；細碎裝飾物（如碎石、瓶罐）不參與相機碰撞。
  * **操作 3**：將 `CinemachineDeoccluder` 的 `CollideAgainst` 僅勾選 `CameraCollision`（或僅保留主結構層）。

### 1.4 相機輸入阻尼過重與手感粘滯
* **相關資產**：`PlayerTPS.prefab`
* **根因分析**：
  * `CinemachineOrbitalFollow`: `RotationDamping: (1, 1, 1)`（1 秒高延遲阻尼）
  * `CinemachineInputAxisController`: `AccelTime: 0.2`, `DecelTime: 0.2`
  過長的阻尼與加減速疊加，造成滑鼠停止移動後鏡頭持續滑行，在體感上產生強烈的「輸入卡頓 / 拖曳感」。
* **修復規格**：
  * `RotationDamping`: 修改為 `(0.1, 0.1, 0.1)`（或直接設為 0）
  * `AccelTime` / `DecelTime`: 修改為 `0.02` ~ `0.05`，使滑鼠操作即時跟手。

---

## 3. 第二部分：uGUI Canvas 動靜分離與架構規格

### 2.1 動態 UI 與靜態 UI 判定準則

> **核心原理**：uGUI 的合批單位是 **Canvas**。只要同一個 Canvas 下有任何 1 個元素發生幾何變更（Position/Rotation/Scale）或頂點重繪（Text/FillAmount/Size），**整面 Canvas 的所有元素必須在 CPU 上全量重建頂點並重新合批**（`Canvas.BuildBatch`）。

#### 判斷標準：
1. **動態 UI（高頻，每幀/持續變更）➔ 必須隔離出獨立 Sub-Canvas**：
   - 小地圖（轉視角時每幀旋轉 `Rotation`、位移 `AnchoredPosition`）
   - 懸浮提示框（滑鼠移動時每幀更新座標 `Position`）
   - 平滑扣血條/微震動血條（連砍期間每幀插值 `FillAmount`、`DOShake`）
   - 傷害飄字（每幀位移、Billboard 對齊鏡頭、彈跳縮放）
2. **靜態 UI / 事件型動態 ➔ 保留在主 Canvas 享受合批**：
   - 底框、邊框、技能圖標、裝飾性圖片
   - 偶爾變化的數值（如撿金幣時加一次、按一下按鈕換色、切換裝備）
   - *註：事件型動態動一次只需重建一幀，耗時在 0.05ms 以內，留在主 Canvas 合批反而省 Draw Call。*

---

### 2.2 GameScene 現有 Canvas 問題清單

| 編號 | 面板名稱 | 涉及腳本 | 現狀問題 | 改善處方 |
| :---: | :--- | :--- | :--- | :--- |
| **1** | **MinimapPanel** | `MinimapController.cs` | 預設 `RotateMap` 模式，轉視角時每幀 Slerp 旋轉地圖容器，**全場景最大 UI 掉幀源**。 | 根節點加上 `Canvas` 組件獨立隔離。 |
| **2** | **TooltipUI** | `TooltipController.cs` | 顯示期間每幀 `Update` 根據滑鼠更新座標，造成背後裝備欄全量重繪。 | 根節點加上 `Canvas`（Order=100）。 |
| **3** | **BossHealthBar** | `BossHealthUIController.cs` | 扣血時 0.45s 殘影追趕，大傷害時 0.18s 內以 20 次頻率劇烈抖動外框座標。 | 根節點加上 `Canvas` 獨立隔離。 |
| **4** | **彈窗群** (Esc, Setting, Tutorial, Dialog, Lose) | `UIPanelBase.cs` | 開關使用 `SetActive`，且伴隨 0.25s `DOScale` 縮放動畫，開關時造成主 HUD 掉幀。 | 集中於 `Popups` 空父節點，父節點掛載 `Canvas`。 |
| **5** | **全域 RaycastTarget** | 全場景 134 處 | 裝飾背景、圖示、純文字皆開啟點擊檢測，每次點擊滑鼠皆遍歷 134 個物件。 | 關閉非互動元件的 RaycastTarget。 |
| **6** | **CanvasScaler** | `CanvasScaler` | 設為 `Constant Pixel Size`，高低解析度螢幕會縮小或變形。 | 改為 `Scale With Screen Size` (1920x1080)。 |
| **7** | **InteractPanel** | `InteractController.cs` | 靠近/離開寶箱時頻繁調用 `SetActive` 觸發重組。 | 改用 `CanvasGroup.alpha` 漸隱代替開關。 |

---

### 2.3 目標 UI 分層架構規格 (Sub-Canvas 設計)

重構後應採用的標準階層結構（**僅需在對應節點掛載 `Canvas` 組件，自動成為 Sub-Canvas，無需多個 Root Canvas**）：

```text
Canvas (Root 主畫布 - Screen Space Overlay, 帶 CanvasScaler)
│   [設定：Scale With Screen Size, 1920x1080, Match=0.5]
│
├── GoldPanel               (金幣/關卡資訊 - 事件觸發，常駐主層)
├── PlayerStats             (角色數值與裝備欄 - 事件觸發，常駐主層)
├── CButton                 (快捷提示按鈕 - 常駐主層)
├── InteractPanel           (交互提示 - 建議掛 CanvasGroup 控制透明度)
│
├── [Sub-Canvas] MinimapPanel
│   ├── Component: Canvas (Override Sorting: false)
│   └── 說明: 隔絕 mapContainer 每幀旋轉，徹底杜絕轉視角 UI 尖峰
│
├── [Sub-Canvas] BossHealthBar
│   ├── Component: Canvas (Override Sorting: false)
│   └── 說明: 隔絕殘影條 DOFillAmount 與 DOShake 微震動重繪
│
├── [Sub-Canvas] Popups (新建空物件集中管理)
│   ├── Component: Canvas (Override Sorting: true, Order in Layer: 20)
│   ├── EscPanel
│   ├── SettingPanel
│   ├── TutorialPanel
│   ├── ConfirmDialogPanel
│   └── LosePanel
│   └── 說明: 隔絕彈窗 SetActive 與 0.25s 入場縮放動畫 (DOScale)
│
└── [Sub-Canvas] TooltipUI
    ├── Component: Canvas (Override Sorting: true, Order in Layer: 100)
    └── 說明: 隔絕滑鼠跟隨每幀位移，並確保懸浮提示永遠在最頂層
```

---

## 4. 第三部分：URP 渲染管線設定調優

* **相關檔案**：
  * `Assets/Settings/PC_RPAsset.asset`
  * `Assets/Settings/PC_Renderer.asset`

| 項目 | 原始參數 | 推薦目標參數 | 優化收益 |
| :--- | :--- | :--- | :--- |
| **Additional Lights Shadowmap** | `4096` (4K) | **`1024` 或 `2048`** | 大幅減少進入新房間/視角旋轉時點光源陰影貼圖切換的 GPU 與顯存開銷。 |
| **SSAO Downsample** | `0` (全解析度) | **`1` (半解析度)** | Deferred 管線下視角大幅轉動時，降低環境遮擋採樣的像素填充率負載，視覺幾乎無損。 |
| **Shadow Cascades** | `4 Cascades` | **`2 Cascades`** | 地下城室內場景視距短，2 級串聯陰影已足夠，可節省陰影繪製批次。 |
| **GPU Resident Drawer Mode** | `Disabled (0)` | **`Instanced Drawing (1)`** | 利用 Unity 6 的 GPU Driven 特性自動合批動態生成的房間幾何體。 |

---

## 5. 第四部分：實施檢核清單與驗收標準

### 5.1 實施步驟清單 (Checklist)

- [ ] **Phase 1: 相機與手感修復**
  - [ ] 在 `PlayerTPS.prefab` 移除 `CinemachineDecollider` 組件。
  - [ ] `CinemachineDeoccluder` 的 `MaximumEffort` 調為 `2`，`Damping` 調為 `0.1`。
  - [ ] 將 `FarClipPlane` 從 `1000` 降至 `50~60`。
  - [ ] 相機旋轉阻尼 `RotationDamping` 改為 `(0.1, 0.1, 0.1)`。
- [ ] **Phase 2: UI Sub-Canvas 隔離**
  - [ ] 在 `MinimapPanel` 增加 `Canvas` 組件。
  - [ ] 在 `BossHealthBar` 增加 `Canvas` 組件。
  - [ ] 在 `TooltipUI` 增加 `Canvas` 組件（Override Sorting 勾選，Order 設為 100）。
  - [ ] 建立 `Popups` 物件集中彈窗，掛載 `Canvas` 組件（Order 設為 20）。
  - [ ] 執行清理：將 Background、Icon、純文字的 `Raycast Target` 批次取消勾選。
  - [ ] 將 Root `CanvasScaler` 改為 `Scale With Screen Size` (1920x1080, Match=0.5)。
- [ ] **Phase 3: 渲染管線參數**
  - [ ] `PC_RPAsset` 附加光源陰影解析度降為 1024/2048。
  - [ ] `PC_Renderer` 中的 SSAO 啟用 Downsample = 1。

### 5.2 驗收測試標準
1. **視角旋轉測試**：在擁有多個相鄰房間的地牢中心快速 360 度轉動相機，觀察畫面應保持平滑流暢，無肉眼可見頓挫（Micro-stutter 消除）。
2. **Unity Profiler 數據指標**：
   * **`Canvas.BuildBatch` / `Canvas.SendWillRenderCanvases`**：在轉視角且無開關彈窗時，CPU 耗時應降至 **< 0.1ms**。
   * **`Physics.SphereCast`**：相機靠近牆體旋轉時，PhysX 耗時無突發尖峰。
   * **Draw Calls / Batches**：面向地牢深處時，Draw Call 數量應大幅收斂，不再暴增數倍。
