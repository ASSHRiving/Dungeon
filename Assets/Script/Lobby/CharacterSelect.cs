using UnityEngine;

public class CharacterSelect : MonoBehaviour
{
    [Header("角色基本設定")]
    public int id = 0;
    [SerializeField] private string characterName = "戰士";
    [SerializeField] private Transform cameraTarget;

    [Header("角色屬性與介紹 (顯示於左側單面板)")]
    [SerializeField] private float hp = 177f;
    [SerializeField] private float def = 26.5f;
    [SerializeField] private float atk = 18f;
    [TextArea(3, 5)]
    [SerializeField] private string description = "擅長近身搏殺，擁有極高的防禦力與生存韌性，是地牢探索中最穩固的前排護盾。";

    public string CharacterName => characterName;
    public Transform CameraTarget => cameraTarget;
    public float HP => hp;
    public float DEF => def;
    public float ATK => atk;
    public string Description => description;

    public void Select()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.SelectCharacter(this);
        }
    }
}