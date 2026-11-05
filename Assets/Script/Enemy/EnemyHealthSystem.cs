using UnityEngine;
using UnityEngine.AI;

public class EnemyHealthSystem : CharacterHealthBase
{
    [Header("掉落設定")]
    [SerializeField] private GameObject lootPrefab;
    [SerializeField] private int gold = 5;

    [Header("Boss 專屬血條設定")]
    [SerializeField] private bool isBoss = false;
    [SerializeField] private string bossName = "Boss";
    [SerializeField] private bool showBossBarOnStart = true; // 生成時立即顯示頂部血條（若為 false 則在第一次受擊或手動呼叫時顯示）

    private NavMeshAgent navMeshAgent;
    private bool bossBarActivated = false;

    public System.Action OnDeath;           // 死亡廣播

    protected override void Awake()
    {
        base.Awake();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    protected override void Start()
    {
        currentHealth = maxHealth;

        if (isBoss)
        { 
            // Boss 不顯示頭頂的小怪血條與文字
            if (healthUI != null) healthUI.gameObject.SetActive(false);
            if (healthText != null) healthText.gameObject.SetActive(false);

            if (showBossBarOnStart)
            {
                ActivateBossBar();
            }
        }
        else
        {
            base.Start();
        }
    }

    /// <summary>
    /// 觸發螢幕頂部 Boss 獨立血條登場（也可由 Boss 房門觸發器手動呼叫）
    /// </summary>
    public void ActivateBossBar()
    {
        if (!isBoss || bossBarActivated) return;
        bossBarActivated = true;
        UIEvents.BossFightStart(bossName, maxHealth);
    }

    protected override void UpdateHealthBar(float targetFill)
    {
        if (isBoss)
        {
            // 若進房還沒顯示血條就被玩家砍中，自動喚醒血條
            if (!bossBarActivated)
            {
                ActivateBossBar();
            }
            UIEvents.BossHealthChanged(currentHealth, maxHealth);
            return;
        }

        // 一般小怪維持更新頭頂血條
        base.UpdateHealthBar(targetFill);
    }

    public override void TakeDamage(Transform attacker, float damageAmount, AttackData attackData, bool isCritical = false)
    {
        if (isDead)
        {
            return;
        }

        // 減傷公式
        float armorDR = shield / (shield + 100f);
        armorDR = Mathf.Clamp(armorDR, 0f, 0.85f);
        int finaldamage = Mathf.RoundToInt(damageAmount * (1f - armorDR));

        ObjectPoolManager.Instance.SpawnDamageText(transform.position, finaldamage, isCritical);

        currentHealth = Mathf.Clamp(currentHealth - finaldamage, 0f, maxHealth);
        UpdateHealthBar(currentHealth / maxHealth);
        SetAttacker(attacker);

        // 播放受擊音效、觸發攝影機震動、頓幀
        GameAssets.Instance.PlaySoundEffect(audioSource, attackData.hitSound);
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulseWithForce(attackData.shakeForce);
        }
        GameAssets.Instance.DoHitstop(attackData.hitStopTime, 0.03f); // 觸發 Hitstop (頓幀)
        Debug.Log($"敵人受到{damageAmount}點傷害，剩餘血量：{currentHealth}");

        // 扣除韌性與判斷是否被打斷/播放受擊動畫
        if (finaldamage / maxHealth > 0.05f) // 如果這次攻擊造成的傷害超過最大血量的5%，則扣除韌性
        {
            currentPoise -= attackData.poiseDamage;
            poiseRecoveryTimer = poiseRecoveryDelay; // 刷新恢復延遲時間
            if (currentPoise <= 0) // 如果韌性破除，則打斷當前動作並播放受擊動畫並重置韌性
            {
                animator.CrossFade(attackData.hitAnimationName, 0.1f);
                combat.canAttack = true;
                combat.currentWeapon.combo = 0;

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

        // 若為 Boss，廣播擊殺退場事件讓頂部大血條淡出
        if (isBoss)
        {
            UIEvents.BossDefeated();
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }
        gameObject.layer = LayerMask.NameToLayer("UnInteractable");
        animator.CrossFade("Die", 0.1f);

        if (lootPrefab != null)
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }

        // 在怪物的位置爆出金幣
        for (int i = 0; i < gold; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
            GameObject coin = ObjectPoolManager.Instance.GetCoin(transform.position + randomOffset, Quaternion.identity);

            if (coin != null && coin.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                Vector3 force = new Vector3(Random.Range(-2f, 2f), 4f, Random.Range(-2f, 2f));
                rb.AddForce(force, ForceMode.Impulse);
            }
        }

        if (healthUI != null) healthUI.gameObject.SetActive(false);
        if (healthText != null) healthText.gameObject.SetActive(false);

        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }
        Destroy(gameObject, 3.0f);
    }
}