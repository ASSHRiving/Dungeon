# Dungeon 專案資產授權健檢與刪減版清理清單

> 盤點日期：2026-11-20  
> 盤點範圍：`Assets/GameAssets`、`Assets/Res`、`Assets/Animator`、`Assets/Prefab`、`Assets/ScriptableObject`

---

## 0. 發佈授權紅線速查

| 發佈形式 | Unity Asset Store（免費／已購） | Mixamo | CC0 | CC-BY-4.0 | 未授權 / 商業 IP 二創 |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **編譯後的遊戲執行檔（Steam / itch.io / 比賽，含商用）** | ✅ 允許（Standard EULA） | ✅ 允許 | ✅ 允許 | ✅ 允許（**須署名**） | ❌ 禁止 |
| **公開原始碼 GitHub Repo** | ❌ **禁止**（須放進 `.gitignore`） | ❌ 禁止再散佈原檔 | ✅ 允許 | ✅ 允許（須署名） | ❌ 禁止 |

---

## 1. `Assets/GameAssets` 未授權資產清理進度總表

| 資料夾 | 容量 | 目前狀態 | 外部引用 | 處理方式 |
| :--- | :---: | :---: | :--- | :--- |
| `Assets/GameAssets/Evil Spirit Katana` | 341 MB | ✅ 已刪除 | 0 個引用（原含無用 `Resources/`） | 已完成 |
| `Assets/GameAssets/Sci_Fi_Character_09` | 1.93 GB | ✅ 已刪除 | 原僅被 `Legacy/Character/Enemy.prefab` 引用（已刪） | 已完成 |
| `Assets/GameAssets/Import/Thermal_Katana` | 29 MB | ✅ 已移出 | 搬移至 `Assets/Res/Model/Thermal_Katana`（CC-BY-4.0） | 保留並於 Credits 署名 |
| `Assets/GameAssets/Sound` | 3.7 MB | ⬜ 待處理 | 1 個：`Assets/ScriptableObject/SoundAssets/SoundAssets.asset`（13 個音效） | 替換 13 個音效後整包刪除 |
| `Assets/GameAssets/GreatSword_Animset` | 1.91 GB | ⬜ 待處理 | 2 個有效 Controller（`Player.controller`、`Skeleton.controller`）＋ 2 個閒置 Legacy Controller | 刪閒置 Controller → 替換 40 個動畫 → 整包刪除 |

### 1A. `Assets/GameAssets/Sound` 需替換音效明細（共 13 個）

全部集中在 `Assets/ScriptableObject/SoundAssets/SoundAssets.asset`（對應 `GameSoundSO.cs` 的 `SoundAssetsType`）：

| `SoundAssetsType` | Enum 值 | 數量 | 目前引用的未授權檔案（位於 `Assets/GameAssets/Sound/`） | 建議替代來源 |
| :--- | :---: | :---: | :--- | :--- |
| **`SwordHit`**（劍擊命中） | 0 | 5 | `Lang/Hit_0.wav`, `Hit_1.wav`, `Hit_2.wav`, `Hit_3.wav`, `Hit_4.wav` | `Assets/Res/Sound/Audio/` 現有合法音效或 CC0 音效 |
| **`Sword`**（單手劍／太刀揮砍） | 1 | 5 | `Lang/swing-sword.wav`, `swing-sword2.wav`, `SwordWave_2.wav`, `SwordWave_3.wav`, `SwordWave_4.wav` | `Assets/Res/Sound/Audio/` 現有合法音效或 CC0 音效 |
| **`GSword`**（巨劍重揮） | 2 | 3 | `Lang/GS/GS0.wav`, `GS1.wav`, `GS2.wav` | `Assets/Res/Sound/Audio/` 現有合法音效或 CC0 音效 |

### 1B. `Assets/GameAssets/GreatSword_Animset` Animator Controller 盤點

