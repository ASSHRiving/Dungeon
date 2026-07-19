using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("基礎設定")]
    public string weaponName = "武器名稱";
    public float damage = 0f;
    public float attackRate = 1f;
    protected float stateTimer;
    public float combatStateDuration = 1f;
    protected float nextAttackTime;
    public int weaponType = 0;
    public SoundAssetsType weaponSoundType = SoundAssetsType.Sword;

    public abstract void Attack(Animator anim);
    protected virtual void Update()
    {
        if (stateTimer > 0)
            stateTimer -= Time.deltaTime;
    }

    protected void ResetCombatTimer()
    {
        stateTimer = combatStateDuration;
    }

    public bool StopCombat => stateTimer <= 0f;
    
}
