using UnityEngine;
using TMPro;

public class FinalController : MonoBehaviour
{
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
        string deathCount = GameManager.Instance.deathCount.ToString();
        string killCount = GameManager.Instance.killCount.ToString();
        string timeCount = GameManager.Instance.GetFormattedPlayTime();
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
