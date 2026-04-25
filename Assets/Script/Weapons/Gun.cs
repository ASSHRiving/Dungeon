using UnityEngine;

public class Gun : ShootWeapon
{
    protected override void ExecuteShoot(Animator anim){
        anim.SetTrigger("Attack");
        anim.SetBool("InCombat", true);
    }
}
