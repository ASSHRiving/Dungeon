using System;
using UnityEngine;

public enum PlayerState
{
    Locomotion,   // 待機 / 走路 / 跑步
    Attacking,    // 攻擊中
    Rolling,      // 翻滾中
    HitStun,      // 受擊硬直中
    Falling,      // 墜落 / 著地中
    Interacting,  // 換武器 / 蹲下等互動中
    Dead          // 死亡（終態）
}

/// <summary>
/// 玩家單一動作狀態總管：
/// 統一控管角色「現在在哪個動作」，離開舊狀態時自動收尾，並將切狀態與 CrossFade 播動畫綁定為單一入口。
/// </summary>
[DefaultExecutionOrder(-80)]
public class PlayerStateController : MonoBehaviour
{
    private static readonly int MotionStateHash = Animator.StringToHash("Base Layer.Motion");
    private static readonly int RollStateHash = Animator.StringToHash("Base Layer.Roll");
    private static readonly int DieStateHash = Animator.StringToHash("Base Layer.Die");
    private static readonly int DefaultHitStateHash = Animator.StringToHash("Base Layer.Hit.Hit_F");
    private static readonly int RollTriggerHash = Animator.StringToHash("Roll");
    private static readonly int LAtkTriggerHash = Animator.StringToHash("LAtk");

    [Header("狀態切換設定")]
    [SerializeField, Range(0f, 1f)] private float rollRecoveryExitTransitionProgress = 0.5f;
    [SerializeField, Min(0.2f)] private float maxRollStateDuration = 1.5f;
    [SerializeField, Min(0.2f)] private float maxHitStunDuration = 1.5f;
    [SerializeField, Min(0.2f)] private float maxLandingRecoveryDuration = 0.65f;
    [SerializeField, Min(0.2f)] private float maxInteractingDuration = 1.8f;

    public PlayerState CurrentState { get; private set; } = PlayerState.Locomotion;
    public int StateStartFrame { get; private set; }
    public float StateElapsedTime { get; private set; }

    public event Action<PlayerState, PlayerState> OnStateChanged;

    private Animator animator;
    private CharacterInputSystem inputSystem;
    private PlayerMovementSystem movement;
    private PlayerCombatSystem combat;

    private int currentTargetStateHash;
    private float groundedWhileFallingTimer;

    public static PlayerStateController GetOrAdd(Component requester)
    {
        if (requester == null) return null;
        GameObject root = requester.transform.root != null ? requester.transform.root.gameObject : requester.gameObject;
        PlayerStateController controller = root.GetComponent<PlayerStateController>()
            ?? root.GetComponentInChildren<PlayerStateController>();

        if (controller == null)
        {
            controller = root.AddComponent<PlayerStateController>();
        }
        return controller;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (inputSystem == null) inputSystem = GetComponent<CharacterInputSystem>() ?? GetComponentInParent<CharacterInputSystem>();
        if (movement == null) movement = GetComponent<PlayerMovementSystem>() ?? GetComponentInParent<PlayerMovementSystem>();
        if (combat == null) combat = GetComponentInChildren<PlayerCombatSystem>();
    }

    private void Update()
    {
        if (CurrentState == PlayerState.Dead) return;

        StateElapsedTime += Time.deltaTime;
        ResolveReferences();
        if (animator == null) return;

        switch (CurrentState)
        {
            case PlayerState.Rolling:
                UpdateRollingExit();
                break;

            case PlayerState.HitStun:
                UpdateHitStunExit();
                break;

            case PlayerState.Falling:
                UpdateFallingExit();
                break;

            case PlayerState.Interacting:
                UpdateInteractingExit();
                break;
        }
    }

    public bool ChangeState(PlayerState newState, bool allowSelfTransition = false)
    {
        // 死亡後不可再切換回任何其他狀態
        if (CurrentState == PlayerState.Dead) return false;
        if (!allowSelfTransition && CurrentState == newState) return false;

        ResolveReferences();

        PlayerState oldState = CurrentState;
        OnExitState(oldState, newState);

        CurrentState = newState;
        StateStartFrame = Time.frameCount;
        StateElapsedTime = 0f;
        groundedWhileFallingTimer = 0f;
        currentTargetStateHash = 0;

        OnEnterState(newState);
        OnStateChanged?.Invoke(oldState, newState);
        return true;
    }

    private void OnExitState(PlayerState oldState, PlayerState nextState)
    {
        switch (oldState)
        {
            case PlayerState.Attacking:
                // 離開攻擊時強制收尾：關閉 Hitbox、關閉刀光；若不是接續連段，由外部或新狀態決定是否清連段
                combat?.OnExitAttackState(nextState);
                break;

            case PlayerState.Rolling:
                movement?.OnExitRollState();
                break;
        }
    }

