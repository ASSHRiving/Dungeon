using UnityEngine;
public interface IDamageable
{
    void TakeDamage(int amount);
    void TakeDamage(string hitAnimationName);
    void TakeDamage(string hitAnimationName, Transform attacker);
}