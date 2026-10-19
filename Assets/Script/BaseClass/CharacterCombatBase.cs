using UnityEngine;
using System.Collections.Generic;


public abstract class CharacterCombatBase : MonoBehaviour
{
    [System.Serializable]
    public class HitboxData
    {
        public string hitboxName;
        public Transform center;
        public float radius;
    }
    protected Animator _animator;
    protected CharacterInputSystem _inputSystem;
    protected CharacterMovementBase _movement;
    protected CharacterHealthBase _health;
    [SerializeField]protected AudioSource _audio;
    private AnimationEventHelper _animationEvent;
    
    //===================================================================
    [Header("攻擊設定")]
    [SerializeField] protected HitboxData[] hitboxes;
    [SerializeField] protected LayerMask whatIsEnemy;
    private Collider[] attackHits = new Collider[10];
    private bool isHitboxActive = false;
    private AttackData currentAttackData;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();   // 用來記錄「當前這一招已經命中過哪些敵人」，防止重複造成傷害

    //===================================================================

    public Weapon currentWeapon;
    public float damage = 0f;
    public float critRate = 0f;
    public float totalDamage => currentWeapon.damage + damage;
    protected SoundAssetsType weaponSoundType;
    protected TrailRenderer[] weaponTrails;
    protected ParticleSystem weaponEffect;
    [SerializeField] protected GameObject arrowPrefab;

    [SerializeField] protected Transform currentTarget;

    //AnimationID
    protected int lAtkID = Animator.StringToHash("LAtk");
    protected int rAtkID = Animator.StringToHash("RAtk");
    protected int defenID = Animator.StringToHash("Defen");
    protected int speedID = Animator.StringToHash("Speed");
    protected int animationMoveID = Animator.StringToHash("AnimationMove");
    public bool canAttack = true;
    

    /*
    private bool _canAttack = true;
    public bool canAttack
    {
        get => _canAttack;
        set
        {
            // 當有人把它從 false 改成 true，或者從 true 改成 false 時，印出到底是誰改的！
            if (_canAttack != value)
            {
                Debug.Log($"[canAttack 狀態改變] 變成 {value}，呼叫來源：{System.Environment.StackTrace}");
            }
            _canAttack = value;
        }
    }
    */
    
    public bool inAttack = false;

    protected virtual void Awake()
    {
        _animator = GetComponent<Animator>();
        _inputSystem = GetComponentInParent<CharacterInputSystem>();
        _movement = GetComponentInParent<CharacterMovementBase>();
        _audio = _movement.GetComponentInChildren<AudioSource>();
        _animationEvent = GetComponent<AnimationEventHelper>();
        _health = GetComponentInParent<CharacterHealthBase>();
        currentWeapon = GetComponentInChildren<Weapon>();
        InitWeapon();
    }

    void OnEnable()
    {
        _animationEvent.OnAnimationFinish += AttackFinish;
    }

    protected virtual void Update()
    {
        // 只要 Hitbox 處於開啟狀態，每一幀都進行重疊檢測
        if (isHitboxActive && _animator.CheckAnimationTag("Attack"))
        {
            CheckAttackHitbox();
        }
        else
        {
            OnAttackHitboxEnd(); // 確保在攻擊動畫結束後，Hitbox 也會被關閉
        }
    }

    #region 動畫事件
    public void OnAttackHitboxStart(AttackData attackData)
    {
        isHitboxActive = true; // 開啟hitbox，在Update()開始攻擊檢測
        currentAttackData = attackData; // 紀錄當前的attack data 在攻擊檢測中使用
        hitTargets.Clear(); // 每次開啟新招式時，清空歷史命中紀錄
        EnableWeaponTrails();
        if(weaponEffect != null)
        {
            weaponEffect.Clear(); // 清空舊粒子
            var emission = weaponEffect.emission;
            emission.enabled = true; // 開啟發射
            weaponEffect.Play();
        }
        PlayWeaponEffect();
    }
    public void OnAttackHitboxEnd()
    {
        isHitboxActive = false;
        currentAttackData = null;
        hitTargets.Clear();
        DisableWeaponTrails();
        if(weaponEffect != null)
        {
            var emission = weaponEffect.emission;
            emission.enabled = false;
        }
    }

    public void OnAnimationShoot(AttackData attackData)
    {
        if(arrowPrefab == null) return;
        Transform shootPoint = hitboxes[attackData.hitboxes[0]].center;
        GameObject arrowGO = Instantiate(arrowPrefab, shootPoint.position, shootPoint.rotation);
        Arrow arrow = arrowGO.GetComponent<Arrow>();
        if(arrow != null)
        {
            arrow.Setup(this.transform, currentTarget, attackData);
        }
    }
    #endregion

