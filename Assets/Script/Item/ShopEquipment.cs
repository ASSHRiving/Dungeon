using UnityEngine;

public class ShopEquipment : ShopItem
{
    private Equipment equipment;

    public override void initItem()
    {
        equipment = GetComponent<Equipment>();
        if (equipment != null)
        {
            // 若尚未設定數據（例如非商店桌生成時），預設為初級 T1
            if (equipment.EquipmentData == null && equipment.TieredData != null && equipment.TieredData.Count > 0)
            {
                equipment.SetIndex(0);
            }

            itemName = equipment.equipmentName;
            price = equipment.price;
        }
    }

    /// <summary>
    /// 設定此裝備商品的特定階級 (0: T1初級, 1: T2中級, 2: T3高級)
    /// </summary>
    public void SetTier(int tier)
    {
        equipment = GetComponent<Equipment>();
        if (equipment != null)
        {
            equipment.SetIndex(tier);
            itemName = equipment.equipmentName;
            price = equipment.price;
        }
    }
}
