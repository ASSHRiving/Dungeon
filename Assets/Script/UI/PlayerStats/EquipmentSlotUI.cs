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

    private GameObject currentItem;
    private Equipment currentEquipment;

    /// <summary>
    /// 更新槽位顯示（傳入場景或 Prefab GameObject）
    /// </summary>
    public void DisplayItem(GameObject item)
    {
        currentItem = item;
        Equipment equipment = item != null ? item.GetComponent<Equipment>() : null;
        DisplayItem(equipment);
    }

    /// <summary>
    /// 更新槽位顯示（直接傳入 Equipment 組件，避免重複 GetComponent）
    /// </summary>
    public void DisplayItem(Equipment equipment)
    {
        currentEquipment = equipment;

        if (currentEquipment != null && currentEquipment.itemIcon != null && iconImage != null)
        {
            iconImage.sprite = currentEquipment.itemIcon;
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
        currentItem = null;
        currentEquipment = null;
        if (iconImage != null) iconImage.gameObject.SetActive(false);
        if (emptyPlaceholder != null) emptyPlaceholder.SetActive(true);
    }

    // 滑鼠懸停
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentEquipment != null)
        {
            UIEvents.ShowTooltip(currentEquipment);
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

