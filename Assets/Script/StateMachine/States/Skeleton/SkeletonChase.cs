using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SkeletonChase", menuName ="StateMachine/States/SkeletonChase")]
public class SkeletonChase : StateActionSO
{
    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        stateMachineSystem.agent.isStopped = false;
    }
    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        NavMeshAgent agent = stateMachineSystem.agent;
        Animator animator = stateMachineSystem.animator;

        if(combat.GetCurrentTarget() == null) return;
        agent.SetDestination(combat.GetCurrentTarget().position);
        if(animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }

    }
}
