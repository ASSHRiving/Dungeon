using UnityEngine;

public abstract class AbilityBase : ScriptableObject
{
    [Header("技能基本資訊")]
    public string skillName;
    public string animTriggerName;  // Animator 的 Trigger 名稱
    public float minDistance = 0f;  // 技能最低使用距離
    public float maxDistance = 5f;  // 技能最高使用距離
    public float cooldown = 5f;     // 冷卻時間
    public int weight = 1;          // 隨機權重 (機率)

    [Header("技能距離調整設定")]
    public float chaseSpeed = 3.5f;         // 太遠時的追擊速度
    public float retreatSpeed = 1.8f;       // 太近時的後退速度
    public float maxAdjustDuration = 4.0f;  // 調整距離最大容許秒數（超時重選技能，防止卡牆死鎖）

    // 執行技能
    /// <summary>
    /// 執行技能決策：委派給 movement 追擊、後退，距離適中時釋放技能
    /// </summary>
    public virtual void ExecuteSkill(StateMachineSystem system)
    {
        if (system == null || system.combat == null || system.movement == null || system.animator == null)
            return;

        Transform target = system.combat.GetCurrentTarget();
        if (target == null) return;

        float distance = system.combat.GetCurrentTargetDistance();
        system.skillAdjustTimer += Time.deltaTime;

        // 防卡死保護：若因地形阻擋或玩家拉扯導致長時間無法進入技能距離，放棄當前鎖定並重選符合當前距離的技能
        if (system.skillAdjustTimer >= maxAdjustDuration)
        {
            system.skillAdjustTimer = 0f;
            AbilityBase fallbackSkill = system.SelectReadySkill(distance);
            system.currentSkill = fallbackSkill;
            if (fallbackSkill == null)
            {
                system.movement.StopMoving();
                return;
            }
            if (fallbackSkill != this)
            {
                fallbackSkill.ExecuteSkill(system);
                return;
            }
        }

        // 1. 距離太遠：呼叫移動系統追擊
        if (distance > maxDistance)
        {
            system.movement.ChaseTarget(target, chaseSpeed);
        }
        // 2. 距離太近：呼叫移動系統後退（若背後貼牆無路可退，直接釋放技能避免卡死）
        else if (distance < minDistance - 0.1f)
        {
            if (!system.movement.TryRetreatFromTarget(target, retreatSpeed))
            {
                TriggerAndCompleteSkill(system);
            }
        }
        // 3. 距離適合：停止移動並釋放技能
        else
        {
            TriggerAndCompleteSkill(system);
        }
    }

    /// <summary>
    /// 停止移動、觸發技能、重置 CD 並清空當前鎖定技能
    /// </summary>
    protected void TriggerAndCompleteSkill(StateMachineSystem system)
    {
        system.movement.StopMoving();
        OnCastSkill(system);

        system.ResetSkillCooldown(this);
        system.currentSkill = null;
        system.skillAdjustTimer = 0f;
    }

    /// <summary>
    /// 距離適合時實際觸發的技能行為（子類別可覆寫擴充，預設觸發動畫 Trigger）
    /// </summary>
    protected virtual void OnCastSkill(StateMachineSystem system)
    {
        system.animator.SetTrigger(animTriggerName);
    }
}
