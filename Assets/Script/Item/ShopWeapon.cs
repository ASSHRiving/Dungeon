using UnityEngine;

public class ShopWeapon : ShopItem
{
    private Weapon weapon;
    public override void initItem()
    {
        weapon = GetComponent<Weapon>();
        if(weapon != null)
        {
            itemName = weapon.weaponName;
            if (weapon.WeaponData != null && weapon.WeaponData.price > 0)
            {
                price = weapon.WeaponData.price;
            }
        }
    }
}
