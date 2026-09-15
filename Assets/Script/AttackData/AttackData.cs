using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAttackData", menuName = "Attack Data")]
public class AttackData : ScriptableObject
{
    public string hitAnimationName = "Hit_F";   // 受擊動畫
    public float damageMultiplier = 1.0f;       // 傷害倍率
    public float poiseDamage = 25f;             // 削韌值
    public SoundAssetsType hitSound;
    [Tooltip("打擊鏡頭抖動(輕:0.3 重:1.2)")]
    public float shakeForce;
    [Tooltip("需要開啟的hitbox(1.右手主武器)")]
    public List<int> hitboxes = new List<int> { 0 };
}