using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] public string interactableName = "Door";
    string IInteractable.interactableName => interactableName;
    private bool openIn = false;
    private bool openOut = false;
    private Animator animator;
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void Interact(Transform player)
    {
        if(openIn == false && openOut == false)
        {
            Vector3 dir = player.position - transform.position;
            dir.Normalize();
            float dot = Vector3.Dot(transform.forward, dir);
            if(dot > 0)
            {
                openIn = true;
                animator.SetBool("OpenIn", openIn);
            }
            else
            {
                openOut = true;
                animator.SetBool("OpenOut", openOut);
            }
        }else if(openIn == true)
        {
            openIn = false;
            animator.SetBool("OpenIn", openIn);
        }else if(openOut == true)
        {
            openOut = false;
            animator.SetBool("OpenOut", openOut);
        }
    }
}