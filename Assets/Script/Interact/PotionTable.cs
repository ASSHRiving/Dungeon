using UnityEngine;
using System.Collections.Generic;

public class PotionTable : MonoBehaviour, IInteractable
{
    [SerializeField] private List<GameObject> itemList;
    [SerializeField] Transform itemPoint;

    private GameObject itemGO;
    ShopItem item;

    public string interactableName
    {
        get
        {
            if(itemGO == null) return "空的購物桌"; // 如果物品尚未生成，返回 "空的購物桌"
            else
            {
                if (item != null)
                {
                    return $"購買 \n{item.equipmentName}";
                }
                return $"購買 \n{itemGO.name}"; // 如果物品沒有實現 IInteractable，則返回物品的名稱
            }
        }
    }

    public void Interact(Transform player)
    {
        throw new System.NotImplementedException();
    }
}
