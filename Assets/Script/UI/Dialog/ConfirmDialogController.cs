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

        if (slot != null)
        {
            if (item != null)
            {
                slot.gameObject.SetActive(true);
                slot.DisplayItem(item);
            }
            else
            {
                slot.ClearSlot();
                slot.gameObject.SetActive(false);
            }
        }

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

    public void OpenDialog() => Open();

    public void CloseDialog() => Close();
}

