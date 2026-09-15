using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAttackData", menuName = "Attack Data")]
public class AttackData : ScriptableObject
{
    public string hitAnimationName = "Hit_F";   // 受擊動畫
    public float damageMultiplier = 1.0f;       // 傷害倍率
    public float poiseDamage = 25f;             // 削韌值
    public SoundAssetsType hitSound;
    public List<int> hitboxs;
}