    private void CheckAttackHitbox() // 當開啟isHitboxActive = true時，每一幀都會檢查攻擊範圍內的敵人
    {
        foreach(int index in currentAttackData.hitboxes) // attack data裡面有指定要開啟的hitbox，可能是右手武器、左手武器、或是其他hitbox
        {
            if(index > hitboxes.Length - 1) continue;

            // 使用 OverlapSphereNonAlloc 來檢測指定 hitbox 的範圍內的敵人，存入 attackHits 陣列，並返回命中的數量
            int count = Physics.OverlapSphereNonAlloc(hitboxes[index].center.position, hitboxes[index].radius, attackHits, whatIsEnemy);
            for (int i = 0; i < count; i++)
            {
                IDamageable damageable = attackHits[i].GetComponentInParent<IDamageable>();
                
                // 如果該物體有 IDamageable 接口，且尚未被命中過，則進行傷害計算
                if (damageable != null && !hitTargets.Contains(damageable))
                {
                    hitTargets.Add(damageable); // 標記為已命中，避免在同一招式中重複造成傷害

                    bool isCritical = Random.value < (critRate / 100f); // 判斷是否暴擊
                    float attackDamage = Mathf.Clamp(currentWeapon.damage + damage, 0f, float.MaxValue);
                    float finalDamage = attackDamage * currentAttackData.damageMultiplier; // 解析attack data中的傷害倍率，計算最終傷害
                    if(isCritical) finalDamage *= 1.5f; // 暴擊傷害加成

                    Debug.Log($"[命中新目標] {attackHits[i].name} | 傷害: {finalDamage}");
                    // 呼叫攻擊目標的 TakeDamage 方法，傳入攻擊者、最終傷害值、attack data和是否暴擊，交由目標自己處理傷害邏輯
                    damageable.TakeDamage(transform.root, finalDamage, currentAttackData, isCritical);
                    // 觸發受擊特效
                    currentWeapon.SpawnHitImpact(attackHits[i]);
                }
            }
        }
    }
    protected virtual void OnAnimationAttackEvent(string hitName)
    {
        Debug.LogWarning("Legacy Attack Event");
        /*
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
                    damageable.TakeDamage(hitName, transform.root, currentWeapon.damage, 10);
                }
            }
        }
        PlayWeaponEffect();
        */
    }
    private void PlayWeaponEffect()
    {
        if (_animator.CheckAnimationTag("Attack"))
        {
            GameAssets.Instance.PlaySoundEffect(_audio, currentAttackData.weaponSound);
        }
    }

    protected void AttackFinish()
    {
        canAttack = true;
    }

    protected virtual void InitWeapon()
    {
        //更改Hitbox的中心點與半徑，對應武器的攻擊範圍
        if(currentWeapon.attackPoint != null && currentWeapon.attackRadius != 0)
        {
            if(hitboxes != null && hitboxes.Length > 0){
                hitboxes[0].hitboxName = "主武器";
                hitboxes[0].center = currentWeapon.attackPoint;
                hitboxes[0].radius = currentWeapon.attackRadius;
            }
        }

        weaponSoundType = currentWeapon.weaponSoundType;
        Rigidbody rb = currentWeapon.GetComponent<Rigidbody>();
        Collider col = currentWeapon.GetComponent<Collider>();
        if(rb != null)
        {
            rb.isKinematic = true;
        }
        if(col != null)
        {
            col.enabled = false;
        }
        if(currentWeapon.weaponTrails != null)
        {
            weaponTrails = currentWeapon.weaponTrails;
        }
        else
        {
            weaponTrails = null;
        }
        if(currentWeapon.weaponEffect != null)
        {
            weaponEffect = currentWeapon.weaponEffect;
            weaponEffect.Stop();
        }
    }
    private void DisableWeaponTrails()
    {
        if(weaponTrails == null) return;
        for(int i = 0; i < weaponTrails.Length; i++)
        {
            if(weaponTrails[i] != null)
            {
                weaponTrails[i].emitting = false;
            }
        }
    }
    private void EnableWeaponTrails()
    {
        if(weaponTrails == null) return;
        for(int i = 0; i < weaponTrails.Length; i++)
        {
            if(weaponTrails[i] != null)
            {
                weaponTrails[i].Clear();
                weaponTrails[i].emitting = true;
            }
        }
    }
    #if UNITY_EDITOR
    // 🎨 開發除錯：可以在 Scene 畫面預覽 Hitbox 的球體範圍
    private void OnDrawGizmosSelected()
    {
        if (hitboxes == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        foreach (var hb in hitboxes)
        {
            if (hb != null && hb.center != null)
            {
                Gizmos.DrawSphere(hb.center.position, hb.radius);
            }
        }
    }
    #endif
}

