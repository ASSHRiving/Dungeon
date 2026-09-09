using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public abstract class CharacterHealthBase : MonoBehaviour, IDamageable
{
    [Header("血條UI")]
    public Image healthBar;
    public Image healthBarBuffer;
    public Image healthUI;
    public TMP_Text healthText;

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
    protected Coroutine bufferCoroutine;
    [SerializeField] protected MonoBehaviour[] scriptsToDisable;


    protected Animator _animator;
    protected CharacterMovementBase _movement;
    protected CharacterCombatBase _combat; 
    protected Transform _attacker;
    protected AudioSource _audio;

    //AnimationID
    protected int animationMovementID = Animator.StringToHash("AnimationMove");

    protected abstract void Die();

    protected virtual void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _movement = GetComponent<CharacterMovementBase>();
        _combat = GetComponentInChildren<CharacterCombatBase>();
        _audio = _movement.GetComponentInChildren<AudioSource>();
    }
    private void Update()
    {
        RecoverPoise();
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
    private void OnHitLookTarget()
    {
        if(_animator.CheckAnimationTag("Hit"))
            transform.rotation = transform.LockOnTarget(_attacker,transform,50f);
    }

    protected void UpdateHealthBar(float targetFill)
    {
        healthBar.fillAmount = targetFill;
        healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
        if(bufferCoroutine != null)
        {
            StopCoroutine(bufferCoroutine);
        }
        bufferCoroutine = StartCoroutine(UpdateHealthBarBuffer(targetFill));
    }
    IEnumerator UpdateHealthBarBuffer(float targetFill)
    {
        yield return new WaitForSeconds(0.2f);
        float startFill = healthBarBuffer.fillAmount;
        for(float t = 0; t < 0.25f; t += Time.deltaTime)
        {
            healthBarBuffer.fillAmount = Mathf.Lerp(startFill, targetFill, t / 0.25f);
            yield return null;
        }
        healthBarBuffer.fillAmount = targetFill;
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
    public virtual void SetAttacker(Transform attacker)
    {
        if (_attacker != attacker || _attacker == null)
            _attacker = attacker;
    }
    public virtual void TakeDamage(int amount)
    {
        throw new System.NotImplementedException("TakeDamage method must be implemented by subclasses.");
    }

    public virtual void TakeDamage(Transform attacker, float damageAmount, AttackData attackData, bool isCritical = false)
    {
        _animator.Play(attackData.hitAnimationName,0,0f);
        SetAttacker(attacker);
        GameAssets.Instance.PlaySoundEffect(_audio, SoundAssetsType.Hit);
    }
    public void ModifyMaxHealth(float newMaxHealth)
    {
        float healthRatio = currentHealth / maxHealth;
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Clamp(maxHealth * healthRatio, 1f, maxHealth);
        UpdateHealthBar(currentHealth / maxHealth);
    }
    #endregion

}
