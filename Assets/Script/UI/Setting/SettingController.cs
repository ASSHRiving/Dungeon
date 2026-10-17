using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using DG.Tweening;

public class SettingsController : UIPanelBase
{
    private const string BgmVolumePrefKey = "BGMVolume";
    private const string EffectVolumePrefKey = "EffectVolume";
    private const string HorizontalSensPrefKey = "HorizontalSensitivity";
    private const string VerticalSensPrefKey = "VerticalSensitivity";
    private const string RebindsPrefKey = "Rebinds";

    private const string ActionLeftAttack = "Player/LAtk";
    private const string ActionRightAttack = "Player/RAtk";
    private const string ActionRun = "Player/Run";
    private const string ActionInteract = "Player/Interact";

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
    [SerializeField] private MinimapController minimapController;
    [SerializeField] private CinemachineInputAxisController cameraInput;
    [SerializeField] public AudioMixer mainMixer;

    [Header("分頁切換動態設定")]
    [SerializeField] private float tabSwitchDuration = 0.18f;                       // 分頁切換淡入時間
    [SerializeField] private float tabSlideOffset = 12f;                            // 分頁切換微上浮距離(px)

    private Sequence tabSequence;
    private Tweener rebindBlinkTween;
    private InputActionRebindingExtensions.RebindingOperation currentRebindOp;

    // 快取各分頁的初始座標，防止快速切換或中途關閉視窗導致分頁位置偏移
    private readonly Dictionary<RectTransform, Vector2> initialTabPositions = new Dictionary<RectTransform, Vector2>();

    protected override GameObject PanelRoot => settingsPanel;
    protected override bool HideOnAwake => false;

