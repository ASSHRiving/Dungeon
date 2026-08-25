using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SkeletonAttack", menuName ="StateMachine/States/SkeletonAttack")]
public class SkeletonAttackState : StateActionSO
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
        }
    }

    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        NavMeshAgent agent = stateMachineSystem.agent;
        
        if(combat.GetCurrentTarget() != null)
        {
            Transform target = combat.GetCurrentTarget();
            Vector3 dir = (target.position - stateMachineSystem.transform.position).normalized;
            dir.y = 0; // 保持水平轉向
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                stateMachineSystem.transform.rotation = Quaternion.Slerp(stateMachineSystem.transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

        //攻擊
        AbilityBase readySkill = stateMachineSystem.SelectReadySkill(1);
        if (readySkill != null)
        {
            agent.isStopped = true;
            stateMachineSystem.UseSkill(readySkill); 
            return;
        }      
    }
    public override void OnExit(StateMachineSystem stateMachineSystem)
    {
        NavMeshAgent agent = stateMachineSystem.agent;
        agent.isStopped = false;
    }
}
