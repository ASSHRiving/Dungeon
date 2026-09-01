using UnityEngine;


public abstract class Weapon : MonoBehaviour
{
    [Header("基礎設定")]
    public string weaponName = "武器名稱";
    public float damage = 0f;
    public int weaponType = 0;
    public int combo = 0;

    [Header("攻擊檢測")]
    public Transform attackPoint;
    public float attackRadius;
    
    public SoundAssetsType weaponSoundType = SoundAssetsType.Sword;
    public AnimatorOverrideController overrideController;

    public TrailRenderer weaponTrail;

    public abstract void Attack(Animator anim);
}
