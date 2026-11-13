using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonCharge", menuName = "Ability/Skeleton/SkeletonCharge")]
public class SkeletonCharge : AbilityBase
{
    public override void ExecuteSkill(StateMachineSystem system)
    {
        system.animator.SetTrigger(animTriggerName);
    }
}
