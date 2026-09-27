using UnityEngine;
using TMPro;

public class InteractController : MonoBehaviour
{
    [SerializeField] private TMP_Text interactText; // 用於顯示交互提示的 UI Text 元件
    private void OnEnable()
    {
        UIEvents.OnInteractableDetected += UpdateInteractUI;
    }
    private void OnDisable()
    {
        UIEvents.OnInteractableDetected -= UpdateInteractUI;
    }
    private void UpdateInteractUI(string interactableName)
    {
        if (!string.IsNullOrEmpty(interactableName))
        {
            interactText.text = $"[E] {interactableName}";
            interactText.gameObject.SetActive(true);
        }
        else
        {
            interactText.gameObject.SetActive(false);
        }
    }
}
