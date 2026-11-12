using System;
using UnityEngine;

/// <summary>
/// 角色局外養成升級管理器 (三圍同步一體化升級架構)
/// 負責儲存與管理每個角色的統一強化等級，一次升級同步提升生命、防禦與攻擊，
/// 對接 MetaProgressionManager 扣除靈魂貨幣，並在進入戰鬥時將數值注入角色身上的 CharacterStats 大腦。
/// </summary>
public class CharacterUpgradeManager : SingletonBase<CharacterUpgradeManager>
{
    private const string PREFS_LEVEL_KEY = "MetaUpgrade_Char_{0}_Level";

    [Header("數值與消耗配置")]
    [SerializeField] private CharacterUpgradeConfigSO upgradeConfig;

    #region 事件廣播
    /// <summary>
    /// 當特定角色的強化等級提升時廣播 (characterID, newLevel)
    /// </summary>
    public static event Action<int, int> OnCharacterLevelUpgraded;

    /// <summary>
    /// 任一升級發生時廣播 (供 UI 刷新全部按鈕狀態與數值)
    /// </summary>
    public static event Action OnAnyUpgradeChanged;

    /// <summary>
    /// 供外部或測試工具手動觸發全局養成刷新廣播
    /// </summary>
    public static void TriggerAnyUpgradeChanged() => OnAnyUpgradeChanged?.Invoke();
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

    #region CharacterDataSO 專用介面 (推薦使用，無須在外部傳遞 ID)
    public int GetCharacterLevel(CharacterDataSO characterData) => characterData != null ? GetCharacterLevel(characterData.characterID) : 0;
    public int GetNextUpgradeCost(CharacterDataSO characterData) => characterData != null ? GetNextUpgradeCost(characterData.characterID) : 0;
    public float GetStatBonus(CharacterDataSO characterData, UpgradeType type) => characterData != null ? GetStatBonus(characterData.characterID, type) : 0f;
    public CharacterUpgradeSnapshot GetSnapshot(CharacterDataSO characterData) => characterData != null ? GetSnapshot(characterData.characterID) : default;
    public bool CanUpgrade(CharacterDataSO characterData) => characterData != null && CanUpgrade(characterData.characterID);
    public bool TryUpgrade(CharacterDataSO characterData) => characterData != null && TryUpgrade(characterData.characterID);
    public void ApplyUpgradesToPlayer(GameObject playerGo, CharacterDataSO characterData)
    {
        if (playerGo == null || characterData == null) return;
        ApplyUpgradesToPlayer(playerGo, characterData.characterID);
    }
    public void ResetCharacterUpgrades(CharacterDataSO characterData)
    {
        if (characterData == null) return;
        ResetCharacterUpgrades(characterData.characterID);
    }
    #endregion

    /// <summary>
    /// 獲取指定角色的當前統一強化等級 (自動相容舊版分開等級存檔)
    /// </summary>
    public int GetCharacterLevel(int characterID)
    {
        string key = string.Format(PREFS_LEVEL_KEY, characterID);
        if (PlayerPrefs.HasKey(key))
        {
            return PlayerPrefs.GetInt(key, 0);
        }

        // 舊存檔相容遷移：若有舊版 Health/Defense/Attack 等級，取最高值作為初始階級
        int legacyHp = PlayerPrefs.GetInt($"MetaUpgrade_{characterID}_Health", 0);
        int legacyDef = PlayerPrefs.GetInt($"MetaUpgrade_{characterID}_Defense", 0);
        int legacyAtk = PlayerPrefs.GetInt($"MetaUpgrade_{characterID}_Attack", 0);
        int legacyLevel = Mathf.Max(legacyHp, legacyDef, legacyAtk);

        if (legacyLevel > 0)
        {
            PlayerPrefs.SetInt(key, legacyLevel);
            PlayerPrefs.Save();
            return legacyLevel;
        }

        return 0;
    }

    /// <summary>
    /// 獲取指定角色升至下一級所需的靈魂消耗 (若已達上限則傳回 0)
    /// </summary>
    public int GetNextUpgradeCost(int characterID)
    {
        if (upgradeConfig == null) return 0;
        int curLevel = GetCharacterLevel(characterID);
        return upgradeConfig.GetCostForLevel(curLevel);
    }

    /// <summary>
    /// 獲取指定角色指定屬性目前的累積數值加成 (由統一等級計算)
    /// </summary>
    public float GetStatBonus(int characterID, UpgradeType type)
    {
        if (upgradeConfig == null) return 0f;
        int curLevel = GetCharacterLevel(characterID);
        return upgradeConfig.GetStatBonus(type, curLevel);
    }

