using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 選角大廳角色升級面板 UI 控制器 (三圍同步強化架構)
/// 負責顯示角色當前強化階級、視覺等級進度條、生命/防禦/攻擊三圍加成預覽，
/// 以及單一整合式的升級按鈕與點擊反饋。
/// </summary>
public class CharacterUpgradeUIController : MonoBehaviour
{
    [Header("持有貨幣顯示")]
    [SerializeField] private TMP_Text currentSoulsText; // 顯示玩家目前持有的總靈魂 (例如 "1,250 靈魂")

    [Header("角色等級與視覺進度條")]
    [SerializeField] private TMP_Text levelValueText;   // 例如 "Lv. 3 / 10" 或 "Lv. 10 (MAX)"
    [SerializeField] private Image levelProgressBar;    // 視覺進度條 (FillAmount 0~1)

    [Header("三圍屬性加成文字")]
    [SerializeField] private TMP_Text hpBonusText;      // 例如 "+45 HP → +60"
    [SerializeField] private TMP_Text defBonusText;     // 例如 "+9 護甲 → +12"
    [SerializeField] private TMP_Text atkBonusText;     // 例如 "+12 攻擊 → +16"

    [Header("一體化升級行動按鈕")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text buttonLabelText;  // "強 化"
    [SerializeField] private TMP_Text buttonCostText;   // "50 靈魂" 或 "已達上限"
    [SerializeField] private CanvasGroup buttonCanvasGroup;

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
    /// 刷新面板上所有階級、三圍數值、進度條與按鈕狀態
    /// </summary>
    public void RefreshAllUI()
    {
        if (CharacterUpgradeManager.Instance == null) return;

        CharacterUpgradeSnapshot snapshot = CharacterUpgradeManager.Instance.GetSnapshot(activeCharacterID);

        // 1. 等級文字
        if (levelValueText != null)
        {
            if (snapshot.isMaxLevel)
            {
                levelValueText.text = $"Lv.{snapshot.maxLevel} <color=#50E3C2>(MAX)</color>";
            }
            else
            {
                levelValueText.text = $"Lv.{snapshot.currentLevel} <color=#A0988A>/ {snapshot.maxLevel}</color>";
            }
        }

        // 2. 視覺進度條
        if (levelProgressBar != null)
        {
            float targetFill = (snapshot.maxLevel > 0) ? (float)snapshot.currentLevel / snapshot.maxLevel : 0f;
            levelProgressBar.DOKill();
            levelProgressBar.DOFillAmount(targetFill, 0.3f).SetEase(Ease.OutCubic);
        }

        // 3. 三圍屬性加成 (生命、防禦、攻擊)
        if (hpBonusText != null)
        {
            if (snapshot.isMaxLevel)
            {
                hpBonusText.text = $"+{snapshot.currentHpBonus:0} HP <color=#50E3C2>(最大)</color>";
            }
            else
            {
                hpBonusText.text = $"+{snapshot.currentHpBonus:0} HP  →  <color=#80FF80>+{snapshot.nextHpBonus:0}</color>";
            }
        }

        if (defBonusText != null)
        {
            if (snapshot.isMaxLevel)
            {
                defBonusText.text = $"+{snapshot.currentDefBonus:0} 護甲 <color=#50E3C2>(最大)</color>";
            }
            else
            {
                defBonusText.text = $"+{snapshot.currentDefBonus:0} 護甲  →  <color=#80FF80>+{snapshot.nextDefBonus:0}</color>";
            }
        }

        if (atkBonusText != null)
        {
            if (snapshot.isMaxLevel)
            {
                atkBonusText.text = $"+{snapshot.currentAtkBonus:0} 攻擊 <color=#50E3C2>(最大)</color>";
            }
            else
            {
                atkBonusText.text = $"+{snapshot.currentAtkBonus:0} 攻擊  →  <color=#80FF80>+{snapshot.nextAtkBonus:0}</color>";
            }
        }

        // 4. 一體化升級按鈕
        if (buttonLabelText != null)
        {
            buttonLabelText.text = snapshot.isMaxLevel ? "已滿級" : "強 化";
        }

        if (buttonCostText != null)
        {
            if (snapshot.isMaxLevel)
            {
                buttonCostText.text = "<color=#8E8E93>MAX</color>";
            }
            else
            {
                string colHex = snapshot.canAfford ? "#FFE28A" : "#FF6666";
                buttonCostText.text = $"<color={colHex}>{snapshot.nextCost} 靈魂</color>";
            }
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = snapshot.canAfford;
        }

        if (buttonCanvasGroup != null)
        {
            buttonCanvasGroup.alpha = snapshot.isMaxLevel ? 0.4f : (snapshot.canAfford ? 1f : 0.6f);
        }

        // 5. 持有貨幣顯示
        RefreshCurrencyDisplay();
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

    /// <summary>
    /// 點擊升級按鈕觸發 (一次強化全三圍)
    /// </summary>
    public void OnClickUpgrade()
    {
        if (CharacterUpgradeManager.Instance == null) return;

        bool success = CharacterUpgradeManager.Instance.TryUpgrade(activeCharacterID);
        if (success)
        {
            if (GameAssets.Instance != null)
            {
                GameAssets.Instance.PlayUIClickSound();
            }

            // 按鈕微震動反饋
            if (upgradeButton != null)
            {
                upgradeButton.transform.DOKill();
                upgradeButton.transform.localScale = Vector3.one;
                upgradeButton.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.2f, 10, 1f);
            }

            // 等級文字微跳動
            if (levelValueText != null)
            {
                levelValueText.transform.DOKill();
                levelValueText.transform.localScale = Vector3.one;
                levelValueText.transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.22f, 8, 1f);
            }

            RefreshAllUI();
        }
    }
}
