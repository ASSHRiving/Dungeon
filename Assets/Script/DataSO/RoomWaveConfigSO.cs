using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 房間波次配置總表：根據關卡層數 (Level 1, 2, 3...) 組織多組可選的波次組合，並提供加權隨機抽取
/// </summary>
[CreateAssetMenu(fileName = "RoomWaveConfig_", menuName = "Dungeon/Level/Room Wave Config")]
public class RoomWaveConfigSO : ScriptableObject
{
    [System.Serializable]
    public class LevelWaveGroup
    {
        public int level = 1;
        public List<WaveGroupSO> waveGroups = new List<WaveGroupSO>();
    }

    [Header("各關卡的敵人波次組合")]
    public List<LevelWaveGroup> levelWaveGroups = new List<LevelWaveGroup>();

    /// <summary>
    /// 依據關卡層數與各組合的權重，隨機選取一組波次配置
    /// </summary>
    public WaveGroupSO SelectWaveGroup(int currentLevel)
    {
        LevelWaveGroup currentLevelGroups = null;
        for (int i = 0; i < levelWaveGroups.Count; i++)
        {
            if (levelWaveGroups[i] != null && levelWaveGroups[i].level == currentLevel)
            {
                currentLevelGroups = levelWaveGroups[i];
                break;
            }
        }

        if (currentLevelGroups == null || currentLevelGroups.waveGroups == null || currentLevelGroups.waveGroups.Count == 0)
        {
            Debug.LogWarning($"[RoomWaveConfigSO] 找不到 Level {currentLevel} 的敵人波次組合！");
            return null;
        }

        // 計算總權重
        float totalWeight = 0f;
        for (int i = 0; i < currentLevelGroups.waveGroups.Count; i++)
        {
            WaveGroupSO group = currentLevelGroups.waveGroups[i];
            if (group != null && group.waves != null && group.waves.Count > 0)
            {
                totalWeight += Mathf.Max(0f, group.weight);
            }
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning($"[RoomWaveConfigSO] Level {currentLevel} 的所有波次組合權重均為 0！");
            return currentLevelGroups.waveGroups[0];
        }

        // 依權重隨機挑選
        float randomValue = Random.Range(0f, totalWeight);
        for (int i = 0; i < currentLevelGroups.waveGroups.Count; i++)
        {
            WaveGroupSO group = currentLevelGroups.waveGroups[i];
            if (group == null || group.waves == null || group.waves.Count == 0)
                continue;

            float w = Mathf.Max(0f, group.weight);
            randomValue -= w;
            if (randomValue <= 0f)
            {
                return group;
            }
        }

        return currentLevelGroups.waveGroups[0];
    }
}
