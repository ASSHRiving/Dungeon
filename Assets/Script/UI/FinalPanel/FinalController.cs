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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UpdateStatsUI();
    }

    public void UpdateStatsUI()
    {
        if (GameManager.Instance == null) return;

        if (titleText != null)
        {
            titleText.text = GameManager.Instance.isWin ? "恭喜通關！" : "遊戲結束！";
        }

        if (stateText != null) stateText.text = $"遊戲階段: 1-{GameManager.Instance.currentLevel}";
        if (deathText != null) deathText.text = $"死亡次數: {GameManager.Instance.deathCount}";
        if (killText != null) killText.text = $"殺敵數: {GameManager.Instance.killCount}";
        if (timeText != null) timeText.text = $"遊戲用時: {GameManager.Instance.GetFormattedPlayTime()}";
    }

    public void Exit()
    {
        if (GameAssets.Instance != null)
        {
            GameAssets.Instance.PlayUIClickSound();
            GameAssets.Instance.PlayMenuMusic();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetStats();
        }

        if (LoadingController.Instance != null)
        {
            LoadingController.Instance.LoadLevel("MainMenu");
        }
    }
}

