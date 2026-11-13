using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonIdleToChase", menuName ="StateMachine/Conditions/SkeletonIdleToChase")]
public class SkeletonIdleToChase : ConditionSO
{
    public override bool ConditionSetUp(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        return combat.GetCurrentTarget() != null;
    }
}
