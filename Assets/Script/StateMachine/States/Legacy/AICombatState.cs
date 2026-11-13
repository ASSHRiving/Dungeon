using UnityEngine;

[CreateAssetMenu(fileName = "AICombat", menuName ="StateMachine/States/AICombat")]
public class AICombatState : StateActionSO
{
    

    
    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        Animator animator = stateMachineSystem.animator;
        animator.Play("Ready");
    }

    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        NoCombatMove(stateMachineSystem);
    }

    private void NoCombatMove(StateMachineSystem stateMachineSystem)
    {
        Animator animator = stateMachineSystem.animator;
        EnemyCombatSystem combat = stateMachineSystem.combat;
        EnemyMovementSystem movement = stateMachineSystem.movement;
        int randomHorizontal = GetRandomHorizontal();

        //非戰鬥時邏輯
        if (animator.CheckAnimationTag("Motion"))
        {
            if(combat.GetCurrentTargetDistance() < 2.5f + 0.1f)
            {
                //太進後退
                movement.CharacterMoveInterface(-movement.transform.forward, 1.4f, true);
                animator.SetFloat(verticalID, -1, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, 0, 0.25f, Time.deltaTime);

                randomHorizontal = GetRandomHorizontal();

                if(combat.GetCurrentTargetDistance() < 1.5 + 0.05f)
                {
                    animator.Play("AutoAttack_1", 0, 0);
                    randomHorizontal = GetRandomHorizontal();
                }
            }else if(combat.GetCurrentTargetDistance() > 2.5f + 0.1f && combat.GetCurrentTargetDistance() < 6.1f + 0.5f)
            {
                movement.CharacterMoveInterface(movement.transform.right * ((randomHorizontal == 0)? 1 : randomHorizontal), 1.4f, true);
                animator.SetFloat(verticalID, 0, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, ((randomHorizontal == 0)? 1 : randomHorizontal), 0.25f, Time.deltaTime);
            }else if(combat.GetCurrentTargetDistance() > 6.1f + 0.5f)
            {
                movement.CharacterMoveInterface(movement.transform.forward, 1.4f, true);
                animator.SetFloat(verticalID, 1, 0.25f, Time.deltaTime);
                animator.SetFloat(horizontalID, 0, 0.25f, Time.deltaTime);

                randomHorizontal = GetRandomHorizontal();
            }
        }
        else
        {
            animator.SetFloat(verticalID, 0);
            animator.SetFloat(horizontalID, 0);
            animator.SetFloat(runID, 0f);
        }
    }

    private int GetRandomHorizontal() => Random.Range(-1, 2);
}
