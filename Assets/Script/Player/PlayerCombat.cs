using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public Weapon currentWeapon;
    public Weapon nextWeapon;
    private Animator animator;
    private int weaponType = 0;
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
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            changeWeapon(nextWeapon);
        }
    }
    void changeWeapon(Weapon newWeapon)
    {
        if(currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(false);
        }
        nextWeapon = currentWeapon;
        currentWeapon = newWeapon;
        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(true);
        }
        animator.SetInteger("WeaponType", weaponType == 1? 0 : 1); 
    }
}
