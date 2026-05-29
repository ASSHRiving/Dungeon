using UnityEngine;

public class PlayerHealthSystem : CharacterHealthBase
{
    public override void TakeDamage(string hitAnimationName, Transform attacker)
    {
        if (_movement.immune)
        {
            Debug.Log("閃避");
            return;
        }
        base.TakeDamage(hitAnimationName, attacker);
    }
}
