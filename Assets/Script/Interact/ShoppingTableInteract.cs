using UnityEngine;
using System.Collections.Generic;

public class ShopTableInteract : MonoBehaviour, IInteractable
{

    [SerializeField] private List<GameObject> itemList;
    [SerializeField] private Transform itemPoint;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clip;

    private GameObject itemGO;
    private ShopItem item;

    public string interactableName
    {
        get
        {
            if(itemGO == null) return "空的購物桌"; // 如果物品尚未生成，返回 "空的購物桌"
            else
            {
                if (item != null)
                {
                    return $"購買 \n{item.itemName}";
                }
                return $"購買 \n{itemGO.name}"; // 如果物品沒有實現 IInteractable，則返回物品的名稱
            }
        }
    }

    private void Start()
    {
        init();
    }
    public void init()
    {
        GameObject selectedPrefab = GetWeightedRandomItem();
        if (selectedPrefab == null) return;

        itemGO = Instantiate(selectedPrefab, itemPoint);
        itemGO.layer = LayerMask.NameToLayer("UnInteractable");
        item = itemGO.GetComponent<ShopItem>();
        if (item != null)
        {
            item.initItem();
        }
    }

    private GameObject GetWeightedRandomItem()
    {
        if (itemList == null || itemList.Count == 0) return null;

        float totalWeight = 0f;
        foreach (GameObject prefab in itemList)
        {
            if (prefab == null) continue;
            ShopItem shopItem = prefab.GetComponent<ShopItem>();
            float w = (shopItem != null) ? shopItem.weight : 1f;
            if (w > 0f)
            {
                totalWeight += w;
            }
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("商店所有商品的權重總和為 0，改用等機率隨機選擇！");
            return itemList[Random.Range(0, itemList.Count)];
        }

        float randomValue = Random.Range(0f, totalWeight);
        foreach (GameObject prefab in itemList)
        {
            if (prefab == null) continue;
            ShopItem shopItem = prefab.GetComponent<ShopItem>();
            float w = (shopItem != null) ? shopItem.weight : 1f;
            if (w <= 0f) continue;

            randomValue -= w;
            if (randomValue <= 0f)
            {
                return prefab;
            }
        }

        return itemList[itemList.Count - 1];
    }

    public void Interact(Transform player)
    {
        // 觸發確認對話框事件
        UIEvents.ConfirmDialogRequested(itemGO, () =>
        {
            PlayerBalanceSystem playerBalance = player.GetComponent<PlayerBalanceSystem>();
            if (playerBalance != null)
            {
                int itemCost = item.price;
                if (playerBalance.GetCoin() >= itemCost)
                {
                    if(audioSource != null && clip != null)
                    {
                        audioSource.PlayOneShot(clip, 2f);
                    }
                    playerBalance.SpendCoin(itemCost);
                    Debug.Log($"購買成功！花費 {itemCost} 金幣。");
                    // 在這裡可以添加購買成功後的邏輯，例如給玩家物品等
                    
                    foreach (Transform child in transform)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("UnInteractable");
                    }
                    itemGO.layer = LayerMask.NameToLayer("Interactable");
                    gameObject.layer = LayerMask.NameToLayer("UnInteractable");

                    //直接使用商品
                    IInteractable itemInteract = itemGO.GetComponent<IInteractable>();
                    if(itemInteract != null)
                    {
                        itemInteract.Interact(player);
                    }

                }
                else
                {
                    UIEvents.LevelChanged("金幣不足");
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
