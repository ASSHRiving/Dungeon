using UnityEngine;

public class ShopTableInteract : MonoBehaviour, IInteractable
{

    [SerializeField] private GameObject itemPrefab;

    string IInteractable.interactableName => itemPrefab.name;
    private void Start()
    {
        itemPrefab.SetActive(false);
    }

    public void Interact(Transform player)
    {
        // 觸發確認對話框事件
        UIEvents.ConfirmDialogRequested("你想要購買這個物品嗎？", () =>
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
                }
                else
                {
                    Debug.Log("金幣不足，無法購買！");
                }
            }
        });
    }

}
