using System.Collections.Generic;
using UnityEngine;

public class ShopItem : MonoBehaviour
{
    [System.Serializable]
    public class EquipmentData
    {
        public string equipmentName;
        public Mesh mesh;
        public int price;
        public float damage;
        public float shield;
        public float health;
        public float critRate;
    }
    [SerializeField] public List<EquipmentData> equipmentDataList;
    [SerializeField] public int currentEquipmentIndex = 0;
    
    public string equipmentName => equipmentDataList[currentEquipmentIndex].equipmentName;
    public Mesh mesh => equipmentDataList[currentEquipmentIndex].mesh;
    public int price => equipmentDataList[currentEquipmentIndex].price;
    public float damage => equipmentDataList[currentEquipmentIndex].damage;
    public float shield => equipmentDataList[currentEquipmentIndex].shield;
    public float health => equipmentDataList[currentEquipmentIndex].health;
    public float critRate => equipmentDataList[currentEquipmentIndex].critRate;
}
