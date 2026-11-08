using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.Cinemachine;
using DG.Tweening;

public abstract class CharacterHealthBase : MonoBehaviour, IDamageable
{
    [Header("血條UI")]
    public Image healthBar;
    public Image healthBarBuffer;
    public Image healthUI;
    public TMP_Text healthText;

    [Header("血條 DOTween 動態參數")]
    [SerializeField] protected float mainBarDuration = 0.05f;   // 主血條瞬扣時間
    [SerializeField] protected float bufferHoldDelay = 0.25f;   // 連擊時殘影停滯延遲 (Combo Hold)
    [SerializeField] protected float bufferCatchUpTime = 0.35f; // 殘影追趕時間

    [Header("基礎數值 (向下相容)")]
    [SerializeField] public float maxHealth = 100;
    [SerializeField] public float shield = 10;

    [Header("韌性系統 (Poise System)")]
    public float maxPoise = 100f;        // 最大韌性值
    public float currentPoise = 100f;    // 當前韌性值
    public float poiseRecoveryRate = 20f; // 每秒恢復的韌性
    protected float poiseRecoveryTimer = 0f;
    public float poiseRecoveryDelay = 3f; // 停止受擊後多長時間開始恢復韌性

    public bool isDead { get; protected set; } = false;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected MonoBehaviour[] scriptsToDisable;

    protected CharacterStats stats; // 中央數值大腦
    protected Tweener healthBarTween;
    protected Tweener bufferBarTween;

    protected Animator animator;
    protected CharacterMovementBase movement;
    protected CharacterCombatBase combat;
    protected Transform attacker;
    protected AudioSource audioSource;
    protected CinemachineImpulseSource impulseSource;

    // AnimationID
    protected int animationMovementID = Animator.StringToHash("AnimationMove");

    protected abstract void Die();

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<CharacterMovementBase>();
        combat = GetComponentInChildren<CharacterCombatBase>();
        stats = GetComponent<CharacterStats>() ?? GetComponentInParent<CharacterStats>();

        if (movement != null)
            audioSource = movement.GetComponentInChildren<AudioSource>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    protected virtual void OnEnable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged += HandleStatsHealthChanged;
        }
    }

    protected virtual void OnDisable()
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= HandleStatsHealthChanged;
        }
    }

    /// <summary>
    /// 動態綁定/重新綁定中央數值大腦 (供 EnemyFactory 生成怪物時注入)
    /// </summary>
    public void BindStats(CharacterStats newStats)
    {
        if (stats != null)
        {
            stats.OnHealthChanged -= HandleStatsHealthChanged;
        }
        stats = newStats;
        if (stats != null)
        {
            stats.OnHealthChanged += HandleStatsHealthChanged;
            maxHealth = stats.MaxHealth;
            currentHealth = stats.CurrentHealth;
            float initialFill = maxHealth > 0 ? currentHealth / maxHealth : 1f;
            if (healthBar != null) healthBar.fillAmount = initialFill;
            if (healthText != null) healthText.text = $"{currentHealth}/{maxHealth}";
            UpdateHealthBar(initialFill);
        }
    }

    protected virtual void Start()
    {
        if (stats != null)
        {
            maxHealth = stats.MaxHealth;
            currentHealth = stats.CurrentHealth;
        }
        else
        {
            currentHealth = maxHealth;
        }

        float initialFill = maxHealth > 0 ? currentHealth / maxHealth : 1f;
        if (healthBar != null) healthBar.fillAmount = initialFill;
        if (healthText != null) healthText.text = $"{Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
        UpdateHealthBar(initialFill);
    }

    protected virtual void OnDestroy()
    {
        healthBarTween?.Kill();
        bufferBarTween?.Kill();
    }

    private void Update()
    {
        if (isDead) return;
        RecoverPoise();
    }

    private void LateUpdate()
    {
        if (isDead) return;
        OnHitLookTarget();
        HitAnimationMotion();
    }

    private void HandleStatsHealthChanged(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
        UpdateHealthBar(max > 0 ? current / max : 0f);
    }

    private void HitAnimationMotion()
    {
        if (animator != null && movement != null && animator.CheckAnimationTag("Hit"))
        {
            movement.CharacterMoveInterface(transform.forward, animator.GetFloat(animationMovementID), true);
        }
    }

    private void OnHitLookTarget()
    {
        if (animator != null && attacker != null && animator.CheckAnimationTag("Hit"))
        {
            transform.rotation = transform.LockOnTarget(attacker, transform, 50f);
        }
    }

    protected virtual void UpdateHealthBar(float targetFill)
    {
        targetFill = Mathf.Clamp01(targetFill);

        if (healthText != null)
        {
            healthText.SetText("{0:0}/{1:0}", currentHealth, maxHealth);
        }

        // 1. 主血條更新 (微幅平滑瞬扣)
        if (healthBar != null)
        {
            healthBarTween?.Kill();
            healthBarTween = healthBar.DOFillAmount(targetFill, mainBarDuration).SetEase(Ease.OutQuad);
        }

        // 2. 緩衝殘影條更新 (DOTween Combo Hold 延遲追趕)
        if (healthBarBuffer != null)
        {
            bufferBarTween?.Kill();

            if (targetFill < healthBarBuffer.fillAmount)
            {
                // 受傷扣血：每次受擊重置延遲，停手後以 OutQuad 平滑追上紅條
                bufferBarTween = healthBarBuffer.DOFillAmount(targetFill, bufferCatchUpTime)
                    .SetDelay(bufferHoldDelay)
                    .SetEase(Ease.OutQuad);
            }
            else
            {
                // 回血或提升上限：直接同步跟上，不延遲
                healthBarBuffer.fillAmount = targetFill;
            }
        }
    }

    private void RecoverPoise()
    {
        if (currentPoise < maxPoise)
        {
            if (poiseRecoveryTimer > 0)
            {
                poiseRecoveryTimer -= Time.deltaTime;
            }
            else
            {
                currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRecoveryRate * Time.deltaTime);
            }
        }
    }

    #region 外部方法
    public virtual void SetAttacker(Transform _attacker)
    {
        if (attacker != _attacker || attacker == null)
            attacker = _attacker;
    }

    public virtual void TakeDamage(int amount)
    {
        throw new System.NotImplementedException("TakeDamage method must be implemented by subclasses.");
    }

    public virtual void TakeDamage(Transform attacker, float damageAmount, AttackData attackData, bool isCritical = false)
    {
        animator.Play(attackData.hitAnimationName, 0, 0f);
        SetAttacker(attacker);
        GameAssets.Instance.PlaySoundEffect(audioSource, attackData.hitSound);
    }

    public void ModifyMaxHealth(float newMaxHealth)
    {
        if (stats != null)
        {
            stats.GetStatByType(StatType.MaxHealth).BaseValue = newMaxHealth;
        }
        else
        {
            float healthRatio = maxHealth > 0 ? currentHealth / maxHealth : 1f;
            maxHealth = newMaxHealth;
            currentHealth = Mathf.Clamp(maxHealth * healthRatio, 1f, maxHealth);
            UpdateHealthBar(maxHealth > 0 ? currentHealth / maxHealth : 1f);
        }
    }

    public void Recover(float amount)
    {
        if (stats != null)
        {
            stats.Heal(amount);
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth + amount, 1f, maxHealth);
            UpdateHealthBar(maxHealth > 0 ? currentHealth / maxHealth : 1f);
        }
    }
    #endregion
}
