using UnityEngine;
using CombatBase;

public class PlayerCombatSystem : CharacterCombatBase
{
    [SerializeField] private Transform currentTarget;

    [SerializeField, Header("檢測敵人")] private Transform enemyDetectionCenter;
    [SerializeField] private float enemyDetectionRadius;

    private Collider[] detectedEnemies = new Collider[1];
    override protected void Awake()
    {
        base.Awake();
        currentWeapon.GetComponentInParent<Rigidbody>().isKinematic = true;
        currentWeapon.GetComponentInParent<Collider>().enabled = false;
    }

    private void Update()
    {
        PlayerAttackAction();
        ActionMotion();
        DetectEnemy();
        updateTarget();
        AttackLockOnTarget();
        CancelAttackMove();
    }

    private void LateUpdate()
    {
        
    }

    private void PlayerAttackAction()
    {
        if (_inputSystem.playerLAtk && canAttack)
        {
            canAttack = false;
            currentWeapon.Attack(_animator);
            inAttack = true;
        }
    }

    private void ActionMotion()
    {
        if (_animator.CheckAnimationTag("Attack"))
        {
            _movement.CharacterMoveInterface(transform.forward, _animator.GetFloat(animationMoveID)*4f, true);
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
                if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.75f)
                {
                    return true;
                }
            }
            return false;
        }

    private void DetectEnemy()
    {
        int count = Physics.OverlapSphereNonAlloc(enemyDetectionCenter.position, enemyDetectionRadius, detectedEnemies, whatIsEnemy);
        if(count > 0)
        {
            SetTarget(detectedEnemies[0].transform);
        }
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

    }
}

