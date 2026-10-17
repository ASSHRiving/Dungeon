using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerStatsController : UIPanelBase
{
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

    protected override GameObject PanelRoot => playerStatsPanel;
    protected override bool HideOnAwake => false;

    private void Start()
    {
        if (playerStatsPanel != null)
        {
            HideImmediate();
        }
    }

    public void TogglePanel() => Toggle();

    public void OpenPanel() => Open();

    public void ClosePanel() => Close();

    protected override void OnBeforeOpen()
    {
        UpdatePlayerStatsUI();
    }

    private void UpdatePlayerStatsUI()
    {
        if (uiController == null) return;

        Transform playerTransform = uiController.GetPlayer();
        if (playerTransform == null) return;

        if (playerStats == null)
        {
            playerStats = playerTransform.GetComponent<CharacterStats>();
        }
        if (playerEquipment == null)
        {
            playerEquipment = playerTransform.GetComponent<CharacterEquipment>();
        }

        if (playerStats != null)
        {
            if (healthText != null) healthText.text = $"生命值: {playerStats.maxHealth}";
            if (shieldText != null) shieldText.text = $"護甲: {playerStats.shield}";
            if (attackText != null) attackText.text = $"攻擊力: {playerStats.damage}";
            if (critRateText != null) critRateText.text = $"暴擊率: {playerStats.critRate}%";
        }

        if (equipmentSlots != null && playerEquipment != null)
        {
            foreach (var slot in equipmentSlots)
            {
                if (slot == null) continue;
                GameObject equipment = playerEquipment.GetEquippedItem(slot.slotType);
                slot.DisplayItem(equipment);
            }
        }
    }
}

