using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 25f;      // 箭矢飛行速度
    [SerializeField] private float damage = 20f;     // 箭矢傷害
    [SerializeField] private float lifeTime = 5f;    // 沒命中時的生存時間
    private AttackData attackData;

    private Transform target;

    public void Setup(Transform targetTransform, AttackData _attackData)
    {
        target = targetTransform;
        attackData = _attackData;

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
        // 直線向前飛行
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 忽略 Boss 自身的碰撞體
        if (other.CompareTag("Enemy") || other.CompareTag("Boss")) return;

        // 擊中玩家
        if (other.CompareTag("Player"))
        {
            // 假設玩家有 IDamageable 或 PlayerCombatSystem
            // other.GetComponent<IDamageable>()?.TakeDamage(damage);
            Debug.Log($"箭矢擊中玩家！造成 {damage} 點傷害");

            // 可在此生成命中特效 (VFX)
            Destroy(gameObject);
        }
        else if (other.CompareTag("Environment") || other.CompareTag("Untagged"))
        {
            // 擊中牆壁或地面時銷毀
            Destroy(gameObject);
        }
    }
}