    private void OnEnterState(PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.HitStun:
            case PlayerState.Interacting:
            case PlayerState.Dead:
                inputSystem?.ClearBuffer();
                break;
        }
    }

    #region 單一狀態進入點（切狀態＋播動畫綁定）

    public void EnterLocomotion(bool crossFadeToMotion = false, float fadeDuration = 0.1f)
    {
        if (!ChangeState(PlayerState.Locomotion)) return;

        if (crossFadeToMotion && animator != null)
        {
            PlayStateSmooth(MotionStateHash, fadeDuration, resetNormalizedTime: false);
        }
    }

    public bool EnterAttack(int stateFullPathHash, float crossFadeDuration = 0.08f)
    {
        if (animator == null || stateFullPathHash == 0) return false;
        if (!animator.HasState(0, stateFullPathHash))
        {
            Debug.LogWarning($"[PlayerStateController] Animator 找不到攻擊狀態 Hash: {stateFullPathHash}，已中止切入攻擊避免卡死。");
            return false;
        }
        if (!ChangeState(PlayerState.Attacking, allowSelfTransition: true)) return false;

        currentTargetStateHash = stateFullPathHash;
        animator.ResetTrigger(LAtkTriggerHash);
        PlayStateSmooth(stateFullPathHash, crossFadeDuration, resetNormalizedTime: true);
        return true;
    }

    public bool EnterRoll(float crossFadeDuration = 0.08f)
    {
        if (animator == null) return false;
        if (!ChangeState(PlayerState.Rolling)) return false;

        currentTargetStateHash = RollStateHash;
        movement?.OnEnterRollState();
        animator.ResetTrigger(RollTriggerHash);
        PlayStateSmooth(RollStateHash, crossFadeDuration, resetNormalizedTime: true);
        return true;
    }

    public bool EnterHitStun(string hitAnimationName, float crossFadeDuration = 0.1f)
    {
        if (animator == null) return false;
        if (!ChangeState(PlayerState.HitStun, allowSelfTransition: true)) return false;

        int hitHash = ResolveHitStateHash(hitAnimationName);
        currentTargetStateHash = hitHash;
        animator.ResetTrigger(RollTriggerHash);
        animator.ResetTrigger(LAtkTriggerHash);
        PlayStateSmooth(hitHash, crossFadeDuration, resetNormalizedTime: true);
        return true;
    }

    public void EnterFalling()
    {
        ChangeState(PlayerState.Falling);
    }

    public bool EnterInteracting(string animStatePath = "Base Layer.Crouch.Crouch", float crossFadeDuration = 0.08f)
    {
        if (!ChangeState(PlayerState.Interacting)) return false;

        if (animator != null && !string.IsNullOrEmpty(animStatePath))
        {
            int stateHash = Animator.StringToHash(animStatePath);
            currentTargetStateHash = stateHash;
            PlayStateSmooth(stateHash, crossFadeDuration, resetNormalizedTime: true);
        }
        return true;
    }

    /// <summary>
    /// Unity Animator 的 CrossFadeInFixedTime 在 IsInTransition(0) == true 時會被靜默忽略；
    /// 因此若當下剛好在過渡中（例如移動取消回 Motion 的 0.1s 融合內又瞬間出刀/翻滾），改用 Play 立即中斷過渡，確保動畫 100% 播放。
    /// </summary>
    private void PlayStateSmooth(int stateFullPathHash, float fadeDuration, bool resetNormalizedTime)
    {
        if (animator == null) return;

        if (animator.IsInTransition(0))
        {
            if (resetNormalizedTime)
                animator.Play(stateFullPathHash, 0, 0f);
            else
                animator.Play(stateFullPathHash, 0);
        }
        else
        {
            if (resetNormalizedTime)
                animator.CrossFadeInFixedTime(stateFullPathHash, fadeDuration, 0, 0f);
            else
                animator.CrossFadeInFixedTime(stateFullPathHash, fadeDuration, 0);
        }
    }

    public void EnterDead()
    {
        if (!ChangeState(PlayerState.Dead)) return;

        currentTargetStateHash = DieStateHash;
        if (animator != null)
        {
            animator.Play(DieStateHash, 0, 0f);
        }
    }

    #endregion

    #region 動畫進度查詢與自動退出檢查

    /// <summary>
    /// 取得目前目標狀態的 0~1 正規化播放進度。
    /// 在剛切入的同幀保證回傳 0，避免誤讀舊狀態的進度。
    /// </summary>
    public float GetCurrentTargetNormalizedTime()
    {
        return GetStateNormalizedTime(currentTargetStateHash);
    }

    public float GetStateNormalizedTime(int expectedFullPathHash)
    {
        if (animator == null || expectedFullPathHash == 0) return 0f;
        if (Time.frameCount <= StateStartFrame) return 0f;

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(0);
            if (nextInfo.fullPathHash == expectedFullPathHash)
                return nextInfo.normalizedTime;

            AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (currentInfo.fullPathHash == expectedFullPathHash && nextInfo.fullPathHash == MotionStateHash)
                return 1f;

            return 0f;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.fullPathHash == expectedFullPathHash)
            return stateInfo.normalizedTime;

        if (stateInfo.fullPathHash == MotionStateHash && StateElapsedTime > 0.1f)
            return 1f;

        return 0f;
    }

    /// <summary>
    /// 查詢受擊動畫目前的 0~1 進度（供受擊中段翻滾脫困使用）
    /// </summary>
    public float GetHitStunNormalizedTime()
    {
        if (CurrentState != PlayerState.HitStun || animator == null) return 0f;
        if (Time.frameCount <= StateStartFrame) return 0f;

        AnimatorStateInfo info = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);

        return info.IsTag("Hit") ? info.normalizedTime : 0f;
    }

    private void UpdateRollingExit()
    {
        if (Time.frameCount <= StateStartFrame) return;

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(0);
            if (nextInfo.fullPathHash == MotionStateHash &&
                animator.GetAnimatorTransitionInfo(0).normalizedTime >= rollRecoveryExitTransitionProgress)
            {
                EnterLocomotion();
                return;
            }
        }
        else
        {
            AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (currentInfo.fullPathHash == MotionStateHash && StateElapsedTime > 0.1f)
            {
                EnterLocomotion();
                return;
            }
        }

        if (StateElapsedTime >= maxRollStateDuration)
        {
            EnterLocomotion(crossFadeToMotion: true, fadeDuration: 0.1f);
        }
    }

    private void UpdateHitStunExit()
    {
        if (Time.frameCount <= StateStartFrame) return;

        if (!animator.IsInTransition(0))
        {
            AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (currentInfo.fullPathHash == MotionStateHash && StateElapsedTime > 0.08f)
            {
                EnterLocomotion();
                return;
            }
        }

        if (StateElapsedTime >= maxHitStunDuration)
        {
            EnterLocomotion(crossFadeToMotion: true, fadeDuration: 0.1f);
        }
    }

    private void UpdateFallingExit()
    {
        if (Time.frameCount <= StateStartFrame) return;

        if (movement != null && movement.IsGrounded)
        {
            groundedWhileFallingTimer += Time.deltaTime;

            if (!animator.IsInTransition(0))
            {
                AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (currentInfo.fullPathHash == MotionStateHash)
                {
                    EnterLocomotion();
                    return;
                }
            }

            if (groundedWhileFallingTimer >= maxLandingRecoveryDuration)
            {
                EnterLocomotion(crossFadeToMotion: true, fadeDuration: 0.1f);
            }
        }
        else
        {
            groundedWhileFallingTimer = 0f;
        }
    }

    private void UpdateInteractingExit()
    {
        if (Time.frameCount <= StateStartFrame) return;

        // 換武器協程約 0.5s，至少等待其完成後，偵測動畫回到 Motion 或逾時即恢復自由移動
        if (StateElapsedTime > 0.55f && !animator.IsInTransition(0))
        {
            AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (currentInfo.fullPathHash == MotionStateHash)
            {
                EnterLocomotion();
                return;
            }
        }

        if (StateElapsedTime >= maxInteractingDuration)
        {
            EnterLocomotion(crossFadeToMotion: true, fadeDuration: 0.1f);
        }
    }

    private int ResolveHitStateHash(string hitAnimationName)
    {
        if (string.IsNullOrEmpty(hitAnimationName))
            return DefaultHitStateHash;

        string subPath = hitAnimationName.StartsWith("Base Layer.", StringComparison.Ordinal)
            ? hitAnimationName
            : "Base Layer.Hit." + hitAnimationName;

        int subHash = Animator.StringToHash(subPath);
        if (animator != null && animator.HasState(0, subHash))
            return subHash;

        int rawHash = Animator.StringToHash(hitAnimationName);
        if (animator != null && animator.HasState(0, rawHash))
            return rawHash;

        return DefaultHitStateHash;
    }

    #endregion
}
