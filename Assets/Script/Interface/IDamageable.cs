using UnityEngine;
public interface IDamageable
{
    void TakeDamage(int amount);
    void TakeDamage(Transform attacker, float damageAmount, AttackData attackData, bool isCritical = false);
}