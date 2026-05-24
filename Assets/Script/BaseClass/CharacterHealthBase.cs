using MoveBase;
using UnityEngine;

public abstract class CharacterHealthBase : MonoBehaviour, IDamageable
{
    protected Animator _animator;
    protected CharacterMovementBase _movement;

    protected Transform _attacker;

    //AnimationID
    protected int animationMovementID = Animator.StringToHash("AnimationMove");

    protected virtual void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _movement = GetComponent<CharacterMovementBase>();

    }

    private void LateUpdate()
    {
        OnHitLookTarget();
        HitAnimationMotion();
    }

    private void HitAnimationMotion()
    {
        if (_animator.CheckAnimationTag("Hit"))
        {
            _movement.CharacterMoveInterface(transform.forward, _animator.GetFloat(animationMovementID), true);
        }
    }

    public virtual void TakeDamage(int amount)
    {
        throw new System.NotImplementedException("TakeDamage method must be implemented by subclasses.");
    }

    public virtual void TakeDamage(string hitAnimationName)
    {
        _animator.Play(hitAnimationName, 0, 0f);
    }

    public void TakeDamage(string hitAnimationName, Transform attacker)
    {
        _animator.Play(hitAnimationName,0,0f);
        SetAttacker(attacker);
    }

    public virtual void SetAttacker(Transform attacker)
    {
        if (_attacker != attacker || _attacker == null)
            _attacker = attacker;
    }

    private void OnHitLookTarget()
    {
        if(_animator.CheckAnimationTag("Hit"))
            transform.rotation = transform.LockOnTarget(_attacker,transform,50f);
    }

}
