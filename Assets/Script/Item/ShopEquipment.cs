using UnityEngine;

public class ShopEquipment : ShopItem
{
    Equipment equipment;
    public override void initItem()
    {
        equipment = GetComponent<Equipment>();
        if(equipment != null)
        {
            equipment.SetIndex(Random.Range(1,3));

            itemName = equipment.equipmentName;
            price = equipment.price;
        }
    }
}
