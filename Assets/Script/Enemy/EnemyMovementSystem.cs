using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovementSystem : CharacterMovementBase
{
    private NavMeshAgent agent;

    [Header("轉向平滑")]
    [SerializeField] private float rotationSpeed = 10f;


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
        }
        else
        {
            // 停步時傳入 Vector3.zero，仍保留重力貼地
            CharacterMoveInterface(Vector3.zero, 0f, useGravity: true);
        }

        // 3. 同步 NavMeshAgent 內部位置至 CharacterController 實際位置
        SyncAgentPosition();
    }

    public void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    public void SyncAgentPosition()
    {
        if (agent != null)
        {
            agent.nextPosition = transform.position;
        }
    }
}