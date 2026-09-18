using UnityEngine;
using TMPro;

public class TooltipController : MonoBehaviour
{
    [Header("UI Component References")]
    public GameObject tooltipPanel;
    public RectTransform tooltipRect;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statsText;

    [Header("Settings")]
    public Vector2 offset = new Vector2(15f, -15f); // 懸浮框相對於滑鼠游標的偏移量

    private Canvas parentCanvas;
    private void OnEnable()
    {
        UIEvents.OnShowTooltip += ShowTooltip;
        UIEvents.OnHideTooltip += HideTooltip;
    }
    private void OnDisable()
    {
        UIEvents.OnShowTooltip -= ShowTooltip;
        UIEvents.OnHideTooltip -= HideTooltip;
    }

    private void Awake()
    {
        if (tooltipRect == null) tooltipRect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        // 預設隱藏
        HideTooltip();
    }

    private void Update()
    {
        if (gameObject.activeSelf)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// 更新懸浮框位置，使其跟隨滑鼠並防止超出螢幕邊界
    /// </summary>
    private void UpdatePosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        Vector2 targetPos = mousePosition + offset;

        // 防止懸浮框超出螢幕右方與下方
        float pivotX = (mousePosition.x + tooltipRect.rect.width > Screen.width) ? 1f : 0f;
        float pivotY = (mousePosition.y - tooltipRect.rect.height < 0) ? 0f : 1f;

        tooltipRect.pivot = new Vector2(pivotX, pivotY);
        transform.position = mousePosition + new Vector2(pivotX == 1f ? -offset.x : offset.x, pivotY == 0f ? -offset.y : offset.y);
    }

    /// <summary>
    /// 顯示並填入裝備詳細屬性
    /// </summary>
    public void ShowTooltip(ShopItem shopItem, EquipmentInteract.EquipmentType equipmentType)
    {
        if (shopItem == null) return;

        // 設定裝備名稱與部位
        nameText.text = shopItem.equipmentName;

        // 組合屬性字串 (只顯示 > 0 的屬性，以四捨五入整數呈現)
        string stats = "";
        if (shopItem.damage > 0) stats += $"攻擊力: +{Mathf.RoundToInt(shopItem.damage)}\n";
        if (shopItem.critRate > 0) stats += $"暴擊率: +{Mathf.RoundToInt(shopItem.critRate)}%\n";
        if (shopItem.health > 0) stats += $"最大生命: +{Mathf.RoundToInt(shopItem.health)}\n";
        if (shopItem.shield > 0) stats += $"護甲: +{Mathf.RoundToInt(shopItem.shield)}\n";

        statsText.text = string.IsNullOrEmpty(stats) ? "無附加屬性" : stats.TrimEnd();

        tooltipPanel.SetActive(true);
        UpdatePosition(); // 立即更新一次位置防止閃爍
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
}