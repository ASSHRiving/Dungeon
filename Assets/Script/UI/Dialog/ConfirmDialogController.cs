using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class ConfirmDialogController : MonoBehaviour
{
    [SerializeField] private UIController uiController;
    [Header("UI Elements")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private EquipmentSlotUI slot;
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    [Header("動態參數設定")]
    [SerializeField] private float duration = 0.25f;           // 動態時間（現代風格偏好短促俐落，建議 0.2s~0.3s）
    [SerializeField] private Vector3 initialScale = new Vector3(0.88f, 0.88f, 1f); // 初始微縮大小
    private Sequence sequence;

    private void Awake()
    {
        canvasGroup = dialogPanel.GetComponent<CanvasGroup>();
        rectTransform = dialogPanel.GetComponent<RectTransform>();
        CloseDialog();
    }

    private void OnEnable()
    {
        UIEvents.OnConfirmDialogRequested += ShowConfirmDialog;
    }

    private void OnDisable()
    {
        UIEvents.OnConfirmDialogRequested -= ShowConfirmDialog;
    }

    private void ShowConfirmDialog(GameObject item, System.Action onConfirm)
    {
        OpenDialog();
        ShopItem shopItem = item.GetComponent<ShopItem>();
        messageText.text = shopItem.message;
        slot.DisplayItem(item);

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() =>
        {
            onConfirm?.Invoke();
            CloseDialog();
            GameAssets.Instance.PlayUIClickSound();
        });

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() =>
        {
            CloseDialog();
            GameAssets.Instance.PlayUIClickSound();
        });
    }
    private void CloseDialog()
    {
        if(dialogPanel == null) return;

        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("Player");
        }

        Cursor.lockState = CursorLockMode.Locked;

        //DOTween
        KillCurrentTween();
        sequence = DOTween.Sequence();
        float closeDuration = duration * 0.8f;
        sequence.Append(canvasGroup.DOFade(0f, closeDuration).SetEase(Ease.Linear));
        sequence.Join(rectTransform.DOScale(initialScale, closeDuration).SetEase(Ease.InCubic));
        sequence.OnComplete(() =>
        {
            dialogPanel.SetActive(false);
        });
        sequence.SetUpdate(true);
    }
    private void OpenDialog()
    {
        if(dialogPanel == null) return;
        
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("UI");
        }
        Cursor.lockState = CursorLockMode.None;

        //DOTween
        KillCurrentTween();
        dialogPanel.SetActive(true);
        ResetToHiddenState();
        sequence = DOTween.Sequence();
        sequence.Append(canvasGroup.DOFade(1f, duration * 0.7f).SetEase(Ease.Linear));
        sequence.Join(rectTransform.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic));
        sequence.SetUpdate(true);
    }
    private void ResetToHiddenState()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (rectTransform != null) rectTransform.localScale = initialScale;
    }

    private void KillCurrentTween()
    {
        if (sequence != null && sequence.IsActive())
        {
            sequence.Kill();
        }
    }
}
