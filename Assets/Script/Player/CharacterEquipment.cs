using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
public class CharacterEquipment : MonoBehaviour
{
    private Dictionary<Equipment.EquipmentType, GameObject> equippedItems = new Dictionary<Equipment.EquipmentType, GameObject>();
    private CharacterCombatBase combat;
    private CharacterHealthBase health;
    [SerializeField]private Transform equipmentHolder;

    [Header("角色部位SkinnedMeshRenderer")]
    [SerializeField] private SkinnedMeshRenderer HeadSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer ChestSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer ArmSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer BeltSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer LegSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer FeetSkinnedMeshRenderer;

    [SerializeField] private GameObject hair;         //頭髮在戴頭盔時不顯示
    [SerializeField] private GameObject ear;
    private void Awake()
    {
        combat = GetComponentInChildren<CharacterCombatBase>();
        health = GetComponentInChildren<CharacterHealthBase>();
        // 初始化字典，確保每個裝備類型都有一個對應的值
        foreach (Equipment.EquipmentType type in System.Enum.GetValues(typeof(Equipment.EquipmentType)))
        {
            equippedItems[type] = null;
        }
    }
    public void EquipItem(GameObject item)
    {
        if (item == null) return;
        //Debug.Log($"Equipping item: {item.name}");

        Equipment.EquipmentType type = item.GetComponent<Equipment>().equipmentType;
        UpdateRenderer(type, item.GetComponent<Equipment>().mesh);
        // 如果已經裝備了相同類型的裝備，先卸下它
        if (equippedItems[type] != null)
        {
            UnequipItem(type);
        }

        // 裝備新的物品
        equippedItems[type] = item;
        EquipStatsUpdate(item);
        item.transform.SetParent(equipmentHolder);
        item.transform.localPosition = Vector3.zero;
        item.gameObject.SetActive(false);

        if(type == Equipment.EquipmentType.Head)    //頭髮在戴頭盔時不顯示
        {
            hair.SetActive(false);
            ear.SetActive(false);
        }
    }

    public void UnequipItem(Equipment.EquipmentType type)
    {
        GameObject item = equippedItems[type];
        if(item != null)
        {
            UnequipStatsUpdate(item);
            item.GetComponent<Rigidbody>().AddForce(Vector3.forward * 5f, ForceMode.Impulse); // 給予一個向上的力，讓物品掉落
            item.transform.SetParent(null);
            item.gameObject.SetActive(true);
            SceneManager.MoveGameObjectToScene(item.gameObject, SceneManager.GetActiveScene());   //移除Dont Destory
            equippedItems[type] = null;
        }
    }
    /// <summary>
    /// 取得特定部位目前穿戴的裝備物件
    /// </summary>
    public GameObject GetEquippedItem(Equipment.EquipmentType type)
    {
        if (equippedItems.ContainsKey(type))
        {
            return equippedItems[type];
        }
        return null;
    }
    private void UnequipStatsUpdate(GameObject item)
    {
        Equipment equipment = item.GetComponent<Equipment>();
        if (equipment != null)
        {
            combat.damage -= equipment.damage;
            health.shield -= equipment.shield;
            health.ModifyMaxHealth(health.maxHealth - equipment.health);
            combat.critRate -= equipment.critRate;
        }
    }
    private void EquipStatsUpdate(GameObject item)
    {
        Equipment equipment = item.GetComponent<Equipment>();
        if (equipment != null)
        {
            combat.damage += equipment.damage;
            health.shield += equipment.shield;
            health.ModifyMaxHealth(health.maxHealth + equipment.health);
            combat.critRate += equipment.critRate;
        }
    }
    private void UpdateRenderer(Equipment.EquipmentType type, Mesh newMesh)
    {
        switch (type)
        {
            case Equipment.EquipmentType.Head:
                if (HeadSkinnedMeshRenderer != null && newMesh != null)
                {
                    HeadSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case Equipment.EquipmentType.Chest:
                if (ChestSkinnedMeshRenderer != null && newMesh != null)
                {
                    ChestSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case Equipment.EquipmentType.Arm:
                if (ArmSkinnedMeshRenderer != null && newMesh != null)
                {
                    ArmSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case Equipment.EquipmentType.Belt:
                if (BeltSkinnedMeshRenderer != null && newMesh != null)
                {
                    BeltSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case Equipment.EquipmentType.Leg:
                if (LegSkinnedMeshRenderer != null && newMesh != null)
                {
                    LegSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case Equipment.EquipmentType.Feet:
                if (FeetSkinnedMeshRenderer != null && newMesh != null)
                {
                    FeetSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break; 
        }
    }
}
