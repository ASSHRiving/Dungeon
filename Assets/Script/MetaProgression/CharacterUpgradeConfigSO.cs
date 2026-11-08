using UnityEngine;

/// <summary>
/// 角色升級配置資產：定義生命、防禦、攻擊的升級上限、消耗曲線與數值加成
/// </summary>
[CreateAssetMenu(fileName = "CharacterUpgradeConfig", menuName = "Dungeon/Meta/Upgrade Config")]
public class CharacterUpgradeConfigSO : ScriptableObject
{
    [System.Serializable]
    public class StatUpgradeSetting
    {
        [Header("基礎設定")]
        public string displayName = "屬性";
        public int maxLevel = 10;

        [Header("消耗靈魂 (Soul) 設定")]
        [Tooltip("Lv.0 升 Lv.1 的初始消耗")]
        public int baseCost = 50;
        [Tooltip("每升一級額外增加的靈魂消耗")]
        public int costIncrementPerLevel = 30;

        [Header("屬性加成設定")]
        [Tooltip("每升一級增加的數值 (固定值加成)")]
        public float bonusPerLevel = 15f;

        /// <summary>
        /// 計算給定等級升至下一級所需的靈魂花費
        /// </summary>
        public int GetCostForLevel(int currentLevel)
        {
            if (currentLevel >= maxLevel) return 0;
            return baseCost + currentLevel * costIncrementPerLevel;
        }

        /// <summary>
        /// 計算給定等級所獲得的累積總加成
        /// </summary>
        public float GetTotalBonus(int level)
        {
            int clampedLevel = Mathf.Clamp(level, 0, maxLevel);
            return clampedLevel * bonusPerLevel;
        }
    }

    [Header("三大核心局外屬性設定")]
    public StatUpgradeSetting healthUpgrade = new StatUpgradeSetting
    {
        displayName = "生命上限",
        maxLevel = 10,
        baseCost = 50,
        costIncrementPerLevel = 30,
        bonusPerLevel = 15f // Lv 10 時獲得 +150 HP
    };

    public StatUpgradeSetting defenseUpgrade = new StatUpgradeSetting
    {
        displayName = "防禦護甲",
        maxLevel = 10,
        baseCost = 40,
        costIncrementPerLevel = 25,
        bonusPerLevel = 3f  // Lv 10 時獲得 +30 護盾
    };

    public StatUpgradeSetting attackUpgrade = new StatUpgradeSetting
    {
        displayName = "基礎攻擊",
        maxLevel = 10,
        baseCost = 60,
        costIncrementPerLevel = 40,
        bonusPerLevel = 4f  // Lv 10 時獲得 +40 攻擊力
    };

    public StatUpgradeSetting GetSetting(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Health: return healthUpgrade;
            case UpgradeType.Defense: return defenseUpgrade;
            case UpgradeType.Attack: return attackUpgrade;
            default: return healthUpgrade;
        }
    }
}
