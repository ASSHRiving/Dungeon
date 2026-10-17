using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EscController : MonoBehaviour
{
    [SerializeField] private UIController uiController;
    [Header("UI 面板綁定")]
    [SerializeField] private GameObject escPanel;
    [SerializeField] private CanvasGroup windowCanvasGroup;
    [SerializeField] private RectTransform windowRect;
    [Header("動態參數設定")]
    [SerializeField] private float duration = 0.25f;           // 動態時間（現代風格偏好短促俐落，建議 0.2s~0.3s）
    [SerializeField] private Vector3 initialScale = new Vector3(0.88f, 0.88f, 1f); // 初始微縮大小
    Sequence sequence;

    [Header("外部UI連結")]
    [SerializeField] private SettingsController settingsPanel;
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
        ResetToHiddenState();
        escPanel.SetActive(false);
    }

    private void Update()
    {
        if(uiController.GetPlayerInput() != null && uiController.GetPlayerInput().actions["Esc"].triggered)
        {
            ToggleEsc();
        }
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
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("UI");
        }

        if (escPanel == null)
        {
            return;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;
        isEscOpen = true;

        //DOTween
        KillCurrentTween();
        escPanel.SetActive(true);
        ResetToHiddenState();
        sequence = DOTween.Sequence();
        sequence.Append(windowCanvasGroup.DOFade(1f, duration * 0.7f).SetEase(Ease.Linear));
        sequence.Join(windowRect.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));
        sequence.SetUpdate(true);
    }

    public void CloseEsc()
    {
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("Player");
        }

        if (escPanel == null)
        {
            return;
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
        isEscOpen = false;

        //DOTween
        KillCurrentTween();
        sequence = DOTween.Sequence();
        float closeDuration = duration * 0.8f;
        sequence.Append(windowCanvasGroup.DOFade(0f, closeDuration).SetEase(Ease.Linear));
        sequence.Join(windowRect.DOScale(initialScale, closeDuration).SetEase(Ease.InCubic));
        sequence.OnComplete(() =>
        {
            escPanel.SetActive(false);
        });
        sequence.SetUpdate(true);
    }

    /// <summary>
    /// 「繼續」按鈕使用的方法。
    /// </summary>
    public void ResumeGame()
    {
        GameAssets.Instance.PlayUIClickSound();
        CloseEsc();
    }

    /// <summary>
    /// 「設定」按鈕使用的方法。
    /// </summary>
    public void OpenSettings()
    {
        GameAssets.Instance.PlayUIClickSound();
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
        GameAssets.Instance.PlayUIClickSound();
        if (escPanel == null)
        {
            return;
        }
        escPanel.SetActive(false);
        GameManager.Instance.AddDeath();
        GameManager.Instance.GameOver();
    }

    private void ResetToHiddenState()
    {
        if (windowCanvasGroup != null) windowCanvasGroup.alpha = 0f;
        if (windowRect != null) windowRect.localScale = initialScale;
    }
    private void KillCurrentTween()
    {
        if (sequence != null && sequence.IsActive())
        {
            sequence.Kill();
        }
    }
}


