using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Weapon currentWeapon;
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            currentWeapon.Attack(animator);
        }
        else
        {
            if (currentWeapon.StopCombat)
            {
                animator.SetBool("InCombat", false);
            }
        }
    }
}
