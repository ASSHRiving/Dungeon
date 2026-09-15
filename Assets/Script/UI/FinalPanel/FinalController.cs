using UnityEngine;
using TMPro;

public class FinalController : MonoBehaviour
{
    [SerializeField] private TMP_Text deathText;
    private void OnEnable()
    {
        UpdateStatsUI();
        GameManager.Instance.ResetStats();
    }
    public void UpdateStatsUI()
    {
        if (GameManager.Instance == null) return;
        string deathCount = GameManager.Instance.deathCount.ToString();
        deathText.text = "死亡次數: " + deathCount;

    }
    public void Exit()
    {
        LoadingController.Instance.LoadLevel("MainMenu");
        GameAssets.Instance.PlayMenuMusic();
    }
}
