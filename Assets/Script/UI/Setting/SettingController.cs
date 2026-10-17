using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using DG.Tweening;

public class SettingsController : MonoBehaviour
{
    [Header("UI 面板綁定")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Input Actions")]
    public InputActionAsset inputActions;

    [Header("分頁容器與按鈕視覺")]
    [SerializeField] private GameObject soundType;
    [SerializeField] private GameObject imageType;
    [SerializeField] private GameObject preferType;
    [SerializeField] private Image soundTabBtnImage;
    [SerializeField] private Image imageTabBtnImage;
    [SerializeField] private Image preferTabBtnImage;
    [SerializeField] private Sprite tabActiveSprite;
    [SerializeField] private Sprite tabInactiveSprite;
    [SerializeField] private Color activeTabTextColor = new Color(0.96f, 0.94f, 0.90f, 1f);
    [SerializeField] private Color inactiveTabTextColor = new Color(0.55f, 0.53f, 0.50f, 1f);

    [Header("設定 UI 元件")]
    [SerializeField] private TMP_Dropdown minimapDropdown;
    [SerializeField] private Slider horizontalSlider;
    [SerializeField] private Slider verticalSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider effectSlider;
    [SerializeField] private TMP_Text lAtkText;
    [SerializeField] private TMP_Text rAtkText;
    [SerializeField] private TMP_Text runText;
    [SerializeField] private TMP_Text interactText;

    [Header("其他控制器參照")]
    [SerializeField] private UIController uiController;
    [SerializeField] private MinimapController minimapController;
    [SerializeField] private CinemachineInputAxisController cameraInput;
    [SerializeField] public AudioMixer mainMixer;

    [Header("DOTween 動態參數設定")]
    [SerializeField] private float duration = 0.22f;                                // 開關面板時間
    [SerializeField] private Vector3 initialScale = new Vector3(0.92f, 0.92f, 1f); // 初始微縮大小
    [SerializeField] private float tabSwitchDuration = 0.18f;                       // 分頁切換淡入時間
    [SerializeField] private float tabSlideOffset = 12f;                            // 分頁切換微上浮距離(px)

    private bool isSettingsOpen;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Sequence panelSequence;
    private Sequence tabSequence;
    private Tweener rebindBlinkTween;
    private InputActionRebindingExtensions.RebindingOperation currentRebindOp;

    private void Awake()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning($"{nameof(SettingsController)} 缺少 Settings Panel 參照。", this);
            return;
        }
        canvasGroup = settingsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = settingsPanel.AddComponent<CanvasGroup>();
        rectTransform = settingsPanel.GetComponent<RectTransform>();
    }

    private void Start()
    {
        LoadMinimap();      // 初始化小地圖
        LoadSensitivity();  // 初始化靈敏度
        LoadVolume();       // 初始化音效
        LoadRebinds();      // 初始化按鍵綁定
        InitType();         // 初始化頁面

        settingsPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        panelSequence?.Kill();
        tabSequence?.Kill();
        rebindBlinkTween?.Kill();
        currentRebindOp?.Dispose();
    }

    private void Update()
    {
        if (uiController != null && uiController.GetPlayerInput() != null && uiController.GetPlayerInput().actions["Esc"].triggered)
        {
            if (isSettingsOpen)
            {
                CloseSettings();
            }
        }
    }

    public void OpenSettings()
    {
        if (uiController != null && uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("UI");
        }

        isSettingsOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // DOTween 開場動畫
        KillCurrentTween();
        settingsPanel.SetActive(true);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        ResetToHiddenState();

        panelSequence = DOTween.Sequence();
        panelSequence.Append(canvasGroup.DOFade(1f, duration * 0.75f).SetEase(Ease.OutQuad));
        panelSequence.Join(rectTransform.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));
        panelSequence.SetUpdate(true);
    }

    public void CloseSettings()
    {
        // 若正在等待按鍵綁定，先取消綁定操作
        currentRebindOp?.Cancel();

        if (GameAssets.Instance != null)
            GameAssets.Instance.PlayUIClickSound();

        if (uiController != null && uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("Player");
        }

        isSettingsOpen = false;
        Time.timeScale = 1f;
        PlayerPrefs.Save();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 關閉期間禁止重複點擊
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // DOTween 退場動畫
        KillCurrentTween();
        panelSequence = DOTween.Sequence();
        float closeDuration = duration * 0.75f;
        panelSequence.Append(canvasGroup.DOFade(0f, closeDuration).SetEase(Ease.InQuad));
        panelSequence.Join(rectTransform.DOScale(initialScale, closeDuration).SetEase(Ease.InCubic));
        panelSequence.OnComplete(() =>
        {
            settingsPanel.SetActive(false);
        });
        panelSequence.SetUpdate(true);
    }

    private void ResetToHiddenState()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (rectTransform != null) rectTransform.localScale = initialScale;
    }

    private void KillCurrentTween()
    {
        if (panelSequence != null && panelSequence.IsActive())
        {
            panelSequence.Kill();
        }
    }

    #region 分頁與 DOTween 轉場
    private void InitType()
    {
        SwitchTabInternal(soundType, soundTabBtnImage, playAnim: false);
    }

    public void ShowSoundType()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        SwitchTabInternal(soundType, soundTabBtnImage, playAnim: true);
    }

    public void ShowImageType()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        SwitchTabInternal(imageType, imageTabBtnImage, playAnim: true);
    }

    public void ShowPreferType()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        SwitchTabInternal(preferType, preferTabBtnImage, playAnim: true);
    }

    private void SwitchTabInternal(GameObject targetPage, Image activeBtnImg, bool playAnim)
    {
        // 如果已經在當前分頁則不重複播放動畫
        if (playAnim && targetPage != null && targetPage.activeSelf) return;

        soundType.SetActive(targetPage == soundType);
        imageType.SetActive(targetPage == imageType);
        preferType.SetActive(targetPage == preferType);

        // 更新三個分頁按鈕的底圖與文字顏色
        UpdateTabButtonVisual(soundTabBtnImage, activeBtnImg == soundTabBtnImage);
        UpdateTabButtonVisual(imageTabBtnImage, activeBtnImg == imageTabBtnImage);
        UpdateTabButtonVisual(preferTabBtnImage, activeBtnImg == preferTabBtnImage);

        if (!playAnim || targetPage == null) return;

        // 為目標分頁自動取得或掛載 CanvasGroup 進行淡入與微上浮動畫
        CanvasGroup pageCG = targetPage.GetComponent<CanvasGroup>();
        if (pageCG == null) pageCG = targetPage.AddComponent<CanvasGroup>();
        RectTransform pageRT = targetPage.GetComponent<RectTransform>();

        tabSequence?.Kill(complete: true);
        pageCG.alpha = 0f;

        Vector2 targetPos = pageRT.anchoredPosition;
        pageRT.anchoredPosition = targetPos - new Vector2(0f, tabSlideOffset);

        tabSequence = DOTween.Sequence();
        tabSequence.Append(pageCG.DOFade(1f, tabSwitchDuration).SetEase(Ease.OutQuad));
        tabSequence.Join(pageRT.DOAnchorPos(targetPos, tabSwitchDuration).SetEase(Ease.OutCubic));
        /*
        if (activeBtnImg != null)
        {
            activeBtnImg.transform.localScale = Vector3.one;
            tabSequence.Join(activeBtnImg.transform.DOPunchScale(new Vector3(0.05f, 0.05f, 0f), 0.15f, 6, 0.5f));
        }
        */
        tabSequence.SetUpdate(true);
    }

    private void UpdateTabButtonVisual(Image btnImg, bool isSelected)
    {
        if (btnImg == null) return;
        if (tabActiveSprite != null && tabInactiveSprite != null)
        {
            btnImg.sprite = isSelected ? tabActiveSprite : tabInactiveSprite;
        }

        TMP_Text btnText = btnImg.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            btnText.color = isSelected ? activeTabTextColor : inactiveTabTextColor;
        }
    }
    #endregion

    #region 小地圖
    private void LoadMinimap()
    {
        int savedMinimapMode = PlayerPrefs.GetInt("MinimapModeSetting", 0);
        if (minimapDropdown != null)
        {
            minimapDropdown.SetValueWithoutNotify(savedMinimapMode);
            minimapDropdown.RefreshShownValue();
            minimapDropdown.onValueChanged.AddListener(OnMinimapModeChanged);
        }
    }

    private void OnMinimapModeChanged(int modeIndex)
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        if (minimapController != null)
        {
            minimapController.SetMinimapMode((MinimapController.MinimapMode)modeIndex);
        }
        PlayerPrefs.SetInt("MinimapModeSetting", modeIndex);
    }
    #endregion

    #region 音效
    private void LoadVolume()
    {
        float bgmVal = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        float effectVal = PlayerPrefs.GetFloat("EffectVolume", 0.5f);

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(bgmVal);
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }
        if (effectSlider != null)
        {
            effectSlider.SetValueWithoutNotify(effectVal);
            effectSlider.onValueChanged.AddListener(SetEffectVolume);
        }

        SetBGMVolume(bgmVal);
        SetEffectVolume(effectVal);
    }

    public void SetBGMVolume(float value)
    {
        // 避免 value 為 0 時 Log10(0) 產生 -Infinity 導致混音器異常
        float safeValue = Mathf.Max(value, 0.0001f);
        float adjustedValue = Mathf.Pow(safeValue, 2f);
        float dB = Mathf.Log10(adjustedValue) * 20f;
        if (mainMixer != null) mainMixer.SetFloat("BGMVolume", dB);
        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    public void SetEffectVolume(float value)
    {
        float safeValue = Mathf.Max(value, 0.0001f);
        float adjustedValue = Mathf.Pow(safeValue, 2f);
        float dB = Mathf.Log10(adjustedValue) * 20f;
        if (mainMixer != null) mainMixer.SetFloat("EffectVolume", dB);
        PlayerPrefs.SetFloat("EffectVolume", value);
    }
    #endregion

    #region 靈敏度
    private void LoadSensitivity()
    {
        float savedHorizontalSensitivity = PlayerPrefs.GetFloat("HorizontalSensitivity", 6f);
        float savedVerticalSensitivity = PlayerPrefs.GetFloat("VerticalSensitivity", 6f);

        if (horizontalSlider != null)
        {
            horizontalSlider.SetValueWithoutNotify(savedHorizontalSensitivity);
            horizontalSlider.onValueChanged.AddListener(ChangeHorizontalSensitivity);
        }
        ChangeHorizontalSensitivity(savedHorizontalSensitivity);

        if (verticalSlider != null)
        {
            verticalSlider.SetValueWithoutNotify(savedVerticalSensitivity);
            verticalSlider.onValueChanged.AddListener(ChangeVerticalSensitivity);
        }
        ChangeVerticalSensitivity(savedVerticalSensitivity);
    }

    public void ChangeHorizontalSensitivity(float value)
    {
        if (cameraInput == null || cameraInput.Controllers.Count < 1) return;
        cameraInput.Controllers[0].Input.Gain = value;
        PlayerPrefs.SetFloat("HorizontalSensitivity", value);
    }

    public void ChangeVerticalSensitivity(float value)
    {
        if (cameraInput == null || cameraInput.Controllers.Count < 2) return;
        cameraInput.Controllers[1].Input.Gain = -value;
        PlayerPrefs.SetFloat("VerticalSensitivity", value);
    }
    #endregion

    #region 按鍵綁定 (含 DOTween 等待閃爍與完成回饋)
    public void RebindLAtk()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        StartRebind("Player/LAtk", 0, lAtkText);
    }

    public void RebindRAtk()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        StartRebind("Player/RAtk", 0, rAtkText);
    }

    public void RebindRun()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        StartRebind("Player/Run", 0, runText);
    }

    public void RebindInteract()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        StartRebind("Player/Interact", 0, interactText);
    }

    private void StartRebind(string actionName, int bindingIndex, TMP_Text keyText)
    {
        InputAction action = inputActions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError("找不到 Input Action: " + actionName);
            return;
        }

        // 若上一個綁定尚未結束，先取消
        currentRebindOp?.Cancel();
        rebindBlinkTween?.Kill();

        action.Disable();

        // 啟動 DOTween 文字呼吸閃爍提示
        if (keyText != null)
        {
            keyText.text = "請按任意鍵...";
            keyText.alpha = 1f;
            rebindBlinkTween = keyText.DOFade(0.25f, 0.45f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        currentRebindOp = action.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(operation =>
            {
                FinishRebind(action, bindingIndex, keyText, operation);
                SaveRebinds();
            })
            .OnCancel(operation =>
            {
                FinishRebind(action, bindingIndex, keyText, operation);
            })
            .Start();
    }

    private void FinishRebind(InputAction action, int bindingIndex, TMP_Text keyText, InputActionRebindingExtensions.RebindingOperation operation)
    {
        rebindBlinkTween?.Kill();
        action.Enable();

        if (keyText != null)
        {
            keyText.alpha = 1f;
            UpdateKeyText(action, bindingIndex, keyText);
            // 綁定完成時給文字一個輕微彈跳確認感
            keyText.transform.localScale = Vector3.one;
            keyText.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.2f, 8, 0.5f).SetUpdate(true);
        }

        operation.Dispose();
        currentRebindOp = null;
    }

    private void UpdateKeyText(InputAction action, int bindingIndex, TMP_Text keyText)
    {
        if (action == null || keyText == null) return;
        keyText.text = InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    private void SaveRebinds()
    {
        string json = inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("Rebinds", json);
        PlayerPrefs.Save();
    }

    private void LoadRebinds()
    {
        if (PlayerPrefs.HasKey("Rebinds"))
        {
            string json = PlayerPrefs.GetString("Rebinds");
            inputActions.LoadBindingOverridesFromJson(json);
        }
        UpdateKeyText(inputActions.FindAction("Player/LAtk"), 0, lAtkText);
        UpdateKeyText(inputActions.FindAction("Player/RAtk"), 0, rAtkText);
        UpdateKeyText(inputActions.FindAction("Player/Run"), 0, runText);
        UpdateKeyText(inputActions.FindAction("Player/Interact"), 0, interactText);
    }
    #endregion
}