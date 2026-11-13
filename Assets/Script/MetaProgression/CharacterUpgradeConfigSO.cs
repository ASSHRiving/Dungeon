using UnityEngine;

/// <summary>
/// 角色升級配置資產：定義三圍同步升級之等級上限、靈魂消耗曲線與各屬性每級增量
/// </summary>
[CreateAssetMenu(fileName = "CharacterUpgradeConfig", menuName = "Dungeon/Meta/Upgrade Config")]
public class CharacterUpgradeConfigSO : ScriptableObject
{
    [Header("全能力同步升級設定")]
    [Tooltip("角色強化的最高等級上限")]
    public int maxLevel = 10;

    [Header("靈魂消耗曲線")]
    [Tooltip("Lv.0 升 Lv.1 的初始消耗")]
    public int baseCost = 50;
    [Tooltip("每升一級額外增加的靈魂消耗")]
    public int costIncrementPerLevel = 35;

    [Header("每次升級三圍同步加成")]
    [Tooltip("每升一級增加的生命上限 (HP)")]
    public float hpBonusPerLevel = 15f;
    [Tooltip("每升一級增加的護甲防禦 (DEF)")]
    public float defBonusPerLevel = 3f;
    [Tooltip("每升一級增加的基礎攻擊 (ATK)")]
    public float atkBonusPerLevel = 4f;

    /// <summary>
    /// 計算給定等級升至下一級所需的靈魂花費 (滿級傳回 0)
    /// </summary>
    public int GetCostForLevel(int currentLevel)
    {
        if (currentLevel >= maxLevel) return 0;
        return baseCost + currentLevel * costIncrementPerLevel;
    }

    /// <summary>
    /// 獲取指定屬性在給定等級的累積總加成
    /// </summary>
    public float GetStatBonus(UpgradeType type, int level)
    {
        int clampedLevel = Mathf.Clamp(level, 0, maxLevel);
        switch (type)
        {
            case UpgradeType.Health: return clampedLevel * hpBonusPerLevel;
            case UpgradeType.Defense: return clampedLevel * defBonusPerLevel;
            case UpgradeType.Attack: return clampedLevel * atkBonusPerLevel;
            default: return 0f;
        }
    }
}
