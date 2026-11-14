using UnityEngine;

public class RollEnter : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerMovementSystem movement = animator.GetComponentInParent<PlayerMovementSystem>();
        CharacterCombatBase combat = animator.GetComponent<CharacterCombatBase>();
        if (movement != null)
        {
            movement.OnRollEnter();
        }
        if(combat != null)
        {
            if (combat.currentWeapon != null) combat.currentWeapon.combo = 0;
            combat.canAttack = true;
            combat.inAttack = false;
        }
    }
}
