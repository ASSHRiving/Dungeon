---
name: dungeon-threejs-asset-forge
description: 在 Dungeon Unity 專案中生成或製作任何 3D 武器、地牢機關、寶箱、藥水或環境道具時優先使用。繼承全域 threejs-model-to-unity-pipeline 的 .glb 匯出與姿勢歸零機制，並將幾何著色、256x256 低噪點像素貼圖、面數預算、武器握把與 -X 軸向、四大發光主題色盤、glTFast 匯入及 Unity Layer/Prefab 裝配規範鎖定為 Dungeon 專案標準。
---

# Dungeon Three.js 3D Asset Forge

本 Skill 為 **Dungeon** Unity URP 專案專屬的 Three.js 程式化 3D 建模與資產匯出規範，擴充並特化全域 `threejs-model-to-unity-pipeline`（`.glb` 雙向預覽與匯出管線），確保任何透過瀏覽器 Three.js 鍛造器產出的武器、藥水、寶箱、機關與環境道具能無縫融入現有美術風格與 Prefab 系統。

---

## 1. 繼承自全域管線的基礎工程規範

在實作任何 `Tools/ModelForge/<asset_name>_forge.html` 頁面時，必須沿用 `threejs-model-to-unity-pipeline` 的核心骨架：
1. **乾淨的匯出隔離根節點（`assetGroup`）**：
   - 所有需匯入 Unity 的幾何體一律掛在 `assetGroup` 下，座標單位嚴格遵守 **`1 Three.js unit = 1 Unity meter`**。
   - 地板格線、安全距離線框、參考人物膠囊體、`AttackPoint` 指示球、燈光與相機控制皆放在 `previewHelpersGroup`，絕不可併入 `assetGroup`。
2. **Pre-Export Pose Reset Hook（匯出前姿勢歸零鉤子）**：
   - `GLTFExporter` / `OBJExporter` 會直接序列化當下場景節點的 `position` / `rotation` / `scale`。
   - 匯出函式執行前**必須**：暫存預覽狀態（如待命旋轉、懸浮呼吸、開蓋/揮砍展示角度）→ 將 `assetGroup` 與所有 `Part_*` 可動子節點歸零至預設靜止姿勢 → 執行 `GLTFExporter({ binary: true })` → 恢復預覽動畫。
3. **色彩空間與光源控制**：
   - Albedo（`map`）與 Emission（`emissiveMap`）使用 `THREE.SRGBColorSpace`；Roughness / Metalness / Normal 等資料貼圖使用 `THREE.NoColorSpace`。
   - 預覽場景使用 `PMREMGenerator` + `RoomEnvironment`（適度降強度模擬地牢幽暗環境光）與 `DirectionalLight` + 至多 **1 顆**動態 `PointLight` 烘托發光紋路。
4. **`mergeGeometries` 屬性對齊與防呆**：
   - 合併同材質靜態零件前，先統一 `.toNonIndexed()`、確保 `position` / `normal` / `uv` 屬性完全一致（或統一先剔除/重建），避免 `BufferGeometryUtils.mergeGeometries` 回傳 `null`；單獨零件縮放禁止出現 `0`。
5. **本地網頁執行方式**：
   - HTML 鍛造器統一放置於 `Tools/ModelForge/<asset_name>_forge.html`。
   - 由於 ES Module CDN / 匯入與貼圖匯出限制，開啟預覽時透過本機 HTTP Server（如 `python -m http.server`）提供服務並開啟瀏覽器測試匯出按鈕。

---

## 2. Dungeon 專案美術風格與面數預算

