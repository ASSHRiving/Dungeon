using UnityEngine;

/// <summary>
/// 怪物生成工廠 (Enemy Factory)
/// 統一負責實體化怪物、綁定中央數值大腦 (CharacterStats)，
/// 並根據當前關卡難度 (Level) 與菁英狀態動態縮放生命值、攻擊力、掉落金幣與體積。
/// </summary>
public static class EnemyFactory
{
    /// <summary>
    /// 生成怪物並注入關卡縮放數值
    /// </summary>
    /// <param name="enemyPrefab">要生成的怪物 Prefab</param>
    /// <param name="enemyData">怪物配置資產 (若為 null 則自動以 Prefab 基礎數值進行動態成長縮放)</param>
    /// <param name="position">生成座標</param>
    /// <param name="rotation">生成旋轉</param>
    /// <param name="currentLevel">當前遊戲關卡層數 (預設第 1 層)</param>
    /// <param name="isElite">是否生成為菁英怪物</param>
    /// <returns>生成之怪物 GameObject</returns>
    public static GameObject SpawnEnemy(
        GameObject enemyPrefab,
        EnemyDataSO enemyData,
        Vector3 position,
        Quaternion rotation,
        int currentLevel = 1,
        bool isElite = false)
    {
        if (enemyPrefab == null || enemyData == null)
        {
            Debug.LogError("[EnemyFactory] enemyPrefab 或 enemyData 為空，無法生成怪物！");
            return null;
        }

        // 1. 實體化怪物
        GameObject enemyGo = Object.Instantiate(enemyPrefab, position, rotation);

        // 2. 獲取或掛載中央數值大腦
        CharacterStats stats = enemyGo.GetComponent<CharacterStats>();
        if (stats == null)
        {
            stats = enemyGo.AddComponent<CharacterStats>();
        }

        // 3. 獲取血量與戰鬥組件並初始化 EnemyDataSO 數值
        EnemyHealthSystem health = enemyGo.GetComponentInChildren<EnemyHealthSystem>();
        EnemyCombatSystem combat = enemyGo.GetComponentInChildren<EnemyCombatSystem>();

        // 精準從 EnemyDataSO 計算並套用關卡縮放 (HP, ATK, 防禦, 跑速等)
        stats.InitFromData(enemyData, currentLevel, isElite);

        if (health != null)
        {
            health.ConfigureBoss(enemyData.isBoss, enemyData.enemyName);
            health.SetGoldDrop(enemyData.CalculateGold(currentLevel, isElite));
        }

        if (isElite)
        {
            enemyGo.transform.localScale *= enemyData.eliteScale;
        }

        if (health != null) health.BindStats(stats);
        if (combat != null) combat.BindStats(stats);

        return enemyGo;
    }
}
