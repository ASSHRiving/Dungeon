using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LoadingController : SingletonBase<LoadingController>
{
    [Header("UI 組件")]
    [SerializeField] private CanvasGroup fadeCanvasGroup; // 掛在 FadeImage 上的 CanvasGroup
    [SerializeField] private float fadeDuration = 0.5f;   // 淡入淡出時間 (秒)

    private bool isLoading;
    private Tweener fadeTween;

    protected override void Awake()
    {
        base.Awake();

        // 剛進入遊戲時，預設黑屏為透明
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        fadeTween?.Kill();
    }

    /// <summary>
    /// 外部呼叫的載入下一關方法（具備防重複觸發保護）
    /// </summary>
    public void LoadLevel(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadLevelRoutine(sceneName));
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        isLoading = true;
        Time.timeScale = 1f;

        // 1. 畫面變黑（Fade Out）
        yield return Fade(1f);

        // 2. 非同步載入場景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                // 當載入進度達到 0.9 時，代表場景載入完畢，準備激活
                if (asyncLoad.progress >= 0.9f)
                {
                    asyncLoad.allowSceneActivation = true;
                }
                yield return null;
            }
        }

        // 3. 等待 1 幀，確保 StartRoom.Init() 與 Generator 完成房間生成與 Player 傳送
        yield return new WaitForEndOfFrame();

        // 4. 畫面變透明（Fade In）
        yield return Fade(0f);

        isLoading = false;
    }

    /// <summary>
    /// 使用 DOTween 控制 CanvasGroup Alpha 的淡入淡出協程
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true; // 變黑與過場過程中阻擋玩家點擊 UI
        fadeTween?.Kill();

        fadeTween = fadeCanvasGroup
            .DOFade(targetAlpha, fadeDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true);

        yield return fadeTween.WaitForCompletion();

        // 如果完全透明了，關閉 Raycast 阻擋
        if (Mathf.Approximately(targetAlpha, 0f))
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}

