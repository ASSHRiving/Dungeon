using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SkeletonIdle", menuName = "StateMachine/States/SkeletonIdle")]
public class SkeletonIdle : StateActionSO
{
    [SerializeField] private float stoppingDistance = 0.3f;
    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        NavMeshAgent agent = stateMachineSystem.agent;
        EnemyCombatSystem combat = stateMachineSystem.combat;

        if (agent != null && combat != null && combat.GetSpawnPoint() != null)
        {
            // 1. 重新啟用 Agent（確保 Agent 正常運作）
            agent.isStopped = false;
            
            // 2. 命令 Agent 跑回出生點
            agent.SetDestination(combat.GetSpawnPoint().position); // 確保目標點在地面上
        }
    }

    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        NavMeshAgent agent = stateMachineSystem.agent;
        Animator animator = stateMachineSystem.animator;

        if (agent == null) return;

        // 3. 檢查 NavMeshAgent 是否已經計算好路徑，且開始移動
        if (!agent.pathPending)
        {
            // 判斷是否「已經抵達目的地」：剩餘距離 <= 設定的停止距離
            if (agent.remainingDistance <= stoppingDistance)
            {
                // 到了出生點：徹底停止 Agent 並清除路徑
                agent.isStopped = true;
                agent.velocity = Vector3.zero; // 將慣性速度歸零，防止微幅漂移
                
                // 讓動畫切換回待機 (Idle) 狀態
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f);
                }
            }
            else
            {
                // 還在回出生點的路上：播放移動動畫
                if (animator != null)
                {
                    // 也可以根據目前實際速度傳入：agent.velocity.magnitude
                    animator.SetFloat("Speed", 1f); 
                }
            }
        }
    }

    public override void OnExit(StateMachineSystem stateMachineSystem)
    {
        // 離開 Idle 狀態（例如重新發現玩家）時，確保 reset 狀態
        NavMeshAgent agent = stateMachineSystem.agent;
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }
}