using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBase<GameManager>
{
    [Header("遊戲進度")]
    public int currentLevel = 1;

    public void GoToNextLevel()
    {
        currentLevel++;
        Debug.Log($"[GameManager] 前往第 {currentLevel} 關！");

        // 🔑 呼叫 LoadingManager 進行淡入淡出載入
        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel(SceneManager.GetActiveScene().name);
        }
        else
        {
            // 備用方案
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    public void GameOver()
    {
        Debug.Log("[GameManager] 遊戲結束！");
        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel("MainMenu");
        }
        else
        {
            // 備用方案
            SceneManager.LoadScene("MainMenu");
        }
    }
}
