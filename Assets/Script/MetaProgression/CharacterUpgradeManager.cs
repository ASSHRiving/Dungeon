using System;
using UnityEngine;

/// <summary>
/// 角色局外養成升級管理器
/// 負責儲存與管理每個角色的生命、防禦、攻擊升級等級，
/// 對接 MetaProgressionManager 扣除靈魂貨幣，並在進入戰鬥時將數值注入角色身上的 CharacterStats 大腦。
/// </summary>
public class CharacterUpgradeManager : SingletonBase<CharacterUpgradeManager>
{
    private const string PREFS_PREFIX = "MetaUpgrade_";

    [Header("數值與消耗配置")]
    [SerializeField] private CharacterUpgradeConfigSO upgradeConfig;

    #region 事件廣播
    /// <summary>
    /// 當特定角色的特定屬性升級時廣播 (characterID, upgradeType, newLevel)
    /// </summary>
    public static event Action<int, UpgradeType, int> OnCharacterUpgradeChanged;

    /// <summary>
    /// 任一升級發生時廣播 (供 UI 刷新全部按鈕狀態與數值)
    /// </summary>
    public static event Action OnAnyUpgradeChanged;
    #endregion

    public CharacterUpgradeConfigSO Config => upgradeConfig;

    protected override void Awake()
    {
        base.Awake();
        if (upgradeConfig == null)
        {
            // 嘗試自動載入預設資產
            upgradeConfig = Resources.Load<CharacterUpgradeConfigSO>("CharacterUpgradeConfig");
            if (upgradeConfig == null)
            {
                // 若無資產則動態生成預設實例
                upgradeConfig = ScriptableObject.CreateInstance<CharacterUpgradeConfigSO>();
            }
        }
    }

    /// <summary>
    /// 獲取指定角色指定屬性的當前升級等級
    /// </summary>
    public int GetUpgradeLevel(int characterID, UpgradeType type)
    {
        string key = GetPrefsKey(characterID, type);
        return PlayerPrefs.GetInt(key, 0);
    }

    /// <summary>
    /// 獲取指定角色指定屬性升至下一級所需的靈魂消耗 (若已達上限則傳回 0)
    /// </summary>
    public int GetUpgradeCost(int characterID, UpgradeType type)
    {
        int curLevel = GetUpgradeLevel(characterID, type);
        var setting = upgradeConfig.GetSetting(type);
        return setting.GetCostForLevel(curLevel);
    }

    /// <summary>
    /// 獲取指定角色指定屬性目前的累積數值加成
    /// </summary>
    public float GetStatBonus(int characterID, UpgradeType type)
    {
        int curLevel = GetUpgradeLevel(characterID, type);
        var setting = upgradeConfig.GetSetting(type);
        return setting.GetTotalBonus(curLevel);
    }

    /// <summary>
    /// 獲取指定屬性完整的即時資訊包 (供 UI 直接資料綁定)
    /// </summary>
    public UpgradeInfo GetUpgradeInfo(int characterID, UpgradeType type)
    {
        var setting = upgradeConfig.GetSetting(type);
        int curLevel = GetUpgradeLevel(characterID, type);
        int cost = setting.GetCostForLevel(curLevel);
        float curBonus = setting.GetTotalBonus(curLevel);
        float nextBonus = setting.GetTotalBonus(curLevel + 1);
        bool canAfford = MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.HasCurrency(cost);

        return new UpgradeInfo(
            type,
            setting.displayName,
            curLevel,
            setting.maxLevel,
            cost,
            curBonus,
            nextBonus,
            canAfford
        );
    }

    /// <summary>
    /// 檢查是否可進行升級 (等級未滿且靈魂足夠)
    /// </summary>
    public bool CanUpgrade(int characterID, UpgradeType type)
    {
        var setting = upgradeConfig.GetSetting(type);
        int curLevel = GetUpgradeLevel(characterID, type);
        if (curLevel >= setting.maxLevel) return false;

        int cost = setting.GetCostForLevel(curLevel);
        return MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.HasCurrency(cost);
    }

