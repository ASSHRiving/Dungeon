using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsController : UIPanelBase
{
    private CharacterStats playerStats;
    private CharacterEquipment playerEquipment;
    private CharacterCombatBase playerCombat;

    [Header("玩家狀態UI")]
    public GameObject playerStatsPanel;
    public ScrollRect statsScrollRect;

    [Header("基礎屬性文字")]
    public TMP_Text healthText;
    public TMP_Text shieldText;
    public TMP_Text attackText;
    public TMP_Text critRateText;

    [Header("擴充屬性文字 (全部屬性捲動清單)")]
    public TMP_Text critDamageText;
    public TMP_Text lifeStealText;
    public TMP_Text poiseText;
    public TMP_Text lifeOnKillText;
    public TMP_Text moveSpeedText;
    public TMP_Text hitStunReductionText;

    [Header("Weapon & Equipment Slots")]
    public EquipmentSlotUI weaponSlot;
    public List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();

    protected override GameObject PanelRoot => playerStatsPanel;
    protected override bool HideOnAwake => false;

    private void Start()
    {
        if (statsScrollRect == null && playerStatsPanel != null)
        {
            statsScrollRect = playerStatsPanel.GetComponentInChildren<ScrollRect>(true);
        }

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
            playerStats.OnHealthChanged += HandlePlayerHealthChanged;
        }

        if (statsScrollRect == null && playerStatsPanel != null)
        {
            statsScrollRect = playerStatsPanel.GetComponentInChildren<ScrollRect>(true);
        }
        if (statsScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            statsScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    protected override void OnAfterClose()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= UpdatePlayerStatsUI;
            playerStats.OnHealthChanged -= HandlePlayerHealthChanged;
        }
    }

    private void HandlePlayerHealthChanged(float current, float max)
    {
        UpdatePlayerStatsUI();
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
            if (healthText != null)
            {
                healthText.text = $"生命值: {Mathf.RoundToInt(playerStats.CurrentHealth)}/{Mathf.RoundToInt(playerStats.MaxHealth)}";
            }

            if (shieldText != null)
            {
                float shield = playerStats.Shield;
                float armorDR = Mathf.Clamp(shield / (shield + 100f), 0f, 0.85f) * 100f;
                shieldText.text = $"護甲: {Mathf.RoundToInt(shield)} (-{armorDR:0.#}%)";
            }

            float totalAtk = playerCombat != null ? playerCombat.totalDamage : playerStats.AttackDamage;
            if (attackText != null)
            {
                attackText.text = $"攻擊力: {Mathf.RoundToInt(totalAtk)}";
            }

            if (critRateText != null)
            {
                critRateText.text = $"暴擊率: {playerStats.CritRate:0.#}%";
            }

            if (critDamageText != null)
            {
                critDamageText.text = $"暴擊傷害: {playerStats.CritDamage * 100f:0.#}%";
            }

            if (lifeStealText != null)
            {
                float lifeStealPct = Mathf.Clamp01(playerStats.LifeSteal) * 100f;
                lifeStealText.text = $"命中吸血: {lifeStealPct:0.#}%";
            }

            if (poiseText != null)
            {
                poiseText.text = $"韌性: {Mathf.RoundToInt(playerStats.Poise)}";
            }

            if (lifeOnKillText != null)
            {
                lifeOnKillText.text = $"擊殺回血: {Mathf.RoundToInt(playerStats.LifeOnKill)}";
            }

            if (moveSpeedText != null)
            {
                moveSpeedText.text = $"移動速度: {playerStats.MoveSpeed:0.##}";
            }

            if (hitStunReductionText != null)
            {
                float stunRedPct = Mathf.Clamp(playerStats.HitStunReduction, 0f, 0.9f) * 100f;
                hitStunReductionText.text = $"縮短硬直: {stunRedPct:0.#}%";
            }
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
