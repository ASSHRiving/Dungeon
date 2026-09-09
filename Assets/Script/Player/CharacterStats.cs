using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("基礎屬性")]
    public float maxHealth => health.maxHealth;
    public float shield => health.shield;
    public float damage => combat.damage;
    public float critRate => combat.critRate;
    [SerializeField] private CharacterCombatBase combat;
    [SerializeField] private CharacterHealthBase health;

}
