using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 非阻塞式左側新手教學 HUD 表現層控制器（獨立 Sub-Canvas，不繼承 UIPanelBase）。
/// 負責：進退場滑動動畫、彈窗避讓淡出、動態按鍵文案顯示、進度條與完成打勾回饋。
/// </summary>
public class TutorialHUDController : MonoBehaviour
{
    [Header("畫布與根節點")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private CanvasGroup cardCanvasGroup;
    [SerializeField] private RectTransform cardRect;

    [Header("文案與進度 UI")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject progressRow;
    [SerializeField] private Image progressFillImage;
    [SerializeField] private TMP_Text progressValueText;

    [Header("完成狀態與裝飾")]
    [SerializeField] private RectTransform checkIconRect;
    [SerializeField] private Image checkIconImage;

    [Header("略過按鈕")]
    [SerializeField] private Button skipButton;
    [SerializeField] private Image skipHoldFillImage;

    [Header("動畫與位置參數")]
    [SerializeField] private float shownPosX = 24f;
    [SerializeField] private float hiddenPosX = -420f;
    [SerializeField] private float slideInDuration = 0.32f;
    [SerializeField] private float slideOutDuration = 0.26f;
    [SerializeField] private float completionHoldDuration = 0.65f;
    [SerializeField] private float suppressFadeDuration = 0.18f;

    [Header("視覺色彩")]
    [SerializeField] private Color activeProgressColor = new Color(0.878f, 0.624f, 0.243f, 1f);    // #E09F3E 琥珀金
    [SerializeField] private Color completedProgressColor = new Color(0.369f, 0.918f, 0.831f, 1f); // #5EEAD4 完成青金
    [SerializeField] private Color normalTitleColor = new Color(0.961f, 0.937f, 0.902f, 1f);       // #F5EFE6 羊皮紙暖白
    [SerializeField] private Color completedTitleColor = new Color(0.988f, 0.925f, 0.651f, 1f);    // #FCECA6 亮金

    private Sequence cardSequence;
    private Tweener suppressTween;
    private bool isSuppressed = false;
    private bool isCardVisible = false;
    private Action skipClickCallback;

    private void Awake()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(HandleSkipClicked);
        }

        HideImmediate();
    }

    private void OnDestroy()
    {
        cardSequence?.Kill();
        suppressTween?.Kill();
    }

    public void SetSkipClickAction(Action onSkipClicked)
    {
        skipClickCallback = onSkipClicked;
    }

    private void HandleSkipClicked()
    {
        skipClickCallback?.Invoke();
    }

    /// <summary>
    /// 立即隱藏教學卡（不播放動畫）。
    /// </summary>
    public void HideImmediate()
    {
        cardSequence?.Kill();
        isCardVisible = false;

        if (cardRect != null)
        {
            Vector2 pos = cardRect.anchoredPosition;
            pos.x = hiddenPosX;
            cardRect.anchoredPosition = pos;
            cardRect.localScale = Vector3.one;
        }

        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.alpha = 0f;
        }

        if (checkIconRect != null)
        {
            checkIconRect.localScale = Vector3.zero;
        }

        if (skipHoldFillImage != null)
        {
            skipHoldFillImage.fillAmount = 0f;
        }

