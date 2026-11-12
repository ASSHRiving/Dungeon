using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WaveSpawnEntry
{
    public GameObject enemyPrefab;        // 該位置要刷出的敵人 Prefab
    public EnemyDataSO enemyData;         // (可選) 怪物數值配置資產，若為空則依 Prefab 原有基礎值動態成長
    public int spawnPointIndex;           // 房間對應的點位編號 (0, 1, 2, ...)
    public bool canBeElite = true;        // 此位置是否可隨機生成菁英怪
    public GameObject spawnEffectPrefab;  // 生成預警特效
}

[System.Serializable]
public class WaveData
{
    public string waveName = "Wave";
    [Tooltip("特效出現到敵人刷出的延遲時間 (秒)")]
    public float spawnDelay = 1.5f;
    public List<WaveSpawnEntry> spawnConfigs = new List<WaveSpawnEntry>();
}

/// <summary>
/// 敵人波次組合：代表一組連貫的戰鬥流程 (例如第 1 波小兵 + 第 2 波弓手伏擊)
/// 設計師點開此資產即可直接編輯內部各波次，無需跨多個資產檔案
/// </summary>
[CreateAssetMenu(fileName = "Waves_", menuName = "Dungeon/Level/Wave Group")]
public class WaveGroupSO : ScriptableObject
{
    public string groupName = "Group 1";
    [Min(0f)] public float weight = 1f;
    public List<WaveData> waves = new List<WaveData>();
}
