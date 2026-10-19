using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ResultUIController : MonoBehaviour
{
    [Header("背景與頂部主標題")]
    public Image backgroundImage;
    public Image headerBannerImage;
    public Sprite victoryBgSprite;
    public Sprite defeatBgSprite;
    public Sprite victoryHeaderSprite;
    public Sprite defeatHeaderSprite;
    public TMP_Text mainTitleText;       // 顯示 "遠 征 完 成" 或 "殞 命 地 牢"
    public TMP_Text subTitleText;        // 顯示 "EXPEDITION COMPLETED" 或 "EXPEDITION FAILED"
    public Color victoryTitleColor = new Color(0.96f, 0.94f, 0.90f, 1f);
    public Color defeatTitleColor = new Color(0.99f, 0.65f, 0.65f, 1f);

    [Header("區塊動態容器 (CanvasGroup)")]
    public CanvasGroup topCardsGroup;    // 頂部四連戰績卡容器
    public CanvasGroup leftStatsPanel;   // 左下角色與屬性容器
    public CanvasGroup rightEquipPanel;  // 右下主武器與 6 格防具容器
    public CanvasGroup bottomBtnsGroup;  // 底部雙按鈕容器

    [Header("頂部四連戰績卡文字")]
    public TMP_Text stageProgressText;   // 顯示 "1 - 3"
    public TMP_Text stageNameText;       // 顯示 "深淵石牢・第三區"
    public TMP_Text playTimeText;        // 顯示 "00 : 18 : 42"
    public TMP_Text killCountText;       // 顯示 "148"
    public TMP_Text goldCountText;       // 顯示 "1,260 G"

    [Header("左下角色最終屬性文字")]
    public TMP_Text characterNameText;   // 顯示 "角色：戰士"
    public TMP_Text hpText;              // 顯示 "160"
    public TMP_Text defText;             // 顯示 "89"
    public TMP_Text atkText;             // 顯示 "100"

    [Header("右下主武器與六部位裝備文字")]
    public TMP_Text weaponNameText;      // 顯示 "主武器：噬星者"
    public TMP_Text weaponSubText;       // 顯示 "單手長劍 | 攻擊 +42"
    public Image weaponIconImage;
    public TMP_Text[] armorNameTexts = new TMP_Text[6]; // 頭盔、胸甲、護腿、手套、腰帶、戰靴

    private Sequence resultSequence;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        RunResultData data = (GameManager.Instance != null && GameManager.Instance.LastRunResult != null)
            ? GameManager.Instance.LastRunResult
            : CreateDefaultPreviewData();

        PopulateAndAnimateUI(data);
    }

    private void OnDestroy()
    {
        resultSequence?.Kill();
    }

    private RunResultData CreateDefaultPreviewData()
    {
        var d = new RunResultData
        {
            isVictory = false,
            stageProgress = "1 - 3",
            stageName = "深淵石牢・第三區",
            playTimeText = "00 : 18 : 42",
            killCount = 148,
            goldCount = 1260,
            characterName = "戰士",
            hp = 160,
            def = 89,
            atk = 100,
            mainWeapon = new EquipmentSlotData { slotCategory = "主武器", itemName = "噬星者", subInfo = "單手長劍 | 攻擊 +42" }
        };
        string[] slots = { "頭盔", "胸甲", "護腿", "手套", "腰帶", "戰靴" };
        string[] names = { "鍛鐵戰盔", "岩峰重鎧", "灰岩護腿", "鎖子鐵腕", "重裝扣帶", "暗鐵脛靴" };
        for (int i = 0; i < 6; i++)
        {
            d.armors[i] = new EquipmentSlotData { slotCategory = slots[i], itemName = names[i] };
        }
        return d;
    }

    public void PopulateAndAnimateUI(RunResultData data)
    {
        // 1. 切換勝利/失敗背景與頂部標題
        if (backgroundImage != null)
        {
            Sprite bg = data.isVictory ? victoryBgSprite : defeatBgSprite;
            if (bg != null) backgroundImage.sprite = bg;
        }
        if (headerBannerImage != null)
        {
            Sprite hb = data.isVictory ? victoryHeaderSprite : defeatHeaderSprite;
            if (hb != null) headerBannerImage.sprite = hb;
        }
        if (mainTitleText != null)
        {
            mainTitleText.text = data.isVictory ? "遠 征 完 成" : "殞 命 地 牢";
            mainTitleText.color = data.isVictory ? victoryTitleColor : defeatTitleColor;
        }
        if (subTitleText != null)
        {
            subTitleText.text = data.isVictory ? "EXPEDITION COMPLETED" : "EXPEDITION FAILED";
        }

        // 2. 填入四連卡與左下角色文字
        if (stageProgressText != null) stageProgressText.text = data.stageProgress;
        if (stageNameText != null) stageNameText.text = data.stageName;
        if (playTimeText != null) playTimeText.text = data.playTimeText;
        if (characterNameText != null) characterNameText.text = $"角色：{data.characterName}";

        if (hpText != null) hpText.text = data.hp.ToString();
        if (defText != null) defText.text = data.def.ToString();
        if (atkText != null) atkText.text = data.atk.ToString();

        // 3. 填入武器與 6 格防具
        if (data.mainWeapon != null)
        {
            if (weaponNameText != null) weaponNameText.text = $"主武器：{data.mainWeapon.itemName}";
            if (weaponSubText != null) weaponSubText.text = data.mainWeapon.subInfo;
            if (weaponIconImage != null && data.mainWeapon.icon != null)
                weaponIconImage.sprite = data.mainWeapon.icon;
        }

        for (int i = 0; i < armorNameTexts.Length; i++)
        {
            if (i < data.armors.Length && data.armors[i] != null && armorNameTexts[i] != null)
            {
                armorNameTexts[i].text = data.armors[i].itemName;
            }
        }

        // 4. DOTween 進場動畫
        resultSequence?.Kill();
        resultSequence = DOTween.Sequence();

        if (topCardsGroup != null)
        {
            topCardsGroup.alpha = 0f;
            RectTransform rt = topCardsGroup.GetComponent<RectTransform>();
            Vector2 origin = rt.anchoredPosition;
            rt.anchoredPosition = origin + new Vector2(0f, 25f);
            resultSequence.Join(topCardsGroup.DOFade(1f, 0.28f).SetEase(Ease.OutQuad));
            resultSequence.Join(rt.DOAnchorPos(origin, 0.28f).SetEase(Ease.OutCubic));
        }

        if (leftStatsPanel != null)
        {
            leftStatsPanel.alpha = 0f;
            RectTransform rt = leftStatsPanel.GetComponent<RectTransform>();
            Vector2 origin = rt.anchoredPosition;
            rt.anchoredPosition = origin - new Vector2(35f, 0f);
            resultSequence.Join(leftStatsPanel.DOFade(1f, 0.3f).SetDelay(0.08f).SetEase(Ease.OutQuad));
            resultSequence.Join(rt.DOAnchorPos(origin, 0.3f).SetDelay(0.08f).SetEase(Ease.OutCubic));
        }

        if (rightEquipPanel != null)
        {
            rightEquipPanel.alpha = 0f;
            RectTransform rt = rightEquipPanel.GetComponent<RectTransform>();
            Vector2 origin = rt.anchoredPosition;
            rt.anchoredPosition = origin + new Vector2(35f, 0f);
            resultSequence.Join(rightEquipPanel.DOFade(1f, 0.3f).SetDelay(0.12f).SetEase(Ease.OutQuad));
            resultSequence.Join(rt.DOAnchorPos(origin, 0.3f).SetDelay(0.12f).SetEase(Ease.OutCubic));
        }

        if (killCountText != null)
        {
            killCountText.text = "0";
            resultSequence.Join(DOVirtual.Int(0, data.killCount, 0.65f, v => killCountText.text = v.ToString()).SetEase(Ease.OutCubic));
        }
        if (goldCountText != null)
        {
            goldCountText.text = "0 G";
            resultSequence.Join(DOVirtual.Int(0, data.goldCount, 0.75f, v => goldCountText.text = $"{v:N0} G").SetEase(Ease.OutCubic));
        }

        if (bottomBtnsGroup != null)
        {
            bottomBtnsGroup.alpha = 0f;
            RectTransform rt = bottomBtnsGroup.GetComponent<RectTransform>();
            Vector2 origin = rt.anchoredPosition;
            rt.anchoredPosition = origin - new Vector2(0f, 25f);
            resultSequence.Join(bottomBtnsGroup.DOFade(1f, 0.25f).SetDelay(0.2f).SetEase(Ease.OutQuad));
            resultSequence.Join(rt.DOAnchorPos(origin, 0.25f).SetDelay(0.2f).SetEase(Ease.OutCubic));
        }
    }

    public void OnClickMainMenu()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        if (GameManager.Instance != null) GameManager.Instance.QuitGame();
    }

    public void OnClickRetry()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        if (GameManager.Instance != null) GameManager.Instance.init();
        if (LoadingController.Instance != null) LoadingController.Instance.LoadLevel("LobbyScene");
    }
}