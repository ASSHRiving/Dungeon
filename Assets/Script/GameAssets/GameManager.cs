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
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }
}
