#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DefaultDataSOAssetGenerator
{
    private const string CHARACTER_DIR = "Assets/ScriptableObjects/Characters";
    private const string ENEMY_DIR = "Assets/ScriptableObjects/Enemies";

    [MenuItem("Tools/⚔️ 生成預設角色與敵人 SO 數據資產")]
    public static void GenerateDefaultAssets()
    {
        EnsureDirectory(CHARACTER_DIR);
        EnsureDirectory(ENEMY_DIR);

        // 1. 戰士 (Warrior)
        string warriorPath = $"{CHARACTER_DIR}/Character_Warrior.asset";
        if (AssetDatabase.LoadAssetAtPath<CharacterDataSO>(warriorPath) == null)
        {
            var warrior = ScriptableObject.CreateInstance<CharacterDataSO>();
            warrior.characterID = 0;
            warrior.characterName = "戰士";
            warrior.description = "擅長近身搏殺，擁有極高的防禦力與生存韌性，是地牢探索中最穩固的前排護盾。";
            warrior.baseMaxHealth = 100f;
            warrior.baseShield = 10f;
            warrior.baseAttackDamage = 15f;
            warrior.baseCritRate = 5f;
            warrior.baseMoveSpeed = 7f;
            warrior.basePoise = 100f;
            warrior.characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Character/Player/Warrior.prefab");

            AssetDatabase.CreateAsset(warrior, warriorPath);
            Debug.Log($"[DataSO] 建立預設戰士配置: {warriorPath}");
        }

        // 2. 骷髏戰士 (Skeleton Warrior)
        string skelPath = $"{ENEMY_DIR}/Enemy_SkeletonWarrior.asset";
        if (AssetDatabase.LoadAssetAtPath<EnemyDataSO>(skelPath) == null)
        {
            var skel = ScriptableObject.CreateInstance<EnemyDataSO>();
            skel.enemyID = "Skeleton_Warrior";
            skel.enemyName = "骷髏戰士";
            skel.isBoss = false;
            skel.baseMaxHealth = 80f;
            skel.baseDamage = 12f;
            skel.baseShield = 0f;
            skel.baseMoveSpeed = 3.2f;
            skel.basePoise = 80f;
            skel.baseGold = 5;

            skel.healthGrowthPerLevel = 0.15f; // 每關血量 +15%
            skel.damageGrowthPerLevel = 0.08f; // 每關傷害 +8%
            skel.goldGrowthPerLevel = 0.10f;   // 每關金幣 +10%

            skel.eliteHealthMult = 2.0f;
            skel.eliteDamageMult = 1.3f;
            skel.eliteGoldMult = 3.0f;
            skel.eliteScale = 1.25f;

            AssetDatabase.CreateAsset(skel, skelPath);
            Debug.Log($"[DataSO] 建立預設骷髏戰士配置: {skelPath}");
        }

        // 3. 骷髏弓箭手 (Skeleton Archer)
        string archerPath = $"{ENEMY_DIR}/Enemy_SkeletonArcher.asset";
        if (AssetDatabase.LoadAssetAtPath<EnemyDataSO>(archerPath) == null)
        {
            var archer = ScriptableObject.CreateInstance<EnemyDataSO>();
            archer.enemyID = "Skeleton_Archer";
            archer.enemyName = "骷髏弓箭手";
            archer.isBoss = false;
            archer.baseMaxHealth = 60f;
            archer.baseDamage = 14f;
            archer.baseShield = 0f;
            archer.baseMoveSpeed = 3.0f;
            archer.basePoise = 50f;
            archer.baseGold = 6;

            archer.healthGrowthPerLevel = 0.12f;
            archer.damageGrowthPerLevel = 0.10f;
            archer.goldGrowthPerLevel = 0.10f;

            archer.eliteHealthMult = 1.8f;
            archer.eliteDamageMult = 1.4f;
            archer.eliteGoldMult = 3.0f;
            archer.eliteScale = 1.2f;

            AssetDatabase.CreateAsset(archer, archerPath);
            Debug.Log($"[DataSO] 建立預設骷髏弓箭手配置: {archerPath}");
        }

        // 4. Boss 巨石守衛 (Stone Giant)
        string bossPath = $"{ENEMY_DIR}/Enemy_StoneGiantBoss.asset";
        if (AssetDatabase.LoadAssetAtPath<EnemyDataSO>(bossPath) == null)
        {
            var boss = ScriptableObject.CreateInstance<EnemyDataSO>();
            boss.enemyID = "Boss_StoneGiant";
            boss.enemyName = "深淵石巨人";
            boss.isBoss = true;
            boss.baseMaxHealth = 600f;
            boss.baseDamage = 28f;
            boss.baseShield = 15f;
            boss.baseMoveSpeed = 2.8f;
            boss.basePoise = 250f;
            boss.baseGold = 60;

            boss.healthGrowthPerLevel = 0.25f;
            boss.damageGrowthPerLevel = 0.15f;
            boss.goldGrowthPerLevel = 0.20f;

            AssetDatabase.CreateAsset(boss, bossPath);
            Debug.Log($"[DataSO] 建立預設 Boss 配置: {bossPath}");
        }

        // 5. 小骷髏 (Skeleton Minion)
        string minionPath = $"{ENEMY_DIR}/Enemy_SkeletonMinion.asset";
        if (AssetDatabase.LoadAssetAtPath<EnemyDataSO>(minionPath) == null)
        {
            var minion = ScriptableObject.CreateInstance<EnemyDataSO>();
            minion.enemyID = "Skeleton_Minion";
            minion.enemyName = "召喚小骷髏";
            minion.isBoss = false;
            minion.baseMaxHealth = 35f;
            minion.baseDamage = 8f;
            minion.baseShield = 0f;
            minion.baseMoveSpeed = 3.8f;
            minion.basePoise = 30f;
            minion.baseGold = 2;

            minion.healthGrowthPerLevel = 0.15f;
            minion.damageGrowthPerLevel = 0.08f;
            minion.goldGrowthPerLevel = 0.05f;

            minion.eliteHealthMult = 1.6f;
            minion.eliteDamageMult = 1.25f;
            minion.eliteGoldMult = 2.0f;
            minion.eliteScale = 1.2f;

            AssetDatabase.CreateAsset(minion, minionPath);
            Debug.Log($"[DataSO] 建立預設小骷髏配置: {minionPath}");
        }

        // 6. 獸人戰士 (Orc)
        string orcPath = $"{ENEMY_DIR}/Enemy_Orc.asset";
        if (AssetDatabase.LoadAssetAtPath<EnemyDataSO>(orcPath) == null)
        {
            var orc = ScriptableObject.CreateInstance<EnemyDataSO>();
            orc.enemyID = "Orc";
            orc.enemyName = "獸人蠻卒";
            orc.isBoss = false;
            orc.baseMaxHealth = 120f;
            orc.baseDamage = 18f;
            orc.baseShield = 5f;
            orc.baseMoveSpeed = 3.0f;
            orc.basePoise = 120f;
            orc.baseGold = 8;

            orc.healthGrowthPerLevel = 0.15f;
            orc.damageGrowthPerLevel = 0.10f;
            orc.goldGrowthPerLevel = 0.12f;

            orc.eliteHealthMult = 2.2f;
            orc.eliteDamageMult = 1.4f;
            orc.eliteGoldMult = 3.0f;
            orc.eliteScale = 1.3f;

            AssetDatabase.CreateAsset(orc, orcPath);
            Debug.Log($"[DataSO] 建立預設獸人蠻卒配置: {orcPath}");
        }

        // 7. 法師 (Mage)
        string magePath = $"{ENEMY_DIR}/Enemy_Mage.asset";
        if (AssetDatabase.LoadAssetAtPath<EnemyDataSO>(magePath) == null)
        {
            var mage = ScriptableObject.CreateInstance<EnemyDataSO>();
            mage.enemyID = "Mage";
            mage.enemyName = "死靈法師";
            mage.isBoss = false;
            mage.baseMaxHealth = 90f;
            mage.baseDamage = 16f;
            mage.baseShield = 2f;
            mage.baseMoveSpeed = 3.2f;
            mage.basePoise = 60f;
            mage.baseGold = 10;

            mage.healthGrowthPerLevel = 0.14f;
            mage.damageGrowthPerLevel = 0.12f;
            mage.goldGrowthPerLevel = 0.15f;

            mage.eliteHealthMult = 2.0f;
            mage.eliteDamageMult = 1.35f;
            mage.eliteGoldMult = 3.0f;
            mage.eliteScale = 1.25f;

            AssetDatabase.CreateAsset(mage, magePath);
            Debug.Log($"[DataSO] 建立預設法師配置: {magePath}");
        }

        // 8. 角色局外升級設定 (CharacterUpgradeConfig)
        string upgradeConfigPath = $"{CHARACTER_DIR}/CharacterUpgradeConfig.asset";
        if (AssetDatabase.LoadAssetAtPath<CharacterUpgradeConfigSO>(upgradeConfigPath) == null)
        {
            var config = ScriptableObject.CreateInstance<CharacterUpgradeConfigSO>();
            AssetDatabase.CreateAsset(config, upgradeConfigPath);
            Debug.Log($"[DataSO] 建立預設局外養成設定: {upgradeConfigPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ [DataSO] 所有預設角色與敵人 ScriptableObject 資產建立完成！");
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
#endif
