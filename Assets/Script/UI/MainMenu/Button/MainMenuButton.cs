using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "GameScene"; // 預設下一關場景名稱
    public void StartGame()
    {
        GameAssets.Instance.PlayUIClickSound();
        GameManager.Instance.init(); // 初始化遊戲進度
        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel(nextSceneName);
        }
        else
        {
            // 備用方案
            Debug.LogWarning("[MainMenuButton] LoadingController 實例不存在，直接載入場景。");
            SceneManager.LoadScene(nextSceneName);
        }
        GameAssets.Instance.PlayInGameMusic();
    }

    public void QuitGame()
    {
        GameAssets.Instance.PlayUIClickSound();
        Application.Quit();
    }
}
