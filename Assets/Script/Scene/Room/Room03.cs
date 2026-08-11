using UnityEngine;
using System.Collections.Generic;

public class Room03 : Room
{
    // 定義單一生成點對應的敵人配置
    [System.Serializable]
    public class SpawnConfig
    {
        public GameObject enemyPrefab;  // 該位置要刷出的敵人 Prefab
        public Transform spawnPoint;    // 生成點 Transform
    }
    // 定義單一波次
    [System.Serializable]
    public class EnemyWave
    {
        public string waveName = "Wave";
        public List<SpawnConfig> spawnConfigs; // 該波次的敵人與生成點配置
    }

    [Header("波次設定")]
    [SerializeField] private List<EnemyWave> waves = new List<EnemyWave>(); // 在 Inspector 設定 3 波次

    private List<GameObject> currentWaveEnemies = new List<GameObject>();
    private int currentWaveIndex = 0;
    private bool isBattleStarted = false;
    private bool isClear = false;
    private Transform playerTransform;

    public override void Init()
    {
        // 覆寫 Init：進房前不生成任何敵人，等待玩家進入 Trigger
        isBattleStarted = false;
        isClear = false;
        currentWaveIndex = 0;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);

        if (other.CompareTag("Player"))
        {
            // 首次進房且尚未開始戰鬥
            if (!isBattleStarted && !isClear)
            {
                isBattleStarted = true;
                playerTransform = other.transform;

                CloseGate();
                GameAssets.Instance.PlayBossMusic();

                // 生成第一波敵人
                SpawnWave(currentWaveIndex);
            }
        }
    }

    void Update()
    {
        // 戰鬥中且尚未全通關時進行波次監控
        if (isBattleStarted && !isClear)
        {
            // 當前波次敵人已全清
            if (IsCurrentWaveCleared())
            {
                currentWaveIndex++;

                // 檢查是否還有下一波
                if (currentWaveIndex < waves.Count)
                {
                    SpawnWave(currentWaveIndex);
                }
                else
                {
                    // 所有波次完成，通關！
                    isClear = true;
                    OpenGate();
                    GameAssets.Instance.PlayInGameMusic();
                }
            }
        }
    }

    /// <summary>
    /// 生成指定波次的敵人
    /// </summary>
    private void SpawnWave(int waveIndex)
    {
        currentWaveEnemies.Clear();

        if (waveIndex >= waves.Count) return;

        EnemyWave wave = waves[waveIndex];
        if (wave == null || wave.spawnConfigs == null) return;

        foreach (var config in wave.spawnConfigs)
        {
            if (config.enemyPrefab == null || config.spawnPoint == null) continue;

            GameObject enemyGo = Instantiate(config.enemyPrefab, config.spawnPoint.position, config.spawnPoint.rotation);
            currentWaveEnemies.Add(enemyGo);

            EnemyCombatSystem combat = enemyGo.GetComponentInChildren<EnemyCombatSystem>();
            if (combat != null)
            {
                combat.SetSpawnPoint(config.spawnPoint);
                if (playerTransform != null)
                {
                    combat.SetCurrentTarget(playerTransform);
                }
            }
        }
    }

    /// <summary>
    /// 檢查當前波次的所有敵人是否已被摧毀
    /// </summary>
    private bool IsCurrentWaveCleared()
    {
        foreach (var enemy in currentWaveEnemies)
        {
            if (enemy != null)
            {
                return false; // 還有敵人存活
            }
        }
        return true; // 當前波次已清空
    }

    private void CloseGate()
    {
        foreach (var exit in exitSets)
        {
            if (exit.door.activeSelf)
            {
                Gate gate = exit.door.GetComponent<Gate>();
                if (gate != null)
                {
                    gate.CloseGate();
                }
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
                if (gate != null)
                {
                    gate.OpenGate();
                }
            }
        }
    }
}