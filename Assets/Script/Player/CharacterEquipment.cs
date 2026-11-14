using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class CharacterEquipment : MonoBehaviour
{
    private Dictionary<Equipment.EquipmentType, GameObject> equippedItems = new Dictionary<Equipment.EquipmentType, GameObject>();
    private CharacterStats stats;

    [SerializeField] private Transform equipmentHolder;

    [Header("角色部位SkinnedMeshRenderer")]
    [SerializeField] private SkinnedMeshRenderer HeadSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer ChestSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer ArmSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer BeltSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer LegSkinnedMeshRenderer;
    [SerializeField] private SkinnedMeshRenderer FeetSkinnedMeshRenderer;

    [SerializeField] private GameObject hair;         // 頭髮在戴頭盔時不顯示
    [SerializeField] private GameObject ear;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>() ?? GetComponentInParent<CharacterStats>() ?? GetComponentInChildren<CharacterStats>();

        // 初始化字典，確保每個裝備類型都有一個對應的值
        foreach (Equipment.EquipmentType type in System.Enum.GetValues(typeof(Equipment.EquipmentType)))
        {
            equippedItems[type] = null;
        }
    }

    public void EquipItem(GameObject item)
    {
        if (item == null) return;

        Equipment equipment = item.GetComponent<Equipment>();
        if (equipment == null) return;

        Equipment.EquipmentType type = equipment.equipmentType;
        UpdateRenderer(type, equipment.mesh);

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

        if (type == Equipment.EquipmentType.Head)    // 頭髮在戴頭盔時不顯示
        {
            if (hair != null) hair.SetActive(false);
            if (ear != null) ear.SetActive(false);
        }
    }

    public void UnequipItem(Equipment.EquipmentType type)
    {
        GameObject item = equippedItems[type];
        if (item != null)
        {
            UnequipStatsUpdate(item);
            if (item.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddForce(Vector3.forward * 10f, ForceMode.Impulse); // 給予一個力讓物品掉落
            }
            item.transform.SetParent(null);
            item.gameObject.SetActive(true);
            SceneManager.MoveGameObjectToScene(item.gameObject, SceneManager.GetActiveScene()); // 移除 DontDestroy
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

    /// <summary>
    /// 取得特定部位目前穿戴的裝備 SO 資料
    /// </summary>
    public EquipmentDataSO GetEquippedData(Equipment.EquipmentType type)
    {
        GameObject go = GetEquippedItem(type);
        if (go != null && go.TryGetComponent<Equipment>(out var eq))
        {
            return eq.EquipmentData;
        }
        return null;
    }

    private void EquipStatsUpdate(GameObject item)
    {
        if (stats == null) return;

        Equipment equipment = item.GetComponent<Equipment>();
        if (equipment == null || equipment.EquipmentData == null || equipment.EquipmentData.statBonuses == null) return;

        foreach (var bonus in equipment.EquipmentData.statBonuses)
        {
            if (!Mathf.Approximately(bonus.value, 0f))
            {
                stats.AddModifier(new StatModifier(bonus.statType, bonus.modType, bonus.value, item));
            }
        }
    }

    private void UnequipStatsUpdate(GameObject item)
    {
        if (item == null || stats == null) return;

        // 直接以該裝備實體為 source，一鍵清空其所有加成，徹底消除數值殘留或浮點數漂移！
        stats.RemoveAllModifiersFromSource(item);
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
