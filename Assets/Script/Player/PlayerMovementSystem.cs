using UnityEngine;
using System.Collections;

public class PlayerMovementSystem : CharacterMovementBase
{
    private float targetRotation;
    private float rotationVelocity;
    private Transform characterCamera;
    private float rotationLerpTime = 0.1f;
    private float moveDirctionSlerpTime = 20;
    private Vector3 rollDirection;

    protected CharacterStats stats; // 中央數值大腦
    protected CharacterHealthBase health;
    private PlayerStateController stateController;
    private PlayerCombatSystem playerCombat;
    private Coroutine rollImmuneCoroutine;

    public const float BASE_RUN_SPEED = 7f;
    public const float BASE_WALK_SPEED = 3f;

    [SerializeField, Header("行走速度")] public float walkSpeed = BASE_WALK_SPEED;
    [SerializeField, Header("奔跑速度")] public float runSpeed = BASE_RUN_SPEED;

    [Header("翻滾緩衝與取消設定")]
    [SerializeField, Range(0f, 1f)] private float hitRollCancelNormalizedTime = 0.30f;
    [SerializeField, Min(0f)] private float rollCrossFadeDuration = 0.08f;

    protected override void Awake()
    {
        base.Awake();
        stats = GetComponent<CharacterStats>() ?? GetComponentInParent<CharacterStats>();
        health = GetComponent<CharacterHealthBase>() ?? GetComponentInParent<CharacterHealthBase>();
        stateController = PlayerStateController.GetOrAdd(this);
        playerCombat = combat as PlayerCombatSystem ?? GetComponentInChildren<PlayerCombatSystem>();
        if (Camera.main != null)
        {
            characterCamera = Camera.main.transform;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (stateController == null) stateController = PlayerStateController.GetOrAdd(this);

        CheckFallingState();
        HandleRollInput();
        PlayerMoveDirection();    
    }

    private void LateUpdate()
    {
        UpdateMotionAnimation();
        ApplyRollMotion();
        SyncAnimationSpeed();
    }

    private void OnDisable()
    {
        if (animator != null) animator.speed = 1f;
        rollImmuneCoroutine = null;
        immune = false;
    }

    private void CheckFallingState()
    {
        if (stateController == null || animator == null) return;
        if (stateController.CurrentState == PlayerState.Dead) return;

        // CharacterGravity 經過土狼時間後會將 IsGround 設為 false，此時統一進入 Falling 狀態自動收尾攻擊/翻滾
        if (!animator.GetBool(isGroundID) && stateController.CurrentState != PlayerState.Falling)
        {
            stateController.EnterFalling();
        }
    }

    private bool CanMoveControl()
    {
        return isOnGround && stateController != null && stateController.CurrentState == PlayerState.Locomotion;
    }

    private bool CanRunControl()
    {
        if (Vector3.Dot(movementDirection.normalized, transform.forward) < 0.75f) return false;
        if (!CanMoveControl()) return false;
       
        return true;
    }

    private void PlayerMoveDirection()
    {
        if (characterCamera == null)
        {
            if (Camera.main != null)
            {
                characterCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("找不到主相機，請確保場景中有一個標記為 MainCamera 的相機。");
                return;
            }
        }
        
        if (isOnGround && inputSystem.playerMovement == Vector2.zero)
            movementDirection = Vector3.zero;
        
        if (CanMoveControl()) 
        {
            if (inputSystem.playerMovement != Vector2.zero)
            {
                // 1. 抓取相機在世界座標的正前方與正右方
                Vector3 camForward = characterCamera.forward;
                Vector3 camRight = characterCamera.right;

                // 2. 重要：把 Y 軸（高度）歸零！我們只需要水平面的方向
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                // 3. 根據玩家的輸入直接計算出世界座標的移動方向
                Vector3 targetDirection = camForward * inputSystem.playerMovement.y + camRight * inputSystem.playerMovement.x;

                // 4. 計算出這個方向對應的 3D 角度（讓角色轉身用）
                targetRotation = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

                // 5. 讓角色平滑轉向目標角度
                transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationLerpTime);

                // 6. 這裡直接把我們算好的精準方向餵給 movementDirection
                movementDirection = Vector3.Slerp(movementDirection, targetDirection, moveDirctionSlerpTime * Time.deltaTime);
                rollDirection = movementDirection;
            }
        }
        else 
        {
            movementDirection = Vector3.zero;
        }

        if (stateController == null || stateController.CurrentState != PlayerState.Rolling)
        {  
            control.Move((characterCurrentMoveSpeed * Time.deltaTime)
                * movementDirection.normalized + Time.deltaTime
                * new Vector3(0.0f, verticalSpeed, 0.0f));
        }
    }

    private void UpdateMotionAnimation()
    {
        if (CanRunControl())
        {
            float targetSpeed = inputSystem.playerMovement.magnitude * (inputSystem.playerRun ? 2f : 1f);
            animator.SetFloat(speedID, targetSpeed, 0.1f, Time.deltaTime);

            // 基礎奔跑速度直接由 CharacterStats.MoveSpeed 驅動 (基準為 7.0f，武器 Flat 差值與裝備百分比加成已全數整合)
            float effectiveRunSpeed = (stats != null && stats.MoveSpeed > 0f) ? stats.MoveSpeed : BASE_RUN_SPEED;
            // 行走速度按 3.0 / 7.0 比例精確等比聯動
            float effectiveWalkSpeed = effectiveRunSpeed * (BASE_WALK_SPEED / BASE_RUN_SPEED);

            runSpeed = effectiveRunSpeed;
            walkSpeed = effectiveWalkSpeed;

            characterCurrentMoveSpeed = inputSystem.playerRun ? effectiveRunSpeed : effectiveWalkSpeed;
        }
        else
        {
            animator.SetFloat(speedID, 0f, 0.1f, Time.deltaTime);
            if (animator.GetFloat(speedID) < 0.001f)
            {
                animator.SetFloat(speedID, 0f);
            }
            characterCurrentMoveSpeed = 0f;
        }

        animator.SetFloat(runID, inputSystem.playerRun ? 1f : 0f);
    }

