using UnityEngine;

/// <summary>
/// 第 5 步（戰鬥完美閃避教學）：
/// 進場鎖定攻擊，敵人出招預警後顯示卡片並進入子彈時間；
/// 按翻滾先解除慢動作以便無敵幀接招，唯有真正觸發「完美閃避」才算完成並解鎖攻擊。
/// </summary>
[CreateAssetMenu(fileName = "Step_PerfectDodge", menuName = "Dungeon/Tutorial/Steps/Perfect Dodge Step")]
public class PerfectDodgeStepSO : TutorialStepSO
{
    [Header("閃避子彈時間設定")]
    [SerializeField] private float dodgeTriggerDistance = 4.5f;
    [SerializeField] private float dodgeBulletTimeDelay = 0.1f;
    [SerializeField] private float dodgeBulletTimeScale = 0.08f;
    [SerializeField] private float maxBulletTimeRealDuration = 4.0f;

    private readonly TutorialBulletTimeHandler bulletTime = new TutorialBulletTimeHandler();

    protected override void OnEnter()
    {
        bulletTime.Reset();
        // 強制玩家在完成一次完美閃避前無法進行攻擊
        GameEvent.PlayerAttackLockChanged(true);

        GameEvent.OnEnemyAttackStarted -= HandleEnemyAttackStarted;
        GameEvent.OnEnemyAttackStarted += HandleEnemyAttackStarted;
        GameEvent.OnPlayerRolled -= HandlePlayerRolled;
        GameEvent.OnPlayerRolled += HandlePlayerRolled;
        GameEvent.OnPlayerPerfectDodge -= HandlePerfectDodgeSucceeded;
        GameEvent.OnPlayerPerfectDodge += HandlePerfectDodgeSucceeded;
    }

    public override void OnUpdate()
    {
        if (Manager == null || Manager.IsSuppressed)
        {
            bulletTime.Exit(restoreNormalTimeScale: false);
            bulletTime.ClearPendingAttacker();
            return;
        }

        bulletTime.Tick(
            Manager.PlayerTransform,
            dodgeTriggerDistance,
            dodgeBulletTimeDelay,
            maxBulletTimeRealDuration,
            HandleBulletTimeTriggerReady
        );
    }

    protected override void OnExit()
    {
        GameEvent.OnEnemyAttackStarted -= HandleEnemyAttackStarted;
        GameEvent.OnPlayerRolled -= HandlePlayerRolled;
        GameEvent.OnPlayerPerfectDodge -= HandlePerfectDodgeSucceeded;
        bulletTime.Reset();
        GameEvent.PlayerAttackLockChanged(false);
    }

    private void OnDisable()
    {
        GameEvent.OnEnemyAttackStarted -= HandleEnemyAttackStarted;
        GameEvent.OnPlayerRolled -= HandlePlayerRolled;
        GameEvent.OnPlayerPerfectDodge -= HandlePerfectDodgeSucceeded;

        if (Application.isPlaying)
        {
            bulletTime.Reset();
        }
    }

    private void HandleEnemyAttackStarted(Transform attacker)
    {
        if (bulletTime.IsActive || Manager == null || Manager.IsSuppressed)
        {
            return;
        }

        bulletTime.OnEnemyAttackStarted(
            attacker,
            Manager.PlayerTransform,
            dodgeTriggerDistance,
            dodgeBulletTimeDelay,
            HandleBulletTimeTriggerReady
        );
    }

    private void HandleBulletTimeTriggerReady()
    {
        if (bulletTime.IsActive || (Manager != null && Manager.IsSuppressed))
        {
            return;
        }

        ShowCard();
        bulletTime.Enter(dodgeBulletTimeScale);
    }

    private void HandlePlayerRolled()
    {
        if (!IsCardShown || (Manager != null && Manager.IsSuppressed))
        {
            return;
        }

        // 開始翻滾時恢復正常時間流速，讓無敵幀能順暢接住敵人攻擊；但是否達標仍待 OnPlayerPerfectDodge 判定
        bulletTime.Exit(restoreNormalTimeScale: true);
    }

    private void HandlePerfectDodgeSucceeded()
    {
        if (!IsCardShown || (Manager != null && Manager.IsSuppressed))
        {
            return;
        }

        // 保留 PlayerHealthSystem.DoHitstop 的頓幀效果，僅關閉子彈時間並還原 fixedDeltaTime
        bulletTime.Exit(restoreNormalTimeScale: false);
        AddProgress(1f);
    }
}
