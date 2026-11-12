using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 裝備階級出現權重配置 (T1 初級, T2 中級, T3 高級)
/// </summary>
[System.Serializable]
public class EquipmentTierChances
{
    [Range(0f, 100f), Tooltip("T1 (初級) 出現權重")]
    public float t1Weight = 0f;

    [Range(0f, 100f), Tooltip("T2 (中級) 出現權重")]
    public float t2Weight = 50f;

    [Range(0f, 100f), Tooltip("T3 (高級) 出現權重")]
    public float t3Weight = 50f;

    public EquipmentTierChances() { }

    public EquipmentTierChances(float t1, float t2, float t3)
    {
        t1Weight = t1;
        t2Weight = t2;
        t3Weight = t3;
    }

    /// <summary>
    /// 依權重隨機抽取階級索引 (0: T1, 1: T2, 2: T3)
    /// </summary>
    public int RollTier()
    {
        float w1 = Mathf.Max(0f, t1Weight);
        float w2 = Mathf.Max(0f, t2Weight);
        float w3 = Mathf.Max(0f, t3Weight);
        float total = w1 + w2 + w3;

        if (total <= 0f) return 1; // 預設中級 T2

        float r = Random.Range(0f, total);
        if (r < w1) return 0;
        if (r < w1 + w2) return 1;
        return 2;
    }
}

/// <summary>
/// 商店物品條目：定義物品 Prefab、權重、可選的指定階級與可選的覆寫價格
/// </summary>
[System.Serializable]
public class ShopItemEntry
{
    [Tooltip("商品名稱（供 Inspector 檢視識別）")]
    public string itemName;

    [Tooltip("生成物體的 Prefab（例如防具、武器、藥水）")]
    public GameObject itemPrefab;

    [Min(0f), Tooltip("隨機抽樣權重 (Weight)")]
    public float weight = 1f;

    [Tooltip("指定裝備階級 (-1: 依關卡機率隨機, 0: 初級 T1, 1: 中級 T2, 2: 高級 T3)")]
    public int targetTier = -1;

    [Tooltip("覆寫價格（若小於 0 則使用 Prefab/SO 預設價格）")]
    public int overridePrice = -1;

    public ShopItemEntry() { }

    public ShopItemEntry(GameObject prefab, float weight = 1f, int targetTier = -1, int overridePrice = -1, string name = "")
    {
        this.itemPrefab = prefab;
        this.weight = weight;
        this.targetTier = targetTier;
        this.overridePrice = overridePrice;
        this.itemName = !string.IsNullOrEmpty(name) ? name : (prefab != null ? prefab.name : "");
    }
}

/// <summary>
/// 各關卡層級的商品池設定
/// </summary>
[System.Serializable]
public class LevelShopPool
{
    [Tooltip("關卡層數 (Level 1, 2, 3...)")]
    public int level = 1;

    [Tooltip("該層數的專屬商品清單")]
    public List<ShopItemEntry> items = new List<ShopItemEntry>();

    [Tooltip("該層數的裝備階級機率配置（T1 初級 / T2 中級 / T3 高級）")]
    public EquipmentTierChances tierChances = new EquipmentTierChances(0f, 50f, 50f);
}

/// <summary>
/// 商店與掉落池 ScriptableObject：支援帶權重隨機抽樣、裝備階級機率、關卡分層配置與集中定價調整
/// </summary>
[CreateAssetMenu(fileName = "ShopPool_", menuName = "Dungeon/Shop/Shop Pool")]
public class ShopPoolSO : ScriptableObject
{
    [Tooltip("商品池名稱或描述")]
    public string poolName;

    [Header("預設商品清單（若不分關卡或無匹配關卡時使用）")]
    public List<ShopItemEntry> defaultItems = new List<ShopItemEntry>();

    [Header("預設裝備階級機率（若無特定關卡設定時使用）")]
    public EquipmentTierChances defaultTierChances = new EquipmentTierChances(0f, 50f, 50f);

    [Header("各關卡專屬商品池（可選，若有設定則依關卡優先使用）")]
    public List<LevelShopPool> levelPools = new List<LevelShopPool>();

    /// <summary>
    /// 依關卡層數取得可用的商品條目清單
    /// </summary>
    public List<ShopItemEntry> GetPoolForLevel(int currentLevel)
    {
        if (levelPools != null && levelPools.Count > 0)
        {
            for (int i = 0; i < levelPools.Count; i++)
            {
                if (levelPools[i] != null && levelPools[i].level == currentLevel && levelPools[i].items != null && levelPools[i].items.Count > 0)
                {
                    return levelPools[i].items;
                }
            }
        }
        return defaultItems;
    }

    /// <summary>
    /// 依關卡層數隨機抽取裝備階級 (0: T1, 1: T2, 2: T3)
    /// </summary>
    public int RollEquipmentTier(int currentLevel)
    {
        if (levelPools != null && levelPools.Count > 0)
        {
            for (int i = 0; i < levelPools.Count; i++)
            {
                if (levelPools[i] != null && levelPools[i].level == currentLevel && levelPools[i].tierChances != null)
                {
                    return levelPools[i].tierChances.RollTier();
                }
            }
        }
        return defaultTierChances != null ? defaultTierChances.RollTier() : 1;
    }

    /// <summary>
    /// 依據權重自商品池中隨機抽取一個商品條目
    /// </summary>
    public ShopItemEntry SelectRandomEntry(int currentLevel = 1)
    {
        List<ShopItemEntry> pool = GetPoolForLevel(currentLevel);
        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning($"[ShopPoolSO] '{name}' 在 Level {currentLevel} 沒有任何可用的商品！");
            return null;
        }

        // 計算總權重
        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null && pool[i].itemPrefab != null)
            {
                totalWeight += Mathf.Max(0f, pool[i].weight);
            }
        }

        if (totalWeight <= 0f)
        {
            // 所有權重為 0 時等機率隨機挑選
            List<ShopItemEntry> validEntries = new List<ShopItemEntry>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && pool[i].itemPrefab != null)
                {
                    validEntries.Add(pool[i]);
                }
            }
            if (validEntries.Count == 0) return null;
            return validEntries[Random.Range(0, validEntries.Count)];
        }

        // 輪盤法隨機挑選
        float randomValue = Random.Range(0f, totalWeight);
        for (int i = 0; i < pool.Count; i++)
        {
            ShopItemEntry entry = pool[i];
            if (entry == null || entry.itemPrefab == null) continue;

            float w = Mathf.Max(0f, entry.weight);
            if (w <= 0f) continue;

            randomValue -= w;
            if (randomValue <= 0f)
            {
                return entry;
            }
        }

        // 防禦性回傳最後一個有效條目
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (pool[i] != null && pool[i].itemPrefab != null)
            {
                return pool[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 依據權重自商品池中隨機抽取一個商品 Prefab
    /// </summary>
    public GameObject GetRandomItem(int currentLevel = 1)
    {
        ShopItemEntry entry = SelectRandomEntry(currentLevel);
        return entry != null ? entry.itemPrefab : null;
    }
}
