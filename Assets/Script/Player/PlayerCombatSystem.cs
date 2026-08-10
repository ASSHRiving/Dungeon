using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerCombatSystem : CharacterCombatBase
{
    [SerializeField] private Transform currentTarget;
    [SerializeField] private Transform weaponHolder;

    [SerializeField, Header("檢測敵人")] private Transform enemyDetectionCenter;
    [SerializeField] private float enemyDetectionRadius;

    private Collider[] detectedEnemies = new Collider[10];
    override protected void Awake()
    {
        base.Awake();

        weaponType = currentWeapon.weaponType;
        if(currentWeapon.overrideController != null)
        {
            _animator.runtimeAnimatorController = currentWeapon.overrideController;
        }
        // _animator.runtimeAnimatorController = currentWeapon.overrideController;
        _animator.SetInteger("WeaponType", weaponType);
        currentWeapon.GetComponent<Rigidbody>().isKinematic = true;
        currentWeapon.GetComponent<Collider>().enabled = false;
    }

    private void Update()
    {
        PlayerAttackAction();
        ActionMotion();
        DetectEnemy();
        updateTarget();
        AttackLockOnTarget();
        CancelAttackMove();
        //ResetInAttack();
    }

    private void PlayerAttackAction()
    {
        if (_inputSystem.playerLAtk && canAttack && !_animator.CheckAnimationTag("Hit"))
        {
            canAttack = false;
            currentWeapon.Attack(_animator);
            inAttack = true;
        }
    }

    //攻擊時AnimationMove參數
    private void ActionMotion()
    {
        if (_animator.CheckAnimationTag("Attack"))
        {
            _movement.CharacterMoveInterface(transform.forward, _animator.GetFloat(animationMoveID)*4f, true);
        }
    }
    private void ResetInAttack()
    {
        if (_animator.CheckAnimationTag("Motion"))
        {
            inAttack = false;
        }
    }
    private void CancelAttackMove()
    {
        if(inAttack && canAttack && _animator.GetFloat(speedID) > 0.2)
        {
            inAttack = false;
            canAttack = true;
            _animator.CrossFade("Motion", 0.1f);
        }
    }

    private void AttackLockOnTarget()
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
            if (_animator.CheckAnimationTag("Attack"))
            {
                if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.65f)
                {
                    return true;
                }
            }
            return false;
        }

    private void DetectEnemy()
    {
        int count = Physics.OverlapSphereNonAlloc(enemyDetectionCenter.position, enemyDetectionRadius, detectedEnemies, whatIsEnemy);
        
        if (count > 0)
        {
            Transform bestTarget = null;
            float highestDot = -1f; // 點積最低是 -1，所以從 -1 開始比

            // 取得玩家當前正前方的方向 vector
            Vector3 playerForward = transform.forward;

            for (int i = 0; i < count; i++)
            {
                if (detectedEnemies[i] == null) continue;

                // 1. 取得敵人的根物件，避免同一個敵人因為有多個 Collider 而被重複或算錯位置
                Transform enemyRoot = detectedEnemies[i].transform.root;

                // 2. 計算從「玩家」指向「敵人」的向量（忽略 Y 軸高度差，讓純平面旋轉更精確）
                Vector3 directionToEnemy = (enemyRoot.position - transform.position);
                directionToEnemy.y = 0; // 如果敵人高低差大，歸零可以避免影響視線角度判斷
                directionToEnemy.Normalize(); // 單位化，確保長度為 1

                // 3. 計算內積（Dot Product）
                float dot = Vector3.Dot(playerForward, directionToEnemy);

                // 5. 找出內積最大（最接近玩家面向）的敵人
                if (dot > highestDot)
                {
                    highestDot = dot;
                    bestTarget = enemyRoot;
                }
            }

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
        if (_animator.CheckAnimationTag("Motion"))
        {
            if(_inputSystem.playerMovement.sqrMagnitude > 0.1f)
            {
                currentTarget = null;
            }
        }
    }
    public void ChangeWeapon(GameObject newWeapon)
    {
        _animator.Play("Crouch", 0, 0f);
        StartCoroutine(WaitAndChangeWeapon(newWeapon));
        
    }
    private IEnumerator WaitAndChangeWeapon(GameObject newWeapon)
    {
        yield return new WaitForSeconds(0.5f);
        //丟掉舊武器
        if(currentWeapon != null)
        {
            currentWeapon.GetComponent<Rigidbody>().isKinematic = false;
            currentWeapon.GetComponent<Collider>().enabled = true;
            currentWeapon.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(currentWeapon.gameObject, SceneManager.GetActiveScene());

        }
        //撿起新武器
        newWeapon.transform.SetParent(weaponHolder);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;

        currentWeapon = newWeapon.GetComponent<Weapon>();
        weaponType = currentWeapon.weaponType;
        _animator.runtimeAnimatorController = currentWeapon.overrideController;
        _animator.SetInteger("WeaponType", weaponType);
        weaponSoundType = currentWeapon.weaponSoundType;

        currentWeapon.GetComponent<Rigidbody>().isKinematic = true;
        currentWeapon.GetComponent<Collider>().enabled = false;
        currentWeapon.GetComponent<DropsInteract>().enabled = false;
    }
}

