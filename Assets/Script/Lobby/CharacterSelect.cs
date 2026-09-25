using UnityEngine;

public class CharacterSelect : MonoBehaviour
{
    [SerializeField] private string characterName;
    [SerializeField] private Transform cameraTarget;

    public Transform CameraTarget => cameraTarget;

    public void Select()
    {
        LobbyManager.Instance.SelectCharacter(this);
        UIEvents.ChangeTitle(characterName);
    }

    public void Deselect()
    {
        UIEvents.ChangeTitle("選擇角色");
    }
}
