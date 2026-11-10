using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色/怪物核心數值大腦 (中央屬性管理器)
/// 統一負責管理所有屬性基礎值、修正器 (裝備/天賦/Buff) 以及生命值即時狀態
/// </summary>
public class CharacterStats : MonoBehaviour
{
    [Header("核心屬性容器 (Base & Modifiers)")]
    [SerializeField] private CharacterStat statMaxHealth = new CharacterStat(100f);
    [SerializeField] private CharacterStat statShield = new CharacterStat(10f);
    [SerializeField] private CharacterStat statDamage = new CharacterStat(15f);
    [SerializeField] private CharacterStat statCritRate = new CharacterStat(5f);
    [SerializeField] private CharacterStat statMoveSpeed = new CharacterStat(5f);
    [SerializeField] private CharacterStat statPoise = new CharacterStat(100f);

    [Header("即時動態狀態")]
    [SerializeField] private float currentHealth;
    public bool isDead => currentHealth <= 0f;

    [Header("向下相容參考 (舊版組件 fallback)")]
    [SerializeField] private CharacterCombatBase combat;
    [SerializeField] private CharacterHealthBase health;

    private bool isInitialized = false;

    #region 公開屬性存取介面
    public float MaxHealth => statMaxHealth.Value;
    public float CurrentHealth => currentHealth;
    public float Shield => statShield.Value;
    public float AttackDamage => statDamage.Value;
    public float CritRate => statCritRate.Value;
    public float MoveSpeed => statMoveSpeed.Value;
    public float Poise => statPoise.Value;

    // 向下相容舊代碼 (例如 GameManager / PlayerStatsController 原有的欄位存取)
    public float maxHealth => MaxHealth;
    public float shield => Shield;
    public float damage => AttackDamage;
    public float critRate => CritRate;
    #endregion

    #region 事件廣播
    public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
    public event Action OnStatsChanged;                // 任一屬性更新時廣播
    #endregion

    protected virtual void Awake()
    {
        if (combat == null) combat = GetComponentInChildren<CharacterCombatBase>();
        if (health == null) health = GetComponentInChildren<CharacterHealthBase>();

        if (!isInitialized)
        {
            AutoFallbackInit();
        }
    }

    /// <summary>
    /// 自動向後相容初始化：若未透過 SO 初始化，則從現有的 Health/Combat 提取基準值
    /// </summary>
    private void AutoFallbackInit()
    {
        float hp = health != null ? health.maxHealth : 100f;
        float def = health != null ? health.shield : 10f;
        float atk = (combat != null && combat.damage > 0f) ? combat.damage : statDamage.BaseValue;
        float crit = (combat != null && combat.critRate > 0f) ? combat.critRate : statCritRate.BaseValue;

        InitBaseStats(hp, def, atk, crit);
    }

    /// <summary>
    /// 直接透過數值手動初始化
    /// </summary>
    public void InitBaseStats(float hp, float def, float atk, float crit = 5f, float spd = 5f, float poise = 100f)
    {
        statMaxHealth.BaseValue = hp;
        statShield.BaseValue = def;
        statDamage.BaseValue = atk;
        statCritRate.BaseValue = crit;
        statMoveSpeed.BaseValue = spd;
        statPoise.BaseValue = poise;

        currentHealth = MaxHealth;
        isInitialized = true;

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 從玩家職業配置 ScriptableObject 初始化
    /// </summary>
    public void InitFromData(CharacterDataSO data)
    {
        if (data == null) return;

        statMaxHealth.BaseValue = data.baseMaxHealth;
        statShield.BaseValue = data.baseShield;
        statDamage.BaseValue = data.baseAttackDamage;
        statCritRate.BaseValue = data.baseCritRate;
        statMoveSpeed.BaseValue = data.baseMoveSpeed;
        statPoise.BaseValue = data.basePoise;

        currentHealth = MaxHealth;
        isInitialized = true;

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 從怪物配置 ScriptableObject 與關卡難度縮放初始化
    /// </summary>
    public void InitFromData(EnemyDataSO data, int currentLevel = 1, bool isElite = false)
    {
        if (data == null) return;

        float hp = data.CalculateMaxHealth(currentLevel, isElite);
        float dmg = data.CalculateDamage(currentLevel, isElite);

        statMaxHealth.BaseValue = hp;
        statShield.BaseValue = data.baseShield;
        statDamage.BaseValue = dmg;
        statCritRate.BaseValue = 0f;
        statMoveSpeed.BaseValue = data.baseMoveSpeed;
        statPoise.BaseValue = data.basePoise;

        currentHealth = MaxHealth;
        isInitialized = true;

        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        OnStatsChanged?.Invoke();
    }

    #region 屬性修正器 (裝備/天賦/Buff)
    /// <summary>
    /// 掛載一個屬性修正器
    /// </summary>
    public void AddModifier(StatModifier mod)
    {
        if (mod == null) return;

        float oldMax = MaxHealth;
        GetStatByType(mod.statType)?.AddModifier(mod);

        // 若提升了最大生命上限，同步擴充當前生命 (避免穿上裝備時呈現扣血/未滿狀態)
        if (mod.statType == StatType.MaxHealth)
        {
            float diff = MaxHealth - oldMax;
            if (oldMax <= 0f)
            {
                currentHealth = MaxHealth;
            }
            else if (diff > 0f)
            {
                currentHealth = Mathf.Min(MaxHealth, currentHealth + diff);
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 1f, MaxHealth);
            }
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// 移除特定的屬性修正器
    /// </summary>
    public bool RemoveModifier(StatModifier mod)
    {
        if (mod == null) return false;

        bool removed = GetStatByType(mod.statType)?.RemoveModifier(mod) ?? false;
        if (removed)
        {
            if (mod.statType == StatType.MaxHealth)
            {
                currentHealth = Mathf.Clamp(currentHealth, 1f, MaxHealth);
                OnHealthChanged?.Invoke(currentHealth, MaxHealth);
            }
            OnStatsChanged?.Invoke();
        }
        return removed;
    }

    /// <summary>
    /// 移除來自特定來源 (如某件特定裝備、某個 Buff 實體) 的所有修正器
    /// </summary>
    public void RemoveAllModifiersFromSource(object source)
    {
        if (source == null) return;

        bool hpAffected = statMaxHealth.RemoveAllModifiersFromSource(source);
        statShield.RemoveAllModifiersFromSource(source);
        statDamage.RemoveAllModifiersFromSource(source);
        statCritRate.RemoveAllModifiersFromSource(source);
        statMoveSpeed.RemoveAllModifiersFromSource(source);
        statPoise.RemoveAllModifiersFromSource(source);

        if (hpAffected)
        {
            currentHealth = Mathf.Clamp(currentHealth, 1f, MaxHealth);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        OnStatsChanged?.Invoke();
    }

    public CharacterStat GetStatByType(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHealth: return statMaxHealth;
            case StatType.Shield: return statShield;
            case StatType.AttackDamage: return statDamage;
            case StatType.CritRate: return statCritRate;
            case StatType.MoveSpeed: return statMoveSpeed;
            case StatType.Poise: return statPoise;
            default: return null;
        }
    }
    #endregion

    #region 生命值管理方法
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void SetCurrentHealth(float amount)
    {
        currentHealth = Mathf.Clamp(amount, 0f, MaxHealth);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }
    #endregion
}
