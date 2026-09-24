using UnityEngine;
using Unity.Cinemachine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [SerializeField] private CinemachineCamera lobbyCamera;
    [SerializeField] private CinemachineCamera characterCamera;

    private CharacterSelect currentCharacter;

    private void Awake()
    {
        Instance = this;
    }

    public void SelectCharacter(CharacterSelect character)
    {
        currentCharacter = character;

        characterCamera.Target.TrackingTarget =
            character.CameraTarget;

        lobbyCamera.Priority = 0;
        characterCamera.Priority = 10;
    }
}
