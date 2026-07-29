using UnityEngine;

public class ConfirmDialogController : MonoBehaviour
{
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
        Cursor.lockState = CursorLockMode.None;
        dialogPanel.SetActive(true);
        messageText.text = message;

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(() =>
        {
            onConfirm?.Invoke();
            dialogPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
        });

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() =>
        {
            dialogPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
        });
    }
}