    /// <summary>
    /// 獲取指定角色完整的綜合升級快照 (供 UI 直接資料綁定)
    /// </summary>
    public CharacterUpgradeSnapshot GetSnapshot(int characterID)
    {
        int curLevel = GetCharacterLevel(characterID);
        int maxLvl = upgradeConfig != null ? upgradeConfig.maxLevel : 10;
        int cost = upgradeConfig != null ? upgradeConfig.GetCostForLevel(curLevel) : 0;
        bool canAfford = MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.HasCurrency(cost);

        float curHp = upgradeConfig != null ? upgradeConfig.GetStatBonus(UpgradeType.Health, curLevel) : 0f;
        float nextHp = upgradeConfig != null ? upgradeConfig.GetStatBonus(UpgradeType.Health, curLevel + 1) : 0f;

        float curDef = upgradeConfig != null ? upgradeConfig.GetStatBonus(UpgradeType.Defense, curLevel) : 0f;
        float nextDef = upgradeConfig != null ? upgradeConfig.GetStatBonus(UpgradeType.Defense, curLevel + 1) : 0f;

        float curAtk = upgradeConfig != null ? upgradeConfig.GetStatBonus(UpgradeType.Attack, curLevel) : 0f;
        float nextAtk = upgradeConfig != null ? upgradeConfig.GetStatBonus(UpgradeType.Attack, curLevel + 1) : 0f;

        return new CharacterUpgradeSnapshot(
            characterID,
            curLevel,
            maxLvl,
            cost,
            canAfford,
            curHp, nextHp,
            curDef, nextDef,
            curAtk, nextAtk
        );
    }

    /// <summary>
    /// 檢查是否可進行角色升級 (等級未滿且靈魂足夠)
    /// </summary>
    public bool CanUpgrade(int characterID)
    {
        if (upgradeConfig == null) return false;
        int curLevel = GetCharacterLevel(characterID);
        if (curLevel >= upgradeConfig.maxLevel) return false;

        int cost = upgradeConfig.GetCostForLevel(curLevel);
        return MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.HasCurrency(cost);
    }

    /// <summary>
    /// 執行角色升級：扣除靈魂貨幣、提升等級、同時強化三圍屬性並發送廣播
    /// </summary>
    public bool TryUpgrade(int characterID)
    {
        if (!CanUpgrade(characterID))
        {
            return false;
        }

        int cost = GetNextUpgradeCost(characterID);
        bool spendSuccess = MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.SpendCurrency(cost);
        if (!spendSuccess) return false;

        int newLevel = GetCharacterLevel(characterID) + 1;
        string key = string.Format(PREFS_LEVEL_KEY, characterID);
        PlayerPrefs.SetInt(key, newLevel);
        PlayerPrefs.Save();

        Debug.Log($"[CharacterUpgrade] 角色 [{characterID}] 成功升級至 Lv.{newLevel}！(消耗 {cost} 靈魂，生命/防禦/攻擊同步提升)");

        OnCharacterLevelUpgraded?.Invoke(characterID, newLevel);
        OnAnyUpgradeChanged?.Invoke();

        return true;
    }

    #region 舊版 API 相容轉發 (確保舊程式碼調用不報錯)
    public int GetUpgradeLevel(int characterID, UpgradeType type) => GetCharacterLevel(characterID);
    public int GetUpgradeCost(int characterID, UpgradeType type) => GetNextUpgradeCost(characterID);
    public bool CanUpgrade(int characterID, UpgradeType type) => CanUpgrade(characterID);
    public bool TryUpgrade(int characterID, UpgradeType type) => TryUpgrade(characterID);

    public UpgradeInfo GetUpgradeInfo(int characterID, UpgradeType type)
    {
        int curLevel = GetCharacterLevel(characterID);
        int maxLvl = upgradeConfig != null ? upgradeConfig.maxLevel : 10;
        int cost = upgradeConfig != null ? upgradeConfig.GetCostForLevel(curLevel) : 0;
        float curBonus = upgradeConfig != null ? upgradeConfig.GetStatBonus(type, curLevel) : 0f;
        float nextBonus = upgradeConfig != null ? upgradeConfig.GetStatBonus(type, curLevel + 1) : 0f;
        bool canAfford = MetaProgressionManager.Instance != null && MetaProgressionManager.Instance.HasCurrency(cost);
        string name = (type == UpgradeType.Health) ? "生命上限" : (type == UpgradeType.Defense ? "防禦護甲" : "基礎攻擊");

        return new UpgradeInfo(type, name, curLevel, maxLvl, cost, curBonus, nextBonus, canAfford);
    }
    #endregion

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

        Debug.Log($"[CharacterUpgrade] 已套用三圍局外養成加成至角色 [ID:{characterID}] -> 生命+{hpBonus}, 防禦+{defBonus}, 攻擊+{atkBonus}");
    }

    /// <summary>
    /// 重置指定角色的升級等級
    /// </summary>
    public void ResetCharacterUpgrades(int characterID)
    {
        PlayerPrefs.DeleteKey(string.Format(PREFS_LEVEL_KEY, characterID));
        PlayerPrefs.DeleteKey($"MetaUpgrade_{characterID}_Health");
        PlayerPrefs.DeleteKey($"MetaUpgrade_{characterID}_Defense");
        PlayerPrefs.DeleteKey($"MetaUpgrade_{characterID}_Attack");
        PlayerPrefs.Save();

        OnAnyUpgradeChanged?.Invoke();
        Debug.Log($"[CharacterUpgrade] 角色 [{characterID}] 的局外養成等級已重置為 0！");
    }

    #region 編輯器測試輔助
    [ContextMenu("測試：重置角色 0 (戰士) 升級等級")]
    private void TestResetChar0() => ResetCharacterUpgrades(0);

    [ContextMenu("測試：重置所有角色升級等級")]
    private void TestResetAllUpgrades()
    {
        for (int i = 0; i < 10; i++)
        {
            ResetCharacterUpgrades(i);
        }
        PlayerPrefs.Save();
        OnAnyUpgradeChanged?.Invoke();
        Debug.Log("[CharacterUpgrade] 所有角色的局外養成等級已重置為 0！");
    }
    #endregion
}
