using UnityEngine;

public class Gun : RangedWeapon
{
    protected override void ExecuteShoot(Animator anim){
        anim.SetTrigger("Attack");
        anim.SetBool("InCombat", true);
    }
}
