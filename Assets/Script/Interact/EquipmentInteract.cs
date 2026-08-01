using UnityEngine;

public class EquipmentInteract : MonoBehaviour, IInteractable
{
    [SerializeField] public string interactableName = "Equipment";
    enum EquipmentType { Head, Chest, Arm, Belt, Leg, Feet }
    [SerializeField] private EquipmentType equipmentType;
    [SerializeField] private Mesh newMesh;
    [SerializeField] private int price;
    public int GetPrice()
    {
        return price;
    }
    public void Interact(Transform player)
    {
        PlayerHealthSystem health = player.GetComponentInChildren<PlayerHealthSystem>();
        if (health != null)
        {
            switch (equipmentType)
            {
                case EquipmentType.Head:
                    health.ChangeHeadMesh(newMesh);
                    break;
                case EquipmentType.Chest:
                    health.ChangeChestMesh(newMesh);
                    break;
                case EquipmentType.Arm:
                    health.ChangeArmMesh(newMesh);
                    break;
                case EquipmentType.Belt:
                    health.ChangeBeltMesh(newMesh);
                    break;
                case EquipmentType.Leg:
                    health.ChangeLegMesh(newMesh);
                    break;
                case EquipmentType.Feet:
                    health.ChangeFeetMesh(newMesh);
                    break;
            }
        }
        Destroy(this.gameObject);
    }
    string IInteractable.interactableName => interactableName;
}
