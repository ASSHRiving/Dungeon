using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [SerializeField] private CinemachineCamera lobbyCamera;
    [SerializeField] private CinemachineCamera characterCamera;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject exitButton;

    private CharacterSelect currentCharacter;

    private void Awake()
    {
        Instance = this;
        DeSelect();
    }

    public void SelectCharacter(CharacterSelect character)
    {
        startButton.SetActive(true);
        backButton.SetActive(true);
        exitButton.SetActive(false);
        currentCharacter = character;

        characterCamera.Target.TrackingTarget =
            character.CameraTarget;

        lobbyCamera.Priority = 0;
        characterCamera.Priority = 10;
    }
    public void DeSelect()
    {
        startButton.SetActive(false);
        backButton.SetActive(false);
        exitButton.SetActive(true);
        currentCharacter = null;
        lobbyCamera.Priority = 10;
        characterCamera.Priority = 0;
        UIEvents.ChangeTitle("選擇角色");
    }
}
