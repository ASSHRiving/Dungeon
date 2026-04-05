using UnityEngine;

public class EnenyHit : MonoBehaviour, IDamageable
{
    public int health = 100;
    Animator animator;
    private bool isDead = false;

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log("Enemy took damage, current health: " + health);
        if (health <= 0)
        {
            Die();
        }
        
    }

    void Die()
    {
        if (isDead) return; // Prevent multiple death triggers
        isDead = true;
        animator.SetBool("IsDead", true);
        // 停止行為（很重要）
        //GetComponent<UnityEngine.AI.NavMeshAgent>()?.enabled = false;
        GetComponent<Collider>().enabled = false;

        // 如果有攻擊 / 移動腳本
        //this.enabled = false;
        Debug.Log("Enemy died!");
        Destroy(gameObject, 2f);
    }
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        
    }
}
