using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCombatSystem : CharacterCombatBase
{
    private struct TargetCandidate : IComparable<TargetCandidate>
    {
        public Transform target;
        public CharacterHealthBase health;
        public Vector3 aimPoint;
        public float surfaceDist;
        public int tier; // 0: 近戰圈活體敵人, 1: 遠距視野活體敵人, 2: 近戰圈可破壞物, 3: 遠距可破壞物
        public float score;

        public int CompareTo(TargetCandidate other)
        {
            int tierCmp = tier.CompareTo(other.tier);
            return tierCmp != 0 ? tierCmp : other.score.CompareTo(score);
        }
    }

    private const float MinFarForwardDot = -0.1f;
    private const float MeleeSkipLosDistance = 1.2f;
    private const float RaycastEndMargin = 0.35f;
    private const float StickyTargetBonus = 0.12f;
    private const float DefaultLockOnWindow = 0.25f;
    private const float TurnSpeedToDegreesPerSec = 45f;
    private const float PlayerEyeHeight = 1.1f;
    private const float MinAimHeightOffset = 0.85f;

    [SerializeField] private Transform weaponHolder;

    [Header("索敵設定")]
    [SerializeField] private Transform enemyDetectionCenter;
    [SerializeField] private float enemyDetectionRadius = 7f;
    [SerializeField] private float attackLockRange = 2.5f;
    [SerializeField] private float attackTurnSpeed = 25f;
    [SerializeField] private LayerMask whatIsObs;

    [Header("鏡頭參考")]
    [SerializeField] private Transform mainCameraTransform;

    private readonly Collider[] detectedEnemies = new Collider[32];
    private readonly List<TargetCandidate> candidateBuffer = new List<TargetCandidate>(32);
    private readonly HashSet<Transform> visitedEntities = new HashSet<Transform>();
    private CharacterHealthBase currentTargetHealth;
    private PlayerStateController stateController;
    private bool isAttackLockedByTutorial;

    [Header("連擊系統設定")]
    [SerializeField, Min(0.3f)] private float maxAttackFallbackDuration = 2.5f;

    private ComboStepData currentAttackStep;
    private int currentComboIndex;
    private float comboExpireTime;

    // 內建保底連段表（當武器資產未指定 ComboSetSO 時依 weaponType 自動套用）
    private static readonly ComboStepData[] FallbackSwordSteps =
    {
        new ComboStepData { animatorStatePath = "Sword.Attack1", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.30f, comboCancelTime = 0.31f, moveCancelTime = 0.34f, endNormalizedTime = 0.88f, comboHoldDuration = 0.65f },
        new ComboStepData { animatorStatePath = "Sword.Attack2", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.42f, comboCancelTime = 0.43f, moveCancelTime = 0.46f, endNormalizedTime = 0.90f, comboHoldDuration = 0.65f },
        new ComboStepData { animatorStatePath = "Sword.Attack3", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.42f, comboCancelTime = 0.43f, moveCancelTime = 0.46f, endNormalizedTime = 0.87f, comboHoldDuration = 0.65f },
        new ComboStepData { animatorStatePath = "Sword.Attack4", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.44f, comboCancelTime = 0.45f, moveCancelTime = 0.48f, endNormalizedTime = 0.91f, comboHoldDuration = 0.65f },
        new ComboStepData { animatorStatePath = "Sword.Attack5", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.51f, comboCancelTime = 0.52f, moveCancelTime = 0.55f, endNormalizedTime = 0.87f, comboHoldDuration = 0.65f },
    };

    private static readonly ComboStepData[] FallbackKatanaSteps =
    {
        new ComboStepData { animatorStatePath = "Katana.Attack1", crossFadeDuration = 0.06f, moveCancelCrossFadeDuration = 0.20f, rollCancelTime = 0.21f, comboCancelTime = 0.22f, moveCancelTime = 0.26f, endNormalizedTime = 0.88f, comboHoldDuration = 0.60f },
        new ComboStepData { animatorStatePath = "Katana.Attack2", crossFadeDuration = 0.06f, moveCancelCrossFadeDuration = 0.20f, rollCancelTime = 0.24f, comboCancelTime = 0.25f, moveCancelTime = 0.29f, endNormalizedTime = 0.87f, comboHoldDuration = 0.60f },
        new ComboStepData { animatorStatePath = "Katana.Attack3", crossFadeDuration = 0.06f, moveCancelCrossFadeDuration = 0.20f, rollCancelTime = 0.25f, comboCancelTime = 0.26f, moveCancelTime = 0.30f, endNormalizedTime = 0.90f, comboHoldDuration = 0.60f },
        new ComboStepData { animatorStatePath = "Katana.Attack4", crossFadeDuration = 0.06f, moveCancelCrossFadeDuration = 0.20f, rollCancelTime = 0.25f, comboCancelTime = 0.26f, moveCancelTime = 0.30f, endNormalizedTime = 0.90f, comboHoldDuration = 0.60f },
    };

    private static readonly ComboStepData[] FallbackGreatSwordSteps =
    {
        new ComboStepData { animatorStatePath = "Hammer.GreatSword_Attack01", crossFadeDuration = 0.10f, moveCancelCrossFadeDuration = 0.28f, rollCancelTime = 0.42f, comboCancelTime = 0.43f, moveCancelTime = 0.48f, endNormalizedTime = 0.89f, comboHoldDuration = 0.75f },
        new ComboStepData { animatorStatePath = "Hammer.GreatSword_Attack02", crossFadeDuration = 0.10f, moveCancelCrossFadeDuration = 0.28f, rollCancelTime = 0.37f, comboCancelTime = 0.38f, moveCancelTime = 0.43f, endNormalizedTime = 0.89f, comboHoldDuration = 0.75f },
        new ComboStepData { animatorStatePath = "Hammer.GreatSword_Attack03", crossFadeDuration = 0.10f, moveCancelCrossFadeDuration = 0.28f, rollCancelTime = 0.56f, comboCancelTime = 0.57f, moveCancelTime = 0.62f, endNormalizedTime = 0.90f, comboHoldDuration = 0.75f },
    };

    protected override void Awake()
    {
        base.Awake();
        stateController = PlayerStateController.GetOrAdd(this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvent.OnPlayerAttackLockChanged += HandleTutorialAttackLockChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvent.OnPlayerAttackLockChanged -= HandleTutorialAttackLockChanged;
        isAttackLockedByTutorial = false;
    }

    private void HandleTutorialAttackLockChanged(bool locked)
    {
        isAttackLockedByTutorial = locked;
        if (locked && inputSystem != null)
        {
            inputSystem.ClearBuffer(BufferedAction.Attack);
        }
    }

    override protected void Update()
    {
        base.Update();
        if (stateController == null) stateController = PlayerStateController.GetOrAdd(this);

        ValidateCurrentTarget();

        if (stateController.CurrentState == PlayerState.Locomotion)
        {
            TryStartAttackFromLocomotion();
        }
        else if (stateController.CurrentState == PlayerState.Attacking)
        {
            UpdateAttackingState();
        }
    }

    /// <summary>
    /// 供 PlayerMovementSystem 查詢：目前攻擊是否已過前搖與揮砍判定，允許翻滾取消
    /// </summary>
    public bool CanRollCancelCurrentAttack()
    {
        if (stateController == null || stateController.CurrentState != PlayerState.Attacking)
            return false;
        if (currentAttackStep == null)
            return true;

        return stateController.GetCurrentTargetNormalizedTime() >= currentAttackStep.rollCancelTime;
    }

    /// <summary>
    /// 由 PlayerStateController.OnExitState 自動呼叫：無論因任何原因離開 Attacking，保證統一收尾
    /// </summary>
    public void OnExitAttackState(PlayerState nextState)
    {
        OnAttackHitboxEnd();
        currentAttackStep = null;

        // 僅在自然結束/移動取消回 Locomotion 或接續連段 Attacking 時保留連段計數；其他打斷（翻滾/受擊/墜落/死亡/換武）一律歸零
        if (nextState != PlayerState.Attacking && nextState != PlayerState.Locomotion)
        {
            ResetCombo();
        }
    }

    public void ResetCombo()
    {
        currentComboIndex = 0;
        comboExpireTime = 0f;
        if (currentWeapon != null)
        {
            currentWeapon.combo = 0;
        }
    }

    private void TryStartAttackFromLocomotion()
    {
        if (isAttackLockedByTutorial || currentWeapon == null || inputSystem == null) return;
        if (health != null && health.isDead) return;
        if (movement != null && !movement.IsGrounded) return;
        if (!inputSystem.HasBuffered(BufferedAction.Attack)) return;

        // 超過連段保留時間則回到第 1 段
        if (Time.time > comboExpireTime)
        {
            currentComboIndex = 0;
        }

        if (!inputSystem.TryConsumeBuffer(BufferedAction.Attack)) return;

        ExecuteComboStep(currentComboIndex);
    }

    private void UpdateAttackingState()
    {
        ActionMotion();
        AttackLockOnTarget();

        if (currentAttackStep == null)
        {
            stateController.EnterLocomotion();
            return;
        }

        float progress = stateController.GetCurrentTargetNormalizedTime();

        // 1. 優先禮讓翻滾緩衝（由 PlayerMovementSystem 處理進入 Rolling）
        if (inputSystem != null && inputSystem.HasBuffered(BufferedAction.Roll))
        {
            if (progress >= currentAttackStep.rollCancelTime)
                return;
        }

        // 2. 連段取消視窗：進入可接段進度且玩家有 Attack 緩衝 → 直接切下一段
        if (!isAttackLockedByTutorial &&
            progress >= currentAttackStep.comboCancelTime &&
            inputSystem != null &&
            inputSystem.TryConsumeBuffer(BufferedAction.Attack))
        {
            int nextIndex = GetNextComboIndex(currentComboIndex);
            ExecuteComboStep(nextIndex);
            return;
        }

        // 3. 移動取消後搖視窗：推方向鍵提早切回 Locomotion，同時在 comboHoldDuration 內保留連段段數
        if (progress >= currentAttackStep.moveCancelTime &&
            inputSystem != null &&
            inputSystem.playerMovement != Vector2.zero)
        {
            float moveFade = currentAttackStep.moveCancelCrossFadeDuration;
            PrepareComboHoldAfterExit();
            stateController.EnterLocomotion(crossFadeToMotion: true, fadeDuration: moveFade);
            return;
        }

        // 4. 自然播完（或防卡死安全逾時）回到 Locomotion
        if (progress >= currentAttackStep.endNormalizedTime || stateController.StateElapsedTime >= maxAttackFallbackDuration)
        {
            PrepareComboHoldAfterExit();
            stateController.EnterLocomotion();
        }
    }

    private void PrepareComboHoldAfterExit()
    {
        float holdDuration = currentAttackStep != null ? currentAttackStep.comboHoldDuration : 0.6f;
        currentComboIndex = GetNextComboIndex(currentComboIndex);
        comboExpireTime = Time.time + holdDuration;
    }

    private void ExecuteComboStep(int stepIndex)
    {
        ComboStepData step = ResolveComboStep(stepIndex, out int resolvedIndex);
        if (step == null) return;

        int stateHash = step.GetFullPathHash();
        if (stateHash == 0) return;

        AcquireAttackTarget();

        if (!stateController.EnterAttack(stateHash, step.crossFadeDuration))
            return;

        currentComboIndex = resolvedIndex;
        currentAttackStep = step;
        if (currentWeapon != null)
        {
            currentWeapon.combo = resolvedIndex + 1;
        }

        // 切入攻擊當幀立即啟動轉向，避免第 0 幀漏轉
        AttackLockOnTarget();

        GameEvent.PlayerAttacked();
    }

    private int GetNextComboIndex(int index)
    {
        int count = GetActiveComboStepCount();
        if (count <= 0) return 0;
        return (index + 1) % count;
    }

    private int GetActiveComboStepCount()
    {
        if (currentWeapon != null && currentWeapon.WeaponData != null &&
            currentWeapon.WeaponData.comboSet != null && currentWeapon.WeaponData.comboSet.StepCount > 0)
        {
            return currentWeapon.WeaponData.comboSet.StepCount;
        }

        return GetFallbackSteps().Length;
    }

    private ComboStepData ResolveComboStep(int index, out int resolvedIndex)
    {
        if (currentWeapon != null && currentWeapon.WeaponData != null &&
            currentWeapon.WeaponData.comboSet != null && currentWeapon.WeaponData.comboSet.StepCount > 0)
        {
            int count = currentWeapon.WeaponData.comboSet.StepCount;
            resolvedIndex = ((index % count) + count) % count;
            return currentWeapon.WeaponData.comboSet.GetStep(resolvedIndex);
        }

        ComboStepData[] fallback = GetFallbackSteps();
        resolvedIndex = ((index % fallback.Length) + fallback.Length) % fallback.Length;
        return fallback[resolvedIndex];
    }

    private ComboStepData[] GetFallbackSteps()
    {
        int weaponType = currentWeapon != null ? currentWeapon.weaponType : 0;
        switch (weaponType)
        {
            case 1: return FallbackKatanaSteps;
            case 2: return FallbackGreatSwordSteps;
            default: return FallbackSwordSteps;
        }
    }

    private void ActionMotion()         //攻擊時AnimationMove參數
    {
        if (stateController != null && stateController.CurrentState == PlayerState.Attacking)
        {
            movement.CharacterMoveInterface(transform.forward, animator.GetFloat(animationMoveID) * 5f, true);
        }
    }

    private void AttackLockOnTarget()   //攻擊鎖敵
    {
        if (!CanAttackLockOn() || currentTarget == null) return;

        Transform root = transform.root;
        Vector3 toTarget = currentTarget.position - root.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized);
        float maxDegrees = Mathf.Max(0f, attackTurnSpeed) * TurnSpeedToDegreesPerSec * Time.deltaTime;
        root.rotation = Quaternion.RotateTowards(root.rotation, targetRotation, maxDegrees);
    }

    private bool CanAttackLockOn()
    {
        if (stateController == null || stateController.CurrentState != PlayerState.Attacking)
            return false;

        float lockWindow = currentAttackStep != null
            ? Mathf.Clamp(currentAttackStep.rollCancelTime * 0.75f, 0.20f, 0.30f)
            : DefaultLockOnWindow;
        return stateController.GetCurrentTargetNormalizedTime() < lockWindow;
    }

    private void AcquireAttackTarget()
    {
        if (mainCameraTransform == null && Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        if (mainCameraTransform == null)
        {
            currentTarget = null;
            currentTargetHealth = null;
            return;
        }

        Vector3 playerPos = transform.root.position;
        Vector3 centerPos = enemyDetectionCenter != null ? enemyDetectionCenter.position : playerPos;
        int count = Physics.OverlapSphereNonAlloc(centerPos, enemyDetectionRadius, detectedEnemies, whatIsEnemy);
        if (count <= 0)
        {
            currentTarget = null;
            currentTargetHealth = null;
            return;
        }

        Vector3 intentDir = GetIntentDirection(out bool hasMoveInput);
        Vector3 rayOrigin = playerPos + Vector3.up * PlayerEyeHeight;
        float meleeRange = Mathf.Max(0.5f, attackLockRange);
        float invMeleeRange = 1f / meleeRange;
        float invDetectRadius = enemyDetectionRadius > 0.001f ? 1f / enemyDetectionRadius : 0f;

        candidateBuffer.Clear();
        visitedEntities.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider col = detectedEnemies[i];
            if (col == null || !TryResolveLivingEnemy(col, out Transform entity, out CharacterHealthBase enemyHealth))
                continue;
            if (!visitedEntities.Add(entity))
                continue;

            // 使用 Bounds.ClosestPoint 計算碰撞體表面水平距離，同時相容 CharacterController 與大型 Boss
            Vector3 closestPoint = col.bounds.extents != Vector3.zero
                ? col.bounds.ClosestPoint(rayOrigin)
                : entity.position;
            Vector3 flatToSurface = closestPoint - playerPos;
            flatToSurface.y = 0f;
            float surfaceDist = flatToSurface.magnitude;

            Vector3 flatToEntity = entity.position - playerPos;
            flatToEntity.y = 0f;
            float centerDist = flatToEntity.magnitude;
            float dot = centerDist > 0.001f ? Vector3.Dot(intentDir, flatToEntity / centerDist) : 1f;

            bool inMeleeRing = surfaceDist <= meleeRange;
            // 近戰命中圈內採 360° 保底鎖定；圈外遠距目標只鎖定意圖方向前方半球
            if (!inMeleeRing && dot < MinFarForwardDot)
                continue;

            bool isLivingEnemy = enemyHealth != null;
            int tier = (isLivingEnemy ? 0 : 2) + (inMeleeRing ? 0 : 1);

            float dirScore = (dot + 1f) * 0.5f;
            float distScore = inMeleeRing
                ? 1f - Mathf.Clamp01(surfaceDist * invMeleeRange)
                : 1f - Mathf.Clamp01(surfaceDist * invDetectRadius);

            float dirWeight = inMeleeRing ? (hasMoveInput ? 0.70f : 0.45f) : 0.55f;
            float distWeight = 1f - dirWeight;
            float stickyBonus = entity == currentTarget ? StickyTargetBonus : 0f;

            Vector3 boundsCenter = col.bounds.extents != Vector3.zero ? col.bounds.center : entity.position;
            float aimY = Mathf.Max(boundsCenter.y, entity.position.y + MinAimHeightOffset);

            candidateBuffer.Add(new TargetCandidate
            {
                target = entity,
                health = enemyHealth,
                aimPoint = new Vector3(boundsCenter.x, aimY, boundsCenter.z),
                surfaceDist = surfaceDist,
                tier = tier,
                score = dirScore * dirWeight + distScore * distWeight + stickyBonus
            });
        }

        System.Array.Clear(detectedEnemies, 0, count);
        candidateBuffer.Sort();

        Transform bestTarget = null;
        CharacterHealthBase bestHealth = null;
        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            TargetCandidate candidate = candidateBuffer[i];
            if (candidate.surfaceDist <= MeleeSkipLosDistance)
            {
                bestTarget = candidate.target;
                bestHealth = candidate.health;
                break;
            }

            Vector3 toAim = candidate.aimPoint - rayOrigin;
            float dist = toAim.magnitude;
            float checkDist = dist - RaycastEndMargin;
            if (checkDist <= 0.05f || !Physics.Raycast(rayOrigin, toAim / dist, checkDist, whatIsObs, QueryTriggerInteraction.Ignore))
            {
                bestTarget = candidate.target;
                bestHealth = candidate.health;
                break;
            }
        }

        candidateBuffer.Clear();
        visitedEntities.Clear();
        currentTarget = bestTarget;
        currentTargetHealth = bestHealth;
    }

    private Vector3 GetIntentDirection(out bool hasMoveInput)
    {
        Vector3 camForward = mainCameraTransform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.0001f)
        {
            camForward = transform.root.forward;
            camForward.y = 0f;
        }
        camForward.Normalize();

        Vector2 moveInput = inputSystem != null ? inputSystem.playerMovement : Vector2.zero;
        hasMoveInput = moveInput.sqrMagnitude > 0.01f;
        if (hasMoveInput)
        {
            Vector3 camRight = mainCameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;
            if (inputDir.sqrMagnitude > 0.0001f)
                return inputDir.normalized;
        }

        return camForward;
    }

    private bool TryResolveLivingEnemy(Collider col, out Transform entityTransform, out CharacterHealthBase enemyHealth)
    {
        enemyHealth = col.GetComponentInParent<CharacterHealthBase>();
        if (enemyHealth != null)
        {
            entityTransform = enemyHealth.transform;
            return entityTransform != transform.root && !enemyHealth.isDead;
        }

        IDamageable damageable = col.GetComponentInParent<IDamageable>();
        entityTransform = damageable is Component comp ? comp.transform : col.transform;
        return entityTransform != transform.root && entityTransform != transform;
    }

    private void ValidateCurrentTarget()
    {
        if (currentTarget == null) return;

        bool isDeadOrInactive = !currentTarget.gameObject.activeInHierarchy
            || (currentTargetHealth != null && currentTargetHealth.isDead);
        if (isDeadOrInactive)
        {
            currentTarget = null;
            currentTargetHealth = null;
            return;
        }

        if (stateController == null || stateController.CurrentState != PlayerState.Attacking)
        {
            Vector3 centerPos = enemyDetectionCenter != null ? enemyDetectionCenter.position : transform.position;
            if ((currentTarget.position - centerPos).sqrMagnitude > enemyDetectionRadius * enemyDetectionRadius)
            {
                currentTarget = null;
                currentTargetHealth = null;
            }
        }
    }

    protected override void InitWeapon()
    {
        base.InitWeapon();
        ResetCombo();
        if (currentWeapon == null) return;

        if (currentWeapon.overrideController != null)
        {
            animator.runtimeAnimatorController = currentWeapon.overrideController;
        }
        animator.SetInteger("WeaponType", currentWeapon.weaponType);
        DropsInteract dropsInteract = currentWeapon.GetComponent<DropsInteract>();
        if (dropsInteract != null) dropsInteract.enabled = false;

        // 武器移速差值已由 base.InitWeapon() -> UpdateWeaponStatModifier() 透過 CharacterStats 統一管理
    }
    public void ChangeWeapon(GameObject newWeapon)
    {
        if (stateController == null) stateController = PlayerStateController.GetOrAdd(this);
        stateController.EnterInteracting("Base Layer.Crouch.Crouch", 0.08f);
        StartCoroutine(WaitAndChangeWeapon(newWeapon));
        
    }
    private IEnumerator WaitAndChangeWeapon(GameObject newWeapon)
    {
        yield return new WaitForSeconds(0.5f);
        //丟掉舊武器
        if(currentWeapon != null)
        {
            ClearWeaponModifier();
            currentWeapon.GetComponent<Rigidbody>().isKinematic = false;
            currentWeapon.GetComponent<Collider>().enabled = true;
            currentWeapon.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(currentWeapon.gameObject, SceneManager.GetActiveScene());   //移除Dont Destory

        }
        //撿起新武器
        newWeapon.transform.SetParent(weaponHolder);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;

        currentWeapon = newWeapon.GetComponent<Weapon>();
        InitWeapon();
    }
}
