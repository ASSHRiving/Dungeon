using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonChaseToIdle", menuName ="StateMachine/Conditions/SkeletonChaseToIdle")]
public class SkeletonChaseToIdle : ConditionSO
{
    public override bool ConditionSetUp(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        return combat.GetCurrentTarget() == null;
    }
}
