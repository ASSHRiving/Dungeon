using UnityEngine;

[CreateAssetMenu(fileName = "SkeletonAttack", menuName = "Ability/Skeleton/SkeletonAttack")]
public class SkeletonAttack : AbilityBase
{
    private string[] triggerName = {"Attack1", "Attack2", "Attack3"};
    public override void ExecuteSkill(StateMachineSystem system)
    {
        animTriggerName = triggerName[Random.Range(0, triggerName.Length)];
        system.animator.SetTrigger(animTriggerName);
    }
}