### 2.1 幾何與著色規範：硬邊平直著色（Flat-Shaded）
專案既有資產（`DungeonPackLite`、`LowPolyDungeonsLite`、`FREE Low Poly Weapons Pack`、`Table`、`potion`、`RoyalSword`）皆採用**硬邊塊面低多邊形（Faceted Low-Poly）**：
- **強制硬邊法線**：幾何體建立後執行 `geom = geom.toNonIndexed()` 並呼叫 `geom.computeVertexNormals()`，搭配材質 `flatShading: true`，確保匯出 `.glb` 後保留類似 FBX `ByPolygonVertex` 的俐落硬邊切面，不進行軟邊平滑（Smooth Shading）。
- **結構感來源**：依賴倒角切邊（Chamfer / Bevel segments = 1）、階梯式塊面堆疊、金屬鉚釘/箍環與木板/石磚分塊造型建立剪影辨識度，而非高細分曲面。
- **頂點色（`COLOR_0`）策略**：專案既有模型幾乎無頂點色（`has_vcol = False`），因此 **預設關閉** 頂點 AO 烘焙以保持與現有材質管線一致；可在網頁 UI 提供選配開關「微調縫隙 AO（Vertex AO）」供展示檢視。

### 2.2 嚴格鎖定的三角面數（Triangles）預算
網頁預覽右上角 HUD 必須即時計算並顯示 `assetGroup` 總三角面數與目標區間燈號（綠燈達標 / 黃燈偏高 / 紅燈超標）：

| 資產分類 | 目標面數區間 | 硬上限 / 備註 |
| :--- | :---: | :--- |
| **小型道具**（藥水、鑰匙、金幣、火把、可破壞瓶罐） | `300 ~ 1,200 tris` | 可破壞物建議額外規劃 `Broken` 碎片變體 |
| **武器**（單手劍、武士刀、戰斧、大劍、法杖） | `800 ~ 2,200 tris` | 含齒輪/鍊條/浮空結晶等可動機構上限 `3,500 tris` |
| **大型機關與建築道具**（寶箱、機關門、陷阱、貨架桌、傳送門） | `500 ~ 2,500 tris` | 需確保碰撞體可簡化為 Box / Capsule |

*同時在 UI 保留「Game-Ready（預設，嚴格遵守上表預算）」與「Showcase（高細分展示）」切換開關，確保預設匯出即為實戰規格。*

---

## 3. 材質分區、256×256 貼圖管線與四大發光主題

### 3.1 語意化 `Mat_*` 分區材質槽
嚴禁將整個模型合併為單一材質球，也不使用單一純色調色盤（雖然場景套裝有 Palette，但 Dungeon 近距離道具與武器已演化為語意材質分區）。每個部件依物理屬性分配清楚的英文材質名稱：
- `Mat_Dungeon_Stone` / `Mat_Dungeon_Brick`（石材、底座、符文碑）
- `Mat_Weathered_DungeonWood`（握柄、木桌、寶箱箱身、木桶）
- `Mat_Forged_DarkIron` / `Mat_Rusty_Iron`（暗黑鍛鐵、護手、箍環、陷阱利刃）
- `Mat_Blade_Steel` / `Mat_Blade_Edge`（武器開鋒刃口、亮銀金屬）
- `Mat_Ornate_Bronze` / `Mat_Gold_Trim`（青銅包邊、華麗金飾、鎖扣）
- `Mat_Leather_Wrap`（劍柄皮革纏繞、帶扣）
- `Mat_Potion_Glass`（半透明玻璃瓶身）
- `Mat_Emissive_Rune` / `Mat_Emissive_Gem`（符文、寶石、藥水液體、傳送門核心）

> **命名防呆**：絕對避免在材質或節點名稱結尾留下 `.001`、`.002` 等 Blender 重複命名後綴。

### 3.2 256×256 風格化低噪點像素貼圖（預設不烘法線貼圖）
- **紋理生成**：使用 `<canvas>` 即時繪製 **`256×256`**（精細部件至多 `512×512`）低噪點風格化紋理：
  - 石材：深灰藍塊磚底色 + 暗縫 + 微量青苔/礦物像素點綴（對齊 `Mat_Dungeon_Stone` / `Image_0.png`）。
  - 木材：深胡桃木直紋條帶 + 暗色板縫（對齊 `Mat_Weathered_DungeonWood` / `Image_1.png`）。
  - 鍛鐵/鏽鐵：炭黑金屬底色 + 邊緣微高光與極少量鐵鏽色塊（對齊 `Mat_Forged_DarkIron` / `Image_2.png`）。
  - 軟木塞/皮革：暖褐細微像素顆粒（對齊 `Image_0.001.png`）。