| Animator Controller | 目前狀態 | 實際使用者 | 引用動畫數 | 處理方式 |
| :--- | :---: | :--- | :---: | :--- |
| `Assets/Animator/Player/Player.controller` | ⬜ 有效（核心） | `Warrior.prefab`、`LobbyScene.unity` | 40 個 | 替換為 `Assets/Res/Animation` 合法動畫 |
| `Assets/Animator/Enemy/Skeleton.controller` | ⬜ 有效（核心） | `SkeletonMinion.prefab` | 26 個（全為 Player 子集） | 跟隨 Player 同一套 40 個對應表替換 |
| `Assets/Animator/Enemy/Boss.controller` | ⬜ 閒置 Legacy | 僅 `Legacy/Character/Boss.prefab`（正式場景 0 引用） | 34 個 | 連同 Legacy Boss 一起刪除 |
| `Assets/Animator/Character.controller` | ⬜ 閒置 Legacy | **0 個引用** | 35 個 | 直接刪除 |
| `Assets/Animator/Enemy/Enemy.controller` | ✅ 已刪除 | 原僅 `Legacy/Character/Enemy.prefab`（已刪） | — | 已完成 |

#### 實際需替換的 40 個動畫分類（`Player.controller` + `Skeleton.controller` 聯集）

| 動作分類 | 數量 | 包含動畫 | 是否綁定 Animation Event |
| :--- | :---: | :--- | :--- |
| **攻擊連擊（最關鍵）** | 7 | 太刀 4 段（`GhostSamurai_APose_Attack01_1..4`）＋ 巨劍 3 段（`Inplace_GreatSword_Attack01..03`） | ⚠️ **7 個全部有**：`OnAttackHitboxStart`、`OnAttackHitboxEnd`、`OnAnimationFinishEvent` |
| **移動 BlendTree** | 21 | Idle (2)、普通 Walk/Sprint (2)、8向鎖定 Walk (8)、8向鎖定 Run + 前衝 (9) | ⚠️ **其中 4 個前走/前跑有**：`PlayFootstepSound`（`Strafe_Run_F_Sprint`、`Strafe_Walk_F_Loop`、`Common_Run_Sprint`、`Common_Walk_Loop`） |
| **受擊（Hit）** | 5 | `Hit_F`、`Hit_FL`、`Hit_FR`、`Hit_L`、`Hit_R` | 無 |
| **閃避（Dodge）** | 4 | `Dodge_F`、`Dodge_B`、`Dodge_L`、`Dodge_R` | 無 |
| **死亡與蹲下** | 3 | `Die01` (1) ＋ `Common2CommonCrouch` / `CommonCrouch2Common` (2) | 無 |

> 替換攻擊動畫後，記得同步微調 `Assets/Editor/ComboSetSetupTool.cs` 中的連擊取消時間點與 Hitbox Event 幀數。

---

## 2. `Assets/Res` 授權健檢總表

### 2A. 🔴 高風險（建議刪除或替換）

| 路徑 | 檔案特徵 / 問題原因 | 風險類型 | 目前引用 | 建議處理方式 | 狀態 |
| :--- | :--- | :--- | :--- | :--- | :---: |
| `Assets/Res/Model/Sword/` | 內含 `Rebellion_Sword.fbx`（《惡魔獵人》但丁「叛逆之刃」），Sketchfab 結構且無授權檔 | 商業 IP 侵權風險 | `Assets/Prefab/Weapon/Sword.prefab` | 換成 `FREE Low Poly Weapons Pack – Fantasy Dream` 或 `MeleeWeaponsPack` 合法長劍模型後刪除 | ⬜ |
| `Assets/Res/Model/spartan/` | 內含 `Spartan_Sketchfab.fbx`、`ODST_Shoulder_Mat_*`（微軟《最後一戰 Halo》裝甲），無授權檔 | 商業 IP 侵權風險 | 僅 `Assets/Prefab/Legacy/Character/Boss.prefab`（正式場景 0 引用） | 連同 Legacy Boss 一起整包刪除 | ⬜ |
| `Assets/Res/Model/Samruai/` | 內含 `model1.fbx`，Sketchfab 下載結構但缺少 `license.txt` | 授權不明（可能禁商用） | **0 個引用** | 直接整包刪除 | ⬜ |

### 2B. 🟡 中度留意（需署名、補查來源或評估風險）

