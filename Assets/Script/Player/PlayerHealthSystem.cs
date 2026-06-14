using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthSystem : CharacterHealthBase
{
    [SerializeField] private TMP_Text deathText;

    void Start()
    {
        deathText.gameObject.SetActive(false);
        maxHealth = 100f;
        currentHealth = maxHealth;
        healthBar.fillAmount = currentHealth / maxHealth;
        healthText.text = $"{currentHealth}/{maxHealth}";
    }
    public override void TakeDamage(string hitAnimationName, Transform attacker, float damageAmount)
    {
        if (_movement.immune)
        {
            Debug.Log("閃避");
            return;
        }else if(isDead)
        {
            return;
        }
        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0f, maxHealth);
        healthBar.fillAmount = currentHealth / maxHealth;
        healthText.text = $"{currentHealth}/{maxHealth}";
        _animator.Play(hitAnimationName,0,0f);
        SetAttacker(attacker);
        GameAssets.Instance.PlaySoundEffect(_audio, SoundAssetsType.Hit);
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    protected override void Die()
    {
        isDead = true;
        deathText.gameObject.SetActive(true);
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
