using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "GameScene"; // 預設下一關場景名稱
    public void StartGame()
    {
        Debug.Log("[Test] 按鈕被點擊了: " + Time.realtimeSinceStartup);
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
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
