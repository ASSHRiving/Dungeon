using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Room03 : Room
{
    [System.Serializable]
    public class SpawnConfig
    {
        public GameObject enemyPrefab;  // 該位置要刷出的敵人 Prefab
        public Transform spawnPoint;    // 生成點 Transform
    }

    [System.Serializable]
    public class EnemyWave
    {
        public string waveName = "Wave";
        public List<SpawnConfig> spawnConfigs; 
    }

    [Header("波次設定")]
    [SerializeField] private List<EnemyWave> waves = new List<EnemyWave>();

    [Header("預警與生成機制")]
    [SerializeField] private GameObject warningEffectPrefab; // 🔑 預警特效 Prefab (如紅圈/魔法陣)
    [SerializeField] private float spawnDelay = 1.5f;          // 🔑 特效出現到敵人刷出的延遲時間 (秒)

    private List<GameObject> currentWaveEnemies = new List<GameObject>();
    private int remainingEnemyCount = 0;
    private int currentWaveIndex = 0;
    private bool isBattleStarted = false;
    private bool isSpawningWave = false; // 防呆：避免預警期間重複觸發判斷
    private bool isClear = false;
    private Transform playerTransform;

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
                GameAssets.Instance.PlayBossMusic();

                // 啟動第一波生成協程
                StartCoroutine(SpawnWaveRoutine(currentWaveIndex));
            }
        }
    }

    /// <summary>
    /// 包含「預警特效」與「延遲刷怪」的波次生成協程
    /// </summary>
    private IEnumerator SpawnWaveRoutine(int waveIndex)
    {
        isSpawningWave = true;
        currentWaveEnemies.Clear();

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
                    if (warningEffectPrefab != null)
                    {
                        GameObject vfx = Instantiate(warningEffectPrefab, config.spawnPoint.position, config.spawnPoint.rotation);
                        activeEffects.Add(vfx);
                    }
                }

                yield return new WaitForSeconds(spawnDelay);

                // 2. 清除特效
                foreach (var vfx in activeEffects)
                {
                    if (vfx != null) Destroy(vfx);
                }

                // 🔑 設定這波敵人要擊殺的總數
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
                    currentWaveEnemies.Add(enemyGo);

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
                GameAssets.Instance.PlayInGameMusic();
            }
        }
    }

    private void CloseGate()
    {
        foreach (var exit in exitSets)
        {
            if (exit.door.activeSelf)
            {
                Gate gate = exit.door.GetComponent<Gate>();
                if (gate != null) gate.CloseGate();
            }
        }
    }

    private void OpenGate()
    {
        foreach (var exit in exitSets)
        {
            if (exit.door.activeSelf)
            {
                Gate gate = exit.door.GetComponent<Gate>();
                if (gate != null) gate.OpenGate();
            }
        }
    }
}