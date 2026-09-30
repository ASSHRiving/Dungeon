using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "BossChase", menuName = "StateMachine/States/Boss/BossChase")]
public class BossChase : StateActionSO
{
    [Header("戰鬥距離判定")]
    [SerializeField] private float minDistance = 2.4f;      // 太近界線 (後退)
    [SerializeField] private float maxDistance = 6.6f;      // 太遠界線 (追擊)

    [Header("移動速度")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float strafeSpeed = 1.6f;
    [SerializeField] private float retreatSpeed = 1.8f;

    [Header("徘徊計時")]
    [SerializeField] private float strafeChangeInterval = 2.0f;

    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        Animator animator = stateMachineSystem.animator;
        if (animator != null)
        {
            animator.SetFloat(lockOnID, 1);
            animator.Play("Ready");
        }

        stateMachineSystem.strafeTimer = 0f;
        stateMachineSystem.attackTimer = 0f;
        stateMachineSystem.UpdateRandomHorizontal();

        // 確保 Agent 啟用並重置
        if (stateMachineSystem.agent != null)
        {
            stateMachineSystem.agent.isStopped = false;
        }
    }

    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        Animator animator = stateMachineSystem.animator;
        EnemyMovementSystem movement = stateMachineSystem.movement;
        NavMeshAgent agent = stateMachineSystem.agent;

        if (combat == null || animator == null || movement == null || agent == null || combat.GetCurrentTarget() == null) 
            return;

        Transform targetTransform = combat.GetCurrentTarget();
        Transform selfTransform = stateMachineSystem.transform;
        float distance = combat.GetCurrentTargetDistance();

        stateMachineSystem.strafeTimer += Time.deltaTime;
        stateMachineSystem.attackTimer += Time.deltaTime;

        // 1. 面向鎖定目標 (在可旋轉的狀態下)
        if (animator.CheckAnimationTag("Motion") || 
           (animator.CheckAnimationTag("Attack") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.4f) || 
            animator.CheckAnimationTag("Aim"))
        {
            selfTransform.rotation = stateMachineSystem.transform.LockOnTarget(targetTransform, selfTransform, 10f);
        }

        // 2. 優先檢查技能施放
        if (animator.CheckAnimationTag("Motion"))
        {
            AbilityBase readySkill = stateMachineSystem.SelectReadySkill(distance);
            if (readySkill != null)
            {
                agent.ResetPath();
                stateMachineSystem.UseSkill(readySkill);
                return;
            }
        }

        // 3. 移動決策處理
        if (animator.CheckAnimationTag("Motion"))
        {
            HandleTacticalMovement(stateMachineSystem, targetTransform, distance);
        }
        else
        {
            // 處於攻擊後搖或不能移動的狀態，停止 NavMesh 移動
            agent.ResetPath();
            animator.SetFloat(verticalID, 0f, 0.1f, Time.deltaTime);
            animator.SetFloat(horizontalID, 0f, 0.1f, Time.deltaTime);
        }
    }

    private void HandleTacticalMovement(StateMachineSystem sm, Transform target, float distance)
    {
        NavMeshAgent agent = sm.agent;
        Animator animator = sm.animator;
        Transform self = sm.transform;

        // A. 距離太近：拉開距離 (Retreat)
        if (distance < minDistance)
        {
            agent.speed = retreatSpeed;
            Vector3 retreatDir = (self.position - target.position).normalized;
            Vector3 rawTargetPos = self.position + retreatDir * 2f;

            // 安全取點：防止退入牆體
            if (TryGetValidNavMeshPoint(rawTargetPos, out Vector3 validPos))
            {
                agent.SetDestination(validPos);
            }

            animator.SetFloat(verticalID, -1f, 0.2f, Time.deltaTime);
            animator.SetFloat(horizontalID, 0f, 0.2f, Time.deltaTime);
        }
        // B. 戰鬥優勢距離：左右側移徘徊 (Strafe)
        else if (distance >= minDistance && distance <= maxDistance)
        {
            agent.speed = strafeSpeed;

            if (sm.strafeTimer >= strafeChangeInterval)
            {
                sm.UpdateRandomHorizontal();
                sm.strafeTimer = 0f;
            }

            Vector3 strafeDir = self.right * sm.randomHorizontal;
            Vector3 rawTargetPos = self.position + strafeDir * 2.5f;

            // 安全取點：若側邊有障礙，自動轉向另一側
            if (TryGetValidNavMeshPoint(rawTargetPos, out Vector3 validPos))
            {
                agent.SetDestination(validPos);
            }
            else
            {
                sm.randomHorizontal *= -1; // 撞牆時立即反向
            }

            animator.SetFloat(verticalID, 0f, 0.2f, Time.deltaTime);
            animator.SetFloat(horizontalID, sm.randomHorizontal, 0.2f, Time.deltaTime);
        }
        // C. 距離太遠：快速追擊 (Chase)
        else
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(target.position);

            animator.SetFloat(verticalID, 1f, 0.2f, Time.deltaTime);
            animator.SetFloat(horizontalID, 0f, 0.2f, Time.deltaTime);

            sm.strafeTimer = 0f;
        }
    }

    /// <summary>
    /// 確保目標點位於合法的 NavMesh 上，避免撞牆發呆
    /// </summary>
    private bool TryGetValidNavMeshPoint(Vector3 targetPos, out Vector3 result)
    {
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = targetPos;
        return false;
    }

    public override void OnExit(StateMachineSystem stateMachineSystem)
    {
        if (stateMachineSystem.agent != null && stateMachineSystem.agent.hasPath)
        {
            stateMachineSystem.agent.ResetPath();
        }
    }
}