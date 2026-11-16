# Dungeon UI 與 VFX 美術風格與工程規格指南

本文件從專案歷次開發的 Editor 生成器腳本中提煉並統一所有 **uGUI 介面設計**、**程式化 PNG 繪製**、**URP 武器刀光** 與 **技能／Boss 特效** 的視覺語言與技術鐵則，作為後續人工製作或 AI 協助擴充資產的標準規範。

> 相關輔助文件：
> - 3D 模型與 `.glb` 匯出規範：[dungeon-threejs-asset-forge](file:///c:/Unity/Dungeon/.agent/skills/dungeon-threejs-asset-forge/SKILL.md)
> - UI 效能與 Canvas 動靜分離：[PERFORMANCE_AND_UI_SPECIFICATION.md](file:///c:/Unity/Dungeon/PERFORMANCE_AND_UI_SPECIFICATION.md)
> - 武器架構：[Weapon_Prefab_Architecture.md](file:///c:/Unity/Dungeon/Weapon_Prefab_Architecture.md)
> - 單把武器 VFX 細節：`VFX_Spec_Crag.md`、`VFX_Spec_Katana.md`、`VFX_Spec_TitansTooth.md`

---

## 第一部分：UI 美術風格與組裝規範

### 1.1 視覺調性與色彩系統

整體 UI 採用 **暗黑奇幻金銅（Dark Fantasy Gold & Bronze）** 結合 **地牢深灰石板（Dungeon Slate）**，搭配微括號 `⌜ ⌝`、45° 斜切角（Chamfer）與金菱形 `◆` 刻度元素：

| 語意分類 | 主色 / 漸層參考 | 用途 |
| :--- | :--- | :--- |
| **深色底板（石板/暗褐）** | `#12100F` (Alpha ~0.91) ～ `#1E222B` / `#141312` | 面板背景、戰績卡底板、血條底槽 |
| **羊皮紙暖白（主文字）** | `#F5EFE6` / `#EBE4DA` | Boss 名稱、項目標題、主要數值 |
| **古銅金（主框/強調）** | `#D4A359` / `#E09F3E` (`224,159,62`) ～ `#BD995C` | 邊框、分隔線菱形 `◆`、進度標籤、勝利金光 |
| **香檳金 / 琥珀亮金** | `#F5D07D` / `#FCECA6` ～ `#F59E23` | Slider 填充條、升級按鈕、殘影扣血條 (`#C7A87D`→`#F5EDE0`) |
| **緋紅 / 暗紅（危險/失敗/關閉）** | `#DC2626` / `#991B1B` / `#B82424` | Boss 血條主填充、失敗標題 (`#FCA5A5`)、關閉按鈕 |
| **靈魂紫晶（局外貨幣）** | `#C084FC` / `#D8B4FE` / `#E4C6FF` | 靈魂卡片邊框、持有靈魂數值、紫晶微光 |
| **輔助灰銀（次要文字）** | `#9CA3AF` / `#D6C3A5` | 英文副標、次要說明、未選中分頁文字 |

### 1.2 Canvas、字型與互動鐵則

1. **Canvas 標準設定**：
   - `RenderMode`: `ScreenSpaceOverlay`
   - `CanvasScaler`: `ScaleWithScreenSize`，基準解析度 **`1920 × 1080`**，`matchWidthOrHeight = 0.5f`。
   - **效能規範**：高頻變動 UI（小地圖旋轉、滑動血條、傷害飄字）須隔離在獨立 Sub-Canvas 中（見 `PERFORMANCE_AND_UI_SPECIFICATION.md`）。
2. **中文字型（TextMeshPro）**：
   - 優先使用 `Assets/TextMesh Pro/Resources/Fonts & Materials/SourceHanSerifTW-SemiBold SDF.asset`。
   - 若動態搜尋 `t:TMP_FontAsset`，必須跳過預設英文字型 `LiberationSans`，避免中文缺字。
3. **射線遮擋優化**：
   - 所有純視覺裝飾圖片、背景、分隔線與 `TextMeshProUGUI`，**一律設定 `raycastTarget = false`**，僅可點擊的 `Button` / `Slider` 本體保留接收點擊。
4. **按鈕動效慣例**：
   - 主要互動按鈕可設定 `btn.transition = Selectable.Transition.None` 並掛載專案自訂的 `UIButtonTween`（如結算頁返回／重試按鈕）。

### 1.3 程式化 UI Sprite（PNG）生成規範

若以 Editor 腳本繪製高畫質 2D UI 素材，必須遵守以下幾何與匯入規則：

* **幾何造型公式（抗鋸齒 SDF）**：
  * **圓角矩形**：`SdfRoundedBox(p, b, r)`，外邊緣用 `Clamp01(0.5f - dist)` 做 1px 平滑抗鋸齒。
  * **45° 斜切角矩形（Chamfer Box）**：用於大廳卡片、屬性列、按鈕。
    ```csharp
    float cut = (Mathf.Abs(p.x) + Mathf.Abs(p.y) - (b.x + b.y - chamfer)) * 0.70710678f;
    float dist = Mathf.Max(rectDist, cut);
    ```
* **招牌裝飾語法**：
  * **四角微括號 `⌜ ⌝`**：取代粗俗全包框，在卡片/血條四角繪製長 `13~18px`、厚 `2~3px` 的亮色 L 形括號。
  * **中央漸層分隔線**：水平方向由中心向兩側依 `1 - |x - cx| / half` 線性衰減透明度，正中央鑲嵌一個 `r = 6px` 的金色菱形 `◆`（內嵌深色菱形核心）。
  * **立體漸層高光**：進度條與按鈕垂直方向由下至上從深色漸層至亮色，頂部 `2~3px` 混入白色高光緣（模擬金屬/能量條厚度）。
  * **純白可染色填充條**：Slider Fill 繪製為純白漸層（`slider_fill_white.png`），在場景中透過 Unity `Image.color` 染色以利多主題複用。
* **TextureImporter 固定參數**：
  * `textureType = TextureImporterType.Sprite`
  * `spriteImportMode = SpriteImportMode.Single`
  * `alphaIsTransparency = true`
  * `mipmapEnabled = false`
  * `filterMode = FilterMode.Bilinear`
  * 支援拉伸的面板底框需設定 `spriteBorder`（9-Slice，例如四周留 `24~32px`）並將 `Image.type` 設為 `Image.Type.Sliced`。

### 1.4 典型介面佈局規格參考

* **Boss 螢幕頂部血條 (`BossHealthHUD_TopCenter`)**：
  * 錨點固定 `Top-Center` (`anchorMin/Max = (0.5, 1)`, `pivot = (0.5, 1)`, `anchoredPosition = (0, -36)`, `size = 880×76`)。
  * 雙層 `Image.Type.Filled` (`Horizontal / Left`)：`Bar_Chip_Fill`（香檳金延遲殘影）在底層、`Bar_Main_Fill`（暗紅主條）在上層，最上層覆蓋帶微括號與 **50% 中央二階段金菱形刻度** 的 `Bar_Frame_Overlay`（`880×28`）。
* **系統設定彈窗 (`SettingsPanel`)**：
  * 緊湊比例 `560×470`（或 `512×640`），含頂部標題 + 金色漸層分隔線 + 3 分頁標籤（選中琥珀金銅 / 未選中暗灰石板）+ `490×58` 設定橫條（左標籤、中加厚 Slider、右百分比）+ 底部重置/關閉鈕。
* **大廳養成面板 (`CharacterUpgradePanel`)**：
  * `500×650` 與左側 `LeftPanel` 對稱，復用 `lobby_side_panel_bg.png`（9-Slice）與 `lobby_title_banner.png`，內含靈魂持有列、強化階級卡 + 進度條、3 項屬性摘要卡與底部升級按鈕。
* **結算頁面 (`ResultUI_VerC`)**：
  * 2D 暗角石牆背景（勝利暖金光暈 / 失敗暗紅光暈）+ 頂部標題橫幅 + **五連戰績卡**（進度[金框]、時間、殺敵、金幣、靈魂[紫框]，每張 `318×195`）+ 中央魂系長分隔線 + 左下角色屬性列 / 右下武器與護甲裝備欄 + 底部返回與重試按鈕。

---

## 第二部分：VFX 特效與程序化貼圖規範

### 2.1 URP 渲染與著色器鐵則

1. **標準 Shader**：
   - 所有粒子與刀光 Trail 統一使用 **`Universal Render Pipeline/Particles/Unlit`**（專案自訂特效除外，如 `Custom/URP_NecroSmokeDissolve`、`Custom/URP_NecroStaffGlow`）。
2. **Surface 與 Blend 模式設定**：
   - 統一開啟透明表面：`_Surface = 1`、`_ZWrite = 0`、`_Cull = CullMode.Off`、`_AlphaClip = 0`、`_ColorMode = 0`，並啟用關鍵字 `_SURFACE_TYPE_TRANSPARENT`。
   - **底層陰影 / 岩層 / 煙霧（Alpha Blend，RenderQueue `2999`）**：真正壓暗背景、建立質感厚度，不會被 Additive 吃掉。
     - `_Blend = 0`, `_SrcBlend = SrcAlpha`, `_DstBlend = OneMinusSrcAlpha`, `_SrcBlendAlpha = One`, `_DstBlendAlpha = OneMinusSrcAlpha`
   - **發光刃緣 / 核心 / 火花（Additive，RenderQueue `3000 ~ 3002`）**：
     - `_Blend = 2`, `_SrcBlend = SrcAlpha`, `_DstBlend = One`, `_SrcBlendAlpha = One`, `_DstBlendAlpha = One`
3. **HDR 亮度控制**：
   - 透過 `_BaseColor` / `_Color` 的 RGB 超過 `1.0`（例如 `1.3 ~ 2.5`）觸發 URP Bloom；但**寫實/厚重風格（如 `TitansTooth`）刻意壓低 HDR（`0.95 ~ 1.25`）**，避免過曝刺眼白光。
4. **實作 Smoothstep 的重要踩坑**：
   - **絕對不要將 Unity 的 `Mathf.SmoothStep(edge0, edge1, x)` 當作 GLSL `smoothstep` 使用**（Unity 內建版本前兩個參數是輸出上下限 `from, to` 而非邊界閾值，會導致貼圖 Alpha 異常變淡）。
   - 一律自行實作：
     ```csharp
     float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.00001f, edge1 - edge0));
     return t * t * (3f - 2f * t);
     ```

### 2.2 武器刀光架構標準（3 層 Trail + 1 層粒子）

專案中高品質武器 VFX 統一採用 **「1 層壓底質感 + 2 層發光刃核 + 1 層粒子」** 的拓撲，掛載在武器 Prefab 下並綁定至 `Weapon.weaponTrails` 與 `Weapon.weaponEffect`：

| 層級 | RenderQueue | Blend Mode | 作用與命名慣例 |
| :--- | :---: | :--- | :--- |
| **Layer 1（底層襯托）** | `2999` | Alpha Blend | 壓出空間對比與質量感（如 `Trail_Abyss_Shadow`、`Trail_Basalt_Slab`、`Trail_Katana_Gale`、`Trail_Daybreak_Veil`、`Trail_Titans_Aura`） |
| **Layer 2（主刃外緣）** | `3000` | Additive *(TitansTooth 為 Alpha)* | 表現武器獨特剪影：鋸齒、羽翼、岩齒、弦月（如 `Trail_*_Edge`） |
| **Layer 3（核心能量）** | `3001` | Additive | 高亮度細線、礦脈裂紋、電漿核心（如 `Trail_*_Core` / `Vein` / `Spine` / `Ray`） |
| **Layer 4（飛散粒子）** | `3002` | Additive *(碎岩可 Alpha)* | 星芒火花、重力碎岩/骨刺、熱能電漿微粒（`PS_*`），`SimulationSpace = World` |

#### `TrailRenderer` 必備標準參數
* `alignment = LineAlignment.View`
* `textureMode = LineTextureMode.Stretch`
* `minVertexDistance = 0.012f ~ 0.015f`
* `numCornerVertices = 4`、`numCapVertices = 2`（避免高速急轉折角破碎）
* `shadowCastingMode = Off`、`receiveShadows = false`、`lightProbeUsage = Off`、`reflectionProbeUsage = Off`、`generateLightingData = false`
* `emitting = false`、`autodestruct = false`（由戰鬥腳本在攻擊判定區間控制開關）

#### 座標與攻擊範圍包覆原則
* 武器模型與刀身統一沿 **局部 `-X` 軸** 延伸。
* 外緣主刀光（Layer 2）的掛載距離 + 半寬應**完整包覆或貼合攻擊判定球外緣**（即 `|AttackPoint.x| + attackRadius`）。
* 重型雙手武器可加重力粒子（`gravityModifier = 0.45 ~ 0.65`）；敏捷/聖光/輕武器使用無重力或微幅上揚漂浮粒子。

#### 各類武器節奏與寬度基準表

| 武器定位 | 代表武器 | 主刃寬度 | 核心寬度 | 底層寬度 | 滯留時間 `time` | 視覺語言關鍵字 |
| :--- | :--- | :---: | :---: | :---: | :---: | :--- |
| **極速太刀** | `Katana` | `0.30m` | `0.10m` | `0.45m` | `0.09s ~ 0.18s` | 柳葉弦月、針尖收束、高頻熱能電漿 |
| **輕型單手劍** | `Daybreak` | `0.46m` | `0.16m` | `0.54m` | `0.13s ~ 0.19s` | 破曉晨曦金紫、後掠羽翼鋸齒、菱羽四芒星 |
| **標準單手劍** | `RoyalSword` | `0.36m` | `0.22m` | *(2層)* | `0.12s ~ 0.16s` | 聖光青藍＋燦金、十字星芒、鑽石切面 |
| **敏捷虛空劍** | `StarEater` | `0.54m` | `0.18m` | `0.70m` | `0.16s ~ 0.32s` | 深淵紫黑吞噬陰影、鯊鰭倒刺、冷紫電隙 |
| **雙手重巨劍** | `Crag` | `0.92m` | `0.40m` | `1.30m` | `0.24s ~ 0.38s` | 玄武斷層、梯形崩口石齒、金珀礦脈、0.65G 碎石 |
| **雙手獸骨刃** | `TitansTooth` | `0.80m+` | `0.32m` | `1.22m` | `0.28s ~ 0.35s` | 暗沉土黃礦岩（無刺眼死白/火橘）、Voronoi 裂紋、0.50G 碎岩 |

> **禁止常駐光效原則**：近戰武器不隨便添加常駐劍身呼吸光或握把拖尾，特效 100% 聚焦於揮砍判定瞬間（如 `TitansTooth` 規範）。

### 2.3 程序化 VFX 貼圖規範

* **Trail UV 方向約定（`LineTextureMode.Stretch`）**：
  * **`U` 軸（水平）**：沿刀光長度，`0.0` = 刀頭最新處（迅速展開），`1.0` = 刀尾最舊處（鋸齒刷痕 / Voronoi 碎塊侵蝕 / 尖端收束消散）。
  * **`V` 軸（垂直）**：沿刀光寬度，圖案以 **`V = 0.5` 為中心上下近似對稱**，防止左揮 / 右揮時因 Trail UV 朝向翻轉造成正反面不一致。
* **標準解析度**：
  * 主刀光 / 殘影：`512×128` 或 `1024×256`
  * 核心細光束 / 脊脈：`512×64`
  * 粒子貼圖：`128×128` 或 `256×256`（512×512 用於高精細地裂 Decal）
* **TextureImporter 設定**：
  * `textureType = TextureImporterType.Default`
  * `alphaIsTransparency = true`
  * `mipmapEnabled = false`
  * `wrapMode = TextureWrapMode.Clamp`（粒子與單段刀光必設 Clamp，避免邊緣採樣滲漏出現方框邊界）
  * `filterMode = FilterMode.Bilinear`
  * 貼圖生成邊緣必須自然衰減至 `Alpha = 0`（零邊界矩形硬邊）。

### 2.4 非武器技能與 Boss 特效規範

1. **瞬移煙霧組 (`Teleport_Vanish_VFX` & `Teleport_Appear_VFX`)**：
   - 分層：`Abyss_Dark_Smoke` (Alpha Blend) + `Necro_Rim_Mist` (Additive 紫芒) + `Void_Slit_Flash` (垂直空間裂隙) + `Warp_Soul_Streaks` (向心/離心流光) + `Ground_Shadow_Ring` (局部 Y `-0.88m` 補償角色胸口生成高度以貼齊地板) + 動態點光源。
   - 動態差異：Vanish 使用 `radial < 0` 強烈向心坍縮；Appear 使用高初速 Burst 搭配 `limitVelocityOverLifetime` 空氣阻力急停，營造煙霧炸開後懸浮消散的厚重感。
2. **法杖施法光效 (`Staff_Glow_VFX`)**：
   - `Core_StarFlare`（中心呼吸星芒）+ `Inward_SoulVortex`（向心吸入死靈微粒）+ `Rune_Halo_Ring`（頂端旋轉微型法陣環）+ 點光源；Prefab 預設 `SetActive(false)` 以便於 Animation Clip 直接錄製開關。
3. **Boss 砸地重擊 (`Golem` / `StoneGiantSlam`)**：
   - 針對暗色地牢地面使用高對比冷灰/銀白氣浪與亮裂紋（避免土黃與地面混濁）。
   - 多層組合：放射狀地裂 Decal + 擴張震波環 + 硬邊低多邊形碎石網格粒子（`Mesh` renderMode）+ Toon 石塵 + 迸裂火花 + 相機 Cinemachine Impulse 震動。
4. **特效點光源效能鐵則**：
   - 所有附帶於 VFX 的動態 `Light (Point)` **必須設定 `shadows = LightShadows.None`**，並由專屬 Controller 腳本在生命週期結束時平滑衰減熄滅。

---

## 第三部分：原 `Assets/Editor/` 腳本清理對照表

記錄既有生成腳本的用途與資產去向（供清理時確認）：

| 腳本名稱 | 原本目的 | 實際資產現況 |
| :--- | :--- | :--- |
| `ARPGSettingsUIBuilder.cs` / `SettingsUIAssetGenerator.cs` | 設定介面 PNG 與面板建置 | 已整合移至 `Assets/SpritesUI/SettingsUI` 與 `SettingPanel` |
| `BossHealthUIGeneratorTool.cs` | Boss 頂部血條 PNG 與 UI | 已生成於 `Assets/Sprites/UI_BossBar` |
| `ResultUIGeneratorTool.cs` | 結算畫面 15 張 PNG 與 UI | 已生成於 `Assets/Sprites/UI_Result` |
| `LobbyUpgradeUIBuilder.cs` | 大廳角色養成 UI 與貼圖 | 已生成於 `Assets/SpritesUI/LobbyUpgrade` 與 `LobbyScene.unity` |
| `PlayerStatsScrollUIBuilder.cs` | 角色狀態欄 ScrollRect 擴充 | 已應用於 `GameScene.unity` |
| `DefaultDataSOAssetGenerator.cs` | 預設角色/敵人 SO 生成 | 已生成於 `Assets/ScriptableObjects/` 與 `Resources/` |
| `RoyalSwordTextureGenerator.cs` | RoyalSword 貼圖與 URP 材質 | 資產已移至 `Assets/VFX/RoyalSword` |
| `StoneGiantSlamVfxBuilder.cs` | 石巨人砸地 VFX | 實際資產位於 `Assets/VFX/Golem`（與 `GolemImpact`） |
| `TeleportSmokeVfxBuilder.cs` | 瞬移黑紫煙霧 VFX | 實際資產已移至 `Assets/VFX/Mage/TeleportSmoke` |
| `StaffGlowVfxBuilder.cs` | 法杖施法發光 VFX | 實際資產已移至 `Assets/VFX/Mage/SpawnMinionStaffGlow` |
| `Crag*` / `Daybreak*` / `Katana*` / `StarEater*` / `TitansTooth*`（10支） | 5 把武器的刀光貼圖與 Prefab 掛載 | 對應 `Assets/VFX/<Weapon>` 與 `Assets/Prefab/Weapon/<Weapon>.prefab` 皆已建置 |
