using UnityEngine;
using UnityEngine.InputSystem;
public class ConfirmDialogController : MonoBehaviour
{
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMPro.TMP_Text messageText;
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private UnityEngine.UI.Button cancelButton;
    private PlayerInput _inputSystem;

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
        });

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() =>
        {
            CloseDialog();
        });
    }
    private void CloseDialog()
    {
        if(dialogPanel == null) return;

        _inputSystem = FindPlayerInput();
        _inputSystem.SwitchCurrentActionMap("Player");

        dialogPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void OpenDialog()
    {
        if(dialogPanel == null) return;
        
        _inputSystem = FindPlayerInput();
        _inputSystem.SwitchCurrentActionMap("UI");

        dialogPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
    }
    private PlayerInput FindPlayerInput()
    {
        if(_inputSystem == null)
        {
            _inputSystem = FindFirstObjectByType<PlayerInput>();
        }
        return _inputSystem;
    }
}
