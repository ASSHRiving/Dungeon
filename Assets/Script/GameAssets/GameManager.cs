using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonBase<GameManager>
{
    [Header("遊戲進度")]
    public int currentLevel = 1;
    public Transform player;

    [Header("統計資料")]
    public int deathCount {get; private set;}
    public float totalPlayTime;
    public int killCount;

    private void OnEnable()
    {
        GameEvent.OnPlayerSpawn += SetPlayer;
    }

    protected override void Awake()
    {
        base.Awake();
        deathCount = PlayerPrefs.GetInt("DeathCount", 0);
        totalPlayTime = PlayerPrefs.GetFloat("TotalPlayTime", 0f);
    }
    private void Update()
    {
        totalPlayTime += Time.unscaledDeltaTime;
    }
    public void init()
    {
        currentLevel = 1;
    }
    private void SetPlayer(Transform transform)
    {
        player = transform;
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
    public void StartStats()
    {
        deathCount = 0;
    }
    public void AddDeath()
    {
        deathCount += 1;
        PlayerPrefs.SetInt("DeathCount", deathCount);
        PlayerPrefs.Save();
    }
    public void AddKill()
    {
        killCount += 1;
    }
    public void ResetStats()
    {
        deathCount = 0;
        totalPlayTime = 0;

        PlayerPrefs.DeleteKey("DeathCount");
        PlayerPrefs.DeleteKey("TotalPlayTime");

        PlayerPrefs.Save();
    }
    public void OnApplicationQuit()
    {
        PlayerPrefs.SetFloat("TotalPlayTime", totalPlayTime);
        PlayerPrefs.Save();
    }
    public void OApplicationPause(bool pause)
    {
        if (pause)
        {
            PlayerPrefs.SetFloat("TotalPlayTime", totalPlayTime);
            PlayerPrefs.Save();
        }
    }
    public string GetFormattedPlayTime()
    {
        int totalSeconds = Mathf.FloorToInt(totalPlayTime);

        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
    #endregion
}
