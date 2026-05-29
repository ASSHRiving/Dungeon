using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    private bool openIn = false;
    private bool openOut = false;
    private Animator _animator;
    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
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
                _animator.SetBool("OpenIn", openIn);
            }
            else
            {
                openOut = true;
                _animator.SetBool("OpenOut", openOut);
            }
        }else if(openIn == true)
        {
            openIn = false;
            _animator.SetBool("OpenIn", openIn);
        }else if(openOut == true)
        {
            openOut = false;
            _animator.SetBool("OpenOut", openOut);
        }
    }
}