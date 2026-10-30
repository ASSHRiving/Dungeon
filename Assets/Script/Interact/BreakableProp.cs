using UnityEngine;

public class BreakableProp : MonoBehaviour, IDamageable
{
    [Header("替換與特效設定")]
    [SerializeField] private GameObject brokenPrefab;
    [SerializeField] private GameObject breakVFX;
    [SerializeField] private AudioClip breakSFX;

    [Header("物理爆破參數")]
    [SerializeField] private float explosionForce = 300f;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float shardLifeTime = 3.5f;

    [Header("掉落設定")]
    [SerializeField] private int dropCoinCount = 2;

    private bool isBroken = false;

    private void OnEnable()
    {
        isBroken = false;
    }

    public void TakeDamage(int amount, Vector3 hitPoint)
    {
        // 確保單幀內被多段攻擊掃到時只會觸發一次
        if (isBroken) return;
        isBroken = true;
    }
    public void TakeDamage(int amount)
    {
        
    }
    public void TakeDamage(Transform attacker, float damageAmount, AttackData attackData, bool isCritical = false)
    {
        if (isBroken) return;
        isBroken = true;
        Vector3 hitPoint = transform.position + new Vector3(0, 0.5f, 0);

        ObjectPoolManager.Instance.SpawnDebris(
            brokenPrefab,
            transform.position,
            transform.rotation,
            hitPoint,
            explosionForce,
            explosionRadius,
            shardLifeTime
        );

        for (int i = 0; i < dropCoinCount; i++)
        {
            ObjectPoolManager.Instance.GetCoin(transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    private void Break(Vector3 hitPoint)
    {
        // 1. 生成碎片預製體並對齊原木箱的位置與旋轉
        GameObject fracturedObj = Instantiate(brokenPrefab, transform.position, transform.rotation);

        // 2. 對所有碎片施加從受擊點向外擴散的物理推力
        Rigidbody[] shards = fracturedObj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody shard in shards)
        {
            shard.AddExplosionForce(explosionForce, hitPoint, explosionRadius);
        }

        // 3. 播放粒子特效與音效
        if (breakVFX != null)
        {
            Instantiate(breakVFX, transform.position, Quaternion.identity);
        }
        if (breakSFX != null)
        {
            AudioSource.PlayClipAtPoint(breakSFX, transform.position);
        }

        // 4. 定期清理碎片以釋放效能，並銷毀原本的完整木箱
        Destroy(fracturedObj, shardLifeTime);
        
    }
}
