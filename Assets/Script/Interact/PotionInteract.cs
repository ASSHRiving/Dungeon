using UnityEngine;

public class PotionInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName;
    [SerializeField] private float heal;
    public string interactableName => itemName;

    public void Interact(Transform player)
    {
        CharacterHealthBase health = player.GetComponent<CharacterHealthBase>();
        if(health != null)
        {
            health.Recover(heal);
            Destroy(gameObject);
        }
    }
}
