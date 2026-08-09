using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonAttackToIdle", menuName ="StateMachine/Conditions/SkeletonAttackToIdle")]
public class SkeletonAttackToIdle : ConditionSO
{
    public override bool ConditionSetUp(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        return combat.GetCurrentTarget() == null;
    }
}
