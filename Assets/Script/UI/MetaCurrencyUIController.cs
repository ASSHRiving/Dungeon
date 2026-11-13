using UnityEngine;
using TMPro;

/// <summary>
/// 通用局外貨幣顯示 UI 控制器
/// 可掛載於選角大廳 (Lobby)、主選單 (MainMenu) 或局外養成天賦面板上
/// 自動監聽貨幣變更事件並即時刷新數值
/// </summary>
public class MetaCurrencyUIController : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private TMP_Text currencyText;

    [Header("格式設定")]
    [SerializeField] private string prefix = "";
    [SerializeField] private string suffix = " 靈魂";

    private void OnEnable()
    {
        UIEvents.OnMetaCurrencyChanged += HandleCurrencyChanged;
        RefreshUI();
    }

    private void OnDisable()
    {
        UIEvents.OnMetaCurrencyChanged -= HandleCurrencyChanged;
    }

    public void RefreshUI()
    {
        int amount = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.TotalCurrency : 0;
        HandleCurrencyChanged(amount);
    }

    private void HandleCurrencyChanged(int newAmount)
    {
        if (currencyText != null)
        {
            currencyText.text = $"{prefix}{newAmount:N0}{suffix}";
        }
    }
}
