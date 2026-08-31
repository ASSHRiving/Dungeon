using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class FightingRoom : Room
{
    [System.Serializable]
    public class SpawnConfig
    {
        public GameObject enemyPrefab;  // 該位置要刷出的敵人 Prefab
        public GameObject spawnEffectPrefab; //生成特效
        public Transform spawnPoint;    // 生成點 Transform
    }

    [System.Serializable]
    public class EnemyWave
    {
        public string waveName = "Wave";
        public List<SpawnConfig> spawnConfigs; 
    }

    [Header("波次設定")]
    [SerializeField] protected List<EnemyWave> waves = new List<EnemyWave>();
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
        isBattleStarted = false;
        isSpawningWave = false;
        isClear = false;
        currentWaveIndex = 0;
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
