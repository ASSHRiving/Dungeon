using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerStatsController : MonoBehaviour
{
    [SerializeField] private UIController uiController;
    private CharacterStats playerStats;
    private CharacterEquipment playerEquipment;

    [Header("玩家狀態UI")]
    public GameObject playerStatsPanel;
    public TMP_Text healthText;
    public TMP_Text shieldText;
    public TMP_Text attackText;
    public TMP_Text critRateText;
    
    [Header("Equipment Slots")]
    public List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();

    private bool isPanelOpen = false;

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
        else if(uiController.GetPlayerInput() != null && uiController.GetPlayerInput().actions["Esc"].triggered && isPanelOpen)
        {
            TogglePanel();
        }
    }

    private void TogglePanel()
    {
        if (isPanelOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }
    public void OpenPanel()
    {
        isPanelOpen = true;
        if (playerStatsPanel == null) return;
        playerStatsPanel.SetActive(true);
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("UI");
        }
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;

        UpdatePlayerStatsUI();
    }
    public void ClosePanel()
    {
        isPanelOpen = false;
        if (playerStatsPanel == null) return;
        playerStatsPanel.SetActive(false);
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("Player");
        }
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;

    }

    private void UpdatePlayerStatsUI()
    {
        if (playerStats == null)
        {
            playerStats = uiController.GetPlayer().GetComponent<CharacterStats>();
        }
        if (playerEquipment == null)
        {
            playerEquipment = uiController.GetPlayer().GetComponent<CharacterEquipment>();
        }

        healthText.text = $"生命值: {playerStats.maxHealth}";
        shieldText.text = $"護盾值: {playerStats.shield}";
        attackText.text = $"攻擊力: {playerStats.damage}";
        critRateText.text = $"暴擊率: {playerStats.critRate}%";

        if(equipmentSlots != null)
        {
            foreach(var slot in equipmentSlots)
            {
                if(slot == null) continue;
                GameObject equipment = playerEquipment.GetEquippedItem(slot.slotType);
                slot.DisplayItem(equipment);
            }
        }
    }

}
