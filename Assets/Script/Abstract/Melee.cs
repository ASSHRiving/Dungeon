using UnityEngine;

public abstract class Melee : Weapon
{
    [Header("連段設定")]
    public int totalComboSteps = 3;      // 總共有幾段
    protected int currentCombo = 0;      // 目前走到第幾段
    public float comboResetTime = 1.0f;  // 超過多久沒按就重設
    protected float lastAttackTime;
    public override void Attack(Animator anim)
    {
        // 1. 判斷是否重置連段
        if (Time.time - lastAttackTime > comboResetTime)
        {
            currentCombo = 0;
        }

        currentCombo++;
        if (currentCombo > totalComboSteps) currentCombo = 1;

        // 2. 執行攻擊動作
        ExecuteMeleeAttack(anim, currentCombo);

        // 3. 更新計時與狀態
        lastAttackTime = Time.time;

        ResetCombatTimer(); // 繼承自父類別，維持持刀姿勢
    }
    // 讓具體的刀、劍去決定怎麼播動畫（例如給不同的參數名）
    protected abstract void ExecuteMeleeAttack(Animator anim, int combo);
}
