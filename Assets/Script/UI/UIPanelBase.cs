using UnityEngine;
using DG.Tweening;

/// <summary>
/// 所有彈窗與可開關 UI 面板的抽象基底類別。
/// 統一處理：DOTween 開關動畫、CanvasGroup 防連點、生命週期清理，並向 UIController 註冊堆疊狀態。
/// </summary>
public abstract class UIPanelBase : MonoBehaviour
{
    [SerializeField] protected UIController uiController;

    [Header("動態參數設定")]
    [SerializeField] protected float duration = 0.25f;
    [SerializeField] protected Vector3 initialScale = new Vector3(0.88f, 0.88f, 1f);

    protected CanvasGroup targetCanvasGroup;
    protected RectTransform targetRect;
    protected Sequence panelSequence;

    /// <summary>
    /// 面板目前是否處於開啟狀態。
    /// </summary>
    public bool IsOpen { get; protected set; }

    /// <summary>
    /// 面板開啟時是否需要暫停遊戲 (Time.timeScale = 0)。
    /// </summary>
    public virtual bool PauseGameWhenOpen => true;

    /// <summary>
    /// 是否允許玩家按下 Esc 鍵關閉此面板。
    /// </summary>
    public virtual bool CanCloseWithEsc => true;

    /// <summary>
    /// 是否在 Awake 時自動隱藏面板。
    /// </summary>
    protected virtual bool HideOnAwake => true;

    /// <summary>
    /// 面板開關控制的根物件 (SetActive 的目標)。
    /// </summary>
    protected abstract GameObject PanelRoot { get; }

    /// <summary>
    /// 負責淡入淡出的主視窗 CanvasGroup（若未指定則自動從 PanelRoot 取得或掛載）。
    /// </summary>
    protected virtual CanvasGroup WindowCanvasGroup => null;

    /// <summary>
    /// 負責縮放動畫的主視窗 RectTransform（若未指定則自動從 PanelRoot 取得）。
    /// </summary>
    protected virtual RectTransform WindowRect => null;

    /// <summary>
    /// 選填的外部背景遮罩 CanvasGroup（例如 TutorialPanel 的背景黑幕）。
    /// </summary>
    protected virtual CanvasGroup OverlayCanvasGroup => null;

    protected virtual void Awake()
    {
        ResolveReferences();

        if (HideOnAwake && PanelRoot != null)
        {
            HideImmediate();
        }
    }

    protected virtual void OnDestroy()
    {
        KillCurrentTween();
        if (uiController != null)
        {
            uiController.UnregisterPanel(this);
        }
    }

    /// <summary>
    /// 自動綁定與快取所需的 UIController、CanvasGroup 與 RectTransform。
    /// </summary>
    protected void ResolveReferences()
    {
        if (uiController == null)
        {
            uiController = GetComponentInParent<UIController>();
            if (uiController == null)
            {
                uiController = FindFirstObjectByType<UIController>();
            }
        }

        if (PanelRoot == null) return;

        targetCanvasGroup = WindowCanvasGroup != null
            ? WindowCanvasGroup
            : PanelRoot.GetComponent<CanvasGroup>();

        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = PanelRoot.AddComponent<CanvasGroup>();
        }

