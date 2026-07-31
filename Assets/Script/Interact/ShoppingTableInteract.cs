using UnityEngine;
using System.Collections.Generic;

public class ShopTableInteract : MonoBehaviour, IInteractable
{

    [SerializeField] private List<GameObject> itemList;
    private GameObject itemGO;
    [SerializeField] Transform itemPoint;
    IInteractable interactableItem;

    public string interactableName
    {
        get
        {
            if(itemGO == null) return "空的購物桌"; // 如果物品尚未生成，返回 "空的購物桌"
            else
            {
                if (interactableItem != null)
                {
                    return $"購買 {interactableItem.interactableName}";
                }
                return $"購買 {itemGO.name}"; // 如果物品沒有實現 IInteractable，則返回物品的名稱
            }
        }
    }


    private void Start()
    {
        init();
    }
    public void init()
    {
        itemGO = Instantiate(itemList[Random.Range(0, itemList.Count)], itemPoint.position, itemPoint.rotation, itemPoint);
        itemGO.layer = LayerMask.NameToLayer("UnInteractable");
        interactableItem = itemGO.GetComponent<IInteractable>();
    }

    public void Interact(Transform player)
    {
        // 觸發確認對話框事件
        UIEvents.ConfirmDialogRequested($"你想要購買這個物品嗎？\n{interactableItem.interactableName}", () =>
        {
            PlayerBalanceSystem playerBalance = player.GetComponent<PlayerBalanceSystem>();
            if (playerBalance != null)
            {
                int itemCost = 10; // 假設物品價格為 10 金幣
                if (playerBalance.GetCoin() >= itemCost)
                {
                    playerBalance.SpendCoin(itemCost);
                    Debug.Log($"購買成功！花費 {itemCost} 金幣。");
                    // 在這裡可以添加購買成功後的邏輯，例如給玩家物品等
                    
                    foreach (Transform child in transform)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("UnInteractable");
                    }
                    itemGO.layer = LayerMask.NameToLayer("Interactable");
                    gameObject.layer = LayerMask.NameToLayer("UnInteractable");

                }
                else
                {
                    Debug.Log("金幣不足，無法購買！");
                }
            }
        });
    }

    private void DropItem()
    {
        if(itemGO == null || itemPoint == null) return;
        itemGO.layer = LayerMask.NameToLayer("Interactable");
        Rigidbody rb = itemGO.GetComponentInChildren<Rigidbody>();
        if(rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            float randomRightForce = Random.Range(-1.5f, 1.5f);
            Vector3 dir = itemPoint.up * 1f + itemPoint.forward * 1f + itemPoint.right * randomRightForce;
            rb.AddForce(dir, ForceMode.Impulse);
            float randomTorque = Random.Range(-0.05f, 0.05f);
            rb.AddTorque(new Vector3(randomTorque, randomTorque, randomTorque), ForceMode.Impulse);
        }
    }

}
