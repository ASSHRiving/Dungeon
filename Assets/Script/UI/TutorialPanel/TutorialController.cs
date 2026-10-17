using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class TutorialController : MonoBehaviour
{
    [Header("UI 組件引用")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;   // 背景遮罩的透明度控制
    [SerializeField] private RectTransform windowRect;         // 彈窗本體 RectTransform
    [SerializeField] private CanvasGroup windowCanvasGroup;    // 彈窗本體的透明度控制

    [Header("動態參數設定")]
    [SerializeField] private float duration = 0.25f;           // 動態時間（現代風格偏好短促俐落，建議 0.2s~0.3s）
    [SerializeField] private Vector3 initialScale = new Vector3(0.88f, 0.88f, 1f); // 初始微縮大小
    Sequence sequence;

    [SerializeField] private UIController uiController;
    [SerializeField] private GameObject tutorialPanel;
    private bool isTutorialOpen = false;
    private void Start()
    {
        if(GameManager.Instance.currentLevel == 1)
        {
            Debug.Log("Tutorial Open");
            isTutorialOpen = true;
            OpenTutorial();
        }
        else
        {
            isTutorialOpen = false;
            tutorialPanel.SetActive(false);
        }
    }
    void Update()
    {
        if(isTutorialOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            if(uiController.GetPlayerInput() != null)
            {
                uiController.GetPlayerInput().SwitchCurrentActionMap("UI");
            }
        }
    }
    public void Confirm()
    {
        GameAssets.Instance.PlayUIClickSound();
        isTutorialOpen = false;
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("Player");
        }
        Cursor.lockState = CursorLockMode.Locked;

        //DOTween
        KillCurrentTween();
        sequence = DOTween.Sequence();
        float closeDuration = duration * 0.8f;
        sequence.Append(overlayCanvasGroup.DOFade(0f, closeDuration).SetEase(Ease.Linear));
        sequence.Join(windowRect.DOScale(initialScale, closeDuration).SetEase(Ease.InCubic));
        sequence.Join(windowCanvasGroup.DOFade(0f, closeDuration * 0.8f).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            tutorialPanel.SetActive(false);
        });
        sequence.SetUpdate(true);
    }
    public void OpenTutorial()
    {
        isTutorialOpen = true;
        tutorialPanel.SetActive(true);

        //DOTween
        KillCurrentTween();
        gameObject.SetActive(true);
        ResetToHiddenState();
        sequence = DOTween.Sequence();
        sequence.Append(overlayCanvasGroup.DOFade(1f, duration * 0.8f).SetEase(Ease.Linear));
        sequence.Join(windowRect.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));
        sequence.Join(windowCanvasGroup.DOFade(1f, duration * 0.7f).SetEase(Ease.OutQuad));
        sequence.SetUpdate(true);

        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("UI");
        }
    }
    private void ResetToHiddenState()
    {
        if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 0f;
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
