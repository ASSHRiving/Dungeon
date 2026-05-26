using UnityEngine;
using MoveBase;

namespace CombatBase{
    public abstract class CharacterCombatBase : MonoBehaviour
    {
        protected Animator _animator;
        protected CharacterInputSystem _inputSystem;
        protected CharacterMovementBase _movement;
        protected AudioSource _audio;

        [SerializeField, Header("攻擊範圍")] protected Transform attackRangeCenter;
        [SerializeField] protected float attackRangeRadius;
        [SerializeField] protected LayerMask whatIsEnemy;


        //AnimationID
        protected int lAtkID = Animator.StringToHash("LAtk");
        protected int rAtkID = Animator.StringToHash("RAtk");
        protected int defenID = Animator.StringToHash("Defen");
        protected int animationMoveID = Animator.StringToHash("AnimationMove");
        

        protected bool canAttack = true;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            _inputSystem = GetComponentInParent<CharacterInputSystem>();
            _movement = GetComponentInParent<CharacterMovementBase>();
            _audio = _movement.GetComponentInChildren<AudioSource>();
        }

        protected virtual void OnAnimateAttackEvent(string hitName)
        {
            Collider[] attackHits = new Collider[4];
            int count = Physics.OverlapSphereNonAlloc(attackRangeCenter.position, attackRangeRadius, attackHits, whatIsEnemy);

            if(count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    IDamageable damageable = attackHits[i].GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(hitName, transform.root);
                    }
                }
            }
            PlayWeaponEffect();
        }
        private void PlayWeaponEffect()
        {
            if (_animator.CheckAnimationTag("Attack"))
            {
                GameAssets.Instance.PlaySoundEffect(_audio,SoundAssetsType.Sword);
            }
        }

        protected void OnAnimationFinishEvent()
        {
            canAttack = true;
        }   

        

        public void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(attackRangeCenter.position, attackRangeRadius);
        }
    }
}