| 路徑 | 授權現況與注意事項 | 目前引用 | 建議處理方式 | 狀態 |
| :--- | :--- | :--- | :--- | :---: |
| `Assets/Res/Model/Thermal_Katana/` | 附 `license.txt`：**CC-BY-4.0**（允許商用，**必須署名作者 Valery Kharitonov**） | `Assets/Prefab/Weapon/Katana.prefab` | ① 保留署名到 Credits / README；② 若為商業販售，自行評估外觀與《電馭叛客 2077》熱能武士刀相似度 | ⬜ |
| `Assets/Res/Sound/FreeSound/` | 共 7 檔，其中 `500901__bertsz__wood-crate-destory-2.wav` 已確認為 **CC0**；其餘 6 檔（`Barrel_Broke.wav`、`ChestOpen.wav`、`CollectCoin.mp3`、`EquipSound.mp3`、`GateDown.wav`、`Heavy_hollow_wooden__#4-1793967368327.wav`）已改名，無法從檔名確認是否為 CC0 或 CC-BY-NC | 17 個 Prefab（木桶、木箱、裝備等） | 確認原下載來源皆為 CC0／可商用；若不確定，改用 `Assets/Res/Sound/Audio/` 內合法音效替換 | ⬜ |
| `Assets/Res/Sound/Audio/`<br>`Assets/Res/Sound/Sounds/`<br>`Assets/Res/UI/UI/`<br>`Assets/Res/UI/HelpHints/`<br>`Assets/Res/UI/IconBackground/`<br>`Assets/Res/UI/Projector/` | 命名風格來自 **AnyRPG**（Unity Asset Store 免費開源引擎，程式碼 MIT）。原則上可合法使用，但開源引擎整合的美術／音效可能附帶第三方 CC-BY 署名要求 | `GameManager.prefab`、`SoundAssets.asset`、UI Prefab 等 | 回原包查看 `Third-Party Notices` 確認是否有需署名的項目 | ⬜ |

### 2C. 🟢 綠燈（合法來源，確認為自有帳號下載／自製即可安心使用）

| 類別 | 路徑 | 來源 / 授權說明 | 備註 |
| :--- | :--- | :--- | :--- |
| **動畫** | `Assets/Res/Animation/Blender/` | 自製 | 完全自有版權 |
| **動畫** | `Assets/Res/Animation/Mixamo/` | Adobe Mixamo | 免費可商用（禁止轉賣原始動畫檔） |
| **動畫** | `Assets/Res/Animation/EEJANAI_Team/` | Unity Asset Store（FreeSwordAnimations） | 內含 Unity Toon Shader（Unity Companion License） |
| **動畫** | `Assets/Res/Animation/ExplosiveLLC/` | Unity Asset Store（Warrior Pack Bundle 1 & 2 FREE） | Standard EULA |
| **動畫** | `Assets/Res/Animation/Kevin Iglesias/` | Unity Asset Store | Standard EULA |
| **動畫** | `Assets/Res/Animation/Soldier/` | Unity Asset Store（Kevin Iglesias Human Melee / Soldier 2.0 FREE） | Standard EULA |
| **動畫** | `Assets/Res/Animation/RPG_Movement/` | Unity Asset Store（DoubleL - RPG Animations Pack） | 確認以自己帳號正當取得即適用 Standard EULA |
| **特效** | `Assets/Res/Effect/EffectPack/` | Unity Asset Store（Magic effects pack） | Standard EULA |
| **特效** | `Assets/Res/Effect/VFX/` | Unity Asset Store（FreeQuickEffectsVol1） | Standard EULA |
| **模型** | `Assets/Res/Model/Blender/` | 自製（含 `RoyalSword__anim3.fbx`） | 保留 `Blender/RoyalSword/` |
| **模型** | `Assets/Res/Model/Three.js/` | 自製（`Potion`、`Table`） | 完全自有版權 |
| **模型** | `Assets/Res/Model/AurynSky/` | Unity Asset Store（Dungeon Pack） | README 明確寫明允許商用 |
| **模型** | `Assets/Res/Model/BrokenVector/` | Unity Asset Store（LowPolyDungeon） | Standard EULA |
| **模型** | `Assets/Res/Model/DungeonRes/` | Unity Asset Store（Elementary Dungeon Pack Lite / LowPolyDungeonsLite） | Standard EULA |
| **模型** | `Assets/Res/Model/FREE Low Poly Weapons Pack – Fantasy Dream/` | Unity Asset Store（GanzSe） | Standard EULA |
| **模型** | `Assets/Res/Model/GanzSe Free Character Accessories/` | Unity Asset Store（GanzSe） | Standard EULA（與外層 `Assets/URP GanzSe Free Character Accessories` 可擇一整理） |
| **模型** | `Assets/Res/Model/LowPolyMedievalFantasyBipedCreatures/` | Unity Asset Store | Standard EULA |
| **模型** | `Assets/Res/Model/MeleeWeaponsPack/` | Unity Asset Store | Standard EULA |
| **模型** | `Assets/Res/Model/ModularItems/` | Unity Asset Store（GanzSe） | Standard EULA |
| **模型** | `Assets/Res/Model/SazenGames/` | Unity Asset Store（Skeleton） | Standard EULA |
| **模型** | `Assets/Res/Model/coin/` | Unity Asset Store（Stylized Coin Pack） | Standard EULA |
| **音效** | `Assets/Res/Sound/Grimytheus Vol. 1/` | Unity Asset Store（juanjo_sound） | Standard EULA，可商用 |
| **UI** | `Assets/Res/UI/Clean Vector Icons/` | Unity Asset Store | Standard EULA |
| **UI** | `Assets/Res/UI/Icon1/` | Unity Asset Store（Heathen Flat Icons Free） | Standard EULA |

