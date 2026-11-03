using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovementSystem : CharacterMovementBase
{
    private NavMeshAgent agent;

    [Header("轉向與尋路")]
    [SerializeField] private float navSampleRadius = 1.5f;
    [SerializeField] private float minValidMoveDistance = 0.3f;
    [SerializeField] private float animDampTime = 0.2f;


    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();

        // 核心解耦：關閉 Agent 自動更新 Transform
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    protected override void Update()
    {
        // 執行基類的地面檢測與重力累加
        base.Update();

        // 2. 常態尋路移動：將 Agent 的 desiredVelocity 轉換並傳入基類介面
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
            Vector3 desiredVel = agent.desiredVelocity;
            desiredVel.y = 0f; // 垂直高度交由基類重力計算

            float calculatedSpeed = desiredVel.magnitude;
            Vector3 moveDirection = desiredVel.normalized;

            // 呼叫基類的 CharacterMoveInterface！
            CharacterMoveInterface(moveDirection, calculatedSpeed, useGravity: true);
            UpdateMovementAnimator(moveDirection);
        }
        else
        {
            // 停步時傳入 Vector3.zero，仍保留重力貼地
            CharacterMoveInterface(Vector3.zero, 0f, useGravity: true);
            UpdateMovementAnimator(Vector3.zero);
        }

        // 3. 同步 NavMeshAgent 內部位置至 CharacterController 實際位置
        SyncAgentPosition();
    }


    // =========================================================================
    // 對外公開的高階導航介面（供 StateActionSO 與 AbilityBase 決策層呼叫）
    // =========================================================================

    /// <summary>
    /// 追擊指定目標
    /// </summary>
    public void ChaseTarget(Transform target, float speed)
    {
        if (agent == null || target == null) return;
        agent.isStopped = false;
        agent.speed = speed;
        agent.SetDestination(target.position);
    }

    /// <summary>
    /// 從目標反向安全後退（若背後貼牆無合法 NavMesh 可退則回傳 false）
    /// </summary>
    public bool TryRetreatFromTarget(Transform target, float speed, float retreatStepDistance = 2.0f)
    {
        Debug.Log("TryRetreatFromTarget: target = " + target.name);
        if (agent == null || target == null) return false;

        Vector3 retreatDir = (transform.position - target.position).normalized;
        retreatDir.y = 0f;
        Vector3 rawTargetPos = transform.position + retreatDir * retreatStepDistance;

        if (TryGetValidNavMeshPoint(rawTargetPos, out Vector3 validPos))
        {
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(validPos);
            Debug.Log("TryRetreatFromTarget: Retreating to valid position = " + validPos);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 左右側移徘徊（direction 為 1 或 -1；若側方撞牆無合法點則回傳 false）
    /// </summary>
    public bool TryStrafe(int horizontalDir, float speed, float strafeStepDistance = 2.5f)
    {
        if (agent == null) return false;

        Vector3 strafeDir = transform.right * Mathf.Sign(horizontalDir);
        Vector3 rawTargetPos = transform.position + strafeDir * strafeStepDistance;

        if (TryGetValidNavMeshPoint(rawTargetPos, out Vector3 validPos))
        {
            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(validPos);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 立即停止尋路移動
    /// </summary>
    public void StopMoving()
    {
        if (agent != null && agent.enabled && agent.hasPath)
        {
            agent.ResetPath();
        }
        UpdateMovementAnimator(Vector3.zero);
    }



    public void LockOnTarget(Transform target, float customLerpSpeed = 10f)
    {
       if(target == null) return;
       transform.rotation = transform.LockOnTarget(target, transform, customLerpSpeed);
    }

    /// <summary>
    /// 更新 Animator 的移動參數(HorizontalID, VerticalID)，讓動畫與實際移動方向同步
    /// </summary>
    /// <param name="worldMoveDirection"></param>
    private void UpdateMovementAnimator(Vector3 worldMoveDirection)
    {
        if (animator == null) return;

        if (worldMoveDirection.sqrMagnitude > 0.001f)
        {
            Vector3 localDir = transform.InverseTransformDirection(worldMoveDirection.normalized);
            animator.SetFloat(horizontalID, localDir.x, animDampTime, Time.deltaTime);
            animator.SetFloat(verticalID, localDir.z, animDampTime, Time.deltaTime);
        }
        else
        {
            animator.SetFloat(horizontalID, 0f, 0.1f, Time.deltaTime);
            animator.SetFloat(verticalID, 0f, 0.1f, Time.deltaTime);
        }
    }
    /// <summary>
    /// 嘗試取得合法的 NavMesh 點位，避免撞牆或貼牆時原地踏步
    /// </summary>
    /// <param name="targetPos"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private bool TryGetValidNavMeshPoint(Vector3 targetPos, out Vector3 result)
    {
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
        {
            // 確保取樣點與當前位置有足夠距離，避免貼牆時取回原地導致原地踏步
            if (Vector3.Distance(transform.position, hit.position) >= minValidMoveDistance)
            {
                result = hit.position;
                return true;
            }
        }

        result = targetPos;
        return false;
    }

    public void SyncAgentPosition()
    {
        if (agent != null)
        {
            agent.nextPosition = transform.position;
        }
    }
}