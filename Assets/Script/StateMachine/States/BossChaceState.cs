using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "BossChaceState", menuName = "StateMachine/States/BossChaceState")]
public class BossChaceState : StateActionSO
{
    [Header("距離設定")]
    [SerializeField] private float minDistance = 2.4f;      // 太近界線 (低於此距離後退)
    [SerializeField] private float maxDistance = 6.6f;      // 太遠界線 (高於此距離追擊)
    [SerializeField] private float attackDistance = 2f;   // 攻擊距離

    [Header("移動速度")]
    [SerializeField] private float moveSpeed = 1.4f;

    [Header("徘徊計時設定")]
    [SerializeField] private float strafeChangeInterval = 1.5f; // 每 1.5 秒換一次左右方向


    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        NavMeshAgent agent = stateMachineSystem.agent;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = moveSpeed;
        }

        stateMachineSystem.strafeTimer = 0f;
        stateMachineSystem.UpdateRandomHorizontal();
    }

    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        NoCombatMove(stateMachineSystem);
    }

    private void NoCombatMove(StateMachineSystem stateMachineSystem)
    {
        Animator animator = stateMachineSystem.animator;
        EnemyCombatSystem combat = stateMachineSystem.combat;
        NavMeshAgent agent = stateMachineSystem.agent;
        Transform selfTransform = stateMachineSystem.transform;

        if (combat == null || agent == null || combat.GetCurrentTarget() == null) return;

        Transform targetTransform = combat.GetCurrentTarget(); // 假設 CombatSystem 存有 Target Transform
        float currentDistance = combat.GetCurrentTargetDistance();

        // 計時器更新：在中距離時控制多久切換一次左右徘徊方向
        stateMachineSystem.strafeTimer += Time.deltaTime;

        if (animator.CheckAnimationTag("Motion"))
        {
            // 情況 1：太近 -> 後退 (往目標的反方向點移動)
            if (currentDistance < minDistance)
            {
                // 計算從玩家指向敵人的方向 (即後退方向)
                Vector3 retreatDir = (selfTransform.position - targetTransform.position).normalized;
                Vector3 targetPosition = selfTransform.position + retreatDir * 2f;

                SetAgentDestination(agent, targetPosition);

                animator.SetFloat(verticalID, -1f, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, 0f, 0.25f, Time.deltaTime);

                // 非常近時觸發攻擊
                if (currentDistance < attackDistance)
                {
                    animator.Play("AutoAttack_1", 0, 0);
                    stateMachineSystem.UpdateRandomHorizontal();
                }
            }
            // 情況 2：中距離 -> 左右徘徊 (Strafe)
            else if (currentDistance >= minDistance && currentDistance <= maxDistance)
            {
                // 到達時間間隔就隨機更換左右方向
                if (stateMachineSystem.strafeTimer >= strafeChangeInterval)
                {
                    stateMachineSystem.UpdateRandomHorizontal();
                    stateMachineSystem.strafeTimer = 0f;
                }

                // 算出相對於 enemy 正面的左右側目標點
                Vector3 strafeDir = selfTransform.right * stateMachineSystem.randomHorizontal;
                Vector3 targetPosition = selfTransform.position + strafeDir * 2f;

                SetAgentDestination(agent, targetPosition);

                // 保持 Boss 面向玩家（NavMesh 橫向移動時，建議手動讓轉向面對玩家）
                RotateTowards(selfTransform, targetTransform.position);

                animator.SetFloat(verticalID, 0f, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, stateMachineSystem.randomHorizontal, 0.25f, Time.deltaTime);
            }
            // 情況 3：太遠 -> 直接追擊玩家
            else
            {
                SetAgentDestination(agent, targetTransform.position);

                animator.SetFloat(verticalID, 1f, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, 0f, 0.25f, Time.deltaTime);

                stateMachineSystem.strafeTimer = 0f;
            }
        }
        else
        {
            // 停下 NavMesh 避免播放攻擊動畫時還在滑行
            agent.isStopped = true;

            animator.SetFloat(verticalID, 0f);
            animator.SetFloat(horizontalID, 0f);
            animator.SetFloat(runID, 0f);
        }
    }

    /// <summary>
    /// 設定 NavMesh 移動點並確保 Agent 處於啟用狀態
    /// </summary>
    private void SetAgentDestination(NavMeshAgent agent, Vector3 destination)
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    /// <summary>
    /// 使 Boss 平滑面向目標 (特別適用於中距離左右徘徊時)
    /// </summary>
    private void RotateTowards(Transform self, Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - self.position).normalized;
        direction.y = 0; // 忽略高低差
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            self.rotation = Quaternion.Slerp(self.rotation, targetRotation, Time.deltaTime * 8f);
        }
    }
}