        UpdateRootInteractable();
    }

    /// <summary>
    /// 顯示新的教學步驟卡片並播放左側滑入動畫。
    /// </summary>
    public void ShowStep(TutorialStepSO step, string resolvedDescription, float currentProgress)
    {
        if (step == null) return;

        cardSequence?.Kill();
        isCardVisible = true;

        if (descriptionText != null)
        {
            descriptionText.text = resolvedDescription;
        }
        ResetVisualStateForActiveStep(step, currentProgress);

        if (cardRect != null)
        {
            Vector2 pos = cardRect.anchoredPosition;
            pos.x = hiddenPosX;
            cardRect.anchoredPosition = pos;
            cardRect.localScale = Vector3.one;
        }

        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.alpha = 0f;
        }

        UpdateRootInteractable();

        cardSequence = DOTween.Sequence();
        if (cardRect != null)
        {
            cardSequence.Append(cardRect.DOAnchorPosX(shownPosX, slideInDuration).SetEase(Ease.OutCubic));
        }
        if (cardCanvasGroup != null)
        {
            cardSequence.Join(cardCanvasGroup.DOFade(1f, slideInDuration * 0.85f).SetEase(Ease.OutQuad));
        }
        cardSequence.SetUpdate(true);
    }

    /// <summary>
    /// 即時刷新文案（用於玩家於設定頁更改按鍵綁定後同步更新按鍵標籤）。
    /// </summary>
    public void RefreshDescription(string resolvedDescription)
    {
        if (descriptionText != null)
        {
            descriptionText.text = resolvedDescription;
        }
    }

    /// <summary>
    /// 更新當前步驟進度條與數值文字。
    /// </summary>
    public void UpdateProgress(TutorialStepSO step, float currentProgress)
    {
        if (step == null) return;

        float target = Mathf.Max(0.01f, step.targetProgress);
        float clamped = Mathf.Clamp(currentProgress, 0f, target);
        float ratio = Mathf.Clamp01(clamped / target);

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = ratio;
        }

        if (progressValueText != null)
        {
            if (step.isDurationCondition)
            {
                progressValueText.text = $"{clamped:0.0}/{target:0.0}s";
            }
            else
            {
                int curInt = Mathf.Min(Mathf.FloorToInt(clamped + 0.0001f), Mathf.RoundToInt(target));
                int targetInt = Mathf.RoundToInt(target);
                progressValueText.text = $"{curInt}/{targetInt}";
            }
        }
    }

    /// <summary>
    /// 播放步驟達成回饋（打勾彈出、進度條轉完成色、輕提示音效），停留後滑出退場。
    /// </summary>
    public void PlayStepCompleted(TutorialStepSO step, Action onCompletedAnimationFinished)
    {
        if (step == null)
        {
            onCompletedAnimationFinished?.Invoke();
            return;
        }

        cardSequence?.Kill();

        UpdateProgress(step, step.targetProgress);
        if (progressFillImage != null)
        {
            progressFillImage.color = completedProgressColor;
        }
        if (descriptionText != null)
        {
            descriptionText.color = completedTitleColor;
        }
        if (checkIconImage != null)
        {
            checkIconImage.color = completedProgressColor;
        }

        if (GameAssets.Instance != null)
        {
            GameAssets.Instance.PlayUIClickSound();
        }

        cardSequence = DOTween.Sequence();

        if (checkIconRect != null)
        {
            checkIconRect.localScale = Vector3.zero;
            cardSequence.Append(checkIconRect.DOScale(Vector3.one, 0.26f).SetEase(Ease.OutBack));
        }

        if (cardRect != null)
        {
            cardRect.localScale = Vector3.one;
            cardSequence.Join(cardRect.DOPunchScale(new Vector3(0.04f, 0.04f, 0f), 0.24f, 6, 0.5f));
        }

        cardSequence.AppendInterval(completionHoldDuration);

        if (cardRect != null)
        {
            cardSequence.Append(cardRect.DOAnchorPosX(hiddenPosX, slideOutDuration).SetEase(Ease.InCubic));
        }
        if (cardCanvasGroup != null)
        {
            cardSequence.Join(cardCanvasGroup.DOFade(0f, slideOutDuration * 0.8f).SetEase(Ease.InQuad));
        }

        cardSequence.OnComplete(() =>
        {
            isCardVisible = false;
            UpdateRootInteractable();
            onCompletedAnimationFinished?.Invoke();
        });
        cardSequence.SetUpdate(true);
    }

    /// <summary>
    /// 播放略過或手動關閉退場動畫。
    /// </summary>
    public void PlayHide(Action onFinished = null)
    {
        cardSequence?.Kill();
        SetSkipHoldProgress(0f);

        if (!isCardVisible)
        {
            HideImmediate();
            onFinished?.Invoke();
            return;
        }

        cardSequence = DOTween.Sequence();
        if (cardRect != null)
        {
            cardSequence.Append(cardRect.DOAnchorPosX(hiddenPosX, slideOutDuration).SetEase(Ease.InCubic));
        }
        if (cardCanvasGroup != null)
        {
            cardSequence.Join(cardCanvasGroup.DOFade(0f, slideOutDuration * 0.85f).SetEase(Ease.InQuad));
        }

        cardSequence.OnComplete(() =>
        {
            HideImmediate();
            onFinished?.Invoke();
        });
        cardSequence.SetUpdate(true);
    }

    /// <summary>
    /// 更新長按 Tab 略過進度條 (0~1)。
    /// </summary>
    public void SetSkipHoldProgress(float normalized01)
    {
        if (skipHoldFillImage != null)
        {
            skipHoldFillImage.fillAmount = Mathf.Clamp01(normalized01);
        }
    }

    /// <summary>
    /// 當有全螢幕／中心彈窗（Esc / Settings / Character）開啟時淡出避讓，關閉後恢復。
    /// </summary>
    public void SetSuppressed(bool suppressed)
    {
        if (isSuppressed == suppressed) return;
        isSuppressed = suppressed;

        UpdateRootInteractable();

        if (rootCanvasGroup == null) return;

        suppressTween?.Kill();
        float targetAlpha = suppressed ? 0f : 1f;
        suppressTween = rootCanvasGroup.DOFade(targetAlpha, suppressFadeDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void ResetVisualStateForActiveStep(TutorialStepSO step, float currentProgress)
    {
        if (descriptionText != null)
        {
            descriptionText.color = normalTitleColor;
        }
        if (progressFillImage != null)
        {
            progressFillImage.color = activeProgressColor;
        }
        if (checkIconRect != null)
        {
            checkIconRect.localScale = Vector3.zero;
        }
        if (progressRow != null)
        {
            progressRow.SetActive(step.showProgressBar);
        }
        if (skipHoldFillImage != null)
        {
            skipHoldFillImage.fillAmount = 0f;
        }

        UpdateProgress(step, currentProgress);
    }

    private void UpdateRootInteractable()
    {
        if (rootCanvasGroup == null) return;
        bool active = isCardVisible && !isSuppressed;
        rootCanvasGroup.interactable = active;
        rootCanvasGroup.blocksRaycasts = active;
    }
}
