using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthSystem : CharacterHealthBase
{
    [Header("角色部位SkinnedMeshRenderer")]
    [SerializeField] private SkinnedMeshRenderer HeadSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer ChestSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer ArmSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer BeltSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer LegSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer FeetSkinnedMeshRenderer;

    void Start()
    {
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
        UpdateHealthBar(currentHealth / maxHealth);

        _animator.Play(hitAnimationName,0,0f);
        SetAttacker(attacker);
        GameAssets.Instance.PlaySoundEffect(_audio, SoundAssetsType.Hit);
        _combat.canAttack = true;
        _combat.currentWeapon.combo = 0;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    protected override void Die()
    {
        UIEvents.PlayerDied();
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
    
    //換裝
    public void ChangeHeadMesh(Mesh newMesh)
    {
        if (HeadSkinnedMeshRenderer != null && newMesh != null)
        {
            HeadSkinnedMeshRenderer.sharedMesh = newMesh;
        }
    }
    public void ChangeChestMesh(Mesh newMesh)
    {
        if (ChestSkinnedMeshRenderer != null && newMesh != null)
        {
            ChestSkinnedMeshRenderer.sharedMesh = newMesh;
        }
    }
    public void ChangeArmMesh(Mesh newMesh)
    {
        if (ArmSkinnedMeshRenderer != null && newMesh != null)
        {
            ArmSkinnedMeshRenderer.sharedMesh = newMesh;
        }
    }
    public void ChangeBeltMesh(Mesh newMesh)
    {
        if (BeltSkinnedMeshRenderer != null && newMesh != null)
        {
            BeltSkinnedMeshRenderer.sharedMesh = newMesh;
        }
    }
    public void ChangeLegMesh(Mesh newMesh)
    {
        if (LegSkinnedMeshRenderer != null && newMesh != null)
        {
            LegSkinnedMeshRenderer.sharedMesh = newMesh;
        }
    }
    public void ChangeFeetMesh(Mesh newMesh)
    {
        if (FeetSkinnedMeshRenderer != null && newMesh != null)
        {
            FeetSkinnedMeshRenderer.sharedMesh = newMesh;
        }
    }
}
