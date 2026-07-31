using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("基礎設定")]
    public string weaponName = "武器名稱";
    public float damage = 0f;
    public int weaponType = 0;
    public int combo = 0;
    public SoundAssetsType weaponSoundType = SoundAssetsType.Sword;
    public AnimatorOverrideController overrideController;

    public abstract void Attack(Animator anim);
}
