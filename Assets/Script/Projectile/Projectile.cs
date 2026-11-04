using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] protected float speed = 25f;      // 飛行速度
    [SerializeField] protected float damage = 20f;     // 傷害
    [SerializeField] protected float lifeTime = 5f;    // 沒命中時的生存時間
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioClip sound;
    protected AttackData attackData;
    protected Transform attacker;
    protected Transform target;

    /// <summary>
    /// 初始化投射物 (改為 virtual 讓特殊彈道子類別可以擴充)
    /// </summary>
    public virtual void Setup(Transform _attacker, Transform _target, AttackData _attackData)
    {
        attacker = _attacker;
        target = _target;
        attackData = _attackData;
        if (audioSource != null && sound != null)
        {
            audioSource.PlayOneShot(sound, 2f);
        }

        // 面向目標方向
        if (target != null)
        {
            Vector3 targetPosition = target.position + Vector3.up * 1.2f; // 朝向胸口位置
            transform.LookAt(targetPosition);
        }

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        UpdateProjectile();
    }

    // 射彈飛行
    protected virtual void UpdateProjectile()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        Transform root = other.transform.root;
        // 忽略敵方自身的碰撞體
        if (root.CompareTag("Enemy")) return;
        // 忽略其他非實體的 Trigger 區域 (避免飛過偵測區就自爆)
        if (other.isTrigger && !root.CompareTag("Player")) return;

        // 擊中玩家
        if (root.CompareTag("Player"))
        {
            root.GetComponent<IDamageable>()?.TakeDamage(attacker, damage, attackData);
            Debug.Log($"擊中玩家！造成 {damage} 點傷害");

            OnProjectileHit(transform.position - transform.up * .6f, -transform.forward);
        }
        else if (root.CompareTag("Untagged"))
        {
            // 擊中牆壁或地面時銷毀
            OnProjectileHit(transform.position - transform.up * .6f, -transform.forward);
        }
    }

    /// <summary>
    /// 命中時的特效與銷毀處理 (子類別可 override 來生成爆炸特效與保留拖尾)
    /// </summary>
    protected virtual void OnProjectileHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        Destroy(gameObject);
    }
}