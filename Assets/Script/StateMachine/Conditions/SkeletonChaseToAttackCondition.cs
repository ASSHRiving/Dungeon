using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonChaseToAttackCondition", menuName ="StateMachine/Conditions/SkeletonChaseToAttackCondition")]
public class SkeletonChaseToAttackCondition : ConditionSO
{
    public override bool ConditionSetUp(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        return combat.GetCurrentTarget() != null && combat.GetCurrentTargetDistance() < 1.5f + 0.1f;
    }
}
