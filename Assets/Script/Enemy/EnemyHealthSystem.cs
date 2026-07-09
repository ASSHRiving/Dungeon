using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnemyHealthSystem : CharacterHealthBase
{
    [SerializeField] private GameObject lootPrefab;
    void Start()
    {
        maxHealth = 100f;
        currentHealth = maxHealth;
        healthBar.fillAmount = currentHealth / maxHealth;
        healthText.text = $"{currentHealth}/{maxHealth}";
    }
    public override void TakeDamage(string hitAnimationName, Transform attacker, float damageAmount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0f, maxHealth);
        UpdateHealthBar(currentHealth / maxHealth);
    
        _animator.Play(hitAnimationName,0,0f);
        SetAttacker(attacker);
        GameAssets.Instance.PlaySoundEffect(_audio, SoundAssetsType.Hit);
        Debug.Log($"敵人受到{damageAmount}點傷害，剩餘血量：{currentHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    protected override void Die()
    {
        isDead = true;
        gameObject.layer = LayerMask.NameToLayer("Ground");
        _animator.Play("Die", 0, 0f);
        if (lootPrefab != null)
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }
        healthBar.gameObject.SetActive(false);
        healthText.gameObject.SetActive(false);
        foreach (var script in scriptsToDisable)
        {
            script.enabled = false;
        }
        Destroy(gameObject, 3.0f);
    }
}
