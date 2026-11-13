using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SpawnMinion", menuName = "Ability/Mage/SpawnMinion")]
public class SpawnMinion : AbilityBase
{
    [Header("=== 召喚小骷髏設定 ===")]
    public GameObject minionPrefab;          // 拖入要召喚的小骷髏 Prefab
    public EnemyDataSO minionData;            // (可選) 骷髏數值配置資產
    public int spawnCount = 3;               // 召喚數量 (預設 3 隻)
    public float spawnDelay = 1.2f;          // 預警法陣出現到小骷髏實體化的詠唱秒數

    [Header("=== NavMesh 隨機扇形與防穿牆內縮設定 ===")]
    public float minSpawnRadius = 2.8f;      // 距離法師最近的隨機半徑
    public float maxSpawnRadius = 6.0f;      // 距離法師最遠的隨機半徑
    public float fanSpreadAngle = 240f;      // 前方扇形展開總角度
    public float angleRandomJitter = 20f;    // 每個扇形分區的隨機角度偏移
    public float wallPullbackOffset = 0.85f; // 撞牆時往法師方向縮回的安全距離 (避免法陣一半卡進牆裡)
    public float navMeshSampleDistance = 2.0f; // NavMesh 合法地板搜尋容許半徑
    public LayerMask wallLayerMask = ~0;     // 牆壁與障礙物圖層 (可在 Inspector 勾選 Default / Wall)

    [Header("=== 召喚特效 (複用死靈法陣與粒子) ===")]
    public GameObject summonCircleVfxPrefab; // 階段一：地面旋轉預警法陣 Prefab
    public GameObject summonBurstVfxPrefab;  // 階段二：小骷髏鑽出地面時的黑霧/靈魂噴發 Prefab
    public float circleVfxDuration = 2.0f;   // 預警法陣總存活時間

    protected override void OnCastSkill(StateMachineSystem system)
    {
        base.OnCastSkill(system);
        system.EnsureMinSkillCooldown(spawnDelay + 0.5f);
        system.StartCoroutine(SpawnMinionsRoutine(system));
    }

