using UnityEngine;

/// <summary>
/// 角色/職業基礎數值配置資產
/// 選角大廳 (Lobby) 與局內生成主角時共享此配置
/// </summary>
[CreateAssetMenu(fileName = "CharacterData_", menuName = "Dungeon/Stats/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("角色基本資訊")]
    public int characterID = 0;
    public string characterName = "戰士";
    [TextArea(2, 4)]
    public string description = "擅長近身搏殺，擁有極高的防禦力與生存韌性，是地牢探索中最穩固的前排護盾。";
    public Sprite icon;
    public GameObject characterPrefab;

    [Header("基礎屬性 (Base Stats)")]
    public float baseMaxHealth = 100f;
    public float baseShield = 10f;
    public float baseAttackDamage = 15f;
    public float baseCritRate = 5f;
    public float baseMoveSpeed = 7f;
    public float basePoise = 100f;
}
