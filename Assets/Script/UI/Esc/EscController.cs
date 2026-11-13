using UnityEngine;

public class EscController : UIPanelBase
{
    [Header("UI 面板綁定")]
    [SerializeField] private GameObject escPanel;
    [SerializeField] private CanvasGroup windowCanvasGroup;
    [SerializeField] private RectTransform windowRect;

    [Header("外部UI連結")]
    [SerializeField] private SettingsController settingsPanel;

    protected override GameObject PanelRoot => escPanel;
    protected override CanvasGroup WindowCanvasGroup => windowCanvasGroup;
    protected override RectTransform WindowRect => windowRect;

    /// <summary>
    /// 目前 Esc 選單是否開啟。
    /// </summary>
    public bool IsEscOpen => IsOpen;

    protected override void Awake()
    {
        if (escPanel == null)
        {
            Debug.LogWarning($"{nameof(EscController)} 缺少 Esc Panel 參照。", this);
            return;
        }

        base.Awake();
    }

    public void ToggleEsc() => Toggle();

    public void OpenEsc() => Open();

    public void CloseEsc() => Close();

    /// <summary>
    /// 「繼續」按鈕使用的方法。
    /// </summary>
    public void ResumeGame()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        CloseEsc();
    }

    /// <summary>
    /// 「設定」按鈕使用的方法。
    /// </summary>
    public void OpenSettings()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
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
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        if (escPanel == null)
        {
            return;
        }

        HideImmediate();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddDeath();
            GameManager.Instance.GameOver();
        }
    }
}



