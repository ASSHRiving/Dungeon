using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BossHealthUIController : MonoBehaviour
{
    [Header("UI 元件綁定")]
    public CanvasGroup rootCanvasGroup;
    public RectTransform barContainerRect;
    public TMP_Text bossNameText;
    public Image mainFillImage;       // 暗紅主血條 (Image Type: Filled -> Horizontal)
    public Image chipFillImage;       // 香檳金殘影條 (Image Type: Filled -> Horizontal)

    [Header("DOTween 動態參數")]
    [SerializeField] private float introFadeDuration = 0.28f;
    [SerializeField] private float introFillDuration = 0.65f;
    [SerializeField] private float mainBarFastDuration = 0.05f;
    [SerializeField] private float chipHoldDelay = 0.35f;     // Combo Hold：連砍時殘影停滯秒數
    [SerializeField] private float chipCatchUpDuration = 0.45f;// 殘影追趕時間
    [SerializeField] private float defeatHoldDelay = 1.0f;     // 擊殺後停留秒數
    [SerializeField] private float defeatFadeDuration = 0.4f;

    private Sequence introSequence;
    private Tweener mainFillTween;
    private Tweener chipFillTween;
    private Tweener shakeTween;
    private Vector2 originAnchoredPos;
    private float lastRatio = 1f;

    private void Awake()
    {
        if (barContainerRect != null)
            originAnchoredPos = barContainerRect.anchoredPosition;

        HideImmediate();
    }

    private void OnEnable()
    {
        UIEvents.OnBossFightStart += HandleBossFightStart;
        UIEvents.OnBossHealthChanged += HandleBossHealthChanged;
        UIEvents.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        UIEvents.OnBossFightStart -= HandleBossFightStart;
        UIEvents.OnBossHealthChanged -= HandleBossHealthChanged;
        UIEvents.OnBossDefeated -= HandleBossDefeated;
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    /// <summary>
    /// 1. Boss 登場：展開外框並將血條由 0% 充能至 100%
    /// </summary>
    public void HandleBossFightStart(string bossName, float maxHP)
    {
        KillAllTweens();
        lastRatio = 1f;

        if (bossNameText != null)
            bossNameText.text = $"<color=#E09F3E>◆</color> {bossName}";

        if (mainFillImage != null) mainFillImage.fillAmount = 0f;
        if (chipFillImage != null) chipFillImage.fillAmount = 0f;

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (barContainerRect != null)
        {
            barContainerRect.anchoredPosition = originAnchoredPos;
            barContainerRect.localScale = new Vector3(0.85f, 1f, 1f);
        }

        introSequence = DOTween.Sequence();
        if (rootCanvasGroup != null)
            introSequence.Join(rootCanvasGroup.DOFade(1f, introFadeDuration).SetEase(Ease.OutQuad));
        if (barContainerRect != null)
            introSequence.Join(barContainerRect.DOScaleX(1f, introFadeDuration).SetEase(Ease.OutCubic));

        if (mainFillImage != null)
            introSequence.Join(mainFillImage.DOFillAmount(1f, introFillDuration).SetDelay(0.1f).SetEase(Ease.OutCubic));
        if (chipFillImage != null)
            introSequence.Join(chipFillImage.DOFillAmount(1f, introFillDuration).SetDelay(0.1f).SetEase(Ease.OutCubic));
    }

    /// <summary>
    /// 2. Boss 受傷扣血：主血條瞬扣 + 殘影條重置延遲 (Combo Hold)
    /// </summary>
    public void HandleBossHealthChanged(float currentHP, float maxHP)
    {
        if (maxHP <= 0f) return;

        // 若登場充能還沒播完就被砍，立即結束充能動畫
        if (introSequence != null && introSequence.IsActive())
            introSequence.Complete();

        float targetRatio = Mathf.Clamp01(currentHP / maxHP);
        float damageDelta = lastRatio - targetRatio;
        lastRatio = targetRatio;

        // A. 主血條極速扣除
        if (mainFillImage != null)
        {
            mainFillTween?.Kill();
            mainFillTween = mainFillImage.DOFillAmount(targetRatio, mainBarFastDuration).SetEase(Ease.OutQuad);
        }

        // B. 香檳金殘影條：每次受傷都 Kill 舊動畫並重新計算 0.35s 延遲 (Combo Hold)
        if (chipFillImage != null)
        {
            chipFillTween?.Kill();
            chipFillTween = chipFillImage.DOFillAmount(targetRatio, chipCatchUpDuration)
                .SetDelay(chipHoldDelay)
                .SetEase(Ease.OutQuad);
        }

        // C. 單次傷害大於 12% 最大血量時，觸發血條微震動回饋
        if (damageDelta >= 0.12f && barContainerRect != null)
        {
            shakeTween?.Kill(complete: true);
            barContainerRect.anchoredPosition = originAnchoredPos;
            shakeTween = barContainerRect.DOShakeAnchorPos(0.18f, new Vector2(8f, 5f), vibrato: 20, randomness: 90f, snapping: false, fadeOut: true);
        }

        // D. 若血量歸零，自動觸發退場
        if (currentHP <= 0f)
        {
            HandleBossDefeated();
        }
    }

    /// <summary>
    /// 3. Boss 擊殺退場：停留 1 秒後平滑淡出
    /// </summary>
    public void HandleBossDefeated()
    {
        if (rootCanvasGroup == null) return;

        introSequence?.Kill();
        introSequence = DOTween.Sequence();
        introSequence.AppendInterval(defeatHoldDelay);
        introSequence.Append(rootCanvasGroup.DOFade(0f, defeatFadeDuration).SetEase(Ease.InQuad));
    }

    public void HideImmediate()
    {
        KillAllTweens();
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = false;
        }
    }

    private void KillAllTweens()
    {
        introSequence?.Kill();
        mainFillTween?.Kill();
        chipFillTween?.Kill();
        shakeTween?.Kill();
    }
}