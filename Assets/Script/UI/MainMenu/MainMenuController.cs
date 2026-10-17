using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        if (GameManager.Instance != null) GameManager.Instance.init(); // 初始化遊戲進度

        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel("LobbyScene");
        }
        else
        {
            // 備用方案
            Debug.LogWarning("[MainMenuButton] LoadingController 實例不存在，直接載入場景。");
            SceneManager.LoadScene("LobbyScene");
        }
    }

    public void QuitGame()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        Application.Quit();
    }
}

