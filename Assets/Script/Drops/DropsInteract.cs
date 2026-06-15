using Unity.VisualScripting;
using UnityEngine;

public class DropsInteract : MonoBehaviour, IInteractable
{
    public void Interact(Transform player)
    {
        PlayerCombatSystem _combat = player.GetComponent<PlayerCombatSystem>();
        if (_combat != null)
        {
            _combat.ChangeWeapon(this.gameObject);
        }
    }
}
