using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class CharacterEquipment : MonoBehaviour
{
    private Dictionary<EquipmentInteract.EquipmentType, GameObject> equippedItems = new Dictionary<EquipmentInteract.EquipmentType, GameObject>();
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
    private void Awake()
    {
        combat = GetComponentInChildren<CharacterCombatBase>();
        health = GetComponentInChildren<CharacterHealthBase>();
        // 初始化字典，確保每個裝備類型都有一個對應的值
        foreach (EquipmentInteract.EquipmentType type in System.Enum.GetValues(typeof(EquipmentInteract.EquipmentType)))
        {
            equippedItems[type] = null;
        }
    }
    public void EquipItem(GameObject item)
    {
        if (item == null) return;
        //Debug.Log($"Equipping item: {item.name}");

        EquipmentInteract.EquipmentType type = item.GetComponent<EquipmentInteract>().equipmentType;
        UpdateRenderer(type, item.GetComponent<ShopItem>().mesh);
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
    }

    public void UnequipItem(EquipmentInteract.EquipmentType type)
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
    private void UnequipStatsUpdate(GameObject item)
    {
        ShopItem shopItem = item.GetComponent<ShopItem>();
        if (shopItem != null)
        {
            combat.damage -= shopItem.damage;
            health.shield -= shopItem.shield;
            health.ModifyMaxHealth(health.maxHealth - shopItem.health);
            combat.critRate -= shopItem.critRate;
        }
    }
    private void EquipStatsUpdate(GameObject item)
    {
        ShopItem shopItem = item.GetComponent<ShopItem>();
        if (shopItem != null)
        {
            combat.damage += shopItem.damage;
            health.shield += shopItem.shield;
            health.ModifyMaxHealth(health.maxHealth + shopItem.health);
            combat.critRate += shopItem.critRate;
        }
    }
    private void UpdateRenderer(EquipmentInteract.EquipmentType type, Mesh newMesh)
    {
        switch (type)
        {
            case EquipmentInteract.EquipmentType.Head:
                if (HeadSkinnedMeshRenderer != null && newMesh != null)
                {
                    HeadSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Chest:
                if (ChestSkinnedMeshRenderer != null && newMesh != null)
                {
                    ChestSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Arm:
                if (ArmSkinnedMeshRenderer != null && newMesh != null)
                {
                    ArmSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Belt:
                if (BeltSkinnedMeshRenderer != null && newMesh != null)
                {
                    BeltSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Leg:
                if (LegSkinnedMeshRenderer != null && newMesh != null)
                {
                    LegSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break;
            case EquipmentInteract.EquipmentType.Feet:
                if (FeetSkinnedMeshRenderer != null && newMesh != null)
                {
                    FeetSkinnedMeshRenderer.sharedMesh = newMesh;
                }
                break; 
        }
    }
}
