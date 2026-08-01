using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("UI 面板綁定")]
    public GameObject settingsPanel;       // SettingsPanel 物件

    [Header("設定 UI 元件")]
    public TMP_Dropdown minimapDropdown;   // 小地圖模式 Dropdown

    [Header("其他控制器參照")]
    public MinimapController minimapController;

    private bool isSettingsOpen = false;

    private void Start()
    {
        // 1. 初始化小地圖 UI 狀態
        int savedMinimapMode = PlayerPrefs.GetInt("MinimapModeSetting", 0);
        if (minimapDropdown != null)
        {
            minimapDropdown.value = savedMinimapMode;
            minimapDropdown.RefreshShownValue();
            
            // 監聽 Dropdown 切換事件
            minimapDropdown.onValueChanged.AddListener(OnMinimapModeChanged);
        }

        // 確保進入遊戲時選單是關閉的
        CloseSettings();
    }

    private void Update()
    {
        // 按下 ESC 鍵切換開啟/關閉
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    /// <summary>
    /// 切換設定選單狀態
    /// </summary>
    public void ToggleSettings()
    {
        isSettingsOpen = !isSettingsOpen;

        if (isSettingsOpen)
        {
            OpenSettings();
        }
        else
        {
            CloseSettings();
        }
    }

    public void OpenSettings()
    {
        isSettingsOpen = true;
        settingsPanel.SetActive(true);
        Time.timeScale = 0f; // ⏸️ 暫停遊戲時間
        
        // 可在此解鎖/顯示滑鼠游標（如果是 FPS/3D 遊戲）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSettings()
    {
        isSettingsOpen = false;
        settingsPanel.SetActive(false);
        Time.timeScale = 1f; // ▶️ 恢復遊戲時間

        // 保存所有設定
        PlayerPrefs.Save();

        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    // ==========================================
    // ⚙️ 各項設定觸發的回呼函式 (Event Callbacks)
    // ==========================================

    private void OnMinimapModeChanged(int modeIndex)
    {
        if (minimapController != null)
        {
            minimapController.SetMinimapMode((MinimapController.MinimapMode)modeIndex);
        }
    }

    private void OnVolumeChanged(float volume)
    {
        AudioListener.volume = volume; // 動態修改全局音量
        PlayerPrefs.SetFloat("MasterVolumeSetting", volume);
    }
}