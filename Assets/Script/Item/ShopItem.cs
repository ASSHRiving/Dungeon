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
        public int damage;
        public int shield;
        public int health;
    }
    [SerializeField] public List<EquipmentData> equipmentDataList;
    [SerializeField] public int currentEquipmentIndex = 0;
}
