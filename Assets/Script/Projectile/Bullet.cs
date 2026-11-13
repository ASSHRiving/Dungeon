using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    public float lifeTime = 3f;
    public int damage = 10;

    private Vector3 direction;

    // 初始化方向（從外部設定）
    public void Init(Vector3 dir)
    {
        direction = dir.normalized;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime); // 避免子彈永遠存在
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit: " + other.name);

        IDamageable dmg = other.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
        
        }
        Destroy(gameObject);
    }
}