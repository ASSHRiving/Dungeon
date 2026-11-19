using System;
using UnityEngine;

/// <summary>
/// 負責戰鬥閃避教學的敵人出招預警延遲、子彈時間縮放與超時恢復。
/// </summary>
public class TutorialBulletTimeHandler
{
    private const float DefaultFixedDeltaTime = 0.02f;

    public bool IsActive { get; private set; }
    private float realTimer;
    private Transform pendingAttacker;
    private float pendingDelayTimer;

    public void Reset()
    {
        ClearPendingAttacker();
        Exit(restoreNormalTimeScale: true);
    }

    public void ClearPendingAttacker()
    {
        pendingAttacker = null;
        pendingDelayTimer = 0f;
    }

    public void OnEnemyAttackStarted(Transform attacker, Transform playerTransform, float triggerDistance, float delay, Action onTriggerReady)
    {
        if (IsActive || attacker == null || playerTransform == null) return;
        if (Vector3.Distance(attacker.position, playerTransform.position) > triggerDistance) return;
        if (pendingAttacker == attacker) return;

        if (delay <= 0f)
        {
            onTriggerReady?.Invoke();
            return;
        }

        pendingAttacker = attacker;
        pendingDelayTimer = 0f;
    }

    public void Tick(Transform playerTransform, float triggerDistance, float delay, float maxRealDuration, Action onTriggerReady)
    {
        if (pendingAttacker != null && !IsActive)
        {
            if (!pendingAttacker.gameObject.activeInHierarchy || playerTransform == null ||
                Vector3.Distance(pendingAttacker.position, playerTransform.position) > triggerDistance + 1.5f)
            {
                ClearPendingAttacker();
            }
            else
            {
                pendingDelayTimer += Time.deltaTime;
                if (pendingDelayTimer >= delay)
                {
                    ClearPendingAttacker();
                    onTriggerReady?.Invoke();
                }
            }
        }

        if (!IsActive) return;

        realTimer += Time.unscaledDeltaTime;
        if (realTimer >= maxRealDuration)
        {
            // 若逾時仍未按下閃避，恢復正常時間避免永久慢動作
            Exit(restoreNormalTimeScale: true);
        }
    }

    public void Enter(float bulletTimeScale)
    {
        IsActive = true;
        realTimer = 0f;

        float safeScale = Mathf.Clamp(bulletTimeScale, 0.03f, 0.5f);
        Time.timeScale = safeScale;
        Time.fixedDeltaTime = DefaultFixedDeltaTime * safeScale;
    }

    public void Exit(bool restoreNormalTimeScale)
    {
        if (!IsActive) return;
        IsActive = false;
        realTimer = 0f;

        // 無論是否交還 timeScale 控制權給 UIController，都必須還原 fixedDeltaTime
        Time.fixedDeltaTime = DefaultFixedDeltaTime;
        if (restoreNormalTimeScale)
        {
            Time.timeScale = 1f;
        }
    }
}
