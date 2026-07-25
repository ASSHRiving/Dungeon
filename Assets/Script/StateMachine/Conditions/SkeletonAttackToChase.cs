using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonAttackToChase", menuName ="StateMachine/Conditions/SkeletonAttackToChase")]
public class SkeletonAttackToChase : ConditionSO
{
    public override bool ConditionSetUp(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        return combat.GetCurrentTarget() == null || combat.GetCurrentTargetDistance() > 2f + 0.1f;
    }
}
