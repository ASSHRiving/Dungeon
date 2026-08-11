using UnityEngine;

public class EquipmentInteract : MonoBehaviour, IInteractable
{
    [SerializeField] public string interactableName = "Equipment";
    public enum EquipmentType { Head, Chest, Arm, Belt, Leg, Feet }
    [SerializeField] private EquipmentType equipmentType;
    [SerializeField] private Mesh newMesh;
    [SerializeField] private int price;
    private ShopItem item;
    private void Awake()
    {
        item = GetComponent<ShopItem>();
    }
    public int GetPrice()
    {
        return price;
    }
    public void Interact(Transform player)
    {
        PlayerHealthSystem health = player.GetComponentInChildren<PlayerHealthSystem>();
        if (health != null)
        {
            health.ChangeEquipment(newMesh, equipmentType, item);
        }
        Destroy(this.gameObject);
    }
    string IInteractable.interactableName => interactableName;
}
