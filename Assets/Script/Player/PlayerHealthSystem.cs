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
        currentHealth = maxHealth;
        healthBar.fillAmount = currentHealth / maxHealth;
        healthText.text = $"{currentHealth}/{maxHealth}";
    }
    public override void TakeDamage(Transform attacker, float damageAmount, AttackData attackData)
    {
        if (_movement.immune)
        {
            Debug.Log("閃避");
            return;
        }else if(isDead)
        {
            return;
        }
        //減傷公式
        float damage =  Mathf.Clamp(damageAmount - shield, 0f, damageAmount);

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        UpdateHealthBar(currentHealth / maxHealth);
        SetAttacker(attacker);
        GameAssets.Instance.PlaySoundEffect(_audio, SoundAssetsType.Hit);

        //受擊動畫
        if(damage/maxHealth > 0.05f)
        {
            // 扣除韌性與判斷是否被打斷/播放受擊動畫
            currentPoise -= attackData.poiseDamage;
            poiseRecoveryTimer = poiseRecoveryDelay; // 刷新恢復延遲時間
            if(currentPoise <= 0)
            {
                _animator.Play(attackData.hitAnimationName,0,0f);
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
    public void ChangeEquipment(EquipmentInteract.EquipmentType type, ShopItem item)
    {
        ShopItem.EquipmentData itemData = item.equipmentDataList[item.currentEquipmentIndex];
        Mesh newMesh = itemData.mesh;
        switch (type)
        {
            case EquipmentInteract.EquipmentType.Head:
                if (HeadSkinnedMeshRenderer != null && newMesh != null)
                {
                    HeadSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Chest:
                if (ChestSkinnedMeshRenderer != null && newMesh != null)
                {
                    ChestSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Arm:
                if (ArmSkinnedMeshRenderer != null && newMesh != null)
                {
                    ArmSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Belt:
                if (BeltSkinnedMeshRenderer != null && newMesh != null)
                {
                    BeltSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Leg:
                if (LegSkinnedMeshRenderer != null && newMesh != null)
                {
                    LegSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Feet:
                if (FeetSkinnedMeshRenderer != null && newMesh != null)
                {
                    FeetSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break; 
        }
        maxHealth += itemData.health;
        currentHealth += itemData.health;
        shield += itemData.shield;
        UpdateHealthBar(currentHealth / maxHealth);
    }
}
