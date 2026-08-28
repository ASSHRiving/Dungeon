using UnityEngine;
public interface IDamageable
{
    void TakeDamage(int amount);
    void TakeDamage(string hitAnimationName, Transform attacker, float damageAmount, float poiseDamage = 10f);
}