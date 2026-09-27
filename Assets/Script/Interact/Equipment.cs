using UnityEngine;
using System.Collections.Generic;

public class Equipment : MonoBehaviour, IInteractable
{
    public string interactableName => "裝備\n" + equipmentName;
    public enum EquipmentType { Head, Chest, Arm, Belt, Leg, Feet }
    [System.Serializable]
    public class EquipmentData
    {
        public string equipmentName;
        public Mesh mesh;
        public Mesh prefabMesh;
        public int price;
        public float damage;
        public float shield;
        public float health;
        public float critRate;
        public Sprite itemIcon; 
    }
    [SerializeField] public List<EquipmentData> equipmentDataList;
    [SerializeField] public EquipmentType equipmentType;
    [SerializeField] public int currentEquipmentIndex = 0;
    [SerializeField] public MeshFilter prefabRenderer;

    public string equipmentName => equipmentDataList[currentEquipmentIndex].equipmentName;
    public Mesh mesh => equipmentDataList[currentEquipmentIndex].mesh;
    public Mesh prefabMesh => equipmentDataList[currentEquipmentIndex].prefabMesh;
    public int price => equipmentDataList[currentEquipmentIndex].price;
    public float damage => equipmentDataList[currentEquipmentIndex].damage;
    public float shield => equipmentDataList[currentEquipmentIndex].shield;
    public float health => equipmentDataList[currentEquipmentIndex].health;
    public float critRate => equipmentDataList[currentEquipmentIndex].critRate;
    public Sprite itemIcon => equipmentDataList[currentEquipmentIndex].itemIcon;
    private void Awake()
    {
        //item = GetComponent<ShopItem>();
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
    public void SetIndex(int index)
    {
        currentEquipmentIndex = index;
        UpdateMesh();
    }
    private void UpdateMesh()
    {
        if(prefabRenderer != null)
        {
            prefabRenderer.mesh = prefabMesh;
        }
    }
    string IInteractable.interactableName => interactableName;
}
