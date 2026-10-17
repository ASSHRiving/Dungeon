using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
    private readonly StringBuilder statsBuilder = new StringBuilder(128);

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
        if (tooltipRect == null && tooltipPanel != null)
        {
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        }
        if (tooltipRect == null)
        {
            tooltipRect = GetComponent<RectTransform>();
        }
        parentCanvas = GetComponentInParent<Canvas>();

        // 預設隱藏
        HideTooltip();
    }

    private void Update()
    {
        // 僅在 tooltipPanel 實際顯示時才每幀更新座標
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// 更新懸浮框位置，使其跟隨滑鼠並防止超出螢幕邊界
    /// </summary>
    private void UpdatePosition()
    {
        if (tooltipRect == null) return;

        Vector2 mousePosition = Pointer.current != null
            ? Pointer.current.position.ReadValue()
            : (Vector2)Input.mousePosition;

        // 防止懸浮框超出螢幕右方與下方
        float width = tooltipRect.rect.width * (parentCanvas != null ? parentCanvas.scaleFactor : 1f);
        float height = tooltipRect.rect.height * (parentCanvas != null ? parentCanvas.scaleFactor : 1f);

        float pivotX = (mousePosition.x + offset.x + width > Screen.width) ? 1f : 0f;
        float pivotY = (mousePosition.y + offset.y - height < 0f) ? 0f : 1f;

        tooltipRect.pivot = new Vector2(pivotX, pivotY);
        tooltipRect.position = mousePosition + new Vector2(
            pivotX == 1f ? -offset.x : offset.x,
            pivotY == 0f ? -offset.y : offset.y
        );
    }

    /// <summary>
    /// 顯示並填入裝備詳細屬性
    /// </summary>
    public void ShowTooltip(Equipment equipment)
    {
        if (equipment == null || tooltipPanel == null) return;

        // 設定裝備名稱與部位
        if (nameText != null)
        {
            nameText.text = equipment.equipmentName;
        }

        // 組合屬性字串 (只顯示 > 0 的屬性，以四捨五入整數呈現)
        statsBuilder.Clear();
        if (equipment.damage > 0) statsBuilder.AppendLine($"攻擊力: +{Mathf.RoundToInt(equipment.damage)}");
        if (equipment.critRate > 0) statsBuilder.AppendLine($"暴擊率: +{Mathf.RoundToInt(equipment.critRate)}%");
        if (equipment.health > 0) statsBuilder.AppendLine($"最大生命: +{Mathf.RoundToInt(equipment.health)}");
        if (equipment.shield > 0) statsBuilder.AppendLine($"護甲: +{Mathf.RoundToInt(equipment.shield)}");

        if (statsText != null)
        {
            statsText.text = statsBuilder.Length == 0 ? "無附加屬性" : statsBuilder.ToString().TrimEnd();
        }

        tooltipPanel.SetActive(true);

        if (tooltipRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        }

        UpdatePosition(); // 立即更新一次位置防止閃爍
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}