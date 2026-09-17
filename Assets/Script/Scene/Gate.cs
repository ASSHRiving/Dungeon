using UnityEngine;

public class Gate : MonoBehaviour
{
    private Animator animator;
    private bool isOpen = true;
    private int openID = Animator.StringToHash("IsOpen"); 
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }
    private void Update()
    {
        
    }
    public void CloseGate()
    {
        if (isOpen)
        {
            isOpen = false;
            animator.SetBool(openID, isOpen);
        }
    }
    public void OpenGate()
    {
        if (!isOpen)
        {
            isOpen = true;
            animator.SetBool(openID, isOpen);
        }
    }
}
