using UnityEngine;

public class EnenyHit : MonoBehaviour, IDamageable
{
    public int health = 100;

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log("Enemy took damage, current health: " + health);
        /*
        if (health <= 0)
        {
            Die();
        }
        */
    }

    void Die()
    {
        Debug.Log("Enemy died!");
        Destroy(gameObject);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
