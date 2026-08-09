using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "BossChase", menuName = "StateMachine/States/Boss/BossChase")]
public class BossChase : StateActionSO
{
    [Header("距離設定")]
    [SerializeField] private float minDistance = 2.4f;      // 太近界線 (低於此距離後退)
    [SerializeField] private float maxDistance = 6.6f;      // 太遠界線 (高於此距離追擊)
    [SerializeField] private float attackDistance = 2f;   // 攻擊距離

    [Header("移動速度")]
    [SerializeField] private float moveSpeed = 1.4f;
    [SerializeField] private float strafeSpeed = 1.4f;

    [Header("計時設定")]
    [SerializeField] private float strafeChangeInterval = 1.5f; // 每 1.5 秒換一次左右方向
    [SerializeField] private float AttackColddown = 5f;

    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        NavMeshAgent agent = stateMachineSystem.agent;
        if(agent != null)
        {
            agent.isStopped = false;
            agent.speed = moveSpeed;
        }
        stateMachineSystem.strafeTimer = 0f;
        stateMachineSystem.attackTimer = 0f;
        stateMachineSystem.UpdateRandomHorizontal();
    }
    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        Animator animator = stateMachineSystem.animator;
        NavMeshAgent agent = stateMachineSystem.agent;
        if(combat == null || animator == null || agent == null || combat.GetCurrentTarget() == null) return;

        combat.LockOnCurrentTarget();
        Transform targetTransform = combat.GetCurrentTarget();
        Transform selfTransform = stateMachineSystem.transform;
        float distance = combat.GetCurrentTargetDistance();
        stateMachineSystem.strafeTimer += Time.deltaTime;
        stateMachineSystem.attackTimer += Time.deltaTime;

        if (animator.CheckAnimationTag("Motion"))
        {
            //近距離後退
            if(distance < minDistance)
            {
                Vector3 retreatDir = (selfTransform.position - targetTransform.position).normalized;
                Vector3 destination = selfTransform.position + retreatDir * 2f;
                agent.SetDestination(destination);

                animator.SetFloat(verticalID, -1f, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, 0f, 0.25f, Time.deltaTime);

            }
            //中距離徘徊
            else if(distance >= minDistance && distance <= maxDistance)
            {
                // 到達時間間隔就隨機更換左右方向
                if (stateMachineSystem.strafeTimer >= strafeChangeInterval)
                {
                    stateMachineSystem.UpdateRandomHorizontal();
                    stateMachineSystem.strafeTimer = 0f;
                }

                Vector3 strafeDir = selfTransform.right * stateMachineSystem.randomHorizontal;
                Vector3 destination = selfTransform.position + strafeDir * 2f;
                agent.SetDestination(destination);
                animator.SetFloat(verticalID, 0f, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, stateMachineSystem.randomHorizontal, 0.25f, Time.deltaTime);
            }
            //遠距離追擊
            else if(distance > maxDistance)
            {
                agent.SetDestination(targetTransform.position);
                animator.SetFloat(verticalID, 1f, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, 0f, 0.25f, Time.deltaTime);

                stateMachineSystem.strafeTimer = 0f;
            }
        }
        else
        {
            agent.isStopped = true;

            animator.SetFloat(verticalID, 0f);
            animator.SetFloat(horizontalID, 0f);
            animator.SetFloat(runID, 0f);
        }
    }
    public override void OnExit(StateMachineSystem stateMachineSystem)
    {
        
    }
}
