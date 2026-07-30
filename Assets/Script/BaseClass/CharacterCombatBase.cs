using UnityEngine;
using MoveBase;

namespace CombatBase{
    public abstract class CharacterCombatBase : MonoBehaviour
    {
        protected Animator _animator;
        protected CharacterInputSystem _inputSystem;
        protected CharacterMovementBase _movement;
        protected AudioSource _audio;
        private AnimationEventHelper _animationEvent;

        [SerializeField, Header("攻擊範圍")] protected Transform attackRangeCenter;
        [SerializeField] protected float attackRangeRadius;
        [SerializeField] protected LayerMask whatIsEnemy;
        [SerializeField] protected Weapon currentWeapon;
        protected int weaponType;
        protected SoundAssetsType weaponSoundType;


        //AnimationID
        protected int lAtkID = Animator.StringToHash("LAtk");
        protected int rAtkID = Animator.StringToHash("RAtk");
        protected int defenID = Animator.StringToHash("Defen");
        protected int speedID = Animator.StringToHash("Speed");
        protected int animationMoveID = Animator.StringToHash("AnimationMove");
        

        public bool canAttack = true;
        protected bool inAttack = false;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            _inputSystem = GetComponentInParent<CharacterInputSystem>();
            _movement = GetComponentInParent<CharacterMovementBase>();
            _audio = _movement.GetComponentInChildren<AudioSource>();
            _animationEvent = GetComponent<AnimationEventHelper>();
            currentWeapon = GetComponentInChildren<Weapon>();
            weaponSoundType = currentWeapon.weaponSoundType;
        }

        void OnEnable()
        {
            _animationEvent.OnAnimationFinish += AttackFinish;
        }

        protected virtual void OnAnimationAttackEvent(string hitName)
        {
            Collider[] attackHits = new Collider[10];
            int count = Physics.OverlapSphereNonAlloc(attackRangeCenter.position, attackRangeRadius, attackHits, whatIsEnemy);

            if(count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    IDamageable damageable = attackHits[i].GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        Debug.Log($"攻擊到 {attackHits[i]}，造成 {currentWeapon.damage} 傷害");
                        damageable.TakeDamage(hitName, transform.root, currentWeapon.damage);
                    }
                }
            }
            PlayWeaponEffect();
        }
        private void PlayWeaponEffect()
        {
            if (_animator.CheckAnimationTag("Attack"))
            {
                GameAssets.Instance.PlaySoundEffect(_audio,weaponSoundType);
            }
        }

        protected void AttackFinish()
        {
            canAttack = true;
        }

        public void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(attackRangeCenter.position, attackRangeRadius);
        }
    }
}
