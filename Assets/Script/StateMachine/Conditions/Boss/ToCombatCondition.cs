using UnityEngine;

[CreateAssetMenu(fileName = "ToCombatCondition", menuName ="StateMachine/Conditions/ToCombatCondition")]
public class ToCombatCondition : ConditionSO
{
    public override bool ConditionSetUp(StateMachineSystem stateMachineSystem)
    {
        return stateMachineSystem.combat.GetCurrentTarget() != null;
    }
}
