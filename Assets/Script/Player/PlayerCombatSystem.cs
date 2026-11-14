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
        public float score;

        public int CompareTo(TargetCandidate other) => other.score.CompareTo(score);
    }

    private const float MinForwardDot = -0.25f;
    private const float DirectionWeight = 0.65f;
    private const float DistanceWeight = 0.35f;
    private const float CloseRangeBonus = 0.25f;
    private const float StickyTargetBonus = 0.15f;
    private const float LockOnNormalizedWindow = 0.15f;
    private const float PlayerEyeHeight = 1.1f;

    [SerializeField] private Transform weaponHolder;

    [Header("索敵設定")]
    [SerializeField] private Transform enemyDetectionCenter;
    [SerializeField] private float enemyDetectionRadius = 8f;
    [SerializeField] private float attackLockRange = 2.5f;
    [SerializeField] private float attackTurnSpeed = 25f;
    [SerializeField] private LayerMask whatIsObs;

    [Header("鏡頭參考")]
    [SerializeField] private Transform mainCameraTransform;

    private readonly Collider[] detectedEnemies = new Collider[32];
    private readonly List<TargetCandidate> candidateBuffer = new List<TargetCandidate>(32);
    private readonly HashSet<Transform> visitedEntities = new HashSet<Transform>();
    private CharacterHealthBase currentTargetHealth;

    override protected void Update()
    {
        base.Update();
        ValidateCurrentTarget();
        PlayerAttackAction();
        ActionMotion();
        AttackLockOnTarget();
        CancelAttackMove();
    }

    private void PlayerAttackAction()   //執行攻擊
    {
        if (inputSystem.playerLAtk && canAttack && !animator.CheckAnimationTag("Hit"))
        {
            if (currentWeapon == null) return;

            AcquireAttackTarget();
            canAttack = false;
            currentWeapon.Attack(animator);
            inAttack = true;
        }
    }

    private void ActionMotion()         //攻擊時AnimationMove參數
    {
        if (animator.CheckAnimationTag("Attack"))
        {
            movement.CharacterMoveInterface(transform.forward, animator.GetFloat(animationMoveID) * 5f, true);
        }
    }

    private void CancelAttackMove()     //取消後搖
    {
        if (inAttack && canAttack && inputSystem.playerMovement != Vector2.zero)
        {
            inAttack = false;
            animator.CrossFade("Motion", 0.1f);
        }
    }

    private void AttackLockOnTarget()   //攻擊鎖敵
    {
        if (CanAttackLockOn() && currentTarget != null)
        {
            transform.root.rotation = transform.LockOnTarget(currentTarget, transform.root, attackTurnSpeed);
        }
    }

    private bool CanAttackLockOn()
    {
        if (animator == null) return false;

        AnimatorStateInfo stateInfo = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsTag("Attack") && stateInfo.normalizedTime < LockOnNormalizedWindow;
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

        Vector3 centerPos = enemyDetectionCenter != null ? enemyDetectionCenter.position : transform.position;
        int count = Physics.OverlapSphereNonAlloc(centerPos, enemyDetectionRadius, detectedEnemies, whatIsEnemy);
        if (count <= 0)
        {
            currentTarget = null;
            currentTargetHealth = null;
            return;
        }

        Vector3 intentDir = GetIntentDirection();
        Vector3 rayOrigin = transform.position + Vector3.up * PlayerEyeHeight;
        float invRadius = enemyDetectionRadius > 0.001f ? 1f / enemyDetectionRadius : 0f;

        candidateBuffer.Clear();
        visitedEntities.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider col = detectedEnemies[i];
            if (col == null || !TryResolveLivingEnemy(col, out Transform entity, out CharacterHealthBase enemyHealth))
                continue;
            if (!visitedEntities.Add(entity))
                continue;

            Vector3 flatToEnemy = entity.position - transform.position;
            flatToEnemy.y = 0f;
            float flatDist = flatToEnemy.magnitude;
            float dot = flatDist > 0.001f ? Vector3.Dot(intentDir, flatToEnemy / flatDist) : 1f;
            if (dot < MinForwardDot && flatDist > attackLockRange)
                continue;

            float dirScore = (dot + 1f) * 0.5f;
            float distScore = 1f - Mathf.Clamp01(flatDist * invRadius);
            float bonus = (flatDist <= attackLockRange ? CloseRangeBonus : 0f)
                        + (entity == currentTarget ? StickyTargetBonus : 0f);

            candidateBuffer.Add(new TargetCandidate
            {
                target = entity,
                health = enemyHealth,
                aimPoint = col.bounds.extents != Vector3.zero ? col.bounds.center : entity.position + Vector3.up * PlayerEyeHeight,
                score = dirScore * DirectionWeight + distScore * DistanceWeight + bonus
            });
        }

        System.Array.Clear(detectedEnemies, 0, count);
        candidateBuffer.Sort();

        Transform bestTarget = null;
        CharacterHealthBase bestHealth = null;
        for (int i = 0; i < candidateBuffer.Count; i++)
        {
            Vector3 toAim = candidateBuffer[i].aimPoint - rayOrigin;
            float dist = toAim.magnitude;
            if (dist < 0.001f || !Physics.Raycast(rayOrigin, toAim / dist, dist, whatIsObs, QueryTriggerInteraction.Ignore))
            {
                bestTarget = candidateBuffer[i].target;
                bestHealth = candidateBuffer[i].health;
                break;
            }
        }

        candidateBuffer.Clear();
        visitedEntities.Clear();
        currentTarget = bestTarget;
        currentTargetHealth = bestHealth;
    }

    private Vector3 GetIntentDirection()
    {
        Vector3 camForward = mainCameraTransform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.0001f)
        {
            camForward = transform.forward;
            camForward.y = 0f;
        }
        camForward.Normalize();

        Vector2 moveInput = inputSystem != null ? inputSystem.playerMovement : Vector2.zero;
        if (moveInput.sqrMagnitude > 0.01f)
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

        if (!inAttack && animator != null && !animator.CheckAnimationTag("Attack"))
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
        animator.Play("Crouch", 0, 0f);
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