    protected override void Awake()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning($"{nameof(SettingsController)} 缺少 Settings Panel 參照。", this);
            return;
        }
        base.Awake();
    }

    private void Start()
    {
        LoadMinimap();      // 初始化小地圖
        LoadSensitivity();  // 初始化靈敏度
        LoadVolume();       // 初始化音效
        LoadRebinds();      // 初始化按鍵綁定
        InitType();         // 初始化頁面

        HideImmediate();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        tabSequence?.Kill();
        rebindBlinkTween?.Kill();
        currentRebindOp?.Dispose();
    }

    public void OpenSettings() => Open();

    public void CloseSettings()
    {
        if (GameAssets.Instance != null)
        {
            GameAssets.Instance.PlayUIClickSound();
        }
        Close();
    }

    protected override void OnBeforeClose()
    {
        // 若正在等待按鍵綁定，先取消綁定操作
        currentRebindOp?.Cancel();
        PlayerPrefs.Save();
    }

    public override void OnEscPressed()
    {
        // 若正在進行按鍵重綁，按 Esc 優先取消重綁，避免直接關閉整個設定視窗
        if (currentRebindOp != null)
        {
            currentRebindOp.Cancel();
            return;
        }
        CloseSettings();
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

        if (soundType != null) soundType.SetActive(targetPage == soundType);
        if (imageType != null) imageType.SetActive(targetPage == imageType);
        if (preferType != null) preferType.SetActive(targetPage == preferType);

        // 更新三個分頁按鈕的底圖與文字顏色
        UpdateTabButtonVisual(soundTabBtnImage, activeBtnImg == soundTabBtnImage);
        UpdateTabButtonVisual(imageTabBtnImage, activeBtnImg == imageTabBtnImage);
        UpdateTabButtonVisual(preferTabBtnImage, activeBtnImg == preferTabBtnImage);

        if (!playAnim || targetPage == null) return;

        if (!targetPage.TryGetComponent(out CanvasGroup pageCG))
        {
            pageCG = targetPage.AddComponent<CanvasGroup>();
        }

        RectTransform pageRT = targetPage.GetComponent<RectTransform>();
        if (pageRT == null) return;

        if (!initialTabPositions.TryGetValue(pageRT, out Vector2 targetPos))
        {
            targetPos = pageRT.anchoredPosition;
            initialTabPositions[pageRT] = targetPos;
        }

        tabSequence?.Kill();
        pageCG.alpha = 0f;
        pageRT.anchoredPosition = targetPos - new Vector2(0f, tabSlideOffset);

        tabSequence = DOTween.Sequence();
        tabSequence.Append(pageCG.DOFade(1f, tabSwitchDuration).SetEase(Ease.OutQuad));
        tabSequence.Join(pageRT.DOAnchorPos(targetPos, tabSwitchDuration).SetEase(Ease.OutCubic));
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
        int savedMinimapMode = PlayerPrefs.GetInt(MinimapController.MinimapModePrefKey, 0);
        if (minimapDropdown != null)
        {
            minimapDropdown.SetValueWithoutNotify(savedMinimapMode);
            minimapDropdown.RefreshShownValue();
            minimapDropdown.onValueChanged.RemoveListener(OnMinimapModeChanged);
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
        else
        {
            PlayerPrefs.SetInt(MinimapController.MinimapModePrefKey, modeIndex);
        }
    }
    #endregion

    #region 音效
    private void LoadVolume()
    {
        float bgmVal = PlayerPrefs.GetFloat(BgmVolumePrefKey, 0.5f);
        float effectVal = PlayerPrefs.GetFloat(EffectVolumePrefKey, 0.5f);

        BindSlider(bgmSlider, bgmVal, SetBGMVolume);
        BindSlider(effectSlider, effectVal, SetEffectVolume);

        SetBGMVolume(bgmVal);
        SetEffectVolume(effectVal);
    }

    public void SetBGMVolume(float value)
    {
        ApplyMixerVolume(BgmVolumePrefKey, value);
    }

    public void SetEffectVolume(float value)
    {
        ApplyMixerVolume(EffectVolumePrefKey, value);
    }

    private void ApplyMixerVolume(string parameterName, float linearValue)
    {
        // 避免 linearValue 為 0 時 Log10(0) 產生 -Infinity 導致混音器異常
        float safeValue = Mathf.Max(linearValue, 0.0001f);
        float adjustedValue = Mathf.Pow(safeValue, 2f);
        float dB = Mathf.Log10(adjustedValue) * 20f;

        if (mainMixer != null)
        {
            mainMixer.SetFloat(parameterName, dB);
        }
        PlayerPrefs.SetFloat(parameterName, linearValue);
    }
    #endregion

    #region 靈敏度
    private void LoadSensitivity()
    {
        float savedHorizontalSensitivity = PlayerPrefs.GetFloat(HorizontalSensPrefKey, 6f);
        float savedVerticalSensitivity = PlayerPrefs.GetFloat(VerticalSensPrefKey, 6f);

        BindSlider(horizontalSlider, savedHorizontalSensitivity, ChangeHorizontalSensitivity);
        BindSlider(verticalSlider, savedVerticalSensitivity, ChangeVerticalSensitivity);

        ChangeHorizontalSensitivity(savedHorizontalSensitivity);
        ChangeVerticalSensitivity(savedVerticalSensitivity);
    }

    public void ChangeHorizontalSensitivity(float value)
    {
        if (cameraInput == null || cameraInput.Controllers.Count < 1) return;
        cameraInput.Controllers[0].Input.Gain = value;
        PlayerPrefs.SetFloat(HorizontalSensPrefKey, value);
    }

    public void ChangeVerticalSensitivity(float value)
    {
        if (cameraInput == null || cameraInput.Controllers.Count < 2) return;
        cameraInput.Controllers[1].Input.Gain = -value;
        PlayerPrefs.SetFloat(VerticalSensPrefKey, value);
    }

    private void BindSlider(Slider slider, float initialValue, UnityAction<float> callback)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(initialValue);
        slider.onValueChanged.RemoveListener(callback);
        slider.onValueChanged.AddListener(callback);
    }
    #endregion

    #region 按鍵綁定 (含 DOTween 等待閃爍與完成回饋)
    public void RebindLAtk()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        StartRebind(ActionLeftAttack, 0, lAtkText);
    }

    public void RebindRAtk()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        StartRebind(ActionRightAttack, 0, rAtkText);
    }

    public void RebindRun()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        StartRebind(ActionRun, 0, runText);
    }

    public void RebindInteract()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        StartRebind(ActionInteract, 0, interactText);
    }

    private void StartRebind(string actionName, int bindingIndex, TMP_Text keyText)
    {
        if (inputActions == null) return;

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
        if (action == null || keyText == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count) return;
        keyText.text = InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    private void SaveRebinds()
    {
        if (inputActions == null) return;
        string json = inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(RebindsPrefKey, json);
        PlayerPrefs.Save();
    }

    private void LoadRebinds()
    {
        if (inputActions == null) return;

        if (PlayerPrefs.HasKey(RebindsPrefKey))
        {
            string json = PlayerPrefs.GetString(RebindsPrefKey);
            if (!string.IsNullOrEmpty(json))
            {
                inputActions.LoadBindingOverridesFromJson(json);
            }
        }
        UpdateKeyText(inputActions.FindAction(ActionLeftAttack), 0, lAtkText);
        UpdateKeyText(inputActions.FindAction(ActionRightAttack), 0, rAtkText);
        UpdateKeyText(inputActions.FindAction(ActionRun), 0, runText);
        UpdateKeyText(inputActions.FindAction(ActionInteract), 0, interactText);
    }
    #endregion
}