- **法線貼圖預設關閉**：專案主流模型無高解析寫實凹凸貼圖，因此 **預設不生成、不烘焙 Sobel 法線貼圖（`normalMap = null`）**，避免產生與週邊低多邊形地牢格格不入的油膩塑膠感。
- **選配開關**：僅在網頁 UI 提供**預設關閉**的 `Hero 武器法線貼圖（512×512 Sobel Normal）` 核取方塊，供極少數特寫級 Boss 武器手動開啟測試。

### 3.3 鎖定 PBR 數值基準（Three.js `MeshStandardMaterial` ↔ Unity URP Lit）
轉換關係：`Unity Smoothness = 1.0 - Three.js roughness`。

| 材質類型 | Base Color 參考 | `metalness` | `roughness` (Unity Smoothness) | 備註 |
| :--- | :--- | :---: | :---: | :--- |
| **地牢石材** | `#2B2E37` ~ `#3A3F4B` | `0.0` | `0.90` (`0.10`) | 啞光粗石、微苔蘚點綴 |
| **風化深木** | `#3B281E` ~ `#4A3224` | `0.0` | `0.88` (`0.12`) | 暗調沉穩木色 |
| **暗鍛鐵 / 鏽鐵** | `#2C2623` ~ `#3F3D42` | `0.20 ~ 0.60` | `0.50 ~ 0.58` (`0.42 ~ 0.50`) | 低反光舊化深鐵，部分結構石鐵可設 `0.0` |
| **開鋒鋼刃** | `#9BA3AE` ~ `#C8D0D8` | `0.80 ~ 0.90` | `0.28 ~ 0.35` (`0.65 ~ 0.72`) | 刃緣可稍微提高亮度以凸顯輪廓 |
| **青銅 / 金飾** | Bronze `#B48A48` / Gold `#FFE758` | `0.75 ~ 0.85` | `0.32 ~ 0.45` (`0.55 ~ 0.68`) | 用於皇家武器、寶箱鎖扣與邊框 |
| **藥水玻璃** | `#DBEAFE` (`opacity: 0.30`) | `0.0` | `0.20` (`0.80`) | `transparent: true`, `depthWrite: false` |

### 3.4 內建四大發光主題色盤（Web UI 單鍵切換）
所有帶發光符文、寶石、刀刃溝槽或魔力核心的資產，必須在網頁控制面板提供以下 4 組符合世界觀的 HDR 發光主題切換（`emissiveIntensity` 建議落於 `1.5 ~ 3.0`，配合預覽 UnrealBloom / Unity HDR）：

1. **餘燼橘紅（Ember — 地牢預設）**
   - 主發光：`#FF4500` / HDR 橘紅 `#FF1E00`（對應 `Mat_Ember_Runes`、`LowPolyDungeonsLite_Light`、`Bullet`）
   - 血紅/魔藥變體：Base `#DC2626` + HDR `(3.0, 0.017, 0.048)`（對應 `Mat_Health_Liquid`）
   - 搭配材質：暗鍛鐵 `#2C2623` + 風化深木 + 炭灰石材
2. **聖光青藍＋燦金（Holy — RoyalSword 主題）**
   - 主發光：青藍 `#00E5FF` / HDR `(0, 1.45, 1.84)`（對應 `Mat_HolyGem2`）
   - 輔色：燦金 `#FFD700` 與亮銀鋼刃（對應 `RoyalSwordTextureGenerator`）
3. **岩脈金珀（Crag — 地脈重武主題）**
   - 主發光：熔金琥珀 `#D4AF37` ~ `#FFB703`（對應 `CragTextureGenerator`）
   - 搭配材質：玄武深灰黑岩 + 粗礪鍛鐵
4. **虛空暗紫 / 幽綠（Void / Necro — 深淵變體）**
   - 虛空紫：Base `#9D4EDD` / HDR `(1.6, 0.35, 2.8)`
   - 幽魂綠：Base `#39FF88` / HDR `(0.4, 2.5, 1.1)`

---

## 4. 軸心（Pivot）、朝向與層級命名規範

