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
    protected Animator animator;
    protected CharacterInputSystem inputSystem;
    protected CharacterMovementBase movement;
    protected CharacterHealthBase health;
    protected CharacterStats stats; // 中央數值大腦
    [SerializeField] protected AudioSource audioSource;
    private AnimationEventHelper animationEvent;
    
    //===================================================================
    [Header("攻擊設定")]
    [SerializeField] protected HitboxData[] hitboxes;
    [SerializeField] protected LayerMask whatIsEnemy;
    private Collider[] attackHits = new Collider[10];
    private bool isHitboxActive = false;
    private AttackData currentAttackData;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>(); // 用來記錄「當前這一招已經命中過哪些敵人」，防止重複造成傷害

    //===================================================================

    public const string WEAPON_STAT_SOURCE = "EquippedWeapon";

    public Weapon currentWeapon;
    [Header("基礎數值 (向下相容)")]
    public float damage = 0f;
    public float critRate = 0f;

    public float EffectiveDamage => stats != null ? stats.AttackDamage : damage;
    public float EffectiveCritRate => stats != null ? stats.CritRate : critRate;
    public float EffectiveCritDamage => stats != null ? stats.CritDamage : 1.5f;

    /// <summary>
    /// 總攻擊力：當 stats 存在時，武器傷害已作為 Flat 基質計入 stats.AttackDamage 中，並享受所有百分比 Buff 乘算！
    /// 僅在 stats 為空時作為相容 fallback 手動加算
    /// </summary>
    public float totalDamage => stats != null ? stats.AttackDamage : (damage + (currentWeapon != null ? currentWeapon.damage : 0f));

    protected SoundAssetsType weaponSoundType;
    protected TrailRenderer[] weaponTrails;
    protected ParticleSystem weaponEffect;
    [SerializeField] protected GameObject projectilePrefab;

    [SerializeField] protected Transform currentTarget;

    //AnimationID
    protected int lAtkID = Animator.StringToHash("LAtk");
    protected int rAtkID = Animator.StringToHash("RAtk");
    protected int defenID = Animator.StringToHash("Defen");
    protected int speedID = Animator.StringToHash("Speed");
    protected int animationMoveID = Animator.StringToHash("AnimationMove");
    public bool canAttack = true;
    public bool inAttack = false;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        inputSystem = GetComponentInParent<CharacterInputSystem>();
        movement = GetComponentInParent<CharacterMovementBase>();
        if (movement != null)
        {
            audioSource = movement.GetComponentInChildren<AudioSource>();
        }
        animationEvent = GetComponent<AnimationEventHelper>();
        health = GetComponentInParent<CharacterHealthBase>();
        stats = GetComponent<CharacterStats>() ?? GetComponentInParent<CharacterStats>();
        currentWeapon = GetComponentInChildren<Weapon>();
        InitWeapon();
    }

    protected virtual void Start()
    {
        if (stats == null)
        {
            stats = GetComponent<CharacterStats>() ?? GetComponentInParent<CharacterStats>();
        }
        UpdateWeaponStatModifier();
    }

    protected virtual void OnDestroy()
    {
        ClearWeaponModifier();
    }

    public const float BASE_RUN_SPEED = 7f;

    /// <summary>
    /// 將當前裝備的武器屬性加成 (AttackDamage, MoveSpeed, CritRate 等)
    /// 以 StatModifier 形式註冊進中央數值大腦 (CharacterStats)
    /// 優先由 WeaponDataSO 動態注入所有配置之 Modifiers；若無配置則回退相容舊版數值欄位。
    /// </summary>
    public void UpdateWeaponStatModifier()
    {
        if (stats == null) return;

        ClearWeaponModifier();

        if (currentWeapon != null)
        {
            // 優先由 WeaponDataSO 注入純淨的 StatModifier 清單
            if (currentWeapon.WeaponData != null && currentWeapon.WeaponData.statBonuses != null && currentWeapon.WeaponData.statBonuses.Count > 0)
            {
                for (int i = 0; i < currentWeapon.WeaponData.statBonuses.Count; i++)
                {
                    var bonus = currentWeapon.WeaponData.statBonuses[i];
                    stats.AddModifier(new StatModifier(bonus.statType, bonus.modType, bonus.value, WEAPON_STAT_SOURCE));
                }
            }
            else
            {
                // Fallback: 舊版數值欄位相容處理
                if (currentWeapon.damage > 0f)
                {
                    stats.AddModifier(new StatModifier(StatType.AttackDamage, StatModType.Flat, currentWeapon.damage, WEAPON_STAT_SOURCE));
                }

                float moveSpeedDiff = currentWeapon.runSpeed - BASE_RUN_SPEED;
                if (!Mathf.Approximately(moveSpeedDiff, 0f))
                {
                    stats.AddModifier(new StatModifier(StatType.MoveSpeed, StatModType.Flat, moveSpeedDiff, WEAPON_STAT_SOURCE));
                }
            }
        }
    }

    /// <summary>
    /// 移除武器註冊在 CharacterStats 中的屬性修正器 (例如卸除/丟棄/切換武器時)
    /// </summary>
    public void ClearWeaponModifier()
    {
        if (stats != null)
        {
            stats.RemoveAllModifiersFromSource(WEAPON_STAT_SOURCE);
        }
    }

    /// <summary>
    /// 動態綁定/重新綁定中央數值大腦 (供 EnemyFactory 生成怪物時注入)
    /// </summary>
    public void BindStats(CharacterStats newStats)
    {
        ClearWeaponModifier();
        stats = newStats;
        UpdateWeaponStatModifier();
    }

    void OnEnable()
    {
        if (animationEvent != null)
        {
            animationEvent.OnAnimationFinish += AttackFinish;
        }
    }

    void OnDisable()
    {
        if (animationEvent != null)
        {
            animationEvent.OnAnimationFinish -= AttackFinish;
        }
    }

    protected virtual void Update()
    {
        // 只要 Hitbox 處於開啟狀態，每一幀都進行重疊檢測
        if (isHitboxActive && animator != null && animator.CheckAnimationTag("Attack"))
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
        if(projectilePrefab == null) return;
        Transform shootPoint = hitboxes[attackData.hitboxes[0]].center;
        GameObject projectileGO = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
        Projectile projectile = projectileGO.GetComponent<Projectile>();
        if(projectile != null)
        {
            projectile.Setup(this.transform, currentTarget, attackData);
        }
    }
    #endregion

    private void CheckAttackHitbox() // 當開啟isHitboxActive = true時，每一幀都會檢查攻擊範圍內的敵人
    {
        if (currentAttackData == null) return;

        foreach(int index in currentAttackData.hitboxes) // attack data裡面有指定要開啟的hitbox
        {
            if(index > hitboxes.Length - 1) continue;

            // 使用 OverlapSphereNonAlloc 來檢測指定 hitbox 的範圍內的敵人
            int count = Physics.OverlapSphereNonAlloc(hitboxes[index].center.position, hitboxes[index].radius, attackHits, whatIsEnemy);
            for (int i = 0; i < count; i++)
            {
                IDamageable damageable = attackHits[i].GetComponentInParent<IDamageable>();
                
                // 如果該物體有 IDamageable 接口，且尚未被命中過，則進行傷害計算
                if (damageable != null && !hitTargets.Contains(damageable))
                {
                    hitTargets.Add(damageable); // 標記為已命中，避免在同一招式中重複造成傷害

                    bool isCritical = Random.value < (EffectiveCritRate / 100f); // 判斷是否暴擊
                    float attackDamage = Mathf.Clamp(totalDamage, 0f, float.MaxValue);
                    float finalDamage = attackDamage * currentAttackData.damageMultiplier; // 解析attack data中的傷害倍率，計算最終傷害
                    if (isCritical) finalDamage *= EffectiveCritDamage; // 暴擊傷害加成 (基礎1.5倍，受裝備與Buff加成)

                    Debug.Log($"[命中新目標] {attackHits[i].name} | 傷害: {finalDamage}");
                    // 呼叫攻擊目標的 TakeDamage 方法，交由目標自己處理傷害邏輯
                    damageable.TakeDamage(transform.root, finalDamage, currentAttackData, isCritical);
                    // 觸發受擊特效
                    if (currentWeapon != null)
                    {
                        currentWeapon.SpawnHitImpact(attackHits[i]);
                    }
                }
            }
        }
    }

    protected virtual void OnAnimationAttackEvent(string hitName)
    {
        Debug.LogWarning("Legacy Attack Event");
    }

    private void PlayWeaponEffect()
    {
        if (animator != null && currentAttackData != null && animator.CheckAnimationTag("Attack"))
        {
            if (audioSource != null)
            {
                GameAssets.Instance.PlaySoundEffect(audioSource, currentAttackData.weaponSound);
            }
        }
    }

    protected void AttackFinish()
    {
        canAttack = true;
    }

    protected virtual void InitWeapon()
    {
        UpdateWeaponStatModifier();

        if (currentWeapon == null) return;

        // 更改 Hitbox 的中心點與半徑，對應武器的攻擊範圍
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
