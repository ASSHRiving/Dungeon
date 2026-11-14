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
/// 角色三圍同步升級的綜合即時快照資訊包 (供 UI 直接資料綁定)
/// </summary>
[System.Serializable]
public struct CharacterUpgradeSnapshot
{
    public int characterID;
    public int currentLevel;
    public int maxLevel;
    public bool isMaxLevel;
    public int nextCost;
    public bool canAfford;

    public float currentHpBonus;
    public float nextHpBonus;
    public float currentDefBonus;
    public float nextDefBonus;
    public float currentAtkBonus;
    public float nextAtkBonus;

    public CharacterUpgradeSnapshot(
        int characterID, int currentLevel, int maxLevel, int nextCost, bool canAfford,
        float curHp, float nextHp, float curDef, float nextDef, float curAtk, float nextAtk)
    {
        this.characterID = characterID;
        this.currentLevel = currentLevel;
        this.maxLevel = maxLevel;
        this.isMaxLevel = currentLevel >= maxLevel;
        this.nextCost = nextCost;
        this.canAfford = canAfford && !this.isMaxLevel;
        this.currentHpBonus = curHp;
        this.nextHpBonus = nextHp;
        this.currentDefBonus = curDef;
        this.nextDefBonus = nextDef;
        this.currentAtkBonus = curAtk;
        this.nextAtkBonus = nextAtk;
    }
}