    /// <summary>
    /// 執行角色屬性升級：扣除靈魂貨幣、提升等級、儲存存檔並發送廣播
    /// </summary>
    public bool TryUpgrade(int characterID, UpgradeType type)
    {
        if (!CanUpgrade(characterID, type))
        {
            return false;
        }

        int cost = GetUpgradeCost(characterID, type);
        bool spendSuccess = MetaProgressionManager.Instance.SpendCurrency(cost);
        if (!spendSuccess) return false;

        int newLevel = GetUpgradeLevel(characterID, type) + 1;
        string key = GetPrefsKey(characterID, type);
        PlayerPrefs.SetInt(key, newLevel);
        PlayerPrefs.Save();

        Debug.Log($"[CharacterUpgrade] 角色 [{characterID}] 的 {type} 升級至 Lv.{newLevel}！(消耗 {cost} 靈魂)");

        OnCharacterUpgradeChanged?.Invoke(characterID, type, newLevel);
        OnAnyUpgradeChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 將該角色的局外屬性加成注入到遊戲內的 CharacterStats (供 StartRoom 或角色生成時呼叫)
    /// </summary>
    public void ApplyUpgradesToPlayer(GameObject playerGo, int characterID)
    {
        if (playerGo == null) return;

        CharacterStats stats = playerGo.GetComponent<CharacterStats>();
        if (stats == null)
        {
            stats = playerGo.AddComponent<CharacterStats>();
        }

        // 先清除既有的局外天賦加成 (避免切換關卡或重複呼叫時疊加)
        stats.RemoveAllModifiersFromSource("MetaUpgrade");

        float hpBonus = GetStatBonus(characterID, UpgradeType.Health);
        float defBonus = GetStatBonus(characterID, UpgradeType.Defense);
        float atkBonus = GetStatBonus(characterID, UpgradeType.Attack);

        if (hpBonus > 0f)
        {
            stats.AddModifier(new StatModifier(StatType.MaxHealth, StatModType.Flat, hpBonus, "MetaUpgrade"));
        }
        if (defBonus > 0f)
        {
            stats.AddModifier(new StatModifier(StatType.Shield, StatModType.Flat, defBonus, "MetaUpgrade"));
        }
        if (atkBonus > 0f)
        {
            stats.AddModifier(new StatModifier(StatType.AttackDamage, StatModType.Flat, atkBonus, "MetaUpgrade"));
        }

        Debug.Log($"[CharacterUpgrade] 已套用局外養成屬性至角色 [ID:{characterID}] -> 生命+{hpBonus}, 防禦+{defBonus}, 攻擊+{atkBonus}");
    }

    /// <summary>
    /// 重置指定角色的升級等級
    /// </summary>
    public void ResetCharacterUpgrades(int characterID)
    {
        PlayerPrefs.DeleteKey(GetPrefsKey(characterID, UpgradeType.Health));
        PlayerPrefs.DeleteKey(GetPrefsKey(characterID, UpgradeType.Defense));
        PlayerPrefs.DeleteKey(GetPrefsKey(characterID, UpgradeType.Attack));
        PlayerPrefs.Save();

        OnAnyUpgradeChanged?.Invoke();
        Debug.Log($"[CharacterUpgrade] 角色 [{characterID}] 的局外養成等級已重置為 0！");
    }

    private string GetPrefsKey(int characterID, UpgradeType type)
    {
        return $"{PREFS_PREFIX}{characterID}_{type}";
    }

    #region 編輯器右鍵測試輔助
    [ContextMenu("測試：重置角色 0 (戰士) 升級等級")]
    private void TestResetChar0() => ResetCharacterUpgrades(0);

    [ContextMenu("測試：重置所有角色升級等級")]
    private void TestResetAllUpgrades()
    {
        for (int i = 0; i < 10; i++)
        {
            PlayerPrefs.DeleteKey(GetPrefsKey(i, UpgradeType.Health));
            PlayerPrefs.DeleteKey(GetPrefsKey(i, UpgradeType.Defense));
            PlayerPrefs.DeleteKey(GetPrefsKey(i, UpgradeType.Attack));
        }
        PlayerPrefs.Save();
        OnAnyUpgradeChanged?.Invoke();
        Debug.Log("[CharacterUpgrade] 所有角色的局外養成等級已重置為 0！");
    }
    #endregion
}
