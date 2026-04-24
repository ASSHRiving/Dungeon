using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("基礎設定")]
    public string weaponName;
    public float damage = 10f;
    public float attackRate = 0.5f;
    public LayerMask shootMask;
    protected float stateTimer;
    public float combatStateDuration = 3f;
    protected float nextAttackTime;

    public abstract void Attack(Animator anim);
    protected virtual void Update()
    {
        if (stateTimer > 0)
            stateTimer -= Time.deltaTime;
    }

    public void ResetCombatTimer()
    {
        stateTimer = combatStateDuration;
    }
    public bool StopCombat => stateTimer <= 0f;
    
}
