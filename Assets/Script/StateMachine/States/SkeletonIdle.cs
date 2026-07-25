using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SkeletonIdle", menuName ="StateMachine/States/SkeletonIdle")]
public class SkeletonIdle : StateActionSO
{
    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        NavMeshAgent agent = stateMachineSystem.agent;
        Animator animator = stateMachineSystem.animator;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();

        if(animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetBool("Attack", false);
        }
    }
    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        
    }
    public override void OnExit(StateMachineSystem stateMachineSystem)
    {
        NavMeshAgent agent = stateMachineSystem.agent;
        agent.isStopped = false;
    }
}
