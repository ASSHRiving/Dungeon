using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    private bool isOpen = false;
    Animator animator;
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }
    public void Interact()
    {
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen);
    }
}
