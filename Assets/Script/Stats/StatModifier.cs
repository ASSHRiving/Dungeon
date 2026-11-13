using System;

/// <summary>
/// 屬性修正器：用於裝備、藥水 Buff、局外天賦、關卡難度加成等
/// </summary>
[Serializable]
public class StatModifier
{
    public readonly StatType statType;
    public readonly StatModType modType;
    public readonly float value;
    public readonly int order;
    public readonly object source; // 記錄來源 (例如哪個裝備實體、Buff 名稱、局外天賦)，方便卸下裝備時一次清除

    public StatModifier(StatType statType, StatModType modType, float value, object source = null)
        : this(statType, modType, value, (int)modType, source)
    {
    }

    public StatModifier(StatType statType, StatModType modType, float value, int order, object source)
    {
        this.statType = statType;
        this.modType = modType;
        this.value = value;
        this.order = order;
        this.source = source;
    }
}
