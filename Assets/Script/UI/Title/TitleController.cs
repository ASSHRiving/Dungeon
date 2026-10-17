using UnityEngine;
using TMPro;
using System.Collections;

public class TitleController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private TMP_Text titleText;

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
    private void UpdateTitleText(string titleString)
    {
        titleText.text = titleString;
        StartCoroutine(ShowTitleRoutine());
    }
    private IEnumerator ShowTitleRoutine()
    {
        // 顯示標題
        yield return StartCoroutine(Fade(1f));
        yield return new WaitForSeconds(1f); // 顯示 1 秒
        yield return StartCoroutine(Fade(0f));
    }
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}
