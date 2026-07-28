using Unity.VisualScripting;
using UnityEngine;

public class DropsInteract : MonoBehaviour, IInteractable
{
    [SerializeField] public string interactableName = "Drops";

    string IInteractable.interactableName => interactableName;

    public void Interact(Transform player)
    {
        PlayerCombatSystem _combat = player.GetComponentInChildren<PlayerCombatSystem>();
        if (_combat != null)
        {
            _combat.ChangeWeapon(this.gameObject);
        }
    }
}
