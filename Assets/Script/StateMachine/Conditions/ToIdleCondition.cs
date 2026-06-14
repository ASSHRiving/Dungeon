using UnityEngine;

[CreateAssetMenu(fileName = "ToIdleCondition", menuName ="StateMachine/Conditions/ToIdleCondition")]
public class ToIdleCondition : ConditionSO
{
    public override bool ConditionSetUp(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem _combat = stateMachineSystem.GetComponentInChildren<EnemyCombatSystem>();
        return _combat.GetCurrentTarget() == null;
    }
}
