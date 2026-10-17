using UnityEngine;

public class ShopPotion : ShopItem
{
    Potion potion;
    public override void initItem()
    {
        potion = GetComponent<Potion>();
        if(potion != null)
        {
            itemName = potion.itemName;
            price = potion.Price;
        }
    }
}
