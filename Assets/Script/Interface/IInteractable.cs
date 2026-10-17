using UnityEngine;

public interface IInteractable
{
    string interactableName { get; }
    void Interact(Transform player);
}