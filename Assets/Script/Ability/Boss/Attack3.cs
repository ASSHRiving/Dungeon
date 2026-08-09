using UnityEngine;

[CreateAssetMenu(fileName = "Attack3", menuName = "Ability/Boss/Attack3")]
public class Attack3 : AbilityBase
{
    public override void ExecuteSkill(StateMachineSystem system)
    {
        system.animator.SetTrigger(animTriggerName);
    }
}