    /// <summary>
    /// 動態同步動畫播放速度：實際移速變快時，雙腿邁步頻率隨速度等比縮放，徹底消除滑步！
    /// </summary>
    private void SyncAnimationSpeed()
    {
        if (animator == null) return;

        // 當處於移動狀態且有實際位移時，等比縮放動畫播放速度
        if (stateController != null && stateController.CurrentState == PlayerState.Locomotion && characterCurrentMoveSpeed > 0.01f)
        {
            float referenceSpeed = inputSystem.playerRun ? BASE_RUN_SPEED : BASE_WALK_SPEED;
            animator.speed = characterCurrentMoveSpeed / referenceSpeed;
        }
        else if (stateController != null && stateController.CurrentState == PlayerState.HitStun)
        {
            float reduction = (stats != null) ? stats.HitStunReduction : 0f;
            // 縮短受擊硬直：例如 reduction = 0.3 (30%)，受擊動畫加速播放為 1 / (1 - 0.3) = 1.43 倍速，縮短硬直僵直時間
            animator.speed = 1f / Mathf.Max(0.1f, 1f - Mathf.Clamp(reduction, 0f, 0.9f));
        }
        else
        {
            animator.speed = 1f;
        }
    }

    private void HandleRollInput()
    {
        if (inputSystem == null || stateController == null) return;
        if (!inputSystem.HasBuffered(BufferedAction.Roll)) return;
        if (!CanRollNow()) return;
        if (!inputSystem.TryConsumeBuffer(BufferedAction.Roll)) return;

        // 透過單一入口進入 Rolling（自動幫舊狀態收尾、計算最新方向、播翻滾動畫與開無敵幀）
        stateController.EnterRoll(rollCrossFadeDuration);
    }

    private bool CanRollNow()
    {
        if (animator == null || !isOnGround) return false;
        if (health != null && health.isDead) return false;

        switch (stateController.CurrentState)
        {
            case PlayerState.Locomotion:
                return true;

            case PlayerState.Attacking:
                // 攻擊前搖與揮砍判定中不可打斷；超過該招的 rollCancelTime 後允許翻滾取消
                return playerCombat != null && playerCombat.CanRollCancelCurrentAttack();

            case PlayerState.HitStun:
                // 受擊硬直避開前段，進度超過門檻後允許翻滾脫困
                return stateController.GetHitStunNormalizedTime() >= hitRollCancelNormalizedTime;

            default:
                return false;
        }
    }

    /// <summary>
    /// 由 PlayerStateController.EnterRoll 統一呼叫：設定當下翻滾方向、發送事件與開啟無敵幀
    /// </summary>
    public void OnEnterRollState()
    {
        UpdateRollDirection();
        GameEvent.PlayerRolled();
        StartRollInvulnerability();
    }

    /// <summary>
    /// 由 PlayerStateController.OnExitState 統一呼叫：離開翻滾狀態時的清理
    /// </summary>
    public void OnExitRollState()
    {
        if (animator != null)
        {
            animator.ResetTrigger(rollId);
        }
    }

    private void UpdateRollDirection()
    {
        if (characterCamera == null && Camera.main != null)
        {
            characterCamera = Camera.main.transform;
        }

        if (inputSystem.playerMovement != Vector2.zero && characterCamera != null)
        {
            Vector3 camForward = characterCamera.forward;
            Vector3 camRight = characterCamera.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // 根據玩家當前的輸入決定翻滾方向
            rollDirection = (camForward * inputSystem.playerMovement.y + camRight * inputSystem.playerMovement.x).normalized;
            targetRotation = Mathf.Atan2(rollDirection.x, rollDirection.z) * Mathf.Rad2Deg;
        }
        else
        {
            // 沒推方向時，朝角色當前正前方翻滾
            rollDirection = transform.forward;
            targetRotation = transform.eulerAngles.y;
        }
    }

    private void ApplyRollMotion()
    {
        // 翻滾動畫播放期間
        if (stateController != null && stateController.CurrentState == PlayerState.Rolling)
        {
            if (animator != null) animator.ResetTrigger(rollId);

            // 強制轉向
            transform.eulerAngles = targetRotation * Vector3.up;

            // 執行翻滾位移
            CharacterMoveInterface(rollDirection, animator.GetFloat(animationMoveID), true);
        }
    }

    /// <summary>
    /// 保留向下相容：舊 RollEnter StateMachineBehaviour 仍會呼叫此方法。
    /// 當由 PlayerStateController 統一驅動時，避免重複觸發無敵幀與事件。
    /// </summary>
    public void OnRollEnter()
    {
        if (stateController != null) return;

        GameEvent.PlayerRolled();
        StartRollInvulnerability();
    }

    private void StartRollInvulnerability()
    {
        if (rollImmuneCoroutine != null)
        {
            StopCoroutine(rollImmuneCoroutine);
        }
        rollImmuneCoroutine = StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {
        immune = true;
        yield return new WaitForSeconds(immuneTime);
        immune = false;
        rollImmuneCoroutine = null;
    }
}
