using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthSystem : CharacterHealthBase
{
    [SerializeField] private AudioClip missSound;
    [SerializeField] private AudioClip deathSound;

    void Start()
    {
        currentHealth = maxHealth;
        healthBar.fillAmount = currentHealth / maxHealth;
        healthText.text = $"{currentHealth}/{maxHealth}";
    }
    public override void TakeDamage(Transform attacker, float damageAmount, AttackData attackData, bool isCritical = false)
    {
        if (_movement.immune)
        {
            Debug.Log("閃避");
            GameAssets.Instance.DoHitstop(0.25f, 0.1f);
            _audio.PlayOneShot(missSound, 3f);
            return;
        }else if(isDead)
        {
            return;
        }

        //減傷公式
        float armorDR = shield / (shield + 100f);
        armorDR = Mathf.Clamp(armorDR, 0f, 0.85f);
        int finalDamage = Mathf.RoundToInt(damageAmount * (1f - armorDR));

        currentHealth = Mathf.Clamp(currentHealth - finalDamage, 0f, maxHealth);
        UpdateHealthBar(currentHealth / maxHealth);
        

        //受擊反饋
        GameAssets.Instance.PlaySoundEffect(_audio, attackData.hitSound);
        if(impulseSource != null)
        {
            impulseSource.GenerateImpulseWithForce(attackData.shakeForce);
        }
        GameAssets.Instance.DoHitstop(attackData.hitStopTime, 0.03f); // 觸發 Hitstop (頓幀)
        SetAttacker(attacker);

        //受擊動畫
        if(finalDamage/maxHealth > 0.05f)
        {
            // 扣除韌性與判斷是否被打斷/播放受擊動畫
            currentPoise -= attackData.poiseDamage;
            poiseRecoveryTimer = poiseRecoveryDelay; // 刷新恢復延遲時間
            if(currentPoise <= 0)
            {
                _animator.CrossFade(attackData.hitAnimationName, 0.1f);
                _combat.canAttack = true;
                _combat.currentWeapon.combo = 0;

                currentPoise = maxPoise;
            }     
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    protected override void Die()
    {
        UIEvents.PlayerDied();
        GameManager.Instance.AddDeath();
        _audio.PlayOneShot(deathSound, 2f);
        //GameAssets.Instance.PlaySoundEffect(_audio, SoundAssetsType.Death);
        isDead = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        gameObject.layer = LayerMask.NameToLayer("Ground");
        _animator.Play("Die", 0, 0f);
        foreach (var script in scriptsToDisable)
        {
            script.enabled = false;
        }
    }
}
