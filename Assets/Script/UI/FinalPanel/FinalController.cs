using UnityEngine;
using TMPro;

public class FinalController : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text deathText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text killText;
    private void OnEnable()
    {
        UpdateStatsUI();
        GameManager.Instance.ResetStats();
    }
    public void UpdateStatsUI()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.isWin)
        {
            titleText.text = "恭喜通關！";
        }
        else
        {
            titleText.text = "遊戲結束！";
        }
        
        string deathCount = GameManager.Instance.deathCount.ToString();
        string killCount = GameManager.Instance.killCount.ToString();
        string timeCount = GameManager.Instance.GetFormattedPlayTime();
        stateText.text = "遊戲階段: 1-" + GameManager.Instance.currentLevel;
        deathText.text = "死亡次數: " + deathCount;
        killText.text = "殺敵數: " + killCount;
        timeText.text = "遊戲用時: " + timeCount;

    }
    public void Exit()
    {
        LoadingController.Instance.LoadLevel("MainMenu");
        GameAssets.Instance.PlayMenuMusic();
    }
}
