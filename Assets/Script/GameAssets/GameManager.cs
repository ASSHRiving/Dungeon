using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBase<GameManager>
{
    [Header("遊戲進度")]
    public int currentLevel = 1;
    public int deathCount {get; private set;}
    protected override void Awake()
    {
        base.Awake();
        deathCount = PlayerPrefs.GetInt("DeathCount", 0);
    }
    public void init()
    {
        currentLevel = 1;
    }

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
        Cursor.lockState = CursorLockMode.None;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Destroy(player);
        }

        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel("FinalScene");
        }
        else
        {
            // 備用方案
            SceneManager.LoadScene("FinalScene");
        }
    }
    public void QuitGame()
    {
        Cursor.lockState = CursorLockMode.None;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Destroy(player);
        }

        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel("MainMenu");
        }
        else
        {
            // 備用方案
            SceneManager.LoadScene("MainMenu");
        }
        GameAssets.Instance.PlayMenuMusic();
    }
    #region 統計資料
    public void AddDeath()
    {
        deathCount += 1;
        PlayerPrefs.SetInt("DeathCount", deathCount);
        PlayerPrefs.Save();
    }
    public void ResetStats()
    {
        deathCount = 0;

        PlayerPrefs.DeleteKey("DeathCount");

        PlayerPrefs.Save();
    }
    #endregion
}
