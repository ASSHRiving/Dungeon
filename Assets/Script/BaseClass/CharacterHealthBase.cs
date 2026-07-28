using MoveBase;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using CombatBase;
public abstract class CharacterHealthBase : MonoBehaviour, IDamageable
{
    [Header("血量")]
    public Image healthBar;
    public Image healthBarBuffer;
    public Image healthUI;
    public TMP_Text healthText;
    protected float maxHealth;
    protected bool isDead = false;
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

    protected virtual void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _movement = GetComponent<CharacterMovementBase>();
        _audio = _movement.GetComponentInChildren<AudioSource>();

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

    public virtual void TakeDamage(string hitAnimationName, Transform attacker)
    {
        _animator.Play(hitAnimationName,0,0f);
        SetAttacker(attacker);
        GameAssets.Instance.PlaySoundEffect(_audio, SoundAssetsType.Hit);
    }
    public virtual void TakeDamage(string hitAnimationName, Transform attacker, float damageAmount)
    {
        _animator.Play(hitAnimationName,0,0f);
        SetAttacker(attacker);
        GameAssets.Instance.PlaySoundEffect(_audio, SoundAssetsType.Hit);
    }
    protected abstract void Die();

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

    protected void UpdateHealthBar(float targetFill)
    {
        healthBar.fillAmount = targetFill;
        healthText.text = $"{currentHealth}/{maxHealth}";
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

}
