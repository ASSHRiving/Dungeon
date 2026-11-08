using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 選角大廳角色升級面板 UI 控制器
/// 負責顯示生命、防禦、攻擊升級等級、數值加成、消耗靈魂，
/// 並處理玩家點擊升級按鈕後的更新與回饋。
/// </summary>
public class CharacterUpgradeUIController : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeSlotUI
    {
        [Header("屬性類型")]
        public UpgradeType upgradeType;

        [Header("UI 文字元件")]
        public TMP_Text statTitleText;    // 例如 "生命強化"
        public TMP_Text levelText;        // 例如 "Lv. 3 / 10"
        public TMP_Text bonusText;        // 例如 "+45 生命" (下一級 "+60")
        public TMP_Text costText;         // 例如 "140 靈魂" (滿級顯示 "已滿級 MAX")

        [Header("互動按鈕")]
        public Button upgradeButton;
        public CanvasGroup buttonCanvasGroup; // 用於滿級或貨幣不足時半透明提示
    }

    [Header("持有貨幣顯示")]
    [SerializeField] private TMP_Text currentSoulsText; // 顯示玩家目前持有的總靈魂

    [Header("三大屬性升級插槽")]
    [SerializeField] private UpgradeSlotUI healthSlot = new UpgradeSlotUI { upgradeType = UpgradeType.Health };
    [SerializeField] private UpgradeSlotUI defenseSlot = new UpgradeSlotUI { upgradeType = UpgradeType.Defense };
    [SerializeField] private UpgradeSlotUI attackSlot = new UpgradeSlotUI { upgradeType = UpgradeType.Attack };

    private int activeCharacterID = 0;

    private void OnEnable()
    {
        CharacterUpgradeManager.OnAnyUpgradeChanged += RefreshAllUI;
        UIEvents.OnMetaCurrencyChanged += HandleCurrencyChanged;

        RefreshAllUI();
    }

    private void OnDisable()
    {
        CharacterUpgradeManager.OnAnyUpgradeChanged -= RefreshAllUI;
        UIEvents.OnMetaCurrencyChanged -= HandleCurrencyChanged;
    }

    /// <summary>
    /// 設定目前所選的角色 ID (由 LobbyManager 在切換角色時呼叫)
    /// </summary>
    public void SetActiveCharacter(int characterID)
    {
        activeCharacterID = characterID;
        RefreshAllUI();
    }

    /// <summary>
    /// 刷新所有升級按鈕、文字與持有貨幣
    /// </summary>
    public void RefreshAllUI()
    {
        RefreshSlot(healthSlot);
        RefreshSlot(defenseSlot);
        RefreshSlot(attackSlot);
        RefreshCurrencyDisplay();
    }

    private void RefreshSlot(UpgradeSlotUI slot)
    {
        if (slot == null || CharacterUpgradeManager.Instance == null) return;

        UpgradeInfo info = CharacterUpgradeManager.Instance.GetUpgradeInfo(activeCharacterID, slot.upgradeType);

        if (slot.statTitleText != null)
        {
            slot.statTitleText.text = info.displayName;
        }

        if (slot.levelText != null)
        {
            slot.levelText.text = info.isMaxLevel ? $"Lv.{info.maxLevel} (MAX)" : $"Lv.{info.currentLevel} / {info.maxLevel}";
        }

        if (slot.bonusText != null)
        {
            string unit = (slot.upgradeType == UpgradeType.Health) ? "HP" : (slot.upgradeType == UpgradeType.Defense ? "護甲" : "攻擊");
            if (info.isMaxLevel)
            {
                slot.bonusText.text = $"+{info.currentBonus:0} {unit} (最大)";
            }
            else
            {
                slot.bonusText.text = $"+{info.currentBonus:0} {unit} → <color=#80FF80>+{info.nextBonus:0}</color>";
            }
        }

        if (slot.costText != null)
        {
            if (info.isMaxLevel)
            {
                slot.costText.text = "已滿級";
            }
            else
            {
                string colorHex = info.canAfford ? "#FFFFFF" : "#FF6666";
                slot.costText.text = $"<color={colorHex}>{info.nextCost} 靈魂</color>";
            }
        }

        if (slot.upgradeButton != null)
        {
            slot.upgradeButton.interactable = info.canAfford;
        }

        if (slot.buttonCanvasGroup != null)
        {
            slot.buttonCanvasGroup.alpha = info.isMaxLevel ? 0.4f : (info.canAfford ? 1f : 0.6f);
        }
    }

    private void RefreshCurrencyDisplay()
    {
        if (currentSoulsText != null && MetaProgressionManager.Instance != null)
        {
            currentSoulsText.text = $"{MetaProgressionManager.Instance.TotalCurrency:N0} 靈魂";
        }
    }

    private void HandleCurrencyChanged(int newTotal)
    {
        RefreshAllUI();
    }

    #region 按鈕點擊綁定事件
    public void OnClickUpgradeHealth() => PerformUpgrade(UpgradeType.Health);
    public void OnClickUpgradeDefense() => PerformUpgrade(UpgradeType.Defense);
    public void OnClickUpgradeAttack() => PerformUpgrade(UpgradeType.Attack);

    private void PerformUpgrade(UpgradeType type)
    {
        if (CharacterUpgradeManager.Instance == null) return;

        bool success = CharacterUpgradeManager.Instance.TryUpgrade(activeCharacterID, type);
        if (success)
        {
            if (GameAssets.Instance != null)
            {
                GameAssets.Instance.PlayUIClickSound();
            }
            RefreshAllUI();
        }
    }
    #endregion
}
