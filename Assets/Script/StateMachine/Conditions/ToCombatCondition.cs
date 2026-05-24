using UnityEngine;

[CreateAssetMenu(fileName = "ToCombatCondition", menuName ="StateMachine/Conditions/ToCombatCondition")]
public class ToCombatCondition : ConditionSO
{
    public override bool ConditionSetUp()
    {
        return _combat.GetCurrentTarget() != null;
    }
}
