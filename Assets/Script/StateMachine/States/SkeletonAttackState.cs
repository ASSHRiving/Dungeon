using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "SkeletonAttack", menuName ="StateMachine/States/SkeletonAttack")]
public class SkeletonAttackState : StateActionSO
{
    string[] AttackAnimationNames = new string[] { "Attack1", "Attack2" , "Attack3" };
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
            int randomAttack = Random.Range(0, AttackAnimationNames.Length);
            animator.Play(AttackAnimationNames[randomAttack]);
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
        
    }
    public override void OnExit(StateMachineSystem stateMachineSystem)
    {
        NavMeshAgent agent = stateMachineSystem.agent;
        agent.isStopped = false;
    }
}
