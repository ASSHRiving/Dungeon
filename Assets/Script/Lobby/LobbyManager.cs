using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using DG.Tweening;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Cinemachine 相機")]
    [SerializeField] private CinemachineCamera lobbyCamera;
    [SerializeField] private CinemachineCamera characterCamera;

    [Header("大廳按鈕")]
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject exitButton;

    [Header("左側角色屬性單面板")]
    [SerializeField] private CanvasGroup leftStatsPanel;    // 左側屬性面板 (需掛載 CanvasGroup)
    [SerializeField] private Image hpFillImage;             // 生命進度條 (Image Type: Filled)
    [SerializeField] private TMP_Text hpValueText;          // 生命數值文字
    [SerializeField] private Image defFillImage;            // 防禦進度條 (Image Type: Filled)
    [SerializeField] private TMP_Text defValueText;         // 防禦數值文字
    [SerializeField] private Image atkFillImage;            // 攻擊力進度條 (Image Type: Filled)
    [SerializeField] private TMP_Text atkValueText;         // 攻擊力數值文字
    [SerializeField] private TMP_Text descriptionText;      // 職業介紹文字

    [Header("屬性滿格基準值 (決定進度條長度比例)")]
    [SerializeField] private float maxHP = 200f;
    [SerializeField] private float maxDEF = 150f;
    [SerializeField] private float maxATK = 150f;

    [Header("DOTween 動畫參數")]
    [SerializeField] private float panelAnimDuration = 0.28f;
    [SerializeField] private float panelSlideOffset = 40f;    // 左側面板從左滑入的距離
    [SerializeField] private float startBtnSlideOffset = 35f; // 開始按鈕從下滑入的距離
    [SerializeField] private float statBarDuration = 0.45f;   // 屬性條跑動時間

    private CharacterSelect currentCharacter;
    private RectTransform leftPanelRect;
    private Vector2 leftPanelOriginPos;

    private CanvasGroup startBtnCanvasGroup;
    private RectTransform startBtnRect;
    private Vector2 startBtnOriginPos;

    private Sequence uiSequence;
    private Sequence statSequence;

    public bool HasSelectedCharacter => currentCharacter != null;

    private void Awake()
    {
        Instance = this;

        if (leftStatsPanel != null)
        {
            leftPanelRect = leftStatsPanel.GetComponent<RectTransform>();
            leftPanelOriginPos = leftPanelRect.anchoredPosition;
        }

        if (startButton != null)
        {
            startBtnCanvasGroup = startButton.GetComponent<CanvasGroup>();
            if (startBtnCanvasGroup == null)
                startBtnCanvasGroup = startButton.AddComponent<CanvasGroup>();

            startBtnRect = startButton.GetComponent<RectTransform>();
            startBtnOriginPos = startBtnRect.anchoredPosition;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DeSelectImmediate();
    }

    private void OnDestroy()
    {
        uiSequence?.Kill();
        statSequence?.Kill();
    }

    public void SelectCharacter(CharacterSelect character)
    {
        if (character == null || currentCharacter == character) return;

        if (GameAssets.Instance != null)
            GameAssets.Instance.PlayUIClickSound();

        bool wasInWideLobby = (currentCharacter == null);
        currentCharacter = character;

        if (GameManager.Instance != null)
            GameManager.Instance.characterID = character.id;

        // 1. 更新頂部標題
        UIEvents.ChangeTitle(character.CharacterName);

        // 2. 切換 Cinemachine 相機焦點
        characterCamera.Target.TrackingTarget = character.CameraTarget;
        lobbyCamera.Priority = 0;
        characterCamera.Priority = 10;

        // 3. 切換左上角返回與退出按鈕
        backButton.SetActive(true);
        exitButton.SetActive(false);

        // 4. 展開左側面板與底部「開始遊戲」按鈕滑入動畫
        if (wasInWideLobby)
        {
            PlayShowCharacterUIAnim();
        }

        // 5. 更新並播放生命、防禦、攻擊力與介紹動畫
        UpdateCharacterStatsUI(character);
    }

    public void DeSelect()
    {
        if (GameAssets.Instance != null)
            GameAssets.Instance.PlayUIClickSound();

        currentCharacter = null;

        UIEvents.ChangeTitle("選 擇 角 色");
        lobbyCamera.Priority = 10;
        characterCamera.Priority = 0;

        backButton.SetActive(false);
        exitButton.SetActive(true);

        uiSequence?.Kill();
        statSequence?.Kill();
        uiSequence = DOTween.Sequence();

        // 左側面板向左滑出 + 淡出
        if (leftStatsPanel != null)
        {
            leftStatsPanel.interactable = false;
            leftStatsPanel.blocksRaycasts = false;
            uiSequence.Join(leftStatsPanel.DOFade(0f, 0.2f).SetEase(Ease.InQuad));
            uiSequence.Join(leftPanelRect.DOAnchorPos(leftPanelOriginPos - new Vector2(panelSlideOffset, 0f), 0.2f).SetEase(Ease.InCubic));
        }

        // 開始按鈕向下滑出 + 淡出
        if (startBtnCanvasGroup != null && startBtnRect != null)
        {
            startBtnCanvasGroup.interactable = false;
            startBtnCanvasGroup.blocksRaycasts = false;
            uiSequence.Join(startBtnCanvasGroup.DOFade(0f, 0.18f).SetEase(Ease.InQuad));
            uiSequence.Join(startBtnRect.DOAnchorPos(startBtnOriginPos - new Vector2(0f, startBtnSlideOffset), 0.18f).SetEase(Ease.InCubic));
        }

        uiSequence.OnComplete(() =>
        {
            if (leftStatsPanel != null) leftStatsPanel.gameObject.SetActive(false);
            if (startButton != null) startButton.SetActive(false);
        });
    }

    private void DeSelectImmediate()
    {
        currentCharacter = null;
        UIEvents.ChangeTitle("選 擇 角 色");
        lobbyCamera.Priority = 10;
        characterCamera.Priority = 0;

        if (startButton != null) startButton.SetActive(false);
        if (backButton != null) backButton.SetActive(false);
        if (exitButton != null) exitButton.SetActive(true);

        if (leftStatsPanel != null)
        {
            leftStatsPanel.alpha = 0f;
            leftStatsPanel.gameObject.SetActive(false);
        }
    }

    private void PlayShowCharacterUIAnim()
    {
        uiSequence?.Kill();
        uiSequence = DOTween.Sequence();

        // 1. 左側屬性面板從左向右滑入 + 淡入
        if (leftStatsPanel != null)
        {
            leftStatsPanel.gameObject.SetActive(true);
            leftStatsPanel.interactable = true;
            leftStatsPanel.blocksRaycasts = true;
            leftStatsPanel.alpha = 0f;
            leftPanelRect.anchoredPosition = leftPanelOriginPos - new Vector2(panelSlideOffset, 0f);

            uiSequence.Join(leftStatsPanel.DOFade(1f, panelAnimDuration).SetEase(Ease.OutQuad));
            uiSequence.Join(leftPanelRect.DOAnchorPos(leftPanelOriginPos, panelAnimDuration).SetEase(Ease.OutCubic));
        }

        // 2. 開始遊戲按鈕從下往上滑入 + 淡入
        if (startButton != null && startBtnCanvasGroup != null && startBtnRect != null)
        {
            startButton.SetActive(true);
            startBtnCanvasGroup.interactable = true;
            startBtnCanvasGroup.blocksRaycasts = true;
            startBtnCanvasGroup.alpha = 0f;
            startBtnRect.anchoredPosition = startBtnOriginPos - new Vector2(0f, startBtnSlideOffset);

            uiSequence.Join(startBtnCanvasGroup.DOFade(1f, panelAnimDuration).SetEase(Ease.OutQuad));
            uiSequence.Join(startBtnRect.DOAnchorPos(startBtnOriginPos, panelAnimDuration).SetEase(Ease.OutCubic));
        }
    }

    private void UpdateCharacterStatsUI(CharacterSelect data)
    {
        if (descriptionText != null)
            descriptionText.text = data.Description;

        statSequence?.Kill();
        statSequence = DOTween.Sequence();

        AnimateSingleStat(hpFillImage, hpValueText, data.HP, maxHP);
        AnimateSingleStat(defFillImage, defValueText, data.DEF, maxDEF);
        AnimateSingleStat(atkFillImage, atkValueText, data.ATK, maxATK);
    }

    private void AnimateSingleStat(Image fillImg, TMP_Text valText, float targetVal, float maxVal)
    {
        if (fillImg != null)
        {
            fillImg.fillAmount = 0f;
            float targetRatio = Mathf.Clamp01(targetVal / maxVal);
            statSequence.Join(fillImg.DOFillAmount(targetRatio, statBarDuration).SetEase(Ease.OutCubic));
        }

        if (valText != null)
        {
            statSequence.Join(DOVirtual.Int(0, (int)targetVal, statBarDuration, v => valText.text = v.ToString()).SetEase(Ease.OutCubic));
        }
    }
}