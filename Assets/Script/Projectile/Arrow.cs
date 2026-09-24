using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 25f;      // 箭矢飛行速度
    [SerializeField] private float damage = 20f;     // 箭矢傷害
    [SerializeField] private float lifeTime = 5f;    // 沒命中時的生存時間
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sound;
    private AttackData attackData;
    private Transform attacker;
    private Transform target;

    /// <summary>
    /// 初始化箭矢
    /// </summary>
    /// <param name="_attacker">攻擊者</param>
    /// <param name="_target">目標</param>
    /// <param name="_attackData">attackData</param>
    public void Setup(Transform _attacker, Transform _target, AttackData _attackData)
    {
        attacker = _attacker;
        target = _target;
        attackData = _attackData;
        if(audioSource != null && sound != null)
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
        // 直線向前飛行
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("trigger");
        Transform root = other.transform.root;
        // 忽略 Boss 自身的碰撞體
        if (root.CompareTag("Enemy")) return;

        // 擊中玩家
        if (root.CompareTag("Player"))
        {
            root.GetComponent<IDamageable>()?.TakeDamage(attacker, damage, attackData);
            Debug.Log($"箭矢擊中玩家！造成 {damage} 點傷害");

            // 可在此生成命中特效 (VFX)
            //Destroy(gameObject);
        }
        else if (root.CompareTag("Untagged"))
        {
            // 擊中牆壁或地面時銷毀
            Destroy(gameObject);
        }
    }
}