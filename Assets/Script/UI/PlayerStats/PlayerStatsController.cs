using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerStatsController : MonoBehaviour
{
    [SerializeField] private UIController uiController;
    private CharacterStats playerStats;

    [Header("玩家狀態UI")]
    public GameObject playerStatsPanel;
    public TMP_Text healthText;
    public TMP_Text shieldText;
    public TMP_Text attackText;
    public TMP_Text critRateText;

    private bool isPanelOpen = false;
    private void Awake()
    {
        
    }

    private void Start()
    {
        if(playerStatsPanel != null)
        {
            playerStatsPanel.SetActive(false);
            isPanelOpen = false;
        }
    }

    private void Update()
    {
        if(uiController.GetPlayerInput() != null && uiController.GetPlayerInput().actions["Character"].triggered)
        {
            TogglePanel();
        }
    }

    private void TogglePanel()
    {
        if (playerStatsPanel == null) return;
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap(isPanelOpen ? "Player" : "UI");
        }
        isPanelOpen = !isPanelOpen;
        playerStatsPanel.SetActive(isPanelOpen);

        if (isPanelOpen)
        {
            UpdatePlayerStatsUI();
        }
    }

    private void UpdatePlayerStatsUI()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<CharacterStats>();
        }

        healthText.text = $"生命值: {playerStats.maxHealth}";
        shieldText.text = $"護盾值: {playerStats.shield}";
        attackText.text = $"攻擊力: {playerStats.damage}";
        critRateText.text = $"暴擊率: {playerStats.critRate}%";
    }

}
