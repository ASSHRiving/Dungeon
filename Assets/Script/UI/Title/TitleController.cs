using UnityEngine;
using TMPro;
using DG.Tweening;

public class TitleController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayDuration = 1f;
    [SerializeField] private TMP_Text titleText;

    private Sequence titleSequence;

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        UIEvents.OnLevelChanged += UpdateTitleText;
    }

    private void OnDisable()
    {
        UIEvents.OnLevelChanged -= UpdateTitleText;
    }

    private void OnDestroy()
    {
        titleSequence?.Kill();
    }

    private void UpdateTitleText(string titleString)
    {
        if (titleText != null)
        {
            titleText.text = titleString;
        }

        if (fadeCanvasGroup == null) return;

        titleSequence?.Kill();
        fadeCanvasGroup.alpha = 0f;

        titleSequence = DOTween.Sequence();
        titleSequence.Append(fadeCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad));
        titleSequence.AppendInterval(displayDuration);
        titleSequence.Append(fadeCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        titleSequence.SetUpdate(true);
    }
}

