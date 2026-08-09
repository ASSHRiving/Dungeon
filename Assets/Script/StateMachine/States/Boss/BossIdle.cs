using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "BossIdle", menuName = "StateMachine/States/Boss/BossIdle")]
public class BossIdle : StateActionSO
{
    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        NavMeshAgent agent = stateMachineSystem.agent;
        EnemyCombatSystem combat = stateMachineSystem.combat;
        if(agent != null && combat != null && combat.GetSpawnPoint() != null)
        {
            agent.isStopped = false;
            Transform destination = combat.GetSpawnPoint();
            agent.SetDestination(destination.position);
        }
    }
    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        
    }
    public override void OnExit(StateMachineSystem stateMachineSystem)
    {
        
    }
}
