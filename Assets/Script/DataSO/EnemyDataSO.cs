using UnityEngine;

/// <summary>
/// 敵人數值配置資產：定義單一怪物種類的基礎屬性、關卡難度成長係數與菁英怪倍率
/// </summary>
[CreateAssetMenu(fileName = "EnemyData_", menuName = "Dungeon/Stats/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("敵人識別")]
    public string enemyID = "Skeleton_Warrior";
    public string enemyName = "骷髏戰士";
    public bool isBoss = false;

    [Header("基礎屬性 (Level 1 基準值)")]
    public float baseMaxHealth = 80f;
    public float baseDamage = 10f;
    public float baseShield = 0f;
    public float baseMoveSpeed = 3.5f;
    public float basePoise = 80f;
    public int baseGold = 5;

    [Header("關卡縮放係數 (每小關成長比例)")]
    [Tooltip("每推進一關血量增加的比例 (例如 0.15 代表每關 +15% 生命)")]
    public float healthGrowthPerLevel = 0.15f;

    [Tooltip("每推進一關攻擊力增加的比例 (例如 0.08 代表每關 +8% 傷害)")]
    public float damageGrowthPerLevel = 0.08f;

    [Tooltip("每推進一關金幣掉落增加的比例 (例如 0.10 代表每關 +10% 金幣)")]
    public float goldGrowthPerLevel = 0.10f;

    [Header("菁英怪倍率加成 (若是菁英怪)")]
    public float eliteHealthMult = 2.2f;
    public float eliteDamageMult = 1.35f;
    public float eliteGoldMult = 3.0f;
    public float eliteScale = 1.25f;

    /// <summary>
    /// 計算給定關卡與菁英狀態下的最終最大生命值
    /// 公式：BaseHP × (1 + (Level - 1) × 成長率) × (菁英倍率)
    /// </summary>
    public float CalculateMaxHealth(int level, bool isElite = false)
    {
        int levelOffset = Mathf.Max(0, level - 1);
        float hp = baseMaxHealth * (1f + levelOffset * healthGrowthPerLevel);
        if (isElite) hp *= eliteHealthMult;
        return Mathf.Round(hp);
    }

    /// <summary>
    /// 計算給定關卡與菁英狀態下的最終攻擊力
    /// </summary>
    public float CalculateDamage(int level, bool isElite = false)
    {
        int levelOffset = Mathf.Max(0, level - 1);
        float dmg = baseDamage * (1f + levelOffset * damageGrowthPerLevel);
        if (isElite) dmg *= eliteDamageMult;
        return Mathf.Round(dmg);
    }

    /// <summary>
    /// 計算給定關卡與菁英狀態下的掉落金幣數量
    /// </summary>
    public int CalculateGold(int level, bool isElite = false)
    {
        int levelOffset = Mathf.Max(0, level - 1);
        float gold = baseGold * (1f + levelOffset * goldGrowthPerLevel);
        if (isElite) gold *= eliteGoldMult;
        return Mathf.Max(1, Mathf.RoundToInt(gold));
    }
}
