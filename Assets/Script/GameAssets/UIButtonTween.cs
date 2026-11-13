using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

[DisallowMultipleComponent]
public class UIButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Header("縮放比例設定 (現代極簡風建議微幅變化)")]
    [SerializeField] private float hoverScale = 1.05f;   // 滑鼠移上去時的放大倍率
    [SerializeField] private float pressScale = 0.95f;   // 滑鼠按下去時的縮小倍率
    [SerializeField] private float animDuration = 0.14f; // 動畫過渡時間(秒)

    [Header("懸停亮度/透明度微調 (選填)")]
    [SerializeField] private bool tintOnHover = false;
    [SerializeField] private Color hoverTint = Color.white;

    private Vector3 originalScale;
    private Color originalColor;
    private Image targetImage;
    private Selectable selectable; // 相容 Button, Toggle, Dropdown 等所有 UI 互動元件
    private Tweener scaleTween;
    private Tweener colorTween;

    private void Awake()
    {
        originalScale = transform.localScale;
        selectable = GetComponent<Selectable>();
        targetImage = GetComponent<Image>();

        if (targetImage != null)
        {
            originalColor = targetImage.color;
        }
    }

    private void OnEnable()
    {
        ResetVisualStateImmediate();
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        ResetVisualStateImmediate();
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
    }

    private void ResetVisualStateImmediate()
    {
        transform.localScale = originalScale;
        if (tintOnHover && targetImage != null)
        {
            targetImage.color = originalColor;
        }
    }

    private bool IsInteractable()
    {
        return selectable == null || selectable.interactable;
    }

    // 1. 滑鼠移入 (Hover Enter)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        ApplyHoverVisual();
    }

    // 2. 滑鼠移出 (Hover Exit) — 不受 IsInteractable 阻擋，避免按鈕在懸停時被設為不可互動而卡死放大狀態
    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreNormalVisual();
    }

    // 3. 滑鼠按下 (Press Down)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable() || eventData.button != PointerEventData.InputButton.Left) return;

        TweenScale(originalScale * pressScale, Ease.OutQuad);
    }

    // 4. 滑鼠放開 (Release Up) — 若放開時按鈕已變為不可互動，強制退回原大小
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (!IsInteractable())
        {
            RestoreNormalVisual();
            return;
        }

        // 放開時若滑鼠還在按鈕上，回彈至 Hover 大小並帶一點微彈性 (OutBack)
        bool isHovering = eventData.hovered.Contains(gameObject);
        Vector3 target = isHovering ? (originalScale * hoverScale) : originalScale;
        TweenScale(target, Ease.OutBack);
    }

    // 5. 支援鍵盤/手把導航選取 (Select / Deselect)
    public void OnSelect(BaseEventData eventData)
    {
        if (!IsInteractable()) return;
        ApplyHoverVisual();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        RestoreNormalVisual();
    }

    private void ApplyHoverVisual()
    {
        TweenScale(originalScale * hoverScale, Ease.OutQuad);

        if (tintOnHover && targetImage != null)
        {
            colorTween?.Kill();
            colorTween = targetImage.DOColor(hoverTint, animDuration).SetUpdate(true);
        }
    }

    private void RestoreNormalVisual()
    {
        TweenScale(originalScale, Ease.OutQuad);

        if (tintOnHover && targetImage != null)
        {
            colorTween?.Kill();
            colorTween = targetImage.DOColor(originalColor, animDuration).SetUpdate(true);
        }
    }

    private void TweenScale(Vector3 targetScale, Ease easeType)
    {
        scaleTween?.Kill();
        // SetUpdate(true) 確保在設定頁面 Time.timeScale = 0 時依然絲滑運作
        scaleTween = transform.DOScale(targetScale, animDuration)
            .SetEase(easeType)
            .SetUpdate(true);
    }
}