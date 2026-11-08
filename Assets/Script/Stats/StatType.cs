public enum StatType
{
    MaxHealth,      // 最大生命值
    Shield,         // 護甲 / 護盾 (減傷)
    AttackDamage,   // 攻擊力
    CritRate,       // 暴擊率 (%)
    MoveSpeed,      // 移動速度
    Poise           // 韌性值
}

public enum StatModType
{
    Flat = 100,        // 固定值加成 (例如裝備給予 +20 攻擊力)
    PercentAdd = 200,  // 百分比加算 (例如天賦 +10% 與藥水 +15% 累加為 +25%)
    PercentMult = 300  // 百分比乘算 (例如狂暴狀態乘算 1.2 倍)
}
