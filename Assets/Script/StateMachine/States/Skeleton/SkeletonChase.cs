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
        Animator animator = stateMachineSystem.animator;
        NavMeshAgent agent = stateMachineSystem.agent;
        if(combat == null || animator == null || agent == null || combat.GetCurrentTarget() == null) return;

        stateMachineSystem.transform.root.rotation = stateMachineSystem.transform.LockOnTarget(combat.GetCurrentTarget(),stateMachineSystem.transform.root.transform,50f);
        float distance = combat.GetCurrentTargetDistance();

        if(animator.CheckAnimationTag("Motion"))
        {
            AbilityBase readySkill = stateMachineSystem.SelectReadySkill(distance);
            if (readySkill != null)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
                stateMachineSystem.UseSkill(readySkill); 
                return;
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(combat.GetCurrentTarget().position);
                if(animator != null)
                {
                    animator.SetFloat("Speed", agent.velocity.magnitude, 0.1f, Time.deltaTime);
                }
            }
        }
        else
        {
            agent.isStopped = true;

            animator.SetFloat("Speed", 0);
        }
    }
}
