using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private UIController uiController;
    [SerializeField] private GameObject tutorialPanel;
    private bool isTutorialOpen = false;
    private void Start()
    {
        if(GameManager.Instance.currentLevel == 1)
        {
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
        isTutorialOpen = false;
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("Player");
        }

        tutorialPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void OpenTutorial()
    {
        isTutorialOpen = true;
        if(uiController.GetPlayerInput() != null)
        {
            uiController.GetPlayerInput().SwitchCurrentActionMap("UI");
        }
        tutorialPanel.SetActive(true);
    }
}
