using UnityEngine;

[CreateAssetMenu(fileName = "NormalAttack", menuName = "Ability/NormalAttack")]
public class NormalAttack : AbilityBase
{
    public override void ExecuteSkill(StateMachineSystem system)
    {
        system.animator.SetTrigger(animTriggerName);
    }
}
