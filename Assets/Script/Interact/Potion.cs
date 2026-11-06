using System;
using UnityEngine;

public class Potion : MonoBehaviour, IInteractable, IItem
{
    [SerializeField] public string itemName;
    [SerializeField] public Sprite itemIcon;
    [SerializeField] private float heal;
    [SerializeField] private int price;
    public string interactableName => itemName;

    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;

    public string GetTooltipStatsText()
    {
        return heal > 0 ? $"回復生命: +{Mathf.RoundToInt(heal)}" : "無附加屬性";
    }

    public void Interact(Transform player)
    {
        CharacterHealthBase health = player.GetComponent<CharacterHealthBase>();
        if(health != null)
        {
            health.Recover(heal);
            Destroy(gameObject);
        }
    }
    public int Price => price;
}
