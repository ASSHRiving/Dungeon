using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;
    private PlayerInput _inputSystem;
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
            _inputSystem = FindPlayerInput();
            if(_inputSystem != null)
            {
                _inputSystem.SwitchCurrentActionMap("UI");
            }
        }
    }
    public void Confirm()
    {
        isTutorialOpen = false;
        _inputSystem = FindPlayerInput();
        if(_inputSystem != null)
        {
            _inputSystem.SwitchCurrentActionMap("Player");
        }

        tutorialPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void OpenTutorial()
    {
        isTutorialOpen = true;
        _inputSystem = FindPlayerInput();
        if(_inputSystem != null)
        {
            _inputSystem.SwitchCurrentActionMap("UI");
        }
        tutorialPanel.SetActive(true);
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
