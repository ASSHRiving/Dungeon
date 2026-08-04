using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnemyHealthSystem : CharacterHealthBase
{
    [SerializeField] private GameObject lootPrefab;

    void Start()
    {
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
        
        // 在怪物的位置爆出 5 顆金幣
        for (int i = 0; i < 5; i++)
        {
            // 隨機噴散的位移
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
            
            // 🎯 直接跟你的 Singleton 拿金幣！
            GameObject coin = ObjectPoolManager.Instance.GetCoin(transform.position + randomOffset, Quaternion.identity);

            // (可選) 給金幣一個微小的向外爆發力
            if (coin != null && coin.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                Vector3 force = new Vector3(Random.Range(-2f, 2f), 4f, Random.Range(-2f, 2f));
                rb.AddForce(force, ForceMode.Impulse);
            }
        }
        
        healthUI.gameObject.SetActive(false);
        healthText.gameObject.SetActive(false);
        foreach (var script in scriptsToDisable)
        {
            script.enabled = false;
        }
        Destroy(gameObject, 3.0f);
    }
}
