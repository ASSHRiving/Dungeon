using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class FightingRoom : Room
{
    [Header("波次設定 (ScriptableObject)")]
    [SerializeField] protected RoomWaveConfigSO waveConfig;
    [SerializeField] protected Transform[] spawnPoints;
    [SerializeField] protected float eliteSpawnChance = 0.15f; // 菁英怪生成機率 (預設 15%)

    protected WaveGroupSO currentWaveGroup;
    protected int remainingEnemyCount = 0;
    protected int currentWaveIndex = 0;
    protected bool isBattleStarted = false;
    protected bool isSpawningWave = false; // 防呆：避免預警期間重複觸發判斷
    protected bool isClear = false;
    protected Transform playerTransform;

    private readonly List<GameObject> activeEffects = new List<GameObject>();

    /// <summary>
    /// 包含「預警特效」與「延遲刷怪」的波次生成協程
    /// </summary>
    protected IEnumerator SpawnWaveRoutine(int waveIndex)
    {
        isSpawningWave = true;

        if (currentWaveGroup != null && currentWaveGroup.waves != null && waveIndex < currentWaveGroup.waves.Count)
        {
            WaveData wave = currentWaveGroup.waves[waveIndex];
            ClearActiveEffects();

            if (wave != null && wave.spawnConfigs != null)
            {
                UIEvents.LevelChanged(wave.waveName);

                // 1. 生成預警特效 (使用物件池)
                foreach (var config in wave.spawnConfigs)
                {
                    Transform point = GetSpawnPoint(config.spawnPointIndex);
                    if (point == null) continue;

                    if (config.spawnEffectPrefab != null)
                    {
                        GameObject vfx = ObjectPoolManager.Instance != null
                            ? ObjectPoolManager.Instance.SpawnVFX(config.spawnEffectPrefab, point.position, point.rotation)
                            : Instantiate(config.spawnEffectPrefab, point.position, point.rotation);

                        if (vfx != null)
                        {
                            activeEffects.Add(vfx);
                        }
                    }
                }

                yield return new WaitForSeconds(wave.spawnDelay);

                // 2. 清除 / 回收特效至物件池
                ClearActiveEffects();

                remainingEnemyCount = wave.spawnConfigs.Count;
                int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;

                // 3. 生成敵人並註冊死亡事件
                foreach (var config in wave.spawnConfigs)
                {
                    Transform point = GetSpawnPoint(config.spawnPointIndex);
                    if (config.enemyPrefab == null || point == null)
                    {
                        remainingEnemyCount--; // 如果有無效設定，直接扣掉避免卡關
                        continue;
                    }

                    // 判斷是否生成菁英怪 (第 1 層不生菁英怪，保護新手；第 2 層起依機率決定)
                    bool isElite = config.canBeElite && (currentLevel > 1) && (Random.value < eliteSpawnChance);

                    // 透過 EnemyFactory 工廠生成並注入數值
                    GameObject enemyGo = EnemyFactory.SpawnEnemy(
                        config.enemyPrefab,
                        config.enemyData,
                        point.position,
                        point.rotation,
                        currentLevel,
                        isElite
                    );

                    if (enemyGo == null)
                    {
                        remainingEnemyCount--;
                        continue;
                    }

                    EnemyCombatSystem combat = enemyGo.GetComponentInChildren<EnemyCombatSystem>();
                    EnemyHealthSystem health = enemyGo.GetComponentInChildren<EnemyHealthSystem>();
                    if (combat != null)
                    {
                        combat.SetSpawnPoint(point);
                        if (playerTransform != null)
                        {
                            combat.SetCurrentTarget(playerTransform);
                        }
                    }
                    if (health != null)
                    {
                        health.OnDeath += OnEnemyDeath;
                    }
                }
            }
        }

        isSpawningWave = false; // 生成完畢，恢復檢測
    }

    public void OnEnemyDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKill();
        }
        remainingEnemyCount--;

        // 只有當數量歸零時，才進行波次切換邏輯
        if (remainingEnemyCount <= 0 && !isClear)
        {
            currentWaveIndex++;

            if (currentWaveGroup != null && currentWaveGroup.waves != null && currentWaveIndex < currentWaveGroup.waves.Count)
            {
                StartCoroutine(SpawnWaveRoutine(currentWaveIndex));
            }
            else
            {
                OnRoomCleared();
            }
        }
    }

    public override void Init()
    {
        ClearActiveEffects();
        AutoDetectSpawnPointsIfEmpty();
        SelectWaves();
        isBattleStarted = false;
        isSpawningWave = false;
        isClear = false;
        currentWaveIndex = 0;
    }

    protected virtual void OnDisable()
    {
        ClearActiveEffects();
    }

    private void ClearActiveEffects()
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            GameObject vfx = activeEffects[i];
            if (vfx != null)
            {
                if (ObjectPoolManager.Instance != null)
                {
                    ObjectPoolManager.Instance.ReleaseVFX(vfx);
                }
                else
                {
                    Destroy(vfx);
                }
            }
        }
        activeEffects.Clear();
    }

    protected virtual void SelectWaves()
    {
        currentWaveGroup = null;
        if (waveConfig == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 尚未設定 RoomWaveConfigSO！");
            return;
        }

        int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;
        currentWaveGroup = waveConfig.SelectWaveGroup(currentLevel);

        if (currentWaveGroup == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 找不到 Level {currentLevel} 的有效波次組合！");
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

    protected virtual void OnRoomCleared()
    {
        isClear = true;
        OpenGate();
        isBattleStarted = false;
        if (GameAssets.Instance != null)
        {
            GameAssets.Instance.PlayInGameMusic();
        }
    }

    /// <summary>
    /// 依點位索引取得對應的 SpawnPoint Transform (包含越界防呆與自動環狀循環)
    /// </summary>
    public Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            AutoDetectSpawnPointsIfEmpty();
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] 房間沒有任何 SpawnPoints，使用自身位置。");
            return transform;
        }

        if (index >= 0 && index < spawnPoints.Length && spawnPoints[index] != null)
        {
            return spawnPoints[index];
        }

        int safeIndex = Mathf.Abs(index) % spawnPoints.Length;
        return spawnPoints[safeIndex] != null ? spawnPoints[safeIndex] : transform;
    }

    /// <summary>
    /// 若 Inspector 未手動指派 spawnPoints，自動搜尋子節點中名為 SpawnPoints 的群組或包含 SpawnPoint 字樣的節點
    /// </summary>
    private void AutoDetectSpawnPointsIfEmpty()
    {
        if (spawnPoints != null && spawnPoints.Length > 0 && spawnPoints[0] != null)
            return;

        Transform spParent = transform.Find("SpawnPoints");
        if (spParent != null && spParent.childCount > 0)
        {
            spawnPoints = new Transform[spParent.childCount];
            for (int i = 0; i < spParent.childCount; i++)
            {
                spawnPoints[i] = spParent.GetChild(i);
            }
            return;
        }

        // 搜尋子節點 (如 EliteRoom 的 EmenySpawnPoint)
        List<Transform> list = new List<Transform>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t != transform && (t.name.Contains("SpawnPoint") || t.name.Contains("EmenySpawnPoint")))
            {
                list.Add(t);
            }
        }

        if (list.Count > 0)
        {
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            spawnPoints = list.ToArray();
        }
    }
}
