using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Settings")]
    public Equipment.EquipmentType slotType;

    [Header("UI Component References")]
    public Image iconImage;            // 裝備圖案 (Image)
    public GameObject emptyPlaceholder; // 未裝備時顯示的預設圖樣/文字

    private GameObject currentItemGO;
    private IItem currentItem;

    /// <summary>
    /// 更新槽位顯示（傳入場景或 Prefab GameObject）
    /// </summary>
    public void DisplayItem(GameObject itemGO)
    {
        currentItemGO = itemGO;
        IItem item = itemGO != null
            ? (itemGO.GetComponent<IItem>() ?? itemGO.GetComponentInChildren<IItem>())
            : null;
        DisplayItem(item);
    }

    /// <summary>
    /// 更新槽位顯示（直接傳入實作 IItem 的組件或資料，避免重複 GetComponent）
    /// </summary>
    public void DisplayItem(IItem item)
    {
        currentItem = item;

        if (currentItem != null && currentItem.ItemIcon != null && iconImage != null)
        {
            iconImage.sprite = currentItem.ItemIcon;
            iconImage.gameObject.SetActive(true);
            // 保留深色背景，不關閉 emptyPlaceholder
        }
        else
        {
            ClearSlot();
        }
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void ClearSlot()
    {
        currentItemGO = null;
        currentItem = null;
        if (iconImage != null) iconImage.gameObject.SetActive(false);
        if (emptyPlaceholder != null) emptyPlaceholder.SetActive(true);
    }

    // 滑鼠懸停
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            UIEvents.ShowTooltip(currentItem);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIEvents.HideTooltip();
    }

    private void OnDisable()
    {
        UIEvents.HideTooltip();
    }
}

