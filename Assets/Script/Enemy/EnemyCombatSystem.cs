using CombatBase;
using UnityEngine;

public class EnemyCombatSystem : CharacterCombatBase
{
    [SerializeField, Header("範圍檢測")] private Transform detectionCenter;
    [SerializeField] private float detectionRange;
    [SerializeField] private LayerMask whatisObs;

    Collider[] colliderTarget = new Collider[1];

    [SerializeField, Header("當前目標")] private Transform currentTarget;

    private int lockOnID = Animator.StringToHash("LockOn");

    private void Update()
    {
        AIView();
        LockOnCurrentTarget();
        UpdateAnimationMove();
    }
    private void AIView() //檢查範圍內是否有目標 並且該目標在面前的扇形範圍內
    {
        int count = Physics.OverlapSphereNonAlloc(detectionCenter.position, detectionRange, colliderTarget, whatIsEnemy);
        if(count > 0)
        {
            //檢查與目標之間是否有障礙物遮擋
            if(!Physics.Raycast(transform.root.position + transform.root.up * .5f,
                (colliderTarget[0].transform.position - transform.root.position).normalized, out var hit, detectionRange, whatisObs))
            {
                //檢查目標是否在面前
                if(Vector3.Dot((colliderTarget[0].transform.position - transform.root.position).normalized, transform.root.forward) > 0.4f)
                {
                    currentTarget = colliderTarget[0].transform;
                }
            }      
        }
    }

    private void LockOnCurrentTarget()
    {
        if(_animator.CheckAnimationTag("Motion") || _animator.CheckAnimationTag("Attack"))
        {
            if(currentTarget != null)
            {
                _animator.SetFloat(lockOnID, 1);
                transform.root.rotation = transform.LockOnTarget(currentTarget,transform.root.transform,50f);
            }
        }
        else
        {
            _animator.SetFloat(lockOnID, 0);
        }
    }

    
    public Transform GetCurrentTarget()
    {
        if(currentTarget == null)
        {
            return null;
        }
        else
        {
            return currentTarget;
        }
    }

    private void UpdateAnimationMove()
    {
        if (_animator.CheckAnimationTag("Roll"))
        {
            _movement.CharacterMoveInterface(transform.root.forward, _animator.GetFloat(animationMoveID), true);
        }else if(_animator.CheckAnimationTag("Attack"))
        {
            _movement.CharacterMoveInterface(transform.root.forward, _animator.GetFloat(animationMoveID), true);
        }
    }
    public float GetCurrentTargetDistance() => Vector3.Distance(currentTarget.position, transform.root.position);
}
