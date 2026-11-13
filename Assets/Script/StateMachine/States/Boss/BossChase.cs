using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "BossChase", menuName = "StateMachine/States/Boss/BossChase")]
public class BossChase : StateActionSO
{
    [Header("戰鬥距離判定(無可用技能時的走位)")]
    [SerializeField] private float minDistance = 2.4f;      // 太近界線 (後退)
    [SerializeField] private float maxDistance = 6.6f;      // 太遠界線 (追擊)

    [Header("移動速度")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float strafeSpeed = 1.6f;
    [SerializeField] private float retreatSpeed = 1.8f;

    [Header("徘徊計時")]
    [SerializeField] private float strafeChangeInterval = 2.0f;

    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        Animator animator = stateMachineSystem.animator;
        if (animator != null)
        {
            animator.SetFloat(lockOnID, 1);
            animator.Play("Ready");
        }

        stateMachineSystem.EnsureMinSkillCooldown(1f);
        stateMachineSystem.strafeTimer = 0f;
        stateMachineSystem.attackTimer = 0f;
        stateMachineSystem.skillAdjustTimer = 0f;
        stateMachineSystem.currentSkill = null;
        stateMachineSystem.UpdateRandomHorizontal();
    }

    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        EnemyCombatSystem combat = stateMachineSystem.combat;
        Animator animator = stateMachineSystem.animator;
        EnemyMovementSystem movement = stateMachineSystem.movement;

        if (combat == null || animator == null || movement == null || combat.GetCurrentTarget() == null) 
            return;

        Transform targetTransform = combat.GetCurrentTarget();
        Transform selfTransform = stateMachineSystem.transform;
        float distance = combat.GetCurrentTargetDistance();

        stateMachineSystem.strafeTimer += Time.deltaTime;
        stateMachineSystem.attackTimer += Time.deltaTime;

        // 1. 面向鎖定目標 (在可旋轉的狀態下)
        if (animator.CheckAnimationTag("Motion") || 
           (animator.CheckAnimationTag("Attack") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.4f) || 
            animator.CheckAnimationTag("Aim"))
        {
            movement.LockOnTarget(targetTransform);
        }

        // 2. 優先檢查技能施放
        if (!animator.IsInTransition(0) && animator.CheckAnimationTag("Motion"))
        {
            // 若目前尚未鎖定要放的技能，先從 CD 轉好的技能中選一個）
            if (stateMachineSystem.currentSkill == null)
            {
                stateMachineSystem.currentSkill = stateMachineSystem.SelectReadySkill(distance);
                stateMachineSystem.skillAdjustTimer = 0f;
            }

            // 若有選定技能，交由技能本身的邏輯決定：太遠追擊、太近後退、距離適中施放
            if (stateMachineSystem.currentSkill != null)
            {
                stateMachineSystem.UseSkill(stateMachineSystem.currentSkill);
                return;
            }

            // 3. 當所有技能都在 CD 中時，執行預設戰術走位（徘徊/保持距離）
            HandleTacticalMovement(stateMachineSystem, targetTransform, distance);
        }
        else
        {
            // 處於攻擊後搖或不能移動的狀態，停止 NavMesh 移動
            movement.StopMoving();
        }
    }

    private void HandleTacticalMovement(StateMachineSystem sm, Transform target, float distance)
    {
        EnemyMovementSystem movement = sm.movement;

       // A. 距離太近：拉開距離 (若背後貼牆無法後退，自動轉為側移脫困)
        if (distance < minDistance)
        {
            if (!movement.TryRetreatFromTarget(target, retreatSpeed))
            {
                PerformStrafe(sm, movement);
            }
        }
        // B. 戰鬥優勢距離：左右側移徘徊 (Strafe)
        else if (distance >= minDistance && distance <= maxDistance)
        {
            PerformStrafe(sm, movement);
        }
        // C. 距離太遠：快速追擊 (Chase)
        else
        {
            movement.ChaseTarget(target, chaseSpeed);
            sm.strafeTimer = 0f;
        }
    }
    private void PerformStrafe(StateMachineSystem sm, EnemyMovementSystem movement)
    {
        if (sm.strafeTimer >= strafeChangeInterval)
        {
            sm.UpdateRandomHorizontal();
            sm.strafeTimer = 0f;
        }

        // 若側邊遇到障礙物或邊界，立即反轉方向
        if (!movement.TryStrafe(sm.randomHorizontal, strafeSpeed))
        {
            sm.randomHorizontal *= -1;
            sm.strafeTimer = 0f;
            movement.TryStrafe(sm.randomHorizontal, strafeSpeed);
        }
    }

    public override void OnExit(StateMachineSystem stateMachineSystem)
    {
        stateMachineSystem.currentSkill = null;
        stateMachineSystem.skillAdjustTimer = 0f;
        stateMachineSystem.movement?.StopMoving();
    }
}