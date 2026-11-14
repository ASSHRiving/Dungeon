using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 寶箱物品條目：定義掉落物 Prefab、權重與可選的指定裝備階級
/// </summary>
[System.Serializable]
public class ChestRewardEntry
{
    [Tooltip("物品名稱（供 Inspector 檢視識別）")]
    public string itemName;

    [Tooltip("掉落物 Prefab（例如防具、藥水、武器）")]
    public GameObject itemPrefab;

    [Min(0f), Tooltip("隨機抽樣權重 (Weight)")]
    public float weight = 1f;

    [Tooltip("指定裝備階級 (-1: 依關卡機率隨機, 0: 初級 T1, 1: 中級 T2, 2: 高級 T3)")]
    public int targetTier = -1;
}

/// <summary>
/// 各關卡層級的寶箱獎勵設定
/// </summary>
[System.Serializable]
public class LevelChestPool
{
    [Tooltip("關卡層數 (Level 1, 2, 3...)")]
    public int level = 1;

    [Header("金幣掉落範圍")]
    [Min(0)] public int minGold = 30;
    [Min(0)] public int maxGold = 30;

    [Header("物品掉落設定")]
    [Range(0f, 1f), Tooltip("觸發隨機掉落的機率 (1 = 必掉)")]
    public float itemDropChance = 1f;

    [Min(0), Tooltip("觸發時從隨機物品池抽取的件數")]
    public int randomDropCount = 1;

    [Tooltip("抽取多件時是否允許重複抽到同一條目")]
    public bool allowDuplicateDrops = false;

    [Tooltip("固定額外必掉物品（不受機率與抽取件數限制）")]
    public List<ChestRewardEntry> guaranteedItems = new List<ChestRewardEntry>();

    [Tooltip("該層數的隨機物品抽樣池")]
    public List<ChestRewardEntry> items = new List<ChestRewardEntry>();

    [Tooltip("該層數的裝備階級機率配置（T1 初級 / T2 中級 / T3 高級）")]
    public EquipmentTierChances tierChances = new EquipmentTierChances(0f, 50f, 50f);
}

/// <summary>
/// 寶箱獎勵池 ScriptableObject：支援依關卡調整金幣數量、必掉物、機率隨機抽樣與裝備階級權重
/// </summary>
[CreateAssetMenu(fileName = "ChestPool_", menuName = "Dungeon/Chest/Chest Reward Pool")]
public class ChestRewardPoolSO : ScriptableObject
{
    [Tooltip("獎勵池名稱或描述")]
    public string poolName;

    [Header("預設獎勵配置（無匹配關卡時使用）")]
    [Min(0)] public int defaultMinGold = 30;
    [Min(0)] public int defaultMaxGold = 30;
    [Range(0f, 1f)] public float defaultItemDropChance = 1f;
    [Min(0)] public int defaultRandomDropCount = 1;
    public bool defaultAllowDuplicateDrops = false;
    public List<ChestRewardEntry> defaultGuaranteedItems = new List<ChestRewardEntry>();
    public List<ChestRewardEntry> defaultItems = new List<ChestRewardEntry>();
    public EquipmentTierChances defaultTierChances = new EquipmentTierChances(0f, 50f, 50f);

    [Header("各關卡專屬獎勵配置（依關卡優先使用）")]
    public List<LevelChestPool> levelPools = new List<LevelChestPool>();

    /// <summary>
    /// 取得符合指定關卡層數的獎勵設定；若無則回傳 null
    /// </summary>
    public LevelChestPool GetLevelPool(int currentLevel)
    {
        if (levelPools == null) return null;

        for (int i = 0; i < levelPools.Count; i++)
        {
            if (levelPools[i] != null && levelPools[i].level == currentLevel)
            {
                return levelPools[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 依關卡層數抽取本次寶箱掉落的金幣數量
    /// </summary>
    public int RollGold(int currentLevel)
    {
        LevelChestPool levelPool = GetLevelPool(currentLevel);
        int min = levelPool != null ? levelPool.minGold : defaultMinGold;
        int max = levelPool != null ? levelPool.maxGold : defaultMaxGold;

        if (max < min) max = min;
        return Random.Range(Mathf.Max(0, min), Mathf.Max(0, max) + 1);
    }

    /// <summary>
    /// 依關卡層數隨機抽取裝備階級 (0: T1, 1: T2, 2: T3)
    /// </summary>
    public int RollEquipmentTier(int currentLevel)
    {
        LevelChestPool levelPool = GetLevelPool(currentLevel);
        EquipmentTierChances chances = (levelPool != null && levelPool.tierChances != null)
            ? levelPool.tierChances
            : defaultTierChances;

        return chances != null ? chances.RollTier() : 1;
    }

    /// <summary>
    /// 依關卡層數抽取本次寶箱應掉落的物品清單（包含固定必掉與隨機抽樣）
    /// </summary>
    public List<ChestRewardEntry> RollDrops(int currentLevel)
    {
        List<ChestRewardEntry> results = new List<ChestRewardEntry>();
        LevelChestPool levelPool = GetLevelPool(currentLevel);

        List<ChestRewardEntry> guaranteed = levelPool != null ? levelPool.guaranteedItems : defaultGuaranteedItems;
        List<ChestRewardEntry> randomPool = levelPool != null ? levelPool.items : defaultItems;
        float dropChance = levelPool != null ? levelPool.itemDropChance : defaultItemDropChance;
        int dropCount = levelPool != null ? levelPool.randomDropCount : defaultRandomDropCount;
        bool allowDuplicates = levelPool != null ? levelPool.allowDuplicateDrops : defaultAllowDuplicateDrops;

        if (guaranteed != null)
        {
            for (int i = 0; i < guaranteed.Count; i++)
            {
                if (guaranteed[i] != null && guaranteed[i].itemPrefab != null)
                {
                    results.Add(guaranteed[i]);
                }
            }
        }

        if (dropCount <= 0 || randomPool == null || randomPool.Count == 0 || Random.value > dropChance)
        {
            return results;
        }

        List<ChestRewardEntry> candidates = new List<ChestRewardEntry>(randomPool.Count);
        for (int i = 0; i < randomPool.Count; i++)
        {
            if (randomPool[i] != null && randomPool[i].itemPrefab != null)
            {
                candidates.Add(randomPool[i]);
            }
        }

        for (int i = 0; i < dropCount && candidates.Count > 0; i++)
        {
            ChestRewardEntry picked = SelectWeightedEntry(candidates);
            if (picked == null) break;

            results.Add(picked);
            if (!allowDuplicates)
            {
                candidates.Remove(picked);
            }
        }

        return results;
    }

    private static ChestRewardEntry SelectWeightedEntry(List<ChestRewardEntry> pool)
    {
        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            totalWeight += Mathf.Max(0f, pool[i].weight);
        }

        if (totalWeight <= 0f)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < pool.Count; i++)
        {
            float w = Mathf.Max(0f, pool[i].weight);
            if (w <= 0f) continue;

            roll -= w;
            if (roll <= 0f)
            {
                return pool[i];
            }
        }

        return pool[pool.Count - 1];
    }
}
