using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    private bool isOpen = false;
    private Animator animator;
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }
    public void Interact(Transform player)
    {
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen);
    }
}
