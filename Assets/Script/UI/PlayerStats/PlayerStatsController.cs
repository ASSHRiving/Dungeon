using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerStatsController : UIPanelBase
{
    private CharacterStats playerStats;
    private CharacterEquipment playerEquipment;
    private CharacterCombatBase playerCombat;

    [Header("玩家狀態UI")]
    public GameObject playerStatsPanel;
    public TMP_Text healthText;
    public TMP_Text shieldText;
    public TMP_Text attackText;
    public TMP_Text critRateText;

    [Header("Weapon & Equipment Slots")]
    public EquipmentSlotUI weaponSlot;
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
        HookPlayer();
        UpdatePlayerStatsUI();
        if (playerStats != null)
        {
            playerStats.OnStatsChanged += UpdatePlayerStatsUI;
        }
    }

    protected override void OnAfterClose()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= UpdatePlayerStatsUI;
        }
    }

    private void HookPlayer()
    {
        if (uiController == null) return;
        Transform playerTransform = uiController.GetPlayer();
        if (playerTransform == null) return;

        if (playerStats == null)
        {
            playerStats = playerTransform.GetComponent<CharacterStats>() ?? playerTransform.GetComponentInChildren<CharacterStats>();
        }
        if (playerEquipment == null)
        {
            playerEquipment = playerTransform.GetComponent<CharacterEquipment>() ?? playerTransform.GetComponentInChildren<CharacterEquipment>();
        }
        if (playerCombat == null)
        {
            playerCombat = playerTransform.GetComponentInChildren<CharacterCombatBase>();
        }
    }

    private void UpdatePlayerStatsUI()
    {
        HookPlayer();

        if (playerStats != null)
        {
            if (healthText != null) healthText.text = $"生命值: {Mathf.RoundToInt(playerStats.MaxHealth)}";
            if (shieldText != null) shieldText.text = $"護甲: {Mathf.RoundToInt(playerStats.Shield)}";
            if (attackText != null) attackText.text = $"攻擊力: {Mathf.RoundToInt(playerStats.AttackDamage)}";
            if (critRateText != null) critRateText.text = $"暴擊率: {playerStats.CritRate:0.#}%";
        }

        if (weaponSlot != null)
        {
            IItem currentWeapon = playerCombat != null ? playerCombat.currentWeapon : null;
            weaponSlot.DisplayItem(currentWeapon);
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
