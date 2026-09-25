using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    private void OnEnable()
    {
        UIEvents.OnChangeTitle += ChangeTitle;
    }
    private void ChangeTitle(string text)
    {
        titleText.text = text;
    }
}
