using UnityEngine;

[CreateAssetMenu(fileName = "BossCyclone", menuName = "Ability/Boss/BossCyclone")]
public class BossCyclone : AbilityBase
{
    public override void ExecuteSkill(StateMachineSystem system)
    {
        system.animator.SetTrigger(animTriggerName);
    }
}