### 4.1 武器強制規範（對齊 `Assets/Prefab/Weapon/Weapon_Architecture.md`）
任何劍、斧、匕首、長柄武器或雙手大劍，在匯出時**必須嚴格滿足**：
- **原點 `(0, 0, 0)`**：位於**劍柄／握把中心（Hand Grip Center）**，方便玩家手部 `WeaponSocket` 直接抓握，無需手動位移補償。
- **延伸方向**：刀刃 / 鎚頭 / 攻擊端一律沿 **`-X` 軸（負 X 軸）**延伸；劍尾配重球（Pommel）在 `+X` 側。
- **次要軸向**：刀背 / 護手寬度沿 `Y` 軸，刀身厚度沿 `Z` 軸。
- **預覽輔助（不匯出）**：在 `previewHelpersGroup` 繪製一個紅色線框球標示預估 `AttackPoint`（大約位於刃部前中段 `x ≈ -0.4 ~ -0.9`）與貼合武器長度的線框 `BoxCollider` 參考。

### 4.2 道具、寶箱與機關規範
- **接地道具與建築（藥水、寶箱、貨架桌、門框、火把架、可破壞木桶）**：
  - 原點 `(0, 0, 0)` 位於**底部接觸面中心（Bottom Center）**，所有幾何頂點 `y >= 0`。
  - `+Y` 為上方，正面朝向 `-Z`（或 `+Z`，於 UI 方向箭頭明確標示）。
- **可動機關與層級命名**：
  - 靜態零件：`Mesh_<PartName>`（如 `Mesh_ChestBody`、`Mesh_DoorFrame`、`Mesh_Handle`）。
  - 需由 Unity 腳本或 Animation 驅動旋轉/移動的子物件：`Part_<PartName>`（如 `Part_ChestLid`、`Part_DoorLeaf`、`Part_SawBlade`、`Part_LeverArm`），其局部原點必須精準建立在**鉸鏈軸線或旋轉圓心**上，讓 Unity 只需修改單一軸旋轉即可開門、掀蓋或轉動鋸齒。

---

## 5. Unity URP 匯入與 Prefab 裝配標準作業程序（SOP）

### 5.1 匯出檔案與放置路徑
從 Three.js 網頁按「匯出 `.glb`」後，建議放置於專案路徑：
`Assets/Res/Model/Three.js/<AssetName>/<AssetName>.glb`
若需微調貼圖或備查純色貼圖，同資料夾可存 `textures/*.png`。

### 5.2 標準匯入路徑（預設推薦：`com.unity.cloud.gltfast`）
本專案 `Packages/manifest.json` 預設尚未安裝 glTF 匯入器，**標準 SOP 為安裝 Unity 官方 `com.unity.cloud.gltfast`**：
1. 在 Unity `Window → Package Manager → + → Add package by name...` 輸入 `com.unity.cloud.gltfast`（或由 `Packages/manifest.json` 加入），讓 Unity 原生識別 `.glb` 為 Model Prefab。
2. 直接於 `1 unit = 1m` 比例拖入場景或用於組裝 Prefab（Transform `Scale = (1, 1, 1)`, `Rotation = (0, 0, 0)`）。
3. **材質客製化（URP Lit / Toon）**：因 `.glb` 預設使用 glTFast 內建 Shader，匯入後建議於 `Assets/Res/Model/Three.js/<AssetName>/Materials/` 建立或對應標準 `Universal Render Pipeline/Lit` `.mat` 替換（特別是需開啟 HDR Emission 與 Bloom 輝光的 `Mat_Emissive_*` 材質）。

### 5.3 備用路徑：Blender 轉 `.fbx` 防呆 SOP
僅當模型需要於 Blender 進行複雜骨架綁定或特殊網格編輯再轉 `.fbx` 時採用：
1. Blender 匯入 `.glb` 後，檢查所有物件位移、旋轉與縮放。
2. **必做防呆**：全選物件按 **`Ctrl + A` → `Apply → All Transforms`**，確保在進入 Unity 前沒有殘留縮放（避免重演舊版 `Table` 需在 Prefab 補償 `Scale = 20` 或 `potion` 需旋轉 `-90°` X 軸及 `0.02m` 碰撞體的錯誤）。
3. 清理 Blender 自動生成的 `.001` 材質與物件名稱後綴。
4. Export FBX 時勾選 `Apply Scalings: FBX Units Scale`、`!EXPERIMENTAL! Apply Transform`（或保持 `+Y Up, -Z Forward` 並讓 Unity `Scale Factor = 1`）。

