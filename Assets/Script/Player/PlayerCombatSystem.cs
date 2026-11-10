using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerCombatSystem : CharacterCombatBase
{
    
    [SerializeField] private Transform weaponHolder;

    [Header("索敵設定")]
    [SerializeField] private Transform enemyDetectionCenter;
    [SerializeField] private float enemyDetectionRadius;
    [SerializeField] private float attackLockRange = 1.5f;
    [SerializeField] private LayerMask lockLayer;
    [SerializeField] private LayerMask whatIsObs;
    private Collider[] detectedEnemies = new Collider[10];

    [Header("鏡頭參考")]
    [SerializeField] private Transform mainCameraTransform;

    private int weaponType;


    override protected void Awake()
    {
        base.Awake();
        InitWeapon();
    }

    override protected void Update()
    {
        base.Update();
        PlayerAttackAction();
        ActionMotion();
        DetectEnemy();
        updateTarget();
        AttackLockOnTarget();
        CancelAttackMove();
        //ResetInAttack();
    }
    private void PlayerAttackAction()   //執行攻擊
    {
        if (inputSystem.playerLAtk && canAttack && !animator.CheckAnimationTag("Hit"))
        {
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
        if(inAttack && canAttack && inputSystem.playerMovement != Vector2.zero)
        {
            inAttack = false;
            canAttack = true;
            animator.CrossFade("Motion", 0.1f);
        }
    }

    private void AttackLockOnTarget()   //攻擊鎖敵
    {
        if(CanAttackLockOn()){
            if (currentTarget != null)
            {
                transform.root.rotation = transform.LockOnTarget(currentTarget, transform.root, 50f);
            }
        }
    }

    private bool CanAttackLockOn()
        {
            if (animator.CheckAnimationTag("Attack"))
            {
                if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.15f)
                {
                    return true;
                }
            }
            return false;
        }

    private void DetectEnemy()
    {
        // 1. 確保有抓到 Main Camera
        if (mainCameraTransform == null)
        {
            if (Camera.main != null) mainCameraTransform = Camera.main.transform;
            else return;
        }

        int count = Physics.OverlapSphereNonAlloc(enemyDetectionCenter.position, enemyDetectionRadius, detectedEnemies, lockLayer);
        
        if (count > 0)
        {
            Transform closestTarget = null;
            float minDistance = attackLockRange; // 只記錄攻擊範圍 (預設 3m) 內的最近距離

            Transform bestCameraTarget = null;
            float highestDot = -1f; // 點積最低是 -1，所以從 -1 開始比

            // 取得鏡頭正前方的平面方向
            Vector3 cameraForward = mainCameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            for (int i = 0; i < count; i++)
            {
                if (detectedEnemies[i] == null) continue;

                // 1. 取得敵人的根物件，避免同一個敵人因為有多個 Collider 而被重複或算錯位置
                Transform enemyRoot = detectedEnemies[i].transform;
                if (enemyRoot == transform.root) continue;

                // 計算玩家到敵人的方向與距離
                Vector3 origin = transform.position + Vector3.up * 1f; // 從玩家胸口/眼睛高度發射，避免地面貼地撞到腳下微小坡度
                Vector3 enemyTargetPos = enemyRoot.position + Vector3.up * 1f; // 射向敵人胸口位置

                Vector3 directionToEnemy = (enemyTargetPos - origin);
                float distanceToEnemy = directionToEnemy.magnitude;
                directionToEnemy.Normalize();

                // 優先判斷：是否在攻擊範圍 (3m) 內且比目前最近目標更近
                if (distanceToEnemy <= minDistance)
                {
                    // Raycast 視線遮擋檢查
                    if (!Physics.Raycast(origin, directionToEnemy, out RaycastHit hit, distanceToEnemy, whatIsObs))
                    {
                        minDistance = distanceToEnemy;
                        closestTarget = enemyRoot;
                    }
                }
                // 次要判斷：若尚未找到攻擊範圍內的目標，才根據鏡頭朝向索敵
                else if (closestTarget == null)
                {
                    // 計算平面方向內積（畫面中央優先度）
                    Vector3 flatDir = (enemyRoot.position - transform.position);
                    flatDir.y = 0;
                    flatDir.Normalize();

                    float dot = Vector3.Dot(cameraForward, flatDir);

                    // 條件 A：必須在鏡頭視野範圍內 (例如 Dot > 0.3)
                    if (dot > highestDot)
                    {
                        // 🔑 條件 B：Raycast 視線遮擋檢查
                        // 射線從玩家發射到敵人，長度為實際距離 distanceToEnemy
                        // 如果沒有打到 whatIsObs，代表視線完全無遮擋！
                        if (!Physics.Raycast(origin, directionToEnemy, out RaycastHit hit, distanceToEnemy, whatIsObs))
                        {
                            highestDot = dot;
                            bestCameraTarget = enemyRoot;
                        }
                    }
                }
            }

            // 優先鎖定攻擊範圍內最近的目標，若無則使用鏡頭朝向目標
            Transform bestTarget = closestTarget != null ? closestTarget : bestCameraTarget;

            // 如果有找到適合的目標，設定為 Target
            if (bestTarget != null)
            {
                SetTarget(bestTarget);
            }
            else
            {
                SetTarget(null); // 範圍內沒有前方目標時釋放鎖定（若不需要可拿掉）
            }
        }
        else
        {
            SetTarget(null); // 範圍內完全沒敵人時釋放鎖定
        }

        // 記得清空非配置陣列，避免舊資料殘留
        System.Array.Clear(detectedEnemies, 0, count);
    }

    private void SetTarget(Transform target)
    {
        if(currentTarget == null || currentTarget != target)
        {
            currentTarget = target;
        }
    }

    private void updateTarget()
    {
        if (animator.CheckAnimationTag("Motion"))
        {
            if(inputSystem.playerMovement.sqrMagnitude > 0.1f)
            {
                currentTarget = null;
            }
        }
    }
    protected override void InitWeapon()
    {
        base.InitWeapon();
        if (currentWeapon == null) return;

        weaponType = currentWeapon.weaponType;
        if(currentWeapon.overrideController != null)
        {
            animator.runtimeAnimatorController = currentWeapon.overrideController;
        }
        animator.SetInteger("WeaponType", weaponType);
        DropsInteract dropsInteract = currentWeapon.GetComponent<DropsInteract>();
        if (dropsInteract != null) dropsInteract.enabled = false;

        PlayerMovementSystem playerMovement = GetComponentInParent<PlayerMovementSystem>();
        if (playerMovement != null)
        {
            playerMovement.runSpeed = currentWeapon.runSpeed;
            playerMovement.walkSpeed = currentWeapon.walkSpeed;
        }

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
