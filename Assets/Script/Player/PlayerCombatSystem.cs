using UnityEngine;
using CombatBase;

public class PlayerCombatSystem : CharacterCombatBase
{
    [SerializeField] private Transform currentTarget;
    [SerializeField] private Weapon currentWeapon;

    [SerializeField, Header("檢測敵人")] private Transform enemyDetectionCenter;
    [SerializeField] private float enemyDetectionRadius;

    private Collider[] detectedEnemies = new Collider[1];

    private void Update()
    {
        PlayerAttackAction();
    }

    private void PlayerAttackAction()
    {
        if (_inputSystem.playerLAtk && canAttack)
        {
            currentWeapon.Attack(_animator);
            canAttack = false;
        }
    }
}
