using UnityEngine;

public class EquipmentInteract : MonoBehaviour, IInteractable
{
    [SerializeField] public string interactableName = "Equipment";
    public enum EquipmentType { Head, Chest, Arm, Belt, Leg, Feet }
    [SerializeField] public EquipmentType equipmentType;
    private ShopItem item;
    private void Awake()
    {
        item = GetComponent<ShopItem>();
    }
    public void Interact(Transform player)
    {
        CharacterEquipment characterEquipment = player.GetComponentInChildren<CharacterEquipment>();
        if (characterEquipment != null)
        {
            //Debug.Log($"Equipping {item.equipmentName} to {equipmentType}");
            characterEquipment.EquipItem(this.gameObject);
        }
        else
        {
            //Debug.LogWarning("CharacterEquipment component not found on player.");
        }
        
    }
    string IInteractable.interactableName => interactableName;
}
