using UnityEngine;

[CreateAssetMenu(fileName = "ToCombatCondition", menuName ="StateMachine/Conditions/ToCombatCondition")]
public class ToCombatCondition : ConditionSO
{
    public override bool ConditionSetUp(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem _combat = stateMachineSystem.GetComponentInChildren<EnemyCombatSystem>();
        return _combat.GetCurrentTarget() != null;
    }
}
