using UnityEngine;

public abstract class Melee : Weapon
{
    [Header("連段設定")]
    [SerializeField] protected int totalComboSteps = 5;      // 總共有幾段
    [SerializeField] protected float comboResetTime = 1.0f;  // 超過多久沒按就重設
    protected float lastAttackTime;
    public override void Attack(Animator anim)
    {
        // 1. 判斷是否重置連段
        if (Time.time - lastAttackTime > comboResetTime)
        {
            combo = 0;
        }

        combo++;
        if (combo > totalComboSteps) combo = 1;

        // 2. 執行攻擊動作
        AttackAnimation(anim, combo);

        // 3. 更新計時與狀態
        lastAttackTime = Time.time;

    }
    // 讓具體的刀、劍去決定怎麼播動畫（例如給不同的參數名）
    protected abstract void AttackAnimation(Animator anim, int combo);
    
    public void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
