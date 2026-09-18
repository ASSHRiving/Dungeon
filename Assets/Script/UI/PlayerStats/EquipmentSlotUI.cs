using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("Slot Settings")]
    public EquipmentInteract.EquipmentType slotType;

    [Header("UI Component References")]
    public Image iconImage;            // 裝備圖案 (Image)
    public GameObject emptyPlaceholder; // 未裝備時顯示的預設圖樣/文字

    private GameObject currentItem;

    /// <summary>
    /// 更新槽位顯示
    /// </summary>
    public void DisplayItem(GameObject item)
    {
        currentItem = item;

        if (item != null)
        {
            // 抓取裝備上的 ShopItem 或 EquipmentInteract 取得 Icon
            ShopItem shopItem = item.GetComponent<ShopItem>();
            Sprite itemIcon = (shopItem != null) ? shopItem.itemIcon : null;

            if (itemIcon != null && iconImage != null)
            {
                iconImage.sprite = itemIcon;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                // 若無圖示，依然讓 Icon 顯示預設樣式
                iconImage.gameObject.SetActive(true);
            }

            if (emptyPlaceholder != null) emptyPlaceholder.SetActive(false);
        }
        else
        {
            // 槽位為空
            ClearSlot();
        }
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        if (iconImage != null) iconImage.gameObject.SetActive(false);
        if (emptyPlaceholder != null) emptyPlaceholder.SetActive(true);
    }
}
