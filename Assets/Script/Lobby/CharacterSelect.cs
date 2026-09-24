using UnityEngine;

public class CharacterSelect : MonoBehaviour
{
    [SerializeField] private string characterName;
    [SerializeField] private Transform cameraTarget;

    public Transform CameraTarget => cameraTarget;

    public void Select()
    {
        LobbyManager.Instance.SelectCharacter(this);
    }

    public void Deselect()
    {
        // 移除高亮
    }
}
