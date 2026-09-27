using UnityEngine;

[CreateAssetMenu(fileName = "Attack2", menuName = "Ability/Boss/Attack2")]
public class Attack2 : AbilityBase
{
    public override void ExecuteSkill(StateMachineSystem system)
    {
        system.animator.SetTrigger(animTriggerName);
    }
}