    private IEnumerator SpawnMinionsRoutine(StateMachineSystem system)
    {
        if (minionPrefab == null)
        {
            Debug.LogWarning($"[{skillName}] 尚未指派 minionPrefab！");
            yield break;
        }

        // --- 階段一：計算 3 個 NavMesh 合法地板座標並生成地面預警法陣 ---
        List<Vector3> validSpawnPoints = CalculateNavMeshSpawnPoints(system.transform);
        List<GameObject> activeCircles = new List<GameObject>();

        foreach (Vector3 pt in validSpawnPoints)
        {
            if (summonCircleVfxPrefab != null)
            {
                GameObject circle = Instantiate(
                    summonCircleVfxPrefab, 
                    pt + Vector3.up * 0.02f, 
                    Quaternion.identity
                );
                activeCircles.Add(circle);
            }
        }

        // --- 等待詠唱時間 (逐幀檢查法師是否死亡或被中斷) ---
        float elapsed = 0f;
        while (elapsed < spawnDelay)
        {
            if (IsCasterInterrupted(system))
            {
                foreach (GameObject circle in activeCircles)
                {
                    if (circle != null) Destroy(circle);
                }
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (GameObject circle in activeCircles)
        {
            if (circle != null) Destroy(circle, Mathf.Max(0.1f, circleVfxDuration - spawnDelay));
        }

        if (IsCasterInterrupted(system)) yield break;

        // --- 階段二：在法陣中心噴發爆發粒子，並生成 3 隻小骷髏 ---
        Transform playerTarget = system.combat != null ? system.combat.GetCurrentTarget() : null;
        int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;

        foreach (Vector3 spawnPos in validSpawnPoints)
        {
            if (summonBurstVfxPrefab != null)
            {
                GameObject burst = Instantiate(summonBurstVfxPrefab, spawnPos, Quaternion.identity);
                Destroy(burst, 2.5f);
            }

            Quaternion spawnRot = system.transform.rotation;
            if (playerTarget != null)
            {
                Vector3 dirToPlayer = playerTarget.position - spawnPos;
                dirToPlayer.y = 0f;
                if (dirToPlayer.sqrMagnitude > 0.001f)
                {
                    spawnRot = Quaternion.LookRotation(dirToPlayer.normalized);
                }
            }

            GameObject minion = EnemyFactory.SpawnEnemy(minionPrefab, minionData, spawnPos, spawnRot, currentLevel, false);
            if (minion == null) continue;

            if (minion.TryGetComponent<NavMeshAgent>(out var minionAgent))
            {
                minionAgent.Warp(spawnPos);
            }

            EnemyCombatSystem enemyCombat = minion.GetComponentInChildren<EnemyCombatSystem>();
            if (enemyCombat != null && playerTarget != null)
            {
                enemyCombat.SetCurrentTarget(playerTarget);
            }
        }
    }

    /// <summary>
    /// 在扇形範圍隨機選點；若與法師之間隔著牆壁，自動沿著射線把生成點往法師方向拉近
    /// </summary>
    private List<Vector3> CalculateNavMeshSpawnPoints(Transform casterTransform)
    {
        List<Vector3> points = new List<Vector3>(spawnCount);
        Vector3 origin = casterTransform.position;
        Vector3 forward = casterTransform.forward;

        // 先確保法師自己的起點吸附在房間內的 NavMesh 上
        if (NavMesh.SamplePosition(origin, out NavMeshHit originHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            origin = originHit.position;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            // 1. 扇形基礎角度 + 隨機角度擾動 + 隨機距離 (minSpawnRadius ~ maxSpawnRadius)
            float baseAngle = 0f;
            if (spawnCount > 1)
            {
                float t = (float)i / (spawnCount - 1) - 0.5f; // -0.5 ~ +0.5
                baseAngle = t * fanSpreadAngle;
            }
            float randomAngle = baseAngle + Random.Range(-angleRandomJitter, angleRandomJitter);
            float targetDist = Random.Range(minSpawnRadius, maxSpawnRadius);

            Vector3 dir = (Quaternion.Euler(0f, randomAngle, 0f) * forward).normalized;

            // 2. 【核心邏輯 A】從法師腰部高度往該方向打實體射線，若撞到牆壁就把目標距離縮短到牆壁前方！
            Vector3 rayStart = origin + Vector3.up * 0.8f;
            RaycastHit[] hits = Physics.RaycastAll(rayStart, dir, targetDist, wallLayerMask, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in hits)
            {
                Transform root = h.transform.root;
                // 忽略法師自己、其他敵人與玩家，只針對牆壁/障礙物縮短距離
                if (root == casterTransform.root || root.CompareTag("Enemy") || root.CompareTag("Player"))
                    continue;

                // 把生成距離拉近到「牆壁碰撞點往內縮 wallPullbackOffset」的位置 (最靠近法師保底 0.8m)
                float pulledBackDist = Mathf.Max(0.8f, h.distance - wallPullbackOffset);
                if (pulledBackDist < targetDist)
                {
                    targetDist = pulledBackDist;
                }
            }

            Vector3 desiredPos = origin + dir * targetDist;

            // 3. 【核心邏輯 B】若你的 NavMesh 已經包含牆壁切開，用 NavMesh.Raycast 再做一次導航邊界內縮保底
            if (NavMesh.Raycast(origin, desiredPos, out NavMeshHit navWallHit, NavMesh.AllAreas))
            {
                // navWallHit.position 就是撞上牆壁邊緣的那個點，往法師方向退回一點
                desiredPos = Vector3.MoveTowards(navWallHit.position, origin, wallPullbackOffset);
            }

            // 4. 將拉近後的座標吸附到合法的 NavMesh 地板上
            if (NavMesh.SamplePosition(desiredPos, out NavMeshHit finalHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                // 再確認一次吸附後的點沒有因為 SamplePosition 半徑過大而又跳回牆外
                if (NavMesh.Raycast(origin, finalHit.position, out NavMeshHit recheckHit, NavMesh.AllAreas))
                {
                    points.Add(Vector3.MoveTowards(recheckHit.position, origin, 0.5f));
                }
                else
                {
                    points.Add(finalHit.position);
                }
            }
            else
            {
                points.Add(origin);
            }
        }

        return points;
    }

    private bool IsCasterInterrupted(StateMachineSystem system)
    {
        if (system == null || !system.isActiveAndEnabled || !system.gameObject.activeInHierarchy)
            return true;

        if (system.animator != null && system.animator.CheckAnimationTag("Dead"))
            return true;

        return false;
    }
}
