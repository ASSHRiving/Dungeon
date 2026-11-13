using System;
using UnityEngine;

[System.Serializable]
public struct MetaCurrencyRewardBreakdown
{
    public int stageReward;
    public int victoryBonus;
    public int killReward;
    public int goldReward;
    public int totalReward;

    public MetaCurrencyRewardBreakdown(int stage, int victory, int kills, int gold, int total)
    {
        stageReward = stage;
        victoryBonus = victory;
        killReward = kills;
        goldReward = gold;
        totalReward = total;
    }
}

/// <summary>
/// 局外養成與局外貨幣 (靈魂) 管理中心
/// 負責保存玩家跨局的總靈魂資產，並根據每局結算表現 (關卡、擊殺、金幣) 計算與發放獎勵。
/// </summary>
public class MetaProgressionManager : SingletonBase<MetaProgressionManager>
{
    public const string PREFS_KEY_SOULS = "MetaProgression_TotalSouls";
    public const string PREFS_KEY_LIFETIME_SOULS = "MetaProgression_LifetimeSouls";

    [Header("貨幣名稱與存量")]
    [Tooltip("局外貨幣的顯示名稱 (可自訂為：靈魂、魂石、深淵晶石等)")]
    [SerializeField] private string currencyDisplayName = "靈魂";
    [SerializeField] private int totalCurrency = 0;
    [SerializeField] private int lifetimeSouls = 0;

    [Header("結算公式參數 (可在 Inspector 自由微調)")]
    [Tooltip("每通關/抵達一個小關卡給予的基礎靈魂 (例如進度 1-3 獲得 3 × 25 = 75)")]
    [SerializeField] private int rewardPerLevel = 25;

    [Tooltip("遠征勝利 (擊敗最終 Boss 獲勝) 額外獲得的大量靈魂")]
    [SerializeField] private int victoryBonus = 100;

    [Tooltip("每擊殺一隻敵人折算獲得的靈魂")]
    [SerializeField] private float rewardPerKill = 2.0f;

    [Tooltip("本局累計金幣換算成靈魂的比例 (0.1 = 每 10 金幣兌換 1 靈魂)")]
    [SerializeField] private float goldConversionRatio = 0.1f;

    [Tooltip("單局保底最低獎勵 (避免剛開局死亡獲得 0 體驗過差)")]
    [SerializeField] private int minReward = 5;

    public string CurrencyDisplayName => currencyDisplayName;
    public int TotalCurrency => totalCurrency;
    public int LifetimeSouls => lifetimeSouls;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    /// <summary>
    /// 從 PlayerPrefs 讀取玩家已儲存的局外貨幣
    /// </summary>
    public void LoadData()
    {
        totalCurrency = PlayerPrefs.GetInt(PREFS_KEY_SOULS, 0);
        lifetimeSouls = PlayerPrefs.GetInt(PREFS_KEY_LIFETIME_SOULS, 0);
    }

    /// <summary>
    /// 儲存當前局外貨幣並廣播事件
    /// </summary>
    public void SaveData()
    {
        PlayerPrefs.SetInt(PREFS_KEY_SOULS, totalCurrency);
        PlayerPrefs.SetInt(PREFS_KEY_LIFETIME_SOULS, lifetimeSouls);
        PlayerPrefs.Save();

        UIEvents.MetaCurrencyChanged(totalCurrency);
    }

    /// <summary>
    /// 增加局外貨幣
    /// </summary>
    public void AddCurrency(int amount)
    {
        if (amount <= 0) return;

        totalCurrency += amount;
        lifetimeSouls += amount;
        SaveData();

        Debug.Log($"[MetaProgression] 獲得 {amount} {currencyDisplayName}！當前持有: {totalCurrency:N0} (歷史累計: {lifetimeSouls:N0})");
    }

    /// <summary>
    /// 檢查是否有足夠的局外貨幣 (供未來的局外天賦/升級系統使用)
    /// </summary>
    public bool HasCurrency(int amount)
    {
        return totalCurrency >= amount;
    }

    /// <summary>
    /// 扣除局外貨幣 (供未來的局外天賦/升級系統使用)
    /// </summary>
    public bool SpendCurrency(int amount)
    {
        if (amount <= 0) return false;

        if (totalCurrency >= amount)
        {
            totalCurrency -= amount;
            SaveData();
            Debug.Log($"[MetaProgression] 花費 {amount} {currencyDisplayName}，剩餘持有: {totalCurrency:N0}");
            return true;
        }

        Debug.LogWarning($"[MetaProgression] {currencyDisplayName}不足！需要: {amount}，目前擁有: {totalCurrency}");
        return false;
    }

    /// <summary>
    /// 重置所有局外貨幣存檔
    /// </summary>
    public void ResetCurrency()
    {
        totalCurrency = 0;
        lifetimeSouls = 0;
        PlayerPrefs.DeleteKey(PREFS_KEY_SOULS);
        PlayerPrefs.DeleteKey(PREFS_KEY_LIFETIME_SOULS);
        PlayerPrefs.Save();

        UIEvents.MetaCurrencyChanged(0);
        Debug.Log($"[MetaProgression] 局外貨幣數據已重置為 0！");
    }

    /// <summary>
    /// 純計算本局數據可獲得的貨幣明細 (不直接寫入存檔，可用於結算前預覽)
    /// </summary>
    public MetaCurrencyRewardBreakdown CalculateReward(int majorStage, int level, bool isVictory, int killCount, int goldCount)
    {
        // 1. 關卡進度獎勵 (以目前小關數為主，每關給予 rewardPerLevel)
        int stageReward = Mathf.Max(1, level) * rewardPerLevel;

        // 2. 通關勝利獎勵 (擊敗 Boss)
        int vicReward = isVictory ? victoryBonus : 0;

        // 3. 殺敵數換算 (killCount × rewardPerKill)
        int killReward = Mathf.RoundToInt(Mathf.Max(0, killCount) * rewardPerKill);

        // 4. 金幣折算 (goldCount × goldConversionRatio)
        int goldReward = Mathf.FloorToInt(Mathf.Max(0, goldCount) * goldConversionRatio);

        // 5. 總計與保底
        int total = stageReward + vicReward + killReward + goldReward;
        if (total < minReward)
        {
            total = minReward;
        }

        return new MetaCurrencyRewardBreakdown(stageReward, vicReward, killReward, goldReward, total);
    }

    /// <summary>
    /// 執行本局遊戲結算：計算數值並直接將貨幣加進總額存檔
    /// </summary>
    public MetaCurrencyRewardBreakdown ProcessRunSettlement(int majorStage, int level, bool isVictory, int killCount, int goldCount)
    {
        var breakdown = CalculateReward(majorStage, level, isVictory, killCount, goldCount);
        AddCurrency(breakdown.totalReward);
        return breakdown;
    }

    #region 編輯器右鍵測試輔助
    [ContextMenu("測試：增加 100 靈魂")]
    private void TestAdd100() => AddCurrency(100);

    [ContextMenu("測試：增加 1000 靈魂")]
    private void TestAdd1000() => AddCurrency(1000);

    [ContextMenu("測試：重置所有局外貨幣")]
    private void TestReset() => ResetCurrency();
    #endregion
}
