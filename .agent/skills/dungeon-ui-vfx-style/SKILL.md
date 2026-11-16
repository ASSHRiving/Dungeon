---
name: dungeon-ui-vfx-style
description: 在 Dungeon Unity URP 專案中設計、生成或修改任何 2D UI 介面（uGUI / TextMeshPro / 程序化 Sprite）或 VFX 特效（武器刀光 TrailRenderer、ParticleSystem、程序化貼圖、URP 粒子材質、Boss/法術特效）時優先載入。鎖定暗黑奇幻金銅＋地牢石板 UI 視覺語言、1920x1080 基準與動靜分離原則、URP 2999~3002 分層渲染、GlslSmoothstep 實作與武器 -X 軸向包覆規則。
---

# Dungeon UI & VFX Style Skill

在進行本專案的 **2D UI 建置/美化** 或 **URP VFX 特效/貼圖製作** 前，請務必先閱讀專案根目錄的完整規格書：
👉 **[UI_AND_VFX_STYLE_GUIDE.md](file:///c:/Unity/Dungeon/UI_AND_VFX_STYLE_GUIDE.md)**

## 不可妥協的核心紅線 (Quick Guardrails)

### UI 規範
1. **視覺語言**：暗黑奇幻金銅 (`#D4A359` / `#E09F3E`) ＋ 地牢深灰石板 (`#12100F` ~ `#1E222B`)；多用四角微括號 `⌜ ⌝`、45° 斜切角（Chamfer）與中心漸層菱形分隔線 `◆`，避免廉價純色大圓角方塊。
2. **字型與畫布**：`CanvasScaler` 基準 `1920×1080` (`matchWidthOrHeight = 0.5f`)；中文字型優先使用 `SourceHanSerifTW-SemiBold SDF.asset`（自動搜尋須排除 `LiberationSans`）。
3. **效能與防呆**：純視覺圖片與 `TMP_Text` 一律關閉 `raycastTarget = false`；高頻更新 UI（小地圖、扣血動畫、飄字）必須分離至獨立 Sub-Canvas。

### VFX 規範
1. **貼圖 Smoothstep 鐵則**：程式化繪製貼圖時**嚴禁使用 `Mathf.SmoothStep`**，必須自行實作 `GlslSmoothstep(edge0, edge1, x)`，避免 Alpha 被壓縮。
2. **TextureImporter**：刀光與粒子貼圖一律設定 `alphaIsTransparency = true`、`mipmapEnabled = false`、`wrapMode = TextureWrapMode.Clamp`。
3. **Trail UV 與武器軸向**：刀身沿局部 `-X` 軸延伸；Trail `TextureMode = Stretch` 下，`U=0` 為刀頭最新、`U=1` 為刀尾消散，圖案須以 `V=0.5` 上下近似對稱，且外緣寬度須包覆 `AttackPoint + attackRadius`。
4. **URP 4 層分層拓撲**：
   - `Q:2999` Alpha Blend 底層陰影/風帶/岩層（壓暗對比）
   - `Q:3000` Additive 主刃外緣（或厚重風格用 Alpha Blend）
   - `Q:3001` Additive 核心能量絲/礦脈/電漿
   - `Q:3002` 世界空間粒子系統 (`SimulationSpace = World`)，綁定至 `Weapon.weaponTrails` 與 `Weapon.weaponEffect`。
5. **效能約束**：所有 VFX 內附帶的動態點光源必須設定 `shadows = LightShadows.None`；近戰武器不添加無關的常駐呼吸光。
