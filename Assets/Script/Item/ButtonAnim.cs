using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class ButtonAnim : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, 
    IPointerDownHandler, IPointerUpHandler
{
    [Header("目標組件引用")]
    [SerializeField] private RectTransform buttonRoot;       // 按鈕本體
    [SerializeField] private Image backgroundImage;          // 底板
    [SerializeField] private Image borderImage;              // 1px 外框
    [SerializeField] private RectTransform accentIndicator;  // 左側高亮指示條 (可選)
    [SerializeField] private TMP_Text labelText;      // 按鈕文字

    [Header("顏色配置")]
    [SerializeField] private Color normalBorderColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color hoverBorderColor = new Color(0f, 0.94f, 1f, 0.85f); // 亮青色
    [SerializeField] private Color normalTextColor = new Color(0.85f, 0.85f, 0.88f, 1f);
    [SerializeField] private Color hoverTextColor = Color.white;

    [Header("動態參數")]
    [SerializeField] private float hoverDuration = 0.15f;
    [SerializeField] private float clickDuration = 0.08f;
    [SerializeField] private float hoverScale = 1.03f;
    [SerializeField] private float clickScale = 0.96f;
    [SerializeField] private float hoverShiftX = 4f;        // 懸停時微幅右移距離

    private Vector3 originalPosition;
    private float indicatorTargetWidth;

    private void Awake()
    {
        if (buttonRoot == null) buttonRoot = GetComponent<RectTransform>();
        originalPosition = buttonRoot.anchoredPosition;

        // 若有指示條，記錄其原本設計的寬度
        if (accentIndicator != null)
        {
            indicatorTargetWidth = accentIndicator.sizeDelta.x;
            accentIndicator.sizeDelta = new Vector2(0f, accentIndicator.sizeDelta.y); // 預設寬度收合為 0
        }

        ResetToNormal(instant: true);
    }

    // 滑鼠移入（Hover）
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. 微幅放大 + 微幅右移
        buttonRoot.DOKill();
        buttonRoot.DOScale(hoverScale, hoverDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        //buttonRoot.DOAnchorPosX(originalPosition.x + hoverShiftX, hoverDuration).SetEase(Ease.OutCubic).SetUpdate(true);

        // 2. 邊框換色發光
        if (borderImage != null)
        {
            borderImage.DOKill();
            borderImage.DOColor(hoverBorderColor, hoverDuration).SetUpdate(true);
        }

        // 3. 文字提亮
        if (labelText != null)
        {
            labelText.DOKill();
            labelText.DOColor(hoverTextColor, hoverDuration).SetUpdate(true);
        }

        // 4. 左側指示條彈出
        if (accentIndicator != null)
        {
            accentIndicator.DOKill();
            accentIndicator.DOSizeDelta(new Vector2(indicatorTargetWidth, accentIndicator.sizeDelta.y), hoverDuration)
                .SetEase(Ease.OutQuart).SetUpdate(true);
        }
    }

    // 滑鼠移出（恢復）
    public void OnPointerExit(PointerEventData eventData)
    {
        ResetToNormal(instant: false);
    }

    // 按下滑鼠（Press Down）
    public void OnPointerDown(PointerEventData eventData)
    {
        buttonRoot.DOKill();
        // 極短時間微縮，營造按壓阻尼感
        buttonRoot.DOScale(clickScale, clickDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    // 放開滑鼠（Release）
    public void OnPointerUp(PointerEventData eventData)
    {
        buttonRoot.DOKill();
        // 放開後回彈至 Hover 狀態大小
        buttonRoot.DOScale(hoverScale, clickDuration * 1.5f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void ResetToNormal(bool instant)
    {
        float duration = instant ? 0f : hoverDuration;

        buttonRoot.DOKill();
        buttonRoot.DOScale(1f, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        //buttonRoot.DOAnchorPosX(originalPosition.x, duration).SetEase(Ease.OutCubic).SetUpdate(true);

        if (borderImage != null)
        {
            borderImage.DOKill();
            borderImage.DOColor(normalBorderColor, duration).SetUpdate(true);
        }

        if (labelText != null)
        {
            labelText.DOKill();
            labelText.DOColor(normalTextColor, duration).SetUpdate(true);
        }

        if (accentIndicator != null)
        {
            accentIndicator.DOKill();
            accentIndicator.DOSizeDelta(new Vector2(0f, accentIndicator.sizeDelta.y), duration).SetUpdate(true);
        }
    }

    private void OnDisable()
    {
        // 面板關閉時重置，避免下次開啟時停留在 Hover 狀態
        ResetToNormal(instant: true);
    }
}
