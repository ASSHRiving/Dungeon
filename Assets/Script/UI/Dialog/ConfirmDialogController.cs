using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class ConfirmDialogController : MonoBehaviour
{
    [SerializeField] private UIController uiController;
    [Header("UI Elements")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMPro.TMP_Text messageText;
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private UnityEngine.UI.Button cancelButton;


    private void OnEnable()
    {
        UIEvents.OnConfirmDialogRequested += ShowConfirmDialog;
    }

    private void OnDisable()
    {
        UIEvents.OnConfirmDialogRequested -= ShowConfirmDialog;
    }

    private void ShowConfirmDialog(string message, System.Action onConfirm)
    {
        OpenDialog();
        messageText.text = message;

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

        dialogPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void OpenDialog()
    {
        if(dialogPanel == null) return;
        
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("UI");
        }

        dialogPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
    }
}
