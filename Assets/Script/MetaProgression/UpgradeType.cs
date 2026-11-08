using System;

/// <summary>
/// 角色局外養成屬性類型
/// </summary>
public enum UpgradeType
{
    Health,   // 生命上限強化
    Defense,  // 護甲/防禦強化
    Attack    // 基礎攻擊力強化
}

/// <summary>
/// 單項屬性升級的即時資訊包 (供 UI 資料綁定)
/// </summary>
[System.Serializable]
public struct UpgradeInfo
{
    public UpgradeType type;
    public string displayName;
    public int currentLevel;
    public int maxLevel;
    public bool isMaxLevel;
    public int nextCost;
    public float currentBonus;
    public float nextBonus;
    public bool canAfford;

    public UpgradeInfo(UpgradeType type, string displayName, int currentLevel, int maxLevel, int nextCost, float currentBonus, float nextBonus, bool canAfford)
    {
        this.type = type;
        this.displayName = displayName;
        this.currentLevel = currentLevel;
        this.maxLevel = maxLevel;
        this.isMaxLevel = currentLevel >= maxLevel;
        this.nextCost = nextCost;
        this.currentBonus = currentBonus;
        this.nextBonus = nextBonus;
        this.canAfford = canAfford && !this.isMaxLevel;
    }
}
