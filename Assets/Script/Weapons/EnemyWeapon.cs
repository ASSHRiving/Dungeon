using UnityEngine;

public class EnemyWeapon : Weapon
{
    void Awake()
    {
        weaponName = "敵人武器";
        damage = 10f;
        attackRate = 0.5f;
        combatStateDuration = 3f;
    }
    public override void Attack(Animator anim)
    {
        
    }
}
