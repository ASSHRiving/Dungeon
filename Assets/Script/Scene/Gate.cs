using Unity.VisualScripting;
using UnityEngine;

public class Gate : MonoBehaviour
{
    private Animator animator;
    private bool isOpen = true;
    private int openID = Animator.StringToHash("IsOpen");
    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }
    public void CloseGate()
    {
        if(audioSource != null && audioClip != null)
        {
            audioSource.PlayOneShot(audioClip);
        }
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
