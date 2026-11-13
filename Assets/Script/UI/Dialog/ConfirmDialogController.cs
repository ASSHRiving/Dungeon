using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmDialogController : UIPanelBase
{
    [Header("UI Elements")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private EquipmentSlotUI slot;

    [Header("Equipment Comparison UI")]
    [SerializeField] private EquipmentSlotUI currentEquippedSlot;
    [SerializeField] private GameObject comparisonArrow;
    [SerializeField] private Vector2 compareSlotPosition = new Vector2(-105f, -20f);
    [SerializeField] private Vector2 singleSlotPosition = new Vector2(-225f, -20f);

    [Header("外部UI")]
    [SerializeField] private CanvasGroup interactpanel;

    protected override GameObject PanelRoot => dialogPanel;

    // 商店確認框維持原本設定（開啟時不強制暫停時間）
    public override bool PauseGameWhenOpen => false;

    private void OnEnable()
    {
        UIEvents.OnConfirmDialogRequested += ShowConfirmDialog;
    }

    private void OnDisable()
    {
        UIEvents.OnConfirmDialogRequested -= ShowConfirmDialog;
        if (interactpanel != null)
        {
            interactpanel.alpha = 1f;
        }
    }

    /// <summary>
    /// 顯示商品購買確認彈窗（相容既有 UIEvents.OnConfirmDialogRequested 事件）。
    /// </summary>
    private void ShowConfirmDialog(GameObject item, Action onConfirm)
    {
        string msg = string.Empty;
        if (item != null && item.TryGetComponent(out ShopItem shopItem))
        {
            msg = shopItem.message;
        }

        ShowConfirmDialog(msg, onConfirm, null, item);
    }

    /// <summary>
    /// 通用確認彈窗接口，支援自訂訊息、確認/取消回呼與選填的物品圖示展示。
    /// </summary>
    public void ShowConfirmDialog(string message, Action onConfirm, Action onCancel = null, GameObject item = null)
    {
        Open();

        if (messageText != null)
        {
            messageText.text = message;
        }

        UpdateItemSlots(item);

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
            {
                if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
                CloseDialog();
                onConfirm?.Invoke();
            });
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() =>
            {
                if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
                CloseDialog();
                onCancel?.Invoke();
            });
        }
    }

    /// <summary>
    /// 更新購買商品欄位與當前裝備比較欄位：
    /// 若商品為防具 (Equipment) 或武器 (Weapon)，則同時顯示「當前裝備」與「購買商品」進行比對；
    /// 若商品為一般消耗品（如血瓶 Potion），則隱藏比較欄位，僅居中顯示購買商品。
    /// </summary>
    private void UpdateItemSlots(GameObject item)
    {
        if (item == null)
        {
            if (slot != null)
            {
                slot.ClearSlot();
                slot.gameObject.SetActive(false);
            }

            if (currentEquippedSlot != null)
            {
                currentEquippedSlot.ClearSlot();
                currentEquippedSlot.gameObject.SetActive(false);
            }

            if (comparisonArrow != null)
            {
                comparisonArrow.SetActive(false);
            }

            return;
        }

        bool shouldCompare = TryGetEquippedComparisonItem(item, out IItem currentEquippedItem);

        if (shouldCompare)
        {
            if (currentEquippedSlot != null)
            {
                currentEquippedSlot.gameObject.SetActive(true);
                currentEquippedSlot.DisplayItem(currentEquippedItem);
            }

            if (comparisonArrow != null)
            {
                comparisonArrow.SetActive(true);
            }

            if (slot != null)
            {
                slot.gameObject.SetActive(true);
                if (slot.TryGetComponent(out RectTransform slotRect))
                {
                    slotRect.anchoredPosition = compareSlotPosition;
                }
                slot.DisplayItem(item);
            }
        }
        else
        {
            if (currentEquippedSlot != null)
            {
                currentEquippedSlot.ClearSlot();
                currentEquippedSlot.gameObject.SetActive(false);
            }

            if (comparisonArrow != null)
            {
                comparisonArrow.SetActive(false);
            }

            if (slot != null)
            {
                slot.gameObject.SetActive(true);
                if (slot.TryGetComponent(out RectTransform slotRect))
                {
                    slotRect.anchoredPosition = singleSlotPosition;
                }
                slot.DisplayItem(item);
            }
        }
    }

    /// <summary>
    /// 判斷商品是否需要與玩家身上的裝備/武器進行比對，並取得對應部位的當前物品。
    /// </summary>
    private bool TryGetEquippedComparisonItem(GameObject shopItemGO, out IItem equippedItem)
    {
        equippedItem = null;
        if (shopItemGO == null) return false;

        Transform player = ResolvePlayer();

        // 1. 防具裝備：比對相同部位 (EquipmentType) 的當前穿戴裝備
        Equipment shopEquip = shopItemGO.GetComponent<Equipment>() ?? shopItemGO.GetComponentInChildren<Equipment>();
        if (shopEquip != null)
        {
            if (player != null)
            {
                CharacterEquipment playerEquip = player.GetComponent<CharacterEquipment>() ?? player.GetComponentInChildren<CharacterEquipment>();
                if (playerEquip != null)
                {
                    GameObject currentEquipGO = playerEquip.GetEquippedItem(shopEquip.equipmentType);
                    if (currentEquipGO != null)
                    {
                        equippedItem = currentEquipGO.GetComponent<IItem>() ?? currentEquipGO.GetComponentInChildren<IItem>();
                    }
                }
            }
            return true;
        }

        // 2. 武器：比對角色當前持有的武器 (currentWeapon)
        Weapon shopWeapon = shopItemGO.GetComponent<Weapon>() ?? shopItemGO.GetComponentInChildren<Weapon>();
        if (shopWeapon != null)
        {
            if (player != null)
            {
                CharacterCombatBase playerCombat = player.GetComponent<CharacterCombatBase>() ?? player.GetComponentInChildren<CharacterCombatBase>();
                if (playerCombat != null)
                {
                    equippedItem = playerCombat.currentWeapon;
                }
            }
            return true;
        }

        // 3. 其他消耗品（如血瓶 Potion）不需比對
        return false;
    }

    private Transform ResolvePlayer()
    {
        if (uiController != null)
        {
            Transform player = uiController.GetPlayer();
            if (player != null) return player;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        return playerObj != null ? playerObj.transform : null;
    }

    protected override void OnBeforeOpen()
    {
        base.OnBeforeOpen();
        if (interactpanel != null)
        {
            interactpanel.alpha = 0f;
        }
    }

    protected override void OnBeforeClose()
    {
        base.OnBeforeClose();
        UIEvents.HideTooltip();
    }

    protected override void OnAfterClose()
    {
        base.OnAfterClose();
        if (interactpanel != null)
        {
            interactpanel.alpha = 1f;
        }
    }

    public void OpenDialog() => Open();

    public void CloseDialog() => Close();
}

