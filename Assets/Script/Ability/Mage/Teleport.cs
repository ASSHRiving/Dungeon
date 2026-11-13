using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "Teleport", menuName = "Ability/Mage/Teleport")]
public class Teleport : AbilityBase
{
    [Header("=== 瞬移脫困參數 ===")]
    public float minTeleportDist = 4.0f;      // 期望至少拉開的瞬移距離
    public float maxTeleportDist = 8.0f;      // 最大瞬移搜尋半徑
    public float teleportDelay = 0.25f;       // 觸發動畫後幾秒瞬間消失 (配合舉杖/縮身前搖)
    public float invisibleDuration = 0.15f;   // 消失在空氣中的短暫無敵/隱形時間 (設 0 則瞬間到達)

    [Header("=== 防穿牆與環境檢測 ===")]
    public int sampleDirections = 12;         // 環繞 360 度探測幾個逃生方向
    public float wallPullbackOffset = 0.85f;  // 撞牆時往房間內退縮的安全距離
    public float navMeshSampleDistance = 2.0f;
    public LayerMask wallLayerMask = ~0;      // 牆壁與障礙物圖層

    [Header("=== 瞬移特效 (可直接複用之前的黑霧或爆炸 Prefab) ===")]
    public GameObject vanishVfxPrefab;        // 原地消失時的特效
    public GameObject appearVfxPrefab;        // 新位置現身時的特效

    protected override void OnCastSkill(StateMachineSystem system)
    {
        base.OnCastSkill(system);
        system.EnsureMinSkillCooldown(teleportDelay + invisibleDuration + 0.4f);
        system.StartCoroutine(TeleportRoutine(system));
    }

    private IEnumerator TeleportRoutine(StateMachineSystem system)
    {
        Transform caster = system.transform;
        Transform player = system.combat != null ? system.combat.GetCurrentTarget() : null;

        // 1. 先算出房間內最安全的逃生瞬移點
        Vector3 bestEscapePos = FindBestEscapePoint(caster, player);

        // 等待施法短暫前搖
        if (teleportDelay > 0f)
        {
            yield return new WaitForSeconds(teleportDelay);
        }

        if (system == null || !system.isActiveAndEnabled) yield break;
        if (system.animator != null && system.animator.CheckAnimationTag("Dead")) yield break;

        // 2. 在起點生成「消失特效」
        if (vanishVfxPrefab != null)
        {
            GameObject vfxOut = Instantiate(vanishVfxPrefab, caster.position + Vector3.up * 0.9f, Quaternion.identity);
            Destroy(vfxOut, 2.0f);
        }

        // 3. 若有設定短暫消失時間 (讓視覺上有「化為黑煙消失 ➔ 遠處重組」的感覺)
        Renderer[] renderers = null;
        if (invisibleDuration > 0f)
        {
            renderers = system.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
            if (system.characterController != null) system.characterController.enabled = false;

            yield return new WaitForSeconds(invisibleDuration);
        }

        if (system == null || !system.isActiveAndEnabled) yield break;

        // 4. 執行瞬移 (優先使用 NavMeshAgent.Warp 避免網格拉回)
        if (system.characterController != null) system.characterController.enabled = false;

        if (system.agent != null && system.agent.isActiveAndEnabled && system.agent.isOnNavMesh)
        {
            system.agent.Warp(bestEscapePos);
            system.agent.ResetPath();
        }
        else
        {
            caster.position = bestEscapePos;
        }

        if (system.characterController != null) system.characterController.enabled = true;

        // 瞬移落地後立即轉身面向玩家
        if (player != null)
        {
            Vector3 lookDir = player.position - caster.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                caster.rotation = Quaternion.LookRotation(lookDir.normalized);
            }
        }

        // 恢復模型顯示
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = true;
            }
        }

        // 5. 在終點生成「現身特效」
        if (appearVfxPrefab != null)
        {
            GameObject vfxIn = Instantiate(appearVfxPrefab, caster.position + Vector3.up * 0.9f, Quaternion.identity);
            Destroy(vfxIn, 2.0f);
        }
    }

    /// <summary>
    /// 環繞 360 度發射多條防穿牆探測線，選出房間內「離玩家最遠」的合法座標
    /// </summary>
    private Vector3 FindBestEscapePoint(Transform casterTransform, Transform playerTransform)
    {
        Vector3 origin = casterTransform.position;
        if (NavMesh.SamplePosition(origin, out NavMeshHit originHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            origin = originHit.position;
        }

        Vector3 playerPos = playerTransform != null ? playerTransform.position : origin;
        Vector3 bestPoint = origin;
        float bestScore = -9999f;

        float angleStep = 360f / Mathf.Max(4, sampleDirections);

        for (int i = 0; i < sampleDirections; i++)
        {
            // 加入微幅隨機角度，讓每次瞬移落點不會死板固定
            float angle = i * angleStep + Random.Range(-10f, 10f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            float targetDist = Random.Range(minTeleportDist, maxTeleportDist);

            // A. 實體牆壁射線檢測：撞牆自動往內縮
            Vector3 rayStart = origin + Vector3.up * 0.8f;
            RaycastHit[] hits = Physics.RaycastAll(rayStart, dir, targetDist, wallLayerMask, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in hits)
            {
                Transform root = h.transform.root;
                if (root == casterTransform.root || root.CompareTag("Enemy") || root.CompareTag("Player"))
                    continue;

                float pulledDist = Mathf.Max(0.5f, h.distance - wallPullbackOffset);
                if (pulledDist < targetDist)
                {
                    targetDist = pulledDist;
                }
            }

            Vector3 desiredPos = origin + dir * targetDist;

            // B. NavMesh 邊界阻斷內縮
            if (NavMesh.Raycast(origin, desiredPos, out NavMeshHit navWallHit, NavMesh.AllAreas))
            {
                desiredPos = Vector3.MoveTowards(navWallHit.position, origin, wallPullbackOffset);
            }

            // C. 吸附至合法 NavMesh 並進行逃生評分
            if (NavMesh.SamplePosition(desiredPos, out NavMeshHit finalHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                Vector3 candidate = finalHit.position;

                // 防止 SamplePosition 跨牆吸到隔壁房間
                if (NavMesh.Raycast(origin, candidate, out NavMeshHit recheckHit, NavMesh.AllAreas))
                {
                    candidate = Vector3.MoveTowards(recheckHit.position, origin, 0.5f);
                }

                float distFromCaster = Vector3.Distance(origin, candidate);
                float distFromPlayer = Vector3.Distance(playerPos, candidate);

                // 若某個方向被牆壁擋死（離法師原位不到 2 公尺），大幅扣分淘汰！
                if (distFromCaster < 2.0f) continue;

                // 評分公式：離玩家越遠越好 + 加上一點點隨機權重讓走位更難預測
                float score = distFromPlayer * 1.5f + distFromCaster * 0.5f + Random.Range(0f, 1.5f);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = candidate;
                }
            }
        }

        return bestPoint;
    }
}