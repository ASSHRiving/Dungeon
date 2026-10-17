using System;
using UnityEngine;

public class Potion : MonoBehaviour, IInteractable
{
    [SerializeField] public string itemName;
    [SerializeField] private float heal;
    [SerializeField] private int price;
    public string interactableName => itemName;

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
