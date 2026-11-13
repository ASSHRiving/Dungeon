using UnityEngine;

public class ShopPotion : ShopItem
{
    private Potion potion;
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