### 5.4 Unity 腳本與 Layer 裝配對照
將匯入模型組裝為遊戲可用 Prefab 時，依類型對齊現有架構：

| 資產類型 | 目標 Layer（`TagManager.asset`） | 必要元件與裝配工具 |
| :--- | :---: | :--- |
| **武器** | **`Layer 12 (Weapon)`**<br>（Root、`Model`、`AttackPoint` 一致） | 優先使用專案自動化工具：**`Tools/Weapons/Batch Weapon Prefab Generator`**（`Assets/Editor/WeaponPrefabBuilder.cs`），自動掛載 `BoxCollider`（`isTrigger: false`）、`Rigidbody`（`Mass=1, UseGravity=true`）、`AttackPoint`、`DropsInteract`、`ShopWeapon` 與 `Sword`（`weaponType`: `0`=單手5段/`1`=武士刀4段/`2`=雙手3段）。詳見 `Assets/Prefab/Weapon/Weapon_Architecture.md`。 |
| **可互動機關與道具**<br>（寶箱、大門、藥水、商店桌、傳送門） | **`Layer 14 (Interactable)`** | 依類型掛載 `Chest`、`Door`、`Potion` / `ShopPotion`、`ShopTable`、`Portal` / `EndPortal` 及觸發用 `Collider`（視腳本需求設 `BoxCollider` 與 Interactable Trigger）。 |
| **可破壞物件**<br>（木桶、木箱、陶罐） | 本體：**`Layer 16 (Breakable)`**<br>碎片：**`Layer 15 (Debris)`** | 掛載 `BreakableProp.cs`（實作 `IDamageable`），並於 `brokenPrefab` 指定預設已設為 `Layer 15 (Debris)` 的破碎變體 Prefab。 |
| **純地形與牆柱** | 地板：**`Layer 6 (Ground)`** / **`Layer 11 (NavMeshGround)`**<br>牆面：**`Layer 17 (Wall)`** | 依實際碰撞需求加上簡化 `BoxCollider` / `MeshCollider`，並保留適當寬裕給角色行走與 NavMesh 烘焙。 |

---

## 6. 常見坑點防呆（Dungeon Gotchas）

1. **武器方向反了（刺向玩家自己）**：匯出時若將刀刃朝 `+X` 或 `+Z`，使用 `WeaponPrefabBuilder.cs` 計算 `bounds.min.x` 時 `AttackPoint` 會落到劍柄後方且揮砍朝內。開發期間務必開啟 `-X` 軸向箭頭輔助確認刀刃朝向 `-X`。
2. **glTFast 與發光材質失真**：`.glb` 內的自發光係數受 glTF `KHR_materials_emissive_strength` 影響；進入 Unity URP 後若覺得不夠亮，請改用可調 HDR Color 的 URP/Lit `.mat` 覆蓋材質槽，而非盲目在 Three.js 無限拉高貼圖亮度。
3. **非單一座標比例導致剛體彈飛**：武器的 `BoxCollider` 與 `Rigidbody` 必須掛在 `Scale = (1, 1, 1)` 的根物件上；絕不可依賴 Unity 節點 `Scale = 20` 或 `0.01` 來硬撐模型大小。
4. **動畫殘留狀態匯出**：寶箱蓋子半開、鋸齒盤正在旋轉或藥水正在上下浮動時直接按匯出，會把當下旋轉角寫死進 `.glb`。務必確保 Pre-Export Pose Reset Hook 將 `Part_*` 歸回 `0°` 閉合/靜止位置。
5. **非預期高面數與平滑法線**：Three.js `CylinderGeometry` / `SphereGeometry` 預設 radialSegments 偏高（32~64），在 Dungeon 風格中應主動降為 **6~12 邊（如六角、八角或十二角形）** 並呼叫 `.toNonIndexed()` + `flatShading: true`，才能與現有場景資產融為一體。
