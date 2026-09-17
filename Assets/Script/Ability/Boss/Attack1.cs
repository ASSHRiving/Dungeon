using UnityEngine;

[CreateAssetMenu(fileName = "Attack1", menuName = "Ability/Boss/Attack1")]
public class Attack1 : AbilityBase
{
    public override void ExecuteSkill(StateMachineSystem system)
    {
        system.animator.SetTrigger(animTriggerName);
    }
}
