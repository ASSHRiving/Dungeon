using UnityEngine;
using System.Collections.Generic;

public class ShopTableInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private ShopPoolSO shopPool;
    [SerializeField] private Transform itemPoint;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clip;

    private GameObject itemGO;
    private ShopItem item;

    public ShopPoolSO ShopPool => shopPool;

    public void SetShopPool(ShopPoolSO pool)
    {
        shopPool = pool;
    }

    public string interactableName
    {
        get
        {
            if (itemGO == null) return "空的購物桌";
            else
            {
                if (item != null)
                {
                    return $"購買 \n{item.itemName}";
                }
                return $"購買 \n{itemGO.name}";
            }
        }
    }

    private void Start()
    {
        init();
    }

    public void init()
    {
        if (shopPool == null)
        {
            Debug.LogWarning($"[ShopTableInteract] '{gameObject.name}' 尚未指定 ShopPoolSO！");
            return;
        }

        int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;
        ShopItemEntry selectedEntry = shopPool.SelectRandomEntry(currentLevel);
        if (selectedEntry == null || selectedEntry.itemPrefab == null)
        {
            Debug.LogWarning($"[ShopTableInteract] '{gameObject.name}' 在 Level {currentLevel} 未能從商品池中抽取到有效商品！");
            return;
        }

        Transform spawnPoint = itemPoint != null ? itemPoint : transform;
        itemGO = Instantiate(selectedEntry.itemPrefab, spawnPoint);
        itemGO.layer = LayerMask.NameToLayer("UnInteractable");

        // 若為裝備，設定其階級（若條目有指定則用指定值，否則依該層階級權重隨機抽取）
        if (itemGO.TryGetComponent<Equipment>(out var equipment))
        {
            int tierToSet = selectedEntry.targetTier >= 0
                ? selectedEntry.targetTier
                : shopPool.RollEquipmentTier(currentLevel);
            equipment.SetIndex(tierToSet);
        }

        item = itemGO.GetComponent<ShopItem>();
        if (item != null)
        {
            item.initItem();
            if (selectedEntry.overridePrice >= 0)
            {
                item.price = selectedEntry.overridePrice;
            }
        }
    }

    /// <summary>
    /// 清除目前商品並重新自商品池隨機生成
    /// </summary>
    public void RefreshShop()
    {
        if (itemGO != null)
        {
            Destroy(itemGO);
            itemGO = null;
            item = null;
        }

        init();
    }

    public void Interact(Transform player)
    {
        // 觸發確認對話框事件
        UIEvents.ConfirmDialogRequested(itemGO, () =>
        {
            PlayerBalanceSystem playerBalance = player.GetComponent<PlayerBalanceSystem>();
            if (playerBalance != null)
            {
                int itemCost = item != null ? item.price : 0;
                if (playerBalance.GetCoin() >= itemCost)
                {
                    if (audioSource != null && clip != null)
                    {
                        audioSource.PlayOneShot(clip, 2f);
                    }
                    playerBalance.SpendCoin(itemCost);
                    Debug.Log($"購買成功！花費 {itemCost} 金幣。");

                    foreach (Transform child in transform)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("UnInteractable");
                    }
                    if (itemGO != null)
                    {
                        itemGO.layer = LayerMask.NameToLayer("Interactable");
                    }
                    gameObject.layer = LayerMask.NameToLayer("UnInteractable");

                    // 直接使用商品
                    if (itemGO != null)
                    {
                        IInteractable itemInteract = itemGO.GetComponent<IInteractable>();
                        if (itemInteract != null)
                        {
                            itemInteract.Interact(player);
                        }
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
        if (itemGO == null || itemPoint == null) return;
        itemGO.layer = LayerMask.NameToLayer("Interactable");
        Rigidbody rb = itemGO.GetComponentInChildren<Rigidbody>();
        if (rb != null)
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
