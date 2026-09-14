using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

public class SettingsController : MonoBehaviour
{
    [Header("UI 面板綁定")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Input Actions")]
    public InputActionAsset inputActions;

    [Header("設定 UI 元件")]
    [SerializeField] private GameObject soundType;
    [SerializeField] private GameObject imageType;
    [SerializeField] private GameObject preferType;
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

    private bool isSettingsOpen;

    private void Start()
    {
        LoadMinimap();      //初始化小地圖
        LoadSensitivity();  //初始化靈敏度
        LoadVolume();       //初始化音效
        LoadRebinds();      //初始化按鍵綁定
        InitType();         //初始化頁面
        
        //CloseSettings();
        settingsPanel.SetActive(false);
    }
    private void Update()
    {
        if(uiController.GetPlayerInput() != null && uiController.GetPlayerInput().actions["Esc"].triggered)
        {
            if (isSettingsOpen)
            {
                CloseSettings();
            }
        }
    }
    public void OpenSettings()
    {
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("UI");
        }

        isSettingsOpen = true;
        settingsPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;
    }

    public void CloseSettings()
    {
        GameAssets.Instance.PlayUIClickSound();
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("Player");
        }

        isSettingsOpen = false;
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        PlayerPrefs.Save();
        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    
    #region 小地圖
    private void LoadMinimap()
    {
        int savedMinimapMode = PlayerPrefs.GetInt("MinimapModeSetting", 0);
        if (minimapDropdown != null)
        {
            minimapDropdown.value = savedMinimapMode;
            minimapDropdown.RefreshShownValue();
            minimapDropdown.onValueChanged.AddListener(OnMinimapModeChanged);
        }
    }
    private void OnMinimapModeChanged(int modeIndex)
    {
        GameAssets.Instance.PlayUIClickSound();
        if (minimapController != null)
        {
            minimapController.SetMinimapMode((MinimapController.MinimapMode)modeIndex);
        }
    }
    #endregion
    #region 分頁
    private void InitType()
    {
        ShowSoundType();
    }
    public void ShowSoundType()
    {
        soundType.SetActive(true);
        imageType.SetActive(false);
        preferType.SetActive(false);
    }
    public void ShowImageType()
    {
        soundType.SetActive(false);
        imageType.SetActive(true);
        preferType.SetActive(false);
    }
    public void ShowPreferType()
    {
        soundType.SetActive(false);
        imageType.SetActive(false);
        preferType.SetActive(true);
    }
    #endregion
    #region 音效
    private void LoadVolume()
    {
        float bgmVal = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        float effectVal = PlayerPrefs.GetFloat("EffectVolume", 0.5f);
        if(bgmSlider != null)
        {
            bgmSlider.value = bgmVal;
        }
        if(effectSlider != null)
        {
            effectSlider.value = effectVal;
        }
        SetBGMVolume(bgmVal);
        SetEffectVolume(effectVal);

        // 動態綁定 UI Slider 事件
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        effectSlider.onValueChanged.AddListener(SetEffectVolume);
    }
    public void SetBGMVolume(float value)
    {
        float adjustedValue = Mathf.Pow(value, 2);
        float dB = Mathf.Log10(adjustedValue) * 20;
        mainMixer.SetFloat("BGMVolume", dB);
        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    public void SetEffectVolume(float value)
    {
        float adjustedValue = Mathf.Pow(value, 2);
        float dB = Mathf.Log10(adjustedValue) * 20;
        mainMixer.SetFloat("EffectVolume", dB);
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
        }
        ChangeHorizontalSensitivity(savedHorizontalSensitivity);

        if (verticalSlider != null)
        {
            verticalSlider.SetValueWithoutNotify(savedVerticalSensitivity);
        }
        ChangeVerticalSensitivity(savedVerticalSensitivity);
    }
    public void ChangeHorizontalSensitivity(float value)
    {
        if (cameraInput == null || cameraInput.Controllers.Count < 1)
        {
            return;
        }

        cameraInput.Controllers[0].Input.Gain = value;
        PlayerPrefs.SetFloat("HorizontalSensitivity", value);
    }

    public void ChangeVerticalSensitivity(float value)
    {
        if (cameraInput == null || cameraInput.Controllers.Count < 2)
        {
            return;
        }

        cameraInput.Controllers[1].Input.Gain = -value;
        PlayerPrefs.SetFloat("VerticalSensitivity", value);
    }
    #endregion
    #region 按鍵綁定
    public void RebindLAtk()
    {
        StartRebind("Player/LAtk", 0, lAtkText);
    }
    public void RebindRAtk()
    {
        StartRebind("Player/RAtk", 0, rAtkText);
    }
    public void RebindRun()
    {
        StartRebind("Player/Run", 0, runText);
    }
    public void RebindInteract()
    {
        StartRebind("Player/Interact", 0, interactText);
    }
    private void StartRebind(
        string actionName,
        int bindingIndex,
        TMP_Text keyText)
    {
        InputAction action = inputActions.FindAction(actionName);

        if (action == null)
        {
            Debug.LogError("找不到 Input Action:" + actionName);
            return;
        }

        action.Disable();

        action.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(operation =>
            {
                action.Enable();

                UpdateKeyText(action, bindingIndex, keyText);
                SaveRebinds();
                operation.Dispose();
            })
            .Start();
    }
    private void UpdateKeyText(
        InputAction action,
        int bindingIndex,
        TMP_Text keyText)
    {
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