---

## 3. 重複檔與閒置 Legacy 檔案清理清單

| 路徑 | 原因 | 狀態 |
| :--- | :--- | :---: |
| `Assets/Res/Model/RoyalSword*.fbx`（根目錄 4 檔）、`Mat_HolyGem2.mat`、`RuinSword.fbx`、`GSSword.fbx` | 與 `Assets/Res/Model/Blender/RoyalSword/` 重複且 0 引用（正式引用的是 `Blender/RoyalSword/RoyalSword__anim3.fbx`） | ✅ 已清理 |
| `Assets/Prefab/Legacy/Character/Enemy.prefab` & `Assets/Animator/Enemy/Enemy.controller` | 舊版未啟用 | ✅ 已刪除 |
| `Assets/Prefab/Legacy/Character/Boss.prefab` | 正式關卡 0 引用，且同時拉住 `spartan` 與 `Boss.controller` | ⬜ 建議刪除 |
| `Assets/Animator/Enemy/Boss.controller` | 僅被上述 Legacy Boss 引用 | ⬜ 建議刪除 |
| `Assets/Animator/Character.controller` | 全專案 0 引用 | ⬜ 建議刪除 |

---

## 4. 必須放入遊戲 Credits / README 的署名清單

若保留 `Thermal_Katana`，須於製作名單或發佈頁面附上：

```text
This work is based on "Thermal Katana" (https://sketchfab.com/3d-models/thermal-katana-fb401eace2f447f8a25ea7527e0833bf) by Valery Kharitonov (https://sketchfab.com/KJLOYH) licensed under CC-BY-4.0 (http://creativecommons.org/licenses/by/4.0/)
```

*(若後續確認 `FreeSound/` 或 AnyRPG 素材中有其他 CC-BY 項目，也一併補在這裡。)*

---

## 5. 正式發佈前最終驗收 CheckList

- [ ] 以獨立資料夾（`git archive` 或 `git worktree`）切出刪減發佈版，不破壞原專案進度
- [ ] `Assets/GameAssets/` 已整個資料夾清空刪除
- [ ] `Assets/Res/Model/Sword`（叛逆之刃）、`spartan`、`Samruai` 已替換或刪除
- [ ] `Assets/Res/Sound/FreeSound/` 其餘 6 檔來源已確認可商用或替換完成
- [ ] 確認 `Assets/TextMesh Pro/` 內使用的中英文字體授權允許商業嵌入（例如思源黑體 / 免費商用字型，避免誤用微軟正黑體或華康等系統商用字型）
- [ ] 確認 `Assets/Resources/` 下沒有殘留未授權或測試用資源
- [ ] 確認 `EditorBuildSettings` 僅勾選 4 個正式場景：`MainMenu`、`LobbyScene`、`GameScene`、`FinalScene`
- [ ] 執行一次正式 Build，檢查 `Editor.log` 中的 **Used Assets** 清單，確認 0 個未授權資產被打包進去
- [ ] 若要公開 GitHub 原始碼：建立**全新 Git 歷史**，並將所有 Asset Store 第三方資料夾加入 `.gitignore`
