using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("基礎設定")]
    public string weaponName;
    public float damage;
    public float attackRate;
    protected float stateTimer;
    public float combatStateDuration;
    protected float nextAttackTime;
    public int weaponType;

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
