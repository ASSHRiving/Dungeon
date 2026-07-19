using UnityEngine;

public class EnemyWeapon : Weapon
{
    void Awake()
    {
        weaponName = "敵人武器";
        damage = 10f;
    }
    public override void Attack(Animator anim)
    {
        
    }
}
