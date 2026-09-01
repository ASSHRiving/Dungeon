using UnityEngine;
using UnityEngine.AI;
using Unity.Cinemachine;

public class EnemyHealthSystem : CharacterHealthBase
{
    [SerializeField] private GameObject lootPrefab;
    [SerializeField] private int gold = 5;
    private NavMeshAgent _navMeshAgent;
    private CinemachineImpulseSource impulseSource;
    public System.Action OnDeath;           //死亡廣播


    protected override void Awake()
    {
        base.Awake();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }
    void Start()
    {
        currentHealth = maxHealth;
        healthBar.fillAmount = currentHealth / maxHealth;
        healthText.text = $"{currentHealth}/{maxHealth}";
        _navMeshAgent = GetComponentInParent<NavMeshAgent>();
    }
    public override void TakeDamage(Transform attacker, float damageAmount, AttackData attackData)
    {
        if(isDead)
        {
            return;
        }
        //減傷公式
        float damage =  Mathf.Clamp(damageAmount - shield, 0f, damageAmount);

        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0f, maxHealth);
        UpdateHealthBar(currentHealth / maxHealth);
        SetAttacker(attacker);
        GameAssets.Instance.PlaySoundEffect(_audio, SoundAssetsType.Hit);
        if(impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
        GameAssets.Instance.DoHitstop(0.05f, 0.05f); // 觸發 Hitstop (頓幀)
        Debug.Log($"敵人受到{damageAmount}點傷害，剩餘血量：{currentHealth}");

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
        OnDeath?.Invoke();
        isDead = true;
        _navMeshAgent.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Ground");
        _animator.Play("Die", 0, 0f);
        
        if (lootPrefab != null)
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }
        // 在怪物的位置爆出 5 顆金幣
        for (int i = 0; i < gold; i++)
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
