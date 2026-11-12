using UnityEngine;
using System.Collections.Generic;

public class Equipment : MonoBehaviour, IInteractable, IItem
{
    public string interactableName => "裝備\n" + equipmentName;
    public enum EquipmentType { Head, Chest, Arm, Belt, Leg, Feet }

    [Header("裝備數據 (ScriptableObject)")]
    [SerializeField] private EquipmentDataSO equipmentData;
    [Tooltip("階級清單 (0: 初級, 1: 中級, 2: 高級)")]
    [SerializeField] private List<EquipmentDataSO> tieredData = new List<EquipmentDataSO>();

    [SerializeField] public MeshFilter prefabRenderer;
    [Header("音效")]
    [SerializeField] private AudioClip audioClip;

    public EquipmentDataSO EquipmentData => equipmentData;
    public List<EquipmentDataSO> TieredData => tieredData;

    public string equipmentName => equipmentData != null ? equipmentData.equipmentName : "";
    public Mesh mesh => equipmentData != null ? equipmentData.characterMesh : null;
    public Mesh prefabMesh => equipmentData != null ? equipmentData.prefabMesh : null;
    public int price => equipmentData != null ? equipmentData.price : 0;
    public float damage => equipmentData != null ? equipmentData.damage : 0f;
    public float shield => equipmentData != null ? equipmentData.shield : 0f;
    public float health => equipmentData != null ? equipmentData.health : 0f;
    public float critRate => equipmentData != null ? equipmentData.critRate : 0f;
    public Sprite itemIcon => equipmentData != null ? equipmentData.itemIcon : null;
    public EquipmentType equipmentType => equipmentData != null ? equipmentData.equipmentType : EquipmentType.Head;

    public string ItemName => equipmentName;
    public Sprite ItemIcon => itemIcon;

    public string GetTooltipStatsText()
    {
        return equipmentData != null ? equipmentData.GetTooltipStatsText() : "無附加屬性";
    }

    private void Awake()
    {
        if (equipmentData == null && tieredData != null && tieredData.Count > 0)
        {
            SetEquipmentData(tieredData[0]);
        }
    }

    public void SetEquipmentData(EquipmentDataSO data)
    {
        equipmentData = data;
        UpdateMesh();
    }

    public void SetIndex(int index)
    {
        if (tieredData != null && index >= 0 && index < tieredData.Count && tieredData[index] != null)
        {
            SetEquipmentData(tieredData[index]);
        }
    }

    public void UpdateMesh()
    {
        if (prefabRenderer != null && prefabMesh != null)
        {
            prefabRenderer.sharedMesh = prefabMesh;
        }
    }

    public void Interact(Transform player)
    {
        CharacterEquipment characterEquipment = player.GetComponentInChildren<CharacterEquipment>();
        if (characterEquipment != null)
        {
            AudioSource audioSource = player.GetComponentInChildren<AudioSource>();
            if (audioSource != null && audioClip != null)
            {
                audioSource.PlayOneShot(audioClip, 10f);
            }

            characterEquipment.EquipItem(this.gameObject);
        }
    }

    string IInteractable.interactableName => interactableName;
}
