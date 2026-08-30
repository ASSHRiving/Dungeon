using UnityEngine;

public class EnemyWeapon : Weapon
{
    public override void Attack(Animator anim)
    {
        
    }
    public void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
