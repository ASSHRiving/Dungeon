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

    [Header("設定 UI 元件")]
    [SerializeField] private TMP_Dropdown minimapDropdown;
    [SerializeField] private Slider horizontalSlider;
    [SerializeField] private Slider verticalSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider effectSlider;

    [Header("其他控制器參照")]
    [SerializeField] private UIController uiController;
    [SerializeField] private MinimapController minimapController;
    [SerializeField] private CinemachineInputAxisController cameraInput;
    [SerializeField] public AudioMixer mainMixer;

    private bool isSettingsOpen;

    private void Start()
    {
        int savedMinimapMode = PlayerPrefs.GetInt("MinimapModeSetting", 0);
        if (minimapDropdown != null)
        {
            minimapDropdown.value = savedMinimapMode;
            minimapDropdown.RefreshShownValue();
            minimapDropdown.onValueChanged.AddListener(OnMinimapModeChanged);
        }

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

        CloseSettings();
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

    private void OnMinimapModeChanged(int modeIndex)
    {
        if (minimapController != null)
        {
            minimapController.SetMinimapMode((MinimapController.MinimapMode)modeIndex);
        }
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
}