        targetRect = WindowRect != null
            ? WindowRect
            : PanelRoot.GetComponent<RectTransform>();
    }

    /// <summary>
    /// 切換面板開關狀態。
    /// </summary>
    public virtual void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    /// <summary>
    /// 開啟面板並播放入場動畫，同時通知 UIController 更新全域狀態。
    /// </summary>
    public virtual void Open()
    {
        if (PanelRoot == null) return;
        if (targetCanvasGroup == null || targetRect == null)
        {
            ResolveReferences();
        }

        IsOpen = true;
        if (uiController != null)
        {
            uiController.RegisterOpenPanel(this);
        }

        KillCurrentTween();
        PanelRoot.SetActive(true);
        SetInteractableState(true);
        ResetToHiddenState();

        OnBeforeOpen();

        if (duration <= 0f)
        {
            if (OverlayCanvasGroup != null) OverlayCanvasGroup.alpha = 1f;
            if (targetCanvasGroup != null) targetCanvasGroup.alpha = 1f;
            if (targetRect != null) targetRect.localScale = Vector3.one;
            OnAfterOpen();
            return;
        }

        panelSequence = DOTween.Sequence();
        if (OverlayCanvasGroup != null)
        {
            panelSequence.Append(OverlayCanvasGroup.DOFade(1f, duration * 0.8f).SetEase(Ease.Linear));
            if (targetCanvasGroup != null)
            {
                panelSequence.Join(targetCanvasGroup.DOFade(1f, duration * 0.7f).SetEase(Ease.OutQuad));
            }
        }
        else if (targetCanvasGroup != null)
        {
            panelSequence.Append(targetCanvasGroup.DOFade(1f, duration * 0.75f).SetEase(Ease.OutQuad));
        }

        if (targetRect != null)
        {
            panelSequence.Join(targetRect.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));
        }

        panelSequence.OnComplete(OnAfterOpen);
        panelSequence.SetUpdate(true);
    }

    /// <summary>
    /// 關閉面板並播放退場動畫，同時通知 UIController 恢復或切換全域狀態。
    /// </summary>
    public virtual void Close()
    {
        if (PanelRoot == null) return;

        IsOpen = false;
        if (uiController != null)
        {
            uiController.UnregisterPanel(this);
        }

        OnBeforeClose();

        // 退場期間禁止重複點擊按鈕
        SetInteractableState(false);
        KillCurrentTween();

        if (duration <= 0f || !PanelRoot.activeInHierarchy)
        {
            PanelRoot.SetActive(false);
            OnAfterClose();
            return;
        }

        panelSequence = DOTween.Sequence();
        float closeDuration = duration * 0.8f;

        if (OverlayCanvasGroup != null)
        {
            panelSequence.Append(OverlayCanvasGroup.DOFade(0f, closeDuration).SetEase(Ease.Linear));
            if (targetCanvasGroup != null)
            {
                panelSequence.Join(targetCanvasGroup.DOFade(0f, closeDuration * 0.8f).SetEase(Ease.InQuad));
            }
        }
        else if (targetCanvasGroup != null)
        {
            panelSequence.Append(targetCanvasGroup.DOFade(0f, closeDuration).SetEase(Ease.InQuad));
        }

        if (targetRect != null)
        {
            panelSequence.Join(targetRect.DOScale(initialScale, closeDuration).SetEase(Ease.InCubic));
        }

        panelSequence.OnComplete(() =>
        {
            if (PanelRoot != null)
            {
                PanelRoot.SetActive(false);
            }
            OnAfterClose();
        });
        panelSequence.SetUpdate(true);
    }

    /// <summary>
    /// 不播放動畫，立即將面板重置為隱藏狀態（常用於 Awake 初始化或強制切換場景前）。
    /// </summary>
    public void HideImmediate()
    {
        IsOpen = false;
        if (uiController != null)
        {
            uiController.UnregisterPanel(this);
        }

        KillCurrentTween();
        ResetToHiddenState();
        SetInteractableState(false);

        if (PanelRoot != null)
        {
            PanelRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 當此面板位於堆疊最上層且玩家按下 Esc 鍵時由 UIController 呼叫。
    /// </summary>
    public virtual void OnEscPressed()
    {
        Close();
    }

    protected virtual void OnBeforeOpen() { }
    protected virtual void OnAfterOpen() { }
    protected virtual void OnBeforeClose() { }
    protected virtual void OnAfterClose() { }

    protected void ResetToHiddenState()
    {
        if (OverlayCanvasGroup != null) OverlayCanvasGroup.alpha = 0f;
        if (targetCanvasGroup != null) targetCanvasGroup.alpha = 0f;
        if (targetRect != null) targetRect.localScale = initialScale;
    }

    protected void SetInteractableState(bool interactable)
    {
        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.interactable = interactable;
            targetCanvasGroup.blocksRaycasts = interactable;
        }
        if (OverlayCanvasGroup != null)
        {
            OverlayCanvasGroup.blocksRaycasts = interactable;
        }
    }

    protected void KillCurrentTween()
    {
        if (panelSequence != null && panelSequence.IsActive())
        {
            panelSequence.Kill();
        }
        panelSequence = null;
    }
}
