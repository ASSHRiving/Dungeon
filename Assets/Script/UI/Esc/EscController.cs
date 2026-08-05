using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EscController : MonoBehaviour
{
    [Header("UI 面板綁定")]
    [SerializeField] private GameObject escPanel;
    [SerializeField] private SettingsController settingsPanel;
    private PlayerInput _inputSystem;

    private bool isEscOpen;

    /// <summary>
    /// 目前 Esc 選單是否開啟。
    /// </summary>
    public bool IsEscOpen => isEscOpen;

    private void Awake()
    {
        if (escPanel == null)
        {
            Debug.LogWarning($"{nameof(EscController)} 缺少 Esc Panel 參照。", this);
            return;
        }

        isEscOpen = false;
        escPanel.SetActive(false);
    }

    private void Update()
    {
        _inputSystem = FindPlayerInput();
        if(_inputSystem != null && _inputSystem.actions["Esc"].triggered)
        {
            ToggleEsc();
        }
    }
    private PlayerInput FindPlayerInput()
    {
        if(_inputSystem == null)
        {
            _inputSystem = FindFirstObjectByType<PlayerInput>();
        }
        return _inputSystem;
    }

    public void ToggleEsc()
    {
        if (isEscOpen)
        {
            CloseEsc();
        }
        else
        {
            OpenEsc();
        }
    }

    public void OpenEsc()
    {
        _inputSystem = FindPlayerInput();
        _inputSystem.SwitchCurrentActionMap("UI");

        if (escPanel == null)
        {
            return;
        }

        escPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;
        isEscOpen = true;
    }

    public void CloseEsc()
    {
        _inputSystem = FindPlayerInput();
        _inputSystem.SwitchCurrentActionMap("Player");

        if (escPanel == null)
        {
            return;
        }

        escPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        isEscOpen = false;
    }

    /// <summary>
    /// 「繼續」按鈕使用的方法。
    /// </summary>
    public void ResumeGame()
    {
        CloseEsc();
    }

    /// <summary>
    /// 「設定」按鈕使用的方法。
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning($"{nameof(EscController)} 缺少 SettingsController 參照。", this);
            return;
        }

        CloseEsc();
        settingsPanel.OpenSettings();
    }

    /// <summary>
    /// 「退出」按鈕使用的方法。
    /// </summary>
    public void QuitGame()
    {
        if (escPanel == null)
        {
            return;
        }
        escPanel.SetActive(false);

        GameManager.Instance.QuitGame();
    }
}


