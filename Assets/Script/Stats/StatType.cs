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
    HitStunReduction    // 縮短受擊硬直 (%) (8)
}

public enum StatModType
{
    Flat = 100,        // 固定值加成 (例如裝備給予 +20 攻擊力)
    PercentAdd = 200,  // 百分比加算 (例如天賦 +10% 與藥水 +15% 累加為 +25%)
    PercentMult = 300  // 百分比乘算 (例如狂暴狀態乘算 1.2 倍)
}
