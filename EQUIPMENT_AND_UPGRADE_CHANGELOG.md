# 本次系統與數值修改完整紀錄（程式碼 + 資產清單）

本文件記錄本次對話期間所有實際修改的 C# 程式碼、Editor 工具、局外升級配置與防具資產異動明細。  
（完整的 36 件防具修改前後逐項數值對比表請見：[EQUIPMENT_BALANCE_COMPARISON.md](file:///c:/Unity/Dungeon/EQUIPMENT_BALANCE_COMPARISON.md)）

---

## 一、異動檔案總覽

| 類別 | 檔案路徑 | 異動類型 | 異動說明 |
| :--- | :--- | :---: | :--- |
| **屬性系統** | [Assets/Script/Stats/StatType.cs](file:///c:/Unity/Dungeon/Assets/Script/Stats/StatType.cs) | 修改 | 新增 `LifeSteal`（索引 9，命中吸血百分比） |
| **屬性系統** | [Assets/Script/Player/CharacterStats.cs](file:///c:/Unity/Dungeon/Assets/Script/Player/CharacterStats.cs) | 修改 | 新增 `statLifeSteal`、`LifeSteal`、`RemoveAllModifiersFromSource` 與 `GetStatByType` 對應 |
| **資料資產** | [Assets/Script/DataSO/EquipmentDataSO.cs](file:///c:/Unity/Dungeon/Assets/Script/DataSO/EquipmentDataSO.cs) | 修改 | Tooltip 支援 `LifeSteal` 百分比換算與顯示名稱「命中吸血」 |
| **資料資產** | [Assets/Script/DataSO/WeaponDataSO.cs](file:///c:/Unity/Dungeon/Assets/Script/DataSO/WeaponDataSO.cs) | 修改 | Tooltip 支援 `LifeSteal` 百分比換算與顯示名稱「命中吸血」 |
| **戰鬥系統** | [Assets/Script/Enemy/EnemyHealthSystem.cs](file:///c:/Unity/Dungeon/Assets/Script/Enemy/EnemyHealthSystem.cs) | 修改 | 實作命中吸血 `ApplyAttackerLifeSteal`、抽出共用 `TryGetAttackerStatsAndHealth`、韌性重置改用 `EffectiveMaxPoise` |
| **生命與韌性** | [Assets/Script/BaseClass/CharacterHealthBase.cs](file:///c:/Unity/Dungeon/Assets/Script/BaseClass/CharacterHealthBase.cs) | 修改 | 新增 `EffectiveMaxPoise`，於 `Start`/`BindStats`/`OnStatsChanged`/`RecoverPoise` 同步 `stats.Poise` |
| **生命與韌性** | [Assets/Script/Player/PlayerHealthSystem.cs](file:///c:/Unity/Dungeon/Assets/Script/Player/PlayerHealthSystem.cs) | 修改 | 玩家破韌重置改為使用 `EffectiveMaxPoise` |
| **局外升級** | [Assets/Script/MetaProgression/CharacterUpgradeConfigSO.cs](file:///c:/Unity/Dungeon/Assets/Script/MetaProgression/CharacterUpgradeConfigSO.cs) | 修改 | 預設增量調整為 `hpBonusPerLevel = 12f`、`defBonusPerLevel = 2f`、`atkBonusPerLevel = 2f` |
| **編輯器工具** | [Assets/Editor/DefaultDataSOAssetGenerator.cs](file:///c:/Unity/Dungeon/Assets/Editor/DefaultDataSOAssetGenerator.cs) | 修改 | 升級配置生成路徑改為 `Assets/Resources/CharacterUpgradeConfig.asset` |
| **升級資產** | [Assets/Resources/CharacterUpgradeConfig.asset](file:///c:/Unity/Dungeon/Assets/Resources/CharacterUpgradeConfig.asset) | 修改 | 遷移至新版 `CharacterUpgradeConfigSO` 欄位格式並寫入 `12 / 2 / 2` |
| **升級資產** | `Assets/ScriptableObjects/Characters/CharacterUpgradeConfig.asset` (`.meta`) | **刪除** | 移除未被場景引用的重複配置檔，避免再次改錯檔案 |
| **防具資產** | [KnightSet](file:///c:/Unity/Dungeon/Assets/ScriptableObjects/Equipment/KnightSet) / [SaintKnightSet](file:///c:/Unity/Dungeon/Assets/ScriptableObjects/Equipment/SaintKnightSet) | 修改 | 調整兩套共 36 件防具屬性與價格（詳見下方與對比表） |
| **對比文件** | [EQUIPMENT_BALANCE_COMPARISON.md](file:///c:/Unity/Dungeon/EQUIPMENT_BALANCE_COMPARISON.md) | 新增 | 36 件防具調整前後完整對比表 |

---

## 二、程式碼修改明細與代碼

### 1. 新增命中吸血屬性：`StatType.cs`
[StatType.cs](file:///c:/Unity/Dungeon/Assets/Script/Stats/StatType.cs#L1-L14)
```csharp
public enum StatType
{
    MaxHealth,          // 最大生命值 (0)
    Shield,             // 護甲 / 護盾 (減傷) (1)
    AttackDamage,       // 攻擊力 (2)
    CritRate,           // 暴擊率 (%) (3)
    MoveSpeed,          // 移動速度 (4)
    Poise,              // 韌性值 (5)
    CritDamage,         // 暴擊傷害倍率 (預設 1.5x) (6)
    LifeOnKill,         // 擊殺回血 (7)
    HitStunReduction,   // 縮短受擊硬直 (%) (8)
    LifeSteal           // 命中吸血 (%) (9)
}
```

### 2. 中央屬性容器：`CharacterStats.cs`
[CharacterStats.cs](file:///c:/Unity/Dungeon/Assets/Script/Player/CharacterStats.cs#L18-L45) / [L209-L239](file:///c:/Unity/Dungeon/Assets/Script/Player/CharacterStats.cs#L209-L239)
```csharp
// 欄位與公開屬性
[SerializeField] private CharacterStat statLifeSteal = new CharacterStat(0f);
public float LifeSteal => statLifeSteal.Value;

// RemoveAllModifiersFromSource(object source)
statHitStunReduction.RemoveAllModifiersFromSource(source);
statLifeSteal.RemoveAllModifiersFromSource(source);

// GetStatByType(StatType type)
case StatType.HitStunReduction: return statHitStunReduction;
case StatType.LifeSteal: return statLifeSteal;
```

### 3. 裝備與武器 Tooltip 顯示：`EquipmentDataSO.cs` & `WeaponDataSO.cs`
- [EquipmentDataSO.cs](file:///c:/Unity/Dungeon/Assets/Script/DataSO/EquipmentDataSO.cs#L71-L108)
- [WeaponDataSO.cs](file:///c:/Unity/Dungeon/Assets/Script/DataSO/WeaponDataSO.cs#L58-L94)

兩者皆同步加入 `StatType.LifeSteal` 判斷（以小數 `0.02` 儲存、顯示為 `+2%`）：
```csharp
else if (bonus.statType == StatType.CritDamage || bonus.statType == StatType.HitStunReduction || bonus.statType == StatType.LifeSteal)
{
    valStr = $"{bonus.value * 100f:0.#}%";
}
```
```csharp
case StatType.HitStunReduction: return "縮短受擊硬直";
case StatType.LifeSteal: return "命中吸血";
```

### 4. 命中吸血觸發與共用輔助：`EnemyHealthSystem.cs`
[EnemyHealthSystem.cs](file:///c:/Unity/Dungeon/Assets/Script/Enemy/EnemyHealthSystem.cs#L122-L200)
```csharp
// TakeDamage 內扣血完成後觸發：
SetAttacker(attacker);
ApplyAttackerLifeSteal(attacker, finaldamage);

// 破韌重置時改用 EffectiveMaxPoise：
currentPoise = EffectiveMaxPoise;
```
```csharp
private static bool TryGetAttackerStatsAndHealth(Transform attackerTransform, out CharacterStats attackerStats, out CharacterHealthBase attackerHealth)
{
    attackerStats = null;
    attackerHealth = null;
    if (attackerTransform == null) return false;

    attackerStats = attackerTransform.GetComponentInParent<CharacterStats>() ?? attackerTransform.GetComponentInChildren<CharacterStats>();
    if (attackerStats == null) return false;

    attackerHealth = attackerTransform.GetComponentInParent<CharacterHealthBase>() ?? attackerTransform.GetComponentInChildren<CharacterHealthBase>();
    return attackerHealth != null && !attackerHealth.isDead;
}

private void ApplyAttackerLifeSteal(Transform attackerTransform, int damageDealt)
{
    if (damageDealt <= 0) return;
    if (!TryGetAttackerStatsAndHealth(attackerTransform, out var attackerStats, out var attackerHealth)) return;

    float lifeStealRate = Mathf.Clamp01(attackerStats.LifeSteal);
    if (lifeStealRate <= 0f) return;

    float healAmount = damageDealt * lifeStealRate;
    if (healAmount > 0f)
    {
        attackerHealth.Recover(healAmount);
    }
}

protected override void Die()
{
    OnDeath?.Invoke();
    isDead = true;

    // 擊殺回血 (Life on Kill) 觸發
    if (TryGetAttackerStatsAndHealth(attacker, out var attackerStats, out var attackerHealth) && attackerStats.LifeOnKill > 0f)
    {
        attackerHealth.Recover(attackerStats.LifeOnKill);
        Debug.Log($"[擊殺回血] 觸發！{attacker.name} 回復 {attackerStats.LifeOnKill} 點生命");
    }
    // ...
}
```

### 5. 修復裝備韌性 (`Poise`) 未套用：`CharacterHealthBase.cs` & `PlayerHealthSystem.cs`
- [CharacterHealthBase.cs](file:///c:/Unity/Dungeon/Assets/Script/BaseClass/CharacterHealthBase.cs#L36-L240)
- [PlayerHealthSystem.cs](file:///c:/Unity/Dungeon/Assets/Script/Player/PlayerHealthSystem.cs#L63-L68)

```csharp
// CharacterHealthBase.cs
protected float EffectiveMaxPoise => stats != null ? stats.Poise : maxPoise;

// OnEnable / OnDisable / BindStats 訂閱與取消訂閱 stats.OnStatsChanged += HandleStatsChanged;
// BindStats 與 Start 同步初始化：
maxPoise = stats.Poise;
currentPoise = maxPoise;

private void HandleStatsChanged()
{
    float newMaxPoise = EffectiveMaxPoise;
    if (newMaxPoise > maxPoise)
    {
        currentPoise = Mathf.Min(newMaxPoise, currentPoise + (newMaxPoise - maxPoise));
    }
    else
    {
        currentPoise = Mathf.Min(newMaxPoise, currentPoise);
    }
    maxPoise = newMaxPoise;
}

private void RecoverPoise()
{
    float targetMaxPoise = EffectiveMaxPoise;
    if (currentPoise < targetMaxPoise)
    {
        if (poiseRecoveryTimer > 0)
        {
            poiseRecoveryTimer -= Time.deltaTime;
        }
        else
        {
            currentPoise = Mathf.Min(targetMaxPoise, currentPoise + poiseRecoveryRate * Time.deltaTime);
        }
    }
}
```
```csharp
// PlayerHealthSystem.cs
if (currentPoise <= 0)
{
    if (animator != null) animator.CrossFade(attackData.hitAnimationName, 0.1f);
    currentPoise = EffectiveMaxPoise;
}
```

### 6. 局外升級設定同步與預設值：`CharacterUpgradeConfigSO.cs` & `DefaultDataSOAssetGenerator.cs`
- [CharacterUpgradeConfigSO.cs](file:///c:/Unity/Dungeon/Assets/Script/MetaProgression/CharacterUpgradeConfigSO.cs#L19-L25)
```csharp
[Header("每次升級三圍同步加成")]
[Tooltip("每升一級增加的生命上限 (HP)")]
public float hpBonusPerLevel = 12f;
[Tooltip("每升一級增加的護甲防禦 (DEF)")]
public float defBonusPerLevel = 2f;
[Tooltip("每升一級增加的基礎攻擊 (ATK)")]
public float atkBonusPerLevel = 2f;
```
- [DefaultDataSOAssetGenerator.cs](file:///c:/Unity/Dungeon/Assets/Editor/DefaultDataSOAssetGenerator.cs#L200-L202)
```csharp
// 8. 角色局外升級設定 (CharacterUpgradeConfig)
EnsureDirectory("Assets/Resources");
string upgradeConfigPath = "Assets/Resources/CharacterUpgradeConfig.asset";
```

---

## 三、資產檔案異動明細

### 1. 局外升級配置：[CharacterUpgradeConfig.asset](file:///c:/Unity/Dungeon/Assets/Resources/CharacterUpgradeConfig.asset)
將原本殘留的舊版巢狀欄位（`healthUpgrade` / `defenseUpgrade` / `attackUpgrade`，導致反序列化失敗而吃回 C# 預設 `+4` 攻擊）替換為新版欄位格式，並刪除重複的 `Assets/ScriptableObjects/Characters/CharacterUpgradeConfig.asset`：

```yaml
  m_Name: CharacterUpgradeConfig
  m_EditorClassIdentifier: Assembly-CSharp::CharacterUpgradeConfigSO
  maxLevel: 10
  baseCost: 50
  costIncrementPerLevel: 35
  hpBonusPerLevel: 12
  defBonusPerLevel: 2
  atkBonusPerLevel: 2
```

- 滿級 (Lv.10) 累積加成：**+120 HP / +20 護甲 / +20 攻擊力**（總花費 2,075 靈魂）。

### 2. 防具資產（共 36 件）
- **騎士套裝（KnightSet）**：保留原防禦數值並補入均衡輸出／機動詞條（頭盔暴擊率 +1%/+2%/+3%、胸甲攻擊 +1/+2/+4、腰帶暴傷 +5%/+8%/+12%、護腿移速 +1%/+3%/+4% `PercentAdd`、靴子移速 +7%/+11%/+17% `PercentAdd`）；全套總價調整為 **T1: 102 / T2: 251 / T3: 490**。
- **聖騎士套裝（SaintKnightSet）**：護手移除暴擊與暴傷，改為「攻擊力 + 命中吸血」（T1: 攻擊 +4 / 吸血 +2%、T2: 攻擊 +7 / 吸血 +3%、T3: 攻擊 +10 / 吸血 +5%）；靴子移速改為 +4%/+7%/+11% `PercentAdd`；全套總價調整為 **T1: 119 / T2: 292 / T3: 557**。
- 36 件裝備舊版與新版逐項表格請見：[EQUIPMENT_BALANCE_COMPARISON.md](file:///c:/Unity/Dungeon/EQUIPMENT_BALANCE_COMPARISON.md)。
