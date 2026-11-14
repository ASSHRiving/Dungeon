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

    public System.Action OnDeath; // 死亡廣播

    protected override void Awake()
    {
        base.Awake();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    protected override void Start()
    {
        base.Start();

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
    }

    /// <summary>
    /// 依 EnemyDataSO 同步 Boss 身分與顯示名稱 (供 EnemyFactory 生成時覆寫，避免 Prefab 設定過時)
    /// </summary>
    public void ConfigureBoss(bool bossFlag, string displayName)
    {
        isBoss = bossFlag;
        if (!string.IsNullOrEmpty(displayName))
        {
            bossName = displayName;
        }
    }

    /// <summary>
    /// 動態設定怪物掉落金幣 (供 EnemyFactory 於生成時注入關卡難度加成或菁英怪加成)
    /// </summary>
    public void SetGoldDrop(int newGold)
    {
        gold = newGold;
    }

    public int GetGoldDrop() => gold;

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
            else
            {
                UIEvents.BossHealthChanged(currentHealth, maxHealth);
            }
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

        // 減傷公式：優先使用 CharacterStats 的 Shield，若無則 fallback 原有 shield 欄位
        float effectiveShield = stats != null ? stats.Shield : shield;
        float armorDR = effectiveShield / (effectiveShield + 100f);
        armorDR = Mathf.Clamp(armorDR, 0f, 0.85f);
        int finaldamage = Mathf.RoundToInt(damageAmount * (1f - armorDR));

        ObjectPoolManager.Instance.SpawnDamageText(transform.position, finaldamage, isCritical);

        // 透過 CharacterStats 統一扣血；若無 stats 則手動扣除
        if (stats != null)
        {
            stats.TakeDamage(finaldamage);
            currentHealth = stats.CurrentHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth - finaldamage, 0f, maxHealth);
            UpdateHealthBar(maxHealth > 0 ? currentHealth / maxHealth : 0f);
        }

        SetAttacker(attacker);

        // 播放受擊音效、觸發攝影機震動、頓幀
        if (audioSource != null)
        {
            GameAssets.Instance.PlaySoundEffect(audioSource, attackData.hitSound);
        }
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulseWithForce(attackData.shakeForce);
        }
        GameAssets.Instance.DoHitstop(attackData.hitStopTime, 0.03f); // 觸發 Hitstop (頓幀)
        Debug.Log($"敵人受到 {finaldamage} 點傷害，剩餘血量：{currentHealth}/{maxHealth}");

        // 扣除韌性與判斷是否被打斷/播放受擊動畫
        float effectiveMaxHealth = stats != null ? stats.MaxHealth : maxHealth;
        if (effectiveMaxHealth > 0 && finaldamage / effectiveMaxHealth > 0.05f)
        {
            currentPoise -= attackData.poiseDamage;
            poiseRecoveryTimer = poiseRecoveryDelay; // 刷新恢復延遲時間
            if (currentPoise <= 0) // 如果韌性破除，則打斷當前動作並播放受擊動畫並重置韌性
            {
                if (animator != null) animator.CrossFade(attackData.hitAnimationName, 0.1f);
                if (combat != null)
                {
                    combat.canAttack = true;
                    if (combat.currentWeapon != null) combat.currentWeapon.combo = 0;
                }

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

        // 擊殺回血 (Life on Kill) 觸發
        if (attacker != null)
        {
            var attackerStats = attacker.GetComponentInParent<CharacterStats>() ?? attacker.GetComponentInChildren<CharacterStats>();
            if (attackerStats != null && attackerStats.LifeOnKill > 0)
            {
                var attackerHealth = attacker.GetComponentInParent<CharacterHealthBase>() ?? attacker.GetComponentInChildren<CharacterHealthBase>();
                if (attackerHealth != null)
                {
                    attackerHealth.Recover(attackerStats.LifeOnKill);
                    Debug.Log($"[擊殺回血] 觸發！{attacker.name} 回復 {attackerStats.LifeOnKill} 點生命");
                }
            }
        }

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
        if (animator != null)
        {
            animator.CrossFade("Die", 0.1f);
        }

        if (lootPrefab != null)
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }

        // 在怪物的位置爆出金幣
        for (int i = 0; i < gold; i++)
        {
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.5f, UnityEngine.Random.Range(-0.5f, 0.5f));
            GameObject coin = ObjectPoolManager.Instance.GetCoin(transform.position + randomOffset, Quaternion.identity);

            if (coin != null && coin.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                Vector3 force = new Vector3(UnityEngine.Random.Range(-2f, 2f), 4f, UnityEngine.Random.Range(-2f, 2f));
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
