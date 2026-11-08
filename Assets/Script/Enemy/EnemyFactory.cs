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
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[EnemyFactory] 嘗試生成空的怪物 Prefab！");
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

        // 3. 獲取血量與戰鬥組件並重新綁定
        EnemyHealthSystem health = enemyGo.GetComponentInChildren<EnemyHealthSystem>();
        EnemyCombatSystem combat = enemyGo.GetComponentInChildren<EnemyCombatSystem>();

        if (health != null) health.BindStats(stats);
        if (combat != null) combat.BindStats(stats);

        // 4. 根據是否有指定 EnemyDataSO 進行屬性初始化與關卡成長計算
        if (enemyData != null)
        {
            // 精準從 EnemyDataSO 計算並套用關卡縮放 (HP, ATK, 防禦, 跑速等)
            stats.InitFromData(enemyData, currentLevel, isElite);

            if (health != null)
            {
                health.SetGoldDrop(enemyData.CalculateGold(currentLevel, isElite));
            }

            if (isElite)
            {
                enemyGo.transform.localScale *= enemyData.eliteScale;
            }
        }
        else
        {
            // 防呆 Fallback：若場景內舊有波次未指定 EnemyDataSO，自動依 Prefab 原有基礎值計算關卡成長
            float levelHpScale = 1f + Mathf.Max(0, currentLevel - 1) * 0.15f;   // 每關 +15% HP
            float levelDmgScale = 1f + Mathf.Max(0, currentLevel - 1) * 0.08f;  // 每關 +8% 傷害
            float levelGoldScale = 1f + Mathf.Max(0, currentLevel - 1) * 0.10f; // 每關 +10% 金幣

            float eliteHp = isElite ? 2.0f : 1.0f;
            float eliteDmg = isElite ? 1.35f : 1.0f;
            float eliteGold = isElite ? 3.0f : 1.0f;

            float baseHp = health != null ? health.maxHealth : 80f;
            float baseDef = health != null ? health.shield : 0f;
            float baseDmg = 10f;
            if (combat != null) baseDmg = combat.damage > 0 ? combat.damage : 10f;

            float finalHp = Mathf.Round(baseHp * levelHpScale * eliteHp);
            float finalDmg = Mathf.Round(baseDmg * levelDmgScale * eliteDmg);

            stats.InitBaseStats(finalHp, baseDef, finalDmg, 0f);

            if (health != null)
            {
                int currentGold = health.GetGoldDrop() > 0 ? health.GetGoldDrop() : 5;
                health.SetGoldDrop(Mathf.Max(1, Mathf.RoundToInt(currentGold * levelGoldScale * eliteGold)));
            }

            if (isElite)
            {
                enemyGo.transform.localScale *= 1.25f;
            }
        }

        return enemyGo;
    }
}
