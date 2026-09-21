using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class FightingRoom : Room
{
    [System.Serializable]
    public class LevelWaveGroups
    {
        public int level;

        public List<EnemyWaveGroup> waveGroups = new List<EnemyWaveGroup>();
    }
    [System.Serializable]
    public class EnemyWaveGroup
    {
        public string groupName = "Group 1";
        public float weight = 1;
        public List<EnemyWave> waves;
    }
    [System.Serializable]
    public class EnemyWave
    {
        public string waveName = "Wave";
        public List<SpawnConfig> spawnConfigs; 
    }
    [System.Serializable]
    public class SpawnConfig
    {
        public GameObject enemyPrefab;  // 該位置要刷出的敵人 Prefab
        public GameObject spawnEffectPrefab; //生成特效
        public Transform spawnPoint;    // 生成點 Transform
    }

    [Header("波次設定")]
    [Header("各關卡的敵人波次組合")]
    [SerializeField] protected List<LevelWaveGroups> levelWaveGroups = new List<LevelWaveGroups>();
    protected List<EnemyWave> waves;
    [SerializeField] private float spawnDelay = 1.5f;          // 特效出現到敵人刷出的延遲時間 (秒)


    protected int remainingEnemyCount = 0;
    protected int currentWaveIndex = 0;
    protected bool isBattleStarted = false;
    protected bool isSpawningWave = false; // 防呆：避免預警期間重複觸發判斷
    protected bool isClear = false;
    protected Transform playerTransform;

    /// <summary>
    /// 包含「預警特效」與「延遲刷怪」的波次生成協程
    /// </summary>
    protected IEnumerator SpawnWaveRoutine(int waveIndex)
    {
        isSpawningWave = true;

        if (waveIndex < waves.Count)
        {
            EnemyWave wave = waves[waveIndex];
            List<GameObject> activeEffects = new List<GameObject>();

            if (wave != null && wave.spawnConfigs != null)
            {
                UIEvents.LevelChanged(wave.waveName);
                // 1. 生成預警特效
                foreach (var config in wave.spawnConfigs)
                {
                    if (config.spawnPoint == null) continue;
                    if (config.spawnEffectPrefab != null)
                    {
                        GameObject vfx = Instantiate(config.spawnEffectPrefab, config.spawnPoint.position, config.spawnPoint.rotation);
                        activeEffects.Add(vfx);
                    }
                }

                yield return new WaitForSeconds(spawnDelay);

                // 2. 清除特效
                foreach (var vfx in activeEffects)
                {
                    if (vfx != null) Destroy(vfx);
                }

                remainingEnemyCount = wave.spawnConfigs.Count; 

                // 3. 生成敵人並註冊死亡事件
                foreach (var config in wave.spawnConfigs)
                {
                    if (config.enemyPrefab == null || config.spawnPoint == null) 
                    {
                        remainingEnemyCount--; // 如果有無效設定，直接扣掉避免卡關
                        continue;
                    }

                    GameObject enemyGo = Instantiate(config.enemyPrefab, config.spawnPoint.position, config.spawnPoint.rotation);

                    EnemyCombatSystem combat = enemyGo.GetComponentInChildren<EnemyCombatSystem>();
                    EnemyHealthSystem health = enemyGo.GetComponentInChildren<EnemyHealthSystem>();
                    if (combat != null)
                    {
                        combat.SetSpawnPoint(config.spawnPoint);
                        if (playerTransform != null)
                        {
                            combat.SetCurrentTarget(playerTransform);
                        }
                    }
                    if(health != null)
                    {
                        health.OnDeath += OnEnemyDeath;
                    }
                }
            }
        }

        isSpawningWave = false; // 生成完畢，恢復 Update 檢測
    }
    
    public void OnEnemyDeath()
    {
        remainingEnemyCount--;

        // 只有當數量歸零時，才進行波次切換邏輯！
        if (remainingEnemyCount <= 0 && !isClear)
        {
            currentWaveIndex++;

            if (currentWaveIndex < waves.Count)
            {
                StartCoroutine(SpawnWaveRoutine(currentWaveIndex));
            }
            else
            {
                isClear = true;
                OpenGate();
                isBattleStarted = false;
                GameAssets.Instance.PlayInGameMusic();
            }
        }
    }
    public override void Init()
    {
        SelectWaves();
        isBattleStarted = false;
        isSpawningWave = false;
        isClear = false;
        currentWaveIndex = 0;
    }
    private void SelectWaves()
    {
        int currentLevel = GameManager.Instance.currentLevel;

        LevelWaveGroups currentLevelGroups = null;

        // 找到目前關卡的波次組合
        foreach (LevelWaveGroups levelGroups in levelWaveGroups)
        {
            if (levelGroups.level == currentLevel)
            {
                currentLevelGroups = levelGroups;
                break;
            }
        }

        if (currentLevelGroups == null ||
            currentLevelGroups.waveGroups.Count == 0)
        {
            Debug.LogWarning(
                $"找不到 Level {currentLevel} 的敵人波次組合！"
            );

            return;
        }

        // 計算總權重
        float totalWeight = 0f;

        foreach (EnemyWaveGroup group in currentLevelGroups.waveGroups)
        {
            if (group != null && group.waves.Count > 0)
            {
                totalWeight += group.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("所有波次組合的權重都是 0！");
            return;
        }

        // 依照權重隨機選擇
        float randomValue = Random.Range(0f, totalWeight);

        foreach (EnemyWaveGroup group in currentLevelGroups.waveGroups)
        {
            if (group == null || group.waves.Count == 0)
                continue;

            randomValue -= group.weight;

            if (randomValue <= 0f)
            {
                waves = group.waves;
                return;
            }
        }
    }
    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player"))
        {
            if (!isBattleStarted && !isClear)
            {
                isBattleStarted = true;
                playerTransform = other.transform;

                CloseGate();
                // 啟動第一波生成協程
                StartCoroutine(SpawnWaveRoutine(currentWaveIndex));
            }
        }
    }
}
