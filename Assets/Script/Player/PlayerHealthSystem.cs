using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthSystem : CharacterHealthBase
{
    [SerializeField] private AudioClip missSound;
    [SerializeField] private AudioClip deathSound;

    public override void TakeDamage(Transform attacker, float damageAmount, AttackData attackData, bool isCritical = false)
    {
        if (movement != null && movement.immune)
        {
            Debug.Log("閃避");
            GameAssets.Instance.DoHitstop(0.25f, 0.1f);
            if (audioSource != null && missSound != null)
            {
                audioSource.PlayOneShot(missSound, 3f);
            }
            return;
        }
        else if (isDead)
        {
            return;
        }

        // 減傷公式：優先使用 CharacterStats 的 Shield，若無則 fallback 原有 shield 欄位
        float effectiveShield = stats != null ? stats.Shield : shield;
        float armorDR = effectiveShield / (effectiveShield + 100f);
        armorDR = Mathf.Clamp(armorDR, 0f, 0.85f);
        int finalDamage = Mathf.RoundToInt(damageAmount * (1f - armorDR));

        // 透過 CharacterStats 統一扣血；若無 stats 則手動扣除
        if (stats != null)
        {
            stats.TakeDamage(finalDamage);
            currentHealth = stats.CurrentHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth - finalDamage, 0f, maxHealth);
            UpdateHealthBar(maxHealth > 0 ? currentHealth / maxHealth : 0f);
        }

        // 受擊反饋
        if (audioSource != null)
        {
            GameAssets.Instance.PlaySoundEffect(audioSource, attackData.hitSound);
        }
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulseWithForce(attackData.shakeForce);
        }
        GameAssets.Instance.DoHitstop(attackData.hitStopTime, 0.03f); // 觸發 Hitstop (頓幀)
        SetAttacker(attacker);

        // 受擊動畫與韌性判斷
        float effectiveMaxHealth = stats != null ? stats.MaxHealth : maxHealth;
        if (effectiveMaxHealth > 0 && finalDamage / effectiveMaxHealth > 0.05f)
        {
            currentPoise -= attackData.poiseDamage;
            poiseRecoveryTimer = poiseRecoveryDelay; // 刷新恢復延遲時間
            if (currentPoise <= 0)
            {
                if (animator != null) animator.CrossFade(attackData.hitAnimationName, 0.1f);
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddDeath();
        }
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound, 2f);
        }
        isDead = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        if (animator != null)
        {
            animator.Play("Die", 0, 0f);
        }
        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }
    }
}
