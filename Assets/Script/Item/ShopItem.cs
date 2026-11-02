using System.Collections.Generic;
using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public string itemName;
    public int price;
    [Min(0f)] public float weight = 1f;
    public string message => $"你要購買這個物品嗎？\n" + itemName + "\n" + price + "金幣";
    public virtual void initItem()
    {
        
    } 
}
