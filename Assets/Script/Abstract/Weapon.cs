using UnityEngine;
using CombatBase;


public abstract class Weapon : MonoBehaviour
{
    [Header("基礎設定")]
    public string weaponName = "武器名稱";
    public float damage = 0f;
    public int weaponType = 0;
    public int combo = 0;
    protected CharacterCombatBase _combat;
    public SoundAssetsType weaponSoundType = SoundAssetsType.Sword;
    public AnimatorOverrideController overrideController;

    private void Awake()
    {
        _combat = GetComponentInParent<CharacterCombatBase>();
    }

    public abstract void Attack(Animator anim);
}
