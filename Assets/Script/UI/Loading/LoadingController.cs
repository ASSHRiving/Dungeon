using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingController : SingletonBase<LoadingController>
{
    [Header("UI 組件")]
    [SerializeField] private CanvasGroup fadeCanvasGroup; // 掛在 FadeImage 上的 CanvasGroup
    [SerializeField] private float fadeDuration = 0.5f;   // 淡入淡出時間 (秒)

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

    /// <summary>
    /// 外部呼叫的載入下一關方法
    /// </summary>
    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadLevelRoutine(sceneName));
    }

    private IEnumerator LoadLevelRoutine(string sceneName)
    {
        Time.timeScale = 1f;

        // 1. 畫面變黑（Fade Out）
        yield return StartCoroutine(Fade(1f));
        

        // 3. 非同步載入場景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // 阻止場景載入完成後立刻激活（可選，確保讀取完畢）
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

        // 4. 等待 1 幀，確保 StartRoom.Init() 與 Generator 完成房間生成與 Player 傳送
        yield return new WaitForEndOfFrame();

        // 5. 畫面變透明（Fade In）
        yield return StartCoroutine(Fade(0f));
    }

    /// <summary>
    /// 控制 CanvasGroupAlpha 的淡入淡出協程
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        Debug.Log($"[Test] Fade 開始: target={targetAlpha}, time={Time.realtimeSinceStartup}");
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true; // 變黑過程中阻擋玩家點擊 UI
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        
        // 如果完全透明了，關閉 Raycast 阻擋
        if (targetAlpha == 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}
