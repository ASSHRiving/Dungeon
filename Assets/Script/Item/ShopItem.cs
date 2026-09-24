using System.Collections.Generic;
using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public string itemName;
    public int price;
    public string message => $"你要購買這個物品嗎？\n" + itemName + "\n" + price + "金幣";
    public virtual void initItem()
    {
        
    } 
}
